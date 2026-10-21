using System;
using OrzioClashReport.Launcher.Application.Localization;
using OrzioClashReport.Launcher.Contracts.Engine;

namespace OrzioClashReport.Launcher.Application.Presentation
{
    /// <summary>
    /// How one <see cref="EngineStatusKind"/> is presented. Glyph and label are both required and both
    /// unique across states, so the badge never communicates through colour alone.
    /// </summary>
    public sealed class EngineStatusPresentation
    {
        public EngineStatusKind Status { get; }
        public string Glyph { get; }
        public string Label { get; }
        public string Explanation { get; }
        public LauncherSeverity Severity { get; }

        private EngineStatusPresentation(
            EngineStatusKind status,
            string glyph,
            string label,
            string explanation,
            LauncherSeverity severity)
        {
            Status = status;
            Glyph = glyph;
            Label = label;
            Explanation = explanation;
            Severity = severity;
        }

        public static EngineStatusPresentation For(EngineStatusKind status)
        {
            switch (status)
            {
                case EngineStatusKind.Checking:
                    return new EngineStatusPresentation(
                        status,
                        "⋯",
                        Loc.T("engine.checking.label"),
                        Loc.T("engine.checking.explanation"),
                        LauncherSeverity.Neutral);

                case EngineStatusKind.Ready:
                    return new EngineStatusPresentation(
                        status,
                        "✓",
                        Loc.T("engine.ready.label"),
                        Loc.T("engine.ready.explanation"),
                        LauncherSeverity.Positive);

                case EngineStatusKind.VersionMismatch:
                    return new EngineStatusPresentation(
                        status,
                        "≠",
                        Loc.T("engine.versionMismatch.label"),
                        Loc.T("engine.versionMismatch.explanation"),
                        LauncherSeverity.Caution);

                case EngineStatusKind.IntegrityFailure:
                    return new EngineStatusPresentation(
                        status,
                        "⚠",
                        Loc.T("engine.integrityFailure.label"),
                        Loc.T("engine.integrityFailure.explanation"),
                        LauncherSeverity.Critical);

                case EngineStatusKind.Missing:
                    return new EngineStatusPresentation(
                        status,
                        "✕",
                        Loc.T("engine.missing.label"),
                        Loc.T("engine.missing.explanation"),
                        LauncherSeverity.Critical);

                case EngineStatusKind.Unsupported:
                    return new EngineStatusPresentation(
                        status,
                        "?",
                        Loc.T("engine.unsupported.label"),
                        Loc.T("engine.unsupported.explanation"),
                        LauncherSeverity.Critical);

                default:
                    throw new ArgumentOutOfRangeException(nameof(status), status, "Unknown engine status.");
            }
        }
    }
}
