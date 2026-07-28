using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using OrzioClashReport.Core.Model;

namespace OrzioClashReport.Persistence.IdentityGovernanceJson
{
    /// <summary>Serializes and parses a strict deterministic schema-version-1 identity-governance JSON document.</summary>
    public sealed class JsonIdentityGovernanceSerializer
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
                Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
            };
            options.Converters.Add(new StrictHumanIdentityDecisionKindJsonConverter());
            return options;
        }

        public string Serialize(IdentityGovernanceDocument document)
        {
            if (document == null)
            {
                throw new ArgumentNullException(nameof(document));
            }

            IdentityGovernanceDocumentDto dto = BuildDto(document);
            string json = JsonSerializer.Serialize(dto, SerializerOptions);
            return json.Replace("\r\n", "\n") + "\n";
        }

        public void Save(IdentityGovernanceDocument document, string filePath)
        {
            if (document == null)
            {
                throw new ArgumentNullException(nameof(document));
            }

            if (filePath == null)
            {
                throw new ArgumentNullException(nameof(filePath));
            }

            if (filePath.Trim().Length == 0)
            {
                throw new IdentityGovernanceFormatException(
                    "Identity governance file path cannot be empty or whitespace.");
            }

            string json = Serialize(document);
            byte[] bytes = Utf8WithoutBom.GetBytes(json);

            if (File.Exists(filePath))
            {
                throw new IdentityGovernanceFormatException(
                    $"Identity governance file already exists: '{filePath}'.");
            }

            FileStream stream;
            try
            {
                stream = new FileStream(filePath, FileMode.CreateNew, FileAccess.Write, FileShare.None);
            }
            catch (IOException ex) when (File.Exists(filePath))
            {
                throw new IdentityGovernanceFormatException(
                    $"Identity governance file already exists: '{filePath}'.",
                    ex);
            }
            catch (IOException ex)
            {
                throw new IdentityGovernanceFormatException(
                    $"Failed to write identity governance document to '{filePath}'.",
                    ex);
            }
            catch (UnauthorizedAccessException ex)
            {
                throw new IdentityGovernanceFormatException(
                    $"Failed to write identity governance document to '{filePath}'.",
                    ex);
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
                throw new IdentityGovernanceFormatException(
                    $"Failed to write identity governance document to '{filePath}'.",
                    ex);
            }
            catch (UnauthorizedAccessException ex)
            {
                throw new IdentityGovernanceFormatException(
                    $"Failed to write identity governance document to '{filePath}'.",
                    ex);
            }
        }

        public IdentityGovernanceDocument Parse(string json)
        {
            if (json == null)
            {
                throw new ArgumentNullException(nameof(json));
            }

            if (json.Trim().Length == 0)
            {
                throw new IdentityGovernanceFormatException(
                    "Identity governance JSON cannot be empty or whitespace.");
            }

            IdentityGovernanceDocumentDto dto;
            try
            {
                DuplicatePropertyValidator.EnsureNoDuplicateProperties(json);
                dto = JsonSerializer.Deserialize<IdentityGovernanceDocumentDto>(json, SerializerOptions)
                    ?? throw new IdentityGovernanceFormatException(
                        "Identity governance JSON deserialized to an empty document.");
            }
            catch (JsonException ex)
            {
                throw new IdentityGovernanceFormatException(FormatJsonExceptionMessage(ex), ex);
            }

            return BuildDocument(dto);
        }

        public IdentityGovernanceDocument Load(string filePath)
        {
            if (filePath == null)
            {
                throw new ArgumentNullException(nameof(filePath));
            }

            if (filePath.Trim().Length == 0)
            {
                throw new IdentityGovernanceFormatException(
                    "Identity governance file path cannot be empty or whitespace.");
            }

            if (!File.Exists(filePath))
            {
                throw new IdentityGovernanceFormatException(
                    $"Identity governance file not found: '{filePath}'.");
            }

            string json = File.ReadAllText(filePath, Encoding.UTF8);
            return Parse(json);
        }

        private static IdentityGovernanceDocumentDto BuildDto(IdentityGovernanceDocument document)
        {
            var decisions = new List<HumanIdentityDecisionDto?>(document.Decisions.Count);
            for (int i = 0; i < document.Decisions.Count; i++)
            {
                HumanIdentityDecision decision = document.Decisions[i];
                decisions.Add(new HumanIdentityDecisionDto
                {
                    DecisionId = decision.DecisionId,
                    ProjectId = decision.ProjectId,
                    DecisionKind = decision.DecisionKind,
                    LeftEvidence = BuildEvidenceDto(decision.LeftEvidence),
                    RightEvidence = BuildEvidenceDto(decision.RightEvidence),
                    PersistentIdentityId = decision.PersistentIdentityId,
                    ReviewerAlias = decision.ReviewerAlias,
                    Reason = decision.Reason,
                });
            }

            return new IdentityGovernanceDocumentDto
            {
                SchemaVersion = SupportedSchemaVersion,
                ProjectId = document.ProjectId,
                Decisions = decisions,
            };
        }

        private static ClashEvidenceEndpointDto BuildEvidenceDto(ClashEvidenceEndpoint endpoint) => new ClashEvidenceEndpointDto
        {
            RunId = endpoint.RunId,
            OccurrenceIndex = endpoint.OccurrenceIndex,
        };

        private static IdentityGovernanceDocument BuildDocument(IdentityGovernanceDocumentDto dto)
        {
            if (dto.SchemaVersion == null)
            {
                throw new IdentityGovernanceFormatException("Field 'schemaVersion' is required.");
            }

            if (dto.SchemaVersion.Value != SupportedSchemaVersion)
            {
                throw new IdentityGovernanceFormatException(
                    $"Unsupported identity governance schemaVersion '{dto.SchemaVersion.Value}'. Supported version: {SupportedSchemaVersion}.");
            }

            if (dto.ProjectId == null)
            {
                throw new IdentityGovernanceFormatException("Field 'projectId' is required.");
            }

            if (dto.Decisions == null)
            {
                throw new IdentityGovernanceFormatException(
                    "Field 'decisions' is required and must be an array (an empty array is allowed).");
            }

            var decisions = new List<HumanIdentityDecision>(dto.Decisions.Count);
            for (int i = 0; i < dto.Decisions.Count; i++)
            {
                decisions.Add(BuildDecision(dto.Decisions[i], i));
            }

            try
            {
                return new IdentityGovernanceDocument(dto.ProjectId, decisions);
            }
            catch (ArgumentException ex)
            {
                throw new IdentityGovernanceFormatException(
                    $"Identity governance document is invalid: {FormatArgumentExceptionMessage(ex)}",
                    ex);
            }
        }

        private static HumanIdentityDecision BuildDecision(HumanIdentityDecisionDto? dto, int index)
        {
            string path = $"decisions[{index}]";

            if (dto == null)
            {
                throw new IdentityGovernanceFormatException($"Decision at {path} cannot be null.");
            }

            if (dto.DecisionKind == null)
            {
                throw new IdentityGovernanceFormatException($"Field '{path}.decisionKind' is required.");
            }

            ClashEvidenceEndpoint leftEvidence = BuildEvidence(dto.LeftEvidence, $"{path}.leftEvidence");
            ClashEvidenceEndpoint rightEvidence = BuildEvidence(dto.RightEvidence, $"{path}.rightEvidence");

            try
            {
                return new HumanIdentityDecision(
                    dto.DecisionId!,
                    dto.ProjectId!,
                    dto.DecisionKind.Value,
                    leftEvidence,
                    rightEvidence,
                    dto.PersistentIdentityId,
                    dto.ReviewerAlias!,
                    dto.Reason);
            }
            catch (ArgumentException ex)
            {
                throw new IdentityGovernanceFormatException(
                    $"Decision at {path} is invalid: {FormatArgumentExceptionMessage(ex)}",
                    ex);
            }
        }

        private static ClashEvidenceEndpoint BuildEvidence(ClashEvidenceEndpointDto? dto, string path)
        {
            if (dto == null)
            {
                throw new IdentityGovernanceFormatException($"Field '{path}' is required.");
            }

            if (dto.RunId == null)
            {
                throw new IdentityGovernanceFormatException($"Field '{path}.runId' is required.");
            }

            if (dto.OccurrenceIndex == null)
            {
                throw new IdentityGovernanceFormatException($"Field '{path}.occurrenceIndex' is required.");
            }

            try
            {
                return new ClashEvidenceEndpoint(dto.RunId, dto.OccurrenceIndex.Value);
            }
            catch (ArgumentException ex)
            {
                throw new IdentityGovernanceFormatException(
                    $"Field '{path}' is invalid: {FormatArgumentExceptionMessage(ex)}",
                    ex);
            }
        }

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
            return $"Identity governance JSON is malformed or invalid{context}: {ex.Message}";
        }
    }
}
