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

        public LauncherSettings(
            LauncherThemePreference theme,
            string? lastOutputDirectory,
            bool showExperimentalWarnings)
        {
            if (!Enum.IsDefined(typeof(LauncherThemePreference), theme))
            {
                throw new ArgumentOutOfRangeException(nameof(theme), theme, "Unknown theme preference.");
            }

            Theme = theme;
            LastOutputDirectory = string.IsNullOrWhiteSpace(lastOutputDirectory) ? null : lastOutputDirectory;
            ShowExperimentalWarnings = showExperimentalWarnings;
        }

        public static LauncherSettings Default { get; } =
            new LauncherSettings(LauncherThemePreference.System, null, true);

        public LauncherSettings WithTheme(LauncherThemePreference theme) =>
            new LauncherSettings(theme, LastOutputDirectory, ShowExperimentalWarnings);

        public LauncherSettings WithLastOutputDirectory(string? directory) =>
            new LauncherSettings(Theme, directory, ShowExperimentalWarnings);

        public LauncherSettings WithShowExperimentalWarnings(bool show) =>
            new LauncherSettings(Theme, LastOutputDirectory, show);
    }
}
