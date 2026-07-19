using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.ExceptionServices;
using OrzioClashReport.Core.Abstractions;
using OrzioClashReport.Core.Continuity;
using OrzioClashReport.Core.Lifecycle;
using OrzioClashReport.Core.Matching;
using OrzioClashReport.Core.Model;

namespace OrzioClashReport.Tests
{
    /// <summary>
    /// Tests for <see cref="ClashRunSequenceContinuityPathsResult"/> invariants, invoking its internal constructor
    /// through reflection (no InternalsVisibleTo, no visibility increase). Valid paths are produced through the
    /// real matcher/run-comparer/lifecycle-classifier/sequence-comparer/projector/assembler pipeline; invalid
    /// inputs are built from genuinely distinct real paths/links, never hand-built domain objects.
    /// </summary>
    public class ClashRunSequenceContinuityPathsResultTests
    {
        private static readonly ConstructorInfo ResultConstructor =
            typeof(ClashRunSequenceContinuityPathsResult).GetConstructor(
                BindingFlags.Instance | BindingFlags.NonPublic,
                binder: null,
                types: new[] { typeof(ClashRunSequenceContinuityResult), typeof(IReadOnlyList<SelectedMatchContinuityPath>) },
                modifiers: null)
            ?? throw new InvalidOperationException("Could not find ClashRunSequenceContinuityPathsResult internal constructor.");

        private static ClashRunSequenceContinuityPathsResult CreateResultViaReflection(
            ClashRunSequenceContinuityResult? continuityResult, IReadOnlyList<SelectedMatchContinuityPath>? paths)
        {
            try
            {
                return (ClashRunSequenceContinuityPathsResult)ResultConstructor.Invoke(new object?[] { continuityResult, paths });
            }
            catch (TargetInvocationException ex) when (ex.InnerException != null)
            {
                ExceptionDispatchInfo.Capture(ex.InnerException).Throw();
                throw;
            }
        }

        private static readonly ConstructorInfo PathConstructor =
            typeof(SelectedMatchContinuityPath).GetConstructor(
                BindingFlags.Instance | BindingFlags.NonPublic,
                binder: null,
                types: new[] { typeof(IReadOnlyList<SelectedMatchContinuityLink>) },
                modifiers: null)
            ?? throw new InvalidOperationException("Could not find SelectedMatchContinuityPath internal constructor.");

        private static SelectedMatchContinuityPath CreatePathViaReflection(IReadOnlyList<SelectedMatchContinuityLink> links)
        {
            try
            {
                return (SelectedMatchContinuityPath)PathConstructor.Invoke(new object?[] { links });
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

        private static ClashRunSequenceComparisonResult BuildSequenceComparison(params CoordinationRun[] runs)
        {
            IClashMatcher matcher = new ConservativeClashMatcher();
            IClashRunComparer runComparer = new DeterministicClashRunComparer(matcher);
            IClashLifecycleClassifier classifier = new ConservativeClashLifecycleClassifier();
            IClashRunSequenceComparer sequenceComparer = new DeterministicAdjacentClashRunSequenceComparer(runComparer, classifier);
            return sequenceComparer.Compare(runs);
        }

        private static ClashRunSequenceContinuityResult ProjectContinuity(ClashRunSequenceComparisonResult sequenceComparison) =>
            new DeterministicSelectedMatchContinuityProjector().Project(sequenceComparison);

        private static ClashRunSequenceContinuityPathsResult Assemble(ClashRunSequenceContinuityResult continuityResult) =>
            new DeterministicSelectedMatchContinuityPathAssembler().Assemble(continuityResult);

        /// <summary>Two-run sequence: zero comparisons... actually zero links (only one comparison, no boundary).</summary>
        private static ClashRunSequenceContinuityResult CreateZeroLinkContinuity()
        {
            var occ = MakeOccurrence("shared");
            var sequenceComparison = BuildSequenceComparison(MakeRun("run-A", occ), MakeRun("run-B", occ));
            return ProjectContinuity(sequenceComparison);
        }

        /// <summary>3-run chain sharing one occurrence: exactly one continuity link.</summary>
        private static ClashRunSequenceContinuityResult CreateSingleLinkContinuity()
        {
            var occ = MakeOccurrence("shared");
            var sequenceComparison = BuildSequenceComparison(MakeRun("run-A", occ), MakeRun("run-B", occ), MakeRun("run-C", occ));
            return ProjectContinuity(sequenceComparison);
        }

        /// <summary>4-run chain sharing one occurrence: exactly two connected continuity links (one maximal path).</summary>
        private static ClashRunSequenceContinuityResult CreateTwoLinkChainContinuity()
        {
            var occ = MakeOccurrence("shared");
            var sequenceComparison = BuildSequenceComparison(
                MakeRun("run-A", occ), MakeRun("run-B", occ), MakeRun("run-C", occ), MakeRun("run-D", occ));
            return ProjectContinuity(sequenceComparison);
        }

        /// <summary>
        /// 4-run fixture (A,B,C,D): A=[u], B=[u,w], C=[w,u], D=[w]. Produces exactly two links whose boundary
        /// indices are consecutive but whose selected-match references differ -- two disjoint one-link paths,
        /// never one two-link path.
        /// </summary>
        private static ClashRunSequenceContinuityResult CreateTwoDisconnectedLinksContinuity()
        {
            var runA = MakeRun("run-A", MakeOccurrence("u"));
            var runB = MakeRun("run-B", MakeOccurrence("u"), MakeOccurrence("w"));
            var runC = MakeRun("run-C", MakeOccurrence("w"), MakeOccurrence("u"));
            var runD = MakeRun("run-D", MakeOccurrence("w"));
            var sequenceComparison = BuildSequenceComparison(runA, runB, runC, runD);
            return ProjectContinuity(sequenceComparison);
        }

        /// <summary>
        /// 5-run fixture (A,B,C,D,E) mixing one 2-link connected path ("chain", spanning A-B-C-D) with one
        /// independent 1-link path ("solo", spanning C-D-E): A=[chain], B=[chain], C=[chain,solo],
        /// D=[chain,solo], E=[solo].
        /// </summary>
        private static ClashRunSequenceContinuityResult CreateMixedContinuity()
        {
            var runA = MakeRun("run-A", MakeOccurrence("chain"));
            var runB = MakeRun("run-B", MakeOccurrence("chain"));
            var runC = MakeRun("run-C", MakeOccurrence("chain"), MakeOccurrence("solo"));
            var runD = MakeRun("run-D", MakeOccurrence("chain"), MakeOccurrence("solo"));
            var runE = MakeRun("run-E", MakeOccurrence("solo"));
            var sequenceComparison = BuildSequenceComparison(runA, runB, runC, runD, runE);
            return ProjectContinuity(sequenceComparison);
        }

        // ===================== acceptance =====================

        [Fact]
        public void Result_ZeroLinksAndZeroPaths_Accepted()
        {
            var continuity = CreateZeroLinkContinuity();

            var result = CreateResultViaReflection(continuity, Array.Empty<SelectedMatchContinuityPath>());

            Assert.Empty(result.Paths);
        }

        [Fact]
        public void Result_OneLinkOnePath_Accepted()
        {
            var continuity = CreateSingleLinkContinuity();
            var path = CreatePathViaReflection(new[] { continuity.Links[0] });

            var result = CreateResultViaReflection(continuity, new[] { path });

            Assert.Single(result.Paths);
        }

        [Fact]
        public void Result_TwoConnectedLinksOneMaximalPath_Accepted()
        {
            var continuity = CreateTwoLinkChainContinuity();
            var path = CreatePathViaReflection(continuity.Links);

            var result = CreateResultViaReflection(continuity, new[] { path });

            var onlyPath = Assert.Single(result.Paths);
            Assert.Equal(2, onlyPath.Links.Count);
        }

        [Fact]
        public void Result_TwoDisconnectedLinksTwoPaths_Accepted()
        {
            var continuity = CreateTwoDisconnectedLinksContinuity();
            var pathA = CreatePathViaReflection(new[] { continuity.Links[0] });
            var pathB = CreatePathViaReflection(new[] { continuity.Links[1] });

            var result = CreateResultViaReflection(continuity, new[] { pathA, pathB });

            Assert.Equal(2, result.Paths.Count);
        }

        [Fact]
        public void Result_MixedPaths_Accepted()
        {
            var continuity = CreateMixedContinuity();
            var assembled = Assemble(continuity);

            var result = CreateResultViaReflection(continuity, assembled.Paths);

            Assert.Equal(2, result.Paths.Count);
        }

        // ===================== rejection: null =====================

        [Fact]
        public void Result_RejectsNullContinuityResult()
        {
            Assert.Throws<ArgumentNullException>(() => CreateResultViaReflection(null, Array.Empty<SelectedMatchContinuityPath>()));
        }

        [Fact]
        public void Result_RejectsNullPaths()
        {
            var continuity = CreateZeroLinkContinuity();

            Assert.Throws<ArgumentNullException>(() => CreateResultViaReflection(continuity, null));
        }

        [Fact]
        public void Result_RejectsNullFirstPath()
        {
            var continuity = CreateSingleLinkContinuity();

            Assert.Throws<ArgumentException>(() =>
                CreateResultViaReflection(continuity, new SelectedMatchContinuityPath?[] { null }!));
        }

        [Fact]
        public void Result_RejectsNullLaterPath()
        {
            var continuity = CreateTwoDisconnectedLinksContinuity();
            var pathA = CreatePathViaReflection(new[] { continuity.Links[0] });

            Assert.Throws<ArgumentException>(() =>
                CreateResultViaReflection(continuity, new SelectedMatchContinuityPath?[] { pathA, null }!));
        }

        // ===================== rejection: partition integrity =====================

        [Fact]
        public void Result_RejectsForeignLinkPath()
        {
            var continuity = CreateSingleLinkContinuity();
            var foreignContinuity = CreateSingleLinkContinuity(); // independent pipeline run, unrelated links
            var foreignPath = CreatePathViaReflection(new[] { foreignContinuity.Links[0] });

            Assert.Throws<ArgumentException>(() => CreateResultViaReflection(continuity, new[] { foreignPath }));
        }

        [Fact]
        public void Result_RejectsEquivalentDistinctLinkObject()
        {
            var continuity = CreateSingleLinkContinuity();
            var freshContinuity = CreateSingleLinkContinuity();
            Assert.NotSame(continuity.Links[0], freshContinuity.Links[0]);

            var equivalentPath = CreatePathViaReflection(new[] { freshContinuity.Links[0] });

            Assert.Throws<ArgumentException>(() => CreateResultViaReflection(continuity, new[] { equivalentPath }));
        }

        [Fact]
        public void Result_RejectsMissingPath()
        {
            var continuity = CreateTwoDisconnectedLinksContinuity();
            var pathA = CreatePathViaReflection(new[] { continuity.Links[0] });
            // pathB (for continuity.Links[1]) is missing entirely.

            Assert.Throws<ArgumentException>(() => CreateResultViaReflection(continuity, new[] { pathA }));
        }

        [Fact]
        public void Result_RejectsExtraPath()
        {
            var continuity = CreateSingleLinkContinuity();
            var path = CreatePathViaReflection(new[] { continuity.Links[0] });

            var otherContinuity = CreateSingleLinkContinuity();
            var extraPath = CreatePathViaReflection(new[] { otherContinuity.Links[0] });

            Assert.Throws<ArgumentException>(() => CreateResultViaReflection(continuity, new[] { path, extraPath }));
        }

        [Fact]
        public void Result_RejectsDuplicatePath()
        {
            var continuity = CreateSingleLinkContinuity();
            var path = CreatePathViaReflection(new[] { continuity.Links[0] });

            Assert.Throws<ArgumentException>(() => CreateResultViaReflection(continuity, new[] { path, path }));
        }

        [Fact]
        public void Result_RejectsWrongPathOrder()
        {
            var continuity = CreateTwoDisconnectedLinksContinuity();
            var pathA = CreatePathViaReflection(new[] { continuity.Links[0] }); // canonical position 0
            var pathB = CreatePathViaReflection(new[] { continuity.Links[1] }); // canonical position 1

            Assert.Throws<ArgumentException>(() => CreateResultViaReflection(continuity, new[] { pathB, pathA }));
        }

        [Fact]
        public void Result_RejectsSplitMaximalPath()
        {
            var continuity = CreateTwoLinkChainContinuity();
            var splitFirst = CreatePathViaReflection(new[] { continuity.Links[0] });
            var splitSecond = CreatePathViaReflection(new[] { continuity.Links[1] });

            Assert.Throws<ArgumentException>(() => CreateResultViaReflection(continuity, new[] { splitFirst, splitSecond }));
        }

        [Fact]
        public void Result_RejectsMergeDisconnectedPaths()
        {
            var continuity = CreateTwoDisconnectedLinksContinuity();

            Assert.Throws<ArgumentException>(() => CreateResultViaReflection(continuity, new[] { CreatePathViaReflection(continuity.Links) }));
        }

        [Fact]
        public void Result_RejectsMissingLinkInsidePath()
        {
            var continuity = CreateTwoLinkChainContinuity();
            var incompletePath = CreatePathViaReflection(new[] { continuity.Links[0] }); // missing Links[1]

            Assert.Throws<ArgumentException>(() => CreateResultViaReflection(continuity, new[] { incompletePath }));
        }

        [Fact]
        public void Result_RejectsDuplicateLinkCoverage()
        {
            var continuity = CreateTwoDisconnectedLinksContinuity();
            var pathA = CreatePathViaReflection(new[] { continuity.Links[0] });
            var pathADuplicate = CreatePathViaReflection(new[] { continuity.Links[0] }); // covers Links[0] again, never Links[1]

            Assert.Throws<ArgumentException>(() => CreateResultViaReflection(continuity, new[] { pathA, pathADuplicate }));
        }

        [Fact]
        public void Result_EveryLinkCoveredExactlyOnce()
        {
            var continuity = CreateMixedContinuity();
            var assembled = Assemble(continuity);

            var coveredLinks = assembled.Paths.SelectMany(p => p.Links).ToList();

            Assert.Equal(continuity.Links.Count, coveredLinks.Count);
            foreach (var link in continuity.Links)
            {
                Assert.Single(coveredLinks, l => ReferenceEquals(l, link));
            }
        }

        [Fact]
        public void Result_CanonicalFirstLinkOrder()
        {
            var continuity = CreateMixedContinuity();

            var result = Assemble(continuity);

            for (int i = 1; i < result.Paths.Count; i++)
            {
                int previousFirstLinkPosition = IndexOfByReference(continuity.Links, result.Paths[i - 1].Links[0]);
                int currentFirstLinkPosition = IndexOfByReference(continuity.Links, result.Paths[i].Links[0]);
                Assert.True(previousFirstLinkPosition < currentFirstLinkPosition);
            }
        }

        [Fact]
        public void Result_RejectsNonMaximalPath()
        {
            var continuity = CreateThreeLinkChainContinuityForNonMaximal();
            // A path using only the first two links, when a third genuinely connects further, is non-maximal.
            var nonMaximalPath = CreatePathViaReflection(new[] { continuity.Links[0], continuity.Links[1] });

            Assert.Throws<ArgumentException>(() => CreateResultViaReflection(continuity, new[] { nonMaximalPath }));
        }

        private static ClashRunSequenceContinuityResult CreateThreeLinkChainContinuityForNonMaximal()
        {
            var occ = MakeOccurrence("shared");
            var sequenceComparison = BuildSequenceComparison(
                MakeRun("run-A", occ), MakeRun("run-B", occ), MakeRun("run-C", occ), MakeRun("run-D", occ), MakeRun("run-E", occ));
            return ProjectContinuity(sequenceComparison);
        }

        private static int IndexOfByReference(IReadOnlyList<SelectedMatchContinuityLink> links, SelectedMatchContinuityLink target)
        {
            for (int i = 0; i < links.Count; i++)
            {
                if (ReferenceEquals(links[i], target))
                {
                    return i;
                }
            }

            return -1;
        }

        // ===================== preservation / purity =====================

        [Fact]
        public void Result_ExactContinuityResultReferencePreserved()
        {
            var continuity = CreateSingleLinkContinuity();
            var path = CreatePathViaReflection(new[] { continuity.Links[0] });

            var result = CreateResultViaReflection(continuity, new[] { path });

            Assert.Same(continuity, result.ContinuityResult);
        }

        [Fact]
        public void Result_InputPathsMutationDoesNotAffectResult()
        {
            var continuity = CreateSingleLinkContinuity();
            var path = CreatePathViaReflection(new[] { continuity.Links[0] });
            var mutablePaths = new List<SelectedMatchContinuityPath> { path };

            var result = CreateResultViaReflection(continuity, mutablePaths);
            mutablePaths.Clear();

            Assert.Single(result.Paths);
        }

        [Fact]
        public void Result_PathsIsRuntimeReadOnly()
        {
            var continuity = CreateSingleLinkContinuity();
            var path = CreatePathViaReflection(new[] { continuity.Links[0] });

            var result = CreateResultViaReflection(continuity, new[] { path });

            Assert.Throws<NotSupportedException>(() => ((IList<SelectedMatchContinuityPath>)result.Paths).Add(path));
        }

        [Fact]
        public void Result_ExactLinkReferencesPreserved()
        {
            var continuity = CreateTwoLinkChainContinuity();
            var path = CreatePathViaReflection(continuity.Links);

            var result = CreateResultViaReflection(continuity, new[] { path });

            Assert.Same(continuity.Links[0], result.Paths[0].Links[0]);
            Assert.Same(continuity.Links[1], result.Paths[0].Links[1]);
        }

        [Fact]
        public void Result_ZeroLinksNeverCreateEmptyPath()
        {
            var continuity = CreateZeroLinkContinuity();

            var result = Assemble(continuity);

            Assert.Empty(result.Paths);
            Assert.DoesNotContain(result.Paths, p => p.Links.Count == 0);
        }

        [Fact]
        public void Result_UnlinkedSelectedMatchIsNotRepresented()
        {
            var occShared = MakeOccurrence("shared");
            var decoy = MakeOccurrence("fully-unmatched-decoy");
            var runA = MakeRun("run-A", occShared);
            var runB = MakeRun("run-B", occShared, decoy);
            var runC = MakeRun("run-C", occShared);
            var sequenceComparison = BuildSequenceComparison(runA, runB, runC);
            var continuity = ProjectContinuity(sequenceComparison);

            var result = Assemble(continuity);

            var allSelectedMatchesInPaths = result.Paths.SelectMany(p => p.SelectedMatches).ToList();
            Assert.DoesNotContain(
                allSelectedMatchesInPaths,
                candidate => sequenceComparison.Comparisons[0].MatchResult.UnmatchedCurrent.Contains(decoy)
                    && ReferenceEquals(candidate.Assessment.CurrentOccurrence, decoy));
        }

        [Fact]
        public void Result_ExposesNoIdTrackHistoryLedgerProperties()
        {
            var forbiddenNames = new[]
            {
                "Id", "PathId", "TrackId", "ChainId", "StableId", "PersistentId", "ClashId", "LedgerId",
                "Fingerprint", "History", "Ledger", "Track",
            };

            var properties = typeof(ClashRunSequenceContinuityPathsResult).GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .Select(p => p.Name).ToList();

            foreach (var forbidden in forbiddenNames)
            {
                Assert.DoesNotContain(forbidden, properties);
            }
        }

        // ===================== ToString =====================

        [Fact]
        public void Result_ToString_ExactExample()
        {
            var continuity = CreateTwoLinkChainContinuity();

            var result = Assemble(continuity);

            Assert.Equal("4 runs, 3 adjacent comparisons, 2 continuity link(s), 1 continuity path(s)", result.ToString());
        }
    }
}
