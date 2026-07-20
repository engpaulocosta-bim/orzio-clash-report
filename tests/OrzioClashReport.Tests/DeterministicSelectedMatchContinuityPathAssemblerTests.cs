using System;
using System.Linq;
using System.Reflection;
using OrzioClashReport.Core.Abstractions;
using OrzioClashReport.Core.Continuity;
using OrzioClashReport.Core.Lifecycle;
using OrzioClashReport.Core.Matching;
using OrzioClashReport.Core.Model;

namespace OrzioClashReport.Tests
{
    /// <summary>
    /// Tests for <see cref="DeterministicSelectedMatchContinuityPathAssembler"/>: complete deterministic
    /// assembly of an existing <see cref="ClashRunSequenceContinuityResult"/>'s links into disjoint maximal
    /// <see cref="SelectedMatchContinuityPath"/>s, using exact selected-match reference connectivity only, no
    /// dependency on matcher/run-comparer/lifecycle-classifier/sequence-comparer, and defensive rejection of
    /// structurally impossible branching.
    /// </summary>
    public class DeterministicSelectedMatchContinuityPathAssemblerTests
    {
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

        private static DeterministicSelectedMatchContinuityPathAssembler CreateAssembler() =>
            new DeterministicSelectedMatchContinuityPathAssembler();

        /// <summary>4-run chain (A,B,C,D) sharing one occurrence: two connected links, one maximal 2-link path.</summary>
        private static ClashRunSequenceContinuityResult CreateTwoLinkChainContinuity()
        {
            var occ = MakeOccurrence("shared");
            var sequenceComparison = BuildSequenceComparison(
                MakeRun("run-A", occ), MakeRun("run-B", occ), MakeRun("run-C", occ), MakeRun("run-D", occ));
            return ProjectContinuity(sequenceComparison);
        }

        /// <summary>
        /// 4-run fixture (A,B,C,D): A=[u], B=[u,w], C=[w,u], D=[w]. Two links at consecutive boundary indices
        /// whose selected-match references differ -- two disjoint one-link paths.
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

        /// <summary>5-run chain (A,B,C,D,E) sharing one occurrence: three connected links, one maximal 3-link path.</summary>
        private static ClashRunSequenceContinuityResult CreateThreeLinkChainContinuity()
        {
            var occ = MakeOccurrence("shared");
            var sequenceComparison = BuildSequenceComparison(
                MakeRun("run-A", occ), MakeRun("run-B", occ), MakeRun("run-C", occ), MakeRun("run-D", occ), MakeRun("run-E", occ));
            return ProjectContinuity(sequenceComparison);
        }

        /// <summary>
        /// 5-run fixture (A,B,C,D,E) mixing one 2-link connected path ("chain", A-B-C-D) with one independent
        /// 1-link path ("solo", C-D-E).
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

        // ===================== basic contract =====================

        [Fact]
        public void Assemble_RejectsNullContinuityResult()
        {
            Assert.Throws<ArgumentNullException>(() => CreateAssembler().Assemble(null!));
        }

        [Fact]
        public void Assemble_ZeroLinks_ZeroPaths()
        {
            var occ = MakeOccurrence("shared");
            var sequenceComparison = BuildSequenceComparison(MakeRun("run-A", occ), MakeRun("run-B", occ));
            var continuity = ProjectContinuity(sequenceComparison);

            var result = CreateAssembler().Assemble(continuity);

            Assert.Empty(result.Paths);
        }

        [Fact]
        public void Assemble_OneLink_OnePath()
        {
            var occ = MakeOccurrence("shared");
            var sequenceComparison = BuildSequenceComparison(MakeRun("run-A", occ), MakeRun("run-B", occ), MakeRun("run-C", occ));
            var continuity = ProjectContinuity(sequenceComparison);

            var result = CreateAssembler().Assemble(continuity);

            var path = Assert.Single(result.Paths);
            Assert.Single(path.Links);
        }

        [Fact]
        public void Assemble_OneLinkPath_HasTwoSelectedMatches()
        {
            var occ = MakeOccurrence("shared");
            var sequenceComparison = BuildSequenceComparison(MakeRun("run-A", occ), MakeRun("run-B", occ), MakeRun("run-C", occ));
            var continuity = ProjectContinuity(sequenceComparison);

            var result = CreateAssembler().Assemble(continuity);

            Assert.Equal(2, result.Paths[0].SelectedMatches.Count);
        }

        [Fact]
        public void Assemble_TwoConnectedLinks_OnePath()
        {
            var continuity = CreateTwoLinkChainContinuity();

            var result = CreateAssembler().Assemble(continuity);

            var path = Assert.Single(result.Paths);
            Assert.Equal(2, path.Links.Count);
        }

        [Fact]
        public void Assemble_SelectedRefsM0M1M2AreExact()
        {
            var continuity = CreateTwoLinkChainContinuity();

            var result = CreateAssembler().Assemble(continuity);
            var path = result.Paths[0];

            Assert.Same(continuity.Links[0].IncomingSelectedMatch, path.SelectedMatches[0]);
            Assert.Same(continuity.Links[0].OutgoingSelectedMatch, path.SelectedMatches[1]);
            Assert.Same(continuity.Links[1].IncomingSelectedMatch, path.SelectedMatches[1]);
            Assert.Same(continuity.Links[1].OutgoingSelectedMatch, path.SelectedMatches[2]);
        }

        [Fact]
        public void Assemble_ThreeConnectedLinks_OneMaximalPath()
        {
            var occ = MakeOccurrence("shared");
            var sequenceComparison = BuildSequenceComparison(
                MakeRun("run-A", occ), MakeRun("run-B", occ), MakeRun("run-C", occ), MakeRun("run-D", occ), MakeRun("run-E", occ));
            var continuity = ProjectContinuity(sequenceComparison);
            Assert.Equal(3, continuity.Links.Count);

            var result = CreateAssembler().Assemble(continuity);

            var path = Assert.Single(result.Paths);
            Assert.Equal(3, path.Links.Count);
            Assert.Equal(4, path.SelectedMatches.Count);
        }

        [Fact]
        public void Assemble_TwoDisconnectedLinks_TwoPaths()
        {
            var continuity = CreateTwoDisconnectedLinksContinuity();

            var result = CreateAssembler().Assemble(continuity);

            Assert.Equal(2, result.Paths.Count);
            Assert.All(result.Paths, p => Assert.Single(p.Links));
        }

        [Fact]
        public void Assemble_MixedFixture_ExpectedPathCount()
        {
            var continuity = CreateMixedContinuity();

            var result = CreateAssembler().Assemble(continuity);

            Assert.Equal(2, result.Paths.Count);
            Assert.Contains(result.Paths, p => p.Links.Count == 2);
            Assert.Contains(result.Paths, p => p.Links.Count == 1);
        }

        [Fact]
        public void Assemble_PathOrder_FollowsFirstLinkPosition()
        {
            var continuity = CreateMixedContinuity();

            var result = CreateAssembler().Assemble(continuity);

            int firstPathFirstLinkIndex = IndexOfByReference(continuity.Links, result.Paths[0].Links[0]);
            int secondPathFirstLinkIndex = IndexOfByReference(continuity.Links, result.Paths[1].Links[0]);
            Assert.True(firstPathFirstLinkIndex < secondPathFirstLinkIndex);
        }

        [Fact]
        public void Assemble_SameBoundaryLinks_StartDistinctPaths()
        {
            // Boundary 0 (shared run B) holds two independent shared slots that never connect further: A, B, and
            // C all share slot1 and slot2, each producing its own one-link path with no further successor.
            var shared1 = MakeOccurrence("slot1");
            var shared2 = MakeOccurrence("slot2");
            var runA = MakeRun("run-A", shared1, shared2);
            var runB = MakeRun("run-B", shared1, shared2);
            var runC = MakeRun("run-C", shared1, shared2);
            var threeRunSequence = BuildSequenceComparison(runA, runB, runC);
            var continuity = ProjectContinuity(threeRunSequence);
            Assert.Equal(2, continuity.Links.Count);

            var result = CreateAssembler().Assemble(continuity);

            Assert.Equal(2, result.Paths.Count);
            Assert.All(result.Paths, p => Assert.Single(p.Links));
        }

        [Fact]
        public void Assemble_AdjacentExactCandidate_Connects()
        {
            var continuity = CreateTwoLinkChainContinuity();

            Assert.Same(continuity.Links[0].OutgoingSelectedMatch, continuity.Links[1].IncomingSelectedMatch);

            var result = CreateAssembler().Assemble(continuity);

            Assert.Single(result.Paths);
        }

        [Fact]
        public void Assemble_ValueEquivalentCandidate_DoesNotConnect()
        {
            // Two connected links across a real 4-run chain (A,B,C,D) sharing one occurrence: L0's outgoing and
            // L1's incoming selected match start out as the exact same object (comparison[1]'s single candidate).
            var occ = MakeOccurrence("shared");
            var runA = MakeRun("run-A", occ);
            var runB = MakeRun("run-B", occ);
            var runC = MakeRun("run-C", occ);
            var runD = MakeRun("run-D", occ);
            var sequenceComparison = BuildSequenceComparison(runA, runB, runC, runD);
            var continuity = ProjectContinuity(sequenceComparison);
            Assert.Equal(2, continuity.Links.Count);

            var l0 = continuity.Links[0];
            var l1 = continuity.Links[1];
            Assert.Equal(l0.OutgoingComparisonIndex, l1.IncomingComparisonIndex);
            Assert.Same(l0.OutgoingSelectedMatch, l1.IncomingSelectedMatch);

            // Re-run the real matcher/run-comparer over the exact same runB/runC references that produced L1's
            // incoming selected match, yielding a fresh candidate that is structurally equivalent in every
            // observable respect but is a genuinely distinct object.
            IClashMatcher matcher = new ConservativeClashMatcher();
            IClashRunComparer runComparer = new DeterministicClashRunComparer(matcher);
            var freshMatchResult = runComparer.Compare(runB, runC);
            var freshCandidate = freshMatchResult.SelectedMatches[0];

            Assert.NotSame(l1.IncomingSelectedMatch, freshCandidate);
            Assert.Equal(l1.IncomingSelectedMatch.PreviousIndex, freshCandidate.PreviousIndex);
            Assert.Equal(l1.IncomingSelectedMatch.CurrentIndex, freshCandidate.CurrentIndex);
            Assert.Same(l1.IncomingSelectedMatch.Assessment.PreviousOccurrence, freshCandidate.Assessment.PreviousOccurrence);
            Assert.Same(l1.IncomingSelectedMatch.Assessment.CurrentOccurrence, freshCandidate.Assessment.CurrentOccurrence);
            Assert.Equal(l1.IncomingSelectedMatch.Assessment.Confidence, freshCandidate.Assessment.Confidence);
            Assert.Equal(l1.IncomingSelectedMatch.Assessment.Evidence.Count, freshCandidate.Assessment.Evidence.Count);
            for (int i = 0; i < l1.IncomingSelectedMatch.Assessment.Evidence.Count; i++)
            {
                Assert.Equal(l1.IncomingSelectedMatch.Assessment.Evidence[i].Kind, freshCandidate.Assessment.Evidence[i].Kind);
                Assert.Equal(l1.IncomingSelectedMatch.Assessment.Evidence[i].Verdict, freshCandidate.Assessment.Evidence[i].Verdict);
            }

            // Reflection is required here only because reusing a fresh, independently recomputed candidate as a
            // link's IncomingSelectedMatch is structurally impossible through the valid pipeline (a link's
            // IncomingSelectedMatch is always drawn from the exact ClashRunMatchResult that produced the
            // continuity result, never from a separately re-run comparison).
            CorruptLinkField(l1, nameof(SelectedMatchContinuityLink.IncomingSelectedMatch), freshCandidate);

            Assert.Equal(l0.OutgoingComparisonIndex, l1.IncomingComparisonIndex);
            Assert.NotSame(l0.OutgoingSelectedMatch, l1.IncomingSelectedMatch);

            var corruptedLinks = new System.Collections.Generic.List<SelectedMatchContinuityLink> { l0, l1 }.AsReadOnly();
            CorruptContinuityResultLinksField(continuity, corruptedLinks);

            // The assembler must refuse value-equivalent candidates because connectivity requires ReferenceEquals.
            var result = CreateAssembler().Assemble(continuity);

            Assert.Equal(2, result.Paths.Count);
            Assert.All(result.Paths, p => Assert.Single(p.Links));
        }

        [Fact]
        public void Assemble_DifferentCandidateInSameComparison_DoesNotConnect()
        {
            var continuity = CreateTwoDisconnectedLinksContinuity();

            // Both links pass through comparison[1]'s SelectedMatches, but via two distinct candidate objects.
            Assert.NotSame(continuity.Links[0].OutgoingSelectedMatch, continuity.Links[1].IncomingSelectedMatch);
            Assert.Equal(continuity.Links[0].OutgoingComparisonIndex, continuity.Links[1].IncomingComparisonIndex);

            var result = CreateAssembler().Assemble(continuity);

            Assert.Equal(2, result.Paths.Count);
        }

        [Fact]
        public void Assemble_BoundaryGap_DoesNotConnect()
        {
            // The real pipeline never produces the same exact selected-match reference serving as both an
            // outgoing candidate at one boundary and an incoming candidate at a non-consecutive boundary -- that
            // state is structurally impossible via ConservativeClashMatcher/DeterministicClashRunComparer, since
            // each pairwise ClashRunMatchResult's selected matches are scoped to that single comparison. Minimal
            // reflection corruption is used here only to prove the assembler's connectivity check enforces
            // boundary consecutiveness independently of (and even when defeated by) exact reference equality.
            var continuity = CreateThreeLinkChainContinuity();
            Assert.Equal(3, continuity.Links.Count);

            var first = continuity.Links[0]; // boundary 0 -> 1
            var second = continuity.Links[2]; // boundary 2 -> 3, non-consecutive with first

            CorruptLinkField(second, nameof(SelectedMatchContinuityLink.IncomingSelectedMatch), first.OutgoingSelectedMatch);

            Assert.Same(first.OutgoingSelectedMatch, second.IncomingSelectedMatch);
            Assert.NotEqual(first.OutgoingComparisonIndex, second.IncomingComparisonIndex);

            var corruptedLinks = new System.Collections.Generic.List<SelectedMatchContinuityLink> { first, second }.AsReadOnly();
            CorruptContinuityResultLinksField(continuity, corruptedLinks);

            var result = CreateAssembler().Assemble(continuity);

            Assert.Equal(2, result.Paths.Count);
            Assert.All(result.Paths, p => Assert.Single(p.Links));
        }

        [Fact]
        public void Assemble_DuplicateExactRunReferences_AreNotDeduplicated()
        {
            var occ = MakeOccurrence("shared");
            var runA = MakeRun("run-A", occ);
            var runB = MakeRun("run-B", occ);
            var sequenceComparison = BuildSequenceComparison(runA, runA, runB); // runA appears twice
            var continuity = ProjectContinuity(sequenceComparison);

            var result = CreateAssembler().Assemble(continuity);

            Assert.Single(result.Paths);
        }

        [Fact]
        public void Assemble_DuplicateRunIdValues_AreNotDeduplicated()
        {
            var occ = MakeOccurrence("shared");
            var runA1 = MakeRun("duplicate-run", occ);
            var runA2 = MakeRun("duplicate-run", occ);
            var runB = MakeRun("run-B", occ);
            var sequenceComparison = BuildSequenceComparison(runA1, runA2, runB);
            var continuity = ProjectContinuity(sequenceComparison);

            var result = CreateAssembler().Assemble(continuity);

            Assert.Single(result.Paths);
        }

        [Fact]
        public void Assemble_LifecycleStatusNotInspected()
        {
            var occShared = MakeOccurrence("shared");
            var runADuplicate = MakeRun("run-A-dup", occShared, occShared); // creates an Unverifiable selected match
            var runB = MakeRun("run-B", occShared);
            var runC = MakeRun("run-C", occShared);
            var sequenceComparison = BuildSequenceComparison(runADuplicate, runB, runC);
            var incomingEntry = sequenceComparison.Comparisons[0].Entries.Single(e => e.SelectedMatch != null);
            Assert.Equal(ClashLifecycleStatus.Unverifiable, incomingEntry.Status);
            var continuity = ProjectContinuity(sequenceComparison);

            var result = CreateAssembler().Assemble(continuity);

            Assert.Single(result.Paths);
        }

        [Fact]
        public void Assemble_AlternativeCandidatesNotInspected()
        {
            var occShared = MakeOccurrence("shared");
            var runADuplicate = MakeRun("run-A-dup", occShared, occShared); // ambiguous incoming, one alternative
            var runB = MakeRun("run-B", occShared);
            var runC = MakeRun("run-C", occShared);
            var sequenceComparison = BuildSequenceComparison(runADuplicate, runB, runC);
            Assert.Single(sequenceComparison.Comparisons[0].MatchResult.AlternativeCandidates);
            var continuity = ProjectContinuity(sequenceComparison);

            var result = CreateAssembler().Assemble(continuity);

            var path = Assert.Single(result.Paths);
            Assert.DoesNotContain(
                sequenceComparison.Comparisons[0].MatchResult.AlternativeCandidates,
                c => path.SelectedMatches.Any(m => ReferenceEquals(m, c)));
        }

        [Fact]
        public void Assemble_ExactInputContinuityResultReferencePreserved()
        {
            var continuity = CreateTwoLinkChainContinuity();

            var result = CreateAssembler().Assemble(continuity);

            Assert.Same(continuity, result.ContinuityResult);
        }

        [Fact]
        public void Assemble_LinksNotMutated()
        {
            var continuity = CreateMixedContinuity();
            var linksBefore = continuity.Links.ToList();

            CreateAssembler().Assemble(continuity);

            Assert.Equal(linksBefore, continuity.Links);
        }

        [Fact]
        public void Assemble_SequenceComparisonNotMutated()
        {
            var continuity = CreateMixedContinuity();
            var runsBefore = continuity.SequenceComparison.Runs.ToList();
            var comparisonsBefore = continuity.SequenceComparison.Comparisons.ToList();

            CreateAssembler().Assemble(continuity);

            Assert.Equal(runsBefore, continuity.SequenceComparison.Runs);
            Assert.Equal(comparisonsBefore, continuity.SequenceComparison.Comparisons);
        }

        [Fact]
        public void Assemble_RepeatedAssemble_IsDeterministic()
        {
            var continuity = CreateMixedContinuity();
            var assembler = CreateAssembler();

            var first = assembler.Assemble(continuity);
            var second = assembler.Assemble(continuity);

            Assert.Equal(first.Paths.Count, second.Paths.Count);
            for (int i = 0; i < first.Paths.Count; i++)
            {
                Assert.Equal(first.Paths[i].Links.Count, second.Paths[i].Links.Count);
                for (int j = 0; j < first.Paths[i].Links.Count; j++)
                {
                    Assert.Same(first.Paths[i].Links[j], second.Paths[i].Links[j]);
                }
            }
        }

        [Fact]
        public void Assemble_EveryLinkAppearsExactlyOnce()
        {
            var continuity = CreateMixedContinuity();

            var result = CreateAssembler().Assemble(continuity);
            var coveredLinks = result.Paths.SelectMany(p => p.Links).ToList();

            Assert.Equal(continuity.Links.Count, coveredLinks.Count);
            foreach (var link in continuity.Links)
            {
                Assert.Single(coveredLinks, l => ReferenceEquals(l, link));
            }
        }

        [Fact]
        public void Assemble_EveryPathIsMaximal()
        {
            var continuity = CreateMixedContinuity();

            var result = CreateAssembler().Assemble(continuity);

            foreach (var path in result.Paths)
            {
                var lastLink = path.Links[path.Links.Count - 1];
                bool hasFurtherSuccessor = continuity.Links.Any(candidate =>
                    !ReferenceEquals(candidate, lastLink)
                    && candidate.IncomingComparisonIndex == lastLink.OutgoingComparisonIndex
                    && ReferenceEquals(lastLink.OutgoingSelectedMatch, candidate.IncomingSelectedMatch));

                Assert.False(hasFurtherSuccessor);

                var firstLink = path.Links[0];
                bool hasFurtherPredecessor = continuity.Links.Any(candidate =>
                    !ReferenceEquals(candidate, firstLink)
                    && candidate.OutgoingComparisonIndex == firstLink.IncomingComparisonIndex
                    && ReferenceEquals(candidate.OutgoingSelectedMatch, firstLink.IncomingSelectedMatch));

                Assert.False(hasFurtherPredecessor);
            }
        }

        [Fact]
        public void Assembler_PublicConstructorIsParameterless()
        {
            var constructor = Assert.Single(typeof(DeterministicSelectedMatchContinuityPathAssembler).GetConstructors());
            Assert.Empty(constructor.GetParameters());
        }

        [Fact]
        public void Assembler_HasNoMatcherDependency()
        {
            var constructor = Assert.Single(typeof(DeterministicSelectedMatchContinuityPathAssembler).GetConstructors());
            Assert.DoesNotContain(constructor.GetParameters(), p => p.ParameterType == typeof(IClashMatcher));
        }

        [Fact]
        public void Assembler_HasNoRunComparerDependency()
        {
            var constructor = Assert.Single(typeof(DeterministicSelectedMatchContinuityPathAssembler).GetConstructors());
            Assert.DoesNotContain(constructor.GetParameters(), p => p.ParameterType == typeof(IClashRunComparer));
        }

        [Fact]
        public void Assembler_HasNoLifecycleClassifierDependency()
        {
            var constructor = Assert.Single(typeof(DeterministicSelectedMatchContinuityPathAssembler).GetConstructors());
            Assert.DoesNotContain(constructor.GetParameters(), p => p.ParameterType == typeof(IClashLifecycleClassifier));
        }

        [Fact]
        public void Assembler_HasNoSequenceComparerDependency()
        {
            var constructor = Assert.Single(typeof(DeterministicSelectedMatchContinuityPathAssembler).GetConstructors());
            Assert.DoesNotContain(constructor.GetParameters(), p => p.ParameterType == typeof(IClashRunSequenceComparer));
        }

        [Fact]
        public void Assembler_HasNoContinuityProjectorDependency()
        {
            var constructor = Assert.Single(typeof(DeterministicSelectedMatchContinuityPathAssembler).GetConstructors());
            Assert.DoesNotContain(constructor.GetParameters(), p => p.ParameterType == typeof(IClashRunSequenceContinuityProjector));
            Assert.Empty(constructor.GetParameters());
        }

        [Fact]
        public void Assembler_ExposesNoTrackLedgerPersistentIdentityApi()
        {
            var forbiddenNames = new[] { "Track", "Ledger", "PersistentId", "StableId", "ClashId", "Reopened" };
            var members = typeof(DeterministicSelectedMatchContinuityPathAssembler)
                .GetMembers(BindingFlags.Public | BindingFlags.Instance)
                .Select(m => m.Name)
                .ToList();

            foreach (var forbidden in forbiddenNames)
            {
                Assert.DoesNotContain(forbidden, members);
            }
        }

        [Fact]
        public void FullPipeline_ABCD_CreatesOnePathWithTwoLinks()
        {
            var continuity = CreateTwoLinkChainContinuity();

            var result = CreateAssembler().Assemble(continuity);

            var path = Assert.Single(result.Paths);
            Assert.Equal(2, path.Links.Count);
            Assert.Equal(3, path.SelectedMatches.Count);
            Assert.Equal(0, path.StartComparisonIndex);
            Assert.Equal(2, path.EndComparisonIndex);
            Assert.Equal(0, path.StartRunIndex);
            Assert.Equal(3, path.EndRunIndex);
        }

        [Fact]
        public void FullPipeline_TwoIndependentStreams_CreatesTwoPaths()
        {
            var continuity = CreateTwoDisconnectedLinksContinuity();

            var result = CreateAssembler().Assemble(continuity);

            Assert.Equal(2, result.Paths.Count);
        }

        [Fact]
        public void FullPipeline_WithABreak_CreatesSeparatePaths()
        {
            var continuity = CreateMixedContinuity();

            var result = CreateAssembler().Assemble(continuity);

            Assert.Equal(2, result.Paths.Count);
            Assert.Contains(result.Paths, p => p.Links.Count == 2);
            Assert.Contains(result.Paths, p => p.Links.Count == 1);
        }

        [Fact]
        public void Assembler_NeverCreatesZeroLinkPath()
        {
            var continuity = CreateMixedContinuity();

            var result = CreateAssembler().Assemble(continuity);

            Assert.DoesNotContain(result.Paths, p => p.Links.Count == 0);
        }

        // ===================== defensive branching corruption =====================

        [Fact]
        public void Assemble_MultipleExactSuccessors_Rejected()
        {
            // Reflection is used here only because more than one exact successor for a single link is
            // structurally impossible via the real matcher/run-comparer/projector pipeline (SelectedMatches is
            // always a one-to-one subset, so at most one candidate can share a given PreviousIndex/CurrentIndex).
            // We corrupt a validly constructed continuity result's Links list, post-construction, to prove the
            // assembler defensively detects this rather than silently picking the first match.
            var continuity = CreateTwoLinkChainContinuity();
            var duplicateSuccessor = CreateLinkViaReflection(
                continuity.Links[1].IncomingComparisonIndex,
                continuity.Links[1].SharedOccurrenceIndex,
                continuity.Links[1].SharedRun,
                continuity.Links[1].SharedOccurrence,
                continuity.Links[1].IncomingSelectedMatch,
                continuity.Links[1].OutgoingSelectedMatch);

            var corruptedLinks = new System.Collections.Generic.List<SelectedMatchContinuityLink>(continuity.Links) { duplicateSuccessor };
            CorruptContinuityResultLinksField(continuity, corruptedLinks.AsReadOnly());

            Assert.Throws<InvalidOperationException>(() => CreateAssembler().Assemble(continuity));
        }

        [Fact]
        public void Assemble_MultipleExactPredecessors_Rejected()
        {
            // Same rationale as above: at most one exact predecessor is possible via normal construction; this
            // proves the assembler reports the corrupted/impossible state instead of silently choosing one.
            var continuity = CreateTwoLinkChainContinuity();
            var duplicatePredecessor = CreateLinkViaReflection(
                continuity.Links[0].IncomingComparisonIndex,
                continuity.Links[0].SharedOccurrenceIndex,
                continuity.Links[0].SharedRun,
                continuity.Links[0].SharedOccurrence,
                continuity.Links[0].IncomingSelectedMatch,
                continuity.Links[0].OutgoingSelectedMatch);

            var corruptedLinks = new System.Collections.Generic.List<SelectedMatchContinuityLink>(continuity.Links) { duplicatePredecessor };
            CorruptContinuityResultLinksField(continuity, corruptedLinks.AsReadOnly());

            Assert.Throws<InvalidOperationException>(() => CreateAssembler().Assemble(continuity));
        }

        private static void CorruptLinkField(SelectedMatchContinuityLink link, string propertyName, object? newValue)
        {
            var field = typeof(SelectedMatchContinuityLink).GetField(
                $"<{propertyName}>k__BackingField", BindingFlags.NonPublic | BindingFlags.Instance)
                ?? throw new InvalidOperationException($"Could not find backing field for SelectedMatchContinuityLink.{propertyName}.");
            field.SetValue(link, newValue);
        }

        private static SelectedMatchContinuityLink CreateLinkViaReflection(
            int incomingComparisonIndex,
            int sharedOccurrenceIndex,
            CoordinationRun sharedRun,
            ClashOccurrence sharedOccurrence,
            ClashRunMatchCandidate incomingSelectedMatch,
            ClashRunMatchCandidate outgoingSelectedMatch)
        {
            var constructor = typeof(SelectedMatchContinuityLink).GetConstructor(
                BindingFlags.Instance | BindingFlags.NonPublic,
                binder: null,
                types: new[]
                {
                    typeof(int), typeof(int), typeof(CoordinationRun), typeof(ClashOccurrence),
                    typeof(ClashRunMatchCandidate), typeof(ClashRunMatchCandidate),
                },
                modifiers: null)
                ?? throw new InvalidOperationException("Could not find SelectedMatchContinuityLink internal constructor.");

            return (SelectedMatchContinuityLink)constructor.Invoke(new object?[]
            {
                incomingComparisonIndex, sharedOccurrenceIndex, sharedRun, sharedOccurrence,
                incomingSelectedMatch, outgoingSelectedMatch,
            });
        }

        private static void CorruptContinuityResultLinksField(
            ClashRunSequenceContinuityResult continuityResult,
            System.Collections.ObjectModel.ReadOnlyCollection<SelectedMatchContinuityLink> newLinks)
        {
            var field = typeof(ClashRunSequenceContinuityResult).GetField(
                "<Links>k__BackingField", BindingFlags.NonPublic | BindingFlags.Instance)
                ?? throw new InvalidOperationException("Could not find backing field for ClashRunSequenceContinuityResult.Links.");
            field.SetValue(continuityResult, newLinks);
        }

        private static int IndexOfByReference(
            System.Collections.Generic.IReadOnlyList<SelectedMatchContinuityLink> links, SelectedMatchContinuityLink target)
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
    }
}
