using System;
using System.Collections.Generic;
using OrzioClashReport.Core.Abstractions;
using OrzioClashReport.Core.Lifecycle;
using OrzioClashReport.Core.Matching;
using OrzioClashReport.Core.Model;

namespace OrzioClashReport.Tests
{
    /// <summary>
    /// Tests for <see cref="DeterministicAdjacentClashRunSequenceComparer"/>: adjacent-only traversal, exact
    /// pair roles, caller-order authority, call counts, reference propagation between the injected
    /// <see cref="IClashRunComparer"/> and <see cref="IClashLifecycleClassifier"/>, and fail-fast exception
    /// propagation with no partial result.
    /// </summary>
    public class DeterministicAdjacentClashRunSequenceComparerTests
    {
        private static readonly DateTimeOffset DefaultCreatedAt = new DateTimeOffset(2026, 7, 10, 9, 0, 0, TimeSpan.Zero);

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

        private static DeterministicAdjacentClashRunSequenceComparer CreateDefaultComparer() =>
            new DeterministicAdjacentClashRunSequenceComparer(
                new DeterministicClashRunComparer(new ConservativeClashMatcher()),
                new ConservativeClashLifecycleClassifier());

        /// <summary>Delegates to a real <see cref="IClashRunComparer"/> while recording every call's arguments and result by reference.</summary>
        private sealed class RecordingClashRunComparer : IClashRunComparer
        {
            private readonly IClashRunComparer _inner = new DeterministicClashRunComparer(new ConservativeClashMatcher());

            public List<(CoordinationRun Previous, CoordinationRun Current, ClashRunMatchResult Result)> Calls { get; } = new();

            public ClashRunMatchResult Compare(CoordinationRun previousRun, CoordinationRun currentRun)
            {
                var result = _inner.Compare(previousRun, currentRun);
                Calls.Add((previousRun, currentRun, result));
                return result;
            }
        }

        /// <summary>Delegates to a real <see cref="IClashLifecycleClassifier"/> while recording every call's argument and result by reference.</summary>
        private sealed class RecordingClashLifecycleClassifier : IClashLifecycleClassifier
        {
            private readonly IClashLifecycleClassifier _inner = new ConservativeClashLifecycleClassifier();

            public List<(ClashRunMatchResult Received, ClashLifecycleResult Result)> Calls { get; } = new();

            public ClashLifecycleResult Classify(ClashRunMatchResult matchResult)
            {
                var result = _inner.Classify(matchResult);
                Calls.Add((matchResult, result));
                return result;
            }
        }

        /// <summary>Delegates to a real <see cref="IClashRunComparer"/> but throws on a specific 1-based call number.</summary>
        private sealed class ThrowingClashRunComparer : IClashRunComparer
        {
            private readonly IClashRunComparer _inner = new DeterministicClashRunComparer(new ConservativeClashMatcher());
            private readonly int _throwAtCallNumber;
            private int _callCount;

            public ThrowingClashRunComparer(int throwAtCallNumber) => _throwAtCallNumber = throwAtCallNumber;

            public ClashRunMatchResult Compare(CoordinationRun previousRun, CoordinationRun currentRun)
            {
                _callCount++;
                if (_callCount == _throwAtCallNumber)
                {
                    throw new InvalidOperationException($"Simulated run comparer failure at call {_callCount}.");
                }

                return _inner.Compare(previousRun, currentRun);
            }
        }

        /// <summary>Delegates to a real <see cref="IClashLifecycleClassifier"/> but throws on a specific 1-based call number.</summary>
        private sealed class ThrowingClashLifecycleClassifier : IClashLifecycleClassifier
        {
            private readonly IClashLifecycleClassifier _inner = new ConservativeClashLifecycleClassifier();
            private readonly int _throwAtCallNumber;
            private int _callCount;

            public ThrowingClashLifecycleClassifier(int throwAtCallNumber) => _throwAtCallNumber = throwAtCallNumber;

            public ClashLifecycleResult Classify(ClashRunMatchResult matchResult)
            {
                _callCount++;
                if (_callCount == _throwAtCallNumber)
                {
                    throw new InvalidOperationException($"Simulated lifecycle classifier failure at call {_callCount}.");
                }

                return _inner.Classify(matchResult);
            }
        }

        // ===================== constructor =====================

        [Fact]
        public void Constructor_RejectsNullRunComparer()
        {
            Assert.Throws<ArgumentNullException>(() =>
                new DeterministicAdjacentClashRunSequenceComparer(null!, new ConservativeClashLifecycleClassifier()));
        }

        [Fact]
        public void Constructor_RejectsNullLifecycleClassifier()
        {
            Assert.Throws<ArgumentNullException>(() =>
                new DeterministicAdjacentClashRunSequenceComparer(new DeterministicClashRunComparer(new ConservativeClashMatcher()), null!));
        }

        // ===================== argument validation =====================

        [Fact]
        public void Compare_RejectsNullRuns()
        {
            var comparer = CreateDefaultComparer();

            Assert.Throws<ArgumentNullException>(() => comparer.Compare(null!));
        }

        [Fact]
        public void Compare_RejectsEmptyRuns()
        {
            var comparer = CreateDefaultComparer();

            Assert.Throws<ArgumentException>(() => comparer.Compare(Array.Empty<CoordinationRun>()));
        }

        [Fact]
        public void Compare_RejectsOneRun()
        {
            var comparer = CreateDefaultComparer();
            var runA = MakeRun("run-A", MakeOccurrence("Test 1", "a0", "b0"));

            Assert.Throws<ArgumentException>(() => comparer.Compare(new[] { runA }));
        }

        [Fact]
        public void Compare_RejectsNullFirstRun()
        {
            var comparer = CreateDefaultComparer();
            var runB = MakeRun("run-B", MakeOccurrence("Test 1", "a0", "b0"));
            var runs = new CoordinationRun?[] { null, runB };

            Assert.Throws<ArgumentException>(() => comparer.Compare(runs!));
        }

        [Fact]
        public void Compare_RejectsNullMiddleRun()
        {
            var comparer = CreateDefaultComparer();
            var runA = MakeRun("run-A", MakeOccurrence("Test 1", "a0", "b0"));
            var runC = MakeRun("run-C", MakeOccurrence("Test 1", "a0", "b0"));
            var runs = new CoordinationRun?[] { runA, null, runC };

            Assert.Throws<ArgumentException>(() => comparer.Compare(runs!));
        }

        // ===================== pair count / roles =====================

        [Fact]
        public void Compare_TwoRuns_ProduceExactlyOneComparison()
        {
            var comparer = CreateDefaultComparer();
            var runA = MakeRun("run-A", MakeOccurrence("Test 1", "a0", "b0"));
            var runB = MakeRun("run-B", MakeOccurrence("Test 1", "a0", "b0"));

            var result = comparer.Compare(new[] { runA, runB });

            Assert.Single(result.Comparisons);
        }

        [Fact]
        public void Compare_ThreeRuns_ProduceExactlyTwoComparisons()
        {
            var comparer = CreateDefaultComparer();
            var runA = MakeRun("run-A", MakeOccurrence("Test 1", "a0", "b0"));
            var runB = MakeRun("run-B", MakeOccurrence("Test 1", "a0", "b0"));
            var runC = MakeRun("run-C", MakeOccurrence("Test 1", "a0", "b0"));

            var result = comparer.Compare(new[] { runA, runB, runC });

            Assert.Equal(2, result.Comparisons.Count);
        }

        [Fact]
        public void Compare_FourRuns_ProduceExactlyThreeComparisons()
        {
            var comparer = CreateDefaultComparer();
            var runA = MakeRun("run-A", MakeOccurrence("Test 1", "a0", "b0"));
            var runB = MakeRun("run-B", MakeOccurrence("Test 1", "a0", "b0"));
            var runC = MakeRun("run-C", MakeOccurrence("Test 1", "a0", "b0"));
            var runD = MakeRun("run-D", MakeOccurrence("Test 1", "a0", "b0"));

            var result = comparer.Compare(new[] { runA, runB, runC, runD });

            Assert.Equal(3, result.Comparisons.Count);
        }

        [Fact]
        public void Compare_TwoRuns_RolesAreExactlyAToB()
        {
            var runComparer = new RecordingClashRunComparer();
            var comparer = new DeterministicAdjacentClashRunSequenceComparer(runComparer, new ConservativeClashLifecycleClassifier());
            var runA = MakeRun("run-A", MakeOccurrence("Test 1", "a0", "b0"));
            var runB = MakeRun("run-B", MakeOccurrence("Test 1", "a0", "b0"));

            comparer.Compare(new[] { runA, runB });

            Assert.Single(runComparer.Calls);
            Assert.Same(runA, runComparer.Calls[0].Previous);
            Assert.Same(runB, runComparer.Calls[0].Current);
        }

        [Fact]
        public void Compare_ThreeRuns_RolesAreExactlyAToBThenBToC()
        {
            var runComparer = new RecordingClashRunComparer();
            var comparer = new DeterministicAdjacentClashRunSequenceComparer(runComparer, new ConservativeClashLifecycleClassifier());
            var runA = MakeRun("run-A", MakeOccurrence("Test 1", "a0", "b0"));
            var runB = MakeRun("run-B", MakeOccurrence("Test 1", "a0", "b0"));
            var runC = MakeRun("run-C", MakeOccurrence("Test 1", "a0", "b0"));

            comparer.Compare(new[] { runA, runB, runC });

            Assert.Equal(2, runComparer.Calls.Count);
            Assert.Same(runA, runComparer.Calls[0].Previous);
            Assert.Same(runB, runComparer.Calls[0].Current);
            Assert.Same(runB, runComparer.Calls[1].Previous);
            Assert.Same(runC, runComparer.Calls[1].Current);
        }

        [Fact]
        public void Compare_ThreeRuns_NoAToCPairIsRequested()
        {
            var runComparer = new RecordingClashRunComparer();
            var comparer = new DeterministicAdjacentClashRunSequenceComparer(runComparer, new ConservativeClashLifecycleClassifier());
            var runA = MakeRun("run-A", MakeOccurrence("Test 1", "a0", "b0"));
            var runB = MakeRun("run-B", MakeOccurrence("Test 1", "a0", "b0"));
            var runC = MakeRun("run-C", MakeOccurrence("Test 1", "a0", "b0"));

            comparer.Compare(new[] { runA, runB, runC });

            Assert.DoesNotContain(runComparer.Calls, call => ReferenceEquals(call.Previous, runA) && ReferenceEquals(call.Current, runC));
        }

        [Fact]
        public void Compare_FourRuns_RolesAreExactlyAToBBToCCToD()
        {
            var runComparer = new RecordingClashRunComparer();
            var comparer = new DeterministicAdjacentClashRunSequenceComparer(runComparer, new ConservativeClashLifecycleClassifier());
            var runA = MakeRun("run-A", MakeOccurrence("Test 1", "a0", "b0"));
            var runB = MakeRun("run-B", MakeOccurrence("Test 1", "a0", "b0"));
            var runC = MakeRun("run-C", MakeOccurrence("Test 1", "a0", "b0"));
            var runD = MakeRun("run-D", MakeOccurrence("Test 1", "a0", "b0"));

            comparer.Compare(new[] { runA, runB, runC, runD });

            Assert.Equal(3, runComparer.Calls.Count);
            Assert.Same(runA, runComparer.Calls[0].Previous);
            Assert.Same(runB, runComparer.Calls[0].Current);
            Assert.Same(runB, runComparer.Calls[1].Previous);
            Assert.Same(runC, runComparer.Calls[1].Current);
            Assert.Same(runC, runComparer.Calls[2].Previous);
            Assert.Same(runD, runComparer.Calls[2].Current);
        }

        // ===================== caller order authority =====================

        [Fact]
        public void Compare_ExplicitCreatedAtInvertedOrder_IsPreserved()
        {
            var comparer = CreateDefaultComparer();
            // CreatedAt descends across C, A, B even though the CLI-declared order is C, A, B.
            var runC = MakeRun("run-C", new DateTimeOffset(2026, 7, 12, 9, 0, 0, TimeSpan.Zero), MakeOccurrence("Test 1", "a0", "b0"));
            var runA = MakeRun("run-A", new DateTimeOffset(2026, 7, 11, 9, 0, 0, TimeSpan.Zero), MakeOccurrence("Test 1", "a0", "b0"));
            var runB = MakeRun("run-B", new DateTimeOffset(2026, 7, 10, 9, 0, 0, TimeSpan.Zero), MakeOccurrence("Test 1", "a0", "b0"));

            var result = comparer.Compare(new[] { runC, runA, runB });

            Assert.Same(runC, result.Comparisons[0].MatchResult.PreviousRun);
            Assert.Same(runA, result.Comparisons[0].MatchResult.CurrentRun);
            Assert.Same(runA, result.Comparisons[1].MatchResult.PreviousRun);
            Assert.Same(runB, result.Comparisons[1].MatchResult.CurrentRun);
        }

        [Fact]
        public void Compare_ExplicitRunIdReverseLexicalOrder_IsPreserved()
        {
            var comparer = CreateDefaultComparer();
            var zRun = MakeRun("zzz-run", MakeOccurrence("Test 1", "a0", "b0"));
            var aRun = MakeRun("aaa-run", MakeOccurrence("Test 1", "a0", "b0"));

            var result = comparer.Compare(new[] { zRun, aRun });

            Assert.Same(zRun, result.Comparisons[0].MatchResult.PreviousRun);
            Assert.Same(aRun, result.Comparisons[0].MatchResult.CurrentRun);
        }

        [Fact]
        public void Compare_DuplicateExactRunReferences_ArePreservedAsAToAThenAToB()
        {
            var comparer = CreateDefaultComparer();
            var runA = MakeRun("run-A", MakeOccurrence("Test 1", "a0", "b0"));
            var runB = MakeRun("run-B", MakeOccurrence("Test 1", "a0", "b0"));

            var result = comparer.Compare(new[] { runA, runA, runB });

            Assert.Same(runA, result.Comparisons[0].MatchResult.PreviousRun);
            Assert.Same(runA, result.Comparisons[0].MatchResult.CurrentRun);
            Assert.Same(runA, result.Comparisons[1].MatchResult.PreviousRun);
            Assert.Same(runB, result.Comparisons[1].MatchResult.CurrentRun);
        }

        [Fact]
        public void Compare_DuplicateRunIdValues_AreNotDeduplicated()
        {
            var comparer = CreateDefaultComparer();
            var firstDuplicate = MakeRun("duplicate-run", new DateTimeOffset(2026, 7, 15, 9, 0, 0, TimeSpan.Zero), MakeOccurrence("Test 1", "a0", "b0"));
            var secondDuplicate = MakeRun("duplicate-run", new DateTimeOffset(2026, 7, 15, 10, 0, 0, TimeSpan.Zero), MakeOccurrence("Test 1", "a0", "b0"));
            var thirdDuplicate = MakeRun("duplicate-run", new DateTimeOffset(2026, 7, 15, 11, 0, 0, TimeSpan.Zero), MakeOccurrence("Test 1", "a0", "b0"));

            var result = comparer.Compare(new[] { firstDuplicate, secondDuplicate, thirdDuplicate });

            Assert.Equal(3, result.Runs.Count);
            Assert.Equal(2, result.Comparisons.Count);
            Assert.Same(firstDuplicate, result.Runs[0]);
            Assert.Same(secondDuplicate, result.Runs[1]);
            Assert.Same(thirdDuplicate, result.Runs[2]);
        }

        // ===================== call counts / reference propagation =====================

        [Fact]
        public void Compare_RunComparerIsCalledExactlyRunsCountMinusOneTimes()
        {
            var runComparer = new RecordingClashRunComparer();
            var comparer = new DeterministicAdjacentClashRunSequenceComparer(runComparer, new ConservativeClashLifecycleClassifier());
            var runA = MakeRun("run-A", MakeOccurrence("Test 1", "a0", "b0"));
            var runB = MakeRun("run-B", MakeOccurrence("Test 1", "a0", "b0"));
            var runC = MakeRun("run-C", MakeOccurrence("Test 1", "a0", "b0"));
            var runD = MakeRun("run-D", MakeOccurrence("Test 1", "a0", "b0"));

            comparer.Compare(new[] { runA, runB, runC, runD });

            Assert.Equal(3, runComparer.Calls.Count);
        }

        [Fact]
        public void Compare_LifecycleClassifierIsCalledExactlyRunsCountMinusOneTimes()
        {
            var lifecycleClassifier = new RecordingClashLifecycleClassifier();
            var comparer = new DeterministicAdjacentClashRunSequenceComparer(
                new DeterministicClashRunComparer(new ConservativeClashMatcher()), lifecycleClassifier);
            var runA = MakeRun("run-A", MakeOccurrence("Test 1", "a0", "b0"));
            var runB = MakeRun("run-B", MakeOccurrence("Test 1", "a0", "b0"));
            var runC = MakeRun("run-C", MakeOccurrence("Test 1", "a0", "b0"));
            var runD = MakeRun("run-D", MakeOccurrence("Test 1", "a0", "b0"));

            comparer.Compare(new[] { runA, runB, runC, runD });

            Assert.Equal(3, lifecycleClassifier.Calls.Count);
        }

        [Fact]
        public void Compare_LifecycleClassifierReceivesExactMatchResultReturnedByRunComparer()
        {
            var runComparer = new RecordingClashRunComparer();
            var lifecycleClassifier = new RecordingClashLifecycleClassifier();
            var comparer = new DeterministicAdjacentClashRunSequenceComparer(runComparer, lifecycleClassifier);
            var runA = MakeRun("run-A", MakeOccurrence("Test 1", "a0", "b0"));
            var runB = MakeRun("run-B", MakeOccurrence("Test 1", "a0", "b0"));
            var runC = MakeRun("run-C", MakeOccurrence("Test 1", "a0", "b0"));

            comparer.Compare(new[] { runA, runB, runC });

            Assert.Equal(2, runComparer.Calls.Count);
            Assert.Equal(2, lifecycleClassifier.Calls.Count);
            Assert.Same(runComparer.Calls[0].Result, lifecycleClassifier.Calls[0].Received);
            Assert.Same(runComparer.Calls[1].Result, lifecycleClassifier.Calls[1].Received);
        }

        [Fact]
        public void Compare_ResultRunsPreserveExactInputReferencesAndPositions()
        {
            var comparer = CreateDefaultComparer();
            var runA = MakeRun("run-A", MakeOccurrence("Test 1", "a0", "b0"));
            var runB = MakeRun("run-B", MakeOccurrence("Test 1", "a0", "b0"));
            var runC = MakeRun("run-C", MakeOccurrence("Test 1", "a0", "b0"));

            var result = comparer.Compare(new[] { runA, runB, runC });

            Assert.Same(runA, result.Runs[0]);
            Assert.Same(runB, result.Runs[1]);
            Assert.Same(runC, result.Runs[2]);
        }

        [Fact]
        public void Compare_ResultComparisonsPreserveExactGeneratedLifecycleResultReferencesAndPositions()
        {
            var lifecycleClassifier = new RecordingClashLifecycleClassifier();
            var comparer = new DeterministicAdjacentClashRunSequenceComparer(
                new DeterministicClashRunComparer(new ConservativeClashMatcher()), lifecycleClassifier);
            var runA = MakeRun("run-A", MakeOccurrence("Test 1", "a0", "b0"));
            var runB = MakeRun("run-B", MakeOccurrence("Test 1", "a0", "b0"));
            var runC = MakeRun("run-C", MakeOccurrence("Test 1", "a0", "b0"));

            var result = comparer.Compare(new[] { runA, runB, runC });

            Assert.Same(lifecycleClassifier.Calls[0].Result, result.Comparisons[0]);
            Assert.Same(lifecycleClassifier.Calls[1].Result, result.Comparisons[1]);
        }

        [Fact]
        public void Compare_InputRunCollectionIsNotMutated()
        {
            var comparer = CreateDefaultComparer();
            var runA = MakeRun("run-A", MakeOccurrence("Test 1", "a0", "b0"));
            var runB = MakeRun("run-B", MakeOccurrence("Test 1", "a0", "b0"));
            var input = new List<CoordinationRun> { runA, runB };

            comparer.Compare(input);

            Assert.Equal(2, input.Count);
            Assert.Same(runA, input[0]);
            Assert.Same(runB, input[1]);
        }

        [Fact]
        public void Compare_RepeatedCallsWithSameOrderedRuns_ProduceOrdinallyEquivalentStructure()
        {
            var comparer = CreateDefaultComparer();
            var runA = MakeRun("run-A", MakeOccurrence("Test 1", "a0", "b0"));
            var runB = MakeRun("run-B", MakeOccurrence("Test 1", "a0", "b0"));
            var runC = MakeRun("run-C", MakeOccurrence("Test 1", "a0", "b0"));
            var runs = new[] { runA, runB, runC };

            var firstResult = comparer.Compare(runs);
            var secondResult = comparer.Compare(runs);

            Assert.Equal(firstResult.ToString(), secondResult.ToString());
            Assert.Equal(firstResult.Comparisons.Count, secondResult.Comparisons.Count);
            for (int i = 0; i < firstResult.Comparisons.Count; i++)
            {
                Assert.Equal(firstResult.Comparisons[i].ToString(), secondResult.Comparisons[i].ToString());
            }
        }

        // ===================== fail-fast / no partial result =====================

        [Fact]
        public void Compare_RunComparerExceptionOnFirstPair_Propagates()
        {
            var comparer = new DeterministicAdjacentClashRunSequenceComparer(
                new ThrowingClashRunComparer(throwAtCallNumber: 1), new ConservativeClashLifecycleClassifier());
            var runA = MakeRun("run-A", MakeOccurrence("Test 1", "a0", "b0"));
            var runB = MakeRun("run-B", MakeOccurrence("Test 1", "a0", "b0"));

            var exception = Assert.Throws<InvalidOperationException>(() => comparer.Compare(new[] { runA, runB }));
            Assert.Contains("call 1", exception.Message, StringComparison.Ordinal);
        }

        [Fact]
        public void Compare_RunComparerExceptionOnLaterPair_PropagatesWithNoResultReturned()
        {
            var comparer = new DeterministicAdjacentClashRunSequenceComparer(
                new ThrowingClashRunComparer(throwAtCallNumber: 2), new ConservativeClashLifecycleClassifier());
            var runA = MakeRun("run-A", MakeOccurrence("Test 1", "a0", "b0"));
            var runB = MakeRun("run-B", MakeOccurrence("Test 1", "a0", "b0"));
            var runC = MakeRun("run-C", MakeOccurrence("Test 1", "a0", "b0"));

            var exception = Assert.Throws<InvalidOperationException>(() => comparer.Compare(new[] { runA, runB, runC }));
            Assert.Contains("call 2", exception.Message, StringComparison.Ordinal);
        }

        [Fact]
        public void Compare_LifecycleClassifierExceptionOnFirstPair_Propagates()
        {
            var comparer = new DeterministicAdjacentClashRunSequenceComparer(
                new DeterministicClashRunComparer(new ConservativeClashMatcher()), new ThrowingClashLifecycleClassifier(throwAtCallNumber: 1));
            var runA = MakeRun("run-A", MakeOccurrence("Test 1", "a0", "b0"));
            var runB = MakeRun("run-B", MakeOccurrence("Test 1", "a0", "b0"));

            var exception = Assert.Throws<InvalidOperationException>(() => comparer.Compare(new[] { runA, runB }));
            Assert.Contains("call 1", exception.Message, StringComparison.Ordinal);
        }

        [Fact]
        public void Compare_LifecycleClassifierExceptionOnLaterPair_PropagatesWithNoResultReturned()
        {
            var comparer = new DeterministicAdjacentClashRunSequenceComparer(
                new DeterministicClashRunComparer(new ConservativeClashMatcher()), new ThrowingClashLifecycleClassifier(throwAtCallNumber: 2));
            var runA = MakeRun("run-A", MakeOccurrence("Test 1", "a0", "b0"));
            var runB = MakeRun("run-B", MakeOccurrence("Test 1", "a0", "b0"));
            var runC = MakeRun("run-C", MakeOccurrence("Test 1", "a0", "b0"));

            var exception = Assert.Throws<InvalidOperationException>(() => comparer.Compare(new[] { runA, runB, runC }));
            Assert.Contains("call 2", exception.Message, StringComparison.Ordinal);
        }
    }
}
