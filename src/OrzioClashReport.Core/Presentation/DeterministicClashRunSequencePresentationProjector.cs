using System;
using System.Collections.Generic;
using OrzioClashReport.Core.Abstractions;
using OrzioClashReport.Core.Model;

namespace OrzioClashReport.Core.Presentation
{
    /// <summary>
    /// Deterministically projects a complete <see cref="ClashRunSequenceAnalysisResult"/> onto a lossless
    /// presentation view by indexing and relating exact references only -- no matching, lifecycle
    /// classification, continuity link, or continuity path is recomputed.
    /// </summary>
    public sealed class DeterministicClashRunSequencePresentationProjector : IClashRunSequencePresentationProjector
    {
        public ClashRunSequencePresentationResult Project(ClashRunSequenceAnalysisResult analysisResult)
        {
            if (analysisResult == null)
            {
                throw new ArgumentNullException(nameof(analysisResult));
            }

            var sequenceComparison = analysisResult.SequenceComparison;
            var continuityPaths = analysisResult.ContinuityPathsResult.Paths;

            var pathByCandidate = BuildPathByCandidateLookup(continuityPaths);

            var lifecycleEntries = new List<ClashRunSequenceLifecycleEntryPresentation>();
            var transitions = new List<ClashRunSequenceTransitionPresentation>();
            var entryByComparisonAndCandidate =
                new Dictionary<int, Dictionary<ClashRunMatchCandidate, ClashRunSequenceLifecycleEntryPresentation>>();

            for (int comparisonIndex = 0; comparisonIndex < sequenceComparison.Comparisons.Count; comparisonIndex++)
            {
                var comparison = sequenceComparison.Comparisons[comparisonIndex];
                var transitionEntries = new List<ClashRunSequenceLifecycleEntryPresentation>();
                var candidateLookup = new Dictionary<ClashRunMatchCandidate, ClashRunSequenceLifecycleEntryPresentation>();

                for (int entryIndex = 0; entryIndex < comparison.Entries.Count; entryIndex++)
                {
                    var lifecycleEntry = comparison.Entries[entryIndex];
                    SelectedMatchContinuityPath? continuityPath = null;

                    if (lifecycleEntry.SelectedMatch != null
                        && pathByCandidate.TryGetValue(lifecycleEntry.SelectedMatch, out var foundPath))
                    {
                        continuityPath = foundPath;
                    }

                    var presentation = new ClashRunSequenceLifecycleEntryPresentation(
                        comparisonIndex, entryIndex, comparison, lifecycleEntry, continuityPath);

                    lifecycleEntries.Add(presentation);
                    transitionEntries.Add(presentation);

                    if (lifecycleEntry.SelectedMatch != null)
                    {
                        candidateLookup[lifecycleEntry.SelectedMatch] = presentation;
                    }
                }

                transitions.Add(new ClashRunSequenceTransitionPresentation(comparisonIndex, comparison, transitionEntries));
                entryByComparisonAndCandidate[comparisonIndex] = candidateLookup;
            }

            var pathPresentations = new List<ClashRunSequenceContinuityPathPresentation>();
            foreach (var path in continuityPaths)
            {
                var selectedMatchEntries = new List<ClashRunSequenceLifecycleEntryPresentation>();

                for (int k = 0; k < path.SelectedMatches.Count; k++)
                {
                    int comparisonIndex = path.StartComparisonIndex + k;
                    var candidate = path.SelectedMatches[k];

                    if (!entryByComparisonAndCandidate.TryGetValue(comparisonIndex, out var candidateLookup)
                        || !candidateLookup.TryGetValue(candidate, out var presentation))
                    {
                        throw new InvalidOperationException(
                            $"Continuity path selected match at position {k} has no corresponding lifecycle "
                            + $"entry presentation at comparison index {comparisonIndex}.");
                    }

                    selectedMatchEntries.Add(presentation);
                }

                pathPresentations.Add(new ClashRunSequenceContinuityPathPresentation(path, selectedMatchEntries));
            }

            var standaloneSelectedMatches = new List<ClashRunSequenceLifecycleEntryPresentation>();
            var nonPathLifecycleEntries = new List<ClashRunSequenceLifecycleEntryPresentation>();

            foreach (var entry in lifecycleEntries)
            {
                if (entry.IsInContinuityPath)
                {
                    continue;
                }

                nonPathLifecycleEntries.Add(entry);

                if (entry.LifecycleEntry.SelectedMatch != null)
                {
                    standaloneSelectedMatches.Add(entry);
                }
            }

            return new ClashRunSequencePresentationResult(
                analysisResult,
                transitions,
                lifecycleEntries,
                pathPresentations,
                standaloneSelectedMatches,
                nonPathLifecycleEntries);
        }

        /// <summary>
        /// Maps each selected-match candidate that participates in a continuity path to its exact path, failing
        /// fast if the same exact selected-match reference is ever found in two different paths -- a structural
        /// impossibility given <see cref="ClashRunSequenceContinuityPathsResult"/>'s own disjoint-partition
        /// invariant, guarded here defensively rather than silently picking the first match.
        /// </summary>
        private static Dictionary<ClashRunMatchCandidate, SelectedMatchContinuityPath> BuildPathByCandidateLookup(
            IReadOnlyList<SelectedMatchContinuityPath> paths)
        {
            var lookup = new Dictionary<ClashRunMatchCandidate, SelectedMatchContinuityPath>();

            foreach (var path in paths)
            {
                foreach (var candidate in path.SelectedMatches)
                {
                    if (lookup.TryGetValue(candidate, out var existingPath) && !ReferenceEquals(existingPath, path))
                    {
                        throw new InvalidOperationException(
                            "The same exact selected-match reference appears in two different continuity paths, "
                            + "which is a structural impossibility.");
                    }

                    lookup[candidate] = path;
                }
            }

            return lookup;
        }
    }
}
