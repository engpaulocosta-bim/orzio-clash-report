using System;
using System.Collections.Generic;

namespace OrzioClashReport.Core.Model
{
    /// <summary>
    /// Immutable presentation of one <see cref="SelectedMatchContinuityPath"/>: the path itself, plus the
    /// complete ordered presentation of the lifecycle entry behind each of its selected matches. This carries no
    /// path id, track id, status, or aggregated confidence of its own.
    /// </summary>
    public sealed class ClashRunSequenceContinuityPathPresentation
    {
        public SelectedMatchContinuityPath ContinuityPath { get; }
        public IReadOnlyList<ClashRunSequenceLifecycleEntryPresentation> SelectedMatchEntries { get; }

        internal ClashRunSequenceContinuityPathPresentation(
            SelectedMatchContinuityPath continuityPath,
            IReadOnlyList<ClashRunSequenceLifecycleEntryPresentation> selectedMatchEntries)
        {
            ContinuityPath = continuityPath ?? throw new ArgumentNullException(nameof(continuityPath));

            if (selectedMatchEntries == null)
            {
                throw new ArgumentNullException(nameof(selectedMatchEntries));
            }

            var entriesCopy = new List<ClashRunSequenceLifecycleEntryPresentation>(selectedMatchEntries);

            if (entriesCopy.Count != continuityPath.SelectedMatches.Count)
            {
                throw new ArgumentException(
                    $"SelectedMatchEntries must contain exactly {continuityPath.SelectedMatches.Count} item(s), "
                    + $"one per ContinuityPath.SelectedMatches entry, but found {entriesCopy.Count}.",
                    nameof(selectedMatchEntries));
            }

            for (int i = 0; i < entriesCopy.Count; i++)
            {
                var item = entriesCopy[i];
                if (item == null)
                {
                    throw new ArgumentException($"SelectedMatchEntries item at index {i} cannot be null.", nameof(selectedMatchEntries));
                }

                if (!ReferenceEquals(item.ContinuityPath, continuityPath))
                {
                    throw new ArgumentException(
                        $"SelectedMatchEntries item at index {i} must reference the exact same ContinuityPath instance.",
                        nameof(selectedMatchEntries));
                }

                if (item.LifecycleEntry.SelectedMatch == null
                    || !ReferenceEquals(item.LifecycleEntry.SelectedMatch, continuityPath.SelectedMatches[i]))
                {
                    throw new ArgumentException(
                        $"SelectedMatchEntries item at index {i} must correspond to the exact "
                        + $"ContinuityPath.SelectedMatches[{i}] reference.",
                        nameof(selectedMatchEntries));
                }

                int expectedComparisonIndex = continuityPath.StartComparisonIndex + i;
                if (item.ComparisonIndex != expectedComparisonIndex)
                {
                    throw new ArgumentException(
                        $"SelectedMatchEntries item at index {i} must have ComparisonIndex {expectedComparisonIndex} "
                        + $"(StartComparisonIndex + {i}), but found {item.ComparisonIndex}.",
                        nameof(selectedMatchEntries));
                }
            }

            SelectedMatchEntries = entriesCopy.AsReadOnly();
        }

        /// <summary>Human-readable representation for logs and test output. Not a persistence key.</summary>
        public override string ToString() =>
            $"{ContinuityPath}: {SelectedMatchEntries.Count} selected-match entry presentation(s)";
    }
}
