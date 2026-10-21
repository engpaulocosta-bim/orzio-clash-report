using System;

namespace OrzioClashReport.Launcher.Contracts.Settings
{
    /// <summary>
    /// The launcher's own local preferences. It holds no project data, no credentials, and no telemetry
    /// switch, because there is no telemetry. <see cref="LastOutputDirectory"/> is a convenience for the
    /// file pickers and is never written to a log.
    /// </summary>
    public sealed class LauncherSettings
    {
        public LauncherThemePreference Theme { get; }
        public string? LastOutputDirectory { get; }
        public bool ShowExperimentalWarnings { get; }

        /// <summary>The interface language. Defaults to following the operating system.</summary>
        public LauncherLanguagePreference Language { get; }

        public LauncherSettings(
            LauncherThemePreference theme,
            string? lastOutputDirectory,
            bool showExperimentalWarnings,
            LauncherLanguagePreference language = LauncherLanguagePreference.System)
        {
            if (!Enum.IsDefined(typeof(LauncherThemePreference), theme))
            {
                throw new ArgumentOutOfRangeException(nameof(theme), theme, "Unknown theme preference.");
            }

            if (!Enum.IsDefined(typeof(LauncherLanguagePreference), language))
            {
                throw new ArgumentOutOfRangeException(nameof(language), language, "Unknown language preference.");
            }

            Theme = theme;
            LastOutputDirectory = string.IsNullOrWhiteSpace(lastOutputDirectory) ? null : lastOutputDirectory;
            ShowExperimentalWarnings = showExperimentalWarnings;
            Language = language;
        }

        public static LauncherSettings Default { get; } =
            new LauncherSettings(LauncherThemePreference.System, null, true);

        public LauncherSettings WithTheme(LauncherThemePreference theme) =>
            new LauncherSettings(theme, LastOutputDirectory, ShowExperimentalWarnings, Language);

        public LauncherSettings WithLastOutputDirectory(string? directory) =>
            new LauncherSettings(Theme, directory, ShowExperimentalWarnings, Language);

        public LauncherSettings WithShowExperimentalWarnings(bool show) =>
            new LauncherSettings(Theme, LastOutputDirectory, show, Language);

        public LauncherSettings WithLanguage(LauncherLanguagePreference language) =>
            new LauncherSettings(Theme, LastOutputDirectory, ShowExperimentalWarnings, language);
    }
}
