using System;
using System.Reflection;
using System.Runtime.ExceptionServices;
using OrzioClashReport.Core.Abstractions;
using OrzioClashReport.Core.Matching;
using OrzioClashReport.Core.Model;

namespace OrzioClashReport.Tests
{
    /// <summary>
    /// Tests for <see cref="SelectedMatchContinuityLink"/> invariants, invoking its internal constructor through
    /// reflection (no InternalsVisibleTo, no visibility increase). Selected matches used to build valid links
    /// are produced through the real matcher/run-comparer pipeline, never hand-built.
    /// </summary>
    public class SelectedMatchContinuityLinkTests
    {
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
            int incomingComparisonIndex,
            int sharedOccurrenceIndex,
            CoordinationRun? sharedRun,
            ClashOccurrence? sharedOccurrence,
            ClashRunMatchCandidate? incomingSelectedMatch,
            ClashRunMatchCandidate? outgoingSelectedMatch)
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

        // ===================== acceptance =====================

        [Fact]
        public void Link_ValidLink_IsAccepted()
        {
            var sharedOccurrence = MakeOccurrence("shared");
            var runA = MakeRun("run-A", sharedOccurrence);
            var runB = MakeRun("run-B", sharedOccurrence);
            var runC = MakeRun("run-C", sharedOccurrence);
            var incoming = CreateMatchResult(runA, runB).SelectedMatches[0];
            var outgoing = CreateMatchResult(runB, runC).SelectedMatches[0];

            var link = CreateLinkViaReflection(0, 0, runB, sharedOccurrence, incoming, outgoing);

            Assert.Equal(0, link.IncomingComparisonIndex);
            Assert.Equal(0, link.SharedOccurrenceIndex);
        }

        [Fact]
        public void Link_ExactPropertyReferencesArePreserved()
        {
            var sharedOccurrence = MakeOccurrence("shared");
            var runA = MakeRun("run-A", sharedOccurrence);
            var runB = MakeRun("run-B", sharedOccurrence);
            var runC = MakeRun("run-C", sharedOccurrence);
            var incoming = CreateMatchResult(runA, runB).SelectedMatches[0];
            var outgoing = CreateMatchResult(runB, runC).SelectedMatches[0];

            var link = CreateLinkViaReflection(0, 0, runB, sharedOccurrence, incoming, outgoing);

            Assert.Same(runB, link.SharedRun);
            Assert.Same(sharedOccurrence, link.SharedOccurrence);
            Assert.Same(incoming, link.IncomingSelectedMatch);
            Assert.Same(outgoing, link.OutgoingSelectedMatch);
        }

        [Fact]
        public void Link_OutgoingComparisonIndex_IsDerivedAsIncomingPlusOne()
        {
            var sharedOccurrence = MakeOccurrence("shared");
            var runA = MakeRun("run-A", sharedOccurrence);
            var runB = MakeRun("run-B", sharedOccurrence);
            var runC = MakeRun("run-C", sharedOccurrence);
            var incoming = CreateMatchResult(runA, runB).SelectedMatches[0];
            var outgoing = CreateMatchResult(runB, runC).SelectedMatches[0];

            var link = CreateLinkViaReflection(3, 0, runB, sharedOccurrence, incoming, outgoing);

            Assert.Equal(4, link.OutgoingComparisonIndex);
        }

        [Fact]
        public void Link_SharedRunIndex_IsDerivedAsIncomingPlusOne()
        {
            var sharedOccurrence = MakeOccurrence("shared");
            var runA = MakeRun("run-A", sharedOccurrence);
            var runB = MakeRun("run-B", sharedOccurrence);
            var runC = MakeRun("run-C", sharedOccurrence);
            var incoming = CreateMatchResult(runA, runB).SelectedMatches[0];
            var outgoing = CreateMatchResult(runB, runC).SelectedMatches[0];

            var link = CreateLinkViaReflection(3, 0, runB, sharedOccurrence, incoming, outgoing);

            Assert.Equal(4, link.SharedRunIndex);
        }

        // ===================== rejection: basic argument validation =====================

        [Fact]
        public void Link_RejectsNegativeIncomingComparisonIndex()
        {
            var sharedOccurrence = MakeOccurrence("shared");
            var runA = MakeRun("run-A", sharedOccurrence);
            var runB = MakeRun("run-B", sharedOccurrence);
            var runC = MakeRun("run-C", sharedOccurrence);
            var incoming = CreateMatchResult(runA, runB).SelectedMatches[0];
            var outgoing = CreateMatchResult(runB, runC).SelectedMatches[0];

            Assert.Throws<ArgumentOutOfRangeException>(() =>
                CreateLinkViaReflection(-1, 0, runB, sharedOccurrence, incoming, outgoing));
        }

        [Fact]
        public void Link_RejectsNegativeSharedOccurrenceIndex()
        {
            var sharedOccurrence = MakeOccurrence("shared");
            var runA = MakeRun("run-A", sharedOccurrence);
            var runB = MakeRun("run-B", sharedOccurrence);
            var runC = MakeRun("run-C", sharedOccurrence);
            var incoming = CreateMatchResult(runA, runB).SelectedMatches[0];
            var outgoing = CreateMatchResult(runB, runC).SelectedMatches[0];

            Assert.Throws<ArgumentOutOfRangeException>(() =>
                CreateLinkViaReflection(0, -1, runB, sharedOccurrence, incoming, outgoing));
        }

        [Fact]
        public void Link_RejectsNullSharedRun()
        {
            var sharedOccurrence = MakeOccurrence("shared");
            var runA = MakeRun("run-A", sharedOccurrence);
            var runB = MakeRun("run-B", sharedOccurrence);
            var runC = MakeRun("run-C", sharedOccurrence);
            var incoming = CreateMatchResult(runA, runB).SelectedMatches[0];
            var outgoing = CreateMatchResult(runB, runC).SelectedMatches[0];

            Assert.Throws<ArgumentNullException>(() =>
                CreateLinkViaReflection(0, 0, null, sharedOccurrence, incoming, outgoing));
        }

        [Fact]
        public void Link_RejectsNullSharedOccurrence()
        {
            var sharedOccurrence = MakeOccurrence("shared");
            var runA = MakeRun("run-A", sharedOccurrence);
            var runB = MakeRun("run-B", sharedOccurrence);
            var runC = MakeRun("run-C", sharedOccurrence);
            var incoming = CreateMatchResult(runA, runB).SelectedMatches[0];
            var outgoing = CreateMatchResult(runB, runC).SelectedMatches[0];

            Assert.Throws<ArgumentNullException>(() =>
                CreateLinkViaReflection(0, 0, runB, null, incoming, outgoing));
        }

        [Fact]
        public void Link_RejectsNullIncomingSelectedMatch()
        {
            var sharedOccurrence = MakeOccurrence("shared");
            var runB = MakeRun("run-B", sharedOccurrence);
            var runC = MakeRun("run-C", sharedOccurrence);
            var outgoing = CreateMatchResult(runB, runC).SelectedMatches[0];

            Assert.Throws<ArgumentNullException>(() =>
                CreateLinkViaReflection(0, 0, runB, sharedOccurrence, null, outgoing));
        }

        [Fact]
        public void Link_RejectsNullOutgoingSelectedMatch()
        {
            var sharedOccurrence = MakeOccurrence("shared");
            var runA = MakeRun("run-A", sharedOccurrence);
            var runB = MakeRun("run-B", sharedOccurrence);
            var incoming = CreateMatchResult(runA, runB).SelectedMatches[0];

            Assert.Throws<ArgumentNullException>(() =>
                CreateLinkViaReflection(0, 0, runB, sharedOccurrence, incoming, null));
        }

        // ===================== rejection: slot / occurrence continuity =====================

        [Fact]
        public void Link_RejectsIncomingCurrentIndexMismatch()
        {
            // incoming's real CurrentIndex is 0 (matches occShared at runB slot 0); claiming sharedOccurrenceIndex 1 mismatches it.
            var occAShared = MakeOccurrence("shared");
            var occShared = MakeOccurrence("shared");
            var occOther = MakeOccurrence("other");
            var runA = MakeRun("run-A", occAShared);
            var runB = MakeRun("run-B", occShared, occOther);
            var runC = MakeRun("run-C", occOther);
            var incoming = CreateMatchResult(runA, runB).SelectedMatches[0]; // CurrentIndex 0
            var outgoing = CreateMatchResult(runB, runC).SelectedMatches[0]; // PreviousIndex 1

            Assert.Throws<ArgumentException>(() =>
                CreateLinkViaReflection(0, 1, runB, occOther, incoming, outgoing));
        }

        [Fact]
        public void Link_RejectsOutgoingPreviousIndexMismatch()
        {
            // incoming's real CurrentIndex is 1 (matches occShared at runB slot 1) -- sharedOccurrenceIndex 1
            // satisfies the incoming check. The outgoing match instead comes from a distinct run object
            // (runBAlt) that holds the exact same occShared reference at slot 0, so its real PreviousIndex (0)
            // disagrees with the claimed shared slot (1), isolating the outgoing check.
            var occDecoy = MakeOccurrence("decoy");
            var occShared = MakeOccurrence("shared");
            var runA = MakeRun("run-A", occDecoy, occShared);
            var runB = MakeRun("run-B", occDecoy, occShared); // occShared at index 1
            var runBAlt = MakeRun("run-B-alt", occShared, MakeOccurrence("decoy2")); // occShared at index 0
            var runC = MakeRun("run-C", MakeOccurrence("shared"));

            var incoming = System.Linq.Enumerable.Single(CreateMatchResult(runA, runB).SelectedMatches, c => c.CurrentIndex == 1);
            var outgoing = System.Linq.Enumerable.Single(CreateMatchResult(runBAlt, runC).SelectedMatches, c => c.PreviousIndex == 0);

            Assert.Throws<ArgumentException>(() =>
                CreateLinkViaReflection(0, 1, runB, occShared, incoming, outgoing));
        }

        [Fact]
        public void Link_RejectsIncomingCurrentOccurrenceReferenceMismatch()
        {
            var sharedOccurrence = MakeOccurrence("shared");
            var foreignOccurrence = MakeOccurrence("foreign");
            var runA = MakeRun("run-A", sharedOccurrence);
            var runB = MakeRun("run-B", sharedOccurrence);
            var runC = MakeRun("run-C", sharedOccurrence);
            var incoming = CreateMatchResult(runA, runB).SelectedMatches[0]; // CurrentOccurrence is sharedOccurrence
            var outgoing = CreateMatchResult(runB, runC).SelectedMatches[0];

            Assert.Throws<ArgumentException>(() =>
                CreateLinkViaReflection(0, 0, runB, foreignOccurrence, incoming, outgoing));
        }

        [Fact]
        public void Link_RejectsOutgoingPreviousOccurrenceReferenceMismatch()
        {
            var sharedOccurrence = MakeOccurrence("shared");
            var runA = MakeRun("run-A", sharedOccurrence);
            var runB = MakeRun("run-B", sharedOccurrence);
            var incoming = CreateMatchResult(runA, runB).SelectedMatches[0]; // CurrentOccurrence is sharedOccurrence, CurrentIndex 0

            // A genuine outgoing selected match whose PreviousIndex is also 0, but whose real PreviousOccurrence
            // is a distinct object from sharedOccurrence (from an entirely unrelated pairwise comparison).
            var unrelatedShared = MakeOccurrence("unrelated-shared");
            var unrelatedRunB = MakeRun("run-unrelated-B", unrelatedShared);
            var unrelatedRunC = MakeRun("run-unrelated-C", unrelatedShared);
            var unrelatedOutgoing = CreateMatchResult(unrelatedRunB, unrelatedRunC).SelectedMatches[0];

            Assert.Throws<ArgumentException>(() =>
                CreateLinkViaReflection(0, 0, runB, sharedOccurrence, incoming, unrelatedOutgoing));
        }

        [Fact]
        public void Link_RejectsSharedOccurrenceIndexOutOfRange()
        {
            // Both incoming.CurrentIndex and outgoing.PreviousIndex are genuinely 1, and both reference the
            // exact sharedOccurrence instance -- but the claimed sharedRun (runBSmall) has only one occurrence,
            // so index 1 is out of range for it.
            var occDecoy = MakeOccurrence("decoy");
            var sharedOccurrence = MakeOccurrence("shared");
            var runA = MakeRun("run-A", occDecoy, sharedOccurrence);
            var runBReal = MakeRun("run-B-real", occDecoy, sharedOccurrence); // sharedOccurrence at index 1
            var runBSmall = MakeRun("run-B-small", sharedOccurrence); // only 1 occurrence: index 1 is out of range
            var runC = MakeRun("run-C", MakeOccurrence("shared"));

            var incoming = System.Linq.Enumerable.Single(CreateMatchResult(runA, runBReal).SelectedMatches, c => c.CurrentIndex == 1);
            var outgoing = System.Linq.Enumerable.Single(CreateMatchResult(runBReal, runC).SelectedMatches, c => c.PreviousIndex == 1);

            Assert.Throws<ArgumentException>(() =>
                CreateLinkViaReflection(0, 1, runBSmall, sharedOccurrence, incoming, outgoing));
        }

        [Fact]
        public void Link_RejectsSharedRunSlotExactReferenceMismatch()
        {
            var sharedOccurrence = MakeOccurrence("shared");
            var differentOccurrenceAtSameSlot = MakeOccurrence("different-but-same-slot");
            var runA = MakeRun("run-A", sharedOccurrence);
            var runB = MakeRun("run-B", sharedOccurrence);
            var runWrongShared = MakeRun("run-wrong-shared", differentOccurrenceAtSameSlot); // slot 0 holds a different object
            var runC = MakeRun("run-C", sharedOccurrence);

            var incoming = CreateMatchResult(runA, runB).SelectedMatches[0]; // CurrentOccurrence == sharedOccurrence, CurrentIndex 0
            var outgoing = CreateMatchResult(runB, runC).SelectedMatches[0]; // PreviousOccurrence == sharedOccurrence, PreviousIndex 0

            // runWrongShared.Occurrences[0] is differentOccurrenceAtSameSlot, not sharedOccurrence.
            Assert.Throws<ArgumentException>(() =>
                CreateLinkViaReflection(0, 0, runWrongShared, sharedOccurrence, incoming, outgoing));
        }

        [Fact]
        public void Link_RejectsSameValueShapedOccurrenceDifferentReference()
        {
            // Two occurrences with identical clash-test name and element ids, but distinct object references.
            var sharedOccurrence = MakeOccurrence("value-shape");
            var valueShapedTwin = MakeOccurrence("value-shape");
            Assert.NotSame(sharedOccurrence, valueShapedTwin);

            var runA = MakeRun("run-A", sharedOccurrence);
            var runB = MakeRun("run-B", sharedOccurrence);
            var runC = MakeRun("run-C", sharedOccurrence);
            var incoming = CreateMatchResult(runA, runB).SelectedMatches[0];
            var outgoing = CreateMatchResult(runB, runC).SelectedMatches[0];

            // valueShapedTwin is not the exact instance held by runB at index 0, even though it is value-identical.
            Assert.Throws<ArgumentException>(() =>
                CreateLinkViaReflection(0, 0, runB, valueShapedTwin, incoming, outgoing));
        }

        [Fact]
        public void Link_ToString_ExactExample()
        {
            // Build a run with 6 occurrences where only slot 5 is content-identical across A/B/C, so exactly
            // one selected match forms, landing at CurrentIndex/PreviousIndex 5.
            var runAOccurrences = new[]
            {
                MakeOccurrence("a-only-0"), MakeOccurrence("a-only-1"), MakeOccurrence("a-only-2"),
                MakeOccurrence("a-only-3"), MakeOccurrence("a-only-4"), MakeOccurrence("shared"),
            };
            var runBOccurrences = new[]
            {
                MakeOccurrence("b-only-0"), MakeOccurrence("b-only-1"), MakeOccurrence("b-only-2"),
                MakeOccurrence("b-only-3"), MakeOccurrence("b-only-4"), MakeOccurrence("shared"),
            };
            var runCOccurrences = new[]
            {
                MakeOccurrence("c-only-0"), MakeOccurrence("c-only-1"), MakeOccurrence("c-only-2"),
                MakeOccurrence("c-only-3"), MakeOccurrence("c-only-4"), MakeOccurrence("shared"),
            };

            var runA = MakeRun("run-A", runAOccurrences);
            var runB = MakeRun("run-B", runBOccurrences);
            var runC = MakeRun("run-C", runCOccurrences);

            var incoming = CreateMatchResult(runA, runB).SelectedMatches[0];
            var outgoing = CreateMatchResult(runB, runC).SelectedMatches[0];
            Assert.Equal(5, incoming.CurrentIndex);
            Assert.Equal(5, outgoing.PreviousIndex);

            var link = CreateLinkViaReflection(0, 5, runB, runBOccurrences[5], incoming, outgoing);

            Assert.Equal("comparisons[0->1] via run[1].occurrence[5]", link.ToString());
        }
    }
}
