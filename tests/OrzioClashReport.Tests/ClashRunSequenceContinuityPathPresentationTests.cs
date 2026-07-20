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
    /// Tests for <see cref="ClashRunSequenceContinuityPathPresentation"/> invariants, invoking its internal
    /// constructor through reflection (no InternalsVisibleTo, no visibility increase). Continuity paths and
    /// their lifecycle entry presentations are produced through the real matcher/run-comparer/lifecycle-
    /// classifier/sequence-comparer/continuity-projector/path-assembler pipeline plus
    /// <see cref="ClashRunSequenceLifecycleEntryPresentation"/>'s own internal constructor, never hand-built.
    /// </summary>
    public class ClashRunSequenceContinuityPathPresentationTests
    {
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

        private static ClashRunSequenceContinuityPathPresentation CreatePathPresentationViaReflection(
            SelectedMatchContinuityPath? continuityPath, IReadOnlyList<ClashRunSequenceLifecycleEntryPresentation>? selectedMatchEntries)
        {
            try
            {
                return (ClashRunSequenceContinuityPathPresentation)PathPresentationConstructor.Invoke(
                    new object?[] { continuityPath, selectedMatchEntries });
            }
            catch (TargetInvocationException ex) when (ex.InnerException != null)
            {
                ExceptionDispatchInfo.Capture(ex.InnerException).Throw();
                throw;
            }
        }

        private static ClashRunSequenceLifecycleEntryPresentation CreateEntryViaReflection(
            int comparisonIndex, int entryIndex, ClashLifecycleResult comparison, ClashLifecycleEntry lifecycleEntry,
            SelectedMatchContinuityPath? continuityPath) =>
            (ClashRunSequenceLifecycleEntryPresentation)EntryConstructor.Invoke(
                new object?[] { comparisonIndex, entryIndex, comparison, lifecycleEntry, continuityPath });

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

        /// <summary>Four-run chain producing one continuity path spanning three comparisons (0, 1, 2).</summary>
        private static (ClashRunSequenceComparisonResult SequenceComparison, SelectedMatchContinuityPath Path) CreateChainWithPath()
        {
            var occ = MakeOccurrence("shared");
            var sequenceComparison = BuildSequenceComparison(
                MakeRun("run-A", occ), MakeRun("run-B", occ), MakeRun("run-C", occ), MakeRun("run-D", occ));
            var continuityResult = new DeterministicSelectedMatchContinuityProjector().Project(sequenceComparison);
            var pathsResult = new DeterministicSelectedMatchContinuityPathAssembler().Assemble(continuityResult);
            return (sequenceComparison, pathsResult.Paths[0]);
        }

        private static List<ClashRunSequenceLifecycleEntryPresentation> BuildValidSelectedMatchEntries(
            ClashRunSequenceComparisonResult sequenceComparison, SelectedMatchContinuityPath path)
        {
            var entries = new List<ClashRunSequenceLifecycleEntryPresentation>();
            for (int k = 0; k < path.SelectedMatches.Count; k++)
            {
                int comparisonIndex = path.StartComparisonIndex + k;
                var comparison = sequenceComparison.Comparisons[comparisonIndex];
                var candidate = path.SelectedMatches[k];
                var entryIndex = comparison.Entries.ToList().FindIndex(e => ReferenceEquals(e.SelectedMatch, candidate));
                entries.Add(CreateEntryViaReflection(comparisonIndex, entryIndex, comparison, comparison.Entries[entryIndex], path));
            }

            return entries;
        }

        // ===================== basic validation =====================

        [Fact]
        public void Constructor_RejectsNullContinuityPath()
        {
            var chain = CreateChainWithPath();
            var entries = BuildValidSelectedMatchEntries(chain.SequenceComparison, chain.Path);

            Assert.Throws<ArgumentNullException>(() => CreatePathPresentationViaReflection(null, entries));
        }

        [Fact]
        public void Constructor_RejectsNullSelectedMatchEntries()
        {
            var chain = CreateChainWithPath();

            Assert.Throws<ArgumentNullException>(() => CreatePathPresentationViaReflection(chain.Path, null));
        }

        [Fact]
        public void Constructor_RejectsNullItem()
        {
            var chain = CreateChainWithPath();
            var entries = BuildValidSelectedMatchEntries(chain.SequenceComparison, chain.Path);
            entries[0] = null!;

            Assert.Throws<ArgumentException>(() => CreatePathPresentationViaReflection(chain.Path, entries));
        }

        // ===================== completeness =====================

        [Fact]
        public void Constructor_RejectsMissingEntry()
        {
            var chain = CreateChainWithPath();
            var entries = BuildValidSelectedMatchEntries(chain.SequenceComparison, chain.Path);
            entries.RemoveAt(entries.Count - 1);

            Assert.Throws<ArgumentException>(() => CreatePathPresentationViaReflection(chain.Path, entries));
        }

        [Fact]
        public void Constructor_RejectsExtraOrDuplicateEntry()
        {
            var chain = CreateChainWithPath();
            var entries = BuildValidSelectedMatchEntries(chain.SequenceComparison, chain.Path);
            entries.Add(entries[0]);

            Assert.Throws<ArgumentException>(() => CreatePathPresentationViaReflection(chain.Path, entries));
        }

        [Fact]
        public void Constructor_RejectsOutOfOrderEntries()
        {
            var chain = CreateChainWithPath();
            var entries = BuildValidSelectedMatchEntries(chain.SequenceComparison, chain.Path);
            Assert.Equal(3, entries.Count);
            var reordered = new List<ClashRunSequenceLifecycleEntryPresentation> { entries[0], entries[2], entries[1] };

            Assert.Throws<ArgumentException>(() => CreatePathPresentationViaReflection(chain.Path, reordered));
        }

        // ===================== reference validation =====================

        [Fact]
        public void Constructor_RejectsItemReferencingDifferentContinuityPath()
        {
            var chainA = CreateChainWithPath();
            var chainB = CreateChainWithPath(); // independent, value-equivalent, distinct path instance
            var foreignEntries = BuildValidSelectedMatchEntries(chainB.SequenceComparison, chainB.Path);

            Assert.Throws<ArgumentException>(() => CreatePathPresentationViaReflection(chainA.Path, foreignEntries));
        }

        [Fact]
        public void Constructor_RejectsItemWhoseSelectedMatchDoesNotMatchPathPosition()
        {
            var chain = CreateChainWithPath();
            var entries = BuildValidSelectedMatchEntries(chain.SequenceComparison, chain.Path);
            var swapped = new List<ClashRunSequenceLifecycleEntryPresentation> { entries[1], entries[0], entries[2] };

            // Swapping positions 0 and 1 breaks both the SelectedMatches[i] correspondence and the
            // ComparisonIndex progression at once; either check alone is sufficient to reject it.
            Assert.Throws<ArgumentException>(() => CreatePathPresentationViaReflection(chain.Path, swapped));
        }

        // ===================== acceptance =====================

        [Fact]
        public void Constructor_AcceptsValidCompleteList()
        {
            var chain = CreateChainWithPath();
            var entries = BuildValidSelectedMatchEntries(chain.SequenceComparison, chain.Path);

            var pathPresentation = CreatePathPresentationViaReflection(chain.Path, entries);

            Assert.Same(chain.Path, pathPresentation.ContinuityPath);
            Assert.Equal(3, pathPresentation.SelectedMatchEntries.Count);
            for (int i = 0; i < entries.Count; i++)
            {
                Assert.Same(entries[i], pathPresentation.SelectedMatchEntries[i]);
            }
        }

        [Fact]
        public void SelectedMatchEntries_IsRuntimeReadOnly()
        {
            var chain = CreateChainWithPath();
            var entries = BuildValidSelectedMatchEntries(chain.SequenceComparison, chain.Path);

            var pathPresentation = CreatePathPresentationViaReflection(chain.Path, entries);

            Assert.IsNotType<List<ClashRunSequenceLifecycleEntryPresentation>>(pathPresentation.SelectedMatchEntries);
            Assert.Throws<NotSupportedException>(
                () => ((IList<ClashRunSequenceLifecycleEntryPresentation>)pathPresentation.SelectedMatchEntries).Add(entries[0]));
        }

        [Fact]
        public void Constructor_InputListMutationDoesNotAffectPathPresentation()
        {
            var chain = CreateChainWithPath();
            var entries = BuildValidSelectedMatchEntries(chain.SequenceComparison, chain.Path);

            var pathPresentation = CreatePathPresentationViaReflection(chain.Path, entries);
            entries.Clear();

            Assert.Equal(3, pathPresentation.SelectedMatchEntries.Count);
        }

        // ===================== no persistent identity =====================

        [Fact]
        public void PathPresentation_ExposesNoIdTrackStatusOrAggregatedConfidenceProperties()
        {
            var forbiddenNames = new[]
            {
                "Id", "PathId", "TrackId", "ChainId", "StableId", "PersistentId", "ClashId", "LedgerId", "Fingerprint",
                "Status", "Confidence", "AggregatedConfidence", "Reopened",
            };

            var properties = typeof(ClashRunSequenceContinuityPathPresentation)
                .GetProperties(BindingFlags.Public | BindingFlags.Instance).Select(p => p.Name).ToList();

            foreach (var forbidden in forbiddenNames)
            {
                Assert.DoesNotContain(forbidden, properties);
            }
        }
    }
}
