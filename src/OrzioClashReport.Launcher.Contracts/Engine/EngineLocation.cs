using System;

namespace OrzioClashReport.Launcher.Contracts.Engine
{
    /// <summary>
    /// Where the bundled engine lives on disk. Absolute paths are held in memory only; they are never
    /// written to a log or a diagnostic bundle without redaction.
    /// </summary>
    public sealed class EngineLocation
    {
        public string ExecutablePath { get; }
        public string ManifestPath { get; }

        public EngineLocation(string executablePath, string manifestPath)
        {
            if (string.IsNullOrWhiteSpace(executablePath))
            {
                throw new ArgumentException("Engine executable path cannot be empty.", nameof(executablePath));
            }

            if (string.IsNullOrWhiteSpace(manifestPath))
            {
                throw new ArgumentException("Engine manifest path cannot be empty.", nameof(manifestPath));
            }

            ExecutablePath = executablePath;
            ManifestPath = manifestPath;
        }
    }
}
