namespace OrzioClashReport.Launcher.Application
{
    /// <summary>
    /// Identity of this launcher build. The engine version here is the fallback used only for display
    /// when the packaged engine manifest cannot be read; the manifest is the authority whenever it
    /// exists, so version and hash always come from what was actually shipped.
    /// </summary>
    public static class LauncherBuildInfo
    {
        public const string LauncherVersion = "0.2.0-launcher-preview.1";

        public const string FallbackExpectedEngineVersion = "0.1.0-preview.3";
    }
}
