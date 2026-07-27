using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;

namespace OrzioClashReport.Persistence.ProjectCatalogJson
{
    /// <summary>Serializes and parses a strict deterministic schema-version-1 operational project-catalog JSON document.</summary>
    public sealed class JsonProjectCatalogSerializer
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

        public string Serialize(ProjectCatalogDocument document)
        {
            if (document == null)
            {
                throw new ArgumentNullException(nameof(document));
            }

            ProjectCatalogDocumentDto dto = BuildDto(document);
            string json = JsonSerializer.Serialize(dto, SerializerOptions);
            return json.Replace("\r\n", "\n") + "\n";
        }

        public void Save(ProjectCatalogDocument document, string filePath)
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
                throw new ProjectCatalogFormatException("Project catalog file path cannot be empty or whitespace.");
            }

            string json = Serialize(document);
            byte[] bytes = Utf8WithoutBom.GetBytes(json);

            if (File.Exists(filePath))
            {
                throw new ProjectCatalogFormatException($"Project catalog file already exists: '{filePath}'.");
            }

            FileStream stream;
            try
            {
                stream = new FileStream(filePath, FileMode.CreateNew, FileAccess.Write, FileShare.None);
            }
            catch (IOException ex) when (File.Exists(filePath))
            {
                throw new ProjectCatalogFormatException($"Project catalog file already exists: '{filePath}'.", ex);
            }
            catch (IOException ex)
            {
                throw new ProjectCatalogFormatException($"Failed to write project catalog to '{filePath}'.", ex);
            }
            catch (UnauthorizedAccessException ex)
            {
                throw new ProjectCatalogFormatException($"Failed to write project catalog to '{filePath}'.", ex);
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
                throw new ProjectCatalogFormatException($"Failed to write project catalog to '{filePath}'.", ex);
            }
            catch (UnauthorizedAccessException ex)
            {
                throw new ProjectCatalogFormatException($"Failed to write project catalog to '{filePath}'.", ex);
            }
        }

        public ProjectCatalogDocument Parse(string json)
        {
            if (json == null)
            {
                throw new ArgumentNullException(nameof(json));
            }

            if (json.Trim().Length == 0)
            {
                throw new ProjectCatalogFormatException("Project catalog JSON cannot be empty or whitespace.");
            }

            ProjectCatalogDocumentDto dto;
            try
            {
                DuplicatePropertyValidator.EnsureNoDuplicateProperties(json);
                dto = JsonSerializer.Deserialize<ProjectCatalogDocumentDto>(json, SerializerOptions)
                    ?? throw new ProjectCatalogFormatException("Project catalog JSON deserialized to an empty document.");
            }
            catch (JsonException ex)
            {
                throw new ProjectCatalogFormatException(FormatJsonExceptionMessage(ex), ex);
            }

            return BuildDocument(dto);
        }

        public ProjectCatalogDocument Load(string filePath)
        {
            if (filePath == null)
            {
                throw new ArgumentNullException(nameof(filePath));
            }

            if (filePath.Trim().Length == 0)
            {
                throw new ProjectCatalogFormatException("Project catalog file path cannot be empty or whitespace.");
            }

            if (!File.Exists(filePath))
            {
                throw new ProjectCatalogFormatException($"Project catalog file not found: '{filePath}'.");
            }

            string json = File.ReadAllText(filePath, Encoding.UTF8);
            return Parse(json);
        }

        private static ProjectCatalogDocumentDto BuildDto(ProjectCatalogDocument document)
        {
            EnsureWellFormedUtf16(document.ProjectId, "projectId");
            EnsureWellFormedUtf16(document.DisplayName, "displayName");
            EnsureWellFormedUtf16(document.RunIndexPath, "runIndexPath");
            EnsureWellFormedUtf16(document.LongitudinalReportPath, "longitudinalReportPath");

            ProjectCatalogPathReferenceValidator.EnsureCanonicalReference(document.RunIndexPath, "runIndexPath");
            ProjectCatalogPathReferenceValidator.EnsureCanonicalReference(document.LongitudinalReportPath, "longitudinalReportPath");

            return new ProjectCatalogDocumentDto
            {
                SchemaVersion = SupportedSchemaVersion,
                ProjectId = document.ProjectId,
                DisplayName = document.DisplayName,
                RunIndexPath = document.RunIndexPath,
                LongitudinalReportPath = document.LongitudinalReportPath,
            };
        }

        private static ProjectCatalogDocument BuildDocument(ProjectCatalogDocumentDto dto)
        {
            if (dto.SchemaVersion == null)
            {
                throw new ProjectCatalogFormatException("Field 'schemaVersion' is required.");
            }

            if (dto.SchemaVersion.Value != SupportedSchemaVersion)
            {
                throw new ProjectCatalogFormatException(
                    $"Unsupported project catalog schemaVersion '{dto.SchemaVersion.Value}'. Supported version: {SupportedSchemaVersion}.");
            }

            if (dto.ProjectId == null)
            {
                throw new ProjectCatalogFormatException("Field 'projectId' is required.");
            }

            if (dto.DisplayName == null)
            {
                throw new ProjectCatalogFormatException("Field 'displayName' is required.");
            }

            if (dto.RunIndexPath == null)
            {
                throw new ProjectCatalogFormatException("Field 'runIndexPath' is required.");
            }

            if (dto.LongitudinalReportPath == null)
            {
                throw new ProjectCatalogFormatException("Field 'longitudinalReportPath' is required.");
            }

            try
            {
                return new ProjectCatalogDocument(
                    dto.ProjectId,
                    dto.DisplayName,
                    dto.RunIndexPath,
                    dto.LongitudinalReportPath);
            }
            catch (ArgumentException ex)
            {
                throw new ProjectCatalogFormatException(
                    $"Project catalog JSON is invalid: {FormatArgumentExceptionMessage(ex)}",
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

        private static void EnsureWellFormedUtf16(string value, string context)
        {
            for (int i = 0; i < value.Length; i++)
            {
                char current = value[i];
                if (char.IsHighSurrogate(current))
                {
                    if (i + 1 >= value.Length || !char.IsLowSurrogate(value[i + 1]))
                    {
                        throw new ProjectCatalogFormatException($"Field '{context}' contains invalid UTF-16 data.");
                    }

                    i++;
                    continue;
                }

                if (char.IsLowSurrogate(current))
                {
                    throw new ProjectCatalogFormatException($"Field '{context}' contains invalid UTF-16 data.");
                }
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
            return $"Project catalog JSON is malformed or invalid{context}: {ex.Message}";
        }
    }
}
