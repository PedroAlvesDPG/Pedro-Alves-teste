using Microsoft.AspNetCore.SignalR;

namespace MonitorApi.Hubs;

/// <summary>
/// Hub de tempo real. Não tem métodos: os clientes (dashboard) só se conectam e
/// escutam. Quem publica é a API, ao receber um sinal novo, via IHubContext.
/// O evento enviado aos clientes chama-se "novoSinal".
/// </summary>
public sealed class SignalsHub : Hub
{
}
