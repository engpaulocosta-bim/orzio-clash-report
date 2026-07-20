using System;
using System.Collections.Generic;

namespace OrzioClashReport.Core.Model
{
    /// <summary>
    /// Immutable presentation of one <see cref="ClashLifecycleEntry"/> at its exact position within one
    /// pairwise <see cref="ClashLifecycleResult"/>, optionally annotated with the exact
    /// <see cref="SelectedMatchContinuityPath"/> it belongs to. This adds no status, confidence, or identity of
    /// its own -- <see cref="LifecycleEntry"/> remains the sole authority for status and evidence.
    /// </summary>
    public sealed class ClashRunSequenceLifecycleEntryPresentation
    {
        public int ComparisonIndex { get; }
        public int EntryIndex { get; }
        public ClashLifecycleResult Comparison { get; }
        public ClashLifecycleEntry LifecycleEntry { get; }
        public SelectedMatchContinuityPath? ContinuityPath { get; }
        public bool IsInContinuityPath => ContinuityPath != null;

        internal ClashRunSequenceLifecycleEntryPresentation(
            int comparisonIndex,
            int entryIndex,
            ClashLifecycleResult comparison,
            ClashLifecycleEntry lifecycleEntry,
            SelectedMatchContinuityPath? continuityPath)
        {
            if (comparisonIndex < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(comparisonIndex), comparisonIndex, "Comparison index cannot be negative.");
            }

            if (entryIndex < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(entryIndex), entryIndex, "Entry index cannot be negative.");
            }

            Comparison = comparison ?? throw new ArgumentNullException(nameof(comparison));
            LifecycleEntry = lifecycleEntry ?? throw new ArgumentNullException(nameof(lifecycleEntry));

            if (entryIndex >= comparison.Entries.Count)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(entryIndex), entryIndex,
                    $"Entry index is out of range for Comparison's {comparison.Entries.Count} entries.");
            }

            if (!ReferenceEquals(comparison.Entries[entryIndex], lifecycleEntry))
            {
                throw new ArgumentException(
                    $"LifecycleEntry must be the exact object reference at Comparison.Entries[{entryIndex}].",
                    nameof(lifecycleEntry));
            }

            if (continuityPath != null)
            {
                if (lifecycleEntry.SelectedMatch == null)
                {
                    throw new ArgumentException(
                        "ContinuityPath can only be set for a lifecycle entry that has a selected match.",
                        nameof(continuityPath));
                }

                if (!ContainsByReference(continuityPath.SelectedMatches, lifecycleEntry.SelectedMatch))
                {
                    throw new ArgumentException(
                        "LifecycleEntry.SelectedMatch must be an exact member of ContinuityPath.SelectedMatches.",
                        nameof(continuityPath));
                }
            }

            ComparisonIndex = comparisonIndex;
            EntryIndex = entryIndex;
            ContinuityPath = continuityPath;
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

        /// <summary>Human-readable representation for logs and test output, e.g. "comparison[0].entry[2]: StillOpen (in continuity path)". Not a persistence key.</summary>
        public override string ToString() =>
            $"comparison[{ComparisonIndex}].entry[{EntryIndex}]: {LifecycleEntry.Status}"
            + (IsInContinuityPath ? " (in continuity path)" : string.Empty);
    }
}
