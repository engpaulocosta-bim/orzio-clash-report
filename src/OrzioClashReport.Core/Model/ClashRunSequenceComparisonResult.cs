using System;
using System.Collections.Generic;

namespace OrzioClashReport.Core.Model
{
    /// <summary>
    /// Immutable, auditable result of comparing an explicitly ordered sequence of <see cref="CoordinationRun"/>s
    /// by adjacent pairs only: the ordered runs themselves, plus one independently recalculated
    /// <see cref="ClashLifecycleResult"/> per adjacent transition, in the same order. For runs
    /// <c>[A, B, C]</c>, <see cref="Comparisons"/> is exactly <c>[lifecycle(A -&gt; B), lifecycle(B -&gt; C)]</c>.
    ///
    /// <para>
    /// This type represents only an ordered collection of independently recalculated adjacent pairwise
    /// lifecycle results. It does not represent history, a multi-run lifecycle, persistent clash tracking, a
    /// Clash Ledger, or stable/persistent clash identity: <see cref="Comparisons"/>[i] and
    /// <see cref="Comparisons"/>[i + 1] share no propagated match, confidence, or evidence, and this type never
    /// recomputes matching or lifecycle -- it only validates that the supplied comparisons structurally
    /// continue the supplied runs.
    /// </para>
    /// </summary>
    public sealed class ClashRunSequenceComparisonResult
    {
        public IReadOnlyList<CoordinationRun> Runs { get; }
        public IReadOnlyList<ClashLifecycleResult> Comparisons { get; }

        internal ClashRunSequenceComparisonResult(
            IReadOnlyList<CoordinationRun> runs,
            IReadOnlyList<ClashLifecycleResult> comparisons)
        {
            if (runs == null)
            {
                throw new ArgumentNullException(nameof(runs));
            }

            if (comparisons == null)
            {
                throw new ArgumentNullException(nameof(comparisons));
            }

            var runsCopy = new List<CoordinationRun>(runs);
            if (runsCopy.Count < 2)
            {
                throw new ArgumentException("Runs must contain at least two entries.", nameof(runs));
            }

            for (int i = 0; i < runsCopy.Count; i++)
            {
                if (runsCopy[i] == null)
                {
                    throw new ArgumentException($"Run at index {i} cannot be null.", nameof(runs));
                }
            }

            var comparisonsCopy = new List<ClashLifecycleResult>(comparisons);
            int expectedComparisonCount = runsCopy.Count - 1;
            if (comparisonsCopy.Count != expectedComparisonCount)
            {
                throw new ArgumentException(
                    $"Comparisons must contain exactly {expectedComparisonCount} entries (Runs.Count - 1), "
                    + $"but found {comparisonsCopy.Count}.",
                    nameof(comparisons));
            }

            for (int i = 0; i < comparisonsCopy.Count; i++)
            {
                var comparison = comparisonsCopy[i];
                if (comparison == null)
                {
                    throw new ArgumentException($"Comparison at index {i} cannot be null.", nameof(comparisons));
                }

                if (!ReferenceEquals(comparison.MatchResult.PreviousRun, runsCopy[i]))
                {
                    throw new ArgumentException(
                        $"Comparison at index {i} must reference Runs[{i}] as its previous side by exact "
                        + "object reference.",
                        nameof(comparisons));
                }

                if (!ReferenceEquals(comparison.MatchResult.CurrentRun, runsCopy[i + 1]))
                {
                    throw new ArgumentException(
                        $"Comparison at index {i} must reference Runs[{i + 1}] as its current side by exact "
                        + "object reference.",
                        nameof(comparisons));
                }
            }

            Runs = runsCopy.AsReadOnly();
            Comparisons = comparisonsCopy.AsReadOnly();
        }

        /// <summary>Human-readable representation for logs and test output, e.g. "3 runs, 2 adjacent comparisons". Not a persistence key.</summary>
        public override string ToString() => $"{Runs.Count} runs, {Comparisons.Count} adjacent comparisons";
    }
}
