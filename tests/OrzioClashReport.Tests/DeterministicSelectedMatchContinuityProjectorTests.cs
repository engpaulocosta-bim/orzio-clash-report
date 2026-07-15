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
    /// Tests for <see cref="DeterministicSelectedMatchContinuityProjector"/>: adjacent-boundary-only traversal,
    /// exact shared-slot linking through <see cref="ClashRunMatchResult.SelectedMatches"/> only, canonical
    /// ordering, indifference to lifecycle status, exclusion of candidates/alternatives, duplicate-run
    /// tolerance, determinism, and the absence of any matcher/run-comparer/lifecycle-classifier dependency.
    /// </summary>
    public class DeterministicSelectedMatchContinuityProjectorTests
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

        private static ClashOccurrence MakeOccurrenceWithGuid(string tag, string guid) =>
            new ClashOccurrence(
                "Test 1",
                new ClashResult("Test 1", ClashStatus.New, null, null, null, MakeObject($"{tag}-a"), MakeObject($"{tag}-b"), guid),
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

        private static DeterministicSelectedMatchContinuityProjector CreateProjector() => new DeterministicSelectedMatchContinuityProjector();

        // ===================== basic contract =====================

        [Fact]
        public void Project_RejectsNullSequence()
        {
            Assert.Throws<ArgumentNullException>(() => CreateProjector().Project(null!));
        }

        [Fact]
        public void Project_TwoRunSequence_ReturnsZeroLinks()
        {
            var occ = MakeOccurrence("shared");
            var sequenceComparison = BuildSequenceComparison(MakeRun("run-A", occ), MakeRun("run-B", occ));

            var result = CreateProjector().Project(sequenceComparison);

            Assert.Empty(result.Links);
        }

        [Fact]
        public void Project_MatchingMiddleSlot_CreatesOneLink()
        {
            var occ = MakeOccurrence("shared");
            var runA = MakeRun("run-A", occ);
            var runB = MakeRun("run-B", MakeOccurrence("decoy-0"), MakeOccurrence("decoy-1"), occ); // shared at index 2
            var runC = MakeRun("run-C", occ);
            var sequenceComparison = BuildSequenceComparison(runA, runB, runC);

            var result = CreateProjector().Project(sequenceComparison);

            var link = Assert.Single(result.Links);
            Assert.Equal(0, link.IncomingComparisonIndex);
            Assert.Equal(2, link.SharedOccurrenceIndex);
        }

        [Fact]
        public void Project_LinkExactReferencesAreProven()
        {
            var occ = MakeOccurrence("shared");
            var runA = MakeRun("run-A", occ);
            var runB = MakeRun("run-B", MakeOccurrence("decoy-0"), MakeOccurrence("decoy-1"), occ);
            var runC = MakeRun("run-C", occ);
            var sequenceComparison = BuildSequenceComparison(runA, runB, runC);

            var result = CreateProjector().Project(sequenceComparison);
            var link = result.Links[0];

            Assert.Same(runB, link.SharedRun);
            Assert.Same(occ, link.SharedOccurrence);
            Assert.Same(sequenceComparison.Comparisons[0].MatchResult.SelectedMatches[0], link.IncomingSelectedMatch);
            Assert.Same(sequenceComparison.Comparisons[1].MatchResult.SelectedMatches[0], link.OutgoingSelectedMatch);
        }

        [Fact]
        public void Project_MiddleSlotMismatch_CreatesZeroLinks()
        {
            var occ = MakeOccurrence("shared");
            var runA = MakeRun("run-A", occ);
            var runB = MakeRun("run-B", occ); // matched by A
            var runC = MakeRun("run-C", MakeOccurrence("unrelated")); // does not match runB's content at all
            var sequenceComparison = BuildSequenceComparison(runA, runB, runC);

            Assert.Single(sequenceComparison.Comparisons[0].MatchResult.SelectedMatches);
            Assert.Empty(sequenceComparison.Comparisons[1].MatchResult.SelectedMatches);

            var result = CreateProjector().Project(sequenceComparison);

            Assert.Empty(result.Links);
        }

        [Fact]
        public void Project_ValueShapedMiddleOccurrencesAtDifferentSlots_DoNotLink()
        {
            // B holds two occurrences with IDENTICAL clash-test/element-id content ("twin"), at slots 0 and 1.
            // Source clash GUIDs are used only to force which slot each independent comparison selects (High
            // confidence beats Medium regardless of index), landing incoming and outgoing on different physical
            // slots despite the value-shape looking the same.
            var twinAtSlot0 = MakeOccurrenceWithGuid("twin", "guid-0");
            var twinAtSlot1 = MakeOccurrenceWithGuid("twin", "guid-1");
            var runB = MakeRun("run-B", twinAtSlot0, twinAtSlot1);

            var runA = MakeRun("run-A", MakeOccurrenceWithGuid("twin", "guid-1")); // GUID matches twinAtSlot1 -> High there
            var runC = MakeRun("run-C", MakeOccurrenceWithGuid("twin", "guid-0")); // GUID matches twinAtSlot0 -> High there

            var sequenceComparison = BuildSequenceComparison(runA, runB, runC);

            var incoming = Assert.Single(sequenceComparison.Comparisons[0].MatchResult.SelectedMatches);
            var outgoing = Assert.Single(sequenceComparison.Comparisons[1].MatchResult.SelectedMatches);
            Assert.Equal(1, incoming.CurrentIndex); // landed on twinAtSlot1
            Assert.Equal(0, outgoing.PreviousIndex); // landed on twinAtSlot0

            var result = CreateProjector().Project(sequenceComparison);

            Assert.Empty(result.Links);
        }

        [Fact]
        public void Project_TwoIndependentSharedSlots_ProduceTwoLinks()
        {
            var occ0 = MakeOccurrence("slot0");
            var occ1 = MakeOccurrence("slot1");
            var runA = MakeRun("run-A", occ0, occ1);
            var runB = MakeRun("run-B", occ0, occ1);
            var runC = MakeRun("run-C", occ0, occ1);
            var sequenceComparison = BuildSequenceComparison(runA, runB, runC);

            var result = CreateProjector().Project(sequenceComparison);

            Assert.Equal(2, result.Links.Count);
        }

        [Fact]
        public void Project_SameBoundaryLinks_OrderedBySharedSlotAscending()
        {
            var occ0 = MakeOccurrence("slot0");
            var occ1 = MakeOccurrence("slot1");
            var runA = MakeRun("run-A", occ0, occ1);
            var runB = MakeRun("run-B", occ0, occ1);
            var runC = MakeRun("run-C", occ0, occ1);
            var sequenceComparison = BuildSequenceComparison(runA, runB, runC);

            var result = CreateProjector().Project(sequenceComparison);

            Assert.Equal(0, result.Links[0].SharedOccurrenceIndex);
            Assert.Equal(1, result.Links[1].SharedOccurrenceIndex);
        }

        [Fact]
        public void Project_FourRunLinks_OrderedBoundaryZeroBeforeBoundaryOne()
        {
            var occ = MakeOccurrence("shared");
            var sequenceComparison = BuildSequenceComparison(
                MakeRun("run-A", occ), MakeRun("run-B", occ), MakeRun("run-C", occ), MakeRun("run-D", occ));

            var result = CreateProjector().Project(sequenceComparison);

            Assert.Equal(2, result.Links.Count);
            Assert.Equal(0, result.Links[0].IncomingComparisonIndex);
            Assert.Equal(1, result.Links[1].IncomingComparisonIndex);
        }

        [Fact]
        public void Project_NoLinkSkipsFromComparisonZeroToComparisonTwo()
        {
            // A does not match B's content at all; only B/C and C/D share content. No representable link could
            // ever bridge comparison[0] to comparison[2] anyway (OutgoingComparisonIndex is always derived as
            // IncomingComparisonIndex + 1), and the only real link must sit at boundary 1.
            var unrelatedForA = MakeOccurrence("unrelated-a");
            var occShared = MakeOccurrence("shared");
            var runA = MakeRun("run-A", unrelatedForA);
            var runB = MakeRun("run-B", occShared);
            var runC = MakeRun("run-C", occShared);
            var runD = MakeRun("run-D", occShared);
            var sequenceComparison = BuildSequenceComparison(runA, runB, runC, runD);

            var result = CreateProjector().Project(sequenceComparison);

            var link = Assert.Single(result.Links);
            Assert.Equal(1, link.IncomingComparisonIndex);
            foreach (var resultLink in result.Links)
            {
                Assert.Equal(resultLink.IncomingComparisonIndex + 1, resultLink.OutgoingComparisonIndex);
            }
        }

        [Fact]
        public void Project_CanonicalOrder_IsBoundaryThenSharedSlot()
        {
            // Complete projection fixture: boundary 0 links through shared run B's slots 1 and 4; boundary 1
            // links through shared run C's slot 2. See CreateCompleteProjectionFixture for full layout.
            var sequenceComparison = CreateCompleteProjectionFixture();

            var result = CreateProjector().Project(sequenceComparison);

            Assert.Equal(3, result.Links.Count);
            Assert.Equal((0, 1), (result.Links[0].IncomingComparisonIndex, result.Links[0].SharedOccurrenceIndex));
            Assert.Equal((0, 4), (result.Links[1].IncomingComparisonIndex, result.Links[1].SharedOccurrenceIndex));
            Assert.Equal((1, 2), (result.Links[2].IncomingComparisonIndex, result.Links[2].SharedOccurrenceIndex));
        }

        /// <summary>
        /// Builds a 4-run sequence (A, B, C, D) where boundary 0 (shared run B, 6 occurrences) links through
        /// slots 1 and 4, and boundary 1 (shared run C, 3 occurrences) links through slot 2 only. All other
        /// slots are decoys that match nothing.
        /// </summary>
        private static ClashRunSequenceComparisonResult CreateCompleteProjectionFixture()
        {
            var shared1 = MakeOccurrence("shared-1");
            var shared4 = MakeOccurrence("shared-4");
            var shared2 = MakeOccurrence("shared-2");

            var runA = MakeRun("run-A", shared1, shared4);
            var runB = MakeRun(
                "run-B",
                MakeOccurrence("b0-decoy"), shared1, MakeOccurrence("b2-decoy"), MakeOccurrence("b3-decoy"), shared4, shared2);
            var runC = MakeRun("run-C", shared1, shared4, shared2);
            var runD = MakeRun("run-D", shared2);

            return BuildSequenceComparison(runA, runB, runC, runD);
        }

        [Fact]
        public void Project_IncomingAlternativeCandidate_IsIgnored()
        {
            var occShared = MakeOccurrence("shared");
            var runADuplicate = MakeRun("run-A-dup", occShared, occShared); // ambiguous incoming
            var runB = MakeRun("run-B", occShared);
            var runC = MakeRun("run-C", occShared);
            var sequenceComparison = BuildSequenceComparison(runADuplicate, runB, runC);

            Assert.Single(sequenceComparison.Comparisons[0].MatchResult.AlternativeCandidates);

            var result = CreateProjector().Project(sequenceComparison);

            var link = Assert.Single(result.Links);
            Assert.Same(sequenceComparison.Comparisons[0].MatchResult.SelectedMatches[0], link.IncomingSelectedMatch);
            Assert.DoesNotContain(sequenceComparison.Comparisons[0].MatchResult.AlternativeCandidates, c => ReferenceEquals(c, link.IncomingSelectedMatch));
        }

        [Fact]
        public void Project_OutgoingAlternativeCandidate_IsIgnored()
        {
            var occShared = MakeOccurrence("shared");
            var runA = MakeRun("run-A", occShared);
            var runB = MakeRun("run-B", occShared);
            var runCDuplicate = MakeRun("run-C-dup", occShared, occShared); // ambiguous outgoing
            var sequenceComparison = BuildSequenceComparison(runA, runB, runCDuplicate);

            Assert.Single(sequenceComparison.Comparisons[1].MatchResult.AlternativeCandidates);

            var result = CreateProjector().Project(sequenceComparison);

            var link = Assert.Single(result.Links);
            Assert.Same(sequenceComparison.Comparisons[1].MatchResult.SelectedMatches[0], link.OutgoingSelectedMatch);
            Assert.DoesNotContain(sequenceComparison.Comparisons[1].MatchResult.AlternativeCandidates, c => ReferenceEquals(c, link.OutgoingSelectedMatch));
        }

        [Fact]
        public void Project_OnlySelectedMatchesParticipate_UnmatchedDecoyNeverLinks()
        {
            var occShared = MakeOccurrence("shared");
            var decoy = MakeOccurrence("fully-unmatched-decoy");
            var runA = MakeRun("run-A", occShared);
            var runB = MakeRun("run-B", occShared, decoy); // decoy at index 1 matches nothing on either side
            var runC = MakeRun("run-C", occShared);
            var sequenceComparison = BuildSequenceComparison(runA, runB, runC);

            Assert.Contains(decoy, sequenceComparison.Comparisons[0].MatchResult.UnmatchedCurrent);
            Assert.Contains(decoy, sequenceComparison.Comparisons[1].MatchResult.UnmatchedPrevious);

            var result = CreateProjector().Project(sequenceComparison);

            var link = Assert.Single(result.Links);
            Assert.Equal(0, link.SharedOccurrenceIndex);
        }

        [Fact]
        public void Project_SelectedMatchWithUnverifiableLifecycle_MayStillParticipate()
        {
            var occShared = MakeOccurrence("shared");
            // Ambiguous incoming: two previous slots (duplicate references) both matching runB's single
            // occurrence. The selected match at (previous 0, current 0) will be classified Unverifiable by the
            // real ConservativeClashLifecycleClassifier because an alternative shares its CurrentIndex.
            var runADuplicate = MakeRun("run-A-dup", occShared, occShared);
            var runB = MakeRun("run-B", occShared);
            var runC = MakeRun("run-C", occShared); // clean, unambiguous outgoing
            var sequenceComparison = BuildSequenceComparison(runADuplicate, runB, runC);

            var incomingEntry = sequenceComparison.Comparisons[0].Entries.Single(e => e.SelectedMatch != null);
            Assert.Equal(ClashLifecycleStatus.Unverifiable, incomingEntry.Status);

            var result = CreateProjector().Project(sequenceComparison);

            var link = Assert.Single(result.Links);
            Assert.Equal(0, link.SharedOccurrenceIndex);
            Assert.Same(sequenceComparison.Comparisons[0].MatchResult.SelectedMatches[0], link.IncomingSelectedMatch);
        }

        [Fact]
        public void Project_DuplicateExactRunReferences_AreNotDeduplicated()
        {
            var occ = MakeOccurrence("shared");
            var runA = MakeRun("run-A", occ);
            var runB = MakeRun("run-B", occ);
            var sequenceComparison = BuildSequenceComparison(runA, runA, runB); // runA appears twice, same reference

            Assert.Same(sequenceComparison.Runs[0], sequenceComparison.Runs[1]);

            var result = CreateProjector().Project(sequenceComparison);

            // 3 runs -> 2 comparisons -> 1 boundary; the duplicate reference is preserved (not collapsed), and
            // the single boundary still yields its expected link.
            Assert.Single(result.Links);
        }

        [Fact]
        public void Project_DuplicateRunIdValues_AreNotDeduplicated()
        {
            var occ = MakeOccurrence("shared");
            var runA1 = MakeRun("duplicate-run", occ);
            var runA2 = MakeRun("duplicate-run", occ); // distinct object, same RunId
            var runB = MakeRun("run-B", occ);
            var sequenceComparison = BuildSequenceComparison(runA1, runA2, runB);

            Assert.NotSame(sequenceComparison.Runs[0], sequenceComparison.Runs[1]);
            Assert.Equal(sequenceComparison.Runs[0].RunId, sequenceComparison.Runs[1].RunId);

            var result = CreateProjector().Project(sequenceComparison);

            // 3 runs -> 2 comparisons -> 1 boundary; same RunId across distinct run objects triggers no
            // RunId-based deduplication, and the single boundary still yields its expected link.
            Assert.Single(result.Links);
        }

        // ===================== preservation / purity =====================

        [Fact]
        public void Project_ExactInputSequenceComparisonReferencePreserved()
        {
            var occ = MakeOccurrence("shared");
            var sequenceComparison = BuildSequenceComparison(MakeRun("run-A", occ), MakeRun("run-B", occ), MakeRun("run-C", occ));

            var result = CreateProjector().Project(sequenceComparison);

            Assert.Same(sequenceComparison, result.SequenceComparison);
        }

        [Fact]
        public void Project_RunsAreNotMutated()
        {
            var occ = MakeOccurrence("shared");
            var sequenceComparison = BuildSequenceComparison(MakeRun("run-A", occ), MakeRun("run-B", occ), MakeRun("run-C", occ));
            var runsBefore = sequenceComparison.Runs.ToList();

            CreateProjector().Project(sequenceComparison);

            Assert.Equal(runsBefore, sequenceComparison.Runs);
        }

        [Fact]
        public void Project_ComparisonsAreNotMutated()
        {
            var occ = MakeOccurrence("shared");
            var sequenceComparison = BuildSequenceComparison(MakeRun("run-A", occ), MakeRun("run-B", occ), MakeRun("run-C", occ));
            var comparisonsBefore = sequenceComparison.Comparisons.ToList();

            CreateProjector().Project(sequenceComparison);

            Assert.Equal(comparisonsBefore, sequenceComparison.Comparisons);
        }

        [Fact]
        public void Project_SelectedMatchesAreNotMutated()
        {
            var occ = MakeOccurrence("shared");
            var sequenceComparison = BuildSequenceComparison(MakeRun("run-A", occ), MakeRun("run-B", occ), MakeRun("run-C", occ));
            var selectedBefore = sequenceComparison.Comparisons[0].MatchResult.SelectedMatches.ToList();

            CreateProjector().Project(sequenceComparison);

            Assert.Equal(selectedBefore, sequenceComparison.Comparisons[0].MatchResult.SelectedMatches);
        }

        [Fact]
        public void Project_RepeatedCalls_ProduceOrdinallyEquivalentLinkStructure()
        {
            var sequenceComparison = CreateCompleteProjectionFixture();
            var projector = CreateProjector();

            var first = projector.Project(sequenceComparison);
            var second = projector.Project(sequenceComparison);

            Assert.Equal(first.Links.Count, second.Links.Count);
            for (int i = 0; i < first.Links.Count; i++)
            {
                Assert.Equal(first.Links[i].IncomingComparisonIndex, second.Links[i].IncomingComparisonIndex);
                Assert.Equal(first.Links[i].SharedOccurrenceIndex, second.Links[i].SharedOccurrenceIndex);
            }
        }

        [Fact]
        public void Project_LinksCount_EqualsCompleteStructuralProjection()
        {
            var sequenceComparison = CreateCompleteProjectionFixture();

            var result = CreateProjector().Project(sequenceComparison);

            Assert.Equal(3, result.Links.Count);
        }

        // ===================== no chain / track / persistent identity =====================

        [Fact]
        public void ContinuityResult_And_Link_ExposeNoChainTrackHistoryOrPersistentIdentityProperties()
        {
            var forbiddenNames = new[]
            {
                "Chains", "Tracks", "History", "PersistentId", "ClashId", "StableId", "TrackId", "ChainId", "LedgerId", "Fingerprint",
            };

            var resultProperties = typeof(ClashRunSequenceContinuityResult).GetProperties(BindingFlags.Public | BindingFlags.Instance).Select(p => p.Name).ToList();
            var linkProperties = typeof(SelectedMatchContinuityLink).GetProperties(BindingFlags.Public | BindingFlags.Instance).Select(p => p.Name).ToList();

            foreach (var forbidden in forbiddenNames)
            {
                Assert.DoesNotContain(forbidden, resultProperties);
                Assert.DoesNotContain(forbidden, linkProperties);
            }
        }

        [Fact]
        public void NoChainScenario_FourRuns_ProducesExactlyTwoIndependentLinks()
        {
            var occ = MakeOccurrence("shared");
            var sequenceComparison = BuildSequenceComparison(
                MakeRun("run-A", occ), MakeRun("run-B", occ), MakeRun("run-C", occ), MakeRun("run-D", occ));

            var result = CreateProjector().Project(sequenceComparison);

            Assert.Equal(2, result.Links.Count);
            Assert.Equal(0, result.Links[0].IncomingComparisonIndex);
            Assert.Equal(1, result.Links[0].SharedRunIndex);
            Assert.Equal(1, result.Links[1].IncomingComparisonIndex);
            Assert.Equal(2, result.Links[1].SharedRunIndex);
        }

        // ===================== no dependencies =====================

        [Fact]
        public void Projector_HasNoMatcherDependency()
        {
            var constructor = Assert.Single(typeof(DeterministicSelectedMatchContinuityProjector).GetConstructors());
            Assert.DoesNotContain(constructor.GetParameters(), p => p.ParameterType == typeof(IClashMatcher));
        }

        [Fact]
        public void Projector_HasNoRunComparerDependency()
        {
            var constructor = Assert.Single(typeof(DeterministicSelectedMatchContinuityProjector).GetConstructors());
            Assert.DoesNotContain(constructor.GetParameters(), p => p.ParameterType == typeof(IClashRunComparer));
        }

        [Fact]
        public void Projector_HasNoLifecycleClassifierDependency()
        {
            var constructor = Assert.Single(typeof(DeterministicSelectedMatchContinuityProjector).GetConstructors());
            Assert.DoesNotContain(constructor.GetParameters(), p => p.ParameterType == typeof(IClashLifecycleClassifier));
            Assert.Empty(constructor.GetParameters());
        }
    }
}
