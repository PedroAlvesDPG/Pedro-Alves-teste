using System.Text.Json.Serialization;

namespace MonitorAgent.Models;

/// <summary>
/// Um sinal coletado do sistema, enviado à API a cada ciclo.
/// Contém, no mínimo: hostname, usuário, timestamp UTC e o título da janela ativa.
/// </summary>
public sealed class Signal
{
    /// <summary>Id local no buffer de resiliência (não enviado à API).</summary>
    [JsonIgnore]
    public long LocalId { get; set; }

    /// <summary>Nome da máquina (Environment.MachineName).</summary>
    [JsonPropertyName("hostname")]
    public string Hostname { get; set; } = string.Empty;

    /// <summary>Usuário logado no Windows.</summary>
    [JsonPropertyName("usuario")]
    public string Usuario { get; set; } = string.Empty;

    /// <summary>Momento da coleta em UTC (ISO 8601).</summary>
    [JsonPropertyName("timestampUtc")]
    public DateTimeOffset TimestampUtc { get; set; }

    /// <summary>Título da janela em foco no momento da coleta.</summary>
    [JsonPropertyName("tituloJanela")]
    public string TituloJanela { get; set; } = string.Empty;

    /// <summary>Nome do processo da janela em foco (extra, além do mínimo exigido).</summary>
    [JsonPropertyName("processo")]
    public string Processo { get; set; } = string.Empty;
}
