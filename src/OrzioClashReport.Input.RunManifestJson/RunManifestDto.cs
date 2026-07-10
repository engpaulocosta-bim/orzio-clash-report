using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace OrzioClashReport.Input.RunManifestJson
{
    /// <summary>
    /// Raw deserialization shape of a run manifest JSON document. Internal to this adapter: never exposed to
    /// the Core or the CLI. <see cref="SchemaVersion"/> belongs only here; the Core does not know it exists.
    /// Rejects unmapped properties so unknown/misspelled fields fail loudly instead of being silently ignored.
    /// </summary>
    [JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
    internal sealed class RunManifestDto
    {
        [JsonPropertyName("schemaVersion")]
        public int? SchemaVersion { get; set; }

        [JsonPropertyName("runId")]
        public string? RunId { get; set; }

        [JsonPropertyName("createdAt")]
        public DateTimeOffset? CreatedAt { get; set; }

        [JsonPropertyName("models")]
        public List<ModelRevisionDto?>? Models { get; set; }
    }
}
