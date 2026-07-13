using System.Text.Json.Serialization;

namespace OrzioClashReport.Input.RunManifestJson
{
    /// <summary>
    /// Raw deserialization shape of a revision-free model identity, used inside an executed clash test's
    /// "modelA"/"modelB". Internal to this adapter: never exposed to the Core or the CLI. Deliberately has no
    /// revision, source file, content hash, or published-at field -- coverage declarations are revision-free
    /// by design. Rejects unmapped properties, same rationale as <see cref="RunManifestDto"/>.
    /// </summary>
    [JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
    internal sealed class ModelIdentityDto
    {
        [JsonPropertyName("company")]
        public string? Company { get; set; }

        [JsonPropertyName("discipline")]
        public string? Discipline { get; set; }

        [JsonPropertyName("modelName")]
        public string? ModelName { get; set; }
    }
}
