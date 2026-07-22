using Dapper;
using MonitorApi.Models;

namespace MonitorApi.Data;

/// <summary>Acesso a dados dos sinais: ingestão, leitura e agregações (relatórios).</summary>
public sealed class SignalsRepository
{
    private readonly Database _db;

    public SignalsRepository(Database db) => _db = db;

    /// <summary>Grava um sinal recebido do agente. Retorna o id gerado.</summary>
    public async Task<long> InsertAsync(SignalInput input, CancellationToken ct)
    {
        const string sql = """
            INSERT INTO signals (hostname, usuario, timestamp_utc, titulo_janela, processo)
            VALUES (@Hostname, @Usuario, @TimestampUtc, @TituloJanela, @Processo)
            RETURNING id;
            """;

        await using var conn = await _db.OpenAsync(ct);
        return await conn.ExecuteScalarAsync<long>(new CommandDefinition(sql, input, cancellationToken: ct));
    }

    /// <summary>Leitura dos sinais capturados, com filtros opcionais e paginação.</summary>
    public async Task<IReadOnlyList<SignalRecord>> QueryAsync(
        string? hostname, DateTimeOffset? from, DateTimeOffset? to, int limit, int offset, CancellationToken ct)
    {
        const string sql = """
            SELECT id            AS Id,
                   hostname      AS Hostname,
                   usuario       AS Usuario,
                   timestamp_utc AS TimestampUtc,
                   titulo_janela AS TituloJanela,
                   processo      AS Processo,
                   received_at   AS ReceivedAt
            FROM signals
            WHERE (@Hostname IS NULL OR hostname = @Hostname)
              AND (@From     IS NULL OR timestamp_utc >= @From)
              AND (@To       IS NULL OR timestamp_utc <  @To)
            ORDER BY timestamp_utc DESC
            LIMIT @Limit OFFSET @Offset;
            """;

        await using var conn = await _db.OpenAsync(ct);
        var rows = await conn.QueryAsync<SignalRecord>(new CommandDefinition(
            sql,
            new { Hostname = hostname, From = from, To = to, Limit = limit, Offset = offset },
            cancellationToken: ct));
        return rows.AsList();
    }

    /// <summary>
    /// Relatório: quantas amostras cada processo teve nos últimos N minutos.
    /// Responde "quantas vezes cada processo apareceu na última hora" (minutes = 60).
    /// </summary>
    public async Task<IReadOnlyList<ProcessCount>> ProcessCountsAsync(int minutes, CancellationToken ct)
    {
        const string sql = """
            SELECT processo AS Processo, COUNT(*) AS Amostras
            FROM signals
            WHERE timestamp_utc >= now() - make_interval(mins => @Minutes)
            GROUP BY processo
            ORDER BY Amostras DESC;
            """;

        await using var conn = await _db.OpenAsync(ct);
        var rows = await conn.QueryAsync<ProcessCount>(new CommandDefinition(
            sql, new { Minutes = minutes }, cancellationToken: ct));
        return rows.AsList();
    }

    /// <summary>
    /// Relatório: amostras por máquina, por hora, nas últimas N horas.
    /// Usa date_trunc para agrupar em janelas de 1 hora.
    /// </summary>
    public async Task<IReadOnlyList<MachineHourCount>> SamplesByMachineHourAsync(int hours, CancellationToken ct)
    {
        const string sql = """
            SELECT hostname AS Hostname,
                   date_trunc('hour', timestamp_utc) AS Hora,
                   COUNT(*) AS Amostras
            FROM signals
            WHERE timestamp_utc >= now() - make_interval(hours => @Hours)
            GROUP BY hostname, date_trunc('hour', timestamp_utc)
            ORDER BY Hora DESC, hostname;
            """;

        await using var conn = await _db.OpenAsync(ct);
        var rows = await conn.QueryAsync<MachineHourCount>(new CommandDefinition(
            sql, new { Hours = hours }, cancellationToken: ct));
        return rows.AsList();
    }
}
