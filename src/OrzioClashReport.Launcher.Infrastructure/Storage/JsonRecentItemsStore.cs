using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using OrzioClashReport.Launcher.Contracts.Operations;
using OrzioClashReport.Launcher.Contracts.Results;
using OrzioClashReport.Launcher.Contracts.Settings;

namespace OrzioClashReport.Launcher.Infrastructure.Storage
{
    /// <summary>
    /// Stores the most recent outputs as <c>recent-items.json</c>. This is local convenience state
    /// only: it is never uploaded, never treated as evidence, and can be cleared from Definições.
    /// </summary>
    public sealed class JsonRecentItemsStore : IRecentItemsStore
    {
        public const int MaximumItems = 10;

        private const int SchemaVersion = 1;

        private readonly string _filePath;

        public JsonRecentItemsStore(string filePath)
        {
            if (string.IsNullOrWhiteSpace(filePath))
            {
                throw new ArgumentException("Recent items file path cannot be empty.", nameof(filePath));
            }

            _filePath = filePath;
        }

        public Task<IReadOnlyList<RecentOutputItem>> LoadAsync(CancellationToken cancellationToken) =>
            Task.FromResult(Load());

        public Task<IReadOnlyList<RecentOutputItem>> AddAsync(
            RecentOutputItem item, CancellationToken cancellationToken)
        {
            if (item == null)
            {
                throw new ArgumentNullException(nameof(item));
            }

            var items = new List<RecentOutputItem> { item };

            foreach (RecentOutputItem existing in Load())
            {
                if (string.Equals(existing.Path, item.Path, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                items.Add(existing);

                if (items.Count == MaximumItems)
                {
                    break;
                }
            }

            Save(items);
            return Task.FromResult<IReadOnlyList<RecentOutputItem>>(items);
        }

        public Task ClearAsync(CancellationToken cancellationToken)
        {
            Save(new List<RecentOutputItem>());
            return Task.CompletedTask;
        }

        private IReadOnlyList<RecentOutputItem> Load()
        {
            if (!File.Exists(_filePath))
            {
                return Array.Empty<RecentOutputItem>();
            }

            try
            {
                using (JsonDocument document = JsonDocument.Parse(File.ReadAllText(_filePath)))
                {
                    JsonElement root = document.RootElement;
                    if (root.ValueKind != JsonValueKind.Object
                        || !root.TryGetProperty("items", out JsonElement items)
                        || items.ValueKind != JsonValueKind.Array)
                    {
                        return Array.Empty<RecentOutputItem>();
                    }

                    var result = new List<RecentOutputItem>();
                    foreach (JsonElement element in items.EnumerateArray())
                    {
                        RecentOutputItem? item = TryReadItem(element);
                        if (item != null)
                        {
                            result.Add(item);
                        }

                        if (result.Count == MaximumItems)
                        {
                            break;
                        }
                    }

                    return result;
                }
            }
            catch (Exception exception) when (
                exception is JsonException
                || exception is IOException
                || exception is UnauthorizedAccessException
                || exception is System.Security.SecurityException)
            {
                // A corrupt convenience list is discarded rather than surfaced as a failure; nothing
                // the user depends on is stored here.
                return Array.Empty<RecentOutputItem>();
            }
        }

        private static RecentOutputItem? TryReadItem(JsonElement element)
        {
            if (element.ValueKind != JsonValueKind.Object)
            {
                return null;
            }

            if (!TryReadString(element, "path", out string path)
                || !TryReadString(element, "displayName", out string displayName)
                || !TryReadString(element, "operation", out string operationText)
                || !TryReadString(element, "artifactKind", out string artifactText)
                || !TryReadString(element, "completedAtUtc", out string completedText))
            {
                return null;
            }

            if (!Enum.TryParse(operationText, ignoreCase: false, out LauncherOperationKind operation)
                || !Enum.TryParse(artifactText, ignoreCase: false, out LauncherArtifactKind artifactKind)
                || !DateTimeOffset.TryParse(
                    completedText,
                    CultureInfo.InvariantCulture,
                    DateTimeStyles.RoundtripKind,
                    out DateTimeOffset completedAtUtc))
            {
                return null;
            }

            return new RecentOutputItem(path, displayName, operation, artifactKind, completedAtUtc);
        }

        private static bool TryReadString(JsonElement element, string propertyName, out string value)
        {
            value = string.Empty;

            if (!element.TryGetProperty(propertyName, out JsonElement property)
                || property.ValueKind != JsonValueKind.String)
            {
                return false;
            }

            value = property.GetString() ?? string.Empty;
            return value.Length > 0;
        }

        private void Save(IReadOnlyList<RecentOutputItem> items)
        {
            using (var stream = new MemoryStream())
            {
                using (var writer = new Utf8JsonWriter(stream, new JsonWriterOptions { Indented = true }))
                {
                    writer.WriteStartObject();
                    writer.WriteNumber("schemaVersion", SchemaVersion);
                    writer.WriteStartArray("items");

                    foreach (RecentOutputItem item in items)
                    {
                        writer.WriteStartObject();
                        writer.WriteString("path", item.Path);
                        writer.WriteString("displayName", item.DisplayName);
                        writer.WriteString("operation", item.Operation.ToString());
                        writer.WriteString("artifactKind", item.ArtifactKind.ToString());
                        writer.WriteString("completedAtUtc", item.CompletedAtUtc.ToString("O", CultureInfo.InvariantCulture));
                        writer.WriteEndObject();
                    }

                    writer.WriteEndArray();
                    writer.WriteEndObject();
                }

                AtomicFileWriter.Write(_filePath, Encoding.UTF8.GetString(stream.ToArray()));
            }
        }
    }
}
