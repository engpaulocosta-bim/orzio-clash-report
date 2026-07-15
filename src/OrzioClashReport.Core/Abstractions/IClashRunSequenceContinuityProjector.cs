using OrzioClashReport.Core.Model;

namespace OrzioClashReport.Core.Abstractions
{
    /// <summary>
    /// Projects an already-derived <see cref="ClashRunSequenceComparisonResult"/> onto the set of
    /// <see cref="SelectedMatchContinuityLink"/>s that exist between its consecutive pairwise comparisons: for
    /// each pair of adjacent comparisons that share a run, a link exists wherever one selected match enters an
    /// occurrence slot in that shared run and another selected match leaves the same exact slot.
    ///
    /// <para>
    /// <b>Input authority.</b> The <paramref name="sequenceComparison"/> passed to <see cref="Project"/> is
    /// already fully derived: its run order, its pairwise comparisons, and every comparison's
    /// <see cref="ClashRunMatchResult.SelectedMatches"/> are authoritative and are never recomputed by this
    /// port. This port does not know about run-index JSON, snapshot files, or any other persistence format --
    /// it consumes only the in-memory <see cref="ClashRunSequenceComparisonResult"/>.
    /// </para>
    ///
    /// <para>
    /// <b>Selected matches only.</b> Implementations consider only
    /// <see cref="ClashRunMatchResult.SelectedMatches"/>. <see cref="ClashRunMatchResult.Candidates"/>,
    /// <see cref="ClashRunMatchResult.AlternativeCandidates"/>, <see cref="ClashRunMatchResult.UnmatchedPrevious"/>,
    /// and <see cref="ClashRunMatchResult.UnmatchedCurrent"/> never produce a link. Lifecycle status (for
    /// example <see cref="ClashLifecycleStatus.Unverifiable"/>) never filters a selected match out of
    /// consideration either -- this port projects continuity strictly from the one-to-one selected-match
    /// relation, not from lifecycle classification.
    /// </para>
    ///
    /// <para>
    /// <b>Consecutive comparison boundaries only.</b> For comparisons <c>[0] -&gt; [1] -&gt; [2] -&gt; ...</c>,
    /// only boundary <c>i</c> (between <c>Comparisons[i]</c> and <c>Comparisons[i + 1]</c>) is considered, and
    /// only through the exact run they share (<c>Comparisons[i].CurrentRun</c>, which is the same reference as
    /// <c>Comparisons[i + 1].PreviousRun</c>). No non-adjacent boundary is ever considered, and no run is ever
    /// compared against another run directly by this port.
    /// </para>
    ///
    /// <para>
    /// <b>Exact shared occurrence slot.</b> A link requires that the incoming selected match's current slot and
    /// the outgoing selected match's previous slot be the exact same index into the shared run's occurrences,
    /// and that both selected matches reference the exact same <see cref="ClashOccurrence"/> instance at that
    /// slot. Value-shaped equivalence at a different slot is never sufficient.
    /// </para>
    ///
    /// <para>
    /// <b>Purity.</b> Implementations of <see cref="Project"/> must be synchronous, perform no I/O, filesystem,
    /// network, or clock access, introduce no randomness, and must not mutate <paramref name="sequenceComparison"/>
    /// or anything it references. They call no <see cref="IClashMatcher"/>, <see cref="IClashRunComparer"/>,
    /// <see cref="IClashLifecycleClassifier"/>, or <see cref="IClashRunSequenceComparer"/> -- matching, run
    /// comparison, lifecycle classification, and sequence comparison have already happened by the time this
    /// port runs. For the same input, the result must be deterministic.
    /// </para>
    /// </summary>
    public interface IClashRunSequenceContinuityProjector
    {
        ClashRunSequenceContinuityResult Project(ClashRunSequenceComparisonResult sequenceComparison);
    }
}
