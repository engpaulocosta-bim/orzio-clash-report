namespace OrzioClashReport.Launcher.Contracts.Engine
{
    /// <summary>
    /// The state of the bundled engine as observed by the launcher. Every state must be presented
    /// with an icon and a text label, never with colour alone.
    /// </summary>
    public enum EngineStatusKind
    {
        /// <summary>The launcher has not finished probing the engine yet.</summary>
        Checking = 0,

        /// <summary>The engine executable exists, reports a parseable version, and integrity checks passed.</summary>
        Ready = 1,

        /// <summary>The engine reported a version other than the one this launcher build was packaged with.</summary>
        VersionMismatch = 2,

        /// <summary>The engine executable exists but its SHA-256 does not match the packaged manifest.</summary>
        IntegrityFailure = 3,

        /// <summary>No engine executable was found at the expected location.</summary>
        Missing = 4,

        /// <summary>The engine ran but its output could not be understood, or the platform cannot run it.</summary>
        Unsupported = 5,
    }
}
