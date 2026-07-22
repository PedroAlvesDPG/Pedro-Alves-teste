using MonitorAgent;
using MonitorAgent.Api;
using MonitorAgent.Collectors;
using MonitorAgent.Storage;

var builder = Host.CreateApplicationBuilder(args);

// Permite rodar como Serviço do Windows (e continua funcionando como console em dev).
builder.Services.AddWindowsService(options =>
{
    options.ServiceName = "MonitorAgent";
});

// Configurações (seção "Agent" do appsettings.json).
builder.Services.Configure<AgentOptions>(
    builder.Configuration.GetSection(AgentOptions.SectionName));

// Fila local (SQLite) para resiliência quando a API está offline.
builder.Services.AddSingleton(_ =>
{
    var opts = new AgentOptions();
    builder.Configuration.GetSection(AgentOptions.SectionName).Bind(opts);
    return new LocalStore(opts.DatabasePath);
});

// Coleta específica de SO por trás da interface. Para outro SO, troca-se só esta linha.
builder.Services.AddSingleton<IActivityCollector, WindowsActivityCollector>();

// Relógio (TimeProvider do .NET) e a fábrica de sinais que carimba o UTC.
builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddSingleton(sp => new SignalFactory(
    sp.GetRequiredService<IActivityCollector>(),
    sp.GetRequiredService<TimeProvider>(),
    Environment.MachineName,
    Environment.UserName));

// Cliente HTTP da API.
builder.Services.AddHttpClient<ApiClient>();

builder.Services.AddHostedService<Worker>();

var host = builder.Build();
host.Run();
