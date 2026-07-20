using System;
using System.Collections.Generic;
using OrzioClashReport.Core.Abstractions;
using OrzioClashReport.Core.Model;

namespace OrzioClashReport.Core.Analysis
{
    /// <summary>
    /// Deterministically composes the three existing Core sequence-analysis stages without adding matching,
    /// lifecycle, continuity, path, persistence, or identity logic of its own.
    /// </summary>
    public sealed class DeterministicClashRunSequenceAnalyzer : IClashRunSequenceAnalyzer
    {
        private readonly IClashRunSequenceComparer _sequenceComparer;
        private readonly IClashRunSequenceContinuityProjector _continuityProjector;
        private readonly IClashRunSequenceContinuityPathAssembler _continuityPathAssembler;

        public DeterministicClashRunSequenceAnalyzer(
            IClashRunSequenceComparer sequenceComparer,
            IClashRunSequenceContinuityProjector continuityProjector,
            IClashRunSequenceContinuityPathAssembler continuityPathAssembler)
        {
            _sequenceComparer = sequenceComparer ?? throw new ArgumentNullException(nameof(sequenceComparer));
            _continuityProjector = continuityProjector ?? throw new ArgumentNullException(nameof(continuityProjector));
            _continuityPathAssembler = continuityPathAssembler ?? throw new ArgumentNullException(nameof(continuityPathAssembler));
        }

        public ClashRunSequenceAnalysisResult Analyze(IReadOnlyList<CoordinationRun> runs)
        {
            if (runs == null)
            {
                throw new ArgumentNullException(nameof(runs));
            }

            var sequenceComparison = _sequenceComparer.Compare(runs);
            var continuityResult = _continuityProjector.Project(sequenceComparison);
            var continuityPathsResult = _continuityPathAssembler.Assemble(continuityResult);

            return new ClashRunSequenceAnalysisResult(sequenceComparison, continuityResult, continuityPathsResult);
        }
    }
}
