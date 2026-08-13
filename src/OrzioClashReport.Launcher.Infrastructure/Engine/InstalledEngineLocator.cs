using System;
using System.IO;
using System.Runtime.InteropServices;
using OrzioClashReport.Launcher.Contracts.Engine;

namespace OrzioClashReport.Launcher.Infrastructure.Engine
{
    /// <summary>
    /// Finds the engine at the one place the installer puts it: <c>&lt;install&gt;\engine\win-x64\</c>.
    /// It never searches PATH and never falls back to another copy, so the launcher can only ever run
    /// the engine it was packaged with.
    /// </summary>
    public sealed class InstalledEngineLocator : IEngineLocator
    {
        public const string EngineFolderName = "engine";
        public const string RuntimeFolderName = "win-x64";
        public const string ManifestFileName = "engine-manifest.json";

        private readonly string _baseDirectory;

        public InstalledEngineLocator(string? baseDirectory = null)
        {
            _baseDirectory = baseDirectory ?? AppContext.BaseDirectory;
        }

        /// <summary>
        /// The packaged executable name. Windows is the supported platform for this phase; the
        /// extension-free name lets the same locator be exercised on a Linux build agent.
        /// </summary>
        public static string ExecutableFileName =>
            RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? "orzioclash.exe" : "orzioclash";

        public string EngineDirectory => Path.Combine(_baseDirectory, EngineFolderName, RuntimeFolderName);

        public EngineLocation? Locate()
        {
            string executablePath = Path.Combine(EngineDirectory, ExecutableFileName);
            if (!File.Exists(executablePath))
            {
                return null;
            }

            return new EngineLocation(executablePath, Path.Combine(EngineDirectory, ManifestFileName));
        }
    }
}
