namespace OrzioClashReport.Launcher.Contracts.Engine
{
    /// <summary>
    /// Reads which engine version this launcher build was packaged against. Returns <c>null</c> when
    /// the packaged declaration is absent or unreadable, which is a verification failure rather than
    /// an invitation to accept whatever engine happens to be present.
    /// </summary>
    public interface IEngineExpectationSource
    {
        string? ReadExpectedVersion(EngineLocation location);
    }
}
