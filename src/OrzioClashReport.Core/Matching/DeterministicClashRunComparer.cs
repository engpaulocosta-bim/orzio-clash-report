using System;
using System.Collections.Generic;
using System.Linq;
using OrzioClashReport.Core.Abstractions;
using OrzioClashReport.Core.Model;

namespace OrzioClashReport.Core.Matching
{
    /// <summary>
    /// A deterministic <see cref="IClashRunComparer"/>: evaluates every previous/current occurrence pair
    /// through an injected <see cref="IClashMatcher"/>, preserves every candidate produced, and greedily
    /// selects a one-to-one subset by confidence precedence (<see cref="ClashMatchConfidence.High"/> before
    /// <see cref="ClashMatchConfidence.Medium"/> before <see cref="ClashMatchConfidence.Low"/>), then by
    /// previous index, then by current index.
    ///
    /// <para>
    /// <b>MVP limitation, deliberately not fixed here.</b> This is an explicit greedy policy, not an optimal
    /// bipartite assignment: it does not compute maximum-cardinality or maximum-weight matching (e.g. the
    /// Hungarian algorithm). An early selection made purely by this precedence order can block two later
    /// candidates that a globally optimal assignment could have paired instead. This is acceptable at this
    /// step because the policy is fully deterministic, the precedence is explicit and documented, every
    /// non-selected candidate remains available in <see cref="ClashRunMatchResult.AlternativeCandidates"/>, and
    /// no lifecycle classification is produced from the result.
    /// </para>
    /// </summary>
    public sealed class DeterministicClashRunComparer : IClashRunComparer
    {
        private readonly IClashMatcher _matcher;

        public DeterministicClashRunComparer(IClashMatcher matcher)
        {
            _matcher = matcher ?? throw new ArgumentNullException(nameof(matcher));
        }

        public ClashRunMatchResult Compare(CoordinationRun previousRun, CoordinationRun currentRun)
        {
            if (previousRun == null)
            {
                throw new ArgumentNullException(nameof(previousRun));
            }

            if (currentRun == null)
            {
                throw new ArgumentNullException(nameof(currentRun));
            }

            var candidates = GenerateCandidates(previousRun, currentRun);
            var selected = SelectOneToOne(candidates);
            var selectedByReference = new HashSet<ClashRunMatchCandidate>(selected);

            var alternatives = candidates.Where(c => !selectedByReference.Contains(c)).ToList();

            var usedPreviousIndexes = new HashSet<int>(selected.Select(c => c.PreviousIndex));
            var usedCurrentIndexes = new HashSet<int>(selected.Select(c => c.CurrentIndex));

            var unmatchedPrevious = new List<ClashOccurrence>();
            for (int previousIndex = 0; previousIndex < previousRun.Occurrences.Count; previousIndex++)
            {
                if (!usedPreviousIndexes.Contains(previousIndex))
                {
                    unmatchedPrevious.Add(previousRun.Occurrences[previousIndex]);
                }
            }

            var unmatchedCurrent = new List<ClashOccurrence>();
            for (int currentIndex = 0; currentIndex < currentRun.Occurrences.Count; currentIndex++)
            {
                if (!usedCurrentIndexes.Contains(currentIndex))
                {
                    unmatchedCurrent.Add(currentRun.Occurrences[currentIndex]);
                }
            }

            return new ClashRunMatchResult(
                previousRun, currentRun, candidates, selected, alternatives, unmatchedPrevious, unmatchedCurrent);
        }

        /// <summary>Calls the matcher for every previous-major/current-minor pair, in order, enforcing that a non-null result references the exact instances passed in.</summary>
        private List<ClashRunMatchCandidate> GenerateCandidates(CoordinationRun previousRun, CoordinationRun currentRun)
        {
            var candidates = new List<ClashRunMatchCandidate>();

            for (int previousIndex = 0; previousIndex < previousRun.Occurrences.Count; previousIndex++)
            {
                var previousOccurrence = previousRun.Occurrences[previousIndex];

                for (int currentIndex = 0; currentIndex < currentRun.Occurrences.Count; currentIndex++)
                {
                    var currentOccurrence = currentRun.Occurrences[currentIndex];

                    var assessment = _matcher.Assess(previousOccurrence, currentOccurrence);
                    if (assessment == null)
                    {
                        continue;
                    }

                    if (!ReferenceEquals(assessment.PreviousOccurrence, previousOccurrence))
                    {
                        throw new InvalidOperationException(
                            $"Matcher contract violation at previous index {previousIndex}, current index {currentIndex}: "
                            + "the returned assessment's PreviousOccurrence does not reference the exact previousOccurrence passed to Assess.");
                    }

                    if (!ReferenceEquals(assessment.CurrentOccurrence, currentOccurrence))
                    {
                        throw new InvalidOperationException(
                            $"Matcher contract violation at previous index {previousIndex}, current index {currentIndex}: "
                            + "the returned assessment's CurrentOccurrence does not reference the exact currentOccurrence passed to Assess.");
                    }

                    candidates.Add(new ClashRunMatchCandidate(previousIndex, currentIndex, assessment));
                }
            }

            return candidates;
        }

        /// <summary>Greedily selects a one-to-one subset of candidates ordered by confidence precedence, then previous index, then current index.</summary>
        private static List<ClashRunMatchCandidate> SelectOneToOne(List<ClashRunMatchCandidate> candidates)
        {
            var ordered = candidates
                .OrderBy(c => ConfidencePrecedence(c.Assessment.Confidence))
                .ThenBy(c => c.PreviousIndex)
                .ThenBy(c => c.CurrentIndex)
                .ToList();

            var usedPreviousIndexes = new HashSet<int>();
            var usedCurrentIndexes = new HashSet<int>();
            var selected = new List<ClashRunMatchCandidate>();

            foreach (var candidate in ordered)
            {
                if (usedPreviousIndexes.Contains(candidate.PreviousIndex) || usedCurrentIndexes.Contains(candidate.CurrentIndex))
                {
                    continue;
                }

                usedPreviousIndexes.Add(candidate.PreviousIndex);
                usedCurrentIndexes.Add(candidate.CurrentIndex);
                selected.Add(candidate);
            }

            return selected;
        }

        /// <summary>Internal selection precedence only -- not a public score. Callers must not depend on the enum's numeric order.</summary>
        private static int ConfidencePrecedence(ClashMatchConfidence confidence) => confidence switch
        {
            ClashMatchConfidence.High => 0,
            ClashMatchConfidence.Medium => 1,
            ClashMatchConfidence.Low => 2,
            _ => int.MaxValue
        };
    }
}
