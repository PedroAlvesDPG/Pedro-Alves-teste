using Npgsql;

namespace MonitorApi.Data;

/// <summary>
/// Fábrica de conexões com o PostgreSQL e criação do schema na inicialização.
/// O SQL fica explícito aqui de propósito — fácil de ler e entender o que roda no banco.
/// </summary>
public sealed class Database
{
    private readonly string _connectionString;
    private readonly ILogger<Database> _logger;

    public Database(IConfiguration config, ILogger<Database> logger)
    {
        _connectionString = config.GetConnectionString("Postgres")
            ?? throw new InvalidOperationException("ConnectionStrings:Postgres não configurada.");
        _logger = logger;
    }

    /// <summary>Abre uma conexão pronta para uso (lembre de fazer 'await using').</summary>
    public async Task<NpgsqlConnection> OpenAsync(CancellationToken ct = default)
    {
        var conn = new NpgsqlConnection(_connectionString);
        await conn.OpenAsync(ct);
        return conn;
    }

    /// <summary>
    /// Cria a tabela de sinais e os índices, se ainda não existirem.
    /// Chamado uma vez no start da API. Tenta algumas vezes porque o banco
    /// (em Docker) pode demorar alguns segundos para aceitar conexões.
    /// </summary>
    public async Task InitializeAsync(CancellationToken ct = default)
    {
        const string ddl = """
            CREATE TABLE IF NOT EXISTS signals (
                id            BIGSERIAL   PRIMARY KEY,
                hostname      TEXT        NOT NULL,
                usuario       TEXT        NOT NULL,
                timestamp_utc TIMESTAMPTZ NOT NULL,
                titulo_janela TEXT        NOT NULL,
                processo      TEXT        NOT NULL,
                received_at   TIMESTAMPTZ NOT NULL DEFAULT now()
            );
            -- Índices compostos pensados para as queries reais (não colunas soltas).
            -- Remove os índices single-column antigos, redundantes com os compostos abaixo.
            DROP INDEX IF EXISTS ix_signals_ts;
            DROP INDEX IF EXISTS ix_signals_host;
            DROP INDEX IF EXISTS ix_signals_proc;

            -- Relatório "process-counts": filtra por janela de tempo e agrupa por processo.
            CREATE INDEX IF NOT EXISTS ix_signals_ts_proc ON signals (timestamp_utc, processo);

            -- Relatório "samples-by-machine-hour": filtra por tempo e agrupa por máquina/hora.
            CREATE INDEX IF NOT EXISTS ix_signals_ts_host ON signals (timestamp_utc, hostname);

            -- Leitura "/api/signals?hostname=...": filtra por máquina e ordena por tempo desc.
            CREATE INDEX IF NOT EXISTS ix_signals_host_ts ON signals (hostname, timestamp_utc DESC);

            -- View de leitura: mostra o MESMO dado com colunas extras em horário de Brasília.
            -- A tabela 'signals' continua 100% em UTC; a view só facilita a navegação (ex.: no Adminer).
            CREATE OR REPLACE VIEW signals_local AS
            SELECT
                id,
                hostname,
                usuario,
                timestamp_utc,
                (timestamp_utc AT TIME ZONE 'America/Sao_Paulo') AS timestamp_brasilia,
                titulo_janela,
                processo,
                received_at,
                (received_at   AT TIME ZONE 'America/Sao_Paulo') AS received_at_brasilia
            FROM signals;
            """;

        const int maxAttempts = 10;
        for (int attempt = 1; attempt <= maxAttempts; attempt++)
        {
            try
            {
                await using var conn = await OpenAsync(ct);
                await using var cmd = new NpgsqlCommand(ddl, conn);
                await cmd.ExecuteNonQueryAsync(ct);
                _logger.LogInformation("Schema do PostgreSQL pronto.");
                return;
            }
            catch (NpgsqlException ex) when (attempt < maxAttempts)
            {
                _logger.LogWarning("Banco ainda não disponível (tentativa {Attempt}/{Max}): {Msg}",
                    attempt, maxAttempts, ex.Message);
                await Task.Delay(TimeSpan.FromSeconds(2), ct);
            }
        }

        throw new InvalidOperationException("Não foi possível conectar ao PostgreSQL após várias tentativas.");
    }
}
