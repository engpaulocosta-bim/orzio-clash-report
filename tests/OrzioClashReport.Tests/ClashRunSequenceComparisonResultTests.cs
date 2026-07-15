using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.ExceptionServices;
using OrzioClashReport.Core.Abstractions;
using OrzioClashReport.Core.Lifecycle;
using OrzioClashReport.Core.Matching;
using OrzioClashReport.Core.Model;

namespace OrzioClashReport.Tests
{
    /// <summary>
    /// Tests for <see cref="ClashRunSequenceComparisonResult"/> invariants, invoking its internal constructor
    /// through reflection (no InternalsVisibleTo, no visibility increase). Lifecycle comparisons used to build a
    /// valid result are produced through the real matcher/comparer/classifier pipeline, never hand-built.
    /// </summary>
    public class ClashRunSequenceComparisonResultTests
    {
        private static readonly ConstructorInfo ResultConstructor =
            typeof(ClashRunSequenceComparisonResult).GetConstructor(
                BindingFlags.Instance | BindingFlags.NonPublic,
                binder: null,
                types: new[] { typeof(IReadOnlyList<CoordinationRun>), typeof(IReadOnlyList<ClashLifecycleResult>) },
                modifiers: null)
            ?? throw new InvalidOperationException("Could not find ClashRunSequenceComparisonResult internal constructor.");

        private static ClashRunSequenceComparisonResult CreateResultViaReflection(
            IReadOnlyList<CoordinationRun> runs, IReadOnlyList<ClashLifecycleResult> comparisons)
        {
            try
            {
                return (ClashRunSequenceComparisonResult)ResultConstructor.Invoke(new object?[] { runs, comparisons });
            }
            catch (TargetInvocationException ex) when (ex.InnerException != null)
            {
                ExceptionDispatchInfo.Capture(ex.InnerException).Throw();
                throw;
            }
        }

        private static readonly DateTimeOffset DefaultCreatedAt = new DateTimeOffset(2026, 7, 10, 9, 0, 0, TimeSpan.Zero);
        private static readonly DateTimeOffset OtherCreatedAt = new DateTimeOffset(2026, 7, 11, 9, 0, 0, TimeSpan.Zero);

        private static readonly ModelRevision Sigma =
            new ModelRevision(new ModelIdentity("Sigma", "Structure", "Main"), "R04", "Sigma_Main_R04.nwc", null, null, null);

        private static readonly ExecutedClashTest Test1ForSigma = new ExecutedClashTest("Test 1", Sigma.Identity, Sigma.Identity);

        private static ClashObject MakeObject(string elementId) => new ClashObject(elementId, null, null, null, null, null);

        private static ClashOccurrence MakeOccurrence(string clashTestName, string elementIdA, string elementIdB) =>
            new ClashOccurrence(
                clashTestName,
                new ClashResult(clashTestName, ClashStatus.New, null, null, null, MakeObject(elementIdA), MakeObject(elementIdB), null),
                Sigma, Sigma);

        private static RunManifest MakeManifest(string runId, DateTimeOffset createdAt) =>
            new RunManifest(runId, createdAt, new[] { Sigma }, new[] { Test1ForSigma });

        private static CoordinationRun MakeRun(string runId, DateTimeOffset createdAt, params ClashOccurrence[] occurrences) =>
            new CoordinationRun(MakeManifest(runId, createdAt), occurrences);

        private static CoordinationRun MakeRun(string runId, params ClashOccurrence[] occurrences) =>
            MakeRun(runId, DefaultCreatedAt, occurrences);

        /// <summary>Builds a real lifecycle result through the production pipeline, matching <c>Program.CreateDerivedComparison</c>.</summary>
        private static ClashLifecycleResult CreateLifecycleResult(CoordinationRun previousRun, CoordinationRun currentRun)
        {
            IClashMatcher matcher = new ConservativeClashMatcher();
            IClashRunComparer runComparer = new DeterministicClashRunComparer(matcher);
            var matchResult = runComparer.Compare(previousRun, currentRun);

            IClashLifecycleClassifier lifecycleClassifier = new ConservativeClashLifecycleClassifier();
            return lifecycleClassifier.Classify(matchResult);
        }

        // ===================== acceptance =====================

        [Fact]
        public void Result_AcceptsValidTwoRunOneComparisonResult()
        {
            var runA = MakeRun("run-A", MakeOccurrence("Test 1", "a0", "b0"));
            var runB = MakeRun("run-B", MakeOccurrence("Test 1", "a0", "b0"));
            var comparison = CreateLifecycleResult(runA, runB);

            var result = CreateResultViaReflection(new[] { runA, runB }, new[] { comparison });

            Assert.Equal(2, result.Runs.Count);
            Assert.Single(result.Comparisons);
            Assert.Same(runA, result.Runs[0]);
            Assert.Same(runB, result.Runs[1]);
            Assert.Same(comparison, result.Comparisons[0]);
        }

        [Fact]
        public void Result_AcceptsValidThreeRunTwoComparisonResult()
        {
            var runA = MakeRun("run-A", MakeOccurrence("Test 1", "a0", "b0"));
            var runB = MakeRun("run-B", MakeOccurrence("Test 1", "a0", "b0"));
            var runC = MakeRun("run-C", MakeOccurrence("Test 1", "a0", "b0"));
            var comparisonAB = CreateLifecycleResult(runA, runB);
            var comparisonBC = CreateLifecycleResult(runB, runC);

            var result = CreateResultViaReflection(new[] { runA, runB, runC }, new[] { comparisonAB, comparisonBC });

            Assert.Equal(3, result.Runs.Count);
            Assert.Equal(2, result.Comparisons.Count);
        }

        [Fact]
        public void Result_ExactDuplicateRunReference_IsAcceptedWhenComparisonUsesSameReference()
        {
            var runA = MakeRun("run-A", MakeOccurrence("Test 1", "a0", "b0"));
            var runB = MakeRun("run-B", MakeOccurrence("Test 1", "a0", "b0"));
            var comparisonAA = CreateLifecycleResult(runA, runA);
            var comparisonAB = CreateLifecycleResult(runA, runB);

            var result = CreateResultViaReflection(new[] { runA, runA, runB }, new[] { comparisonAA, comparisonAB });

            Assert.Equal(3, result.Runs.Count);
            Assert.Same(runA, result.Runs[0]);
            Assert.Same(runA, result.Runs[1]);
            Assert.Same(runB, result.Runs[2]);
        }

        [Fact]
        public void Result_RunOrderIsPreserved()
        {
            var runA = MakeRun("run-A", MakeOccurrence("Test 1", "a0", "b0"));
            var runB = MakeRun("run-B", MakeOccurrence("Test 1", "a0", "b0"));
            var runC = MakeRun("run-C", MakeOccurrence("Test 1", "a0", "b0"));
            var comparisonAB = CreateLifecycleResult(runA, runB);
            var comparisonBC = CreateLifecycleResult(runB, runC);

            var result = CreateResultViaReflection(new[] { runA, runB, runC }, new[] { comparisonAB, comparisonBC });

            Assert.Same(runA, result.Runs[0]);
            Assert.Same(runB, result.Runs[1]);
            Assert.Same(runC, result.Runs[2]);
        }

        [Fact]
        public void Result_DuplicateRunPositionsArePreserved()
        {
            var runA = MakeRun("run-A", MakeOccurrence("Test 1", "a0", "b0"));
            var runB = MakeRun("run-B", MakeOccurrence("Test 1", "a0", "b0"));
            var comparisonAA = CreateLifecycleResult(runA, runA);
            var comparisonAB = CreateLifecycleResult(runA, runB);

            var result = CreateResultViaReflection(new[] { runA, runA, runB }, new[] { comparisonAA, comparisonAB });

            Assert.Same(result.Runs[0], result.Runs[1]);
            Assert.NotSame(result.Runs[1], result.Runs[2]);
        }

        [Fact]
        public void Result_ComparisonOrderIsPreserved()
        {
            var runA = MakeRun("run-A", MakeOccurrence("Test 1", "a0", "b0"));
            var runB = MakeRun("run-B", MakeOccurrence("Test 1", "a0", "b0"));
            var runC = MakeRun("run-C", MakeOccurrence("Test 1", "a0", "b0"));
            var comparisonAB = CreateLifecycleResult(runA, runB);
            var comparisonBC = CreateLifecycleResult(runB, runC);

            var result = CreateResultViaReflection(new[] { runA, runB, runC }, new[] { comparisonAB, comparisonBC });

            Assert.Same(comparisonAB, result.Comparisons[0]);
            Assert.Same(comparisonBC, result.Comparisons[1]);
        }

        [Fact]
        public void Result_ToString_ExactForThreeRunsTwoComparisons()
        {
            var runA = MakeRun("run-A", MakeOccurrence("Test 1", "a0", "b0"));
            var runB = MakeRun("run-B", MakeOccurrence("Test 1", "a0", "b0"));
            var runC = MakeRun("run-C", MakeOccurrence("Test 1", "a0", "b0"));
            var comparisonAB = CreateLifecycleResult(runA, runB);
            var comparisonBC = CreateLifecycleResult(runB, runC);

            var result = CreateResultViaReflection(new[] { runA, runB, runC }, new[] { comparisonAB, comparisonBC });

            Assert.Equal("3 runs, 2 adjacent comparisons", result.ToString());
        }

        // ===================== rejection: runs =====================

        [Fact]
        public void Result_RejectsNullRuns()
        {
            var runA = MakeRun("run-A", MakeOccurrence("Test 1", "a0", "b0"));
            var runB = MakeRun("run-B", MakeOccurrence("Test 1", "a0", "b0"));
            var comparison = CreateLifecycleResult(runA, runB);

            Assert.Throws<ArgumentNullException>(() => CreateResultViaReflection(null!, new[] { comparison }));
        }

        [Fact]
        public void Result_RejectsEmptyRuns()
        {
            Assert.Throws<ArgumentException>(() => CreateResultViaReflection(
                Array.Empty<CoordinationRun>(), Array.Empty<ClashLifecycleResult>()));
        }

        [Fact]
        public void Result_RejectsSingleRun()
        {
            var runA = MakeRun("run-A", MakeOccurrence("Test 1", "a0", "b0"));

            Assert.Throws<ArgumentException>(() => CreateResultViaReflection(
                new[] { runA }, Array.Empty<ClashLifecycleResult>()));
        }

        [Fact]
        public void Result_RejectsNullRunAtFirstIndex()
        {
            var runB = MakeRun("run-B", MakeOccurrence("Test 1", "a0", "b0"));
            var runs = new CoordinationRun?[] { null, runB };

            Assert.Throws<ArgumentException>(() => CreateResultViaReflection(
                runs!, new ClashLifecycleResult?[] { null }!));
        }

        [Fact]
        public void Result_RejectsNullRunAtLaterIndex()
        {
            var runA = MakeRun("run-A", MakeOccurrence("Test 1", "a0", "b0"));
            var runs = new CoordinationRun?[] { runA, null };

            Assert.Throws<ArgumentException>(() => CreateResultViaReflection(
                runs!, new ClashLifecycleResult?[] { null }!));
        }

        // ===================== rejection: comparisons =====================

        [Fact]
        public void Result_RejectsNullComparisons()
        {
            var runA = MakeRun("run-A", MakeOccurrence("Test 1", "a0", "b0"));
            var runB = MakeRun("run-B", MakeOccurrence("Test 1", "a0", "b0"));

            Assert.Throws<ArgumentNullException>(() => CreateResultViaReflection(new[] { runA, runB }, null!));
        }

        [Fact]
        public void Result_RejectsTooFewComparisons()
        {
            var runA = MakeRun("run-A", MakeOccurrence("Test 1", "a0", "b0"));
            var runB = MakeRun("run-B", MakeOccurrence("Test 1", "a0", "b0"));
            var runC = MakeRun("run-C", MakeOccurrence("Test 1", "a0", "b0"));
            var comparisonAB = CreateLifecycleResult(runA, runB);

            Assert.Throws<ArgumentException>(() => CreateResultViaReflection(
                new[] { runA, runB, runC }, new[] { comparisonAB }));
        }

        [Fact]
        public void Result_RejectsTooManyComparisons()
        {
            var runA = MakeRun("run-A", MakeOccurrence("Test 1", "a0", "b0"));
            var runB = MakeRun("run-B", MakeOccurrence("Test 1", "a0", "b0"));
            var comparisonAB = CreateLifecycleResult(runA, runB);
            var comparisonAA = CreateLifecycleResult(runA, runA);

            Assert.Throws<ArgumentException>(() => CreateResultViaReflection(
                new[] { runA, runB }, new[] { comparisonAB, comparisonAA }));
        }

        [Fact]
        public void Result_RejectsNullComparisonAtFirstIndex()
        {
            var runA = MakeRun("run-A", MakeOccurrence("Test 1", "a0", "b0"));
            var runB = MakeRun("run-B", MakeOccurrence("Test 1", "a0", "b0"));
            var comparisons = new ClashLifecycleResult?[] { null };

            Assert.Throws<ArgumentException>(() => CreateResultViaReflection(new[] { runA, runB }, comparisons!));
        }

        [Fact]
        public void Result_RejectsNullComparisonAtLaterIndex()
        {
            var runA = MakeRun("run-A", MakeOccurrence("Test 1", "a0", "b0"));
            var runB = MakeRun("run-B", MakeOccurrence("Test 1", "a0", "b0"));
            var runC = MakeRun("run-C", MakeOccurrence("Test 1", "a0", "b0"));
            var comparisonAB = CreateLifecycleResult(runA, runB);
            var comparisons = new ClashLifecycleResult?[] { comparisonAB, null };

            Assert.Throws<ArgumentException>(() => CreateResultViaReflection(new[] { runA, runB, runC }, comparisons!));
        }

        // ===================== rejection: continuity =====================

        [Fact]
        public void Result_RejectsPreviousRunReferenceMismatch()
        {
            var runA = MakeRun("run-A", MakeOccurrence("Test 1", "a0", "b0"));
            var wrongPreviousRun = MakeRun("run-A", MakeOccurrence("Test 1", "a0", "b0"));
            var runB = MakeRun("run-B", MakeOccurrence("Test 1", "a0", "b0"));
            var comparison = CreateLifecycleResult(wrongPreviousRun, runB);

            Assert.Throws<ArgumentException>(() => CreateResultViaReflection(new[] { runA, runB }, new[] { comparison }));
        }

        [Fact]
        public void Result_RejectsCurrentRunReferenceMismatch()
        {
            var runA = MakeRun("run-A", MakeOccurrence("Test 1", "a0", "b0"));
            var runB = MakeRun("run-B", MakeOccurrence("Test 1", "a0", "b0"));
            var wrongCurrentRun = MakeRun("run-B", MakeOccurrence("Test 1", "a0", "b0"));
            var comparison = CreateLifecycleResult(runA, wrongCurrentRun);

            Assert.Throws<ArgumentException>(() => CreateResultViaReflection(new[] { runA, runB }, new[] { comparison }));
        }

        [Fact]
        public void Result_SameRunIdDifferentReference_DoesNotSatisfyContinuity()
        {
            var runA = MakeRun("run-A", DefaultCreatedAt, MakeOccurrence("Test 1", "a0", "b0"));
            // Same RunId as runA, different CreatedAt, and a distinct object reference.
            var runASameId = MakeRun("run-A", OtherCreatedAt, MakeOccurrence("Test 1", "a0", "b0"));
            var runB = MakeRun("run-B", DefaultCreatedAt, MakeOccurrence("Test 1", "a0", "b0"));
            var comparison = CreateLifecycleResult(runASameId, runB);

            Assert.Throws<ArgumentException>(() => CreateResultViaReflection(new[] { runA, runB }, new[] { comparison }));
        }

        [Fact]
        public void Result_SameCreatedAtDifferentReference_DoesNotSatisfyContinuity()
        {
            var runA = MakeRun("run-A", DefaultCreatedAt, MakeOccurrence("Test 1", "a0", "b0"));
            // Same CreatedAt as runA, different RunId, and a distinct object reference.
            var runASameCreatedAt = MakeRun("run-A-other", DefaultCreatedAt, MakeOccurrence("Test 1", "a0", "b0"));
            var runB = MakeRun("run-B", DefaultCreatedAt, MakeOccurrence("Test 1", "a0", "b0"));
            var comparison = CreateLifecycleResult(runASameCreatedAt, runB);

            Assert.Throws<ArgumentException>(() => CreateResultViaReflection(new[] { runA, runB }, new[] { comparison }));
        }

        [Fact]
        public void Result_SameRunIdAndCreatedAtDifferentReference_DoesNotSatisfyContinuity()
        {
            // runA1 and runA2 share both RunId and CreatedAt (same value-shaped metadata), but are
            // distinct CoordinationRun instances. Continuity must still be rejected: neither RunId
            // nor CreatedAt, alone or combined, substitutes for exact object reference.
            var runA1 = MakeRun("run-A", DefaultCreatedAt, MakeOccurrence("Test 1", "a0", "b0"));
            var runA2 = MakeRun("run-A", DefaultCreatedAt, MakeOccurrence("Test 1", "a0", "b0"));
            var runB = MakeRun("run-B", DefaultCreatedAt, MakeOccurrence("Test 1", "a0", "b0"));

            Assert.Equal(runA1.RunId, runA2.RunId);
            Assert.Equal(runA1.CreatedAt, runA2.CreatedAt);
            Assert.NotSame(runA1, runA2);

            var comparison = CreateLifecycleResult(runA1, runB);

            Assert.Throws<ArgumentException>(() => CreateResultViaReflection(new[] { runA2, runB }, new[] { comparison }));
        }

        // ===================== defensive copies / immutability =====================

        [Fact]
        public void Result_ConstructorRunCollectionMutation_DoesNotAffectResult()
        {
            var runA = MakeRun("run-A", MakeOccurrence("Test 1", "a0", "b0"));
            var runB = MakeRun("run-B", MakeOccurrence("Test 1", "a0", "b0"));
            var comparison = CreateLifecycleResult(runA, runB);
            var runsList = new List<CoordinationRun> { runA, runB };

            var result = CreateResultViaReflection(runsList, new[] { comparison });
            runsList.Clear();

            Assert.Equal(2, result.Runs.Count);
        }

        [Fact]
        public void Result_ConstructorComparisonCollectionMutation_DoesNotAffectResult()
        {
            var runA = MakeRun("run-A", MakeOccurrence("Test 1", "a0", "b0"));
            var runB = MakeRun("run-B", MakeOccurrence("Test 1", "a0", "b0"));
            var comparison = CreateLifecycleResult(runA, runB);
            var comparisonsList = new List<ClashLifecycleResult> { comparison };

            var result = CreateResultViaReflection(new[] { runA, runB }, comparisonsList);
            comparisonsList.Clear();

            Assert.Single(result.Comparisons);
        }

        [Fact]
        public void Result_RunsCannotBeMutatedThroughIList()
        {
            var runA = MakeRun("run-A", MakeOccurrence("Test 1", "a0", "b0"));
            var runB = MakeRun("run-B", MakeOccurrence("Test 1", "a0", "b0"));
            var comparison = CreateLifecycleResult(runA, runB);

            var result = CreateResultViaReflection(new[] { runA, runB }, new[] { comparison });

            var mutableView = Assert.IsAssignableFrom<IList<CoordinationRun>>(result.Runs);
            Assert.Throws<NotSupportedException>(() => mutableView.Add(runA));
        }

        [Fact]
        public void Result_ComparisonsCannotBeMutatedThroughIList()
        {
            var runA = MakeRun("run-A", MakeOccurrence("Test 1", "a0", "b0"));
            var runB = MakeRun("run-B", MakeOccurrence("Test 1", "a0", "b0"));
            var comparison = CreateLifecycleResult(runA, runB);

            var result = CreateResultViaReflection(new[] { runA, runB }, new[] { comparison });

            var mutableView = Assert.IsAssignableFrom<IList<ClashLifecycleResult>>(result.Comparisons);
            Assert.Throws<NotSupportedException>(() => mutableView.Add(comparison));
        }
    }
}
