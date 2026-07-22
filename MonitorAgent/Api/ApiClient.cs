using System.Net.Http.Json;
using Microsoft.Extensions.Options;
using MonitorAgent.Models;

namespace MonitorAgent.Api;

/// <summary>
/// Cliente HTTP que envia sinais para a API de monitoramento.
/// Um POST por sinal, em JSON. Retorna true se a API confirmou o recebimento.
/// </summary>
public sealed class ApiClient
{
    private readonly HttpClient _http;
    private readonly ILogger<ApiClient> _logger;
    private readonly AgentOptions _options;

    public ApiClient(HttpClient http, IOptions<AgentOptions> options, ILogger<ApiClient> logger)
    {
        _options = options.Value;
        _logger = logger;

        _http = http;
        _http.BaseAddress = new Uri(_options.ApiBaseUrl);
        _http.Timeout = TimeSpan.FromSeconds(Math.Max(1, _options.HttpTimeoutSeconds));
        if (!string.IsNullOrWhiteSpace(_options.ApiKey))
            _http.DefaultRequestHeaders.Add("X-Api-Key", _options.ApiKey);
    }

    /// <summary>Envia um sinal. Retorna true em sucesso; false se a API está inacessível ou recusou.</summary>
    public async Task<bool> SendAsync(Signal signal, CancellationToken ct)
    {
        try
        {
            using var response = await _http.PostAsJsonAsync(_options.SignalsPath, signal, ct);
            if (response.IsSuccessStatusCode)
                return true;

            _logger.LogWarning("API respondeu {Status} ao enviar sinal.", (int)response.StatusCode);
            return false;
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            throw; // encerramento do serviço — propaga
        }
        catch (Exception ex)
        {
            // API offline / timeout / DNS — o sinal vai para o buffer e é reenviado depois.
            _logger.LogWarning(ex, "Falha ao enviar sinal para a API.");
            return false;
        }
    }
}
