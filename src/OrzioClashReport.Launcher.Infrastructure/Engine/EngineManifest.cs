using System;

namespace OrzioClashReport.Launcher.Infrastructure.Engine
{
    /// <summary>
    /// The packaged description of the engine that shipped with this launcher build: which version it
    /// is and what its executable must hash to. The packaging script is the only writer; the launcher
    /// only ever reads it, and never repairs or regenerates it.
    /// </summary>
    public sealed class EngineManifest
    {
        public const int SupportedSchemaVersion = 1;

        public int SchemaVersion { get; }
        public string EngineVersion { get; }
        public string FileName { get; }
        public string Sha256 { get; }

        public EngineManifest(int schemaVersion, string engineVersion, string fileName, string sha256)
        {
            if (schemaVersion != SupportedSchemaVersion)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(schemaVersion),
                    schemaVersion,
                    $"Only engine manifest schema version {SupportedSchemaVersion} is supported.");
            }

            if (string.IsNullOrWhiteSpace(engineVersion))
            {
                throw new ArgumentException("Engine version cannot be empty.", nameof(engineVersion));
            }

            if (string.IsNullOrWhiteSpace(fileName))
            {
                throw new ArgumentException("Engine file name cannot be empty.", nameof(fileName));
            }

            if (string.IsNullOrWhiteSpace(sha256))
            {
                throw new ArgumentException("Engine SHA-256 cannot be empty.", nameof(sha256));
            }

            SchemaVersion = schemaVersion;
            EngineVersion = engineVersion;
            FileName = fileName;
            Sha256 = sha256.ToLowerInvariant();
        }
    }
}
