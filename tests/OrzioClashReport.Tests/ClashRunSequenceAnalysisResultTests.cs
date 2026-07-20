using System;
using System.Linq;
using System.Reflection;
using System.Runtime.ExceptionServices;
using OrzioClashReport.Core.Abstractions;
using OrzioClashReport.Core.Analysis;
using OrzioClashReport.Core.Continuity;
using OrzioClashReport.Core.Lifecycle;
using OrzioClashReport.Core.Matching;
using OrzioClashReport.Core.Model;

namespace OrzioClashReport.Tests
{
    /// <summary>
    /// Tests for <see cref="ClashRunSequenceAnalysisResult"/> invariants, invoking its internal constructor
    /// through reflection (no InternalsVisibleTo, no visibility increase). Valid chains are produced through the
    /// real sequence-comparer/projector/path-assembler pipeline.
    /// </summary>
    public class ClashRunSequenceAnalysisResultTests
    {
        private static readonly ConstructorInfo ResultConstructor =
            typeof(ClashRunSequenceAnalysisResult).GetConstructor(
                BindingFlags.Instance | BindingFlags.NonPublic,
                binder: null,
                types: new[]
                {
                    typeof(ClashRunSequenceComparisonResult),
                    typeof(ClashRunSequenceContinuityResult),
                    typeof(ClashRunSequenceContinuityPathsResult),
                },
                modifiers: null)
            ?? throw new InvalidOperationException("Could not find ClashRunSequenceAnalysisResult internal constructor.");

        private static ClashRunSequenceAnalysisResult CreateResultViaReflection(
            ClashRunSequenceComparisonResult? sequenceComparison,
            ClashRunSequenceContinuityResult? continuityResult,
            ClashRunSequenceContinuityPathsResult? continuityPathsResult)
        {
            try
            {
                return (ClashRunSequenceAnalysisResult)ResultConstructor.Invoke(new object?[]
                {
                    sequenceComparison,
                    continuityResult,
                    continuityPathsResult,
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

        private static (
            ClashRunSequenceComparisonResult SequenceComparison,
            ClashRunSequenceContinuityResult ContinuityResult,
            ClashRunSequenceContinuityPathsResult ContinuityPathsResult) BuildAnalysisStages()
        {
            var occ = MakeOccurrence("shared");
            var runs = new[]
            {
                MakeRun("run-A", occ),
                MakeRun("run-B", occ),
                MakeRun("run-C", occ),
                MakeRun("run-D", occ),
            };

            IClashRunSequenceComparer sequenceComparer = new DeterministicAdjacentClashRunSequenceComparer(
                new DeterministicClashRunComparer(new ConservativeClashMatcher()),
                new ConservativeClashLifecycleClassifier());
            var sequenceComparison = sequenceComparer.Compare(runs);
            var continuityResult = new DeterministicSelectedMatchContinuityProjector().Project(sequenceComparison);
            var continuityPathsResult = new DeterministicSelectedMatchContinuityPathAssembler().Assemble(continuityResult);

            return (sequenceComparison, continuityResult, continuityPathsResult);
        }

        // ===================== acceptance =====================

        [Fact]
        public void Result_AcceptsCoherentChain()
        {
            var stages = BuildAnalysisStages();

            var result = CreateResultViaReflection(
                stages.SequenceComparison,
                stages.ContinuityResult,
                stages.ContinuityPathsResult);

            Assert.NotNull(result);
        }

        [Fact]
        public void Result_PreservesAllThreePropertiesByExactReference()
        {
            var stages = BuildAnalysisStages();

            var result = CreateResultViaReflection(
                stages.SequenceComparison,
                stages.ContinuityResult,
                stages.ContinuityPathsResult);

            Assert.Same(stages.SequenceComparison, result.SequenceComparison);
            Assert.Same(stages.ContinuityResult, result.ContinuityResult);
            Assert.Same(stages.ContinuityPathsResult, result.ContinuityPathsResult);
        }

        // ===================== rejection =====================

        [Fact]
        public void Result_RejectsNullSequenceComparison()
        {
            var stages = BuildAnalysisStages();

            Assert.Throws<ArgumentNullException>(() =>
                CreateResultViaReflection(null, stages.ContinuityResult, stages.ContinuityPathsResult));
        }

        [Fact]
        public void Result_RejectsNullContinuityResult()
        {
            var stages = BuildAnalysisStages();

            Assert.Throws<ArgumentNullException>(() =>
                CreateResultViaReflection(stages.SequenceComparison, null, stages.ContinuityPathsResult));
        }

        [Fact]
        public void Result_RejectsNullContinuityPathsResult()
        {
            var stages = BuildAnalysisStages();

            Assert.Throws<ArgumentNullException>(() =>
                CreateResultViaReflection(stages.SequenceComparison, stages.ContinuityResult, null));
        }

        [Fact]
        public void Result_RejectsContinuityFromEquivalentButDifferentSequenceComparisonReference()
        {
            var stages = BuildAnalysisStages();
            var otherStages = BuildAnalysisStages();

            var exception = Assert.Throws<ArgumentException>(() =>
                CreateResultViaReflection(
                    stages.SequenceComparison,
                    otherStages.ContinuityResult,
                    otherStages.ContinuityPathsResult));

            Assert.Equal("continuityResult", exception.ParamName);
        }

        [Fact]
        public void Result_RejectsPathsFromEquivalentButDifferentContinuityResultReference()
        {
            var stages = BuildAnalysisStages();
            var otherStages = BuildAnalysisStages();

            var exception = Assert.Throws<ArgumentException>(() =>
                CreateResultViaReflection(
                    stages.SequenceComparison,
                    stages.ContinuityResult,
                    otherStages.ContinuityPathsResult));

            Assert.Equal("continuityPathsResult", exception.ParamName);
        }

        // ===================== public surface =====================

        [Fact]
        public void Result_PropertiesHaveNoPublicSetters()
        {
            var properties = typeof(ClashRunSequenceAnalysisResult)
                .GetProperties(BindingFlags.Public | BindingFlags.Instance);

            Assert.All(properties, property => Assert.Null(property.SetMethod));
        }

        [Fact]
        public void Result_SurfaceContainsNoIdentityTrackingOrAggregatedLifecycleProperties()
        {
            var forbiddenNames = new[]
            {
                "Id", "AnalysisId", "TrackId", "ChainId", "StableId", "PersistentId", "ClashId", "LedgerId",
                "Fingerprint", "Status", "Lifecycle", "LifecycleStatus", "History", "Ledger", "Track",
                "Confidence", "Score", "Reopened",
            };

            var propertyNames = typeof(ClashRunSequenceAnalysisResult)
                .GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .Select(property => property.Name)
                .ToList();

            foreach (var forbiddenName in forbiddenNames)
            {
                Assert.DoesNotContain(forbiddenName, propertyNames);
            }
        }

        [Fact]
        public void Result_PublicSurfaceIsOnlyTheThreeDerivedStageReferences()
        {
            var propertyNames = typeof(ClashRunSequenceAnalysisResult)
                .GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .Select(property => property.Name)
                .OrderBy(name => name, StringComparer.Ordinal)
                .ToArray();

            Assert.Equal(
                new[] { "ContinuityPathsResult", "ContinuityResult", "SequenceComparison" },
                propertyNames);
        }
    }
}
