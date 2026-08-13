namespace OrzioClashReport.Launcher.Contracts.Engine
{
    /// <summary>
    /// Resolves where the bundled engine should be. Returns <c>null</c> when nothing is present at the
    /// expected location; it never searches PATH, never falls back to another directory, and never
    /// downloads anything.
    /// </summary>
    public interface IEngineLocator
    {
        EngineLocation? Locate();
    }
}
