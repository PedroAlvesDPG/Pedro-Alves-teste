using System.Runtime.Versioning;
using MonitorAgent.Native;

namespace MonitorAgent.Collectors;

/// <summary>
/// Implementação Windows do <see cref="IActivityCollector"/>. Usa a Win32
/// (GetForegroundWindow) para descobrir a janela em foco e o processo dono dela.
/// </summary>
[SupportedOSPlatform("windows")]
public sealed class WindowsActivityCollector : IActivityCollector
{
    public ActivitySnapshot? Collect()
    {
        var info = Win32.GetForegroundInfo();
        if (info is null)
            return null;

        return new ActivitySnapshot(info.Value.ProcessName, info.Value.WindowTitle);
    }
}
