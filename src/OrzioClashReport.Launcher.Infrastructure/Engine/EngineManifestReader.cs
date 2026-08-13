using System;
using System.IO;
using System.Text.Json;

namespace OrzioClashReport.Launcher.Infrastructure.Engine
{
    /// <summary>
    /// Reads <c>engine-manifest.json</c>. A missing, unreadable, or malformed manifest yields
    /// <c>null</c> plus a reason: it degrades the engine status to something the user can act on,
    /// rather than throwing during startup.
    /// </summary>
    public sealed class EngineManifestReader
    {
        public EngineManifest? TryRead(string manifestPath, out string failureReason)
        {
            failureReason = string.Empty;

            if (string.IsNullOrWhiteSpace(manifestPath) || !File.Exists(manifestPath))
            {
                failureReason = "O manifesto do motor não existe na instalação.";
                return null;
            }

            string json;
            try
            {
                json = File.ReadAllText(manifestPath);
            }
            catch (Exception exception) when (
                exception is IOException
                || exception is UnauthorizedAccessException
                || exception is System.Security.SecurityException)
            {
                failureReason = "O manifesto do motor não pôde ser lido.";
                return null;
            }

            try
            {
                using (JsonDocument document = JsonDocument.Parse(json))
                {
                    JsonElement root = document.RootElement;
                    if (root.ValueKind != JsonValueKind.Object)
                    {
                        failureReason = "O manifesto do motor não é um objeto JSON.";
                        return null;
                    }

                    int schemaVersion = ReadInt32(root, "schemaVersion");
                    string engineVersion = ReadString(root, "engineVersion");
                    string fileName = ReadString(root, "fileName");
                    string sha256 = ReadString(root, "sha256");

                    return new EngineManifest(schemaVersion, engineVersion, fileName, sha256);
                }
            }
            catch (Exception exception) when (
                exception is JsonException
                || exception is ArgumentException
                || exception is ArgumentOutOfRangeException)
            {
                failureReason = "O manifesto do motor tem um formato inesperado.";
                return null;
            }
        }

        private static int ReadInt32(JsonElement root, string propertyName)
        {
            if (!root.TryGetProperty(propertyName, out JsonElement value)
                || value.ValueKind != JsonValueKind.Number
                || !value.TryGetInt32(out int result))
            {
                throw new JsonException($"Manifest property '{propertyName}' is missing or not an integer.");
            }

            return result;
        }

        private static string ReadString(JsonElement root, string propertyName)
        {
            if (!root.TryGetProperty(propertyName, out JsonElement value)
                || value.ValueKind != JsonValueKind.String)
            {
                throw new JsonException($"Manifest property '{propertyName}' is missing or not a string.");
            }

            return value.GetString() ?? string.Empty;
        }
    }
}
