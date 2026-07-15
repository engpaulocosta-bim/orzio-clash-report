using System;

namespace OrzioClashReport.Core.Model
{
    /// <summary>
    /// Observes that a selected match entering an exact occurrence slot of a shared run, and a selected match
    /// leaving that same exact slot through the immediately following pairwise comparison, exist together. This
    /// is the smallest possible longitudinal observation: it asserts only that the two independently
    /// recalculated pairwise selections happen to pass through the same slot. It carries no identifier, no
    /// fingerprint, no status, and no aggregated confidence, and it does not assert that the underlying clash is
    /// the same clash, that it has stable or persistent identity, or that it belongs to any history or ledger.
    /// </summary>
    public sealed class SelectedMatchContinuityLink
    {
        public int IncomingComparisonIndex { get; }
        public int OutgoingComparisonIndex => IncomingComparisonIndex + 1;

        public int SharedRunIndex => IncomingComparisonIndex + 1;
        public int SharedOccurrenceIndex { get; }

        public CoordinationRun SharedRun { get; }
        public ClashOccurrence SharedOccurrence { get; }

        public ClashRunMatchCandidate IncomingSelectedMatch { get; }
        public ClashRunMatchCandidate OutgoingSelectedMatch { get; }

        internal SelectedMatchContinuityLink(
            int incomingComparisonIndex,
            int sharedOccurrenceIndex,
            CoordinationRun sharedRun,
            ClashOccurrence sharedOccurrence,
            ClashRunMatchCandidate incomingSelectedMatch,
            ClashRunMatchCandidate outgoingSelectedMatch)
        {
            if (incomingComparisonIndex < 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(incomingComparisonIndex), incomingComparisonIndex, "Incoming comparison index cannot be negative.");
            }

            if (sharedOccurrenceIndex < 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(sharedOccurrenceIndex), sharedOccurrenceIndex, "Shared occurrence index cannot be negative.");
            }

            SharedRun = sharedRun ?? throw new ArgumentNullException(nameof(sharedRun));
            SharedOccurrence = sharedOccurrence ?? throw new ArgumentNullException(nameof(sharedOccurrence));
            IncomingSelectedMatch = incomingSelectedMatch ?? throw new ArgumentNullException(nameof(incomingSelectedMatch));
            OutgoingSelectedMatch = outgoingSelectedMatch ?? throw new ArgumentNullException(nameof(outgoingSelectedMatch));

            if (incomingSelectedMatch.CurrentIndex != sharedOccurrenceIndex)
            {
                throw new ArgumentException(
                    $"Incoming selected match's CurrentIndex ({incomingSelectedMatch.CurrentIndex}) must equal "
                    + $"SharedOccurrenceIndex ({sharedOccurrenceIndex}).",
                    nameof(incomingSelectedMatch));
            }

            if (outgoingSelectedMatch.PreviousIndex != sharedOccurrenceIndex)
            {
                throw new ArgumentException(
                    $"Outgoing selected match's PreviousIndex ({outgoingSelectedMatch.PreviousIndex}) must equal "
                    + $"SharedOccurrenceIndex ({sharedOccurrenceIndex}).",
                    nameof(outgoingSelectedMatch));
            }

            if (!ReferenceEquals(incomingSelectedMatch.Assessment.CurrentOccurrence, sharedOccurrence))
            {
                throw new ArgumentException(
                    "Incoming selected match's Assessment.CurrentOccurrence must reference the exact SharedOccurrence instance.",
                    nameof(incomingSelectedMatch));
            }

            if (!ReferenceEquals(outgoingSelectedMatch.Assessment.PreviousOccurrence, sharedOccurrence))
            {
                throw new ArgumentException(
                    "Outgoing selected match's Assessment.PreviousOccurrence must reference the exact SharedOccurrence instance.",
                    nameof(outgoingSelectedMatch));
            }

            if (sharedOccurrenceIndex >= sharedRun.Occurrences.Count)
            {
                throw new ArgumentException(
                    $"SharedOccurrenceIndex ({sharedOccurrenceIndex}) is out of range for SharedRun's "
                    + $"{sharedRun.Occurrences.Count} occurrences.",
                    nameof(sharedOccurrenceIndex));
            }

            if (!ReferenceEquals(sharedRun.Occurrences[sharedOccurrenceIndex], sharedOccurrence))
            {
                throw new ArgumentException(
                    $"SharedOccurrence must be the exact object reference at SharedRun.Occurrences[{sharedOccurrenceIndex}].",
                    nameof(sharedOccurrence));
            }

            IncomingComparisonIndex = incomingComparisonIndex;
            SharedOccurrenceIndex = sharedOccurrenceIndex;
        }

        /// <summary>Human-readable representation for logs and test output, e.g. "comparisons[0->1] via run[1].occurrence[5]". Not a persistence key or clash identity.</summary>
        public override string ToString() =>
            $"comparisons[{IncomingComparisonIndex}->{OutgoingComparisonIndex}] via run[{SharedRunIndex}].occurrence[{SharedOccurrenceIndex}]";
    }
}
