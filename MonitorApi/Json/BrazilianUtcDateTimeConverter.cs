using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace MonitorApi.Json;

/// <summary>
/// Serializa DateTime (sempre UTC) no formato brasileiro de leitura: "dd/MM/aaaa HH:mm:ss UTC".
///
/// IMPORTANTE: o valor continua em UTC — apenas a APRESENTAÇÃO muda. Não convertemos
/// para o fuso local de propósito: manter tudo em UTC é o que garante timestamps corretos
/// e comparáveis entre máquinas. O sufixo "UTC" deixa explícito que não é horário local.
/// </summary>
public sealed class BrazilianUtcDateTimeConverter : JsonConverter<DateTime>
{
    private const string Format = "dd/MM/yyyy HH:mm:ss";

    public override void Write(Utf8JsonWriter writer, DateTime value, JsonSerializerOptions options)
    {
        // Garante que o valor é tratado como UTC antes de formatar.
        var utc = value.Kind == DateTimeKind.Utc ? value : value.ToUniversalTime();
        writer.WriteStringValue(utc.ToString(Format, CultureInfo.InvariantCulture) + " UTC");
    }

    public override DateTime Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var text = reader.GetString() ?? string.Empty;
        text = text.Replace(" UTC", string.Empty).Trim();

        // Aceita o formato brasileiro; se falhar, tenta ISO 8601 como fallback.
        if (DateTime.TryParseExact(text, Format, CultureInfo.InvariantCulture,
                DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal, out var dt))
            return dt;

        return DateTime.Parse(text, CultureInfo.InvariantCulture,
            DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal);
    }
}
