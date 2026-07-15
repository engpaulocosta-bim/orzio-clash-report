using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace OrzioClashReport.Persistence.RunIndexJson
{
    /// <summary>Root shape of a schema-version-1 run-index JSON document.</summary>
    [JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
    internal sealed class RunIndexDocumentDto
    {
        [JsonPropertyName("schemaVersion")]
        [JsonPropertyOrder(0)]
        public int? SchemaVersion { get; set; }

        [JsonPropertyName("snapshotPaths")]
        [JsonPropertyOrder(1)]
        public List<string?>? SnapshotPaths { get; set; }
    }
}
