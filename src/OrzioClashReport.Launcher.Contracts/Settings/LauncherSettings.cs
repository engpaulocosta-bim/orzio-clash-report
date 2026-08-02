using System;

namespace OrzioClashReport.Launcher.Contracts.Settings
{
    /// <summary>
    /// The language the interface is shown in. This changes visible text only: it never changes a
    /// value the engine receives, a file name, or anything a technical result depends on.
    /// </summary>
    public enum InterfaceLanguage
    {
        /// <summary>Follow the operating system, falling back to English.</summary>
        System = 0,

        Portuguese = 1,

        English = 2,
    }

    public enum ThemePreference
    {
        System = 0,
        Light = 1,
        Dark = 2,
    }

    /// <summary>
    /// The launcher's own local preferences. Nothing here affects a technical result: settings
    /// change presentation and convenience only.
    /// </summary>
    public sealed class LauncherSettings
    {
        public LauncherSettings(
            ThemePreference theme,
            InterfaceLanguage language,
            string? lastOutputDirectory,
            bool showExperimentalWarnings)
        {
            Theme = theme;
            Language = language;
            LastOutputDirectory = string.IsNullOrWhiteSpace(lastOutputDirectory)
                ? null
                : lastOutputDirectory!.Trim();
            ShowExperimentalWarnings = showExperimentalWarnings;
        }

        public ThemePreference Theme { get; }

        public InterfaceLanguage Language { get; }

        /// <summary>Where the last output was written, so the next picker opens somewhere useful.</summary>
        public string? LastOutputDirectory { get; }

        public bool ShowExperimentalWarnings { get; }

        public static LauncherSettings Default { get; } = new LauncherSettings(
            ThemePreference.System,
            InterfaceLanguage.System,
            lastOutputDirectory: null,
            showExperimentalWarnings: true);

        public LauncherSettings WithTheme(ThemePreference theme)
        {
            return new LauncherSettings(theme, Language, LastOutputDirectory, ShowExperimentalWarnings);
        }

        public LauncherSettings WithLanguage(InterfaceLanguage language)
        {
            return new LauncherSettings(Theme, language, LastOutputDirectory, ShowExperimentalWarnings);
        }

        public LauncherSettings WithLastOutputDirectory(string? directory)
        {
            return new LauncherSettings(Theme, Language, directory, ShowExperimentalWarnings);
        }

        public LauncherSettings WithShowExperimentalWarnings(bool show)
        {
            return new LauncherSettings(Theme, Language, LastOutputDirectory, show);
        }
    }

    /// <summary>One recently produced output, shown on Home so the last results stay one click away.</summary>
    public sealed class RecentOutputItem
    {
        public RecentOutputItem(string fileName, string fullPath, DateTimeOffset createdAtUtc)
        {
            if (string.IsNullOrWhiteSpace(fileName))
            {
                throw new ArgumentException("File name must not be empty or whitespace.", nameof(fileName));
            }

            if (string.IsNullOrWhiteSpace(fullPath))
            {
                throw new ArgumentException("Full path must not be empty or whitespace.", nameof(fullPath));
            }

            FileName = fileName.Trim();
            FullPath = fullPath.Trim();
            CreatedAtUtc = createdAtUtc;
        }

        public string FileName { get; }

        /// <summary>Absolute path, kept locally so the file can be opened or revealed.</summary>
        public string FullPath { get; }

        public DateTimeOffset CreatedAtUtc { get; }
    }
}
