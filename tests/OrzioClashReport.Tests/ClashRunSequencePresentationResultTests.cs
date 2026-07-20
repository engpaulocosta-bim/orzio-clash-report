using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.ExceptionServices;
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
    /// Tests for <see cref="ClashRunSequencePresentationResult"/> invariants, invoking its internal constructor
    /// through reflection (no InternalsVisibleTo, no visibility increase). Valid components are always obtained
    /// from the real <see cref="DeterministicClashRunSequencePresentationProjector"/> output and then mutated to
    /// prove each independent structural check; they are never hand-built from scratch.
    /// </summary>
    public class ClashRunSequencePresentationResultTests
    {
        private static readonly ConstructorInfo ResultConstructor =
            typeof(ClashRunSequencePresentationResult).GetConstructor(
                BindingFlags.Instance | BindingFlags.NonPublic,
                binder: null,
                types: new[]
                {
                    typeof(ClashRunSequenceAnalysisResult),
                    typeof(IReadOnlyList<ClashRunSequenceTransitionPresentation>),
                    typeof(IReadOnlyList<ClashRunSequenceLifecycleEntryPresentation>),
                    typeof(IReadOnlyList<ClashRunSequenceContinuityPathPresentation>),
                    typeof(IReadOnlyList<ClashRunSequenceLifecycleEntryPresentation>),
                    typeof(IReadOnlyList<ClashRunSequenceLifecycleEntryPresentation>),
                },
                modifiers: null)
            ?? throw new InvalidOperationException("Could not find ClashRunSequencePresentationResult internal constructor.");

        private static readonly ConstructorInfo EntryConstructor =
            typeof(ClashRunSequenceLifecycleEntryPresentation).GetConstructor(
                BindingFlags.Instance | BindingFlags.NonPublic,
                binder: null,
                types: new[]
                {
                    typeof(int), typeof(int), typeof(ClashLifecycleResult), typeof(ClashLifecycleEntry), typeof(SelectedMatchContinuityPath),
                },
                modifiers: null)
            ?? throw new InvalidOperationException("Could not find ClashRunSequenceLifecycleEntryPresentation internal constructor.");

        private static readonly ConstructorInfo PathPresentationConstructor =
            typeof(ClashRunSequenceContinuityPathPresentation).GetConstructor(
                BindingFlags.Instance | BindingFlags.NonPublic,
                binder: null,
                types: new[]
                {
                    typeof(SelectedMatchContinuityPath), typeof(IReadOnlyList<ClashRunSequenceLifecycleEntryPresentation>),
                },
                modifiers: null)
            ?? throw new InvalidOperationException("Could not find ClashRunSequenceContinuityPathPresentation internal constructor.");

        private static ClashRunSequencePresentationResult CreateResultViaReflection(
            ClashRunSequenceAnalysisResult? analysisResult,
            IReadOnlyList<ClashRunSequenceTransitionPresentation>? transitions,
            IReadOnlyList<ClashRunSequenceLifecycleEntryPresentation>? lifecycleEntries,
            IReadOnlyList<ClashRunSequenceContinuityPathPresentation>? pathPresentations,
            IReadOnlyList<ClashRunSequenceLifecycleEntryPresentation>? standaloneSelectedMatches,
            IReadOnlyList<ClashRunSequenceLifecycleEntryPresentation>? nonPathLifecycleEntries)
        {
            try
            {
                return (ClashRunSequencePresentationResult)ResultConstructor.Invoke(new object?[]
                {
                    analysisResult, transitions, lifecycleEntries, pathPresentations, standaloneSelectedMatches, nonPathLifecycleEntries,
                });
            }
            catch (TargetInvocationException ex) when (ex.InnerException != null)
            {
                ExceptionDispatchInfo.Capture(ex.InnerException).Throw();
                throw;
            }
        }

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

        private static ClashRunSequencePresentationResult BuildValid(ClashRunSequenceAnalysisResult analysisResult) =>
            new DeterministicClashRunSequencePresentationProjector().Project(analysisResult);

        /// <summary>
        /// Four-run mixture: "path" is shared A,B,C,D (one continuous path across three comparisons: 3 entries);
        /// "standalone" is shared only A,B (StillOpen, no continuity link: 1 standalone entry); "resolved-only"
        /// is only in A (Resolved in comparison0); "standalone" also resolves in comparison1 (unmatched-previous,
        /// since C never repeats it); "new-only" is only in D (New in comparison2). Totals: 7 lifecycle entries,
        /// 1 path (3 entries), 1 standalone, 4 non-path (1 standalone + 2 resolved + 1 new).
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

        /// <summary>Two runs sharing two independent occurrences: one comparison, two StillOpen entries, zero links/paths -- both standalone.</summary>
        private static ClashRunSequenceAnalysisResult CreateTwoStandaloneFixture()
        {
            var occ0 = MakeOccurrence("slot0");
            var occ1 = MakeOccurrence("slot1");
            return CreateAnalyzer().Analyze(new[] { MakeRun("run-A", occ0, occ1), MakeRun("run-B", occ0, occ1) });
        }

        /// <summary>Four-run chain producing exactly one continuity path spanning comparisons 0, 1, 2.</summary>
        private static ClashRunSequenceAnalysisResult CreateFourRunChainFixture()
        {
            var occ = MakeOccurrence("shared");
            return CreateAnalyzer().Analyze(new[]
            {
                MakeRun("run-A", occ), MakeRun("run-B", occ), MakeRun("run-C", occ), MakeRun("run-D", occ),
            });
        }

        // ===================== null checks =====================

        [Fact]
        public void Constructor_RejectsNullAnalysisResult()
        {
            var valid = BuildValid(CreateMixedFixture());

            Assert.Throws<ArgumentNullException>(() => CreateResultViaReflection(
                null, valid.Transitions, valid.LifecycleEntries, valid.PathPresentations,
                valid.StandaloneSelectedMatches, valid.NonPathLifecycleEntries));
        }

        [Fact]
        public void Constructor_RejectsNullTransitions()
        {
            var analysisResult = CreateMixedFixture();
            var valid = BuildValid(analysisResult);

            Assert.Throws<ArgumentNullException>(() => CreateResultViaReflection(
                analysisResult, null, valid.LifecycleEntries, valid.PathPresentations,
                valid.StandaloneSelectedMatches, valid.NonPathLifecycleEntries));
        }

        [Fact]
        public void Constructor_RejectsNullLifecycleEntries()
        {
            var analysisResult = CreateMixedFixture();
            var valid = BuildValid(analysisResult);

            Assert.Throws<ArgumentNullException>(() => CreateResultViaReflection(
                analysisResult, valid.Transitions, null, valid.PathPresentations,
                valid.StandaloneSelectedMatches, valid.NonPathLifecycleEntries));
        }

        [Fact]
        public void Constructor_RejectsNullPathPresentations()
        {
            var analysisResult = CreateMixedFixture();
            var valid = BuildValid(analysisResult);

            Assert.Throws<ArgumentNullException>(() => CreateResultViaReflection(
                analysisResult, valid.Transitions, valid.LifecycleEntries, null,
                valid.StandaloneSelectedMatches, valid.NonPathLifecycleEntries));
        }

        [Fact]
        public void Constructor_RejectsNullStandaloneSelectedMatches()
        {
            var analysisResult = CreateMixedFixture();
            var valid = BuildValid(analysisResult);

            Assert.Throws<ArgumentNullException>(() => CreateResultViaReflection(
                analysisResult, valid.Transitions, valid.LifecycleEntries, valid.PathPresentations,
                null, valid.NonPathLifecycleEntries));
        }

        [Fact]
        public void Constructor_RejectsNullNonPathLifecycleEntries()
        {
            var analysisResult = CreateMixedFixture();
            var valid = BuildValid(analysisResult);

            Assert.Throws<ArgumentNullException>(() => CreateResultViaReflection(
                analysisResult, valid.Transitions, valid.LifecycleEntries, valid.PathPresentations,
                valid.StandaloneSelectedMatches, null));
        }

        // ===================== transitions =====================

        [Fact]
        public void Constructor_RejectsMissingTransition()
        {
            var analysisResult = CreateMixedFixture();
            var valid = BuildValid(analysisResult);
            var transitions = new List<ClashRunSequenceTransitionPresentation>(valid.Transitions);
            transitions.RemoveAt(transitions.Count - 1);

            Assert.Throws<ArgumentException>(() => CreateResultViaReflection(
                analysisResult, transitions, valid.LifecycleEntries, valid.PathPresentations,
                valid.StandaloneSelectedMatches, valid.NonPathLifecycleEntries));
        }

        [Fact]
        public void Constructor_RejectsExtraOrDuplicateTransition()
        {
            var analysisResult = CreateMixedFixture();
            var valid = BuildValid(analysisResult);
            var transitions = new List<ClashRunSequenceTransitionPresentation>(valid.Transitions) { valid.Transitions[0] };

            Assert.Throws<ArgumentException>(() => CreateResultViaReflection(
                analysisResult, transitions, valid.LifecycleEntries, valid.PathPresentations,
                valid.StandaloneSelectedMatches, valid.NonPathLifecycleEntries));
        }

        [Fact]
        public void Constructor_RejectsOutOfOrderTransitions()
        {
            var analysisResult = CreateMixedFixture();
            var valid = BuildValid(analysisResult);
            Assert.Equal(3, valid.Transitions.Count);
            var reordered = new List<ClashRunSequenceTransitionPresentation> { valid.Transitions[1], valid.Transitions[0], valid.Transitions[2] };

            Assert.Throws<ArgumentException>(() => CreateResultViaReflection(
                analysisResult, reordered, valid.LifecycleEntries, valid.PathPresentations,
                valid.StandaloneSelectedMatches, valid.NonPathLifecycleEntries));
        }

        // ===================== lifecycle entries vs transitions =====================

        [Fact]
        public void Constructor_RejectsLifecycleEntriesMissingItem()
        {
            var analysisResult = CreateMixedFixture();
            var valid = BuildValid(analysisResult);
            var entries = new List<ClashRunSequenceLifecycleEntryPresentation>(valid.LifecycleEntries);
            entries.RemoveAt(0);

            Assert.Throws<ArgumentException>(() => CreateResultViaReflection(
                analysisResult, valid.Transitions, entries, valid.PathPresentations,
                valid.StandaloneSelectedMatches, valid.NonPathLifecycleEntries));
        }

        [Fact]
        public void Constructor_RejectsLifecycleEntriesDuplicateItem()
        {
            var analysisResult = CreateMixedFixture();
            var valid = BuildValid(analysisResult);
            var entries = new List<ClashRunSequenceLifecycleEntryPresentation>(valid.LifecycleEntries) { valid.LifecycleEntries[0] };

            Assert.Throws<ArgumentException>(() => CreateResultViaReflection(
                analysisResult, valid.Transitions, entries, valid.PathPresentations,
                valid.StandaloneSelectedMatches, valid.NonPathLifecycleEntries));
        }

        [Fact]
        public void Constructor_RejectsLifecycleEntriesOutOfOrder()
        {
            var analysisResult = CreateMixedFixture();
            var valid = BuildValid(analysisResult);
            var entries = new List<ClashRunSequenceLifecycleEntryPresentation>(valid.LifecycleEntries);
            (entries[0], entries[1]) = (entries[1], entries[0]);

            Assert.Throws<ArgumentException>(() => CreateResultViaReflection(
                analysisResult, valid.Transitions, entries, valid.PathPresentations,
                valid.StandaloneSelectedMatches, valid.NonPathLifecycleEntries));
        }

        // ===================== path presentations =====================

        [Fact]
        public void Constructor_RejectsMissingPathPresentation()
        {
            var analysisResult = CreateTwoDisconnectedPathsFixture();
            var valid = BuildValid(analysisResult);
            Assert.Equal(2, valid.PathPresentations.Count);
            var paths = new List<ClashRunSequenceContinuityPathPresentation>(valid.PathPresentations);
            paths.RemoveAt(1);

            Assert.Throws<ArgumentException>(() => CreateResultViaReflection(
                analysisResult, valid.Transitions, valid.LifecycleEntries, paths,
                valid.StandaloneSelectedMatches, valid.NonPathLifecycleEntries));
        }

        [Fact]
        public void Constructor_RejectsExtraOrDuplicatePathPresentation()
        {
            var analysisResult = CreateTwoDisconnectedPathsFixture();
            var valid = BuildValid(analysisResult);
            var paths = new List<ClashRunSequenceContinuityPathPresentation>(valid.PathPresentations) { valid.PathPresentations[0] };

            Assert.Throws<ArgumentException>(() => CreateResultViaReflection(
                analysisResult, valid.Transitions, valid.LifecycleEntries, paths,
                valid.StandaloneSelectedMatches, valid.NonPathLifecycleEntries));
        }

        [Fact]
        public void Constructor_RejectsOutOfOrderPathPresentations()
        {
            var analysisResult = CreateTwoDisconnectedPathsFixture();
            var valid = BuildValid(analysisResult);
            Assert.Equal(2, valid.PathPresentations.Count);
            var reordered = new List<ClashRunSequenceContinuityPathPresentation> { valid.PathPresentations[1], valid.PathPresentations[0] };

            Assert.Throws<ArgumentException>(() => CreateResultViaReflection(
                analysisResult, valid.Transitions, valid.LifecycleEntries, reordered,
                valid.StandaloneSelectedMatches, valid.NonPathLifecycleEntries));
        }

        [Fact]
        public void Constructor_RejectsPathPresentationEntryNotFromGlobalLifecycleEntries()
        {
            var analysisResult = CreateFourRunChainFixture();
            var valid = BuildValid(analysisResult);
            var path = valid.ContinuityPaths[0];

            // Build an independently-constructed (value-equivalent but distinct) selected-match entry set for
            // the same path, rather than reusing the canonical objects already present in LifecycleEntries.
            var foreignEntries = new List<ClashRunSequenceLifecycleEntryPresentation>();
            for (int k = 0; k < path.SelectedMatches.Count; k++)
            {
                int comparisonIndex = path.StartComparisonIndex + k;
                var comparison = valid.Comparisons[comparisonIndex];
                var candidate = path.SelectedMatches[k];
                int entryIndex = comparison.Entries.ToList().FindIndex(e => ReferenceEquals(e.SelectedMatch, candidate));
                var freshEntry = (ClashRunSequenceLifecycleEntryPresentation)EntryConstructor.Invoke(
                    new object?[] { comparisonIndex, entryIndex, comparison, comparison.Entries[entryIndex], path });
                foreignEntries.Add(freshEntry);
            }

            var foreignPathPresentation = (ClashRunSequenceContinuityPathPresentation)PathPresentationConstructor.Invoke(
                new object?[] { path, foreignEntries });

            var paths = new List<ClashRunSequenceContinuityPathPresentation> { foreignPathPresentation };

            Assert.Throws<ArgumentException>(() => CreateResultViaReflection(
                analysisResult, valid.Transitions, valid.LifecycleEntries, paths,
                valid.StandaloneSelectedMatches, valid.NonPathLifecycleEntries));
        }

        // ===================== standalone selected matches =====================

        [Fact]
        public void Constructor_RejectsStandaloneMissingEntry()
        {
            var analysisResult = CreateTwoStandaloneFixture();
            var valid = BuildValid(analysisResult);
            Assert.Equal(2, valid.StandaloneSelectedMatches.Count);
            var standalone = new List<ClashRunSequenceLifecycleEntryPresentation>(valid.StandaloneSelectedMatches);
            standalone.RemoveAt(0);

            Assert.Throws<ArgumentException>(() => CreateResultViaReflection(
                analysisResult, valid.Transitions, valid.LifecycleEntries, valid.PathPresentations,
                standalone, valid.NonPathLifecycleEntries));
        }

        [Fact]
        public void Constructor_RejectsStandaloneExtraOrDuplicateEntry()
        {
            var analysisResult = CreateTwoStandaloneFixture();
            var valid = BuildValid(analysisResult);
            var standalone = new List<ClashRunSequenceLifecycleEntryPresentation>(valid.StandaloneSelectedMatches) { valid.StandaloneSelectedMatches[0] };

            Assert.Throws<ArgumentException>(() => CreateResultViaReflection(
                analysisResult, valid.Transitions, valid.LifecycleEntries, valid.PathPresentations,
                standalone, valid.NonPathLifecycleEntries));
        }

        [Fact]
        public void Constructor_RejectsOutOfOrderStandalone()
        {
            var analysisResult = CreateTwoStandaloneFixture();
            var valid = BuildValid(analysisResult);
            Assert.Equal(2, valid.StandaloneSelectedMatches.Count);
            var reordered = new List<ClashRunSequenceLifecycleEntryPresentation>
            {
                valid.StandaloneSelectedMatches[1], valid.StandaloneSelectedMatches[0],
            };

            Assert.Throws<ArgumentException>(() => CreateResultViaReflection(
                analysisResult, valid.Transitions, valid.LifecycleEntries, valid.PathPresentations,
                reordered, valid.NonPathLifecycleEntries));
        }

        [Fact]
        public void Constructor_RejectsStandaloneContainingPathMember()
        {
            var analysisResult = CreateMixedFixture();
            var valid = BuildValid(analysisResult);
            var pathMemberEntry = valid.LifecycleEntries.First(e => e.IsInContinuityPath);
            var standalone = new List<ClashRunSequenceLifecycleEntryPresentation>(valid.StandaloneSelectedMatches) { pathMemberEntry };

            Assert.Throws<ArgumentException>(() => CreateResultViaReflection(
                analysisResult, valid.Transitions, valid.LifecycleEntries, valid.PathPresentations,
                standalone, valid.NonPathLifecycleEntries));
        }

        // ===================== non-path lifecycle entries =====================

        [Fact]
        public void Constructor_RejectsNonPathMissingEntry()
        {
            var analysisResult = CreateMixedFixture();
            var valid = BuildValid(analysisResult);
            var nonPath = new List<ClashRunSequenceLifecycleEntryPresentation>(valid.NonPathLifecycleEntries);
            nonPath.RemoveAt(0);

            Assert.Throws<ArgumentException>(() => CreateResultViaReflection(
                analysisResult, valid.Transitions, valid.LifecycleEntries, valid.PathPresentations,
                valid.StandaloneSelectedMatches, nonPath));
        }

        [Fact]
        public void Constructor_RejectsNonPathExtraOrDuplicateEntry()
        {
            var analysisResult = CreateMixedFixture();
            var valid = BuildValid(analysisResult);
            var nonPath = new List<ClashRunSequenceLifecycleEntryPresentation>(valid.NonPathLifecycleEntries) { valid.NonPathLifecycleEntries[0] };

            Assert.Throws<ArgumentException>(() => CreateResultViaReflection(
                analysisResult, valid.Transitions, valid.LifecycleEntries, valid.PathPresentations,
                valid.StandaloneSelectedMatches, nonPath));
        }

        [Fact]
        public void Constructor_RejectsOutOfOrderNonPath()
        {
            var analysisResult = CreateMixedFixture();
            var valid = BuildValid(analysisResult);
            Assert.True(valid.NonPathLifecycleEntries.Count >= 2);
            var nonPath = new List<ClashRunSequenceLifecycleEntryPresentation>(valid.NonPathLifecycleEntries);
            (nonPath[0], nonPath[1]) = (nonPath[1], nonPath[0]);

            Assert.Throws<ArgumentException>(() => CreateResultViaReflection(
                analysisResult, valid.Transitions, valid.LifecycleEntries, valid.PathPresentations,
                valid.StandaloneSelectedMatches, nonPath));
        }

        [Fact]
        public void Constructor_RejectsNonPathContainingPathMember()
        {
            var analysisResult = CreateMixedFixture();
            var valid = BuildValid(analysisResult);
            var pathMemberEntry = valid.LifecycleEntries.First(e => e.IsInContinuityPath);
            var nonPath = new List<ClashRunSequenceLifecycleEntryPresentation>(valid.NonPathLifecycleEntries) { pathMemberEntry };

            Assert.Throws<ArgumentException>(() => CreateResultViaReflection(
                analysisResult, valid.Transitions, valid.LifecycleEntries, valid.PathPresentations,
                valid.StandaloneSelectedMatches, nonPath));
        }

        // ===================== acceptance / exact references =====================

        [Fact]
        public void Constructor_AcceptsValidCompleteConstruction()
        {
            var analysisResult = CreateMixedFixture();
            var valid = BuildValid(analysisResult);

            var result = CreateResultViaReflection(
                analysisResult, valid.Transitions, valid.LifecycleEntries, valid.PathPresentations,
                valid.StandaloneSelectedMatches, valid.NonPathLifecycleEntries);

            Assert.Same(analysisResult, result.AnalysisResult);
            Assert.Equal(valid.LifecycleEntries.Count, result.LifecycleEntries.Count);
        }

        [Fact]
        public void Constructor_CanonicalReferences_ComeExactlyFromAnalysisResult()
        {
            var analysisResult = CreateMixedFixture();
            var result = BuildValid(analysisResult);

            Assert.Same(analysisResult.SequenceComparison.Runs, result.Runs);
            Assert.Same(analysisResult.SequenceComparison.Comparisons, result.Comparisons);
            Assert.Same(analysisResult.ContinuityResult.Links, result.ContinuityLinks);
            Assert.Same(analysisResult.ContinuityPathsResult.Paths, result.ContinuityPaths);
        }

        // ===================== derived counts =====================

        [Fact]
        public void Counts_MatchTheMixedFixtureExactly()
        {
            var result = BuildValid(CreateMixedFixture());

            Assert.Equal(4, result.RunCount);
            Assert.Equal(3, result.AdjacentComparisonCount);
            Assert.Equal(2, result.ContinuityLinkCount);
            Assert.Equal(1, result.ContinuityPathCount);
            Assert.Equal(1, result.StandaloneSelectedMatchCount);
            Assert.Equal(7, result.LifecycleEntryCount);
            Assert.Equal(4, result.NonPathLifecycleEntryCount);
            Assert.Equal(4, result.SelectedMatchCount);
            Assert.Equal(4, result.StillOpenCount);
            Assert.Equal(1, result.NewCount);
            Assert.Equal(2, result.ResolvedCount);
            Assert.Equal(0, result.UnverifiableCount);

            Assert.Equal(
                result.LifecycleEntryCount, result.StillOpenCount + result.NewCount + result.ResolvedCount + result.UnverifiableCount);
        }

        // ===================== runtime read-only =====================

        [Fact]
        public void Transitions_IsRuntimeReadOnly()
        {
            var result = BuildValid(CreateMixedFixture());

            Assert.IsNotType<List<ClashRunSequenceTransitionPresentation>>(result.Transitions);
            Assert.Throws<NotSupportedException>(() => ((IList<ClashRunSequenceTransitionPresentation>)result.Transitions).Clear());
        }

        [Fact]
        public void LifecycleEntries_IsRuntimeReadOnly()
        {
            var result = BuildValid(CreateMixedFixture());

            Assert.IsNotType<List<ClashRunSequenceLifecycleEntryPresentation>>(result.LifecycleEntries);
            Assert.Throws<NotSupportedException>(() => ((IList<ClashRunSequenceLifecycleEntryPresentation>)result.LifecycleEntries).Clear());
        }

        [Fact]
        public void PathPresentations_IsRuntimeReadOnly()
        {
            var result = BuildValid(CreateMixedFixture());

            Assert.IsNotType<List<ClashRunSequenceContinuityPathPresentation>>(result.PathPresentations);
            Assert.Throws<NotSupportedException>(() => ((IList<ClashRunSequenceContinuityPathPresentation>)result.PathPresentations).Clear());
        }

        [Fact]
        public void StandaloneSelectedMatches_IsRuntimeReadOnly()
        {
            var result = BuildValid(CreateTwoStandaloneFixture());

            Assert.IsNotType<List<ClashRunSequenceLifecycleEntryPresentation>>(result.StandaloneSelectedMatches);
            Assert.Throws<NotSupportedException>(() => ((IList<ClashRunSequenceLifecycleEntryPresentation>)result.StandaloneSelectedMatches).Clear());
        }

        [Fact]
        public void NonPathLifecycleEntries_IsRuntimeReadOnly()
        {
            var result = BuildValid(CreateMixedFixture());

            Assert.IsNotType<List<ClashRunSequenceLifecycleEntryPresentation>>(result.NonPathLifecycleEntries);
            Assert.Throws<NotSupportedException>(() => ((IList<ClashRunSequenceLifecycleEntryPresentation>)result.NonPathLifecycleEntries).Clear());
        }

        // ===================== no persistent identity / ledger / history =====================

        [Fact]
        public void Result_ExposesNoIdLedgerHistoryReopenedOrFingerprintProperties()
        {
            var forbiddenNames = new[]
            {
                "Id", "StableId", "PersistentId", "ClashId", "LedgerId", "TrackId", "Fingerprint",
                "Ledger", "History", "Reopened", "MultiRunLifecycle", "AggregatedStatus", "AggregatedConfidence",
            };

            var properties = typeof(ClashRunSequencePresentationResult)
                .GetProperties(BindingFlags.Public | BindingFlags.Instance).Select(p => p.Name).ToList();

            foreach (var forbidden in forbiddenNames)
            {
                Assert.DoesNotContain(forbidden, properties);
            }
        }
    }
}
