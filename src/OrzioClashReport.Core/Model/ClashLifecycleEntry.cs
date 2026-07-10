using System;
using System.Collections.Generic;
using System.Linq;

namespace OrzioClashReport.Core.Model
{
    /// <summary>
    /// The conservative lifecycle classification of one slot from a <see cref="ClashRunMatchResult"/>: either a
    /// selected match (both sides present, <see cref="SelectedMatch"/> set) or an unmatched previous or current
    /// occurrence (exactly one side present, <see cref="SelectedMatch"/> null). Carries the ordered
    /// <see cref="Evidence"/> that justified <see cref="Status"/>. This type has no stable identity: it is not
    /// equatable, and it exposes no fingerprint or persistent clash id.
    /// </summary>
    public sealed class ClashLifecycleEntry
    {
        public ClashLifecycleStatus Status { get; }

        public int? PreviousIndex { get; }
        public int? CurrentIndex { get; }

        public ClashOccurrence? PreviousOccurrence { get; }
        public ClashOccurrence? CurrentOccurrence { get; }

        public ClashRunMatchCandidate? SelectedMatch { get; }

        public IReadOnlyList<ClashLifecycleEvidence> Evidence { get; }

        internal ClashLifecycleEntry(
            ClashLifecycleStatus status,
            int? previousIndex,
            int? currentIndex,
            ClashOccurrence? previousOccurrence,
            ClashOccurrence? currentOccurrence,
            ClashRunMatchCandidate? selectedMatch,
            IReadOnlyList<ClashLifecycleEvidence> evidence)
        {
            if (!Enum.IsDefined(typeof(ClashLifecycleStatus), status))
            {
                throw new ArgumentException($"'{status}' is not a defined {nameof(ClashLifecycleStatus)}.", nameof(status));
            }

            if (previousIndex.HasValue && previousIndex.Value < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(previousIndex), previousIndex, "Previous index cannot be negative.");
            }

            if (currentIndex.HasValue && currentIndex.Value < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(currentIndex), currentIndex, "Current index cannot be negative.");
            }

            if (previousIndex.HasValue != (previousOccurrence != null))
            {
                throw new ArgumentException(
                    "PreviousIndex and PreviousOccurrence must both be present or both be absent.", nameof(previousOccurrence));
            }

            if (currentIndex.HasValue != (currentOccurrence != null))
            {
                throw new ArgumentException(
                    "CurrentIndex and CurrentOccurrence must both be present or both be absent.", nameof(currentOccurrence));
            }

            bool hasPreviousSide = previousIndex.HasValue;
            bool hasCurrentSide = currentIndex.HasValue;

            if (!hasPreviousSide && !hasCurrentSide)
            {
                throw new ArgumentException("At least one side (previous or current) must be present.", nameof(previousIndex));
            }

            if (evidence == null)
            {
                throw new ArgumentNullException(nameof(evidence));
            }

            var evidenceCopy = new List<ClashLifecycleEvidence>(evidence);
            if (evidenceCopy.Count == 0)
            {
                throw new ArgumentException("Evidence must contain at least one item.", nameof(evidence));
            }

            for (int i = 0; i < evidenceCopy.Count; i++)
            {
                if (evidenceCopy[i] == null)
                {
                    throw new ArgumentException($"Evidence at index {i} cannot be null.", nameof(evidence));
                }
            }

            if (selectedMatch != null)
            {
                if (!hasPreviousSide || !hasCurrentSide)
                {
                    throw new ArgumentException(
                        "A selected match requires both the previous and current sides to be present.", nameof(selectedMatch));
                }

                if (previousIndex!.Value != selectedMatch.PreviousIndex || currentIndex!.Value != selectedMatch.CurrentIndex)
                {
                    throw new ArgumentException(
                        "PreviousIndex and CurrentIndex must equal the selected match's indexes.", nameof(selectedMatch));
                }

                if (!ReferenceEquals(previousOccurrence, selectedMatch.Assessment.PreviousOccurrence))
                {
                    throw new ArgumentException(
                        "PreviousOccurrence must be the same reference as the selected match's assessment previous occurrence.",
                        nameof(previousOccurrence));
                }

                if (!ReferenceEquals(currentOccurrence, selectedMatch.Assessment.CurrentOccurrence))
                {
                    throw new ArgumentException(
                        "CurrentOccurrence must be the same reference as the selected match's assessment current occurrence.",
                        nameof(currentOccurrence));
                }
            }
            else if (hasPreviousSide == hasCurrentSide)
            {
                throw new ArgumentException(
                    "An unmatched entry (no selected match) must have exactly one side present.", nameof(selectedMatch));
            }

            bool hasBlocker = evidenceCopy.Any(e => e.Verdict == ClashLifecycleEvidenceVerdict.BlocksClassification);

            switch (status)
            {
                case ClashLifecycleStatus.StillOpen:
                    if (selectedMatch == null || !hasPreviousSide || !hasCurrentSide || hasBlocker)
                    {
                        throw new ArgumentException(
                            "StillOpen requires a selected match, both sides present, and no blocking evidence.", nameof(status));
                    }

                    break;
                case ClashLifecycleStatus.New:
                    if (selectedMatch != null || hasPreviousSide || !hasCurrentSide || hasBlocker)
                    {
                        throw new ArgumentException(
                            "New requires only the current side, no selected match, and no blocking evidence.", nameof(status));
                    }

                    break;
                case ClashLifecycleStatus.Resolved:
                    if (selectedMatch != null || hasCurrentSide || !hasPreviousSide || hasBlocker)
                    {
                        throw new ArgumentException(
                            "Resolved requires only the previous side, no selected match, and no blocking evidence.", nameof(status));
                    }

                    break;
                case ClashLifecycleStatus.Unverifiable:
                    if (!hasBlocker)
                    {
                        throw new ArgumentException(
                            "Unverifiable requires at least one blocking evidence item.", nameof(status));
                    }

                    break;
            }

            Status = status;
            PreviousIndex = previousIndex;
            CurrentIndex = currentIndex;
            PreviousOccurrence = previousOccurrence;
            CurrentOccurrence = currentOccurrence;
            SelectedMatch = selectedMatch;
            Evidence = evidenceCopy.AsReadOnly();
        }

        /// <summary>Human-readable representation for logs and test output, e.g. "StillOpen: previous[2] x current[5] (3 evidence)". Not a persistence key.</summary>
        public override string ToString()
        {
            string previousPart = PreviousIndex.HasValue ? $"previous[{PreviousIndex}]" : "previous[-]";
            string currentPart = CurrentIndex.HasValue ? $"current[{CurrentIndex}]" : "current[-]";
            return $"{Status}: {previousPart} x {currentPart} ({Evidence.Count} evidence)";
        }
    }
}
