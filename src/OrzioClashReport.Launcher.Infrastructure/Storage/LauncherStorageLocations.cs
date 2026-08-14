using System;
using System.IO;

namespace OrzioClashReport.Launcher.Infrastructure.Storage
{
    /// <summary>
    /// Every path the launcher writes to. All of it lives under the per-user local application data
    /// directory, never under the installation directory, so a user's data survives uninstall and the
    /// application never needs write access to Program Files.
    /// </summary>
    public sealed class LauncherStorageLocations
    {
        public const string VendorFolderName = "Orzio";
        public const string ApplicationFolderName = "ClashReportLauncher";

        public string RootDirectory { get; }
        public string SettingsFilePath { get; }
        public string RecentItemsFilePath { get; }
        public string LogsDirectory { get; }
        public string JobsDirectory { get; }
        public string DiagnosticsDirectory { get; }

        public LauncherStorageLocations(string rootDirectory)
        {
            if (string.IsNullOrWhiteSpace(rootDirectory))
            {
                throw new ArgumentException("Root directory cannot be empty.", nameof(rootDirectory));
            }

            RootDirectory = rootDirectory;
            SettingsFilePath = Path.Combine(rootDirectory, "settings.json");
            RecentItemsFilePath = Path.Combine(rootDirectory, "recent-items.json");
            LogsDirectory = Path.Combine(rootDirectory, "logs");
            JobsDirectory = Path.Combine(rootDirectory, "jobs");
            DiagnosticsDirectory = Path.Combine(rootDirectory, "diagnostics");
        }

        /// <summary>
        /// The production location: <c>%LOCALAPPDATA%\Orzio\ClashReportLauncher</c> on Windows, and the
        /// equivalent local application data folder elsewhere.
        /// </summary>
        public static LauncherStorageLocations CreateDefault()
        {
            string localApplicationData = Environment.GetFolderPath(
                Environment.SpecialFolder.LocalApplicationData,
                Environment.SpecialFolderOption.Create);

            if (string.IsNullOrWhiteSpace(localApplicationData))
            {
                // Environment.SpecialFolder can legitimately return empty on a stripped-down profile.
                // Falling back to the user profile keeps launcher data inside the user's own space.
                localApplicationData = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            }

            return new LauncherStorageLocations(
                Path.Combine(localApplicationData, VendorFolderName, ApplicationFolderName));
        }

        /// <summary>Creates the directories the launcher writes to. Existing directories are left untouched.</summary>
        public void EnsureCreated()
        {
            Directory.CreateDirectory(RootDirectory);
            Directory.CreateDirectory(LogsDirectory);
            Directory.CreateDirectory(JobsDirectory);
            Directory.CreateDirectory(DiagnosticsDirectory);
        }
    }
}
