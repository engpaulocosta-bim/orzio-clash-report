using System;

namespace OrzioClashReport.Launcher.Contracts.Engine
{
    /// <summary>
    /// Immutable snapshot of what the launcher knows about the engine after probing it: its observed
    /// state, the version string it reported, the version this launcher build expects, its location,
    /// and the integrity verdict. <see cref="Detail"/> is an operator-facing explanation and must
    /// never contain an absolute path.
    /// </summary>
    public sealed class EngineInfo
    {
        public EngineStatusKind Status { get; }
        public string? ReportedVersion { get; }
        public string ExpectedVersion { get; }
        public EngineLocation? Location { get; }
        public EngineIntegrityResult Integrity { get; }
        public string Detail { get; }

        public EngineInfo(
            EngineStatusKind status,
            string? reportedVersion,
            string expectedVersion,
            EngineLocation? location,
            EngineIntegrityResult integrity,
            string detail)
        {
            if (!Enum.IsDefined(typeof(EngineStatusKind), status))
            {
                throw new ArgumentOutOfRangeException(nameof(status), status, "Unknown engine status.");
            }

            if (string.IsNullOrWhiteSpace(expectedVersion))
            {
                throw new ArgumentException("Expected engine version cannot be empty.", nameof(expectedVersion));
            }

            if (status == EngineStatusKind.Ready && string.IsNullOrWhiteSpace(reportedVersion))
            {
                throw new ArgumentException(
                    "A ready engine must have reported a version.", nameof(reportedVersion));
            }

            Status = status;
            ReportedVersion = reportedVersion;
            ExpectedVersion = expectedVersion;
            Location = location;
            Integrity = integrity ?? throw new ArgumentNullException(nameof(integrity));
            Detail = detail ?? throw new ArgumentNullException(nameof(detail));
        }

        /// <summary>
        /// True only when the engine is fully usable. Every other state must block execution and be
        /// explained to the user; the launcher never runs an engine it could not verify.
        /// </summary>
        public bool IsReady => Status == EngineStatusKind.Ready;
    }
}
