using System;
using OrzioClashReport.Launcher.Contracts.Results;

namespace OrzioClashReport.Launcher.Contracts.Operations
{
    /// <summary>
    /// Static facts about an operation that the UI needs but must not infer for itself: whether the
    /// engine accepts an explicit <c>-o</c> destination for it, and whether that destination is an
    /// HTML artifact (the only artifact class where a human may authorise a replacement).
    /// </summary>
    public static class LauncherOperationMetadata
    {
        /// <summary>
        /// True when the operation's published CLI contract accepts <c>-o</c>. Operations that do not
        /// (<see cref="LauncherOperationKind.AppendProjectSnapshot"/>, <see cref="LauncherOperationKind.RenderProject"/>,
        /// <see cref="LauncherOperationKind.AppendIdentityDecision"/>, <see cref="LauncherOperationKind.ValidateIdentityGovernance"/>)
        /// resolve their destination inside the engine, from state the engine already owns.
        /// </summary>
        public static bool SupportsOutputOption(LauncherOperationKind operation)
        {
            switch (operation)
            {
                case LauncherOperationKind.QuickReport:
                case LauncherOperationKind.Snapshot:
                case LauncherOperationKind.Compare:
                case LauncherOperationKind.CompareSnapshots:
                case LauncherOperationKind.IndexSnapshots:
                case LauncherOperationKind.CompareIndex:
                case LauncherOperationKind.CreateProject:
                case LauncherOperationKind.CreateIdentityGovernance:
                case LauncherOperationKind.RenderIdentityGovernanceReport:
                    return true;

                case LauncherOperationKind.AppendProjectSnapshot:
                case LauncherOperationKind.RenderProject:
                case LauncherOperationKind.AppendIdentityDecision:
                case LauncherOperationKind.ValidateIdentityGovernance:
                    return false;

                default:
                    throw new ArgumentOutOfRangeException(nameof(operation), operation, "Unknown launcher operation.");
            }
        }

        /// <summary>
        /// True when the operation's <c>-o</c> destination is a derived, regenerable HTML report. The
        /// engine may rewrite such a file, so the launcher detects the collision first and asks a human.
        /// Every other artifact (snapshot, run index, project catalog, governance document) is created
        /// with create-new semantics by the engine, and the launcher never offers to overwrite one.
        /// </summary>
        public static bool ProducesReplaceableHtmlOutput(LauncherOperationKind operation)
        {
            switch (operation)
            {
                case LauncherOperationKind.QuickReport:
                case LauncherOperationKind.Compare:
                case LauncherOperationKind.CompareSnapshots:
                case LauncherOperationKind.CompareIndex:
                case LauncherOperationKind.RenderIdentityGovernanceReport:
                    return true;

                default:
                    return false;
            }
        }

        /// <summary>
        /// What the operation's <c>-o</c> destination is, or <c>null</c> for operations that write no
        /// destination the launcher names. Used only to label a produced file, never to decide anything.
        /// </summary>
        public static LauncherArtifactKind? OutputArtifactKind(LauncherOperationKind operation)
        {
            switch (operation)
            {
                case LauncherOperationKind.QuickReport:
                case LauncherOperationKind.Compare:
                case LauncherOperationKind.CompareSnapshots:
                case LauncherOperationKind.CompareIndex:
                case LauncherOperationKind.RenderIdentityGovernanceReport:
                    return LauncherArtifactKind.HtmlReport;

                case LauncherOperationKind.Snapshot:
                    return LauncherArtifactKind.RunSnapshot;

                case LauncherOperationKind.IndexSnapshots:
                    return LauncherArtifactKind.RunIndex;

                case LauncherOperationKind.CreateProject:
                    return LauncherArtifactKind.ProjectCatalog;

                case LauncherOperationKind.CreateIdentityGovernance:
                    return LauncherArtifactKind.IdentityGovernance;

                case LauncherOperationKind.AppendProjectSnapshot:
                case LauncherOperationKind.RenderProject:
                case LauncherOperationKind.AppendIdentityDecision:
                case LauncherOperationKind.ValidateIdentityGovernance:
                    return null;

                default:
                    throw new ArgumentOutOfRangeException(nameof(operation), operation, "Unknown launcher operation.");
            }
        }
    }
}
