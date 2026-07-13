namespace OrzioClashReport.Core.Model
{
    /// <summary>The category of signal a <see cref="ClashLifecycleEvidence"/> item records while classifying one match-result slot.</summary>
    public enum ClashLifecycleEvidenceKind
    {
        /// <summary>Whether a one-to-one selected match consumes (or does not consume) this slot.</summary>
        SelectedMatch,

        /// <summary>The <see cref="ClashMatchConfidence"/> of a selected match.</summary>
        MatchConfidence,

        /// <summary>Whether an alternative candidate shares this slot's previous or current index.</summary>
        CandidateAmbiguity,

        /// <summary>Whether a revision-free <see cref="ModelIdentity"/> is declared in the other run.</summary>
        ModelIdentityPresence,

        /// <summary>Whether the clash test and revision-free model pair are explicitly declared as executed in the other run's manifest.</summary>
        ClashTestPresence
    }
}
