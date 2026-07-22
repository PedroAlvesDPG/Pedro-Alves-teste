using MonitorApi.Time;

namespace MonitorApi.Models;

/// <summary>Payload de ingestão — o que o agente envia via POST (JSON camelCase).</summary>
public sealed record SignalInput(
    string Hostname,
    string Usuario,
    DateTimeOffset TimestampUtc,
    string TituloJanela,
    string Processo);

/// <summary>Registro de leitura — o que a API devolve (inclui id e quando foi recebido).</summary>
/// <remarks>Timestamps em DateTime UTC: o Npgsql mapeia 'timestamptz' para DateTime (Kind=Utc).</remarks>
public sealed record SignalRecord(
    long Id,
    string Hostname,
    string Usuario,
    DateTime TimestampUtc,
    string TituloJanela,
    string Processo,
    DateTime ReceivedAt)
{
    /// <summary>Mesmo instante de TimestampUtc, exibido no horário de Brasília (só apresentação).</summary>
    public string TimestampLocal => BrazilTime.Format(TimestampUtc);
}

/// <summary>Linha do relatório "quantas vezes cada processo apareceu".</summary>
public sealed record ProcessCount(string Processo, long Amostras);

/// <summary>Linha do relatório "amostras por máquina, por hora".</summary>
public sealed record MachineHourCount(string Hostname, DateTime Hora, long Amostras)
{
    /// <summary>Mesma hora de Hora, exibida no horário de Brasília (só apresentação).</summary>
    public string HoraLocal => BrazilTime.Format(Hora);
}
