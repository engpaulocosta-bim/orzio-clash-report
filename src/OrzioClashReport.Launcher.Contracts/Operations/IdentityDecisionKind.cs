namespace OrzioClashReport.Launcher.Contracts.Operations
{
    /// <summary>
    /// The two explicit human decisions the engine accepts. These member names are the canonical
    /// values passed to <c>--decision-kind</c> verbatim: they are never translated, abbreviated, or
    /// mapped, and only their visible label may be localised.
    /// </summary>
    public enum IdentityDecisionKind
    {
        /// <summary>A human confirmed the two occurrences are the same clash. Requires a persistent identity id.</summary>
        ConfirmSameIdentity = 0,

        /// <summary>A human rejected the pairing. Must never carry a persistent identity id.</summary>
        RejectSameIdentity = 1,
    }
}
