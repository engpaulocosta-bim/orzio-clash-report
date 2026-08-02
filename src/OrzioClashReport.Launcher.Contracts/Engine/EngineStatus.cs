using System;

namespace OrzioClashReport.Launcher.Contracts.Engine
{
    /// <summary>
    /// The engine states the launcher can honestly distinguish. Every state must be presented with
    /// text and an icon, never with colour alone.
    /// </summary>
    public enum EngineStatus
    {
        /// <summary>The probe has not finished yet.</summary>
        Checking = 0,

        /// <summary>The engine was found, its integrity matched, and its version is supported.</summary>
        Ready = 1,

        /// <summary>The engine answered with a version this launcher build does not expect.</summary>
        VersionMismatch = 2,

        /// <summary>The engine file exists but its SHA-256 does not match the packaged manifest.</summary>
        IntegrityFailure = 3,

        /// <summary>No engine executable was found at the expected location.</summary>
        Missing = 4,

        /// <summary>The engine exists but did not answer the version probe in a usable way.</summary>
        Unsupported = 5,
    }

    /// <summary>Result of one engine version probe. Immutable.</summary>
    public sealed class EngineProbeResult
    {
        private EngineProbeResult(EngineStatus status, string? version, string? expectedVersion, string? detail)
        {
            Status = status;
            Version = version;
            ExpectedVersion = expectedVersion;
            Detail = detail;
        }

        public EngineStatus Status { get; }

        /// <summary>The version the engine reported, when it reported one.</summary>
        public string? Version { get; }

        /// <summary>The version this launcher build expects, when the mismatch is known.</summary>
        public string? ExpectedVersion { get; }

        /// <summary>A short, already redacted explanation suitable for display.</summary>
        public string? Detail { get; }

        public bool IsUsable => Status == EngineStatus.Ready;

        public static EngineProbeResult Ready(string version)
        {
            return new EngineProbeResult(EngineStatus.Ready, RequireVersion(version), expectedVersion: null, detail: null);
        }

        public static EngineProbeResult VersionMismatch(string version, string expectedVersion)
        {
            return new EngineProbeResult(
                EngineStatus.VersionMismatch,
                RequireVersion(version),
                RequireVersion(expectedVersion),
                detail: null);
        }

        public static EngineProbeResult IntegrityFailure(string detail)
        {
            return new EngineProbeResult(EngineStatus.IntegrityFailure, version: null, expectedVersion: null, detail);
        }

        public static EngineProbeResult Missing(string detail)
        {
            return new EngineProbeResult(EngineStatus.Missing, version: null, expectedVersion: null, detail);
        }

        public static EngineProbeResult Unsupported(string detail)
        {
            return new EngineProbeResult(EngineStatus.Unsupported, version: null, expectedVersion: null, detail);
        }

        private static string RequireVersion(string version)
        {
            if (string.IsNullOrWhiteSpace(version))
            {
                throw new ArgumentException("Version must not be empty or whitespace.", nameof(version));
            }

            return version.Trim();
        }
    }
}
