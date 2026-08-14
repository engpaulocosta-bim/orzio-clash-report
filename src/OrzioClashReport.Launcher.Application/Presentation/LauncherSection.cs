namespace OrzioClashReport.Launcher.Application.Presentation
{
    /// <summary>
    /// The navigation sections of the shell, in display order. There are exactly seven, and there is no
    /// basic/advanced mode: every user sees the same application.
    /// </summary>
    public enum LauncherSection
    {
        Home = 0,
        QuickReport = 1,
        Snapshots = 2,
        Longitudinal = 3,
        Projects = 4,
        Governance = 5,
        Settings = 6,
    }
}
