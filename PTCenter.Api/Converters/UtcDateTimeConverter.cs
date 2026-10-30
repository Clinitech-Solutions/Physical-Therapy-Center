using System.Text.Json;
using System.Text.Json.Serialization;

namespace PTCenter.Api.Converters;

/// <summary>EF returns UTC database timestamps with Unspecified kind. Always include the UTC designator on the wire.</summary>
public sealed class UtcDateTimeConverter : JsonConverter<DateTime>
{
    private static DateTime Utc(DateTime value) => value.Kind == DateTimeKind.Local ? value.ToUniversalTime() : DateTime.SpecifyKind(value, DateTimeKind.Utc);
    public override DateTime Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) => Utc(reader.GetDateTime());
    public override void Write(Utf8JsonWriter writer, DateTime value, JsonSerializerOptions options) => writer.WriteStringValue(Utc(value));
}
