using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;

namespace OrzioClashReport.Persistence.RunIndexJson
{
    /// <summary>Serializes and parses a strict deterministic schema-version-1 ordered run-index JSON document.</summary>
    public sealed class JsonRunIndexSerializer
    {
        private const int SupportedSchemaVersion = 1;

        private static readonly JsonSerializerOptions SerializerOptions = CreateSerializerOptions();

        private static readonly UTF8Encoding Utf8WithoutBom = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false);

        private static JsonSerializerOptions CreateSerializerOptions()
        {
            return new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = false,
                WriteIndented = true,
                Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
            };
        }

        public string Serialize(RunIndexDocument index)
        {
            if (index == null)
            {
                throw new ArgumentNullException(nameof(index));
            }

            RunIndexDocumentDto dto = BuildDto(index);
            string json = JsonSerializer.Serialize(dto, SerializerOptions);
            return json.Replace("\r\n", "\n") + "\n";
        }

        public void Save(RunIndexDocument index, string filePath)
        {
            if (index == null)
            {
                throw new ArgumentNullException(nameof(index));
            }

            if (filePath == null)
            {
                throw new ArgumentNullException(nameof(filePath));
            }

            if (filePath.Trim().Length == 0)
            {
                throw new RunIndexFormatException("Run index file path cannot be empty or whitespace.");
            }

            string json = Serialize(index);
            byte[] bytes = Utf8WithoutBom.GetBytes(json);

            if (File.Exists(filePath))
            {
                throw new RunIndexFormatException($"Run index file already exists: '{filePath}'.");
            }

            FileStream stream;
            try
            {
                stream = new FileStream(filePath, FileMode.CreateNew, FileAccess.Write, FileShare.None);
            }
            catch (IOException ex) when (File.Exists(filePath))
            {
                throw new RunIndexFormatException($"Run index file already exists: '{filePath}'.", ex);
            }
            catch (IOException ex)
            {
                throw new RunIndexFormatException($"Failed to write run index to '{filePath}'.", ex);
            }
            catch (UnauthorizedAccessException ex)
            {
                throw new RunIndexFormatException($"Failed to write run index to '{filePath}'.", ex);
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
                throw new RunIndexFormatException($"Failed to write run index to '{filePath}'.", ex);
            }
            catch (UnauthorizedAccessException ex)
            {
                throw new RunIndexFormatException($"Failed to write run index to '{filePath}'.", ex);
            }
        }

        public RunIndexDocument Parse(string json)
        {
            if (json == null)
            {
                throw new ArgumentNullException(nameof(json));
            }

            if (json.Trim().Length == 0)
            {
                throw new RunIndexFormatException("Run index JSON cannot be empty or whitespace.");
            }

            RunIndexDocumentDto dto;
            try
            {
                DuplicatePropertyValidator.EnsureNoDuplicateProperties(json);
                dto = JsonSerializer.Deserialize<RunIndexDocumentDto>(json, SerializerOptions)
                    ?? throw new RunIndexFormatException("Run index JSON deserialized to an empty document.");
            }
            catch (JsonException ex)
            {
                throw new RunIndexFormatException(FormatJsonExceptionMessage(ex), ex);
            }

            return BuildDocument(dto);
        }

        public RunIndexDocument Load(string filePath)
        {
            if (filePath == null)
            {
                throw new ArgumentNullException(nameof(filePath));
            }

            if (filePath.Trim().Length == 0)
            {
                throw new RunIndexFormatException("Run index file path cannot be empty or whitespace.");
            }

            if (!File.Exists(filePath))
            {
                throw new RunIndexFormatException($"Run index file not found: '{filePath}'.");
            }

            string json = File.ReadAllText(filePath, Encoding.UTF8);
            return Parse(json);
        }

        private static RunIndexDocumentDto BuildDto(RunIndexDocument index)
        {
            if (index.SnapshotPaths.Count == 0)
            {
                throw new RunIndexFormatException("Run index must contain at least one snapshot path reference.");
            }

            var snapshotPaths = new List<string?>(index.SnapshotPaths.Count);
            for (int i = 0; i < index.SnapshotPaths.Count; i++)
            {
                string reference = index.SnapshotPaths[i];
                RunIndexPathReferenceValidator.EnsureCanonicalReference(reference, $"snapshotPaths[{i}]");
                snapshotPaths.Add(reference);
            }

            return new RunIndexDocumentDto
            {
                SchemaVersion = SupportedSchemaVersion,
                SnapshotPaths = snapshotPaths,
            };
        }

        private static RunIndexDocument BuildDocument(RunIndexDocumentDto dto)
        {
            if (dto.SchemaVersion == null)
            {
                throw new RunIndexFormatException("Field 'schemaVersion' is required.");
            }

            if (dto.SchemaVersion.Value != SupportedSchemaVersion)
            {
                throw new RunIndexFormatException(
                    $"Unsupported run index schemaVersion '{dto.SchemaVersion.Value}'. Supported version: {SupportedSchemaVersion}.");
            }

            if (dto.SnapshotPaths == null)
            {
                throw new RunIndexFormatException("Field 'snapshotPaths' is required and must be a non-empty array.");
            }

            if (dto.SnapshotPaths.Count == 0)
            {
                throw new RunIndexFormatException("Field 'snapshotPaths' must contain at least one snapshot path reference.");
            }

            var snapshotPaths = new List<string>(dto.SnapshotPaths.Count);
            for (int i = 0; i < dto.SnapshotPaths.Count; i++)
            {
                string context = $"snapshotPaths[{i}]";
                RunIndexPathReferenceValidator.EnsureCanonicalReference(dto.SnapshotPaths[i], context);
                snapshotPaths.Add(dto.SnapshotPaths[i]!);
            }

            try
            {
                return new RunIndexDocument(snapshotPaths);
            }
            catch (ArgumentException ex)
            {
                throw new RunIndexFormatException($"Run index JSON is invalid: {ex.Message}", ex);
            }
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
            return $"Run index JSON is malformed or invalid{context}: {ex.Message}";
        }
    }
}
