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
    }
}
