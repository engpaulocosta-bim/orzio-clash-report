using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.ExceptionServices;
using OrzioClashReport.Core.Abstractions;
using OrzioClashReport.Core.Matching;
using OrzioClashReport.Core.Model;

namespace OrzioClashReport.Tests
{
    /// <summary>
    /// Tests for <see cref="ClashRunMatchCandidate"/> through the public comparer entry point, and for
    /// <see cref="ClashRunMatchResult"/> invariants by invoking its internal constructor through reflection.
    /// </summary>
    public class ClashRunMatchResultTests
    {
        private static readonly ConstructorInfo ClashRunMatchResultConstructor =
            typeof(ClashRunMatchResult).GetConstructor(
                BindingFlags.Instance | BindingFlags.NonPublic,
                binder: null,
                types: new[]
                {
                    typeof(CoordinationRun),
                    typeof(CoordinationRun),
                    typeof(IReadOnlyList<ClashRunMatchCandidate>),
                    typeof(IReadOnlyList<ClashRunMatchCandidate>),
                    typeof(IReadOnlyList<ClashRunMatchCandidate>),
                    typeof(IReadOnlyList<ClashOccurrence>),
                    typeof(IReadOnlyList<ClashOccurrence>),
                },
                modifiers: null)
            ?? throw new InvalidOperationException("Could not find ClashRunMatchResult internal constructor.");

        private static readonly ModelRevision Sigma = MakeRevision("Sigma", "Structure", "Main", "R04");

        private sealed class MatchScenario
        {
            public MatchScenario(
                CoordinationRun previousRun,
                CoordinationRun currentRun,
                IReadOnlyList<ClashOccurrence> previousOccurrences,
                IReadOnlyList<ClashOccurrence> currentOccurrences,
                ClashRunMatchResult baseResult)
            {
                PreviousRun = previousRun;
                CurrentRun = currentRun;
                PreviousOccurrences = previousOccurrences;
                CurrentOccurrences = currentOccurrences;
                BaseResult = baseResult;
            }

            public CoordinationRun PreviousRun { get; }
            public CoordinationRun CurrentRun { get; }
            public IReadOnlyList<ClashOccurrence> PreviousOccurrences { get; }
            public IReadOnlyList<ClashOccurrence> CurrentOccurrences { get; }
            public ClashRunMatchResult BaseResult { get; }
        }

        private static ModelRevision MakeRevision(string company, string discipline, string modelName, string revision) =>
            new ModelRevision(new ModelIdentity(company, discipline, modelName), revision, $"{modelName}_{revision}.nwc", null, null, null);

        private static ClashObject MakeObject(string elementId) => new ClashObject(elementId, null, null, null, null, null);

        private static ClashOccurrence MakeOccurrence(
            string clashTestName, ModelRevision modelA, ModelRevision modelB, string elementIdA = "a", string elementIdB = "b") =>
            new ClashOccurrence(
                clashTestName,
                new ClashResult(clashTestName, ClashStatus.New, null, null, null, MakeObject(elementIdA), MakeObject(elementIdB), "g1"),
                modelA, modelB);

        private static readonly ExecutedClashTest Test1ForSigma = new ExecutedClashTest("Test 1", Sigma.Identity, Sigma.Identity);
        private static readonly ExecutedClashTest RepeatedForSigma = new ExecutedClashTest("Repeated", Sigma.Identity, Sigma.Identity);

        private static RunManifest MakeManifest(string runId, params ModelRevision[] models) =>
            new RunManifest(runId, new DateTimeOffset(2026, 7, 10, 9, 0, 0, TimeSpan.Zero), models, new[] { Test1ForSigma, RepeatedForSigma });

        private static CoordinationRun MakeRun(string runId, ModelRevision[] models, params ClashOccurrence[] occurrences) =>
            new CoordinationRun(MakeManifest(runId, models), occurrences);

        private static ClashMatchAssessment MakeAssessment(
            ClashOccurrence previous, ClashOccurrence current, ClashMatchConfidence confidence) =>
            new ClashMatchAssessment(
                previous, current, confidence,
                new[] { new MatchEvidence(MatchEvidenceKind.ClashTestName, MatchEvidenceVerdict.Supports, "same clash test", null, null) });

        /// <summary>Test-only fake: returns a configured assessment for an exact (previous, current) reference pair, or null otherwise.</summary>
        private sealed class ConfigurableClashMatcher : IClashMatcher
        {
            private readonly Dictionary<(ClashOccurrence Previous, ClashOccurrence Current), ClashMatchAssessment> _responses =
                new Dictionary<(ClashOccurrence, ClashOccurrence), ClashMatchAssessment>();

            public void RespondWith(ClashOccurrence previous, ClashOccurrence current, ClashMatchAssessment assessment) =>
                _responses[(previous, current)] = assessment;

            public ClashMatchAssessment? Assess(ClashOccurrence previousOccurrence, ClashOccurrence currentOccurrence)
            {
                if (previousOccurrence == null)
                {
                    throw new ArgumentNullException(nameof(previousOccurrence));
                }

                if (currentOccurrence == null)
                {
                    throw new ArgumentNullException(nameof(currentOccurrence));
                }

                return _responses.TryGetValue((previousOccurrence, currentOccurrence), out var assessment) ? assessment : null;
            }
        }

        private static ClashRunMatchResult CreateResultViaReflection(
            CoordinationRun previousRun,
            CoordinationRun currentRun,
            IReadOnlyList<ClashRunMatchCandidate> candidates,
            IReadOnlyList<ClashRunMatchCandidate> selectedMatches,
            IReadOnlyList<ClashRunMatchCandidate> alternativeCandidates,
            IReadOnlyList<ClashOccurrence> unmatchedPrevious,
            IReadOnlyList<ClashOccurrence> unmatchedCurrent)
        {
            try
            {
                return (ClashRunMatchResult)ClashRunMatchResultConstructor.Invoke(
                    new object[]
                    {
                        previousRun,
                        currentRun,
                        candidates,
                        selectedMatches,
                        alternativeCandidates,
                        unmatchedPrevious,
                        unmatchedCurrent,
                    });
            }
            catch (TargetInvocationException ex) when (ex.InnerException != null)
            {
                ExceptionDispatchInfo.Capture(ex.InnerException).Throw();
                throw;
            }
        }

        private static MatchScenario CreateTwoCandidateScenario()
        {
            var previousOccurrences = new[]
            {
                MakeOccurrence("Test 1", Sigma, Sigma, "p0a", "p0b"),
                MakeOccurrence("Test 1", Sigma, Sigma, "p1a", "p1b"),
            };

            var currentOccurrences = new[]
            {
                MakeOccurrence("Test 1", Sigma, Sigma, "c0a", "c0b"),
                MakeOccurrence("Test 1", Sigma, Sigma, "c1a", "c1b"),
            };

            var previousRun = MakeRun("run-1", new[] { Sigma }, previousOccurrences);
            var currentRun = MakeRun("run-2", new[] { Sigma }, currentOccurrences);

            var matcher = new ConfigurableClashMatcher();
            matcher.RespondWith(previousOccurrences[0], currentOccurrences[0], MakeAssessment(previousOccurrences[0], currentOccurrences[0], ClashMatchConfidence.High));
            matcher.RespondWith(previousOccurrences[1], currentOccurrences[0], MakeAssessment(previousOccurrences[1], currentOccurrences[0], ClashMatchConfidence.Medium));

            var result = new DeterministicClashRunComparer(matcher).Compare(previousRun, currentRun);
            return new MatchScenario(previousRun, currentRun, previousOccurrences, currentOccurrences, result);
        }

        private static MatchScenario CreateOrderedUnmatchedScenario()
        {
            var previousOccurrences = new[]
            {
                MakeOccurrence("Test 1", Sigma, Sigma, "p0a", "p0b"),
                MakeOccurrence("Test 1", Sigma, Sigma, "p1a", "p1b"),
                MakeOccurrence("Test 1", Sigma, Sigma, "p2a", "p2b"),
            };

            var currentOccurrences = new[]
            {
                MakeOccurrence("Test 1", Sigma, Sigma, "c0a", "c0b"),
                MakeOccurrence("Test 1", Sigma, Sigma, "c1a", "c1b"),
                MakeOccurrence("Test 1", Sigma, Sigma, "c2a", "c2b"),
            };

            var previousRun = MakeRun("run-1", new[] { Sigma }, previousOccurrences);
            var currentRun = MakeRun("run-2", new[] { Sigma }, currentOccurrences);

            var matcher = new ConfigurableClashMatcher();
            matcher.RespondWith(previousOccurrences[0], currentOccurrences[0], MakeAssessment(previousOccurrences[0], currentOccurrences[0], ClashMatchConfidence.High));

            var result = new DeterministicClashRunComparer(matcher).Compare(previousRun, currentRun);
            return new MatchScenario(previousRun, currentRun, previousOccurrences, currentOccurrences, result);
        }

        // --- ClashRunMatchCandidate, via a real Compare() result ---

        [Fact]
        public void Candidate_HasCorrectPreviousIndex()
        {
            var scenario = CreateTwoCandidateScenario();

            Assert.Equal(0, scenario.BaseResult.Candidates[0].PreviousIndex);
        }

        [Fact]
        public void Candidate_HasCorrectCurrentIndex()
        {
            var scenario = CreateTwoCandidateScenario();

            Assert.Equal(0, scenario.BaseResult.Candidates[0].CurrentIndex);
        }

        [Fact]
        public void Candidate_HasCorrectAssessment()
        {
            var scenario = CreateTwoCandidateScenario();

            Assert.Same(
                scenario.PreviousOccurrences[0],
                scenario.BaseResult.Candidates[0].Assessment.PreviousOccurrence);
        }

        [Fact]
        public void Candidate_ToString_IncludesIndexes()
        {
            var scenario = CreateTwoCandidateScenario();

            string text = scenario.BaseResult.Candidates[0].ToString();
            Assert.Contains("0", text);
        }

        [Fact]
        public void Candidate_ToString_IncludesConfidence()
        {
            var scenario = CreateTwoCandidateScenario();

            Assert.Contains("High", scenario.BaseResult.Candidates[0].ToString());
        }

        // --- ClashRunMatchResult: immutability and identity ---

        [Fact]
        public void Result_CandidatesList_CannotBeMutated()
        {
            var scenario = CreateTwoCandidateScenario();

            var mutableView = Assert.IsAssignableFrom<IList<ClashRunMatchCandidate>>(scenario.BaseResult.Candidates);
            Assert.Throws<NotSupportedException>(() => mutableView.Add(scenario.BaseResult.Candidates[0]));
        }

        [Fact]
        public void Result_PreservesPreviousRunReference()
        {
            var previousRun = MakeRun("run-1", new[] { Sigma });
            var currentRun = MakeRun("run-2", new[] { Sigma });

            var result = new DeterministicClashRunComparer(new ConfigurableClashMatcher()).Compare(previousRun, currentRun);

            Assert.Same(previousRun, result.PreviousRun);
        }

        [Fact]
        public void Result_PreservesCurrentRunReference()
        {
            var previousRun = MakeRun("run-1", new[] { Sigma });
            var currentRun = MakeRun("run-2", new[] { Sigma });

            var result = new DeterministicClashRunComparer(new ConfigurableClashMatcher()).Compare(previousRun, currentRun);

            Assert.Same(currentRun, result.CurrentRun);
        }

        [Fact]
        public void Result_ToString_IncludesRunIdsAndCounts()
        {
            var scenario = CreateTwoCandidateScenario();

            string text = scenario.BaseResult.ToString();
            Assert.Contains("run-1", text);
            Assert.Contains("run-2", text);
            Assert.Contains("2", text);
        }

        // --- ClashRunMatchResult partition invariants ---

        [Fact]
        public void Constructor_RejectsCandidateOmittedFromSelectedAndAlternatives()
        {
            var scenario = CreateTwoCandidateScenario();

            Assert.Throws<ArgumentException>(() => CreateResultViaReflection(
                scenario.PreviousRun,
                scenario.CurrentRun,
                scenario.BaseResult.Candidates,
                scenario.BaseResult.SelectedMatches,
                Array.Empty<ClashRunMatchCandidate>(),
                scenario.BaseResult.UnmatchedPrevious,
                scenario.BaseResult.UnmatchedCurrent));
        }

        [Fact]
        public void Constructor_RejectsDuplicateCandidateInCandidates()
        {
            var scenario = CreateTwoCandidateScenario();
            var duplicateCandidates = new[]
            {
                scenario.BaseResult.Candidates[0],
                scenario.BaseResult.Candidates[0],
                scenario.BaseResult.Candidates[1],
            };

            Assert.Throws<ArgumentException>(() => CreateResultViaReflection(
                scenario.PreviousRun,
                scenario.CurrentRun,
                duplicateCandidates,
                scenario.BaseResult.SelectedMatches,
                scenario.BaseResult.AlternativeCandidates,
                scenario.BaseResult.UnmatchedPrevious,
                scenario.BaseResult.UnmatchedCurrent));
        }

        [Fact]
        public void Constructor_RejectsDuplicateCandidateInSelectedMatches()
        {
            var scenario = CreateTwoCandidateScenario();
            var duplicateSelected = new[]
            {
                scenario.BaseResult.SelectedMatches[0],
                scenario.BaseResult.SelectedMatches[0],
            };

            Assert.Throws<ArgumentException>(() => CreateResultViaReflection(
                scenario.PreviousRun,
                scenario.CurrentRun,
                scenario.BaseResult.Candidates,
                duplicateSelected,
                scenario.BaseResult.AlternativeCandidates,
                scenario.BaseResult.UnmatchedPrevious,
                scenario.BaseResult.UnmatchedCurrent));
        }

        [Fact]
        public void Constructor_RejectsDuplicateCandidateInAlternativeCandidates()
        {
            var scenario = CreateTwoCandidateScenario();
            var duplicateAlternatives = new[]
            {
                scenario.BaseResult.AlternativeCandidates[0],
                scenario.BaseResult.AlternativeCandidates[0],
            };

            Assert.Throws<ArgumentException>(() => CreateResultViaReflection(
                scenario.PreviousRun,
                scenario.CurrentRun,
                scenario.BaseResult.Candidates,
                scenario.BaseResult.SelectedMatches,
                duplicateAlternatives,
                scenario.BaseResult.UnmatchedPrevious,
                scenario.BaseResult.UnmatchedCurrent));
        }

        [Fact]
        public void Constructor_AcceptsValidCandidatePartition()
        {
            var scenario = CreateTwoCandidateScenario();

            var result = CreateResultViaReflection(
                scenario.PreviousRun,
                scenario.CurrentRun,
                scenario.BaseResult.Candidates,
                scenario.BaseResult.SelectedMatches,
                scenario.BaseResult.AlternativeCandidates,
                scenario.BaseResult.UnmatchedPrevious,
                scenario.BaseResult.UnmatchedCurrent);

            Assert.Equal(2, result.Candidates.Count);
            Assert.Single(result.SelectedMatches);
            Assert.Single(result.AlternativeCandidates);
        }

        // --- ClashRunMatchResult unmatched previous invariants ---

        [Fact]
        public void Constructor_RejectsUnmatchedPreviousContainingSelectedSlot()
        {
            var scenario = CreateTwoCandidateScenario();
            var invalidUnmatchedPrevious = new[] { scenario.PreviousOccurrences[0] };

            Assert.Throws<ArgumentException>(() => CreateResultViaReflection(
                scenario.PreviousRun,
                scenario.CurrentRun,
                scenario.BaseResult.Candidates,
                scenario.BaseResult.SelectedMatches,
                scenario.BaseResult.AlternativeCandidates,
                invalidUnmatchedPrevious,
                scenario.BaseResult.UnmatchedCurrent));
        }

        [Fact]
        public void Constructor_RejectsUnmatchedPreviousOmittingNonSelectedSlot()
        {
            var scenario = CreateTwoCandidateScenario();

            Assert.Throws<ArgumentException>(() => CreateResultViaReflection(
                scenario.PreviousRun,
                scenario.CurrentRun,
                scenario.BaseResult.Candidates,
                scenario.BaseResult.SelectedMatches,
                scenario.BaseResult.AlternativeCandidates,
                Array.Empty<ClashOccurrence>(),
                scenario.BaseResult.UnmatchedCurrent));
        }

        [Fact]
        public void Constructor_RejectsUnmatchedPreviousInWrongOrder()
        {
            var scenario = CreateOrderedUnmatchedScenario();
            var invalidUnmatchedPrevious = new[]
            {
                scenario.PreviousOccurrences[2],
                scenario.PreviousOccurrences[1],
            };

            Assert.Throws<ArgumentException>(() => CreateResultViaReflection(
                scenario.PreviousRun,
                scenario.CurrentRun,
                scenario.BaseResult.Candidates,
                scenario.BaseResult.SelectedMatches,
                scenario.BaseResult.AlternativeCandidates,
                invalidUnmatchedPrevious,
                scenario.BaseResult.UnmatchedCurrent));
        }

        [Fact]
        public void Constructor_RejectsUnmatchedPreviousWithExternalOccurrence()
        {
            var scenario = CreateTwoCandidateScenario();
            var externalPrevious = MakeOccurrence("External", Sigma, Sigma, "external-a", "external-b");
            var invalidUnmatchedPrevious = new[] { externalPrevious };

            Assert.Throws<ArgumentException>(() => CreateResultViaReflection(
                scenario.PreviousRun,
                scenario.CurrentRun,
                scenario.BaseResult.Candidates,
                scenario.BaseResult.SelectedMatches,
                scenario.BaseResult.AlternativeCandidates,
                invalidUnmatchedPrevious,
                scenario.BaseResult.UnmatchedCurrent));
        }

        // --- ClashRunMatchResult unmatched current invariants ---

        [Fact]
        public void Constructor_RejectsUnmatchedCurrentContainingSelectedSlot()
        {
            var scenario = CreateTwoCandidateScenario();
            var invalidUnmatchedCurrent = new[] { scenario.CurrentOccurrences[0] };

            Assert.Throws<ArgumentException>(() => CreateResultViaReflection(
                scenario.PreviousRun,
                scenario.CurrentRun,
                scenario.BaseResult.Candidates,
                scenario.BaseResult.SelectedMatches,
                scenario.BaseResult.AlternativeCandidates,
                scenario.BaseResult.UnmatchedPrevious,
                invalidUnmatchedCurrent));
        }

        [Fact]
        public void Constructor_RejectsUnmatchedCurrentOmittingNonSelectedSlot()
        {
            var scenario = CreateTwoCandidateScenario();

            Assert.Throws<ArgumentException>(() => CreateResultViaReflection(
                scenario.PreviousRun,
                scenario.CurrentRun,
                scenario.BaseResult.Candidates,
                scenario.BaseResult.SelectedMatches,
                scenario.BaseResult.AlternativeCandidates,
                scenario.BaseResult.UnmatchedPrevious,
                Array.Empty<ClashOccurrence>()));
        }

        [Fact]
        public void Constructor_RejectsUnmatchedCurrentInWrongOrder()
        {
            var scenario = CreateOrderedUnmatchedScenario();
            var invalidUnmatchedCurrent = new[]
            {
                scenario.CurrentOccurrences[2],
                scenario.CurrentOccurrences[1],
            };

            Assert.Throws<ArgumentException>(() => CreateResultViaReflection(
                scenario.PreviousRun,
                scenario.CurrentRun,
                scenario.BaseResult.Candidates,
                scenario.BaseResult.SelectedMatches,
                scenario.BaseResult.AlternativeCandidates,
                scenario.BaseResult.UnmatchedPrevious,
                invalidUnmatchedCurrent));
        }

        [Fact]
        public void Constructor_RejectsUnmatchedCurrentWithExternalOccurrence()
        {
            var scenario = CreateTwoCandidateScenario();
            var externalCurrent = MakeOccurrence("External", Sigma, Sigma, "external-a", "external-b");
            var invalidUnmatchedCurrent = new[] { externalCurrent };

            Assert.Throws<ArgumentException>(() => CreateResultViaReflection(
                scenario.PreviousRun,
                scenario.CurrentRun,
                scenario.BaseResult.Candidates,
                scenario.BaseResult.SelectedMatches,
                scenario.BaseResult.AlternativeCandidates,
                scenario.BaseResult.UnmatchedPrevious,
                invalidUnmatchedCurrent));
        }

        [Fact]
        public void Constructor_AcceptsDuplicateOccurrenceReferencesWhenExpectedSequenceMatches()
        {
            var repeatedPrevious = MakeOccurrence("Repeated", Sigma, Sigma, "repeat-pa", "repeat-pb");
            var repeatedCurrent = MakeOccurrence("Repeated", Sigma, Sigma, "repeat-ca", "repeat-cb");
            var previousRun = MakeRun("run-1", new[] { Sigma }, repeatedPrevious, repeatedPrevious);
            var currentRun = MakeRun("run-2", new[] { Sigma }, repeatedCurrent, repeatedCurrent);

            var result = CreateResultViaReflection(
                previousRun,
                currentRun,
                Array.Empty<ClashRunMatchCandidate>(),
                Array.Empty<ClashRunMatchCandidate>(),
                Array.Empty<ClashRunMatchCandidate>(),
                new[] { repeatedPrevious, repeatedPrevious },
                new[] { repeatedCurrent, repeatedCurrent });

            Assert.Equal(2, result.UnmatchedPrevious.Count);
            Assert.Same(repeatedPrevious, result.UnmatchedPrevious[0]);
            Assert.Same(repeatedPrevious, result.UnmatchedPrevious[1]);
            Assert.Same(repeatedCurrent, result.UnmatchedCurrent[0]);
            Assert.Same(repeatedCurrent, result.UnmatchedCurrent[1]);
        }
    }
}
