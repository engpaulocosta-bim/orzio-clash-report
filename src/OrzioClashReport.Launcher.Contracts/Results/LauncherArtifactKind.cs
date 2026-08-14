namespace OrzioClashReport.Launcher.Contracts.Results
{
    /// <summary>
    /// What kind of file an operation produced. The engine owns the persistence semantics of each of
    /// these; the launcher only records that one was produced so it can be opened or revealed.
    /// </summary>
    public enum LauncherArtifactKind
    {
        HtmlReport = 0,
        RunSnapshot = 1,
        RunIndex = 2,
        ProjectCatalog = 3,
        IdentityGovernance = 4,
    }
}
