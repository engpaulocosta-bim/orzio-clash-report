using System;
using System.Text.Json;
using System.Text.Json.Serialization;
using OrzioClashReport.Core.Model;

namespace OrzioClashReport.Persistence.RunSnapshotJson
{
    /// <summary>
    /// Reads and writes the raw <see cref="ClashStatus"/> received from the source, persisted as the exact
    /// enum name. Only the six defined names are accepted, compared with ordinal, case-sensitive equality: no
    /// numeric values, no case folding, and no lifecycle vocabulary (e.g. "StillOpen", "Unverifiable"). This is
    /// deliberately stricter than <see cref="JsonStringEnumConverter"/>, which would tolerate numbers and, in
    /// some configurations, case-insensitive names. The snapshot never stores a derived lifecycle status; only
    /// the raw source status flows through here.
    /// </summary>
    internal sealed class StrictClashStatusJsonConverter : JsonConverter<ClashStatus>
    {
        public override ClashStatus Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType != JsonTokenType.String)
            {
                throw new JsonException("Expected a JSON string for a clash status.");
            }

            string? value = reader.GetString();

            // A C# string switch compares ordinally, matching the case-sensitive contract.
            switch (value)
            {
                case "New":
                    return ClashStatus.New;
                case "Active":
                    return ClashStatus.Active;
                case "Reviewed":
                    return ClashStatus.Reviewed;
                case "Approved":
                    return ClashStatus.Approved;
                case "Resolved":
                    return ClashStatus.Resolved;
                case "Unknown":
                    return ClashStatus.Unknown;
                default:
                    throw new JsonException(
                        $"Clash status '{value}' is not one of the recognized raw statuses: "
                        + "New, Active, Reviewed, Approved, Resolved, Unknown.");
            }
        }

        public override void Write(Utf8JsonWriter writer, ClashStatus value, JsonSerializerOptions options)
        {
            string name;
            switch (value)
            {
                case ClashStatus.New:
                    name = "New";
                    break;
                case ClashStatus.Active:
                    name = "Active";
                    break;
                case ClashStatus.Reviewed:
                    name = "Reviewed";
                    break;
                case ClashStatus.Approved:
                    name = "Approved";
                    break;
                case ClashStatus.Resolved:
                    name = "Resolved";
                    break;
                case ClashStatus.Unknown:
                    name = "Unknown";
                    break;
                default:
                    throw new JsonException($"Clash status value '{value}' is not a defined ClashStatus.");
            }

            writer.WriteStringValue(name);
        }
    }
}
