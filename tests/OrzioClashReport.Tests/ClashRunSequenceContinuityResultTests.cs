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
    /// Tests for <see cref="ClashRunSequenceContinuityResult"/> invariants, invoking its internal constructor
    /// through reflection (no InternalsVisibleTo, no visibility increase). Valid links are produced through the
    /// real matcher/run-comparer/lifecycle-classifier/sequence-comparer/projector pipeline; invalid inputs are
    /// built either from genuinely distinct (impostor/alternative/fresh) real objects, or -- only where no
    /// domain object could otherwise exist in that shape -- by reflectively corrupting a single field on an
    /// already validly constructed object, after construction, to prove the result independently re-validates
    /// rather than trusting an already-built link.
    /// </summary>
    public class ClashRunSequenceContinuityResultTests
    {
        private static readonly ConstructorInfo ResultConstructor =
            typeof(ClashRunSequenceContinuityResult).GetConstructor(
                BindingFlags.Instance | BindingFlags.NonPublic,
                binder: null,
                types: new[] { typeof(ClashRunSequenceComparisonResult), typeof(IReadOnlyList<SelectedMatchContinuityLink>) },
                modifiers: null)
            ?? throw new InvalidOperationException("Could not find ClashRunSequenceContinuityResult internal constructor.");

        private static ClashRunSequenceContinuityResult CreateResultViaReflection(
            ClashRunSequenceComparisonResult? sequenceComparison, IReadOnlyList<SelectedMatchContinuityLink>? links)
        {
            try
            {
                return (ClashRunSequenceContinuityResult)ResultConstructor.Invoke(new object?[] { sequenceComparison, links });
            }
            catch (TargetInvocationException ex) when (ex.InnerException != null)
            {
                ExceptionDispatchInfo.Capture(ex.InnerException).Throw();
                throw;
            }
        }

        private static readonly ConstructorInfo LinkConstructor =
            typeof(SelectedMatchContinuityLink).GetConstructor(
                BindingFlags.Instance | BindingFlags.NonPublic,
                binder: null,
                types: new[]
                {
                    typeof(int), typeof(int), typeof(CoordinationRun), typeof(ClashOccurrence),
                    typeof(ClashRunMatchCandidate), typeof(ClashRunMatchCandidate),
                },
                modifiers: null)
            ?? throw new InvalidOperationException("Could not find SelectedMatchContinuityLink internal constructor.");

        private static SelectedMatchContinuityLink CreateLinkViaReflection(
            int incomingComparisonIndex, int sharedOccurrenceIndex, CoordinationRun sharedRun, ClashOccurrence sharedOccurrence,
            ClashRunMatchCandidate incomingSelectedMatch, ClashRunMatchCandidate outgoingSelectedMatch)
        {
            try
            {
                return (SelectedMatchContinuityLink)LinkConstructor.Invoke(new object?[]
                {
                    incomingComparisonIndex, sharedOccurrenceIndex, sharedRun, sharedOccurrence,
                    incomingSelectedMatch, outgoingSelectedMatch,
                });
            }
            catch (TargetInvocationException ex) when (ex.InnerException != null)
            {
                ExceptionDispatchInfo.Capture(ex.InnerException).Throw();
                throw;
            }
        }

        private static void CorruptField(object target, Type declaringType, string propertyName, object? newValue)
        {
            var field = declaringType.GetField($"<{propertyName}>k__BackingField", BindingFlags.NonPublic | BindingFlags.Instance)
                ?? throw new InvalidOperationException($"Could not find backing field for {declaringType.Name}.{propertyName}.");
            field.SetValue(target, newValue);
        }

        private static void CorruptLinkField(SelectedMatchContinuityLink link, string propertyName, object? newValue) =>
            CorruptField(link, typeof(SelectedMatchContinuityLink), propertyName, newValue);

        private static void CorruptCandidateField(ClashRunMatchCandidate candidate, string propertyName, object? newValue) =>
            CorruptField(candidate, typeof(ClashRunMatchCandidate), propertyName, newValue);

        private static void CorruptAssessmentField(ClashMatchAssessment assessment, string propertyName, object? newValue) =>
            CorruptField(assessment, typeof(ClashMatchAssessment), propertyName, newValue);

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

        private static ClashRunMatchResult CreateMatchResult(CoordinationRun previousRun, CoordinationRun currentRun)
        {
            IClashMatcher matcher = new ConservativeClashMatcher();
            IClashRunComparer comparer = new DeterministicClashRunComparer(matcher);
            return comparer.Compare(previousRun, currentRun);
        }

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

        // ===================== acceptance =====================

        [Fact]
        public void Result_TwoRunSequence_ZeroLinksAccepted()
        {
            var occ = MakeOccurrence("shared");
            var sequenceComparison = BuildSequenceComparison(MakeRun("run-A", occ), MakeRun("run-B", occ));

            var continuity = ProjectContinuity(sequenceComparison);

            Assert.Empty(continuity.Links);
        }

        [Fact]
        public void Result_ThreeRunSequence_OneCompleteLinkAccepted()
        {
            var occ = MakeOccurrence("shared");
            var sequenceComparison = BuildSequenceComparison(MakeRun("run-A", occ), MakeRun("run-B", occ), MakeRun("run-C", occ));

            var continuity = ProjectContinuity(sequenceComparison);

            var link = Assert.Single(continuity.Links);
            Assert.Equal(0, link.IncomingComparisonIndex);
            Assert.Equal(0, link.SharedOccurrenceIndex);
        }

        [Fact]
        public void Result_FourRunSequence_LinksOnTwoBoundariesAccepted()
        {
            var occ = MakeOccurrence("shared");
            var sequenceComparison = BuildSequenceComparison(
                MakeRun("run-A", occ), MakeRun("run-B", occ), MakeRun("run-C", occ), MakeRun("run-D", occ));

            var continuity = ProjectContinuity(sequenceComparison);

            Assert.Equal(2, continuity.Links.Count);
            Assert.Equal(0, continuity.Links[0].IncomingComparisonIndex);
            Assert.Equal(1, continuity.Links[1].IncomingComparisonIndex);
        }

        // ===================== rejection: null =====================

        [Fact]
        public void Result_RejectsNullSequenceComparison()
        {
            Assert.Throws<ArgumentNullException>(() =>
                CreateResultViaReflection(null, Array.Empty<SelectedMatchContinuityLink>()));
        }

        [Fact]
        public void Result_RejectsNullLinks()
        {
            var occ = MakeOccurrence("shared");
            var sequenceComparison = BuildSequenceComparison(MakeRun("run-A", occ), MakeRun("run-B", occ));

            Assert.Throws<ArgumentNullException>(() => CreateResultViaReflection(sequenceComparison, null));
        }

        [Fact]
        public void Result_RejectsNullFirstLink()
        {
            var occ = MakeOccurrence("shared");
            var sequenceComparison = BuildSequenceComparison(MakeRun("run-A", occ), MakeRun("run-B", occ), MakeRun("run-C", occ));

            Assert.Throws<ArgumentException>(() =>
                CreateResultViaReflection(sequenceComparison, new SelectedMatchContinuityLink?[] { null }!));
        }

        [Fact]
        public void Result_RejectsNullLaterLink()
        {
            var occ = MakeOccurrence("shared");
            var sequenceComparison = BuildSequenceComparison(
                MakeRun("run-A", occ), MakeRun("run-B", occ), MakeRun("run-C", occ), MakeRun("run-D", occ));
            var realLinks = ProjectContinuity(sequenceComparison).Links;

            Assert.Throws<ArgumentException>(() =>
                CreateResultViaReflection(sequenceComparison, new SelectedMatchContinuityLink?[] { realLinks[0], null }!));
        }

        // ===================== rejection: boundary range =====================

        [Fact]
        public void Result_RejectsBoundaryIndexOutOfRange()
        {
            var occ = MakeOccurrence("shared");
            // Two-run sequence has 1 comparison, i.e. 0 valid boundaries.
            var twoRunSequence = BuildSequenceComparison(MakeRun("run-A", occ), MakeRun("run-B", occ));

            // A link that is perfectly valid in isolation, borrowed from an unrelated three-run scenario.
            var unrelatedSequence = BuildSequenceComparison(MakeRun("run-X", occ), MakeRun("run-Y", occ), MakeRun("run-Z", occ));
            var foreignLink = ProjectContinuity(unrelatedSequence).Links[0];

            Assert.Throws<ArgumentException>(() => CreateResultViaReflection(twoRunSequence, new[] { foreignLink }));
        }

        // ===================== rejection: incoming membership =====================

        [Fact]
        public void Result_RejectsIncomingMatchNotExactSelectedMember()
        {
            var occ = MakeOccurrence("shared");
            var runA = MakeRun("run-A", occ);
            var runB = MakeRun("run-B", occ);
            var runC = MakeRun("run-C", occ);
            var sequenceComparison = BuildSequenceComparison(runA, runB, runC);
            var realOutgoing = sequenceComparison.Comparisons[1].MatchResult.SelectedMatches[0];

            // An impostor candidate, structurally self-consistent (same shared occurrence, same index), but
            // computed from an entirely separate Compare() call -- never literally a member of the real sequence.
            var impostorSequence = BuildSequenceComparison(MakeRun("run-Impostor-A", occ), MakeRun("run-Impostor-B", occ));
            var impostorIncoming = impostorSequence.Comparisons[0].MatchResult.SelectedMatches[0];

            var link = CreateLinkViaReflection(0, 0, runB, occ, impostorIncoming, realOutgoing);

            Assert.Throws<ArgumentException>(() => CreateResultViaReflection(sequenceComparison, new[] { link }));
        }

        [Fact]
        public void Result_RejectsEquivalentIncomingCandidateReference()
        {
            var occ = MakeOccurrence("shared");
            var runA = MakeRun("run-A", occ);
            var runB = MakeRun("run-B", occ);
            var runC = MakeRun("run-C", occ);
            var sequenceComparison = BuildSequenceComparison(runA, runB, runC);
            var realOutgoing = sequenceComparison.Comparisons[1].MatchResult.SelectedMatches[0];

            // Re-running the exact same real pipeline on the exact same runA/runB objects produces a fresh,
            // value-equivalent, but distinct ClashRunMatchCandidate instance.
            var freshIncoming = CreateMatchResult(runA, runB).SelectedMatches[0];
            Assert.NotSame(sequenceComparison.Comparisons[0].MatchResult.SelectedMatches[0], freshIncoming);

            var link = CreateLinkViaReflection(0, 0, runB, occ, freshIncoming, realOutgoing);

            Assert.Throws<ArgumentException>(() => CreateResultViaReflection(sequenceComparison, new[] { link }));
        }

        [Fact]
        public void Result_RejectsIncomingAlternativeCandidate()
        {
            var occShared = MakeOccurrence("shared");
            // Duplicate previous occurrence creates ambiguity: one selected match, one alternative, both
            // referencing CurrentIndex 0.
            var runADuplicate = MakeRun("run-A-dup", occShared, occShared);
            var runB = MakeRun("run-B", occShared);
            var runC = MakeRun("run-C", occShared);
            var sequenceComparison = BuildSequenceComparison(runADuplicate, runB, runC);

            var incomingAlternative = sequenceComparison.Comparisons[0].MatchResult.AlternativeCandidates.Single(a => a.CurrentIndex == 0);
            var realOutgoing = sequenceComparison.Comparisons[1].MatchResult.SelectedMatches.Single(c => c.PreviousIndex == 0);

            var link = CreateLinkViaReflection(0, 0, runB, occShared, incomingAlternative, realOutgoing);

            Assert.Throws<ArgumentException>(() => CreateResultViaReflection(sequenceComparison, new[] { link }));
        }

        // ===================== rejection: outgoing membership =====================

        [Fact]
        public void Result_RejectsOutgoingMatchNotExactSelectedMember()
        {
            var occ = MakeOccurrence("shared");
            var runA = MakeRun("run-A", occ);
            var runB = MakeRun("run-B", occ);
            var runC = MakeRun("run-C", occ);
            var sequenceComparison = BuildSequenceComparison(runA, runB, runC);
            var realIncoming = sequenceComparison.Comparisons[0].MatchResult.SelectedMatches[0];

            var impostorSequence = BuildSequenceComparison(MakeRun("run-Impostor-B", occ), MakeRun("run-Impostor-C", occ));
            var impostorOutgoing = impostorSequence.Comparisons[0].MatchResult.SelectedMatches[0];

            var link = CreateLinkViaReflection(0, 0, runB, occ, realIncoming, impostorOutgoing);

            Assert.Throws<ArgumentException>(() => CreateResultViaReflection(sequenceComparison, new[] { link }));
        }

        [Fact]
        public void Result_RejectsEquivalentOutgoingCandidateReference()
        {
            var occ = MakeOccurrence("shared");
            var runA = MakeRun("run-A", occ);
            var runB = MakeRun("run-B", occ);
            var runC = MakeRun("run-C", occ);
            var sequenceComparison = BuildSequenceComparison(runA, runB, runC);
            var realIncoming = sequenceComparison.Comparisons[0].MatchResult.SelectedMatches[0];

            var freshOutgoing = CreateMatchResult(runB, runC).SelectedMatches[0];
            Assert.NotSame(sequenceComparison.Comparisons[1].MatchResult.SelectedMatches[0], freshOutgoing);

            var link = CreateLinkViaReflection(0, 0, runB, occ, realIncoming, freshOutgoing);

            Assert.Throws<ArgumentException>(() => CreateResultViaReflection(sequenceComparison, new[] { link }));
        }

        [Fact]
        public void Result_RejectsOutgoingAlternativeCandidate()
        {
            var occShared = MakeOccurrence("shared-out");
            var runA = MakeRun("run-A", occShared);
            var runB = MakeRun("run-B", occShared);
            // Duplicate current occurrence creates ambiguity in the outgoing comparison: one selected, one
            // alternative, both referencing PreviousIndex 0.
            var runCDuplicate = MakeRun("run-C-dup", occShared, occShared);
            var sequenceComparison = BuildSequenceComparison(runA, runB, runCDuplicate);

            var realIncoming = sequenceComparison.Comparisons[0].MatchResult.SelectedMatches.Single(c => c.CurrentIndex == 0);
            var outgoingAlternative = sequenceComparison.Comparisons[1].MatchResult.AlternativeCandidates.Single(a => a.PreviousIndex == 0);

            var link = CreateLinkViaReflection(0, 0, runB, occShared, realIncoming, outgoingAlternative);

            Assert.Throws<ArgumentException>(() => CreateResultViaReflection(sequenceComparison, new[] { link }));
        }

        // ===================== rejection: shared run =====================

        [Fact]
        public void Result_RejectsWrongSharedRunReference()
        {
            var occ = MakeOccurrence("shared");
            var runA = MakeRun("run-A", occ);
            var runB = MakeRun("run-B", occ);
            var runC = MakeRun("run-C", occ);
            var sequenceComparison = BuildSequenceComparison(runA, runB, runC);
            var link = ProjectContinuity(sequenceComparison).Links[0];

            var differentRun = MakeRun("run-B-different", DefaultCreatedAt.AddDays(1), occ);
            CorruptLinkField(link, nameof(SelectedMatchContinuityLink.SharedRun), differentRun);

            Assert.Throws<ArgumentException>(() => CreateResultViaReflection(sequenceComparison, new[] { link }));
        }

        [Fact]
        public void Result_RejectsSameRunIdAndCreatedAtDifferentSharedRunReference()
        {
            var occ = MakeOccurrence("shared");
            var runA = MakeRun("run-A", occ);
            var runB = MakeRun("run-B", occ);
            var runC = MakeRun("run-C", occ);
            var sequenceComparison = BuildSequenceComparison(runA, runB, runC);
            var link = ProjectContinuity(sequenceComparison).Links[0];

            // Same RunId and CreatedAt as runB, but a distinct CoordinationRun instance.
            var runBSameIdentity = MakeRun("run-B", DefaultCreatedAt, occ);
            Assert.Equal(runB.RunId, runBSameIdentity.RunId);
            Assert.Equal(runB.CreatedAt, runBSameIdentity.CreatedAt);
            Assert.NotSame(runB, runBSameIdentity);

            CorruptLinkField(link, nameof(SelectedMatchContinuityLink.SharedRun), runBSameIdentity);

            Assert.Throws<ArgumentException>(() => CreateResultViaReflection(sequenceComparison, new[] { link }));
        }

        // ===================== rejection: shared slot / occurrence =====================

        [Fact]
        public void Result_RejectsWrongSharedOccurrenceSlot()
        {
            var occ = MakeOccurrence("shared");
            var decoy = MakeOccurrence("decoy");
            var runA = MakeRun("run-A", occ);
            var runB = MakeRun("run-B", occ, decoy); // slot 0 = shared, slot 1 = decoy (unmatched)
            var runC = MakeRun("run-C", occ);
            var sequenceComparison = BuildSequenceComparison(runA, runB, runC);
            var link = ProjectContinuity(sequenceComparison).Links[0];

            // Point the link at slot 1 (decoy), a valid index but the wrong slot for this link's occurrence.
            CorruptLinkField(link, nameof(SelectedMatchContinuityLink.SharedOccurrenceIndex), 1);

            Assert.Throws<ArgumentException>(() => CreateResultViaReflection(sequenceComparison, new[] { link }));
        }

        [Fact]
        public void Result_RejectsSameValueShapedOccurrenceDifferentReference()
        {
            var occ = MakeOccurrence("shared");
            var valueShapedTwin = MakeOccurrence("shared");
            Assert.NotSame(occ, valueShapedTwin);

            var runA = MakeRun("run-A", occ);
            var runB = MakeRun("run-B", occ);
            var runC = MakeRun("run-C", occ);
            var sequenceComparison = BuildSequenceComparison(runA, runB, runC);
            var link = ProjectContinuity(sequenceComparison).Links[0];

            CorruptLinkField(link, nameof(SelectedMatchContinuityLink.SharedOccurrence), valueShapedTwin);

            Assert.Throws<ArgumentException>(() => CreateResultViaReflection(sequenceComparison, new[] { link }));
        }

        [Fact]
        public void Result_RejectsIncomingCurrentSlotMismatch()
        {
            var occ = MakeOccurrence("shared");
            var runA = MakeRun("run-A", occ);
            var runB = MakeRun("run-B", occ);
            var runC = MakeRun("run-C", occ);
            var sequenceComparison = BuildSequenceComparison(runA, runB, runC);
            var link = ProjectContinuity(sequenceComparison).Links[0];

            // Mutate the SAME incoming candidate object's own CurrentIndex, after the link was already validly
            // built -- this can never occur through normal construction, since the link's own constructor checks
            // this invariant at build time. It proves the result revalidates independently.
            CorruptCandidateField(link.IncomingSelectedMatch, nameof(ClashRunMatchCandidate.CurrentIndex), 7);

            Assert.Throws<ArgumentException>(() => CreateResultViaReflection(sequenceComparison, new[] { link }));
        }

        [Fact]
        public void Result_RejectsOutgoingPreviousSlotMismatch()
        {
            var occ = MakeOccurrence("shared");
            var runA = MakeRun("run-A", occ);
            var runB = MakeRun("run-B", occ);
            var runC = MakeRun("run-C", occ);
            var sequenceComparison = BuildSequenceComparison(runA, runB, runC);
            var link = ProjectContinuity(sequenceComparison).Links[0];

            CorruptCandidateField(link.OutgoingSelectedMatch, nameof(ClashRunMatchCandidate.PreviousIndex), 7);

            Assert.Throws<ArgumentException>(() => CreateResultViaReflection(sequenceComparison, new[] { link }));
        }

        [Fact]
        public void Result_RejectsIncomingOccurrenceReferenceMismatch()
        {
            var occ = MakeOccurrence("shared");
            var foreignOccurrence = MakeOccurrence("foreign");
            var runA = MakeRun("run-A", occ);
            var runB = MakeRun("run-B", occ);
            var runC = MakeRun("run-C", occ);
            var sequenceComparison = BuildSequenceComparison(runA, runB, runC);
            var link = ProjectContinuity(sequenceComparison).Links[0];

            // Mutate the incoming candidate's own Assessment.CurrentOccurrence, after link construction.
            CorruptAssessmentField(link.IncomingSelectedMatch.Assessment, nameof(ClashMatchAssessment.CurrentOccurrence), foreignOccurrence);

            Assert.Throws<ArgumentException>(() => CreateResultViaReflection(sequenceComparison, new[] { link }));
        }

        [Fact]
        public void Result_RejectsOutgoingOccurrenceReferenceMismatch()
        {
            var occ = MakeOccurrence("shared");
            var foreignOccurrence = MakeOccurrence("foreign");
            var runA = MakeRun("run-A", occ);
            var runB = MakeRun("run-B", occ);
            var runC = MakeRun("run-C", occ);
            var sequenceComparison = BuildSequenceComparison(runA, runB, runC);
            var link = ProjectContinuity(sequenceComparison).Links[0];

            CorruptAssessmentField(link.OutgoingSelectedMatch.Assessment, nameof(ClashMatchAssessment.PreviousOccurrence), foreignOccurrence);

            Assert.Throws<ArgumentException>(() => CreateResultViaReflection(sequenceComparison, new[] { link }));
        }

        // ===================== rejection: complete projection / order =====================

        [Fact]
        public void Result_RejectsMissingExpectedLink()
        {
            var occ0 = MakeOccurrence("slot0");
            var occ1 = MakeOccurrence("slot1");
            var runA = MakeRun("run-A", occ0, occ1);
            var runB = MakeRun("run-B", occ0, occ1);
            var runC = MakeRun("run-C", occ0, occ1);
            var sequenceComparison = BuildSequenceComparison(runA, runB, runC);
            var realLinks = ProjectContinuity(sequenceComparison).Links;
            Assert.Equal(2, realLinks.Count);

            Assert.Throws<ArgumentException>(() => CreateResultViaReflection(sequenceComparison, new[] { realLinks[0] }));
        }

        [Fact]
        public void Result_RejectsExtraLink()
        {
            var occ = MakeOccurrence("shared");
            var runA = MakeRun("run-A", occ);
            var runB = MakeRun("run-B", occ);
            var runC = MakeRun("run-C", occ);
            var sequenceComparison = BuildSequenceComparison(runA, runB, runC);
            var realLink = ProjectContinuity(sequenceComparison).Links[0];

            // A second, structurally distinct (freshly computed) but equally "valid in isolation" link for the
            // exact same boundary/slot.
            var freshIncoming = CreateMatchResult(runA, runB).SelectedMatches[0];
            var freshOutgoing = CreateMatchResult(runB, runC).SelectedMatches[0];
            var freshLink = CreateLinkViaReflection(0, 0, runB, occ, freshIncoming, freshOutgoing);

            Assert.Throws<ArgumentException>(() => CreateResultViaReflection(sequenceComparison, new[] { realLink, freshLink }));
        }

        [Fact]
        public void Result_RejectsDuplicateLink()
        {
            var occ = MakeOccurrence("shared");
            var runA = MakeRun("run-A", occ);
            var runB = MakeRun("run-B", occ);
            var runC = MakeRun("run-C", occ);
            var sequenceComparison = BuildSequenceComparison(runA, runB, runC);
            var realLink = ProjectContinuity(sequenceComparison).Links[0];

            Assert.Throws<ArgumentException>(() => CreateResultViaReflection(sequenceComparison, new[] { realLink, realLink }));
        }

        [Fact]
        public void Result_RejectsWrongBoundaryOrder()
        {
            var occ = MakeOccurrence("shared");
            var sequenceComparison = BuildSequenceComparison(
                MakeRun("run-A", occ), MakeRun("run-B", occ), MakeRun("run-C", occ), MakeRun("run-D", occ));
            var realLinks = ProjectContinuity(sequenceComparison).Links;
            Assert.Equal(2, realLinks.Count);

            Assert.Throws<ArgumentException>(() =>
                CreateResultViaReflection(sequenceComparison, new[] { realLinks[1], realLinks[0] }));
        }

        [Fact]
        public void Result_RejectsWrongSharedSlotOrderWithinBoundary()
        {
            var occ0 = MakeOccurrence("slot0");
            var occ1 = MakeOccurrence("slot1");
            var runA = MakeRun("run-A", occ0, occ1);
            var runB = MakeRun("run-B", occ0, occ1);
            var runC = MakeRun("run-C", occ0, occ1);
            var sequenceComparison = BuildSequenceComparison(runA, runB, runC);
            var realLinks = ProjectContinuity(sequenceComparison).Links;
            Assert.Equal(2, realLinks.Count);
            Assert.Equal(0, realLinks[0].SharedOccurrenceIndex);
            Assert.Equal(1, realLinks[1].SharedOccurrenceIndex);

            Assert.Throws<ArgumentException>(() =>
                CreateResultViaReflection(sequenceComparison, new[] { realLinks[1], realLinks[0] }));
        }

        [Fact]
        public void Result_AcceptsCanonicalOrder()
        {
            var occ0 = MakeOccurrence("slot0");
            var occ1 = MakeOccurrence("slot1");
            var runA = MakeRun("run-A", occ0, occ1);
            var runB = MakeRun("run-B", occ0, occ1);
            var runC = MakeRun("run-C", occ0, occ1);
            var sequenceComparison = BuildSequenceComparison(runA, runB, runC);
            var realLinks = ProjectContinuity(sequenceComparison).Links;

            var result = CreateResultViaReflection(sequenceComparison, new[] { realLinks[0], realLinks[1] });

            Assert.Equal(2, result.Links.Count);
            Assert.Equal(0, result.Links[0].SharedOccurrenceIndex);
            Assert.Equal(1, result.Links[1].SharedOccurrenceIndex);
        }

        // ===================== defensive copies / immutability =====================

        [Fact]
        public void Result_ConstructorLinksCollectionMutation_DoesNotAffectResult()
        {
            var occ = MakeOccurrence("shared");
            var runA = MakeRun("run-A", occ);
            var runB = MakeRun("run-B", occ);
            var runC = MakeRun("run-C", occ);
            var sequenceComparison = BuildSequenceComparison(runA, runB, runC);
            var realLink = ProjectContinuity(sequenceComparison).Links[0];
            var linksList = new List<SelectedMatchContinuityLink> { realLink };

            var result = CreateResultViaReflection(sequenceComparison, linksList);
            linksList.Clear();

            Assert.Single(result.Links);
        }

        [Fact]
        public void Result_LinksCannotBeMutatedThroughIList()
        {
            var occ = MakeOccurrence("shared");
            var runA = MakeRun("run-A", occ);
            var runB = MakeRun("run-B", occ);
            var runC = MakeRun("run-C", occ);
            var sequenceComparison = BuildSequenceComparison(runA, runB, runC);
            var realLink = ProjectContinuity(sequenceComparison).Links[0];

            var result = CreateResultViaReflection(sequenceComparison, new[] { realLink });

            var mutableView = Assert.IsAssignableFrom<IList<SelectedMatchContinuityLink>>(result.Links);
            Assert.Throws<NotSupportedException>(() => mutableView.Add(realLink));
        }

        [Fact]
        public void Result_SequenceComparisonExactReferencePreserved()
        {
            var occ = MakeOccurrence("shared");
            var runA = MakeRun("run-A", occ);
            var runB = MakeRun("run-B", occ);
            var runC = MakeRun("run-C", occ);
            var sequenceComparison = BuildSequenceComparison(runA, runB, runC);
            var realLink = ProjectContinuity(sequenceComparison).Links[0];

            var result = CreateResultViaReflection(sequenceComparison, new[] { realLink });

            Assert.Same(sequenceComparison, result.SequenceComparison);
        }

        [Fact]
        public void Result_LinkReferencesPreservedExactly()
        {
            var occ = MakeOccurrence("shared");
            var runA = MakeRun("run-A", occ);
            var runB = MakeRun("run-B", occ);
            var runC = MakeRun("run-C", occ);
            var sequenceComparison = BuildSequenceComparison(runA, runB, runC);
            var realLink = ProjectContinuity(sequenceComparison).Links[0];

            var result = CreateResultViaReflection(sequenceComparison, new[] { realLink });

            Assert.Same(realLink, result.Links[0]);
        }

        [Fact]
        public void Result_ToString_ExactExample()
        {
            var occ = MakeOccurrence("shared");
            var runA = MakeRun("run-A", occ);
            var runB = MakeRun("run-B", occ);
            var runC = MakeRun("run-C", occ);
            var sequenceComparison = BuildSequenceComparison(runA, runB, runC);

            var continuity = ProjectContinuity(sequenceComparison);

            Assert.Equal("3 runs, 2 adjacent comparisons, 1 selected-match continuity link(s)", continuity.ToString());
        }
    }
}
