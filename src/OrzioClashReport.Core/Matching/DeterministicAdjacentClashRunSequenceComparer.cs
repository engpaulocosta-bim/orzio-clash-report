using System;
using System.Collections.Generic;
using OrzioClashReport.Core.Abstractions;
using OrzioClashReport.Core.Model;

namespace OrzioClashReport.Core.Matching
{
    /// <summary>
    /// A deterministic <see cref="IClashRunSequenceComparer"/>: traverses an explicitly ordered sequence of
    /// <see cref="CoordinationRun"/>s and, for each adjacent pair <c>[i] -&gt; [i + 1]</c>, delegates to an
    /// injected <see cref="IClashRunComparer"/> followed by an injected <see cref="IClashLifecycleClassifier"/>.
    /// Each transition is computed independently: no candidate, selected match, confidence, or evidence is
    /// carried over from one transition into the next, and no non-adjacent pair (for example <c>[0] -&gt; [2]</c>)
    /// is ever evaluated.
    /// </summary>
    public sealed class DeterministicAdjacentClashRunSequenceComparer : IClashRunSequenceComparer
    {
        private readonly IClashRunComparer _runComparer;
        private readonly IClashLifecycleClassifier _lifecycleClassifier;

        public DeterministicAdjacentClashRunSequenceComparer(
            IClashRunComparer runComparer, IClashLifecycleClassifier lifecycleClassifier)
        {
            _runComparer = runComparer ?? throw new ArgumentNullException(nameof(runComparer));
            _lifecycleClassifier = lifecycleClassifier ?? throw new ArgumentNullException(nameof(lifecycleClassifier));
        }

        public ClashRunSequenceComparisonResult Compare(IReadOnlyList<CoordinationRun> runs)
        {
            if (runs == null)
            {
                throw new ArgumentNullException(nameof(runs));
            }

            var runsCopy = new List<CoordinationRun>(runs);
            if (runsCopy.Count < 2)
            {
                throw new ArgumentException("Runs must contain at least two entries to compare adjacent pairs.", nameof(runs));
            }

            for (int i = 0; i < runsCopy.Count; i++)
            {
                if (runsCopy[i] == null)
                {
                    throw new ArgumentException($"Run at index {i} cannot be null.", nameof(runs));
                }
            }

            var comparisons = new List<ClashLifecycleResult>(runsCopy.Count - 1);
            for (int i = 0; i < runsCopy.Count - 1; i++)
            {
                var previousRun = runsCopy[i];
                var currentRun = runsCopy[i + 1];

                var matchResult = _runComparer.Compare(previousRun, currentRun);
                var lifecycleResult = _lifecycleClassifier.Classify(matchResult);

                comparisons.Add(lifecycleResult);
            }

            return new ClashRunSequenceComparisonResult(runsCopy, comparisons);
        }
    }
}
