using System;
using System.Collections.Generic;

namespace OrzioClashReport.Core.Model
{
    /// <summary>
    /// Immutable, lossless presentation projection of a complete <see cref="ClashRunSequenceAnalysisResult"/>:
    /// the ordered runs, adjacent comparisons, continuity links, and continuity paths preserved by exact
    /// reference from <see cref="AnalysisResult"/>, plus indexed presentation views over them -- every lifecycle
    /// entry, every continuity path's selected-match entries, the selected matches that never join a continuity
    /// link (standalone), and the lifecycle entries that fall outside any continuity path (non-path). This type
    /// adds no persistent clash identity, ledger, history, multi-run lifecycle, or `Reopened` status; it is
    /// derived presentation over an already-complete analysis chain, and it never recomputes matching, lifecycle
    /// classification, continuity projection, or continuity path assembly -- it only re-validates that the
    /// supplied presentation collections are structurally the complete, canonically ordered projection implied
    /// by <see cref="AnalysisResult"/>.
    /// </summary>
    public sealed class ClashRunSequencePresentationResult
    {
        public ClashRunSequenceAnalysisResult AnalysisResult { get; }

        public IReadOnlyList<CoordinationRun> Runs { get; }
        public IReadOnlyList<ClashLifecycleResult> Comparisons { get; }
        public IReadOnlyList<SelectedMatchContinuityLink> ContinuityLinks { get; }
        public IReadOnlyList<SelectedMatchContinuityPath> ContinuityPaths { get; }

        public IReadOnlyList<ClashRunSequenceTransitionPresentation> Transitions { get; }
        public IReadOnlyList<ClashRunSequenceLifecycleEntryPresentation> LifecycleEntries { get; }
        public IReadOnlyList<ClashRunSequenceContinuityPathPresentation> PathPresentations { get; }
        public IReadOnlyList<ClashRunSequenceLifecycleEntryPresentation> StandaloneSelectedMatches { get; }
        public IReadOnlyList<ClashRunSequenceLifecycleEntryPresentation> NonPathLifecycleEntries { get; }

        public int RunCount { get; }
        public int AdjacentComparisonCount { get; }
        public int SelectedMatchCount { get; }
        public int ContinuityLinkCount { get; }
        public int ContinuityPathCount { get; }
        public int StandaloneSelectedMatchCount { get; }
        public int LifecycleEntryCount { get; }
        public int NonPathLifecycleEntryCount { get; }
        public int StillOpenCount { get; }
        public int NewCount { get; }
        public int ResolvedCount { get; }
        public int UnverifiableCount { get; }

        internal ClashRunSequencePresentationResult(
            ClashRunSequenceAnalysisResult analysisResult,
            IReadOnlyList<ClashRunSequenceTransitionPresentation> transitions,
            IReadOnlyList<ClashRunSequenceLifecycleEntryPresentation> lifecycleEntries,
            IReadOnlyList<ClashRunSequenceContinuityPathPresentation> pathPresentations,
            IReadOnlyList<ClashRunSequenceLifecycleEntryPresentation> standaloneSelectedMatches,
            IReadOnlyList<ClashRunSequenceLifecycleEntryPresentation> nonPathLifecycleEntries)
        {
            AnalysisResult = analysisResult ?? throw new ArgumentNullException(nameof(analysisResult));

            if (transitions == null)
            {
                throw new ArgumentNullException(nameof(transitions));
            }

            if (lifecycleEntries == null)
            {
                throw new ArgumentNullException(nameof(lifecycleEntries));
            }

            if (pathPresentations == null)
            {
                throw new ArgumentNullException(nameof(pathPresentations));
            }

            if (standaloneSelectedMatches == null)
            {
                throw new ArgumentNullException(nameof(standaloneSelectedMatches));
            }

            if (nonPathLifecycleEntries == null)
            {
                throw new ArgumentNullException(nameof(nonPathLifecycleEntries));
            }

            Runs = analysisResult.SequenceComparison.Runs;
            Comparisons = analysisResult.SequenceComparison.Comparisons;
            ContinuityLinks = analysisResult.ContinuityResult.Links;
            ContinuityPaths = analysisResult.ContinuityPathsResult.Paths;

            var transitionsCopy = new List<ClashRunSequenceTransitionPresentation>(transitions);
            var lifecycleEntriesCopy = new List<ClashRunSequenceLifecycleEntryPresentation>(lifecycleEntries);
            var pathPresentationsCopy = new List<ClashRunSequenceContinuityPathPresentation>(pathPresentations);
            var standaloneCopy = new List<ClashRunSequenceLifecycleEntryPresentation>(standaloneSelectedMatches);
            var nonPathCopy = new List<ClashRunSequenceLifecycleEntryPresentation>(nonPathLifecycleEntries);

            ValidateTransitions(transitionsCopy, Comparisons);
            ValidateLifecycleEntriesMatchTransitions(lifecycleEntriesCopy, transitionsCopy);
            ValidatePathPresentations(pathPresentationsCopy, ContinuityPaths);
            ValidatePathMembership(lifecycleEntriesCopy, pathPresentationsCopy);
            ValidatePartition(lifecycleEntriesCopy, standaloneCopy, nonPathCopy);

            Transitions = transitionsCopy.AsReadOnly();
            LifecycleEntries = lifecycleEntriesCopy.AsReadOnly();
            PathPresentations = pathPresentationsCopy.AsReadOnly();
            StandaloneSelectedMatches = standaloneCopy.AsReadOnly();
            NonPathLifecycleEntries = nonPathCopy.AsReadOnly();

            RunCount = Runs.Count;
            AdjacentComparisonCount = Comparisons.Count;
            ContinuityLinkCount = ContinuityLinks.Count;
            ContinuityPathCount = ContinuityPaths.Count;
            LifecycleEntryCount = LifecycleEntries.Count;
            StandaloneSelectedMatchCount = StandaloneSelectedMatches.Count;
            NonPathLifecycleEntryCount = NonPathLifecycleEntries.Count;

            int selectedMatchCount = 0;
            int stillOpen = 0;
            int newCount = 0;
            int resolved = 0;
            int unverifiable = 0;

            foreach (var entry in LifecycleEntries)
            {
                if (entry.LifecycleEntry.SelectedMatch != null)
                {
                    selectedMatchCount++;
                }

                switch (entry.LifecycleEntry.Status)
                {
                    case ClashLifecycleStatus.StillOpen:
                        stillOpen++;
                        break;
                    case ClashLifecycleStatus.New:
                        newCount++;
                        break;
                    case ClashLifecycleStatus.Resolved:
                        resolved++;
                        break;
                    case ClashLifecycleStatus.Unverifiable:
                        unverifiable++;
                        break;
                }
            }

            SelectedMatchCount = selectedMatchCount;
            StillOpenCount = stillOpen;
            NewCount = newCount;
            ResolvedCount = resolved;
            UnverifiableCount = unverifiable;
        }

        private static void ValidateTransitions(
            List<ClashRunSequenceTransitionPresentation> transitions, IReadOnlyList<ClashLifecycleResult> comparisons)
        {
            if (transitions.Count != comparisons.Count)
            {
                throw new ArgumentException(
                    $"Transitions must contain exactly {comparisons.Count} item(s), one per Comparisons entry, "
                    + $"but found {transitions.Count}.",
                    nameof(transitions));
            }

            for (int i = 0; i < comparisons.Count; i++)
            {
                var transition = transitions[i];
                if (transition == null)
                {
                    throw new ArgumentException($"Transitions item at index {i} cannot be null.", nameof(transitions));
                }

                if (transition.ComparisonIndex != i)
                {
                    throw new ArgumentException(
                        $"Transitions item at index {i} must have ComparisonIndex {i}, but found {transition.ComparisonIndex}.",
                        nameof(transitions));
                }

                if (!ReferenceEquals(transition.Comparison, comparisons[i]))
                {
                    throw new ArgumentException(
                        $"Transitions item at index {i} must reference Comparisons[{i}] by exact object reference.",
                        nameof(transitions));
                }
            }
        }

        private static void ValidateLifecycleEntriesMatchTransitions(
            List<ClashRunSequenceLifecycleEntryPresentation> lifecycleEntries,
            List<ClashRunSequenceTransitionPresentation> transitions)
        {
            var expected = new List<ClashRunSequenceLifecycleEntryPresentation>();
            foreach (var transition in transitions)
            {
                expected.AddRange(transition.LifecycleEntries);
            }

            if (lifecycleEntries.Count != expected.Count)
            {
                throw new ArgumentException(
                    $"LifecycleEntries must contain exactly {expected.Count} item(s), the complete flattening of "
                    + $"Transitions in order, but found {lifecycleEntries.Count}.",
                    nameof(lifecycleEntries));
            }

            for (int i = 0; i < expected.Count; i++)
            {
                if (!ReferenceEquals(lifecycleEntries[i], expected[i]))
                {
                    throw new ArgumentException(
                        $"LifecycleEntries[{i}] must be the exact same object reference as the flattened "
                        + "Transitions entries, in canonical (ComparisonIndex ascending, then EntryIndex "
                        + "ascending) order.",
                        nameof(lifecycleEntries));
                }
            }
        }

        private static void ValidatePathPresentations(
            List<ClashRunSequenceContinuityPathPresentation> pathPresentations,
            IReadOnlyList<SelectedMatchContinuityPath> continuityPaths)
        {
            if (pathPresentations.Count != continuityPaths.Count)
            {
                throw new ArgumentException(
                    $"PathPresentations must contain exactly {continuityPaths.Count} item(s), one per "
                    + $"ContinuityPath, but found {pathPresentations.Count}.",
                    nameof(pathPresentations));
            }

            for (int i = 0; i < continuityPaths.Count; i++)
            {
                var item = pathPresentations[i];
                if (item == null)
                {
                    throw new ArgumentException($"PathPresentations item at index {i} cannot be null.", nameof(pathPresentations));
                }

                if (!ReferenceEquals(item.ContinuityPath, continuityPaths[i]))
                {
                    throw new ArgumentException(
                        $"PathPresentations item at index {i} must reference ContinuityPaths[{i}] by exact object reference.",
                        nameof(pathPresentations));
                }
            }
        }

        private static void ValidatePathMembership(
            List<ClashRunSequenceLifecycleEntryPresentation> lifecycleEntries,
            List<ClashRunSequenceContinuityPathPresentation> pathPresentations)
        {
            var lifecycleEntrySet = new HashSet<ClashRunSequenceLifecycleEntryPresentation>(lifecycleEntries);
            var pathEntrySet = new HashSet<ClashRunSequenceLifecycleEntryPresentation>();

            foreach (var pathPresentation in pathPresentations)
            {
                foreach (var entry in pathPresentation.SelectedMatchEntries)
                {
                    if (!lifecycleEntrySet.Contains(entry))
                    {
                        throw new ArgumentException(
                            "Every PathPresentations[*].SelectedMatchEntries item must be the exact same object "
                            + "reference as an item in LifecycleEntries.",
                            nameof(pathPresentations));
                    }

                    pathEntrySet.Add(entry);
                }
            }

            foreach (var entry in lifecycleEntries)
            {
                if (entry.IsInContinuityPath && !pathEntrySet.Contains(entry))
                {
                    throw new ArgumentException(
                        "Every LifecycleEntries item with a ContinuityPath must appear in that path's "
                        + "PathPresentations SelectedMatchEntries.",
                        nameof(lifecycleEntries));
                }
            }
        }

        private static void ValidatePartition(
            List<ClashRunSequenceLifecycleEntryPresentation> lifecycleEntries,
            List<ClashRunSequenceLifecycleEntryPresentation> standaloneSelectedMatches,
            List<ClashRunSequenceLifecycleEntryPresentation> nonPathLifecycleEntries)
        {
            var expectedNonPath = new List<ClashRunSequenceLifecycleEntryPresentation>();
            var expectedStandalone = new List<ClashRunSequenceLifecycleEntryPresentation>();

            foreach (var entry in lifecycleEntries)
            {
                if (entry == null)
                {
                    throw new ArgumentException("LifecycleEntries cannot contain a null item.", nameof(lifecycleEntries));
                }

                if (entry.IsInContinuityPath)
                {
                    continue;
                }

                expectedNonPath.Add(entry);

                if (entry.LifecycleEntry.SelectedMatch != null)
                {
                    expectedStandalone.Add(entry);
                }
            }

            ValidateExactSequence(nonPathLifecycleEntries, expectedNonPath, nameof(nonPathLifecycleEntries), "NonPathLifecycleEntries");
            ValidateExactSequence(standaloneSelectedMatches, expectedStandalone, nameof(standaloneSelectedMatches), "StandaloneSelectedMatches");
        }

        private static void ValidateExactSequence(
            List<ClashRunSequenceLifecycleEntryPresentation> actual,
            List<ClashRunSequenceLifecycleEntryPresentation> expected,
            string paramName,
            string label)
        {
            if (actual.Count != expected.Count)
            {
                throw new ArgumentException(
                    $"{label} must contain exactly {expected.Count} item(s), but found {actual.Count}.", paramName);
            }

            for (int i = 0; i < expected.Count; i++)
            {
                if (!ReferenceEquals(actual[i], expected[i]))
                {
                    throw new ArgumentException(
                        $"{label}[{i}] must be the exact same object reference, in the same order as LifecycleEntries.",
                        paramName);
                }
            }
        }

        /// <summary>Human-readable representation for logs and test output. Not a persistence key.</summary>
        public override string ToString() =>
            $"{RunCount} runs, {AdjacentComparisonCount} adjacent comparisons, {LifecycleEntryCount} lifecycle entries, "
            + $"{ContinuityPathCount} continuity path(s), {StandaloneSelectedMatchCount} standalone selected match(es)";
    }
}
