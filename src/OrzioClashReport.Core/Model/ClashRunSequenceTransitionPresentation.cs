using System;
using System.Collections.Generic;

namespace OrzioClashReport.Core.Model
{
    /// <summary>
    /// Immutable presentation of one adjacent pairwise <see cref="ClashLifecycleResult"/>: the comparison index,
    /// the comparison itself, and the complete ordered presentation of every one of its lifecycle entries.
    /// </summary>
    public sealed class ClashRunSequenceTransitionPresentation
    {
        public int ComparisonIndex { get; }
        public ClashLifecycleResult Comparison { get; }
        public IReadOnlyList<ClashRunSequenceLifecycleEntryPresentation> LifecycleEntries { get; }

        internal ClashRunSequenceTransitionPresentation(
            int comparisonIndex,
            ClashLifecycleResult comparison,
            IReadOnlyList<ClashRunSequenceLifecycleEntryPresentation> lifecycleEntries)
        {
            if (comparisonIndex < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(comparisonIndex), comparisonIndex, "Comparison index cannot be negative.");
            }

            Comparison = comparison ?? throw new ArgumentNullException(nameof(comparison));

            if (lifecycleEntries == null)
            {
                throw new ArgumentNullException(nameof(lifecycleEntries));
            }

            var entriesCopy = new List<ClashRunSequenceLifecycleEntryPresentation>(lifecycleEntries);

            if (entriesCopy.Count != comparison.Entries.Count)
            {
                throw new ArgumentException(
                    $"LifecycleEntries must contain exactly {comparison.Entries.Count} item(s), the complete "
                    + $"representation of Comparison.Entries, but found {entriesCopy.Count}.",
                    nameof(lifecycleEntries));
            }

            for (int i = 0; i < entriesCopy.Count; i++)
            {
                var item = entriesCopy[i];
                if (item == null)
                {
                    throw new ArgumentException($"LifecycleEntries item at index {i} cannot be null.", nameof(lifecycleEntries));
                }

                if (item.ComparisonIndex != comparisonIndex)
                {
                    throw new ArgumentException(
                        $"LifecycleEntries item at index {i} must have ComparisonIndex {comparisonIndex}, but found {item.ComparisonIndex}.",
                        nameof(lifecycleEntries));
                }

                if (!ReferenceEquals(item.Comparison, comparison))
                {
                    throw new ArgumentException(
                        $"LifecycleEntries item at index {i} must reference the exact same Comparison instance.",
                        nameof(lifecycleEntries));
                }

                if (item.EntryIndex != i)
                {
                    throw new ArgumentException(
                        $"LifecycleEntries item at index {i} must have EntryIndex {i}, but found {item.EntryIndex}.",
                        nameof(lifecycleEntries));
                }

                if (!ReferenceEquals(item.LifecycleEntry, comparison.Entries[i]))
                {
                    throw new ArgumentException(
                        $"LifecycleEntries item at index {i} must reference the exact Comparison.Entries[{i}] instance.",
                        nameof(lifecycleEntries));
                }
            }

            ComparisonIndex = comparisonIndex;
            LifecycleEntries = entriesCopy.AsReadOnly();
        }

        /// <summary>Human-readable representation for logs and test output, e.g. "comparison[0]: 5 lifecycle entry presentation(s)". Not a persistence key.</summary>
        public override string ToString() => $"comparison[{ComparisonIndex}]: {LifecycleEntries.Count} lifecycle entry presentation(s)";
    }
}
