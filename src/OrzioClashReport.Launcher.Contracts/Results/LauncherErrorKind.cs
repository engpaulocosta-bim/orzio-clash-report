namespace OrzioClashReport.Launcher.Contracts.Results
{
    /// <summary>
    /// Launcher-side failure classification. This is deliberately not a taxonomy of engine exit codes:
    /// the engine works with 0 for success and 1 for failure, so every unclassified non-zero exit is
    /// reported as <see cref="EngineExecutionFailure"/> and the engine's own stderr is shown verbatim.
    /// </summary>
    public enum LauncherErrorKind
    {
        /// <summary>The engine exited non-zero and the launcher has nothing more specific to say about it.</summary>
        EngineExecutionFailure = 0,

        /// <summary>The engine exited 0 but the expected output file is absent or empty.</summary>
        OutputMissing = 1,

        /// <summary>No engine executable is available at the expected location.</summary>
        EngineMissing = 2,

        /// <summary>The engine did not finish inside the allowed time and was terminated.</summary>
        EngineTimeout = 3,

        /// <summary>The engine process could not be started at all.</summary>
        EngineStartFailure = 4,

        /// <summary>The user cancelled the job.</summary>
        Canceled = 5,

        /// <summary>The form was incomplete or malformed before anything was executed.</summary>
        InvalidInput = 6,

        /// <summary>The chosen destination already exists and no human decision has authorised replacing it.</summary>
        OutputCollision = 7,

        /// <summary>The engine failed its packaged SHA-256 integrity check.</summary>
        IntegrityFailure = 8,

        /// <summary>The engine reported a version this launcher build was not packaged against.</summary>
        VersionMismatch = 9,

        /// <summary>The launcher itself failed while preparing or finishing the job.</summary>
        LauncherFailure = 10,
    }
}
