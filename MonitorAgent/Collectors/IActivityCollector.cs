namespace MonitorAgent.Collectors;

/// <summary>
/// Parte do sinal que é ESPECÍFICA do sistema operacional: qual janela/aplicativo
/// está em foco. Hostname, usuário e timestamp são coletados fora daqui, pois não
/// dependem do SO.
/// </summary>
public readonly record struct ActivitySnapshot(string ProcessName, string WindowTitle);

/// <summary>
/// Abstrai a coleta do sinal específica de SO. Há uma implementação por plataforma
/// (hoje: Windows). Isolar atrás desta interface deixa o resto do agente independente
/// de SO — e permite testar o fluxo com um coletor "fake", sem depender da Win32.
/// </summary>
public interface IActivityCollector
{
    /// <summary>Coleta a janela/aplicativo em foco. Retorna null se não houver ou não for possível ler.</summary>
    ActivitySnapshot? Collect();
}
