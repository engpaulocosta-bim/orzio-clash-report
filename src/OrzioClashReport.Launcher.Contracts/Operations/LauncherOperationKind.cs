namespace OrzioClashReport.Launcher.Contracts.Operations
{
    /// <summary>
    /// The engine operations the launcher can drive. Every member maps to exactly one published
    /// CLI contract of <c>orzioclash</c>; the launcher never invents a command, a flag, or a
    /// subcommand, and never composes a command line as text.
    /// </summary>
    public enum LauncherOperationKind
    {
        /// <summary>Single-run report: the XML input is positional and there is no subcommand.</summary>
        QuickReport = 0,
        Snapshot = 1,
        Compare = 2,
        CompareSnapshots = 3,
        IndexSnapshots = 4,
        CompareIndex = 5,
        CreateProject = 6,
        AppendProjectSnapshot = 7,
        RenderProject = 8,
        CreateIdentityGovernance = 9,
        AppendIdentityDecision = 10,
        ValidateIdentityGovernance = 11,
        RenderIdentityGovernanceReport = 12,
    }
}
