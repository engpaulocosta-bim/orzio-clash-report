namespace OrzioClashReport.Core.Model
{
    /// <summary>What a single piece of <see cref="MatchEvidence"/> says about a candidate match.</summary>
    public enum MatchEvidenceVerdict
    {
        /// <summary>This evidence is consistent with the two occurrences being the same clash.</summary>
        Supports,

        /// <summary>This evidence is inconsistent with the two occurrences being the same clash.</summary>
        Contradicts,

        /// <summary>This evidence could not be evaluated. Unavailable does not mean the occurrences fail to correspond.</summary>
        Unavailable
    }
}
