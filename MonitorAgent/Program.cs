using MonitorAgent;
using MonitorAgent.Api;
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

// Cliente HTTP da API.
builder.Services.AddHttpClient<ApiClient>();

builder.Services.AddHostedService<Worker>();

var host = builder.Build();
host.Run();
