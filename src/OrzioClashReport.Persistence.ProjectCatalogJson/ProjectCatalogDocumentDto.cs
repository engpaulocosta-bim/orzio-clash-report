using System.Text.Json.Serialization;

namespace OrzioClashReport.Persistence.ProjectCatalogJson
{
    /// <summary>Root shape of a schema-version-1 project-catalog JSON document.</summary>
    [JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
    internal sealed class ProjectCatalogDocumentDto
    {
        [JsonPropertyName("schemaVersion")]
        [JsonPropertyOrder(0)]
        public int? SchemaVersion { get; set; }

        [JsonPropertyName("projectId")]
        [JsonPropertyOrder(1)]
        public string? ProjectId { get; set; }

        [JsonPropertyName("displayName")]
        [JsonPropertyOrder(2)]
        public string? DisplayName { get; set; }

        [JsonPropertyName("runIndexPath")]
        [JsonPropertyOrder(3)]
        public string? RunIndexPath { get; set; }

        [JsonPropertyName("longitudinalReportPath")]
        [JsonPropertyOrder(4)]
        public string? LongitudinalReportPath { get; set; }
    }
}
