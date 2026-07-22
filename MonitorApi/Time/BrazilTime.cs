using System.Globalization;

namespace MonitorApi.Time;

/// <summary>
/// Converte um instante UTC para o horário de Brasília (America/Sao_Paulo) apenas
/// para EXIBIÇÃO. O dado continua armazenado em UTC — este helper só produz um texto
/// amigável para quem lê no Brasil, sem mexer na fonte da verdade.
/// </summary>
public static class BrazilTime
{
    // Resolve o fuso uma única vez. Aceita o id IANA (Linux/.NET moderno) e o id do Windows.
    private static readonly TimeZoneInfo Zone = Resolve();

    private static TimeZoneInfo Resolve()
    {
        foreach (var id in new[] { "America/Sao_Paulo", "E. South America Standard Time" })
        {
            try { return TimeZoneInfo.FindSystemTimeZoneById(id); }
            catch (TimeZoneNotFoundException) { }
            catch (InvalidTimeZoneException) { }
        }
        return TimeZoneInfo.Utc; // fallback improvável
    }

    /// <summary>Formata um UTC como "dd/MM/aaaa HH:mm:ss (Brasília)".</summary>
    public static string Format(DateTime utc)
    {
        var utcKind = DateTime.SpecifyKind(utc, DateTimeKind.Utc);
        var local = TimeZoneInfo.ConvertTimeFromUtc(utcKind, Zone);
        return local.ToString("dd/MM/yyyy HH:mm:ss", CultureInfo.InvariantCulture) + " (Brasília)";
    }
}
