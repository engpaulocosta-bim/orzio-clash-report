namespace OrzioClashReport.Launcher.Contracts.Operations
{
    /// <summary>
    /// The launcher operations that map one-to-one onto an existing published engine contract.
    /// The launcher never invents an operation the engine does not already expose.
    /// </summary>
    public enum LauncherOperationKind
    {
        /// <summary>Single-run grouped HTML report. Positional XML plus <c>-o</c>; no subcommand.</summary>
        QuickReport = 0,

        /// <summary><c>snapshot</c>.</summary>
        CreateSnapshot = 1,

        /// <summary><c>compare</c>.</summary>
        CompareRuns = 2,

        /// <summary><c>compare-snapshots</c>.</summary>
        CompareSnapshots = 3,

        /// <summary><c>index-snapshots</c>.</summary>
        IndexSnapshots = 4,

        /// <summary><c>compare-index</c>.</summary>
        CompareIndex = 5,

        /// <summary><c>create-project</c>.</summary>
        CreateProject = 6,

        /// <summary><c>append-project-snapshot</c>.</summary>
        AppendProjectSnapshot = 7,

        /// <summary><c>render-project</c>.</summary>
        RenderProject = 8,

        /// <summary><c>create-identity-governance</c>.</summary>
        CreateIdentityGovernance = 9,

        /// <summary><c>append-identity-decision</c>.</summary>
        AppendIdentityDecision = 10,

        /// <summary><c>validate-identity-governance</c>.</summary>
        ValidateIdentityGovernance = 11,

        /// <summary><c>render-identity-governance-report</c>.</summary>
        RenderIdentityGovernanceReport = 12,
    }
}
