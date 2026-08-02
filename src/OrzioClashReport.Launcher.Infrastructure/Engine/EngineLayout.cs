using System;
using System.IO;

namespace OrzioClashReport.Launcher.Infrastructure.Engine
{
    /// <summary>
    /// Where the packaged engine lives relative to the installed launcher:
    /// <c>&lt;install&gt;\engine\win-x64\orzioclash.exe</c> beside
    /// <c>&lt;install&gt;\engine\win-x64\engine-manifest.json</c>.
    /// </summary>
    public sealed class EngineLayout
    {
        public const string ExecutableFileNameWindows = "orzioclash.exe";
        public const string ExecutableFileNameOther = "orzioclash";
        public const string ManifestFileName = "engine-manifest.json";
        public const string RuntimeIdentifierDirectory = "win-x64";

        public EngineLayout(string installationDirectory)
        {
            if (string.IsNullOrWhiteSpace(installationDirectory))
            {
                throw new ArgumentException(
                    "Installation directory must not be empty or whitespace.", nameof(installationDirectory));
            }

            InstallationDirectory = installationDirectory.Trim();
        }

        public string InstallationDirectory { get; }

        public string EngineDirectory =>
            Path.Combine(InstallationDirectory, "engine", RuntimeIdentifierDirectory);

        public string ExecutablePath => Path.Combine(EngineDirectory, ExecutableFileName);

        public string ManifestPath => Path.Combine(EngineDirectory, ManifestFileName);

        /// <summary>The samples shipped with the installation, used by the first-run walkthrough.</summary>
        public string SamplesDirectory => Path.Combine(InstallationDirectory, "samples");

        private static string ExecutableFileName =>
            OperatingSystem.IsWindows() ? ExecutableFileNameWindows : ExecutableFileNameOther;

        /// <summary>Resolves the layout from the directory the launcher itself was started from.</summary>
        public static EngineLayout ForInstalledLauncher()
        {
            return new EngineLayout(AppContext.BaseDirectory);
        }
    }
}
