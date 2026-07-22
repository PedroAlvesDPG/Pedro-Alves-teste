namespace MonitorAgent;

/// <summary>Configurações do agente, carregadas de appsettings.json (seção "Agent").</summary>
public sealed class AgentOptions
{
    public const string SectionName = "Agent";

    /// <summary>Intervalo entre coletas/envios de sinal (segundos). Padrão: 3s.</summary>
    public int SampleIntervalSeconds { get; set; } = 3;

    /// <summary>URL base da API que recebe os sinais (ex.: https://localhost:5001).</summary>
    public string ApiBaseUrl { get; set; } = "http://localhost:5000";

    /// <summary>Caminho (rota) do endpoint que recebe um sinal via POST.</summary>
    public string SignalsPath { get; set; } = "/api/signals";

    /// <summary>Chave de API enviada no header (opcional). Vazio = sem autenticação.</summary>
    public string ApiKey { get; set; } = string.Empty;

    /// <summary>Timeout de cada requisição HTTP (segundos).</summary>
    public int HttpTimeoutSeconds { get; set; } = 10;

    /// <summary>Caminho do banco SQLite local (fila de resiliência quando a API está offline).</summary>
    public string DatabasePath { get; set; } =
        Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),
            "MonitorAgent", "buffer.db");

    /// <summary>Máximo de sinais enfileirados reenviados por ciclo, quando a API volta.</summary>
    public int DrainBatchSize { get; set; } = 50;

    /// <summary>Dias de retenção de sinais já entregues no buffer local.</summary>
    public int LocalRetentionDays { get; set; } = 7;
}
