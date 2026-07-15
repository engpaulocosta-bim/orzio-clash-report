using System;
using System.Collections.Generic;

namespace OrzioClashReport.Core.Model
{
    /// <summary>
    /// Immutable, auditable result of projecting selected-match continuity across an exact
    /// <see cref="ClashRunSequenceComparisonResult"/>: the sequence comparison itself, plus the complete
    /// canonical set of <see cref="SelectedMatchContinuityLink"/>s that exist at its consecutive comparison
    /// boundaries. This type validates only that <see cref="Links"/> is exactly, and in canonical order, the
    /// deterministic structural projection implied by <see cref="SequenceComparison"/>'s selected matches; it
    /// never recomputes matching, lifecycle, or sequence comparison, and it creates no history, no multi-run
    /// lifecycle, and no persistent or stable clash identity.
    /// </summary>
    public sealed class ClashRunSequenceContinuityResult
    {
        public ClashRunSequenceComparisonResult SequenceComparison { get; }
        public IReadOnlyList<SelectedMatchContinuityLink> Links { get; }

        internal ClashRunSequenceContinuityResult(
            ClashRunSequenceComparisonResult sequenceComparison,
            IReadOnlyList<SelectedMatchContinuityLink> links)
        {
            SequenceComparison = sequenceComparison ?? throw new ArgumentNullException(nameof(sequenceComparison));

            if (links == null)
            {
                throw new ArgumentNullException(nameof(links));
            }

            var linksCopy = new List<SelectedMatchContinuityLink>(links);
            for (int i = 0; i < linksCopy.Count; i++)
            {
                if (linksCopy[i] == null)
                {
                    throw new ArgumentException($"Link at index {i} cannot be null.", nameof(links));
                }
            }

            int boundaryCount = sequenceComparison.Comparisons.Count - 1;

            for (int i = 0; i < linksCopy.Count; i++)
            {
                ValidateLink(linksCopy[i], i, sequenceComparison, boundaryCount);
            }

            ValidateCompleteCanonicalProjection(linksCopy, sequenceComparison);

            Links = linksCopy.AsReadOnly();
        }

        private static void ValidateLink(
            SelectedMatchContinuityLink link, int linkPosition, ClashRunSequenceComparisonResult sequenceComparison, int boundaryCount)
        {
            int boundary = link.IncomingComparisonIndex;

            if (boundary < 0 || boundary >= boundaryCount)
            {
                throw new ArgumentException(
                    $"Link at index {linkPosition} has IncomingComparisonIndex {boundary}, which is out of range "
                    + $"for {sequenceComparison.Comparisons.Count} comparisons ({boundaryCount} boundaries).",
                    nameof(link));
            }

            var incomingSelectedMatches = sequenceComparison.Comparisons[boundary].MatchResult.SelectedMatches;
            if (!ContainsByReference(incomingSelectedMatches, link.IncomingSelectedMatch))
            {
                throw new ArgumentException(
                    $"Link at index {linkPosition}'s IncomingSelectedMatch must be an exact member of "
                    + $"Comparisons[{boundary}].MatchResult.SelectedMatches.",
                    nameof(link));
            }

            var outgoingSelectedMatches = sequenceComparison.Comparisons[boundary + 1].MatchResult.SelectedMatches;
            if (!ContainsByReference(outgoingSelectedMatches, link.OutgoingSelectedMatch))
            {
                throw new ArgumentException(
                    $"Link at index {linkPosition}'s OutgoingSelectedMatch must be an exact member of "
                    + $"Comparisons[{boundary + 1}].MatchResult.SelectedMatches.",
                    nameof(link));
            }

            if (!ReferenceEquals(link.SharedRun, sequenceComparison.Runs[boundary + 1]))
            {
                throw new ArgumentException(
                    $"Link at index {linkPosition}'s SharedRun must be the exact object reference at Runs[{boundary + 1}].",
                    nameof(link));
            }

            if (link.SharedOccurrenceIndex >= link.SharedRun.Occurrences.Count)
            {
                throw new ArgumentException(
                    $"Link at index {linkPosition}'s SharedOccurrenceIndex ({link.SharedOccurrenceIndex}) is out of "
                    + $"range for SharedRun's {link.SharedRun.Occurrences.Count} occurrences.",
                    nameof(link));
            }

            if (!ReferenceEquals(link.SharedRun.Occurrences[link.SharedOccurrenceIndex], link.SharedOccurrence))
            {
                throw new ArgumentException(
                    $"Link at index {linkPosition}'s SharedOccurrence must be the exact object reference at "
                    + $"SharedRun.Occurrences[{link.SharedOccurrenceIndex}].",
                    nameof(link));
            }

            if (link.IncomingSelectedMatch.CurrentIndex != link.SharedOccurrenceIndex)
            {
                throw new ArgumentException(
                    $"Link at index {linkPosition}'s IncomingSelectedMatch.CurrentIndex must equal SharedOccurrenceIndex.",
                    nameof(link));
            }

            if (link.OutgoingSelectedMatch.PreviousIndex != link.SharedOccurrenceIndex)
            {
                throw new ArgumentException(
                    $"Link at index {linkPosition}'s OutgoingSelectedMatch.PreviousIndex must equal SharedOccurrenceIndex.",
                    nameof(link));
            }

            if (!ReferenceEquals(link.IncomingSelectedMatch.Assessment.CurrentOccurrence, link.SharedOccurrence))
            {
                throw new ArgumentException(
                    $"Link at index {linkPosition}'s IncomingSelectedMatch.Assessment.CurrentOccurrence must "
                    + "reference the exact SharedOccurrence instance.",
                    nameof(link));
            }

            if (!ReferenceEquals(link.OutgoingSelectedMatch.Assessment.PreviousOccurrence, link.SharedOccurrence))
            {
                throw new ArgumentException(
                    $"Link at index {linkPosition}'s OutgoingSelectedMatch.Assessment.PreviousOccurrence must "
                    + "reference the exact SharedOccurrence instance.",
                    nameof(link));
            }
        }

        /// <summary>
        /// Recomputes the complete, canonically ordered set of (boundary, shared slot) pairs that must have a
        /// link -- one exists exactly where a selected match's CurrentIndex enters a shared-run slot and a
        /// selected match's PreviousIndex leaves the same slot in the immediately following comparison -- and
        /// requires <paramref name="links"/> to match it exactly, position for position. This single positional
        /// comparison is what rejects a missing link, an extra link, a duplicate link, and any non-canonical
        /// order all at once: it is structural validation, never rematching.
        /// </summary>
        private static void ValidateCompleteCanonicalProjection(
            IReadOnlyList<SelectedMatchContinuityLink> links, ClashRunSequenceComparisonResult sequenceComparison)
        {
            var expected = new List<(int Boundary, int Slot)>();

            for (int boundary = 0; boundary < sequenceComparison.Comparisons.Count - 1; boundary++)
            {
                var incomingSelectedMatches = sequenceComparison.Comparisons[boundary].MatchResult.SelectedMatches;
                var outgoingSelectedMatches = sequenceComparison.Comparisons[boundary + 1].MatchResult.SelectedMatches;
                var sharedRun = sequenceComparison.Runs[boundary + 1];

                var incomingSlots = new HashSet<int>();
                foreach (var candidate in incomingSelectedMatches)
                {
                    incomingSlots.Add(candidate.CurrentIndex);
                }

                var outgoingSlots = new HashSet<int>();
                foreach (var candidate in outgoingSelectedMatches)
                {
                    outgoingSlots.Add(candidate.PreviousIndex);
                }

                for (int slot = 0; slot < sharedRun.Occurrences.Count; slot++)
                {
                    if (incomingSlots.Contains(slot) && outgoingSlots.Contains(slot))
                    {
                        expected.Add((boundary, slot));
                    }
                }
            }

            if (links.Count != expected.Count)
            {
                throw new ArgumentException(
                    $"Links must contain exactly the complete deterministic continuity projection: expected "
                    + $"{expected.Count} link(s), but found {links.Count}.",
                    nameof(links));
            }

            for (int i = 0; i < expected.Count; i++)
            {
                var link = links[i];
                if (link.IncomingComparisonIndex != expected[i].Boundary || link.SharedOccurrenceIndex != expected[i].Slot)
                {
                    throw new ArgumentException(
                        $"Link at index {i} must be the canonical link for boundary {expected[i].Boundary}, "
                        + $"shared slot {expected[i].Slot} (ordered by IncomingComparisonIndex ascending, then "
                        + "SharedOccurrenceIndex ascending), but found "
                        + $"boundary {link.IncomingComparisonIndex}, shared slot {link.SharedOccurrenceIndex}.",
                        nameof(links));
                }
            }
        }

        private static bool ContainsByReference(IReadOnlyList<ClashRunMatchCandidate> list, ClashRunMatchCandidate item)
        {
            foreach (var candidate in list)
            {
                if (ReferenceEquals(candidate, item))
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>Human-readable representation for logs and test output, e.g. "3 runs, 2 adjacent comparisons, 1 selected-match continuity link(s)". Not a persistence key.</summary>
        public override string ToString() =>
            $"{SequenceComparison.Runs.Count} runs, {SequenceComparison.Comparisons.Count} adjacent comparisons, "
            + $"{Links.Count} selected-match continuity link(s)";
    }
}
