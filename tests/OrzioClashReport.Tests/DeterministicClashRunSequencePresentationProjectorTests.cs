using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using OrzioClashReport.Core.Abstractions;
using OrzioClashReport.Core.Analysis;
using OrzioClashReport.Core.Continuity;
using OrzioClashReport.Core.Lifecycle;
using OrzioClashReport.Core.Matching;
using OrzioClashReport.Core.Model;
using OrzioClashReport.Core.Presentation;

namespace OrzioClashReport.Tests
{
    /// <summary>
    /// Tests for <see cref="DeterministicClashRunSequencePresentationProjector"/>: lossless indexing of an
    /// already-complete <see cref="ClashRunSequenceAnalysisResult"/> into runs/comparisons/entries/links/paths
    /// presentation, path-versus-standalone partitioning, complete lifecycle-entry preservation, canonical
    /// order, determinism, and purity. Fixtures are built through the real matcher/run-comparer/lifecycle-
    /// classifier/sequence-comparer/continuity-projector/path-assembler pipeline, never hand-built.
    /// </summary>
    public class DeterministicClashRunSequencePresentationProjectorTests
    {
        private static readonly ConstructorInfo PathConstructor =
            typeof(SelectedMatchContinuityPath).GetConstructor(
                BindingFlags.Instance | BindingFlags.NonPublic,
                binder: null,
                types: new[] { typeof(IReadOnlyList<SelectedMatchContinuityLink>) },
                modifiers: null)
            ?? throw new InvalidOperationException("Could not find SelectedMatchContinuityPath internal constructor.");

        private static readonly DateTimeOffset DefaultCreatedAt = new DateTimeOffset(2026, 7, 10, 9, 0, 0, TimeSpan.Zero);

        private static readonly ModelRevision Sigma =
            new ModelRevision(new ModelIdentity("Sigma", "Structure", "Main"), "R04", "Sigma_Main_R04.nwc", null, null, null);

        private static readonly ExecutedClashTest Test1ForSigma = new ExecutedClashTest("Test 1", Sigma.Identity, Sigma.Identity);

        private static ClashObject MakeObject(string elementId) => new ClashObject(elementId, null, null, null, null, null);

        private static ClashOccurrence MakeOccurrence(string tag) =>
            new ClashOccurrence(
                "Test 1",
                new ClashResult("Test 1", ClashStatus.New, null, null, null, MakeObject($"{tag}-a"), MakeObject($"{tag}-b"), null),
                Sigma, Sigma);

        private static RunManifest MakeManifest(string runId, DateTimeOffset createdAt) =>
            new RunManifest(runId, createdAt, new[] { Sigma }, new[] { Test1ForSigma });

        private static CoordinationRun MakeRun(string runId, DateTimeOffset createdAt, params ClashOccurrence[] occurrences) =>
            new CoordinationRun(MakeManifest(runId, createdAt), occurrences);

        private static CoordinationRun MakeRun(string runId, params ClashOccurrence[] occurrences) =>
            MakeRun(runId, DefaultCreatedAt, occurrences);

        private static DeterministicClashRunSequenceAnalyzer CreateAnalyzer() =>
            new DeterministicClashRunSequenceAnalyzer(
                new DeterministicAdjacentClashRunSequenceComparer(
                    new DeterministicClashRunComparer(new ConservativeClashMatcher()),
                    new ConservativeClashLifecycleClassifier()),
                new DeterministicSelectedMatchContinuityProjector(),
                new DeterministicSelectedMatchContinuityPathAssembler());

        private static DeterministicClashRunSequencePresentationProjector CreateProjector() =>
            new DeterministicClashRunSequencePresentationProjector();

        private static bool MatchesTag(ClashOccurrence? occurrence, string tag) =>
            occurrence != null && occurrence.Clash.ElementA.ElementId == $"{tag}-a";

        private static ClashRunSequenceLifecycleEntryPresentation FindEntryForTag(
            ClashRunSequencePresentationResult result, int comparisonIndex, string tag) =>
            result.Transitions[comparisonIndex].LifecycleEntries.Single(e =>
                MatchesTag(e.LifecycleEntry.PreviousOccurrence, tag) || MatchesTag(e.LifecycleEntry.CurrentOccurrence, tag));

        // ===================== fixtures =====================

        /// <summary>Two runs sharing one occurrence: one comparison, one StillOpen selected match, zero links, zero paths.</summary>
        private static ClashRunSequenceAnalysisResult CreateTwoRunFixture()
        {
            var occ = MakeOccurrence("shared");
            return CreateAnalyzer().Analyze(new[] { MakeRun("run-A", occ), MakeRun("run-B", occ) });
        }

        /// <summary>Four-run chain (A,B,C,D) sharing one occurrence: three comparisons, two links, one three-entry continuous path.</summary>
        private static ClashRunSequenceAnalysisResult CreateFourRunChainFixture()
        {
            var occ = MakeOccurrence("shared");
            return CreateAnalyzer().Analyze(new[]
            {
                MakeRun("run-A", occ), MakeRun("run-B", occ), MakeRun("run-C", occ), MakeRun("run-D", occ),
            });
        }

        /// <summary>Three runs sharing two independent occurrences: one boundary, two links, two disjoint one-link paths.</summary>
        private static ClashRunSequenceAnalysisResult CreateTwoDisconnectedPathsFixture()
        {
            var occ0 = MakeOccurrence("slot0");
            var occ1 = MakeOccurrence("slot1");
            var runA = MakeRun("run-A", occ0, occ1);
            var runB = MakeRun("run-B", occ0, occ1);
            var runC = MakeRun("run-C", occ0, occ1);
            return CreateAnalyzer().Analyze(new[] { runA, runB, runC });
        }

        /// <summary>
        /// Three runs (A,B,C): A-B share "shared" (StillOpen, no continuity link since B-C never re-match it);
        /// B-C leaves "shared" unmatched-previous (Resolved) and "unrelated-c" unmatched-current (New).
        /// </summary>
        private static ClashRunSequenceAnalysisResult CreateStandaloneAndUnmatchedFixture()
        {
            var occ = MakeOccurrence("shared");
            var unrelatedForC = MakeOccurrence("unrelated-c");
            var runA = MakeRun("run-A", occ);
            var runB = MakeRun("run-B", occ);
            var runC = MakeRun("run-C", unrelatedForC);
            return CreateAnalyzer().Analyze(new[] { runA, runB, runC });
        }

        /// <summary>
        /// Four-run mixture: "path" is shared A,B,C,D (one continuous path across all three comparisons);
        /// "standalone" is shared only A,B (StillOpen selected match with no continuity link); "resolved-only" is
        /// only in A (Resolved in comparison0); "new-only" is only in D (New in comparison2).
        /// </summary>
        private static ClashRunSequenceAnalysisResult CreateMixedFixture()
        {
            var path = MakeOccurrence("path");
            var standalone = MakeOccurrence("standalone");
            var resolvedOnly = MakeOccurrence("resolved-only");
            var newOnly = MakeOccurrence("new-only");

            var runA = MakeRun("run-A", path, standalone, resolvedOnly);
            var runB = MakeRun("run-B", path, standalone);
            var runC = MakeRun("run-C", path);
            var runD = MakeRun("run-D", path, newOnly);

            return CreateAnalyzer().Analyze(new[] { runA, runB, runC, runD });
        }

        /// <summary>
        /// Two runs (A-dup, B): A declares "amb" twice (ambiguous incoming), so the selected match at
        /// (previous 0, current 0) is classified Unverifiable by the real classifier because an alternative
        /// candidate competes for the same CurrentIndex.
        /// </summary>
        private static ClashRunSequenceAnalysisResult CreateUnverifiableFixture()
        {
            var amb = MakeOccurrence("amb");
            var runADuplicate = MakeRun("run-A-dup", amb, amb);
            var runB = MakeRun("run-B", amb);
            return CreateAnalyzer().Analyze(new[] { runADuplicate, runB });
        }

        // ===================== 1. null input =====================

        [Fact]
        public void Project_RejectsNullAnalysisResult()
        {
            Assert.Throws<ArgumentNullException>(() => CreateProjector().Project(null!));
        }

        // ===================== 2. two runs, zero links/paths =====================

        [Fact]
        public void Project_TwoRunsOneComparison_ZeroLinksZeroPaths()
        {
            var analysisResult = CreateTwoRunFixture();

            var result = CreateProjector().Project(analysisResult);

            Assert.Equal(2, result.RunCount);
            Assert.Single(result.Comparisons);
            Assert.Single(result.Transitions);
            Assert.Empty(result.ContinuityLinks);
            Assert.Empty(result.ContinuityPaths);
            Assert.Empty(result.PathPresentations);
            Assert.Single(result.LifecycleEntries);
            Assert.Single(result.StandaloneSelectedMatches);
            Assert.Single(result.NonPathLifecycleEntries);
        }

        // ===================== 3. four-run continuous chain =====================

        [Fact]
        public void Project_FourRunContinuousChain_ProducesOnePathCoveringAllComparisons()
        {
            var analysisResult = CreateFourRunChainFixture();

            var result = CreateProjector().Project(analysisResult);

            Assert.Equal(4, result.RunCount);
            Assert.Equal(3, result.AdjacentComparisonCount);
            Assert.Equal(2, result.ContinuityLinkCount);
            Assert.Equal(1, result.ContinuityPathCount);

            var pathPresentation = Assert.Single(result.PathPresentations);
            Assert.Equal(3, pathPresentation.SelectedMatchEntries.Count);
            Assert.Equal(0, pathPresentation.SelectedMatchEntries[0].ComparisonIndex);
            Assert.Equal(1, pathPresentation.SelectedMatchEntries[1].ComparisonIndex);
            Assert.Equal(2, pathPresentation.SelectedMatchEntries[2].ComparisonIndex);

            Assert.Empty(result.StandaloneSelectedMatches);
            Assert.Empty(result.NonPathLifecycleEntries);
        }

        // ===================== 4. two disconnected paths, canonical order =====================

        [Fact]
        public void Project_TwoDisconnectedPaths_PreservedInCanonicalOrder()
        {
            var analysisResult = CreateTwoDisconnectedPathsFixture();

            var result = CreateProjector().Project(analysisResult);

            Assert.Equal(2, result.ContinuityPathCount);
            Assert.Equal(2, result.PathPresentations.Count);

            for (int i = 0; i < result.ContinuityPaths.Count; i++)
            {
                Assert.Same(result.ContinuityPaths[i], result.PathPresentations[i].ContinuityPath);
            }

            Assert.Empty(result.StandaloneSelectedMatches);
            Assert.Empty(result.NonPathLifecycleEntries);
        }

        // ===================== 5. isolated selected match, no continuity link =====================

        [Fact]
        public void Project_IsolatedSelectedMatchWithoutContinuityLink_IsStandalone()
        {
            var analysisResult = CreateStandaloneAndUnmatchedFixture();
            Assert.Empty(analysisResult.ContinuityResult.Links);
            Assert.Empty(analysisResult.ContinuityPathsResult.Paths);

            var result = CreateProjector().Project(analysisResult);

            var sharedEntry = FindEntryForTag(result, 0, "shared");
            Assert.True(sharedEntry.LifecycleEntry.SelectedMatch != null);
            Assert.Null(sharedEntry.ContinuityPath);
            Assert.False(sharedEntry.IsInContinuityPath);
            Assert.Contains(sharedEntry, result.StandaloneSelectedMatches);
        }

        // ===================== 6. mixture: path + standalone + unmatched =====================

        [Fact]
        public void Project_MixtureOfPathStandaloneAndUnmatchedEntries_ArePartitionedCorrectly()
        {
            var result = CreateProjector().Project(CreateMixedFixture());

            Assert.Equal(1, result.ContinuityPathCount);
            var path = result.ContinuityPaths[0];
            Assert.Equal(3, path.SelectedMatches.Count);

            var pathEntryComparison0 = FindEntryForTag(result, 0, "path");
            var pathEntryComparison1 = FindEntryForTag(result, 1, "path");
            var pathEntryComparison2 = FindEntryForTag(result, 2, "path");
            Assert.True(pathEntryComparison0.IsInContinuityPath);
            Assert.True(pathEntryComparison1.IsInContinuityPath);
            Assert.True(pathEntryComparison2.IsInContinuityPath);
            Assert.Same(path, pathEntryComparison0.ContinuityPath);
            Assert.Same(path, pathEntryComparison1.ContinuityPath);
            Assert.Same(path, pathEntryComparison2.ContinuityPath);

            var standaloneEntry = FindEntryForTag(result, 0, "standalone");
            Assert.False(standaloneEntry.IsInContinuityPath);
            Assert.Contains(standaloneEntry, result.StandaloneSelectedMatches);

            var resolvedEntry = FindEntryForTag(result, 0, "resolved-only");
            Assert.Equal(ClashLifecycleStatus.Resolved, resolvedEntry.LifecycleEntry.Status);
            Assert.False(resolvedEntry.IsInContinuityPath);
            Assert.Contains(resolvedEntry, result.NonPathLifecycleEntries);
            Assert.DoesNotContain(resolvedEntry, result.StandaloneSelectedMatches);

            var newEntry = FindEntryForTag(result, 2, "new-only");
            Assert.Equal(ClashLifecycleStatus.New, newEntry.LifecycleEntry.Status);
            Assert.False(newEntry.IsInContinuityPath);
            Assert.Contains(newEntry, result.NonPathLifecycleEntries);
        }

        // ===================== 7. New, Resolved and Unverifiable never disappear =====================

        [Fact]
        public void Project_NewAndResolvedEntries_NeverDisappear()
        {
            var result = CreateProjector().Project(CreateMixedFixture());

            var resolvedEntry = FindEntryForTag(result, 0, "resolved-only");
            var newEntry = FindEntryForTag(result, 2, "new-only");

            Assert.Contains(resolvedEntry, result.LifecycleEntries);
            Assert.Contains(newEntry, result.LifecycleEntries);
            // "resolved-only" (comparison0) and "standalone" (unmatched-previous in comparison1, since it never
            // reappears in run-C) both resolve; "new-only" is the only unmatched-current entry.
            Assert.Equal(2, result.ResolvedCount);
            Assert.Equal(1, result.NewCount);
        }

        [Fact]
        public void Project_UnverifiableEntry_NeverDisappearsAndIsNonPath()
        {
            var analysisResult = CreateUnverifiableFixture();
            // The duplicate previous slot produces two Unverifiable entries at comparison0: the selected match
            // (competing alternative shares its CurrentIndex) and the losing unmatched-previous slot itself.
            // This assertion targets the selected-match one specifically.
            var unverifiableEntry = analysisResult.SequenceComparison.Comparisons[0].Entries
                .Single(e => e.Status == ClashLifecycleStatus.Unverifiable && e.SelectedMatch != null);

            var result = CreateProjector().Project(analysisResult);

            var presentation = result.LifecycleEntries.Single(e => ReferenceEquals(e.LifecycleEntry, unverifiableEntry));
            Assert.Contains(presentation, result.LifecycleEntries);
            // Both the selected-match entry and the losing unmatched-previous slot are Unverifiable.
            Assert.Equal(2, result.UnverifiableCount);
            Assert.False(presentation.IsInContinuityPath);
            Assert.Contains(presentation, result.NonPathLifecycleEntries);
        }

        // ===================== 8. path selected matches point to exact lifecycle entry =====================

        [Fact]
        public void Project_PathSelectedMatchEntries_PointToExactLifecycleEntry()
        {
            var analysisResult = CreateFourRunChainFixture();
            var result = CreateProjector().Project(analysisResult);

            var path = analysisResult.ContinuityPathsResult.Paths[0];
            var pathPresentation = result.PathPresentations[0];

            for (int i = 0; i < path.SelectedMatches.Count; i++)
            {
                Assert.Same(path.SelectedMatches[i], pathPresentation.SelectedMatchEntries[i].LifecycleEntry.SelectedMatch);
                Assert.Same(pathPresentation.ContinuityPath, pathPresentation.SelectedMatchEntries[i].ContinuityPath);
            }
        }

        [Fact]
        public void Project_SameExactSelectedMatchInTwoDifferentPaths_FailsFast()
        {
            var analysisResult = CreateFourRunChainFixture();
            var firstPath = Assert.Single(analysisResult.ContinuityPathsResult.Paths);
            var secondPath = CreatePathViaReflection(firstPath.Links);

            Assert.NotSame(firstPath, secondPath);
            Assert.Same(firstPath.SelectedMatches[0], secondPath.SelectedMatches[0]);

            CorruptContinuityPathsResultPathsField(
                analysisResult.ContinuityPathsResult,
                new List<SelectedMatchContinuityPath> { firstPath, secondPath }.AsReadOnly());

            var exception = Assert.Throws<InvalidOperationException>(() => CreateProjector().Project(analysisResult));
            Assert.Contains("same exact selected-match reference", exception.Message, StringComparison.Ordinal);
            Assert.Contains("different continuity paths", exception.Message, StringComparison.Ordinal);
        }

        // ===================== 9. standalone / non-path reuse the same global reference =====================

        [Fact]
        public void Project_StandaloneAndNonPath_ReuseSameGlobalPresentationReference()
        {
            var result = CreateProjector().Project(CreateStandaloneAndUnmatchedFixture());

            var standalone = Assert.Single(result.StandaloneSelectedMatches);
            var globalMatch = Assert.Single(result.LifecycleEntries, e => ReferenceEquals(e, standalone));
            Assert.Same(globalMatch, standalone);
            Assert.Contains(standalone, result.NonPathLifecycleEntries);

            var nonPathIndex = result.NonPathLifecycleEntries.ToList().IndexOf(standalone);
            Assert.Same(result.NonPathLifecycleEntries[nonPathIndex], result.StandaloneSelectedMatches[0]);
        }

        // ===================== 10. alternative candidates stay out of selection/paths =====================

        [Fact]
        public void Project_AlternativeCandidates_RemainAccessibleAndNeverBecomeSelectedOrPath()
        {
            var amb = MakeOccurrence("amb");
            var runADuplicate = MakeRun("run-A-dup", amb, amb);
            var runB = MakeRun("run-B", amb);
            var runC = MakeRun("run-C", amb);
            var analysisResult = CreateAnalyzer().Analyze(new[] { runADuplicate, runB, runC });

            var alternatives = analysisResult.SequenceComparison.Comparisons[0].MatchResult.AlternativeCandidates;
            Assert.Single(alternatives);

            var result = CreateProjector().Project(analysisResult);

            Assert.Same(alternatives, result.Comparisons[0].MatchResult.AlternativeCandidates);

            foreach (var entry in result.LifecycleEntries)
            {
                if (entry.LifecycleEntry.SelectedMatch == null)
                {
                    continue;
                }

                Assert.DoesNotContain(alternatives, c => ReferenceEquals(c, entry.LifecycleEntry.SelectedMatch));
            }

            foreach (var path in result.ContinuityPaths)
            {
                foreach (var candidate in path.SelectedMatches)
                {
                    Assert.DoesNotContain(alternatives, c => ReferenceEquals(c, candidate));
                }
            }
        }

        // ===================== 11. determinism =====================

        [Fact]
        public void Project_RepeatedCalls_ProduceStructurallyEquivalentResult()
        {
            var analysisResult = CreateMixedFixture();
            var projector = CreateProjector();

            var first = projector.Project(analysisResult);
            var second = projector.Project(analysisResult);

            Assert.Equal(first.LifecycleEntryCount, second.LifecycleEntryCount);
            Assert.Equal(first.ContinuityPathCount, second.ContinuityPathCount);
            Assert.Equal(first.StandaloneSelectedMatchCount, second.StandaloneSelectedMatchCount);
            Assert.Equal(first.NonPathLifecycleEntryCount, second.NonPathLifecycleEntryCount);

            for (int i = 0; i < first.LifecycleEntries.Count; i++)
            {
                Assert.Same(first.LifecycleEntries[i].LifecycleEntry, second.LifecycleEntries[i].LifecycleEntry);
                Assert.Equal(first.LifecycleEntries[i].IsInContinuityPath, second.LifecycleEntries[i].IsInContinuityPath);
            }
        }

        // ===================== 12. no dependency, I/O, or mutation =====================

        [Fact]
        public void Projector_HasParameterlessConstructorAndNoDependencies()
        {
            var constructor = Assert.Single(typeof(DeterministicClashRunSequencePresentationProjector).GetConstructors());
            Assert.Empty(constructor.GetParameters());
        }

        [Fact]
        public void Project_DoesNotMutateAnalysisResultCollections()
        {
            var analysisResult = CreateMixedFixture();
            var runsBefore = analysisResult.SequenceComparison.Runs.ToList();
            var comparisonsBefore = analysisResult.SequenceComparison.Comparisons.ToList();
            var linksBefore = analysisResult.ContinuityResult.Links.ToList();
            var pathsBefore = analysisResult.ContinuityPathsResult.Paths.ToList();

            CreateProjector().Project(analysisResult);

            Assert.Equal(runsBefore, analysisResult.SequenceComparison.Runs);
            Assert.Equal(comparisonsBefore, analysisResult.SequenceComparison.Comparisons);
            Assert.Equal(linksBefore, analysisResult.ContinuityResult.Links);
            Assert.Equal(pathsBefore, analysisResult.ContinuityPathsResult.Paths);
        }

        [Fact]
        public void Project_ExactAnalysisResultReferencePreserved()
        {
            var analysisResult = CreateTwoRunFixture();

            var result = CreateProjector().Project(analysisResult);

            Assert.Same(analysisResult, result.AnalysisResult);
            Assert.Same(analysisResult.SequenceComparison.Runs, result.Runs);
            Assert.Same(analysisResult.SequenceComparison.Comparisons, result.Comparisons);
            Assert.Same(analysisResult.ContinuityResult.Links, result.ContinuityLinks);
            Assert.Same(analysisResult.ContinuityPathsResult.Paths, result.ContinuityPaths);
        }

        private static SelectedMatchContinuityPath CreatePathViaReflection(IReadOnlyList<SelectedMatchContinuityLink> links) =>
            (SelectedMatchContinuityPath)PathConstructor.Invoke(new object?[] { links });

        private static void CorruptContinuityPathsResultPathsField(
            ClashRunSequenceContinuityPathsResult continuityPathsResult,
            IReadOnlyList<SelectedMatchContinuityPath> paths)
        {
            var field = typeof(ClashRunSequenceContinuityPathsResult).GetField(
                "<Paths>k__BackingField", BindingFlags.NonPublic | BindingFlags.Instance)
                ?? throw new InvalidOperationException("Could not find backing field for ClashRunSequenceContinuityPathsResult.Paths.");
            field.SetValue(continuityPathsResult, paths);
        }
    }
}
