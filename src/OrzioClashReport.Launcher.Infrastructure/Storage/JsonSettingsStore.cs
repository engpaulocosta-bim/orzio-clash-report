using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using OrzioClashReport.Launcher.Contracts.Ports;
using OrzioClashReport.Launcher.Contracts.Settings;

namespace OrzioClashReport.Launcher.Infrastructure.Storage
{
    internal static class LocalJson
    {
        internal static readonly JsonSerializerOptions Options = new JsonSerializerOptions
        {
            WriteIndented = true,
            AllowTrailingCommas = false,
            ReadCommentHandling = JsonCommentHandling.Disallow,
            Converters = { new JsonStringEnumConverter() },
        };

        private static readonly UTF8Encoding Utf8NoBom = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false);

        /// <summary>
        /// Writes through a temporary file in the same directory and replaces the destination only
        /// after the write completed, so a crash mid-write cannot leave a truncated settings file.
        /// </summary>
        internal static void WriteAtomically(string path, string content)
        {
            string directory = Path.GetDirectoryName(path)
                ?? throw new InvalidOperationException("Local data path must have a directory.");
            Directory.CreateDirectory(directory);

            string temporary = Path.Combine(
                directory, "." + Path.GetFileName(path) + "." + Guid.NewGuid().ToString("N") + ".tmp");

            try
            {
                File.WriteAllText(temporary, content, Utf8NoBom);
                File.Move(temporary, path, overwrite: true);
            }
            finally
            {
                if (File.Exists(temporary))
                {
                    try
                    {
                        File.Delete(temporary);
                    }
                    catch (IOException)
                    {
                        // A leftover temporary file is harmless; failing the save because of it is not.
                    }
                }
            }
        }
    }

    /// <summary>Reads and writes the launcher's settings file, falling back to defaults on damage.</summary>
    public sealed class JsonSettingsStore : ISettingsStore
    {
        private sealed class Document
        {
            public ThemePreference Theme { get; set; } = ThemePreference.System;

            public InterfaceLanguage Language { get; set; } = InterfaceLanguage.System;

            public string? LastOutputDirectory { get; set; }

            public bool ShowExperimentalWarnings { get; set; } = true;
        }

        private readonly string _path;

        public JsonSettingsStore(string settingsFilePath)
        {
            if (string.IsNullOrWhiteSpace(settingsFilePath))
            {
                throw new ArgumentException("Settings path must not be empty or whitespace.", nameof(settingsFilePath));
            }

            _path = settingsFilePath.Trim();
        }

        public LauncherSettings Load()
        {
            if (!File.Exists(_path))
            {
                return LauncherSettings.Default;
            }

            try
            {
                Document? document = JsonSerializer.Deserialize<Document>(
                    File.ReadAllText(_path), LocalJson.Options);

                if (document == null)
                {
                    return LauncherSettings.Default;
                }

                return new LauncherSettings(
                    document.Theme,
                    document.Language,
                    document.LastOutputDirectory,
                    document.ShowExperimentalWarnings);
            }
            catch (Exception ex) when (ex is JsonException or IOException or UnauthorizedAccessException)
            {
                // A damaged preferences file must never stop the application from opening.
                return LauncherSettings.Default;
            }
        }

        public void Save(LauncherSettings settings)
        {
            if (settings == null)
            {
                throw new ArgumentNullException(nameof(settings));
            }

            var document = new Document
            {
                Theme = settings.Theme,
                Language = settings.Language,
                LastOutputDirectory = settings.LastOutputDirectory,
                ShowExperimentalWarnings = settings.ShowExperimentalWarnings,
            };

            LocalJson.WriteAtomically(_path, JsonSerializer.Serialize(document, LocalJson.Options));
        }
    }

    /// <summary>
    /// Keeps the most recent outputs, newest first, capped so the list stays a shortcut rather than
    /// a history. Nothing here is evidence and nothing here is ever sent anywhere.
    /// </summary>
    public sealed class JsonRecentItemsStore : IRecentItemsStore
    {
        internal const int MaximumItems = 20;

        private sealed class Entry
        {
            public string FileName { get; set; } = string.Empty;

            public string FullPath { get; set; } = string.Empty;

            public DateTimeOffset CreatedAtUtc { get; set; }
        }

        private readonly string _path;

        public JsonRecentItemsStore(string recentItemsFilePath)
        {
            if (string.IsNullOrWhiteSpace(recentItemsFilePath))
            {
                throw new ArgumentException(
                    "Recent items path must not be empty or whitespace.", nameof(recentItemsFilePath));
            }

            _path = recentItemsFilePath.Trim();
        }

        public IReadOnlyList<RecentOutputItem> Load()
        {
            if (!File.Exists(_path))
            {
                return Array.Empty<RecentOutputItem>();
            }

            List<Entry>? entries;
            try
            {
                entries = JsonSerializer.Deserialize<List<Entry>>(File.ReadAllText(_path), LocalJson.Options);
            }
            catch (Exception ex) when (ex is JsonException or IOException or UnauthorizedAccessException)
            {
                return Array.Empty<RecentOutputItem>();
            }

            if (entries == null)
            {
                return Array.Empty<RecentOutputItem>();
            }

            var items = new List<RecentOutputItem>(entries.Count);
            foreach (Entry entry in entries)
            {
                if (string.IsNullOrWhiteSpace(entry.FileName) || string.IsNullOrWhiteSpace(entry.FullPath))
                {
                    continue;
                }

                items.Add(new RecentOutputItem(entry.FileName, entry.FullPath, entry.CreatedAtUtc));
            }

            return items;
        }

        public void Add(RecentOutputItem item)
        {
            if (item == null)
            {
                throw new ArgumentNullException(nameof(item));
            }

            var items = new List<RecentOutputItem>(Load());
            items.RemoveAll(existing =>
                string.Equals(existing.FullPath, item.FullPath, StringComparison.OrdinalIgnoreCase));
            items.Insert(0, item);

            if (items.Count > MaximumItems)
            {
                items.RemoveRange(MaximumItems, items.Count - MaximumItems);
            }

            var document = new List<Entry>(items.Count);
            foreach (RecentOutputItem entry in items)
            {
                document.Add(new Entry
                {
                    FileName = entry.FileName,
                    FullPath = entry.FullPath,
                    CreatedAtUtc = entry.CreatedAtUtc,
                });
            }

            LocalJson.WriteAtomically(_path, JsonSerializer.Serialize(document, LocalJson.Options));
        }
    }
}
