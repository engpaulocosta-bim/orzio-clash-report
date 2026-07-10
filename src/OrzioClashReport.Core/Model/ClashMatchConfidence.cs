namespace OrzioClashReport.Core.Model
{
    /// <summary>
    /// How strongly a candidate <see cref="ClashMatchAssessment"/> is corroborated by its
    /// <see cref="MatchEvidence"/>. This describes a candidate relationship between two occurrences; it never
    /// proves that they are the same clash, and no confidence level authorizes any automatic action.
    /// </summary>
    public enum ClashMatchConfidence
    {
        /// <summary>Weak or incomplete candidate. Treat as a hypothesis for human review, not a conclusion.</summary>
        Low,

        /// <summary>Plausible candidate with relevant evidence, but still ambiguous or incomplete.</summary>
        Medium,

        /// <summary>
        /// Candidate strongly corroborated by evidence. This does not mean "exact", "confirmed by a human", or
        /// automatically actionable -- there is no higher level than this in the MVP.
        /// </summary>
        High
    }
}
