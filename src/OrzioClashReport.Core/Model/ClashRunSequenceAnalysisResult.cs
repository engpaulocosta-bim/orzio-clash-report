using System;

namespace OrzioClashReport.Core.Model
{
    /// <summary>
    /// Immutable aggregate of one coherent, fully derived longitudinal sequence-analysis chain.
    /// </summary>
    public sealed class ClashRunSequenceAnalysisResult
    {
        public ClashRunSequenceComparisonResult SequenceComparison { get; }
        public ClashRunSequenceContinuityResult ContinuityResult { get; }
        public ClashRunSequenceContinuityPathsResult ContinuityPathsResult { get; }

        internal ClashRunSequenceAnalysisResult(
            ClashRunSequenceComparisonResult sequenceComparison,
            ClashRunSequenceContinuityResult continuityResult,
            ClashRunSequenceContinuityPathsResult continuityPathsResult)
        {
            SequenceComparison = sequenceComparison ?? throw new ArgumentNullException(nameof(sequenceComparison));
            ContinuityResult = continuityResult ?? throw new ArgumentNullException(nameof(continuityResult));
            ContinuityPathsResult = continuityPathsResult ?? throw new ArgumentNullException(nameof(continuityPathsResult));

            if (!ReferenceEquals(continuityResult.SequenceComparison, sequenceComparison))
            {
                throw new ArgumentException(
                    "ContinuityResult must reference the exact supplied SequenceComparison instance.",
                    nameof(continuityResult));
            }

            if (!ReferenceEquals(continuityPathsResult.ContinuityResult, continuityResult))
            {
                throw new ArgumentException(
                    "ContinuityPathsResult must reference the exact supplied ContinuityResult instance.",
                    nameof(continuityPathsResult));
            }
        }
    }
}
