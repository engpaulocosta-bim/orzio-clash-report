using System;
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
    /// Tests for <see cref="ClashRunSequenceLifecycleEntryPresentation"/> invariants, invoking its internal
    /// constructor through reflection (no InternalsVisibleTo, no visibility increase). Comparisons, lifecycle
    /// entries, and continuity paths are produced through the real matcher/run-comparer/lifecycle-
    /// classifier/sequence-comparer/continuity-projector/path-assembler pipeline, never hand-built.
    /// </summary>
    public class ClashRunSequenceLifecycleEntryPresentationTests
    {
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

        private static ClashRunSequenceLifecycleEntryPresentation CreateEntryViaReflection(
            int comparisonIndex, int entryIndex, ClashLifecycleResult? comparison, ClashLifecycleEntry? lifecycleEntry,
            SelectedMatchContinuityPath? continuityPath)
        {
            try
            {
                return (ClashRunSequenceLifecycleEntryPresentation)EntryConstructor.Invoke(
                    new object?[] { comparisonIndex, entryIndex, comparison, lifecycleEntry, continuityPath });
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

        /// <summary>Two runs sharing one occurrence: one comparison, one StillOpen selected-match entry.</summary>
        private static ClashLifecycleResult CreateStillOpenComparison()
        {
            var occ = MakeOccurrence("shared");
            return BuildSequenceComparison(MakeRun("run-A", occ), MakeRun("run-B", occ)).Comparisons[0];
        }

        /// <summary>Four-run chain producing one continuity path spanning all three comparisons.</summary>
        private static (ClashRunSequenceComparisonResult SequenceComparison, SelectedMatchContinuityPath Path) CreateChainWithPath()
        {
            var occ = MakeOccurrence("shared");
            var sequenceComparison = BuildSequenceComparison(
                MakeRun("run-A", occ), MakeRun("run-B", occ), MakeRun("run-C", occ), MakeRun("run-D", occ));
            var continuityResult = new DeterministicSelectedMatchContinuityProjector().Project(sequenceComparison);
            var pathsResult = new DeterministicSelectedMatchContinuityPathAssembler().Assemble(continuityResult);
            return (sequenceComparison, pathsResult.Paths[0]);
        }

        // ===================== index validation =====================

        [Fact]
        public void Constructor_RejectsNegativeComparisonIndex()
        {
            var comparison = CreateStillOpenComparison();
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                CreateEntryViaReflection(-1, 0, comparison, comparison.Entries[0], null));
        }

        [Fact]
        public void Constructor_RejectsNegativeEntryIndex()
        {
            var comparison = CreateStillOpenComparison();
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                CreateEntryViaReflection(0, -1, comparison, comparison.Entries[0], null));
        }

        [Fact]
        public void Constructor_RejectsNullComparison()
        {
            var comparison = CreateStillOpenComparison();
            Assert.Throws<ArgumentNullException>(() =>
                CreateEntryViaReflection(0, 0, null, comparison.Entries[0], null));
        }

        [Fact]
        public void Constructor_RejectsNullLifecycleEntry()
        {
            var comparison = CreateStillOpenComparison();
            Assert.Throws<ArgumentNullException>(() =>
                CreateEntryViaReflection(0, 0, comparison, null, null));
        }

        [Fact]
        public void Constructor_RejectsEntryIndexOutOfRange()
        {
            var comparison = CreateStillOpenComparison();
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                CreateEntryViaReflection(0, comparison.Entries.Count, comparison, comparison.Entries[0], null));
        }

        // ===================== exact reference validation =====================

        [Fact]
        public void Constructor_RejectsLifecycleEntryFromDifferentIndex()
        {
            var occ0 = MakeOccurrence("slot0");
            var occ1 = MakeOccurrence("slot1");
            var comparison = BuildSequenceComparison(MakeRun("run-A", occ0, occ1), MakeRun("run-B", occ0, occ1)).Comparisons[0];
            Assert.True(comparison.Entries.Count >= 2);

            Assert.Throws<ArgumentException>(() =>
                CreateEntryViaReflection(0, 0, comparison, comparison.Entries[1], null));
        }

        [Fact]
        public void Constructor_RejectsValueEquivalentDistinctLifecycleEntry()
        {
            var comparisonA = CreateStillOpenComparison();
            var comparisonB = CreateStillOpenComparison(); // independent, value-equivalent, distinct instance
            Assert.NotSame(comparisonA.Entries[0], comparisonB.Entries[0]);

            Assert.Throws<ArgumentException>(() =>
                CreateEntryViaReflection(0, 0, comparisonA, comparisonB.Entries[0], null));
        }

        [Fact]
        public void Constructor_AcceptsValidEntryWithNullContinuityPath()
        {
            var comparison = CreateStillOpenComparison();

            var presentation = CreateEntryViaReflection(0, 0, comparison, comparison.Entries[0], null);

            Assert.Equal(0, presentation.ComparisonIndex);
            Assert.Equal(0, presentation.EntryIndex);
            Assert.Same(comparison, presentation.Comparison);
            Assert.Same(comparison.Entries[0], presentation.LifecycleEntry);
            Assert.Null(presentation.ContinuityPath);
            Assert.False(presentation.IsInContinuityPath);
        }

        // ===================== continuity path validation =====================

        [Fact]
        public void Constructor_RejectsContinuityPathWhenSelectedMatchIsNull()
        {
            var occShared = MakeOccurrence("shared");
            var occUnrelated = MakeOccurrence("unrelated");
            var comparison = BuildSequenceComparison(MakeRun("run-A", occShared), MakeRun("run-B", occUnrelated)).Comparisons[0];
            // Both sides are unmatched here (occShared -> Resolved, occUnrelated -> New); either works for this test.
            var unmatchedEntry = comparison.Entries.First(e => e.SelectedMatch == null);

            var chain = CreateChainWithPath();

            Assert.Throws<ArgumentException>(() =>
                CreateEntryViaReflection(0, comparison.Entries.ToList().IndexOf(unmatchedEntry), comparison, unmatchedEntry, chain.Path));
        }

        [Fact]
        public void Constructor_RejectsContinuityPathWhenSelectedMatchNotMemberOfPath()
        {
            var occ = MakeOccurrence("unrelated-pair");
            var foreignComparison = BuildSequenceComparison(MakeRun("run-X", occ), MakeRun("run-Y", occ)).Comparisons[0];
            var foreignEntry = foreignComparison.Entries.Single(e => e.SelectedMatch != null);

            var chain = CreateChainWithPath();

            Assert.Throws<ArgumentException>(() =>
                CreateEntryViaReflection(0, 0, foreignComparison, foreignEntry, chain.Path));
        }

        [Fact]
        public void Constructor_AcceptsValidContinuityPathWhenSelectedMatchIsMember()
        {
            var chain = CreateChainWithPath();
            var comparison0 = chain.SequenceComparison.Comparisons[0];
            var entry = comparison0.Entries.Single(e => e.SelectedMatch != null);
            Assert.Same(chain.Path.SelectedMatches[0], entry.SelectedMatch);

            var presentation = CreateEntryViaReflection(0, comparison0.Entries.ToList().IndexOf(entry), comparison0, entry, chain.Path);

            Assert.Same(chain.Path, presentation.ContinuityPath);
            Assert.True(presentation.IsInContinuityPath);
        }

        // ===================== no persistent identity =====================

        [Fact]
        public void Presentation_ExposesNoIdStatusFingerprintOrAggregatedConfidenceProperties()
        {
            var forbiddenNames = new[]
            {
                "Id", "StableId", "PersistentId", "ClashId", "LedgerId", "TrackId", "Fingerprint", "Status", "Confidence",
                "AggregatedConfidence", "Reopened",
            };

            var properties = typeof(ClashRunSequenceLifecycleEntryPresentation)
                .GetProperties(BindingFlags.Public | BindingFlags.Instance).Select(p => p.Name).ToList();

            foreach (var forbidden in forbiddenNames)
            {
                Assert.DoesNotContain(forbidden, properties);
            }
        }

        // ===================== ToString =====================

        [Fact]
        public void ToString_ReflectsContinuityPathPresence()
        {
            var comparison = CreateStillOpenComparison();
            var withoutPath = CreateEntryViaReflection(0, 0, comparison, comparison.Entries[0], null);

            Assert.DoesNotContain("continuity path", withoutPath.ToString());

            var chain = CreateChainWithPath();
            var comparison0 = chain.SequenceComparison.Comparisons[0];
            var entry = comparison0.Entries.Single(e => e.SelectedMatch != null);
            var withPath = CreateEntryViaReflection(0, comparison0.Entries.ToList().IndexOf(entry), comparison0, entry, chain.Path);

            Assert.Contains("continuity path", withPath.ToString());
        }
    }
}
