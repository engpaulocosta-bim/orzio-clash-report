using System;
using System.Collections.Generic;

namespace OrzioClashReport.Core.Model
{
    /// <summary>
    /// An auditable, candidate evaluation of whether a previous and a current <see cref="ClashOccurrence"/>
    /// represent the same clash: a <see cref="Confidence"/> level plus the ordered <see cref="MatchEvidence"/>
    /// that informed it. This type records a judgment call already made elsewhere -- it does not compute
    /// confidence from the evidence, does not require the evidence to agree with each other or with the
    /// confidence, and does not know which <see cref="CoordinationRun"/>s the occurrences came from.
    /// </summary>
    public sealed class ClashMatchAssessment
    {
        public ClashOccurrence PreviousOccurrence { get; }
        public ClashOccurrence CurrentOccurrence { get; }
        public ClashMatchConfidence Confidence { get; }
        public IReadOnlyList<MatchEvidence> Evidence { get; }

        public ClashMatchAssessment(
            ClashOccurrence previousOccurrence,
            ClashOccurrence currentOccurrence,
            ClashMatchConfidence confidence,
            IReadOnlyList<MatchEvidence> evidence)
        {
            PreviousOccurrence = previousOccurrence ?? throw new ArgumentNullException(nameof(previousOccurrence));
            CurrentOccurrence = currentOccurrence ?? throw new ArgumentNullException(nameof(currentOccurrence));

            if (!Enum.IsDefined(typeof(ClashMatchConfidence), confidence))
            {
                throw new ArgumentException($"'{confidence}' is not a defined {nameof(ClashMatchConfidence)}.", nameof(confidence));
            }

            Confidence = confidence;

            if (evidence == null)
            {
                throw new ArgumentNullException(nameof(evidence));
            }

            var copy = new List<MatchEvidence>(evidence);

            if (copy.Count == 0)
            {
                throw new ArgumentException("A match assessment must carry at least one piece of evidence.", nameof(evidence));
            }

            for (int i = 0; i < copy.Count; i++)
            {
                if (copy[i] == null)
                {
                    throw new ArgumentException($"Evidence at index {i} cannot be null.", nameof(evidence));
                }
            }

            Evidence = copy.AsReadOnly();
        }

        /// <summary>Human-readable representation for logs and test output, e.g. "Medium (2 evidence): {previous} -> {current}". Not a persistence key.</summary>
        public override string ToString() =>
            $"{Confidence} ({Evidence.Count} evidence): {PreviousOccurrence} -> {CurrentOccurrence}";
    }
}
