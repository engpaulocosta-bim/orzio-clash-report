using System;
using System.IO;

namespace OrzioClashReport.Launcher.Infrastructure
{
    /// <summary>
    /// The launcher's own local data layout. Project data never lives here and this directory never
    /// lives inside the installation directory, so uninstalling the application cannot take a
    /// coordinator's work with it.
    /// </summary>
    public sealed class LauncherLocalPaths
    {
        private const string VendorFolder = "Orzio";
        private const string ApplicationFolder = "ClashReportLauncher";

        public LauncherLocalPaths(string rootDirectory)
        {
            if (string.IsNullOrWhiteSpace(rootDirectory))
            {
                throw new ArgumentException("Root directory must not be empty or whitespace.", nameof(rootDirectory));
            }

            RootDirectory = rootDirectory.Trim();
        }

        /// <summary><c>%LOCALAPPDATA%\Orzio\ClashReportLauncher</c> on Windows.</summary>
        public string RootDirectory { get; }

        public string SettingsFilePath => Path.Combine(RootDirectory, "settings.json");

        public string RecentItemsFilePath => Path.Combine(RootDirectory, "recent-items.json");

        public string LogsDirectory => Path.Combine(RootDirectory, "logs");

        public string JobsDirectory => Path.Combine(RootDirectory, "jobs");

        public string DiagnosticsDirectory => Path.Combine(RootDirectory, "diagnostics");

        /// <summary>Resolves the layout under the current user's local application data directory.</summary>
        public static LauncherLocalPaths ForCurrentUser()
        {
            string localApplicationData = Environment.GetFolderPath(
                Environment.SpecialFolder.LocalApplicationData,
                Environment.SpecialFolderOption.DoNotVerify);

            if (string.IsNullOrEmpty(localApplicationData))
            {
                // Every supported desktop platform resolves this folder; falling back to the user
                // profile keeps the launcher usable instead of failing at start.
                localApplicationData = Environment.GetFolderPath(
                    Environment.SpecialFolder.UserProfile,
                    Environment.SpecialFolderOption.DoNotVerify);
            }

            return new LauncherLocalPaths(
                Path.Combine(localApplicationData, VendorFolder, ApplicationFolder));
        }

        /// <summary>Creates every directory the launcher writes to. Safe to call repeatedly.</summary>
        public void EnsureCreated()
        {
            Directory.CreateDirectory(RootDirectory);
            Directory.CreateDirectory(LogsDirectory);
            Directory.CreateDirectory(JobsDirectory);
            Directory.CreateDirectory(DiagnosticsDirectory);
        }
    }
}
