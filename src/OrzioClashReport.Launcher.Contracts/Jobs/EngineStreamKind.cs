namespace OrzioClashReport.Launcher.Contracts.Jobs
{
    /// <summary>Which engine stream a captured line came from.</summary>
    public enum EngineStreamKind
    {
        StandardOutput = 0,
        StandardError = 1,
    }
}
