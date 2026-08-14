namespace OrzioClashReport.Launcher.Contracts.Logging
{
    /// <summary>
    /// The class of location a path lives in. This is the only structural fact about a path the launcher
    /// records by default; the path itself never reaches a log.
    /// </summary>
    public enum PathRootKind
    {
        Unknown = 0,
        UserProfile = 1,
        LocalApplicationData = 2,
        InstallationDirectory = 3,
        TemporaryDirectory = 4,
        NetworkShare = 5,
        OtherLocalVolume = 6,
    }
}
