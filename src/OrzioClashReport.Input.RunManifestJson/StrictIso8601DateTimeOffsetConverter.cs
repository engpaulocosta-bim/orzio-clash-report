using System;
using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;

namespace OrzioClashReport.Input.RunManifestJson
{
    /// <summary>
    /// Deserializes JSON timestamp strings into <see cref="DateTimeOffset"/>, rejecting any value that lacks
    /// an explicit UTC offset or 'Z'. The framework's default converter silently treats an offset-less string
    /// as UTC; that is exactly the silent inference this manifest format must never perform. Applies to both
    /// <see cref="DateTimeOffset"/> and <see cref="DateTimeOffset"/>? properties.
    /// </summary>
    internal sealed class StrictIso8601DateTimeOffsetConverter : JsonConverter<DateTimeOffset>
    {
        private static readonly Regex Iso8601WithOffsetPattern = new Regex(
            @"^\d{4}-\d{2}-\d{2}T\d{2}:\d{2}:\d{2}(\.\d+)?(Z|[+-]\d{2}:\d{2})$",
            RegexOptions.Compiled | RegexOptions.CultureInvariant);

        public override DateTimeOffset Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType != JsonTokenType.String)
            {
                throw new JsonException("Expected a JSON string for a timestamp.");
            }

            string? value = reader.GetString();
            if (value == null || !Iso8601WithOffsetPattern.IsMatch(value))
            {
                throw new JsonException(
                    $"Timestamp '{value}' must be an ISO 8601 date-time with an explicit offset (e.g. '+01:00') or 'Z'.");
            }

            if (!DateTimeOffset.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.None, out var result))
            {
                throw new JsonException($"Timestamp '{value}' is not a valid ISO 8601 date-time.");
            }

            return result;
        }

        public override void Write(Utf8JsonWriter writer, DateTimeOffset value, JsonSerializerOptions options)
        {
            writer.WriteStringValue(value.ToString("yyyy-MM-ddTHH:mm:ss.fffffffzzz", CultureInfo.InvariantCulture));
        }
    }
}
