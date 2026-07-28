using System;
using System.Text.Json;
using System.Text.Json.Serialization;
using OrzioClashReport.Core.Model;

namespace OrzioClashReport.Persistence.IdentityGovernanceJson
{
    /// <summary>Reads and writes the exact supported human identity decision kind names with strict ordinal matching.</summary>
    internal sealed class StrictHumanIdentityDecisionKindJsonConverter : JsonConverter<HumanIdentityDecisionKind>
    {
        public override HumanIdentityDecisionKind Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType != JsonTokenType.String)
            {
                throw new JsonException("Expected a JSON string for a human identity decision kind.");
            }

            string? value = reader.GetString();
            switch (value)
            {
                case "ConfirmSameIdentity":
                    return HumanIdentityDecisionKind.ConfirmSameIdentity;
                case "RejectSameIdentity":
                    return HumanIdentityDecisionKind.RejectSameIdentity;
                default:
                    throw new JsonException(
                        $"Human identity decision kind '{value}' is not recognized. "
                        + "Supported values: ConfirmSameIdentity, RejectSameIdentity.");
            }
        }

        public override void Write(Utf8JsonWriter writer, HumanIdentityDecisionKind value, JsonSerializerOptions options)
        {
            switch (value)
            {
                case HumanIdentityDecisionKind.ConfirmSameIdentity:
                    writer.WriteStringValue("ConfirmSameIdentity");
                    return;
                case HumanIdentityDecisionKind.RejectSameIdentity:
                    writer.WriteStringValue("RejectSameIdentity");
                    return;
                default:
                    throw new JsonException(
                        $"Human identity decision kind value '{value}' is not a defined HumanIdentityDecisionKind.");
            }
        }
    }
}
