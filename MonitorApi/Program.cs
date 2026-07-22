using MonitorApi.Data;
using MonitorApi.Json;
using MonitorApi.Models;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSingleton<Database>();
builder.Services.AddScoped<SignalsRepository>();

// Datas na saída da API no formato brasileiro (dd/MM/aaaa), mantendo o valor em UTC.
builder.Services.ConfigureHttpJsonOptions(o =>
    o.SerializerOptions.Converters.Add(new BrazilianUtcDateTimeConverter()));

var app = builder.Build();

// Cria a tabela/índices no PostgreSQL ao iniciar (espera o banco subir, se necessário).
await app.Services.GetRequiredService<Database>().InitializeAsync();

// Chave de API opcional: se configurada (Api:Key), a ingestão exige o header X-Api-Key.
var apiKey = app.Configuration["Api:Key"];

app.MapGet("/", () => "MonitorApi online. Endpoints: POST /api/signals | GET /api/signals | GET /api/reports/*");

// ---------- Passo 2: Ingestão (o agente chama) ----------
app.MapPost("/api/signals", async (SignalInput input, HttpRequest req, SignalsRepository repo, CancellationToken ct) =>
{
    if (!string.IsNullOrEmpty(apiKey) && req.Headers["X-Api-Key"] != apiKey)
        return Results.Unauthorized();

    if (string.IsNullOrWhiteSpace(input.Hostname) || string.IsNullOrWhiteSpace(input.Usuario))
        return Results.BadRequest("hostname e usuario são obrigatórios.");

    long id = await repo.InsertAsync(input, ct);
    return Results.Created($"/api/signals/{id}", new { id });
});

// ---------- Passo 2: Leitura (retorna o que foi capturado) ----------
app.MapGet("/api/signals", async (
    string? hostname, DateTimeOffset? from, DateTimeOffset? to, int? limit, int? offset,
    SignalsRepository repo, CancellationToken ct) =>
{
    int take = Math.Clamp(limit ?? 100, 1, 1000);
    int skip = Math.Max(offset ?? 0, 0);
    var rows = await repo.QueryAsync(hostname, from, to, take, skip, ct);
    return Results.Ok(rows);
});

// ---------- Passo 3: Relatório A — quantas vezes cada processo apareceu na última hora ----------
app.MapGet("/api/reports/process-counts", async (int? minutes, SignalsRepository repo, CancellationToken ct) =>
{
    int window = Math.Clamp(minutes ?? 60, 1, 10080); // 1 min .. 7 dias
    var rows = await repo.ProcessCountsAsync(window, ct);
    return Results.Ok(new { janelaMinutos = window, processos = rows });
});

// ---------- Passo 3: Relatório B — amostras por máquina, por hora ----------
app.MapGet("/api/reports/samples-by-machine-hour", async (int? hours, SignalsRepository repo, CancellationToken ct) =>
{
    int window = Math.Clamp(hours ?? 24, 1, 720); // 1h .. 30 dias
    var rows = await repo.SamplesByMachineHourAsync(window, ct);
    return Results.Ok(new { janelaHoras = window, amostras = rows });
});

app.Run();
