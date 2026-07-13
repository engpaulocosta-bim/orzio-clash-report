using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.Json;
using OrzioClashReport.Core.Model;

namespace OrzioClashReport.Input.RunManifestJson
{
    /// <summary>
    /// Parses a run manifest JSON document (contract: schemaVersion, runId, createdAt, models,
    /// executedClashTests) into a validated <see cref="RunManifest"/>. JSON property names are matched
    /// case-sensitively and deliberately: the contract is exact camelCase, with no aliases, no legacy names,
    /// and no typo tolerance. Unmapped properties are rejected at the root, each model entry, and each
    /// executed clash test entry (including its nested "modelA"/"modelB"), and a repeated property name
    /// within the same JSON object is rejected rather than left to the deserializer's last-value-wins
    /// behavior. Only schemaVersion 2 is accepted: schemaVersion 1 did not declare executed clash test
    /// coverage, so it is rejected rather than silently migrated or treated as declaring zero tests.
    /// </summary>
    public sealed class JsonRunManifestSource
    {
        private const int SupportedSchemaVersion = 2;

        private static readonly JsonSerializerOptions SerializerOptions = CreateSerializerOptions();

        private static JsonSerializerOptions CreateSerializerOptions()
        {
            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = false,
            };
            options.Converters.Add(new StrictIso8601DateTimeOffsetConverter());
            return options;
        }

        public RunManifest Parse(string json)
        {
            if (json == null)
            {
                throw new ArgumentNullException(nameof(json));
            }

            if (json.Trim().Length == 0)
            {
                throw new RunManifestFormatException("Run manifest JSON cannot be empty or whitespace.");
            }

            RunManifestDto dto;
            try
            {
                DuplicatePropertyValidator.EnsureNoDuplicateProperties(json);

                dto = JsonSerializer.Deserialize<RunManifestDto>(json, SerializerOptions)
                    ?? throw new RunManifestFormatException("Run manifest JSON deserialized to an empty document.");
            }
            catch (JsonException ex)
            {
                throw new RunManifestFormatException(FormatJsonExceptionMessage(ex), ex);
            }

            return BuildManifest(dto);
        }

        public RunManifest Load(string filePath)
        {
            if (filePath == null)
            {
                throw new ArgumentNullException(nameof(filePath));
            }

            if (filePath.Trim().Length == 0)
            {
                throw new RunManifestFormatException("Run manifest file path cannot be empty or whitespace.");
            }

            if (!File.Exists(filePath))
            {
                throw new RunManifestFormatException($"Run manifest file not found: '{filePath}'.");
            }

            string json = File.ReadAllText(filePath, Encoding.UTF8);
            return Parse(json);
        }

        private static RunManifest BuildManifest(RunManifestDto dto)
        {
            if (dto.SchemaVersion == null)
            {
                throw new RunManifestFormatException("Field 'schemaVersion' is required.");
            }

            if (dto.SchemaVersion.Value != SupportedSchemaVersion)
            {
                throw new RunManifestFormatException(
                    $"Unsupported run manifest schemaVersion '{dto.SchemaVersion.Value}'. Supported version: {SupportedSchemaVersion}.");
            }

            if (dto.CreatedAt == null)
            {
                throw new RunManifestFormatException(
                    "Field 'createdAt' is required and must be an ISO 8601 timestamp with an explicit offset or Z.");
            }

            if (dto.Models == null)
            {
                throw new RunManifestFormatException("Field 'models' is required and must be a non-empty array.");
            }

            if (dto.ExecutedClashTests == null)
            {
                throw new RunManifestFormatException("Field 'executedClashTests' is required and must be an array (an empty array is allowed).");
            }

            var modelRevisions = new List<ModelRevision>(dto.Models.Count);
            for (int i = 0; i < dto.Models.Count; i++)
            {
                modelRevisions.Add(BuildModelRevision(dto.Models[i], i));
            }

            var executedClashTests = new List<ExecutedClashTest>(dto.ExecutedClashTests.Count);
            for (int i = 0; i < dto.ExecutedClashTests.Count; i++)
            {
                executedClashTests.Add(BuildExecutedClashTest(dto.ExecutedClashTests[i], i));
            }

            try
            {
                return new RunManifest(dto.RunId!, dto.CreatedAt.Value, modelRevisions, executedClashTests);
            }
            catch (ArgumentException ex)
            {
                throw new RunManifestFormatException(FormatArgumentExceptionMessage(ex), ex);
            }
        }

        private static ModelRevision BuildModelRevision(ModelRevisionDto? modelDto, int index)
        {
            if (modelDto == null)
            {
                throw new RunManifestFormatException($"Model at index {index} cannot be null.");
            }

            ModelIdentity identity;
            try
            {
                identity = new ModelIdentity(modelDto.Company!, modelDto.Discipline!, modelDto.ModelName!);
            }
            catch (ArgumentException ex)
            {
                throw new RunManifestFormatException(
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
                throw new RunManifestFormatException(
                    $"Model at index {index} is invalid: {FormatArgumentExceptionMessage(ex)}", ex);
            }
        }

        private static ExecutedClashTest BuildExecutedClashTest(ExecutedClashTestDto? testDto, int index)
        {
            string path = $"executedClashTests[{index}]";

            if (testDto == null)
            {
                throw new RunManifestFormatException($"Executed clash test at {path} cannot be null.");
            }

            var modelA = BuildModelIdentity(testDto.ModelA, $"{path}.modelA");
            var modelB = BuildModelIdentity(testDto.ModelB, $"{path}.modelB");

            try
            {
                return new ExecutedClashTest(testDto.Name!, modelA, modelB);
            }
            catch (ArgumentException ex)
            {
                throw new RunManifestFormatException(
                    $"Executed clash test at {path} is invalid: {FormatArgumentExceptionMessage(ex)}", ex);
            }
        }

        private static ModelIdentity BuildModelIdentity(ModelIdentityDto? identityDto, string path)
        {
            if (identityDto == null)
            {
                throw new RunManifestFormatException($"Field '{path}' is required.");
            }

            try
            {
                return new ModelIdentity(identityDto.Company!, identityDto.Discipline!, identityDto.ModelName!);
            }
            catch (ArgumentException ex)
            {
                throw new RunManifestFormatException(
                    $"Field '{path}' is invalid: {FormatArgumentExceptionMessage(ex)}", ex);
            }
        }

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
            return $"Run manifest JSON is malformed or invalid{context}: {ex.Message}";
        }
    }
}
