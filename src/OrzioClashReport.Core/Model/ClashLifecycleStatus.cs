namespace OrzioClashReport.Core.Model
{
    /// <summary>
    /// Conservative lifecycle classification of one slot in a <see cref="ClashRunMatchResult"/> (a selected
    /// match, or an unmatched previous/current occurrence). This is distinct from <see cref="ClashStatus"/>,
    /// the raw review status reported by the clash-detection source for a single clash: this enum describes
    /// what changed between two coordination runs, not a reviewer's state for one clash. There is no
    /// <c>Reopened</c> value -- distinguishing a genuinely new clash from one that reopened requires history
    /// across more than two runs, which is out of scope here.
    /// </summary>
    public enum ClashLifecycleStatus
    {
        /// <summary>A previous/current pair was selected as a one-to-one match with no blocking evidence.</summary>
        StillOpen,

        /// <summary>An unmatched current occurrence whose model identities and clash test were verifiably present in the previous run, with no blocking evidence.</summary>
        New,

        /// <summary>An unmatched previous occurrence whose model identities and clash test are verifiably present in the current run, with no blocking evidence.</summary>
        Resolved,

        /// <summary>At least one piece of evidence blocks an automatic classification; this slot requires human review.</summary>
        Unverifiable
    }
}
