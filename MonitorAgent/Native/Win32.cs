using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;

namespace MonitorAgent.Native;

/// <summary>
/// P/Invoke da Win32 para descobrir qual janela/aplicativo está em foco.
/// Captura apenas metadados (nome do processo e título da janela) — nunca conteúdo digitado.
/// </summary>
internal static class Win32
{
    [DllImport("user32.dll")]
    private static extern IntPtr GetForegroundWindow();

    [DllImport("user32.dll")]
    private static extern int GetWindowThreadProcessId(IntPtr hWnd, out uint processId);

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    private static extern int GetWindowText(IntPtr hWnd, StringBuilder text, int count);

    [DllImport("user32.dll")]
    private static extern int GetWindowTextLength(IntPtr hWnd);

    /// <summary>Idle time do usuário (ms desde o último input de teclado/mouse).</summary>
    [DllImport("user32.dll")]
    private static extern bool GetLastInputInfo(ref LASTINPUTINFO plii);

    [DllImport("kernel32.dll")]
    private static extern uint GetTickCount();

    [StructLayout(LayoutKind.Sequential)]
    private struct LASTINPUTINFO
    {
        public uint cbSize;
        public uint dwTime;
    }

    public readonly record struct ForegroundInfo(uint ProcessId, string ProcessName, string WindowTitle);

    /// <summary>Retorna o processo e o título da janela atualmente em foco, ou null se indisponível.</summary>
    public static ForegroundInfo? GetForegroundInfo()
    {
        var hWnd = GetForegroundWindow();
        if (hWnd == IntPtr.Zero)
            return null;

        _ = GetWindowThreadProcessId(hWnd, out uint pid);
        if (pid == 0)
            return null;

        string processName;
        try
        {
            using var proc = Process.GetProcessById((int)pid);
            processName = proc.ProcessName;
        }
        catch (ArgumentException)
        {
            // Processo terminou entre a leitura do foco e a consulta.
            return null;
        }

        int len = GetWindowTextLength(hWnd);
        var sb = new StringBuilder(len + 1);
        GetWindowText(hWnd, sb, sb.Capacity);

        return new ForegroundInfo(pid, processName, sb.ToString());
    }

    /// <summary>Milissegundos desde o último input do usuário.</summary>
    public static long GetIdleMilliseconds()
    {
        var info = new LASTINPUTINFO { cbSize = (uint)Marshal.SizeOf<LASTINPUTINFO>() };
        if (!GetLastInputInfo(ref info))
            return 0;

        return unchecked(GetTickCount() - info.dwTime);
    }
}
