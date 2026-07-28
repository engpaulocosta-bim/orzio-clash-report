using System.Collections.Generic;
using System.Text.Json.Serialization;
using OrzioClashReport.Core.Model;

namespace OrzioClashReport.Persistence.IdentityGovernanceJson
{
    /// <summary>Root shape of a schema-version-1 identity-governance JSON document.</summary>
    [JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
    internal sealed class IdentityGovernanceDocumentDto
    {
        [JsonPropertyName("schemaVersion")]
        [JsonPropertyOrder(0)]
        public int? SchemaVersion { get; set; }

        [JsonPropertyName("projectId")]
        [JsonPropertyOrder(1)]
        public string? ProjectId { get; set; }

        [JsonPropertyName("decisions")]
        [JsonPropertyOrder(2)]
        public List<HumanIdentityDecisionDto?>? Decisions { get; set; }
    }

    /// <summary>DTO for one explicit human identity decision.</summary>
    [JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
    internal sealed class HumanIdentityDecisionDto
    {
        [JsonPropertyName("decisionId")]
        [JsonPropertyOrder(0)]
        public string? DecisionId { get; set; }

        [JsonPropertyName("projectId")]
        [JsonPropertyOrder(1)]
        public string? ProjectId { get; set; }

        [JsonPropertyName("decisionKind")]
        [JsonPropertyOrder(2)]
        public HumanIdentityDecisionKind? DecisionKind { get; set; }

        [JsonPropertyName("leftEvidence")]
        [JsonPropertyOrder(3)]
        public ClashEvidenceEndpointDto? LeftEvidence { get; set; }

        [JsonPropertyName("rightEvidence")]
        [JsonPropertyOrder(4)]
        public ClashEvidenceEndpointDto? RightEvidence { get; set; }

        [JsonPropertyName("persistentIdentityId")]
        [JsonPropertyOrder(5)]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? PersistentIdentityId { get; set; }

        [JsonPropertyName("reviewerAlias")]
        [JsonPropertyOrder(6)]
        public string? ReviewerAlias { get; set; }

        [JsonPropertyName("reason")]
        [JsonPropertyOrder(7)]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? Reason { get; set; }
    }

    /// <summary>DTO for one immutable snapshot evidence endpoint.</summary>
    [JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
    internal sealed class ClashEvidenceEndpointDto
    {
        [JsonPropertyName("runId")]
        [JsonPropertyOrder(0)]
        public string? RunId { get; set; }

        [JsonPropertyName("occurrenceIndex")]
        [JsonPropertyOrder(1)]
        public int? OccurrenceIndex { get; set; }
    }
}
