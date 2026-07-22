using MonitorAgent.Collectors;
using MonitorAgent.Models;

namespace MonitorAgent;

/// <summary>
/// Monta um <see cref="Signal"/> a partir do coletor de SO e do relógio.
/// Lógica isolada de propósito (sem HTTP, sem SQLite, sem Win32) para ser testável:
/// garante o carimbo em UTC e o mapeamento correto dos campos.
/// </summary>
public sealed class SignalFactory
{
    private readonly IActivityCollector _collector;
    private readonly TimeProvider _clock;
    private readonly string _hostname;
    private readonly string _userName;

    public SignalFactory(IActivityCollector collector, TimeProvider clock, string hostname, string userName)
    {
        _collector = collector;
        _clock = clock;
        _hostname = hostname;
        _userName = userName;
    }

    /// <summary>Coleta o estado atual e devolve o sinal pronto para envio.</summary>
    public Signal Create()
    {
        var snapshot = _collector.Collect();

        return new Signal
        {
            Hostname = _hostname,
            Usuario = _userName,
            // Sempre UTC: TimeProvider.GetUtcNow() retorna um DateTimeOffset com offset zero.
            TimestampUtc = _clock.GetUtcNow(),
            TituloJanela = snapshot?.WindowTitle ?? string.Empty,
            Processo = snapshot?.ProcessName ?? string.Empty,
        };
    }
}
