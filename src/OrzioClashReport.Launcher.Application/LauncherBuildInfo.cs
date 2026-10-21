using System.Reflection;

namespace OrzioClashReport.Launcher.Application
{
    /// <summary>
    /// Identity of this launcher build. The launcher version comes from <c>src/LauncherVersion.props</c>
    /// through this assembly's informational version, so the installer, the release tag and the update
    /// check can never disagree with what the application reports. The engine version here is the
    /// fallback used only for display when the packaged engine manifest cannot be read; the manifest is
    /// the authority whenever it exists, so version and hash always come from what was actually shipped.
    /// </summary>
    public static class LauncherBuildInfo
    {
        public static string LauncherVersion { get; } = ReadLauncherVersion();

        public const string FallbackExpectedEngineVersion = "0.1.0-preview.3";

        private static string ReadLauncherVersion()
        {
            string? informational = typeof(LauncherBuildInfo).Assembly
                .GetCustomAttribute<AssemblyInformationalVersionAttribute>()?
                .InformationalVersion;

            if (string.IsNullOrWhiteSpace(informational))
            {
                return "0.0.0-unknown";
            }

            // The SDK appends "+<commit>" build metadata; it is not part of the release version.
            int metadata = informational.IndexOf('+');
            return metadata >= 0 ? informational.Substring(0, metadata) : informational;
        }
    }
}
