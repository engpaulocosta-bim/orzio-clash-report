using System;
using System.Collections.Generic;

namespace OrzioClashReport.Core.Model
{
    /// <summary>
    /// Immutable, auditable maximal sequence of <see cref="SelectedMatchContinuityLink"/>s connected only by
    /// exact selected-match object-reference continuity across consecutive comparison boundaries: this path
    /// asserts only that these exact continuity links form one maximal exact-reference-connected sequence. It
    /// does not assert that the underlying clash is a single persistent entity, that the path has stable
    /// identity, that it survives recalculation by id, or that it is a Clash Ledger record or a track; it is a
    /// derived and fully recalculable sequence, never persisted.
    /// </summary>
    public sealed class SelectedMatchContinuityPath
    {
        public IReadOnlyList<SelectedMatchContinuityLink> Links { get; }
        public IReadOnlyList<ClashRunMatchCandidate> SelectedMatches { get; }

        public int StartComparisonIndex { get; }
        public int EndComparisonIndex { get; }
        public int StartRunIndex { get; }
        public int EndRunIndex { get; }

        internal SelectedMatchContinuityPath(IReadOnlyList<SelectedMatchContinuityLink> links)
        {
            if (links == null)
            {
                throw new ArgumentNullException(nameof(links));
            }

            var linksCopy = new List<SelectedMatchContinuityLink>(links);

            if (linksCopy.Count == 0)
            {
                throw new ArgumentException("Links must contain at least one link.", nameof(links));
            }

            for (int i = 0; i < linksCopy.Count; i++)
            {
                if (linksCopy[i] == null)
                {
                    throw new ArgumentException($"Link at index {i} cannot be null.", nameof(links));
                }
            }

            for (int i = 1; i < linksCopy.Count; i++)
            {
                var previous = linksCopy[i - 1];
                var current = linksCopy[i];

                if (current.IncomingComparisonIndex != previous.OutgoingComparisonIndex)
                {
                    throw new ArgumentException(
                        $"Link at index {i} has IncomingComparisonIndex {current.IncomingComparisonIndex}, which "
                        + $"must equal the previous link's OutgoingComparisonIndex ({previous.OutgoingComparisonIndex}).",
                        nameof(links));
                }

                if (!ReferenceEquals(previous.OutgoingSelectedMatch, current.IncomingSelectedMatch))
                {
                    throw new ArgumentException(
                        $"Link at index {i}'s IncomingSelectedMatch must be the exact same object reference as "
                        + "the previous link's OutgoingSelectedMatch.",
                        nameof(links));
                }
            }

            Links = linksCopy.AsReadOnly();

            var selectedMatches = new List<ClashRunMatchCandidate> { linksCopy[0].IncomingSelectedMatch };
            foreach (var link in linksCopy)
            {
                selectedMatches.Add(link.OutgoingSelectedMatch);
            }

            SelectedMatches = selectedMatches.AsReadOnly();

            StartComparisonIndex = linksCopy[0].IncomingComparisonIndex;
            EndComparisonIndex = linksCopy[linksCopy.Count - 1].OutgoingComparisonIndex;
            StartRunIndex = StartComparisonIndex;
            EndRunIndex = EndComparisonIndex + 1;
        }

        /// <summary>Human-readable representation for logs and test output, e.g. "comparisons[0..2], 2 continuity link(s), 3 selected match(es)". Not a persistence key or clash identity.</summary>
        public override string ToString() =>
            $"comparisons[{StartComparisonIndex}..{EndComparisonIndex}], {Links.Count} continuity link(s), "
            + $"{SelectedMatches.Count} selected match(es)";
    }
}
