namespace OrzioClashReport.Core.Model
{
    /// <summary>What a single piece of <see cref="ClashLifecycleEvidence"/> says about classifying a match-result slot automatically. Carries no score or weight.</summary>
    public enum ClashLifecycleEvidenceVerdict
    {
        /// <summary>This evidence is consistent with an automatic classification.</summary>
        SupportsClassification,

        /// <summary>This evidence prevents an automatic classification; the slot requires human review.</summary>
        BlocksClassification
    }
}
