using System;
using OrzioClashReport.Launcher.Contracts.Operations;

namespace OrzioClashReport.Launcher.Application
{
    /// <summary>
    /// How the engine persists what an operation produces. This decides whether the launcher is
    /// allowed to offer a human the choice to replace an existing file.
    /// </summary>
    public enum OutputPersistenceKind
    {
        /// <summary>The operation writes nothing the launcher names; the engine owns the destination.</summary>
        EngineOwned = 0,

        /// <summary>
        /// The engine uses create-new semantics and refuses to overwrite. The launcher must never
        /// offer to replace one of these: snapshots, run indexes, project catalogs, and governance
        /// documents are evidence, and replacing them is not a choice the launcher can grant.
        /// </summary>
        CreateNew = 1,

        /// <summary>
        /// A derived HTML report the engine may overwrite. The launcher detects the collision first
        /// and asks a human, with replacing never the default.
        /// </summary>
        DerivedHtml = 2,
    }

    /// <summary>
    /// The stable per-operation facts the launcher needs before it runs anything. This is policy the
    /// engine's published contracts already fix; nothing here is inferred from engine output.
    /// </summary>
    public static class LauncherOperationPolicy
    {
        /// <summary>Whether the engine accepts <c>-o</c> for this operation.</summary>
        public static bool SupportsOutputOption(LauncherOperationKind kind)
        {
            switch (kind)
            {
                case LauncherOperationKind.AppendProjectSnapshot:
                case LauncherOperationKind.RenderProject:
                case LauncherOperationKind.ValidateIdentityGovernance:
                    return false;
                default:
                    RequireKnown(kind);
                    return true;
            }
        }

        /// <summary>How the engine persists this operation's output.</summary>
        public static OutputPersistenceKind OutputPersistence(LauncherOperationKind kind)
        {
            switch (kind)
            {
                case LauncherOperationKind.QuickReport:
                case LauncherOperationKind.CompareRuns:
                case LauncherOperationKind.CompareSnapshots:
                case LauncherOperationKind.CompareIndex:
                case LauncherOperationKind.RenderIdentityGovernanceReport:
                    return OutputPersistenceKind.DerivedHtml;

                case LauncherOperationKind.CreateSnapshot:
                case LauncherOperationKind.IndexSnapshots:
                case LauncherOperationKind.CreateProject:
                case LauncherOperationKind.CreateIdentityGovernance:
                    return OutputPersistenceKind.CreateNew;

                case LauncherOperationKind.AppendProjectSnapshot:
                case LauncherOperationKind.RenderProject:
                case LauncherOperationKind.ValidateIdentityGovernance:
                case LauncherOperationKind.AppendIdentityDecision:
                    return OutputPersistenceKind.EngineOwned;

                default:
                    throw UnknownKind(kind);
            }
        }

        /// <summary>
        /// Whether the operation exercises engine behaviour that has not been validated against real
        /// sequential historical exports. The user interface says so rather than implying confidence.
        /// </summary>
        public static bool IsExperimental(LauncherOperationKind kind)
        {
            switch (kind)
            {
                case LauncherOperationKind.CompareRuns:
                case LauncherOperationKind.CompareSnapshots:
                case LauncherOperationKind.CompareIndex:
                case LauncherOperationKind.RenderProject:
                    return true;
                default:
                    RequireKnown(kind);
                    return false;
            }
        }

        private static void RequireKnown(LauncherOperationKind kind)
        {
            if (!Enum.IsDefined(typeof(LauncherOperationKind), kind))
            {
                throw UnknownKind(kind);
            }
        }

        private static ArgumentOutOfRangeException UnknownKind(LauncherOperationKind kind)
        {
            return new ArgumentOutOfRangeException(nameof(kind), kind, "Unknown launcher operation.");
        }
    }
}
