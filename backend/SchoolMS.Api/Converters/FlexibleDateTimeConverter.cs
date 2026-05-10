using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace SchoolMS.Api.Converters;

/// <summary>
/// Accepts date-only ("yyyy-MM-dd") and full ISO 8601 datetime strings for DateTime properties.
/// ASP.NET Core's default System.Text.Json rejects date-only strings for DateTime fields,
/// which breaks any frontend input that sends a plain date without a time component.
/// </summary>
public sealed class FlexibleDateTimeConverter : JsonConverter<DateTime>
{
    private static readonly string[] Formats =
    [
        "yyyy-MM-dd",
        "yyyy-MM-ddTHH:mm:ss",
        "yyyy-MM-ddTHH:mm:ssZ",
        "yyyy-MM-ddTHH:mm:ss.fffZ",
        "yyyy-MM-ddTHH:mm:sszzz",
        "dd/MM/yyyy",
        "d/MM/yyyy",
        "dd/M/yyyy",
        "d/M/yyyy",
    ];

    public override DateTime Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var raw = reader.GetString();
        if (string.IsNullOrWhiteSpace(raw))
            return default;

        // Try the built-in parser first (handles timezone offsets / Z suffix natively)
        if (DateTime.TryParse(raw, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var dt))
            return dt;

        // Fall back to explicit formats (date-only strings land here)
        if (DateTime.TryParseExact(raw, Formats, CultureInfo.InvariantCulture, DateTimeStyles.None, out var dtExact))
            return dtExact;

        throw new JsonException($"Unable to parse '{raw}' as DateTime.");
    }

    public override void Write(Utf8JsonWriter writer, DateTime value, JsonSerializerOptions options)
        => writer.WriteStringValue(value.ToString("yyyy-MM-ddTHH:mm:ss"));
}
