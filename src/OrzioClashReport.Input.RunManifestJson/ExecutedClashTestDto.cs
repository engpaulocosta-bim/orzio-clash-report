using System.Text.Json.Serialization;

namespace OrzioClashReport.Input.RunManifestJson
{
    /// <summary>
    /// Raw deserialization shape of one entry in the run manifest's "executedClashTests" array. Internal to
    /// this adapter: never exposed to the Core or the CLI. Rejects unmapped properties, same rationale as
    /// <see cref="RunManifestDto"/>.
    /// </summary>
    [JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
    internal sealed class ExecutedClashTestDto
    {
        [JsonPropertyName("name")]
        public string? Name { get; set; }

        [JsonPropertyName("modelA")]
        public ModelIdentityDto? ModelA { get; set; }

        [JsonPropertyName("modelB")]
        public ModelIdentityDto? ModelB { get; set; }
    }
}
