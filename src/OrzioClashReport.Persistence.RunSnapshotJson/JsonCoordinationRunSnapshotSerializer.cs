using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using OrzioClashReport.Core.Model;

namespace OrzioClashReport.Persistence.RunSnapshotJson
{
    /// <summary>
    /// Serializes a <see cref="CoordinationRun"/> to a deterministic schema-version-1 JSON snapshot and
    /// rehydrates one back, without inference. The snapshot is immutable evidence: it stores the
    /// <see cref="RunManifest"/> facts, the declared <see cref="ModelRevision"/> values, executed-test
    /// coverage, ordered occurrence slots, and raw clash/object evidence (including the raw
    /// <see cref="ClashStatus"/>). It never stores matching candidates, selected matches, confidence, match
    /// evidence, lifecycle entries/evidence/statuses, fingerprints, or persistent clash IDs -- those are all
    /// recalculable and must not be frozen into the evidence layer.
    ///
    /// Executed tests and occurrences reference the snapshot's models array by explicit A/B model indexes;
    /// parsing reuses the exact manifest <see cref="ModelRevision"/> / <see cref="ModelIdentity"/> instances
    /// addressed by those indexes. Model, test, occurrence, and path-hierarchy order is preserved.
    /// <see cref="ClashObject.Properties"/> is the only canonicalized collection: entries are sorted by key
    /// with <see cref="StringComparer.Ordinal"/> before serialization, because dictionary enumeration order is
    /// not part of the Core contract. The snapshot schemaVersion belongs only to this adapter and is unrelated
    /// to the run manifest schemaVersion.
    /// </summary>
    public sealed class JsonCoordinationRunSnapshotSerializer
    {
        private const int SupportedSchemaVersion = 1;

        private static readonly JsonSerializerOptions SerializerOptions = CreateSerializerOptions();

        private static readonly UTF8Encoding Utf8WithoutBom = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false);

        private static JsonSerializerOptions CreateSerializerOptions()
        {
            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = false,
                WriteIndented = true,

                // A run snapshot is a stand-alone data file, never embedded in HTML, so the relaxed encoder is
                // safe and gives a clean, readable canonical form: '+' in timestamps and characters like '<',
                // '>', '&' are written literally instead of as \uXXXX escapes. It still escapes control
                // characters, '"', and '\'. The encoder is deterministic.
                Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
            };
            options.Converters.Add(new StrictIso8601DateTimeOffsetConverter());
            options.Converters.Add(new StrictClashStatusJsonConverter());
            return options;
        }

        // -- Public contract -------------------------------------------------------------------------------

        public string Serialize(CoordinationRun run)
        {
            if (run == null)
            {
                throw new ArgumentNullException(nameof(run));
            }

            CoordinationRunSnapshotDto dto = BuildSnapshotDto(run);
            string json = JsonSerializer.Serialize(dto, SerializerOptions);

            // The persisted line ending is always '\n', regardless of the host platform or the framework's
            // indentation newline (net8's Utf8JsonWriter emits '\r\n'). Only structural indentation ever emits
            // raw CR/LF: string values escape their own CR/LF as '\r'/'\n' text, so normalizing the raw '\r\n'
            // byte pair to '\n' never touches string content. The canonical snapshot then ends with exactly one
            // trailing '\n'. This is deliberately not Environment.NewLine.
            return json.Replace("\r\n", "\n") + "\n";
        }

        public void Save(CoordinationRun run, string filePath)
        {
            if (run == null)
            {
                throw new ArgumentNullException(nameof(run));
            }

            if (filePath == null)
            {
                throw new ArgumentNullException(nameof(filePath));
            }

            if (filePath.Trim().Length == 0)
            {
                throw new CoordinationRunSnapshotFormatException(
                    "Coordination run snapshot file path cannot be empty or whitespace.");
            }

            // Serialize first: if serialization fails, no destination file is ever created.
            string json = Serialize(run);
            byte[] bytes = Utf8WithoutBom.GetBytes(json);

            // A snapshot file is immutable. Refuse to overwrite an existing path, even if the bytes would be
            // identical. The File.Exists check gives a clean message in the common case; the FileMode.CreateNew
            // open is the race-safe guarantee if the file appears between the check and the write.
            if (File.Exists(filePath))
            {
                throw new CoordinationRunSnapshotFormatException(
                    $"Coordination run snapshot file already exists: '{filePath}'.");
            }

            FileStream stream;
            try
            {
                stream = new FileStream(filePath, FileMode.CreateNew, FileAccess.Write, FileShare.None);
            }
            catch (IOException ex) when (File.Exists(filePath))
            {
                throw new CoordinationRunSnapshotFormatException(
                    $"Coordination run snapshot file already exists: '{filePath}'.", ex);
            }
            catch (IOException ex)
            {
                throw new CoordinationRunSnapshotFormatException(
                    $"Failed to write coordination run snapshot to '{filePath}'.", ex);
            }
            catch (UnauthorizedAccessException ex)
            {
                throw new CoordinationRunSnapshotFormatException(
                    $"Failed to write coordination run snapshot to '{filePath}'.", ex);
            }

            try
            {
                using (stream)
                {
                    stream.Write(bytes, 0, bytes.Length);
                }
            }
            catch (IOException ex)
            {
                throw new CoordinationRunSnapshotFormatException(
                    $"Failed to write coordination run snapshot to '{filePath}'.", ex);
            }
            catch (UnauthorizedAccessException ex)
            {
                throw new CoordinationRunSnapshotFormatException(
                    $"Failed to write coordination run snapshot to '{filePath}'.", ex);
            }
        }

        public CoordinationRun Parse(string json)
        {
            if (json == null)
            {
                throw new ArgumentNullException(nameof(json));
            }

            if (json.Trim().Length == 0)
            {
                throw new CoordinationRunSnapshotFormatException(
                    "Coordination run snapshot JSON cannot be empty or whitespace.");
            }

            CoordinationRunSnapshotDto dto;
            try
            {
                DuplicatePropertyValidator.EnsureNoDuplicateProperties(json);

                dto = JsonSerializer.Deserialize<CoordinationRunSnapshotDto>(json, SerializerOptions)
                    ?? throw new CoordinationRunSnapshotFormatException(
                        "Coordination run snapshot JSON deserialized to an empty document.");
            }
            catch (JsonException ex)
            {
                throw new CoordinationRunSnapshotFormatException(FormatJsonExceptionMessage(ex), ex);
            }

            return BuildRun(dto);
        }

        public CoordinationRun Load(string filePath)
        {
            if (filePath == null)
            {
                throw new ArgumentNullException(nameof(filePath));
            }

            if (filePath.Trim().Length == 0)
            {
                throw new CoordinationRunSnapshotFormatException(
                    "Coordination run snapshot file path cannot be empty or whitespace.");
            }

            if (!File.Exists(filePath))
            {
                throw new CoordinationRunSnapshotFormatException(
                    $"Coordination run snapshot file not found: '{filePath}'.");
            }

            string json = File.ReadAllText(filePath, Encoding.UTF8);
            return Parse(json);
        }

        // -- Serialize: CoordinationRun -> DTO -------------------------------------------------------------

        private static CoordinationRunSnapshotDto BuildSnapshotDto(CoordinationRun run)
        {
            IReadOnlyList<ModelRevision> models = run.Models;

            var modelDtos = new List<ModelRevisionSnapshotDto?>(models.Count);
            for (int i = 0; i < models.Count; i++)
            {
                modelDtos.Add(BuildModelRevisionDto(models[i]));
            }

            var testDtos = new List<ExecutedClashTestSnapshotDto?>(run.ExecutedClashTests.Count);
            for (int i = 0; i < run.ExecutedClashTests.Count; i++)
            {
                ExecutedClashTest test = run.ExecutedClashTests[i];
                testDtos.Add(new ExecutedClashTestSnapshotDto
                {
                    Name = test.Name,
                    ModelAIndex = ResolveIdentityIndex(models, test.ModelA, $"executedClashTests[{i}].modelA"),
                    ModelBIndex = ResolveIdentityIndex(models, test.ModelB, $"executedClashTests[{i}].modelB"),
                });
            }

            var occurrenceDtos = new List<ClashOccurrenceSnapshotDto?>(run.Occurrences.Count);
            for (int i = 0; i < run.Occurrences.Count; i++)
            {
                ClashOccurrence occurrence = run.Occurrences[i];
                occurrenceDtos.Add(new ClashOccurrenceSnapshotDto
                {
                    ClashTestName = occurrence.ClashTestName,
                    ModelAIndex = ResolveRevisionIndex(models, occurrence.ModelA, $"occurrences[{i}].modelA"),
                    ModelBIndex = ResolveRevisionIndex(models, occurrence.ModelB, $"occurrences[{i}].modelB"),
                    Clash = BuildClashResultDto(occurrence.Clash),
                });
            }

            return new CoordinationRunSnapshotDto
            {
                SchemaVersion = SupportedSchemaVersion,
                RunId = run.RunId,
                CreatedAt = run.CreatedAt,
                Models = modelDtos,
                ExecutedClashTests = testDtos,
                Occurrences = occurrenceDtos,
            };
        }

        private static ModelRevisionSnapshotDto BuildModelRevisionDto(ModelRevision model) => new ModelRevisionSnapshotDto
        {
            Company = model.Identity.Company,
            Discipline = model.Identity.Discipline,
            ModelName = model.Identity.ModelName,
            Revision = model.Revision,
            SourceFileName = model.SourceFileName,
            SourceFilePath = model.SourceFilePath,
            ContentHash = model.ContentHash,
            PublishedAt = model.PublishedAt,
        };

        private static ClashResultSnapshotDto BuildClashResultDto(ClashResult clash) => new ClashResultSnapshotDto
        {
            Name = clash.Name,
            Status = clash.Status,
            Distance = clash.Distance,
            GridLocation = clash.GridLocation,
            Point = BuildPointDto(clash.Point),
            ElementA = BuildClashObjectDto(clash.ElementA),
            ElementB = BuildClashObjectDto(clash.ElementB),
            Guid = clash.Guid,
        };

        private static ClashPointSnapshotDto? BuildPointDto(ClashPoint? point)
        {
            if (point == null)
            {
                return null;
            }

            ClashPoint value = point.Value;
            return new ClashPointSnapshotDto { X = value.X, Y = value.Y, Z = value.Z };
        }

        private static ClashObjectSnapshotDto BuildClashObjectDto(ClashObject element)
        {
            var pathHierarchy = new List<string?>(element.PathHierarchy.Count);
            for (int i = 0; i < element.PathHierarchy.Count; i++)
            {
                pathHierarchy.Add(element.PathHierarchy[i]);
            }

            // Properties is dictionary evidence: enumeration order is not part of the Core contract. Sort the
            // entries by key (ordinal) to get a canonical, deterministic representation.
            var keys = new List<string>(element.Properties.Keys);
            keys.Sort(StringComparer.Ordinal);

            var properties = new List<PropertyEntrySnapshotDto?>(keys.Count);
            for (int i = 0; i < keys.Count; i++)
            {
                properties.Add(new PropertyEntrySnapshotDto { Key = keys[i], Value = element.Properties[keys[i]] });
            }

            return new ClashObjectSnapshotDto
            {
                ElementId = element.ElementId,
                ElementName = element.ElementName,
                Level = element.Level,
                SourceModel = element.SourceModel,
                PathHierarchy = pathHierarchy,
                Properties = properties,
            };
        }

        /// <summary>Finds the single model whose full <see cref="ModelRevision"/> value equals <paramref name="revision"/>. Never uses reference equality or a partial field.</summary>
        private static int ResolveRevisionIndex(IReadOnlyList<ModelRevision> models, ModelRevision revision, string context)
        {
            int found = -1;
            for (int i = 0; i < models.Count; i++)
            {
                if (models[i].Equals(revision))
                {
                    if (found >= 0)
                    {
                        throw new CoordinationRunSnapshotFormatException(
                            $"Cannot serialize '{context}': model revision '{revision}' matches more than one declared model.");
                    }

                    found = i;
                }
            }

            if (found < 0)
            {
                throw new CoordinationRunSnapshotFormatException(
                    $"Cannot serialize '{context}': model revision '{revision}' is not declared in the run's models.");
            }

            return found;
        }

        /// <summary>Finds the single model whose revision-free <see cref="ModelIdentity"/> equals <paramref name="identity"/>. Never uses reference equality or a partial field.</summary>
        private static int ResolveIdentityIndex(IReadOnlyList<ModelRevision> models, ModelIdentity identity, string context)
        {
            int found = -1;
            for (int i = 0; i < models.Count; i++)
            {
                if (models[i].Identity.Equals(identity))
                {
                    if (found >= 0)
                    {
                        throw new CoordinationRunSnapshotFormatException(
                            $"Cannot serialize '{context}': model identity '{identity}' matches more than one declared model.");
                    }

                    found = i;
                }
            }

            if (found < 0)
            {
                throw new CoordinationRunSnapshotFormatException(
                    $"Cannot serialize '{context}': model identity '{identity}' is not declared in the run's models.");
            }

            return found;
        }

        // -- Parse: DTO -> CoordinationRun -----------------------------------------------------------------

        private static CoordinationRun BuildRun(CoordinationRunSnapshotDto dto)
        {
            if (dto.SchemaVersion == null)
            {
                throw new CoordinationRunSnapshotFormatException("Field 'schemaVersion' is required.");
            }

            if (dto.SchemaVersion.Value != SupportedSchemaVersion)
            {
                throw new CoordinationRunSnapshotFormatException(
                    $"Unsupported coordination run snapshot schemaVersion '{dto.SchemaVersion.Value}'. Supported version: {SupportedSchemaVersion}.");
            }

            if (dto.RunId == null)
            {
                throw new CoordinationRunSnapshotFormatException("Field 'runId' is required.");
            }

            if (dto.CreatedAt == null)
            {
                throw new CoordinationRunSnapshotFormatException(
                    "Field 'createdAt' is required and must be an ISO 8601 timestamp with an explicit offset or Z.");
            }

            if (dto.Models == null)
            {
                throw new CoordinationRunSnapshotFormatException("Field 'models' is required and must be a non-empty array.");
            }

            if (dto.ExecutedClashTests == null)
            {
                throw new CoordinationRunSnapshotFormatException(
                    "Field 'executedClashTests' is required and must be an array (an empty array is allowed).");
            }

            if (dto.Occurrences == null)
            {
                throw new CoordinationRunSnapshotFormatException(
                    "Field 'occurrences' is required and must be an array (an empty array is allowed).");
            }

            var models = new List<ModelRevision>(dto.Models.Count);
            for (int i = 0; i < dto.Models.Count; i++)
            {
                models.Add(BuildModelRevision(dto.Models[i], i));
            }

            var executedTests = new List<ExecutedClashTest>(dto.ExecutedClashTests.Count);
            for (int i = 0; i < dto.ExecutedClashTests.Count; i++)
            {
                executedTests.Add(BuildExecutedClashTest(dto.ExecutedClashTests[i], i, models));
            }

            RunManifest manifest;
            try
            {
                manifest = new RunManifest(dto.RunId, dto.CreatedAt.Value, models, executedTests);
            }
            catch (ArgumentException ex)
            {
                throw new CoordinationRunSnapshotFormatException(
                    $"Coordination run snapshot is invalid: {FormatArgumentExceptionMessage(ex)}", ex);
            }

            var occurrences = new List<ClashOccurrence>(dto.Occurrences.Count);
            for (int i = 0; i < dto.Occurrences.Count; i++)
            {
                occurrences.Add(BuildOccurrence(dto.Occurrences[i], i, manifest.Models));
            }

            try
            {
                return new CoordinationRun(manifest, occurrences);
            }
            catch (ArgumentException ex)
            {
                throw new CoordinationRunSnapshotFormatException(
                    $"Coordination run snapshot is invalid: {FormatArgumentExceptionMessage(ex)}", ex);
            }
        }

        private static ModelRevision BuildModelRevision(ModelRevisionSnapshotDto? modelDto, int index)
        {
            if (modelDto == null)
            {
                throw new CoordinationRunSnapshotFormatException($"Model at index {index} cannot be null.");
            }

            ModelIdentity identity;
            try
            {
                identity = new ModelIdentity(modelDto.Company!, modelDto.Discipline!, modelDto.ModelName!);
            }
            catch (ArgumentException ex)
            {
                throw new CoordinationRunSnapshotFormatException(
                    $"Model at index {index} is invalid: {FormatArgumentExceptionMessage(ex)}", ex);
            }

            try
            {
                return new ModelRevision(
                    identity,
                    modelDto.Revision!,
                    modelDto.SourceFileName!,
                    modelDto.SourceFilePath,
                    modelDto.ContentHash,
                    modelDto.PublishedAt);
            }
            catch (ArgumentException ex)
            {
                throw new CoordinationRunSnapshotFormatException(
                    $"Model at index {index} is invalid: {FormatArgumentExceptionMessage(ex)}", ex);
            }
        }

        private static ExecutedClashTest BuildExecutedClashTest(
            ExecutedClashTestSnapshotDto? testDto, int index, IReadOnlyList<ModelRevision> models)
        {
            string path = $"executedClashTests[{index}]";

            if (testDto == null)
            {
                throw new CoordinationRunSnapshotFormatException($"Executed clash test at {path} cannot be null.");
            }

            int aIndex = RequireModelIndex(testDto.ModelAIndex, $"{path}.modelAIndex", models.Count);
            int bIndex = RequireModelIndex(testDto.ModelBIndex, $"{path}.modelBIndex", models.Count);

            try
            {
                // Reuse the exact ModelIdentity instances the referenced model slots already carry.
                return new ExecutedClashTest(testDto.Name!, models[aIndex].Identity, models[bIndex].Identity);
            }
            catch (ArgumentException ex)
            {
                throw new CoordinationRunSnapshotFormatException(
                    $"Executed clash test at {path} is invalid: {FormatArgumentExceptionMessage(ex)}", ex);
            }
        }

        private static ClashOccurrence BuildOccurrence(
            ClashOccurrenceSnapshotDto? occurrenceDto, int index, IReadOnlyList<ModelRevision> models)
        {
            if (occurrenceDto == null)
            {
                throw new CoordinationRunSnapshotFormatException($"Occurrence at index {index} cannot be null.");
            }

            int aIndex = RequireModelIndex(occurrenceDto.ModelAIndex, $"occurrences[{index}].modelAIndex", models.Count);
            int bIndex = RequireModelIndex(occurrenceDto.ModelBIndex, $"occurrences[{index}].modelBIndex", models.Count);

            if (occurrenceDto.Clash == null)
            {
                throw new CoordinationRunSnapshotFormatException($"Field 'occurrences[{index}].clash' is required.");
            }

            ClashResult clash = BuildClashResult(occurrenceDto.Clash, index);

            try
            {
                // Reuse the exact ModelRevision instances from the manifest's models list.
                return new ClashOccurrence(occurrenceDto.ClashTestName!, clash, models[aIndex], models[bIndex]);
            }
            catch (ArgumentException ex)
            {
                throw new CoordinationRunSnapshotFormatException(
                    $"Occurrence at index {index} is invalid: {FormatArgumentExceptionMessage(ex)}", ex);
            }
        }

        private static ClashResult BuildClashResult(ClashResultSnapshotDto clashDto, int occurrenceIndex)
        {
            if (clashDto.Status == null)
            {
                throw new CoordinationRunSnapshotFormatException(
                    $"Field 'occurrences[{occurrenceIndex}].clash.status' is required.");
            }

            if (clashDto.ElementA == null)
            {
                throw new CoordinationRunSnapshotFormatException(
                    $"Field 'occurrences[{occurrenceIndex}].clash.elementA' is required.");
            }

            if (clashDto.ElementB == null)
            {
                throw new CoordinationRunSnapshotFormatException(
                    $"Field 'occurrences[{occurrenceIndex}].clash.elementB' is required.");
            }

            ClashObject elementA = BuildClashObject(clashDto.ElementA, occurrenceIndex, "elementA");
            ClashObject elementB = BuildClashObject(clashDto.ElementB, occurrenceIndex, "elementB");
            ClashPoint? point = BuildPoint(clashDto.Point, occurrenceIndex);

            try
            {
                return new ClashResult(
                    clashDto.Name,
                    clashDto.Status.Value,
                    clashDto.Distance,
                    clashDto.GridLocation,
                    point,
                    elementA,
                    elementB,
                    clashDto.Guid);
            }
            catch (ArgumentException ex)
            {
                throw new CoordinationRunSnapshotFormatException(
                    $"Occurrence at index {occurrenceIndex} clash is invalid: {FormatArgumentExceptionMessage(ex)}", ex);
            }
        }

        private static ClashPoint? BuildPoint(ClashPointSnapshotDto? pointDto, int occurrenceIndex)
        {
            if (pointDto == null)
            {
                return null;
            }

            if (pointDto.X == null)
            {
                throw new CoordinationRunSnapshotFormatException(
                    $"Field 'occurrences[{occurrenceIndex}].clash.point.x' is required.");
            }

            if (pointDto.Y == null)
            {
                throw new CoordinationRunSnapshotFormatException(
                    $"Field 'occurrences[{occurrenceIndex}].clash.point.y' is required.");
            }

            if (pointDto.Z == null)
            {
                throw new CoordinationRunSnapshotFormatException(
                    $"Field 'occurrences[{occurrenceIndex}].clash.point.z' is required.");
            }

            return new ClashPoint(pointDto.X.Value, pointDto.Y.Value, pointDto.Z.Value);
        }

        private static ClashObject BuildClashObject(ClashObjectSnapshotDto objectDto, int occurrenceIndex, string side)
        {
            string path = $"occurrences[{occurrenceIndex}].clash.{side}";

            if (objectDto.ElementId == null)
            {
                throw new CoordinationRunSnapshotFormatException($"Field '{path}.elementId' is required.");
            }

            if (objectDto.PathHierarchy == null)
            {
                throw new CoordinationRunSnapshotFormatException(
                    $"Field '{path}.pathHierarchy' is required (an empty array is allowed).");
            }

            if (objectDto.Properties == null)
            {
                throw new CoordinationRunSnapshotFormatException(
                    $"Field '{path}.properties' is required (an empty array is allowed).");
            }

            var pathHierarchy = new List<string>(objectDto.PathHierarchy.Count);
            for (int i = 0; i < objectDto.PathHierarchy.Count; i++)
            {
                string? segment = objectDto.PathHierarchy[i];
                if (segment == null)
                {
                    throw new CoordinationRunSnapshotFormatException($"Field '{path}.pathHierarchy[{i}]' cannot be null.");
                }

                pathHierarchy.Add(segment);
            }

            IReadOnlyDictionary<string, string> properties = BuildProperties(objectDto.Properties, occurrenceIndex, side);

            try
            {
                return new ClashObject(
                    objectDto.ElementId,
                    objectDto.ElementName,
                    objectDto.Level,
                    objectDto.SourceModel,
                    pathHierarchy,
                    properties);
            }
            catch (ArgumentException ex)
            {
                throw new CoordinationRunSnapshotFormatException(
                    $"Field '{path}' is invalid: {FormatArgumentExceptionMessage(ex)}", ex);
            }
        }

        private static IReadOnlyDictionary<string, string> BuildProperties(
            List<PropertyEntrySnapshotDto?> entries, int occurrenceIndex, string side)
        {
            var result = new Dictionary<string, string>(StringComparer.Ordinal);

            for (int i = 0; i < entries.Count; i++)
            {
                PropertyEntrySnapshotDto? entry = entries[i];
                string path = $"occurrences[{occurrenceIndex}].clash.{side}.properties[{i}]";

                if (entry == null)
                {
                    throw new CoordinationRunSnapshotFormatException($"Property entry at '{path}' cannot be null.");
                }

                if (entry.Key == null)
                {
                    throw new CoordinationRunSnapshotFormatException($"Field '{path}.key' is required.");
                }

                if (entry.Value == null)
                {
                    throw new CoordinationRunSnapshotFormatException($"Field '{path}.value' is required.");
                }

                if (result.ContainsKey(entry.Key))
                {
                    throw new CoordinationRunSnapshotFormatException(
                        $"Occurrence at index {occurrenceIndex} {side} contains duplicate property key '{entry.Key}'.");
                }

                result.Add(entry.Key, entry.Value);
            }

            return result;
        }

        private static int RequireModelIndex(int? index, string path, int modelCount)
        {
            if (index == null)
            {
                throw new CoordinationRunSnapshotFormatException($"Field '{path}' is required.");
            }

            if (index.Value < 0)
            {
                throw new CoordinationRunSnapshotFormatException($"Field '{path}' cannot be negative.");
            }

            if (index.Value >= modelCount)
            {
                throw new CoordinationRunSnapshotFormatException(
                    $"Field '{path}' references model index {index.Value}, but the snapshot declares {modelCount} models.");
            }

            return index.Value;
        }

        // -- Error message helpers -------------------------------------------------------------------------

        /// <summary>Turns a Core constructor's ArgumentException into a message naming the offending field, without duplicating the Core's own validation rules.</summary>
        private static string FormatArgumentExceptionMessage(ArgumentException ex)
        {
            if (ex is ArgumentNullException && ex.ParamName != null)
            {
                return $"Field '{ex.ParamName}' is required.";
            }

            if (ex.ParamName != null && ex.Message.StartsWith("Value cannot be empty or whitespace.", StringComparison.Ordinal))
            {
                return $"Field '{ex.ParamName}' cannot be empty or whitespace.";
            }

            return ex.Message;
        }

        private static string FormatJsonExceptionMessage(JsonException ex)
        {
            var location = new List<string>();
            if (ex.LineNumber.HasValue)
            {
                location.Add($"line {ex.LineNumber.Value + 1}");
            }

            if (ex.BytePositionInLine.HasValue)
            {
                location.Add($"position {ex.BytePositionInLine.Value}");
            }

            if (!string.IsNullOrEmpty(ex.Path))
            {
                location.Add($"path '{ex.Path}'");
            }

            string context = location.Count > 0 ? $" ({string.Join(", ", location)})" : string.Empty;
            return $"Coordination run snapshot JSON is malformed or invalid{context}: {ex.Message}";
        }
    }
}
