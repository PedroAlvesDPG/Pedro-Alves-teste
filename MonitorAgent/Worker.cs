using Microsoft.Extensions.Options;
using MonitorAgent.Api;
using MonitorAgent.Models;
using MonitorAgent.Storage;

namespace MonitorAgent;

/// <summary>
/// Loop principal do agente. A cada intervalo (padrão 3s) coleta um sinal do sistema
/// — hostname, usuário, timestamp UTC e título da janela ativa — e envia à API.
/// Se a API estiver inacessível, o sinal é enfileirado localmente e reenviado depois.
/// </summary>
public sealed class Worker : BackgroundService
{
    private readonly ILogger<Worker> _logger;
    private readonly ApiClient _api;
    private readonly LocalStore _store;
    private readonly SignalFactory _signals;
    private readonly AgentOptions _options;

    public Worker(ILogger<Worker> logger, ApiClient api, LocalStore store, SignalFactory signals, IOptions<AgentOptions> options)
    {
        _logger = logger;
        _api = api;
        _store = store;
        _signals = signals;
        _options = options.Value;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _store.Initialize();
        _logger.LogInformation(
            "Agente iniciado. Intervalo={Interval}s API={Api}{Path}",
            _options.SampleIntervalSeconds, _options.ApiBaseUrl, _options.SignalsPath);

        var interval = TimeSpan.FromSeconds(Math.Max(1, _options.SampleIntervalSeconds));
        using var timer = new PeriodicTimer(interval);

        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            try
            {
                await CollectAndSendAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break; // encerramento normal do serviço
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro inesperado no ciclo de coleta.");
            }
        }
    }

    private async Task CollectAndSendAsync(CancellationToken ct)
    {
        var signal = _signals.Create();

        bool delivered = await _api.SendAsync(signal, ct);
        if (delivered)
        {
            _logger.LogDebug("Sinal enviado: {Process} [{Title}]", signal.Processo, signal.TituloJanela);
            // API está de pé: aproveita para reenviar o que ficou na fila.
            await DrainQueueAsync(ct);
        }
        else
        {
            // API offline: guarda para reenviar depois.
            _store.Enqueue(signal);
            _logger.LogDebug("Sinal enfileirado (API offline). Pendentes={Count}", _store.PendingCount());
        }
    }

    /// <summary>Reenvia sinais pendentes em lotes, enquanto a API continuar aceitando.</summary>
    private async Task DrainQueueAsync(CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
        {
            var batch = _store.Dequeue(_options.DrainBatchSize);
            if (batch.Count == 0)
                return;

            var deliveredIds = new List<long>(batch.Count);
            foreach (var pending in batch)
            {
                if (!await _api.SendAsync(pending, ct))
                    break; // API caiu de novo — para e tenta no próximo ciclo

                deliveredIds.Add(pending.LocalId);
            }

            _store.Delete(deliveredIds);

            if (deliveredIds.Count > 0)
                _logger.LogInformation("Reenviados {Count} sinais pendentes.", deliveredIds.Count);

            if (deliveredIds.Count < batch.Count)
                return; // não drenou o lote todo → API instável, aguarda próximo ciclo
        }
    }
}
