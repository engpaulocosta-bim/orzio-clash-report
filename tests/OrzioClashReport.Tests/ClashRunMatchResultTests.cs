using System;
using System.Collections.Generic;
using OrzioClashReport.Core.Abstractions;
using OrzioClashReport.Core.Matching;
using OrzioClashReport.Core.Model;

namespace OrzioClashReport.Tests
{
    /// <summary>
    /// Tests for ClashRunMatchCandidate and ClashRunMatchResult, obtained only through the public
    /// DeterministicClashRunComparer.Compare entry point, since both types have internal constructors.
    /// </summary>
    public class ClashRunMatchResultTests
    {
        private static ModelRevision MakeRevision(string company, string discipline, string modelName, string revision) =>
            new ModelRevision(new ModelIdentity(company, discipline, modelName), revision, $"{modelName}_{revision}.nwc", null, null, null);

        private static ClashObject MakeObject(string elementId) => new ClashObject(elementId, null, null, null, null, null);

        private static ClashOccurrence MakeOccurrence(
            string clashTestName, ModelRevision modelA, ModelRevision modelB, string elementIdA = "a", string elementIdB = "b") =>
            new ClashOccurrence(
                clashTestName,
                new ClashResult(clashTestName, ClashStatus.New, null, null, null, MakeObject(elementIdA), MakeObject(elementIdB), "g1"),
                modelA, modelB);

        private static RunManifest MakeManifest(string runId, params ModelRevision[] models) =>
            new RunManifest(runId, new DateTimeOffset(2026, 7, 10, 9, 0, 0, TimeSpan.Zero), models);

        private static CoordinationRun MakeRun(string runId, ModelRevision[] models, params ClashOccurrence[] occurrences) =>
            new CoordinationRun(MakeManifest(runId, models), occurrences);

        private static ClashMatchAssessment MakeAssessment(
            ClashOccurrence previous, ClashOccurrence current, ClashMatchConfidence confidence) =>
            new ClashMatchAssessment(
                previous, current, confidence,
                new[] { new MatchEvidence(MatchEvidenceKind.ClashTestName, MatchEvidenceVerdict.Supports, "same clash test", null, null) });

        /// <summary>Test-only fake: returns a configured assessment for an exact (previous, current) reference pair, or null otherwise. Records every call.</summary>
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

        private static readonly ModelRevision Sigma = MakeRevision("Sigma", "Structure", "Main", "R04");

        // --- ClashRunMatchCandidate, via a real Compare() result ---

        [Fact]
        public void Candidate_HasCorrectPreviousIndex()
        {
            var previousOccurrence = MakeOccurrence("Test 1", Sigma, Sigma);
            var currentOccurrence = MakeOccurrence("Test 1", Sigma, Sigma);
            var previousRun = MakeRun("run-1", new[] { Sigma }, previousOccurrence);
            var currentRun = MakeRun("run-2", new[] { Sigma }, currentOccurrence);

            var matcher = new ConfigurableClashMatcher();
            matcher.RespondWith(previousOccurrence, currentOccurrence, MakeAssessment(previousOccurrence, currentOccurrence, ClashMatchConfidence.High));

            var result = new DeterministicClashRunComparer(matcher).Compare(previousRun, currentRun);

            Assert.Equal(0, result.Candidates[0].PreviousIndex);
        }

        [Fact]
        public void Candidate_HasCorrectCurrentIndex()
        {
            var previousOccurrence = MakeOccurrence("Test 1", Sigma, Sigma);
            var currentOccurrence = MakeOccurrence("Test 1", Sigma, Sigma);
            var previousRun = MakeRun("run-1", new[] { Sigma }, previousOccurrence);
            var currentRun = MakeRun("run-2", new[] { Sigma }, currentOccurrence);

            var matcher = new ConfigurableClashMatcher();
            matcher.RespondWith(previousOccurrence, currentOccurrence, MakeAssessment(previousOccurrence, currentOccurrence, ClashMatchConfidence.High));

            var result = new DeterministicClashRunComparer(matcher).Compare(previousRun, currentRun);

            Assert.Equal(0, result.Candidates[0].CurrentIndex);
        }

        [Fact]
        public void Candidate_HasCorrectAssessment()
        {
            var previousOccurrence = MakeOccurrence("Test 1", Sigma, Sigma);
            var currentOccurrence = MakeOccurrence("Test 1", Sigma, Sigma);
            var previousRun = MakeRun("run-1", new[] { Sigma }, previousOccurrence);
            var currentRun = MakeRun("run-2", new[] { Sigma }, currentOccurrence);

            var matcher = new ConfigurableClashMatcher();
            var assessment = MakeAssessment(previousOccurrence, currentOccurrence, ClashMatchConfidence.High);
            matcher.RespondWith(previousOccurrence, currentOccurrence, assessment);

            var result = new DeterministicClashRunComparer(matcher).Compare(previousRun, currentRun);

            Assert.Same(assessment, result.Candidates[0].Assessment);
        }

        [Fact]
        public void Candidate_ToString_IncludesIndexes()
        {
            var previousOccurrence = MakeOccurrence("Test 1", Sigma, Sigma);
            var currentOccurrence = MakeOccurrence("Test 1", Sigma, Sigma);
            var previousRun = MakeRun("run-1", new[] { Sigma }, previousOccurrence);
            var currentRun = MakeRun("run-2", new[] { Sigma }, currentOccurrence);

            var matcher = new ConfigurableClashMatcher();
            matcher.RespondWith(previousOccurrence, currentOccurrence, MakeAssessment(previousOccurrence, currentOccurrence, ClashMatchConfidence.High));

            var result = new DeterministicClashRunComparer(matcher).Compare(previousRun, currentRun);

            string text = result.Candidates[0].ToString();
            Assert.Contains("0", text);
        }

        [Fact]
        public void Candidate_ToString_IncludesConfidence()
        {
            var previousOccurrence = MakeOccurrence("Test 1", Sigma, Sigma);
            var currentOccurrence = MakeOccurrence("Test 1", Sigma, Sigma);
            var previousRun = MakeRun("run-1", new[] { Sigma }, previousOccurrence);
            var currentRun = MakeRun("run-2", new[] { Sigma }, currentOccurrence);

            var matcher = new ConfigurableClashMatcher();
            matcher.RespondWith(previousOccurrence, currentOccurrence, MakeAssessment(previousOccurrence, currentOccurrence, ClashMatchConfidence.High));

            var result = new DeterministicClashRunComparer(matcher).Compare(previousRun, currentRun);

            Assert.Contains("High", result.Candidates[0].ToString());
        }

        // --- ClashRunMatchResult: immutability and identity ---

        [Fact]
        public void Result_CandidatesList_CannotBeMutated()
        {
            var previousOccurrence = MakeOccurrence("Test 1", Sigma, Sigma);
            var currentOccurrence = MakeOccurrence("Test 1", Sigma, Sigma);
            var previousRun = MakeRun("run-1", new[] { Sigma }, previousOccurrence);
            var currentRun = MakeRun("run-2", new[] { Sigma }, currentOccurrence);

            var matcher = new ConfigurableClashMatcher();
            matcher.RespondWith(previousOccurrence, currentOccurrence, MakeAssessment(previousOccurrence, currentOccurrence, ClashMatchConfidence.High));

            var result = new DeterministicClashRunComparer(matcher).Compare(previousRun, currentRun);

            var mutableView = Assert.IsAssignableFrom<IList<ClashRunMatchCandidate>>(result.Candidates);
            Assert.Throws<NotSupportedException>(() => mutableView.Add(result.Candidates[0]));
        }

        [Fact]
        public void Result_MutatingOriginalOccurrenceArray_DoesNotAlterResult()
        {
            var previousOccurrence = MakeOccurrence("Test 1", Sigma, Sigma);
            var currentOccurrence = MakeOccurrence("Test 1", Sigma, Sigma);
            var previousOccurrences = new List<ClashOccurrence> { previousOccurrence };
            var previousRun = new CoordinationRun(MakeManifest("run-1", Sigma), previousOccurrences);
            var currentRun = MakeRun("run-2", new[] { Sigma }, currentOccurrence);

            var matcher = new ConfigurableClashMatcher();
            matcher.RespondWith(previousOccurrence, currentOccurrence, MakeAssessment(previousOccurrence, currentOccurrence, ClashMatchConfidence.High));

            var result = new DeterministicClashRunComparer(matcher).Compare(previousRun, currentRun);
            previousOccurrences.Clear();

            Assert.Single(result.PreviousRun.Occurrences);
            Assert.Single(result.SelectedMatches);
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
            var previousOccurrence = MakeOccurrence("Test 1", Sigma, Sigma);
            var currentOccurrence = MakeOccurrence("Test 1", Sigma, Sigma);
            var previousRun = MakeRun("run-previous", new[] { Sigma }, previousOccurrence);
            var currentRun = MakeRun("run-current", new[] { Sigma }, currentOccurrence);

            var matcher = new ConfigurableClashMatcher();
            matcher.RespondWith(previousOccurrence, currentOccurrence, MakeAssessment(previousOccurrence, currentOccurrence, ClashMatchConfidence.High));

            var result = new DeterministicClashRunComparer(matcher).Compare(previousRun, currentRun);
            string text = result.ToString();

            Assert.Contains("run-previous", text);
            Assert.Contains("run-current", text);
            Assert.Contains("1", text);
        }
    }
}
