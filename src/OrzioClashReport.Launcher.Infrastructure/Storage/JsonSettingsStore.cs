using System;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using OrzioClashReport.Launcher.Contracts.Settings;

namespace OrzioClashReport.Launcher.Infrastructure.Storage
{
    /// <summary>
    /// Stores preferences as <c>settings.json</c> under the launcher's local application data folder.
    /// A missing or corrupt file yields the defaults: a broken preference must never stop the
    /// application from opening.
    /// </summary>
    public sealed class JsonSettingsStore : ISettingsStore
    {
        private const int SchemaVersion = 1;

        private readonly string _filePath;

        public JsonSettingsStore(string filePath)
        {
            if (string.IsNullOrWhiteSpace(filePath))
            {
                throw new ArgumentException("Settings file path cannot be empty.", nameof(filePath));
            }

            _filePath = filePath;
        }

        public Task<LauncherSettings> LoadAsync(CancellationToken cancellationToken)
        {
            if (!File.Exists(_filePath))
            {
                return Task.FromResult(LauncherSettings.Default);
            }

            try
            {
                using (JsonDocument document = JsonDocument.Parse(File.ReadAllText(_filePath)))
                {
                    JsonElement root = document.RootElement;
                    if (root.ValueKind != JsonValueKind.Object)
                    {
                        return Task.FromResult(LauncherSettings.Default);
                    }

                    LauncherThemePreference theme = LauncherThemePreference.System;
                    if (root.TryGetProperty("theme", out JsonElement themeElement)
                        && themeElement.ValueKind == JsonValueKind.String
                        && Enum.TryParse(themeElement.GetString(), ignoreCase: false, out LauncherThemePreference parsed))
                    {
                        theme = parsed;
                    }

                    string? lastOutputDirectory = null;
                    if (root.TryGetProperty("lastOutputDirectory", out JsonElement directoryElement)
                        && directoryElement.ValueKind == JsonValueKind.String)
                    {
                        lastOutputDirectory = directoryElement.GetString();
                    }

                    bool showExperimentalWarnings = true;
                    if (root.TryGetProperty("showExperimentalWarnings", out JsonElement warningsElement)
                        && (warningsElement.ValueKind == JsonValueKind.True
                            || warningsElement.ValueKind == JsonValueKind.False))
                    {
                        showExperimentalWarnings = warningsElement.GetBoolean();
                    }

                    return Task.FromResult(
                        new LauncherSettings(theme, lastOutputDirectory, showExperimentalWarnings));
                }
            }
            catch (Exception exception) when (
                exception is JsonException
                || exception is IOException
                || exception is UnauthorizedAccessException
                || exception is System.Security.SecurityException)
            {
                return Task.FromResult(LauncherSettings.Default);
            }
        }

        public Task SaveAsync(LauncherSettings settings, CancellationToken cancellationToken)
        {
            if (settings == null)
            {
                throw new ArgumentNullException(nameof(settings));
            }

            var builder = new StringBuilder();
            using (var stream = new MemoryStream())
            {
                using (var writer = new Utf8JsonWriter(stream, new JsonWriterOptions { Indented = true }))
                {
                    writer.WriteStartObject();
                    writer.WriteNumber("schemaVersion", SchemaVersion);
                    writer.WriteString("theme", settings.Theme.ToString());

                    if (settings.LastOutputDirectory == null)
                    {
                        writer.WriteNull("lastOutputDirectory");
                    }
                    else
                    {
                        writer.WriteString("lastOutputDirectory", settings.LastOutputDirectory);
                    }

                    writer.WriteBoolean("showExperimentalWarnings", settings.ShowExperimentalWarnings);
                    writer.WriteEndObject();
                }

                builder.Append(Encoding.UTF8.GetString(stream.ToArray()));
            }

            AtomicFileWriter.Write(_filePath, builder.ToString());
            return Task.CompletedTask;
        }
    }
}
