using System;
using System.Text.Json.Serialization;

namespace OrzioClashReport.Input.RunManifestJson
{
    /// <summary>
    /// Raw deserialization shape of one entry in the run manifest's "models" array. Internal to this adapter:
    /// never exposed to the Core or the CLI. Rejects unmapped properties, same rationale as
    /// <see cref="RunManifestDto"/>.
    /// </summary>
    [JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
    internal sealed class ModelRevisionDto
    {
        [JsonPropertyName("company")]
        public string? Company { get; set; }

        [JsonPropertyName("discipline")]
        public string? Discipline { get; set; }

        [JsonPropertyName("modelName")]
        public string? ModelName { get; set; }

        [JsonPropertyName("revision")]
        public string? Revision { get; set; }

        [JsonPropertyName("sourceFileName")]
        public string? SourceFileName { get; set; }

        [JsonPropertyName("sourceFilePath")]
        public string? SourceFilePath { get; set; }

        [JsonPropertyName("contentHash")]
        public string? ContentHash { get; set; }

        [JsonPropertyName("publishedAt")]
        public DateTimeOffset? PublishedAt { get; set; }
    }
}
