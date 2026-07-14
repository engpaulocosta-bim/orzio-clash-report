using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;
using OrzioClashReport.Core.Model;

namespace OrzioClashReport.Persistence.RunSnapshotJson
{
    // Raw serialization shapes for the coordination run snapshot schema (version 1). All types are internal to
    // this adapter and never exposed to the Core, the CLI, or any other adapter. Property names are exact,
    // case-sensitive camelCase; JsonPropertyOrder pins the canonical property order so serialization is
    // deterministic; JsonUnmappedMemberHandling.Disallow rejects unknown/misspelled fields loudly instead of
    // silently ignoring them. Nullable properties let Parse distinguish "missing required field" from a valid
    // default value (for example, modelAIndex == 0 versus modelAIndex absent).

    /// <summary>Root shape of a coordination run snapshot document.</summary>
    [JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
    internal sealed class CoordinationRunSnapshotDto
    {
        [JsonPropertyName("schemaVersion")]
        [JsonPropertyOrder(0)]
        public int? SchemaVersion { get; set; }

        [JsonPropertyName("runId")]
        [JsonPropertyOrder(1)]
        public string? RunId { get; set; }

        [JsonPropertyName("createdAt")]
        [JsonPropertyOrder(2)]
        public DateTimeOffset? CreatedAt { get; set; }

        [JsonPropertyName("models")]
        [JsonPropertyOrder(3)]
        public List<ModelRevisionSnapshotDto?>? Models { get; set; }

        [JsonPropertyName("executedClashTests")]
        [JsonPropertyOrder(4)]
        public List<ExecutedClashTestSnapshotDto?>? ExecutedClashTests { get; set; }

        [JsonPropertyName("occurrences")]
        [JsonPropertyOrder(5)]
        public List<ClashOccurrenceSnapshotDto?>? Occurrences { get; set; }
    }

    /// <summary>One entry of the snapshot's "models" array: a full <see cref="ModelRevision"/> value.</summary>
    [JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
    internal sealed class ModelRevisionSnapshotDto
    {
        [JsonPropertyName("company")]
        [JsonPropertyOrder(0)]
        public string? Company { get; set; }

        [JsonPropertyName("discipline")]
        [JsonPropertyOrder(1)]
        public string? Discipline { get; set; }

        [JsonPropertyName("modelName")]
        [JsonPropertyOrder(2)]
        public string? ModelName { get; set; }

        [JsonPropertyName("revision")]
        [JsonPropertyOrder(3)]
        public string? Revision { get; set; }

        [JsonPropertyName("sourceFileName")]
        [JsonPropertyOrder(4)]
        public string? SourceFileName { get; set; }

        [JsonPropertyName("sourceFilePath")]
        [JsonPropertyOrder(5)]
        public string? SourceFilePath { get; set; }

        [JsonPropertyName("contentHash")]
        [JsonPropertyOrder(6)]
        public string? ContentHash { get; set; }

        [JsonPropertyName("publishedAt")]
        [JsonPropertyOrder(7)]
        public DateTimeOffset? PublishedAt { get; set; }
    }

    /// <summary>One entry of "executedClashTests": a test name and two indexes into the models array.</summary>
    [JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
    internal sealed class ExecutedClashTestSnapshotDto
    {
        [JsonPropertyName("name")]
        [JsonPropertyOrder(0)]
        public string? Name { get; set; }

        [JsonPropertyName("modelAIndex")]
        [JsonPropertyOrder(1)]
        public int? ModelAIndex { get; set; }

        [JsonPropertyName("modelBIndex")]
        [JsonPropertyOrder(2)]
        public int? ModelBIndex { get; set; }
    }

    /// <summary>One ordered occurrence slot: a clash test name, two model indexes, and the raw clash.</summary>
    [JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
    internal sealed class ClashOccurrenceSnapshotDto
    {
        [JsonPropertyName("clashTestName")]
        [JsonPropertyOrder(0)]
        public string? ClashTestName { get; set; }

        [JsonPropertyName("modelAIndex")]
        [JsonPropertyOrder(1)]
        public int? ModelAIndex { get; set; }

        [JsonPropertyName("modelBIndex")]
        [JsonPropertyOrder(2)]
        public int? ModelBIndex { get; set; }

        [JsonPropertyName("clash")]
        [JsonPropertyOrder(3)]
        public ClashResultSnapshotDto? Clash { get; set; }
    }

    /// <summary>Raw clash evidence. <see cref="Status"/> is the raw source status, never a lifecycle status.</summary>
    [JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
    internal sealed class ClashResultSnapshotDto
    {
        [JsonPropertyName("name")]
        [JsonPropertyOrder(0)]
        public string? Name { get; set; }

        [JsonPropertyName("status")]
        [JsonPropertyOrder(1)]
        public ClashStatus? Status { get; set; }

        [JsonPropertyName("distance")]
        [JsonPropertyOrder(2)]
        public double? Distance { get; set; }

        [JsonPropertyName("gridLocation")]
        [JsonPropertyOrder(3)]
        public string? GridLocation { get; set; }

        [JsonPropertyName("point")]
        [JsonPropertyOrder(4)]
        public ClashPointSnapshotDto? Point { get; set; }

        [JsonPropertyName("elementA")]
        [JsonPropertyOrder(5)]
        public ClashObjectSnapshotDto? ElementA { get; set; }

        [JsonPropertyName("elementB")]
        [JsonPropertyOrder(6)]
        public ClashObjectSnapshotDto? ElementB { get; set; }

        [JsonPropertyName("guid")]
        [JsonPropertyOrder(7)]
        public string? Guid { get; set; }
    }

    /// <summary>A 3D clash point. Coordinates are stored verbatim: never rounded, scaled, or unit-converted.</summary>
    [JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
    internal sealed class ClashPointSnapshotDto
    {
        [JsonPropertyName("x")]
        [JsonPropertyOrder(0)]
        public double? X { get; set; }

        [JsonPropertyName("y")]
        [JsonPropertyOrder(1)]
        public double? Y { get; set; }

        [JsonPropertyName("z")]
        [JsonPropertyOrder(2)]
        public double? Z { get; set; }
    }

    /// <summary>One of the two clash elements. <see cref="Properties"/> is a canonicalized key/value array.</summary>
    [JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
    internal sealed class ClashObjectSnapshotDto
    {
        [JsonPropertyName("elementId")]
        [JsonPropertyOrder(0)]
        public string? ElementId { get; set; }

        [JsonPropertyName("elementName")]
        [JsonPropertyOrder(1)]
        public string? ElementName { get; set; }

        [JsonPropertyName("level")]
        [JsonPropertyOrder(2)]
        public string? Level { get; set; }

        [JsonPropertyName("sourceModel")]
        [JsonPropertyOrder(3)]
        public string? SourceModel { get; set; }

        [JsonPropertyName("pathHierarchy")]
        [JsonPropertyOrder(4)]
        public List<string?>? PathHierarchy { get; set; }

        [JsonPropertyName("properties")]
        [JsonPropertyOrder(5)]
        public List<PropertyEntrySnapshotDto?>? Properties { get; set; }
    }

    /// <summary>One property key/value pair. Entries are serialized sorted by ordinal key for determinism.</summary>
    [JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
    internal sealed class PropertyEntrySnapshotDto
    {
        [JsonPropertyName("key")]
        [JsonPropertyOrder(0)]
        public string? Key { get; set; }

        [JsonPropertyName("value")]
        [JsonPropertyOrder(1)]
        public string? Value { get; set; }
    }
}
