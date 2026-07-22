using MonitorAgent;
using MonitorAgent.Collectors;

namespace MonitorAgent.Tests;

/// <summary>
/// Testes da lógica de montagem do sinal. Usam um coletor "fake" e um relógio fixo —
/// é justamente para isso que existe a interface IActivityCollector e o TimeProvider.
/// </summary>
public class SignalFactoryTests
{
    /// <summary>Coletor de teste: devolve o que mandarmos, sem tocar na Win32.</summary>
    private sealed class FakeCollector(ActivitySnapshot? snapshot) : IActivityCollector
    {
        public ActivitySnapshot? Collect() => snapshot;
    }

    /// <summary>Relógio fixo para tornar o timestamp determinístico no teste.</summary>
    private sealed class FixedClock(DateTimeOffset now) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => now;
    }

    private static SignalFactory Build(ActivitySnapshot? snapshot, DateTimeOffset now) =>
        new(new FakeCollector(snapshot), new FixedClock(now), "HOST-1", "usuario1");

    [Fact]
    public void Create_deve_carimbar_timestamp_em_UTC()
    {
        // Mesmo instante, mas expresso em -03:00: o sinal deve sair em UTC (offset zero).
        var instante = new DateTimeOffset(2026, 7, 22, 17, 0, 0, TimeSpan.Zero);
        var factory = Build(new ActivitySnapshot("chrome", "Gmail"), instante);

        var signal = factory.Create();

        Assert.Equal(TimeSpan.Zero, signal.TimestampUtc.Offset);
        Assert.Equal(instante, signal.TimestampUtc);
    }

    [Fact]
    public void Create_deve_mapear_processo_e_titulo_do_snapshot()
    {
        var factory = Build(new ActivitySnapshot("devenv", "Program.cs - VS"), DateTimeOffset.UnixEpoch);

        var signal = factory.Create();

        Assert.Equal("devenv", signal.Processo);
        Assert.Equal("Program.cs - VS", signal.TituloJanela);
        Assert.Equal("HOST-1", signal.Hostname);
        Assert.Equal("usuario1", signal.Usuario);
    }

    [Fact]
    public void Create_sem_janela_em_foco_deve_gerar_campos_vazios_mas_valido()
    {
        // Coletor retorna null (nenhuma janela em foco) — não pode quebrar nem virar null.
        var factory = Build(null, DateTimeOffset.UnixEpoch);

        var signal = factory.Create();

        Assert.Equal(string.Empty, signal.Processo);
        Assert.Equal(string.Empty, signal.TituloJanela);
        Assert.Equal("HOST-1", signal.Hostname);
    }
}
