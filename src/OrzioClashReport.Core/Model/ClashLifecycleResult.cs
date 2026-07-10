using System;
using System.Collections.Generic;

namespace OrzioClashReport.Core.Model
{
    /// <summary>
    /// Immutable, auditable result of classifying every slot of a <see cref="ClashRunMatchResult"/>: one
    /// <see cref="ClashLifecycleEntry"/> per selected match (in <see cref="ClashRunMatchResult.SelectedMatches"/>
    /// order), then one entry per unmatched previous slot (by ascending previous index), then one entry per
    /// unmatched current slot (by ascending current index). This type only validates that the entries
    /// structurally partition <see cref="MatchResult"/>'s slots correctly; it does not recompute any entry's
    /// <see cref="ClashLifecycleEntry.Status"/>.
    /// </summary>
    public sealed class ClashLifecycleResult
    {
        public ClashRunMatchResult MatchResult { get; }
        public IReadOnlyList<ClashLifecycleEntry> Entries { get; }

        internal ClashLifecycleResult(ClashRunMatchResult matchResult, IReadOnlyList<ClashLifecycleEntry> entries)
        {
            MatchResult = matchResult ?? throw new ArgumentNullException(nameof(matchResult));

            if (entries == null)
            {
                throw new ArgumentNullException(nameof(entries));
            }

            var entriesCopy = new List<ClashLifecycleEntry>(entries);
            for (int i = 0; i < entriesCopy.Count; i++)
            {
                if (entriesCopy[i] == null)
                {
                    throw new ArgumentException($"Entry at index {i} cannot be null.", nameof(entries));
                }
            }

            int selectedCount = matchResult.SelectedMatches.Count;

            var usedPreviousIndexes = new HashSet<int>();
            var usedCurrentIndexes = new HashSet<int>();
            foreach (var selected in matchResult.SelectedMatches)
            {
                usedPreviousIndexes.Add(selected.PreviousIndex);
                usedCurrentIndexes.Add(selected.CurrentIndex);
            }

            var expectedUnmatchedPreviousIndexes = new List<int>();
            for (int index = 0; index < matchResult.PreviousRun.Occurrences.Count; index++)
            {
                if (!usedPreviousIndexes.Contains(index))
                {
                    expectedUnmatchedPreviousIndexes.Add(index);
                }
            }

            var expectedUnmatchedCurrentIndexes = new List<int>();
            for (int index = 0; index < matchResult.CurrentRun.Occurrences.Count; index++)
            {
                if (!usedCurrentIndexes.Contains(index))
                {
                    expectedUnmatchedCurrentIndexes.Add(index);
                }
            }

            int expectedCount = selectedCount + expectedUnmatchedPreviousIndexes.Count + expectedUnmatchedCurrentIndexes.Count;
            if (entriesCopy.Count != expectedCount)
            {
                throw new ArgumentException(
                    $"Entries must contain exactly {expectedCount} items (selected + unmatched previous + unmatched current), "
                    + $"but found {entriesCopy.Count}.",
                    nameof(entries));
            }

            for (int i = 0; i < selectedCount; i++)
            {
                var entry = entriesCopy[i];
                var expectedCandidate = matchResult.SelectedMatches[i];

                if (!ReferenceEquals(entry.SelectedMatch, expectedCandidate))
                {
                    throw new ArgumentException(
                        $"Entry at index {i} must correspond to the selected match at the same position in SelectedMatches.",
                        nameof(entries));
                }
            }

            for (int i = 0; i < expectedUnmatchedPreviousIndexes.Count; i++)
            {
                int entryPosition = selectedCount + i;
                var entry = entriesCopy[entryPosition];
                int expectedIndex = expectedUnmatchedPreviousIndexes[i];

                if (entry.SelectedMatch != null || entry.CurrentIndex.HasValue || entry.PreviousIndex != expectedIndex)
                {
                    throw new ArgumentException(
                        $"Entry at index {entryPosition} must be an unmatched-previous entry for previous index {expectedIndex}.",
                        nameof(entries));
                }

                if (!ReferenceEquals(entry.PreviousOccurrence, matchResult.PreviousRun.Occurrences[expectedIndex]))
                {
                    throw new ArgumentException(
                        $"Entry at index {entryPosition} must reference the exact occurrence at previous index {expectedIndex}.",
                        nameof(entries));
                }
            }

            for (int i = 0; i < expectedUnmatchedCurrentIndexes.Count; i++)
            {
                int entryPosition = selectedCount + expectedUnmatchedPreviousIndexes.Count + i;
                var entry = entriesCopy[entryPosition];
                int expectedIndex = expectedUnmatchedCurrentIndexes[i];

                if (entry.SelectedMatch != null || entry.PreviousIndex.HasValue || entry.CurrentIndex != expectedIndex)
                {
                    throw new ArgumentException(
                        $"Entry at index {entryPosition} must be an unmatched-current entry for current index {expectedIndex}.",
                        nameof(entries));
                }

                if (!ReferenceEquals(entry.CurrentOccurrence, matchResult.CurrentRun.Occurrences[expectedIndex]))
                {
                    throw new ArgumentException(
                        $"Entry at index {entryPosition} must reference the exact occurrence at current index {expectedIndex}.",
                        nameof(entries));
                }
            }

            Entries = entriesCopy.AsReadOnly();
        }

        /// <summary>Human-readable representation for logs and test output, e.g. "run-1 -> run-2: 12 entries (5 still open, 2 new, 3 resolved, 2 unverifiable)". Not a persistence key.</summary>
        public override string ToString()
        {
            int stillOpen = 0;
            int newCount = 0;
            int resolved = 0;
            int unverifiable = 0;

            foreach (var entry in Entries)
            {
                switch (entry.Status)
                {
                    case ClashLifecycleStatus.StillOpen:
                        stillOpen++;
                        break;
                    case ClashLifecycleStatus.New:
                        newCount++;
                        break;
                    case ClashLifecycleStatus.Resolved:
                        resolved++;
                        break;
                    case ClashLifecycleStatus.Unverifiable:
                        unverifiable++;
                        break;
                }
            }

            return $"{MatchResult.PreviousRun.RunId} -> {MatchResult.CurrentRun.RunId}: {Entries.Count} entries "
                + $"({stillOpen} still open, {newCount} new, {resolved} resolved, {unverifiable} unverifiable)";
        }
    }
}
