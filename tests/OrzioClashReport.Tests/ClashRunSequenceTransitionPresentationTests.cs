using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.ExceptionServices;
using OrzioClashReport.Core.Abstractions;
using OrzioClashReport.Core.Lifecycle;
using OrzioClashReport.Core.Matching;
using OrzioClashReport.Core.Model;

namespace OrzioClashReport.Tests
{
    /// <summary>
    /// Tests for <see cref="ClashRunSequenceTransitionPresentation"/> invariants, invoking its internal
    /// constructor through reflection (no InternalsVisibleTo, no visibility increase). Comparisons and their
    /// lifecycle entry presentations are produced through the real matcher/run-comparer/lifecycle-classifier
    /// pipeline plus <see cref="ClashRunSequenceLifecycleEntryPresentation"/>'s own internal constructor, never
    /// hand-built.
    /// </summary>
    public class ClashRunSequenceTransitionPresentationTests
    {
        private static readonly ConstructorInfo TransitionConstructor =
            typeof(ClashRunSequenceTransitionPresentation).GetConstructor(
                BindingFlags.Instance | BindingFlags.NonPublic,
                binder: null,
                types: new[]
                {
                    typeof(int), typeof(ClashLifecycleResult), typeof(IReadOnlyList<ClashRunSequenceLifecycleEntryPresentation>),
                },
                modifiers: null)
            ?? throw new InvalidOperationException("Could not find ClashRunSequenceTransitionPresentation internal constructor.");

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

        private static ClashRunSequenceTransitionPresentation CreateTransitionViaReflection(
            int comparisonIndex, ClashLifecycleResult? comparison, IReadOnlyList<ClashRunSequenceLifecycleEntryPresentation>? lifecycleEntries)
        {
            try
            {
                return (ClashRunSequenceTransitionPresentation)TransitionConstructor.Invoke(
                    new object?[] { comparisonIndex, comparison, lifecycleEntries });
            }
            catch (TargetInvocationException ex) when (ex.InnerException != null)
            {
                ExceptionDispatchInfo.Capture(ex.InnerException).Throw();
                throw;
            }
        }

        private static ClashRunSequenceLifecycleEntryPresentation CreateEntryViaReflection(
            int comparisonIndex, int entryIndex, ClashLifecycleResult comparison, ClashLifecycleEntry lifecycleEntry) =>
            (ClashRunSequenceLifecycleEntryPresentation)EntryConstructor.Invoke(
                new object?[] { comparisonIndex, entryIndex, comparison, lifecycleEntry, null });

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

        /// <summary>Two occurrences shared: one comparison with two selected-match entries at indices 0 and 1.</summary>
        private static ClashLifecycleResult CreateTwoEntryComparison()
        {
            var occ0 = MakeOccurrence("slot0");
            var occ1 = MakeOccurrence("slot1");
            return BuildSequenceComparison(MakeRun("run-A", occ0, occ1), MakeRun("run-B", occ0, occ1)).Comparisons[0];
        }

        private static List<ClashRunSequenceLifecycleEntryPresentation> BuildValidEntries(int comparisonIndex, ClashLifecycleResult comparison)
        {
            var entries = new List<ClashRunSequenceLifecycleEntryPresentation>();
            for (int i = 0; i < comparison.Entries.Count; i++)
            {
                entries.Add(CreateEntryViaReflection(comparisonIndex, i, comparison, comparison.Entries[i]));
            }

            return entries;
        }

        // ===================== basic validation =====================

        [Fact]
        public void Constructor_RejectsNegativeComparisonIndex()
        {
            var comparison = CreateTwoEntryComparison();
            var entries = BuildValidEntries(0, comparison);

            Assert.Throws<ArgumentOutOfRangeException>(() => CreateTransitionViaReflection(-1, comparison, entries));
        }

        [Fact]
        public void Constructor_RejectsNullComparison()
        {
            var comparison = CreateTwoEntryComparison();
            var entries = BuildValidEntries(0, comparison);

            Assert.Throws<ArgumentNullException>(() => CreateTransitionViaReflection(0, null, entries));
        }

        [Fact]
        public void Constructor_RejectsNullLifecycleEntries()
        {
            var comparison = CreateTwoEntryComparison();

            Assert.Throws<ArgumentNullException>(() => CreateTransitionViaReflection(0, comparison, null));
        }

        [Fact]
        public void Constructor_RejectsNullItemInLifecycleEntries()
        {
            var comparison = CreateTwoEntryComparison();
            var entries = BuildValidEntries(0, comparison);
            entries[0] = null!;

            Assert.Throws<ArgumentException>(() => CreateTransitionViaReflection(0, comparison, entries));
        }

        // ===================== completeness =====================

        [Fact]
        public void Constructor_RejectsMissingEntry()
        {
            var comparison = CreateTwoEntryComparison();
            var entries = BuildValidEntries(0, comparison);
            entries.RemoveAt(entries.Count - 1);

            Assert.Throws<ArgumentException>(() => CreateTransitionViaReflection(0, comparison, entries));
        }

        [Fact]
        public void Constructor_RejectsExtraOrDuplicateEntry()
        {
            var comparison = CreateTwoEntryComparison();
            var entries = BuildValidEntries(0, comparison);
            entries.Add(entries[0]);

            Assert.Throws<ArgumentException>(() => CreateTransitionViaReflection(0, comparison, entries));
        }

        [Fact]
        public void Constructor_RejectsOutOfOrderEntries()
        {
            var comparison = CreateTwoEntryComparison();
            var entries = BuildValidEntries(0, comparison);
            Assert.Equal(2, entries.Count);
            var swapped = new List<ClashRunSequenceLifecycleEntryPresentation> { entries[1], entries[0] };

            Assert.Throws<ArgumentException>(() => CreateTransitionViaReflection(0, comparison, swapped));
        }

        // ===================== reference validation =====================

        [Fact]
        public void Constructor_RejectsItemWithWrongComparisonIndex()
        {
            var comparison = CreateTwoEntryComparison();
            var entries = BuildValidEntries(5, comparison); // built for comparisonIndex 5

            Assert.Throws<ArgumentException>(() => CreateTransitionViaReflection(0, comparison, entries));
        }

        [Fact]
        public void Constructor_RejectsItemReferencingDifferentComparisonInstance()
        {
            var comparison = CreateTwoEntryComparison();
            var foreignComparison = CreateTwoEntryComparison(); // independent, value-equivalent, distinct instance
            var foreignEntries = BuildValidEntries(0, foreignComparison);

            Assert.Throws<ArgumentException>(() => CreateTransitionViaReflection(0, comparison, foreignEntries));
        }

        // ===================== acceptance =====================

        [Fact]
        public void Constructor_AcceptsValidCompleteList()
        {
            var comparison = CreateTwoEntryComparison();
            var entries = BuildValidEntries(3, comparison);

            var transition = CreateTransitionViaReflection(3, comparison, entries);

            Assert.Equal(3, transition.ComparisonIndex);
            Assert.Same(comparison, transition.Comparison);
            Assert.Equal(2, transition.LifecycleEntries.Count);
            Assert.Same(entries[0], transition.LifecycleEntries[0]);
            Assert.Same(entries[1], transition.LifecycleEntries[1]);
        }

        [Fact]
        public void LifecycleEntries_IsRuntimeReadOnly()
        {
            var comparison = CreateTwoEntryComparison();
            var entries = BuildValidEntries(0, comparison);

            var transition = CreateTransitionViaReflection(0, comparison, entries);

            Assert.IsNotType<List<ClashRunSequenceLifecycleEntryPresentation>>(transition.LifecycleEntries);
            Assert.Throws<NotSupportedException>(
                () => ((IList<ClashRunSequenceLifecycleEntryPresentation>)transition.LifecycleEntries).Add(entries[0]));
        }

        [Fact]
        public void Constructor_InputListMutationDoesNotAffectTransition()
        {
            var comparison = CreateTwoEntryComparison();
            var entries = BuildValidEntries(0, comparison);

            var transition = CreateTransitionViaReflection(0, comparison, entries);
            entries.Clear();

            Assert.Equal(2, transition.LifecycleEntries.Count);
        }
    }
}
