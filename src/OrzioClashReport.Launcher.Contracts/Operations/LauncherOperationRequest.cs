namespace OrzioClashReport.Launcher.Contracts.Operations
{
    /// <summary>
    /// Base of every immutable launcher operation request.
    /// A request describes intent in typed terms only. It never carries a command line, an
    /// argument string, a working directory, or any engine process detail.
    /// </summary>
    public abstract class LauncherOperationRequest
    {
        private protected LauncherOperationRequest()
        {
        }

        /// <summary>The operation this request describes.</summary>
        public abstract LauncherOperationKind Kind { get; }

        /// <summary>
        /// The absolute destination this operation writes with <c>-o</c>, or <c>null</c> when the
        /// operation has no <c>-o</c> and the engine owns its own destination.
        /// </summary>
        public virtual string? OutputPath => null;

        /// <summary>
        /// Returns the same request aimed at a different absolute destination. Used when a human
        /// resolves an output collision by choosing another file rather than replacing one.
        /// </summary>
        public virtual LauncherOperationRequest WithOutputPath(string outputPath)
        {
            throw new System.NotSupportedException(
                "This operation has no output option; its destination belongs to the engine.");
        }
    }
}
