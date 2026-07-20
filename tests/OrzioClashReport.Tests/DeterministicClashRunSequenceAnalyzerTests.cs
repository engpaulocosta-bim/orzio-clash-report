using System;
using System.Collections.Generic;
using OrzioClashReport.Core.Abstractions;
using OrzioClashReport.Core.Analysis;
using OrzioClashReport.Core.Continuity;
using OrzioClashReport.Core.Lifecycle;
using OrzioClashReport.Core.Matching;
using OrzioClashReport.Core.Model;

namespace OrzioClashReport.Tests
{
    /// <summary>
    /// Tests for <see cref="DeterministicClashRunSequenceAnalyzer"/> as a pure orchestration boundary: it
    /// composes sequence comparison, continuity projection, and continuity path assembly exactly once, in order,
    /// by reference, and fails fast without returning partial aggregate results.
    /// </summary>
    public class DeterministicClashRunSequenceAnalyzerTests
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

        private static DeterministicClashRunSequenceAnalyzer CreateRealAnalyzer() =>
            new DeterministicClashRunSequenceAnalyzer(
                new DeterministicAdjacentClashRunSequenceComparer(
                    new DeterministicClashRunComparer(new ConservativeClashMatcher()),
                    new ConservativeClashLifecycleClassifier()),
                new DeterministicSelectedMatchContinuityProjector(),
                new DeterministicSelectedMatchContinuityPathAssembler());

        private static ClashRunSequenceComparisonResult BuildSequenceComparison(IReadOnlyList<CoordinationRun> runs)
        {
            IClashRunSequenceComparer sequenceComparer = new DeterministicAdjacentClashRunSequenceComparer(
                new DeterministicClashRunComparer(new ConservativeClashMatcher()),
                new ConservativeClashLifecycleClassifier());
            return sequenceComparer.Compare(runs);
        }

        private static (
            ClashRunSequenceComparisonResult SequenceComparison,
            ClashRunSequenceContinuityResult ContinuityResult,
            ClashRunSequenceContinuityPathsResult ContinuityPathsResult) BuildAnalysisStages(IReadOnlyList<CoordinationRun> runs)
        {
            var sequenceComparison = BuildSequenceComparison(runs);
            var continuityResult = new DeterministicSelectedMatchContinuityProjector().Project(sequenceComparison);
            var continuityPathsResult = new DeterministicSelectedMatchContinuityPathAssembler().Assemble(continuityResult);
            return (sequenceComparison, continuityResult, continuityPathsResult);
        }

        private sealed class RecordingSequenceComparer : IClashRunSequenceComparer
        {
            private readonly List<string> _callOrder;
            private readonly ClashRunSequenceComparisonResult _result;

            public RecordingSequenceComparer(List<string> callOrder, ClashRunSequenceComparisonResult result)
            {
                _callOrder = callOrder;
                _result = result;
            }

            public int CallCount { get; private set; }
            public IReadOnlyList<CoordinationRun>? ReceivedRuns { get; private set; }

            public ClashRunSequenceComparisonResult Compare(IReadOnlyList<CoordinationRun> runs)
            {
                CallCount++;
                ReceivedRuns = runs;
                _callOrder.Add("sequence");
                return _result;
            }
        }

        private sealed class RecordingContinuityProjector : IClashRunSequenceContinuityProjector
        {
            private readonly List<string> _callOrder;
            private readonly ClashRunSequenceContinuityResult _result;

            public RecordingContinuityProjector(List<string> callOrder, ClashRunSequenceContinuityResult result)
            {
                _callOrder = callOrder;
                _result = result;
            }

            public int CallCount { get; private set; }
            public ClashRunSequenceComparisonResult? ReceivedSequenceComparison { get; private set; }

            public ClashRunSequenceContinuityResult Project(ClashRunSequenceComparisonResult sequenceComparison)
            {
                CallCount++;
                ReceivedSequenceComparison = sequenceComparison;
                _callOrder.Add("continuity");
                return _result;
            }
        }

        private sealed class RecordingContinuityPathAssembler : IClashRunSequenceContinuityPathAssembler
        {
            private readonly List<string> _callOrder;
            private readonly ClashRunSequenceContinuityPathsResult _result;

            public RecordingContinuityPathAssembler(List<string> callOrder, ClashRunSequenceContinuityPathsResult result)
            {
                _callOrder = callOrder;
                _result = result;
            }

            public int CallCount { get; private set; }
            public ClashRunSequenceContinuityResult? ReceivedContinuityResult { get; private set; }

            public ClashRunSequenceContinuityPathsResult Assemble(ClashRunSequenceContinuityResult continuityResult)
            {
                CallCount++;
                ReceivedContinuityResult = continuityResult;
                _callOrder.Add("paths");
                return _result;
            }
        }

        private sealed class ThrowingSequenceComparer : IClashRunSequenceComparer
        {
            public int CallCount { get; private set; }

            public ClashRunSequenceComparisonResult Compare(IReadOnlyList<CoordinationRun> runs)
            {
                CallCount++;
                throw new InvalidOperationException("Simulated sequence comparer failure.");
            }
        }

        private sealed class ThrowingContinuityProjector : IClashRunSequenceContinuityProjector
        {
            public int CallCount { get; private set; }

            public ClashRunSequenceContinuityResult Project(ClashRunSequenceComparisonResult sequenceComparison)
            {
                CallCount++;
                throw new InvalidOperationException("Simulated continuity projector failure.");
            }
        }

        private sealed class ThrowingContinuityPathAssembler : IClashRunSequenceContinuityPathAssembler
        {
            public int CallCount { get; private set; }

            public ClashRunSequenceContinuityPathsResult Assemble(ClashRunSequenceContinuityResult continuityResult)
            {
                CallCount++;
                throw new InvalidOperationException("Simulated continuity path assembler failure.");
            }
        }

        // ===================== constructor =====================

        [Fact]
        public void Constructor_RejectsNullSequenceComparer()
        {
            Assert.Throws<ArgumentNullException>(() =>
                new DeterministicClashRunSequenceAnalyzer(
                    null!,
                    new DeterministicSelectedMatchContinuityProjector(),
                    new DeterministicSelectedMatchContinuityPathAssembler()));
        }

        [Fact]
        public void Constructor_RejectsNullContinuityProjector()
        {
            Assert.Throws<ArgumentNullException>(() =>
                new DeterministicClashRunSequenceAnalyzer(
                    new DeterministicAdjacentClashRunSequenceComparer(
                        new DeterministicClashRunComparer(new ConservativeClashMatcher()),
                        new ConservativeClashLifecycleClassifier()),
                    null!,
                    new DeterministicSelectedMatchContinuityPathAssembler()));
        }

        [Fact]
        public void Constructor_RejectsNullContinuityPathAssembler()
        {
            Assert.Throws<ArgumentNullException>(() =>
                new DeterministicClashRunSequenceAnalyzer(
                    new DeterministicAdjacentClashRunSequenceComparer(
                        new DeterministicClashRunComparer(new ConservativeClashMatcher()),
                        new ConservativeClashLifecycleClassifier()),
                    new DeterministicSelectedMatchContinuityProjector(),
                    null!));
        }

        // ===================== orchestration =====================

        [Fact]
        public void Analyze_RejectsNullRunsBeforeCallingAnyDependency()
        {
            var stages = BuildAnalysisStages(new[]
            {
                MakeRun("run-A", MakeOccurrence("shared")),
                MakeRun("run-B", MakeOccurrence("shared")),
            });
            var order = new List<string>();
            var sequenceComparer = new RecordingSequenceComparer(order, stages.SequenceComparison);
            var projector = new RecordingContinuityProjector(order, stages.ContinuityResult);
            var assembler = new RecordingContinuityPathAssembler(order, stages.ContinuityPathsResult);
            var analyzer = new DeterministicClashRunSequenceAnalyzer(sequenceComparer, projector, assembler);

            Assert.Throws<ArgumentNullException>(() => analyzer.Analyze(null!));

            Assert.Equal(0, sequenceComparer.CallCount);
            Assert.Equal(0, projector.CallCount);
            Assert.Equal(0, assembler.CallCount);
            Assert.Empty(order);
        }

        [Fact]
        public void Analyze_PassesSameOrderedRunReferenceToComparer()
        {
            var runs = new List<CoordinationRun>
            {
                MakeRun("run-A", MakeOccurrence("shared")),
                MakeRun("run-B", MakeOccurrence("shared")),
            };
            var stages = BuildAnalysisStages(runs);
            var order = new List<string>();
            var sequenceComparer = new RecordingSequenceComparer(order, stages.SequenceComparison);
            var analyzer = new DeterministicClashRunSequenceAnalyzer(
                sequenceComparer,
                new RecordingContinuityProjector(order, stages.ContinuityResult),
                new RecordingContinuityPathAssembler(order, stages.ContinuityPathsResult));

            analyzer.Analyze(runs);

            Assert.Same(runs, sequenceComparer.ReceivedRuns);
        }

        [Fact]
        public void Analyze_CallsEachDependencyExactlyOnceAndInOrder()
        {
            var runs = new[]
            {
                MakeRun("run-A", MakeOccurrence("shared")),
                MakeRun("run-B", MakeOccurrence("shared")),
                MakeRun("run-C", MakeOccurrence("shared")),
            };
            var stages = BuildAnalysisStages(runs);
            var order = new List<string>();
            var sequenceComparer = new RecordingSequenceComparer(order, stages.SequenceComparison);
            var projector = new RecordingContinuityProjector(order, stages.ContinuityResult);
            var assembler = new RecordingContinuityPathAssembler(order, stages.ContinuityPathsResult);
            var analyzer = new DeterministicClashRunSequenceAnalyzer(sequenceComparer, projector, assembler);

            analyzer.Analyze(runs);

            Assert.Equal(1, sequenceComparer.CallCount);
            Assert.Equal(1, projector.CallCount);
            Assert.Equal(1, assembler.CallCount);
            Assert.Equal(new[] { "sequence", "continuity", "paths" }, order);
        }

        [Fact]
        public void Analyze_ProjectorReceivesExactComparerResult()
        {
            var runs = new[]
            {
                MakeRun("run-A", MakeOccurrence("shared")),
                MakeRun("run-B", MakeOccurrence("shared")),
                MakeRun("run-C", MakeOccurrence("shared")),
            };
            var stages = BuildAnalysisStages(runs);
            var order = new List<string>();
            var projector = new RecordingContinuityProjector(order, stages.ContinuityResult);
            var analyzer = new DeterministicClashRunSequenceAnalyzer(
                new RecordingSequenceComparer(order, stages.SequenceComparison),
                projector,
                new RecordingContinuityPathAssembler(order, stages.ContinuityPathsResult));

            analyzer.Analyze(runs);

            Assert.Same(stages.SequenceComparison, projector.ReceivedSequenceComparison);
        }

        [Fact]
        public void Analyze_AssemblerReceivesExactProjectorResult()
        {
            var runs = new[]
            {
                MakeRun("run-A", MakeOccurrence("shared")),
                MakeRun("run-B", MakeOccurrence("shared")),
                MakeRun("run-C", MakeOccurrence("shared")),
            };
            var stages = BuildAnalysisStages(runs);
            var order = new List<string>();
            var assembler = new RecordingContinuityPathAssembler(order, stages.ContinuityPathsResult);
            var analyzer = new DeterministicClashRunSequenceAnalyzer(
                new RecordingSequenceComparer(order, stages.SequenceComparison),
                new RecordingContinuityProjector(order, stages.ContinuityResult),
                assembler);

            analyzer.Analyze(runs);

            Assert.Same(stages.ContinuityResult, assembler.ReceivedContinuityResult);
        }

        [Fact]
        public void Analyze_AggregateExposesExactStageReferences()
        {
            var runs = new[]
            {
                MakeRun("run-A", MakeOccurrence("shared")),
                MakeRun("run-B", MakeOccurrence("shared")),
                MakeRun("run-C", MakeOccurrence("shared")),
            };
            var stages = BuildAnalysisStages(runs);
            var order = new List<string>();
            var analyzer = new DeterministicClashRunSequenceAnalyzer(
                new RecordingSequenceComparer(order, stages.SequenceComparison),
                new RecordingContinuityProjector(order, stages.ContinuityResult),
                new RecordingContinuityPathAssembler(order, stages.ContinuityPathsResult));

            var result = analyzer.Analyze(runs);

            Assert.Same(stages.SequenceComparison, result.SequenceComparison);
            Assert.Same(stages.ContinuityResult, result.ContinuityResult);
            Assert.Same(stages.ContinuityPathsResult, result.ContinuityPathsResult);
        }

        [Fact]
        public void Analyze_TwoValidRuns_ProducesZeroLinksAndZeroPaths()
        {
            var occ = MakeOccurrence("shared");
            var result = CreateRealAnalyzer().Analyze(new[]
            {
                MakeRun("run-A", occ),
                MakeRun("run-B", occ),
            });

            Assert.Single(result.SequenceComparison.Comparisons);
            Assert.Empty(result.ContinuityResult.Links);
            Assert.Empty(result.ContinuityPathsResult.Paths);
        }

        [Fact]
        public void Analyze_FourRunContinuousChain_WithRealStagesProducesLinksAndPath()
        {
            var occ = MakeOccurrence("shared");
            var result = CreateRealAnalyzer().Analyze(new[]
            {
                MakeRun("run-A", occ),
                MakeRun("run-B", occ),
                MakeRun("run-C", occ),
                MakeRun("run-D", occ),
            });

            Assert.Equal(4, result.SequenceComparison.Runs.Count);
            Assert.Equal(3, result.SequenceComparison.Comparisons.Count);
            Assert.Equal(2, result.ContinuityResult.Links.Count);
            var path = Assert.Single(result.ContinuityPathsResult.Paths);
            Assert.Equal(2, path.Links.Count);
        }

        // ===================== fail-fast =====================

        [Fact]
        public void Analyze_ComparerFailurePropagatesAndSkipsLaterStages()
        {
            var throwingComparer = new ThrowingSequenceComparer();
            var runs = new[]
            {
                MakeRun("run-A", MakeOccurrence("shared")),
                MakeRun("run-B", MakeOccurrence("shared")),
            };
            var stages = BuildAnalysisStages(runs);
            var order = new List<string>();
            var projector = new RecordingContinuityProjector(order, stages.ContinuityResult);
            var assembler = new RecordingContinuityPathAssembler(order, stages.ContinuityPathsResult);
            var analyzer = new DeterministicClashRunSequenceAnalyzer(throwingComparer, projector, assembler);

            var exception = Assert.Throws<InvalidOperationException>(() => analyzer.Analyze(runs));

            Assert.Contains("sequence comparer", exception.Message, StringComparison.Ordinal);
            Assert.Equal(1, throwingComparer.CallCount);
            Assert.Equal(0, projector.CallCount);
            Assert.Equal(0, assembler.CallCount);
        }

        [Fact]
        public void Analyze_ProjectorFailurePropagatesAndSkipsAssembler()
        {
            var runs = new[]
            {
                MakeRun("run-A", MakeOccurrence("shared")),
                MakeRun("run-B", MakeOccurrence("shared")),
            };
            var stages = BuildAnalysisStages(runs);
            var order = new List<string>();
            var sequenceComparer = new RecordingSequenceComparer(order, stages.SequenceComparison);
            var throwingProjector = new ThrowingContinuityProjector();
            var assembler = new RecordingContinuityPathAssembler(order, stages.ContinuityPathsResult);
            var analyzer = new DeterministicClashRunSequenceAnalyzer(sequenceComparer, throwingProjector, assembler);

            var exception = Assert.Throws<InvalidOperationException>(() => analyzer.Analyze(runs));

            Assert.Contains("continuity projector", exception.Message, StringComparison.Ordinal);
            Assert.Equal(1, sequenceComparer.CallCount);
            Assert.Equal(1, throwingProjector.CallCount);
            Assert.Equal(0, assembler.CallCount);
        }

        [Fact]
        public void Analyze_AssemblerFailurePropagatesAndReturnsNoAggregate()
        {
            var runs = new[]
            {
                MakeRun("run-A", MakeOccurrence("shared")),
                MakeRun("run-B", MakeOccurrence("shared")),
            };
            var stages = BuildAnalysisStages(runs);
            var order = new List<string>();
            var sequenceComparer = new RecordingSequenceComparer(order, stages.SequenceComparison);
            var projector = new RecordingContinuityProjector(order, stages.ContinuityResult);
            var throwingAssembler = new ThrowingContinuityPathAssembler();
            var analyzer = new DeterministicClashRunSequenceAnalyzer(sequenceComparer, projector, throwingAssembler);

            var exception = Assert.Throws<InvalidOperationException>(() => analyzer.Analyze(runs));

            Assert.Contains("path assembler", exception.Message, StringComparison.Ordinal);
            Assert.Equal(1, sequenceComparer.CallCount);
            Assert.Equal(1, projector.CallCount);
            Assert.Equal(1, throwingAssembler.CallCount);
        }
    }
}
