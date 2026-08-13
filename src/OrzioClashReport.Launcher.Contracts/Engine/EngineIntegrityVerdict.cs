namespace OrzioClashReport.Launcher.Contracts.Engine
{
    /// <summary>Outcome of comparing the engine executable against its packaged SHA-256 manifest.</summary>
    public enum EngineIntegrityVerdict
    {
        /// <summary>No verification was attempted (for example, the executable was not found).</summary>
        NotChecked = 0,

        /// <summary>The computed hash equals the manifest hash.</summary>
        Verified = 1,

        /// <summary>The computed hash differs from the manifest hash.</summary>
        Mismatch = 2,

        /// <summary>The manifest is absent or cannot be read, so nothing can be asserted about the executable.</summary>
        ManifestUnavailable = 3,
    }
}
