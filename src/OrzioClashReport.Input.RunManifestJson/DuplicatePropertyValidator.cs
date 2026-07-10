using System;
using System.Collections.Generic;
using System.Text.Json;

namespace OrzioClashReport.Input.RunManifestJson
{
    /// <summary>
    /// Validates that no JSON object in the manifest document repeats a property name. The manifest is meant
    /// to be an unambiguous, auditable contract, so a document must fail instead of silently depending on
    /// whatever last-value-wins behavior the underlying deserializer happens to have for duplicate keys.
    /// Property names are compared with <see cref="StringComparer.Ordinal"/>, matching the contract's
    /// case-sensitive property names.
    /// </summary>
    internal static class DuplicatePropertyValidator
    {
        private const string RootPath = "root";

        public static void EnsureNoDuplicateProperties(string json)
        {
            using var document = JsonDocument.Parse(json);
            CheckElement(document.RootElement, RootPath);
        }

        private static void CheckElement(JsonElement element, string path)
        {
            switch (element.ValueKind)
            {
                case JsonValueKind.Object:
                    CheckObject(element, path);
                    break;
                case JsonValueKind.Array:
                    CheckArray(element, path);
                    break;
            }
        }

        private static void CheckObject(JsonElement objectElement, string path)
        {
            var seenNames = new HashSet<string>(StringComparer.Ordinal);

            foreach (JsonProperty property in objectElement.EnumerateObject())
            {
                if (!seenNames.Add(property.Name))
                {
                    throw new RunManifestFormatException($"Duplicate JSON property '{property.Name}' at {path}.");
                }
            }

            foreach (JsonProperty property in objectElement.EnumerateObject())
            {
                string childPath = path == RootPath ? property.Name : $"{path}.{property.Name}";
                CheckElement(property.Value, childPath);
            }
        }

        private static void CheckArray(JsonElement arrayElement, string path)
        {
            int index = 0;
            foreach (JsonElement item in arrayElement.EnumerateArray())
            {
                CheckElement(item, $"{path}[{index}]");
                index++;
            }
        }
    }
}
