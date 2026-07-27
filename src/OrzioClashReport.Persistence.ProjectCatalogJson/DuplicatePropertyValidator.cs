using System.Collections.Generic;
using System.Text.Json;

namespace OrzioClashReport.Persistence.ProjectCatalogJson
{
    /// <summary>Validates that no JSON object in the project-catalog document repeats a property name at any depth.</summary>
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
            var seenNames = new HashSet<string>(System.StringComparer.Ordinal);

            foreach (JsonProperty property in objectElement.EnumerateObject())
            {
                if (!seenNames.Add(property.Name))
                {
                    throw new ProjectCatalogFormatException($"Duplicate JSON property '{property.Name}' at {path}.");
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
