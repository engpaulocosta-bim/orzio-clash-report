namespace OrzioClashReport.Launcher.Contracts.Results
{
    /// <summary>
    /// Conditions the user must see but which never change what the launcher submits to the engine. A
    /// warning is never silently resolved: a duplicate entry in an ordered list, for instance, is
    /// reported and kept, never removed.
    /// </summary>
    public enum LauncherWarningKind
    {
        /// <summary>The same file appears more than once in an explicitly ordered list. It is preserved as declared.</summary>
        DuplicateOrderedInput = 0,

        /// <summary>The feature is experimental and has not been validated against real sequential exports.</summary>
        ExperimentalFeature = 1,

        /// <summary>Engine output exceeded the retained buffer and the middle of the stream was dropped.</summary>
        EngineOutputTruncated = 2,

        /// <summary>The engine is not in a ready state, so the operation cannot be started.</summary>
        EngineNotReady = 3,

        /// <summary>An algorithmic suggestion is being displayed. It is never a human decision.</summary>
        AlgorithmicSuggestionOnly = 4,
    }
}
