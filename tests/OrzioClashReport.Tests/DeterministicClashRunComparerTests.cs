using System;
using System.Collections.Generic;
using System.Linq;
using OrzioClashReport.Core.Abstractions;
using OrzioClashReport.Core.Matching;
using OrzioClashReport.Core.Model;

namespace OrzioClashReport.Tests
{
    /// <summary>
    /// Tests for DeterministicClashRunComparer: candidate generation over every previous/current pair, matcher
    /// contract enforcement, greedy one-to-one selection by confidence precedence and index tie-break,
    /// alternatives, unmatched-by-index computation, empty runs, the documented greedy non-optimality, and
    /// determinism.
    /// </summary>
    public class DeterministicClashRunComparerTests
    {
        private static ModelRevision MakeRevision(string company, string discipline, string modelName, string revision) =>
            new ModelRevision(new ModelIdentity(company, discipline, modelName), revision, $"{modelName}_{revision}.nwc", null, null, null);

        private static ClashObject MakeObject(string elementId) => new ClashObject(elementId, null, null, null, null, null);

        private static readonly ModelRevision Sigma = MakeRevision("Sigma", "Structure", "Main", "R04");

        private static ClashOccurrence MakeOccurrence(string label) =>
            new ClashOccurrence(
                "Test 1",
                new ClashResult(label, ClashStatus.New, null, null, null, MakeObject(label + "-a"), MakeObject(label + "-b"), null),
                Sigma, Sigma);

        private static ClashOccurrence[] MakeOccurrences(string prefix, int count) =>
            Enumerable.Range(0, count).Select(i => MakeOccurrence($"{prefix}{i}")).ToArray();

        private static readonly ExecutedClashTest Test1ForSigma = new ExecutedClashTest("Test 1", Sigma.Identity, Sigma.Identity);

        private static RunManifest MakeManifest(string runId) =>
            new RunManifest(runId, new DateTimeOffset(2026, 7, 10, 9, 0, 0, TimeSpan.Zero), new[] { Sigma }, new[] { Test1ForSigma });

        private static CoordinationRun MakeRun(string runId, params ClashOccurrence[] occurrences) =>
            new CoordinationRun(MakeManifest(runId), occurrences);

        private static ClashMatchAssessment MakeAssessment(
            ClashOccurrence previous, ClashOccurrence current, ClashMatchConfidence confidence) =>
            new ClashMatchAssessment(
                previous, current, confidence,
                new[] { new MatchEvidence(MatchEvidenceKind.ClashTestName, MatchEvidenceVerdict.Supports, "same clash test", null, null) });

        /// <summary>Test-only fake: returns a configured assessment (or a computed one) for an exact (previous, current) reference pair; records every call it receives, in order.</summary>
        private sealed class ConfigurableClashMatcher : IClashMatcher
        {
            private readonly Dictionary<(ClashOccurrence Previous, ClashOccurrence Current), Func<ClashMatchAssessment?>> _responses =
                new Dictionary<(ClashOccurrence, ClashOccurrence), Func<ClashMatchAssessment?>>();

            public List<(ClashOccurrence Previous, ClashOccurrence Current)> Calls { get; } =
                new List<(ClashOccurrence, ClashOccurrence)>();

            public void RespondWith(ClashOccurrence previous, ClashOccurrence current, ClashMatchAssessment assessment) =>
                _responses[(previous, current)] = () => assessment;

            public void RespondWith(ClashOccurrence previous, ClashOccurrence current, Func<ClashMatchAssessment?> responder) =>
                _responses[(previous, current)] = responder;

            public void ThrowFor(ClashOccurrence previous, ClashOccurrence current, Exception exception) =>
                _responses[(previous, current)] = () => throw exception;

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

                Calls.Add((previousOccurrence, currentOccurrence));

                return _responses.TryGetValue((previousOccurrence, currentOccurrence), out var responder) ? responder() : null;
            }
        }

        // --- 15.3 Candidate generation ---

        [Fact]
        public void Constructor_RejectsNullMatcher()
        {
            Assert.Throws<ArgumentNullException>(() => new DeterministicClashRunComparer(null!));
        }

        [Fact]
        public void Compare_RejectsNullPreviousRun()
        {
            var comparer = new DeterministicClashRunComparer(new ConfigurableClashMatcher());

            Assert.Throws<ArgumentNullException>(() => comparer.Compare(null!, MakeRun("run-2")));
        }

        [Fact]
        public void Compare_RejectsNullCurrentRun()
        {
            var comparer = new DeterministicClashRunComparer(new ConfigurableClashMatcher());

            Assert.Throws<ArgumentNullException>(() => comparer.Compare(MakeRun("run-1"), null!));
        }

        [Fact]
        public void Compare_CallsMatcherNTimesM()
        {
            var previousOccurrences = MakeOccurrences("p", 2);
            var currentOccurrences = MakeOccurrences("c", 3);
            var matcher = new ConfigurableClashMatcher();
            var comparer = new DeterministicClashRunComparer(matcher);

            comparer.Compare(MakeRun("run-1", previousOccurrences), MakeRun("run-2", currentOccurrences));

            Assert.Equal(6, matcher.Calls.Count);
        }

        [Fact]
        public void Compare_CallsMatcherInPreviousMajorCurrentMinorOrder()
        {
            var previousOccurrences = MakeOccurrences("p", 2);
            var currentOccurrences = MakeOccurrences("c", 2);
            var matcher = new ConfigurableClashMatcher();
            var comparer = new DeterministicClashRunComparer(matcher);

            comparer.Compare(MakeRun("run-1", previousOccurrences), MakeRun("run-2", currentOccurrences));

            Assert.Equal(
                new[]
                {
                    (previousOccurrences[0], currentOccurrences[0]),
                    (previousOccurrences[0], currentOccurrences[1]),
                    (previousOccurrences[1], currentOccurrences[0]),
                    (previousOccurrences[1], currentOccurrences[1]),
                },
                matcher.Calls);
        }

        [Fact]
        public void Compare_NullAssessment_DoesNotCreateCandidate()
        {
            var previousOccurrences = MakeOccurrences("p", 1);
            var currentOccurrences = MakeOccurrences("c", 1);
            var matcher = new ConfigurableClashMatcher();
            var comparer = new DeterministicClashRunComparer(matcher);

            var result = comparer.Compare(MakeRun("run-1", previousOccurrences), MakeRun("run-2", currentOccurrences));

            Assert.Empty(result.Candidates);
        }

        [Fact]
        public void Compare_NonNullAssessment_CreatesCandidateWithCorrectIndexes()
        {
            var previousOccurrences = MakeOccurrences("p", 2);
            var currentOccurrences = MakeOccurrences("c", 2);
            var matcher = new ConfigurableClashMatcher();
            matcher.RespondWith(
                previousOccurrences[1], currentOccurrences[0],
                MakeAssessment(previousOccurrences[1], currentOccurrences[0], ClashMatchConfidence.High));

            var result = new DeterministicClashRunComparer(matcher).Compare(
                MakeRun("run-1", previousOccurrences), MakeRun("run-2", currentOccurrences));

            var candidate = Assert.Single(result.Candidates);
            Assert.Equal(1, candidate.PreviousIndex);
            Assert.Equal(0, candidate.CurrentIndex);
        }

        [Fact]
        public void Compare_CandidatesPreservesGenerationOrder()
        {
            var previousOccurrences = MakeOccurrences("p", 2);
            var currentOccurrences = MakeOccurrences("c", 2);
            var matcher = new ConfigurableClashMatcher();
            matcher.RespondWith(previousOccurrences[1], currentOccurrences[0], MakeAssessment(previousOccurrences[1], currentOccurrences[0], ClashMatchConfidence.High));
            matcher.RespondWith(previousOccurrences[0], currentOccurrences[1], MakeAssessment(previousOccurrences[0], currentOccurrences[1], ClashMatchConfidence.High));

            var result = new DeterministicClashRunComparer(matcher).Compare(
                MakeRun("run-1", previousOccurrences), MakeRun("run-2", currentOccurrences));

            // Generation order is previous-major, current-minor: (p0,c1) is generated before (p1,c0).
            Assert.Equal(0, result.Candidates[0].PreviousIndex);
            Assert.Equal(1, result.Candidates[0].CurrentIndex);
            Assert.Equal(1, result.Candidates[1].PreviousIndex);
            Assert.Equal(0, result.Candidates[1].CurrentIndex);
        }

        [Fact]
        public void Compare_PropagatesMatcherException()
        {
            var previousOccurrences = MakeOccurrences("p", 1);
            var currentOccurrences = MakeOccurrences("c", 1);
            var matcher = new ConfigurableClashMatcher();
            matcher.ThrowFor(previousOccurrences[0], currentOccurrences[0], new InvalidOperationException("boom"));

            var comparer = new DeterministicClashRunComparer(matcher);

            var ex = Assert.Throws<InvalidOperationException>(
                () => comparer.Compare(MakeRun("run-1", previousOccurrences), MakeRun("run-2", currentOccurrences)));
            Assert.Equal("boom", ex.Message);
        }

        // --- 15.4 Matcher contract violations ---

        [Fact]
        public void Compare_AssessmentWithDifferentPreviousOccurrence_ThrowsInvalidOperationException()
        {
            var previousOccurrences = MakeOccurrences("p", 1);
            var currentOccurrences = MakeOccurrences("c", 1);
            var wrongPrevious = MakeOccurrence("wrong");
            var matcher = new ConfigurableClashMatcher();
            matcher.RespondWith(
                previousOccurrences[0], currentOccurrences[0],
                MakeAssessment(wrongPrevious, currentOccurrences[0], ClashMatchConfidence.High));

            var comparer = new DeterministicClashRunComparer(matcher);

            var ex = Assert.Throws<InvalidOperationException>(
                () => comparer.Compare(MakeRun("run-1", previousOccurrences), MakeRun("run-2", currentOccurrences)));
            Assert.Contains("previous index 0", ex.Message);
            Assert.Contains("current index 0", ex.Message);
            Assert.Contains("PreviousOccurrence", ex.Message);
        }

        [Fact]
        public void Compare_AssessmentWithDifferentCurrentOccurrence_ThrowsInvalidOperationException()
        {
            var previousOccurrences = MakeOccurrences("p", 1);
            var currentOccurrences = MakeOccurrences("c", 1);
            var wrongCurrent = MakeOccurrence("wrong");
            var matcher = new ConfigurableClashMatcher();
            matcher.RespondWith(
                previousOccurrences[0], currentOccurrences[0],
                MakeAssessment(previousOccurrences[0], wrongCurrent, ClashMatchConfidence.High));

            var comparer = new DeterministicClashRunComparer(matcher);

            var ex = Assert.Throws<InvalidOperationException>(
                () => comparer.Compare(MakeRun("run-1", previousOccurrences), MakeRun("run-2", currentOccurrences)));
            Assert.Contains("previous index 0", ex.Message);
            Assert.Contains("current index 0", ex.Message);
            Assert.Contains("CurrentOccurrence", ex.Message);
        }

        [Fact]
        public void Compare_AssessmentWithSwappedOccurrences_ThrowsInvalidOperationException()
        {
            var previousOccurrences = MakeOccurrences("p", 1);
            var currentOccurrences = MakeOccurrences("c", 1);
            var matcher = new ConfigurableClashMatcher();
            // Swapped: assessment.PreviousOccurrence is actually the current occurrence, and vice versa.
            matcher.RespondWith(
                previousOccurrences[0], currentOccurrences[0],
                MakeAssessment(currentOccurrences[0], previousOccurrences[0], ClashMatchConfidence.High));

            var comparer = new DeterministicClashRunComparer(matcher);

            Assert.Throws<InvalidOperationException>(
                () => comparer.Compare(MakeRun("run-1", previousOccurrences), MakeRun("run-2", currentOccurrences)));
        }

        // --- 15.5 Selection by confidence ---

        [Fact]
        public void Compare_HighBeatsMedium_ForSamePreviousIndex()
        {
            var previousOccurrences = MakeOccurrences("p", 1);
            var currentOccurrences = MakeOccurrences("c", 2);
            var matcher = new ConfigurableClashMatcher();
            matcher.RespondWith(previousOccurrences[0], currentOccurrences[0], MakeAssessment(previousOccurrences[0], currentOccurrences[0], ClashMatchConfidence.Medium));
            matcher.RespondWith(previousOccurrences[0], currentOccurrences[1], MakeAssessment(previousOccurrences[0], currentOccurrences[1], ClashMatchConfidence.High));

            var result = new DeterministicClashRunComparer(matcher).Compare(
                MakeRun("run-1", previousOccurrences), MakeRun("run-2", currentOccurrences));

            var selected = Assert.Single(result.SelectedMatches);
            Assert.Equal(1, selected.CurrentIndex);
        }

        [Fact]
        public void Compare_HighBeatsMedium_ForSameCurrentIndex()
        {
            var previousOccurrences = MakeOccurrences("p", 2);
            var currentOccurrences = MakeOccurrences("c", 1);
            var matcher = new ConfigurableClashMatcher();
            matcher.RespondWith(previousOccurrences[0], currentOccurrences[0], MakeAssessment(previousOccurrences[0], currentOccurrences[0], ClashMatchConfidence.Medium));
            matcher.RespondWith(previousOccurrences[1], currentOccurrences[0], MakeAssessment(previousOccurrences[1], currentOccurrences[0], ClashMatchConfidence.High));

            var result = new DeterministicClashRunComparer(matcher).Compare(
                MakeRun("run-1", previousOccurrences), MakeRun("run-2", currentOccurrences));

            var selected = Assert.Single(result.SelectedMatches);
            Assert.Equal(1, selected.PreviousIndex);
        }

        [Fact]
        public void Compare_MediumBeatsLow()
        {
            var previousOccurrences = MakeOccurrences("p", 1);
            var currentOccurrences = MakeOccurrences("c", 2);
            var matcher = new ConfigurableClashMatcher();
            matcher.RespondWith(previousOccurrences[0], currentOccurrences[0], MakeAssessment(previousOccurrences[0], currentOccurrences[0], ClashMatchConfidence.Low));
            matcher.RespondWith(previousOccurrences[0], currentOccurrences[1], MakeAssessment(previousOccurrences[0], currentOccurrences[1], ClashMatchConfidence.Medium));

            var result = new DeterministicClashRunComparer(matcher).Compare(
                MakeRun("run-1", previousOccurrences), MakeRun("run-2", currentOccurrences));

            var selected = Assert.Single(result.SelectedMatches);
            Assert.Equal(1, selected.CurrentIndex);
        }

        // --- 15.6 Index tie-break ---

        [Fact]
        public void Compare_SameConfidenceSamePrevious_LowerCurrentIndexWins()
        {
            var previousOccurrences = MakeOccurrences("p", 1);
            var currentOccurrences = MakeOccurrences("c", 2);
            var matcher = new ConfigurableClashMatcher();
            matcher.RespondWith(previousOccurrences[0], currentOccurrences[0], MakeAssessment(previousOccurrences[0], currentOccurrences[0], ClashMatchConfidence.High));
            matcher.RespondWith(previousOccurrences[0], currentOccurrences[1], MakeAssessment(previousOccurrences[0], currentOccurrences[1], ClashMatchConfidence.High));

            var result = new DeterministicClashRunComparer(matcher).Compare(
                MakeRun("run-1", previousOccurrences), MakeRun("run-2", currentOccurrences));

            var selected = Assert.Single(result.SelectedMatches);
            Assert.Equal(0, selected.CurrentIndex);
        }

        [Fact]
        public void Compare_SameConfidenceSameCurrent_LowerPreviousIndexWins()
        {
            var previousOccurrences = MakeOccurrences("p", 2);
            var currentOccurrences = MakeOccurrences("c", 1);
            var matcher = new ConfigurableClashMatcher();
            matcher.RespondWith(previousOccurrences[0], currentOccurrences[0], MakeAssessment(previousOccurrences[0], currentOccurrences[0], ClashMatchConfidence.High));
            matcher.RespondWith(previousOccurrences[1], currentOccurrences[0], MakeAssessment(previousOccurrences[1], currentOccurrences[0], ClashMatchConfidence.High));

            var result = new DeterministicClashRunComparer(matcher).Compare(
                MakeRun("run-1", previousOccurrences), MakeRun("run-2", currentOccurrences));

            var selected = Assert.Single(result.SelectedMatches);
            Assert.Equal(0, selected.PreviousIndex);
        }

        // --- 15.7 One-to-one ---

        [Fact]
        public void Compare_NoPreviousIndexSelectedTwice()
        {
            var previousOccurrences = MakeOccurrences("p", 1);
            var currentOccurrences = MakeOccurrences("c", 2);
            var matcher = new ConfigurableClashMatcher();
            matcher.RespondWith(previousOccurrences[0], currentOccurrences[0], MakeAssessment(previousOccurrences[0], currentOccurrences[0], ClashMatchConfidence.High));
            matcher.RespondWith(previousOccurrences[0], currentOccurrences[1], MakeAssessment(previousOccurrences[0], currentOccurrences[1], ClashMatchConfidence.High));

            var result = new DeterministicClashRunComparer(matcher).Compare(
                MakeRun("run-1", previousOccurrences), MakeRun("run-2", currentOccurrences));

            var distinctPreviousIndexes = result.SelectedMatches.Select(c => c.PreviousIndex).Distinct().Count();
            Assert.Equal(result.SelectedMatches.Count, distinctPreviousIndexes);
        }

        [Fact]
        public void Compare_NoCurrentIndexSelectedTwice()
        {
            var previousOccurrences = MakeOccurrences("p", 2);
            var currentOccurrences = MakeOccurrences("c", 1);
            var matcher = new ConfigurableClashMatcher();
            matcher.RespondWith(previousOccurrences[0], currentOccurrences[0], MakeAssessment(previousOccurrences[0], currentOccurrences[0], ClashMatchConfidence.High));
            matcher.RespondWith(previousOccurrences[1], currentOccurrences[0], MakeAssessment(previousOccurrences[1], currentOccurrences[0], ClashMatchConfidence.High));

            var result = new DeterministicClashRunComparer(matcher).Compare(
                MakeRun("run-1", previousOccurrences), MakeRun("run-2", currentOccurrences));

            var distinctCurrentIndexes = result.SelectedMatches.Select(c => c.CurrentIndex).Distinct().Count();
            Assert.Equal(result.SelectedMatches.Count, distinctCurrentIndexes);
        }

        [Fact]
        public void Compare_ConflictingCandidate_GoesToAlternatives()
        {
            var previousOccurrences = MakeOccurrences("p", 1);
            var currentOccurrences = MakeOccurrences("c", 2);
            var matcher = new ConfigurableClashMatcher();
            matcher.RespondWith(previousOccurrences[0], currentOccurrences[0], MakeAssessment(previousOccurrences[0], currentOccurrences[0], ClashMatchConfidence.High));
            matcher.RespondWith(previousOccurrences[0], currentOccurrences[1], MakeAssessment(previousOccurrences[0], currentOccurrences[1], ClashMatchConfidence.High));

            var result = new DeterministicClashRunComparer(matcher).Compare(
                MakeRun("run-1", previousOccurrences), MakeRun("run-2", currentOccurrences));

            var alternative = Assert.Single(result.AlternativeCandidates);
            Assert.Equal(1, alternative.CurrentIndex);
        }

        [Fact]
        public void Compare_NonConflictingCandidate_RemainsSelectable()
        {
            var previousOccurrences = MakeOccurrences("p", 2);
            var currentOccurrences = MakeOccurrences("c", 2);
            var matcher = new ConfigurableClashMatcher();
            matcher.RespondWith(previousOccurrences[0], currentOccurrences[0], MakeAssessment(previousOccurrences[0], currentOccurrences[0], ClashMatchConfidence.High));
            matcher.RespondWith(previousOccurrences[1], currentOccurrences[1], MakeAssessment(previousOccurrences[1], currentOccurrences[1], ClashMatchConfidence.High));

            var result = new DeterministicClashRunComparer(matcher).Compare(
                MakeRun("run-1", previousOccurrences), MakeRun("run-2", currentOccurrences));

            Assert.Equal(2, result.SelectedMatches.Count);
        }

        // --- 15.8 Alternatives ---

        [Fact]
        public void Compare_AlternativeCandidates_PreserveGenerationOrder()
        {
            var previousOccurrences = MakeOccurrences("p", 1);
            var currentOccurrences = MakeOccurrences("c", 3);
            var matcher = new ConfigurableClashMatcher();
            matcher.RespondWith(previousOccurrences[0], currentOccurrences[0], MakeAssessment(previousOccurrences[0], currentOccurrences[0], ClashMatchConfidence.High));
            matcher.RespondWith(previousOccurrences[0], currentOccurrences[1], MakeAssessment(previousOccurrences[0], currentOccurrences[1], ClashMatchConfidence.High));
            matcher.RespondWith(previousOccurrences[0], currentOccurrences[2], MakeAssessment(previousOccurrences[0], currentOccurrences[2], ClashMatchConfidence.High));

            var result = new DeterministicClashRunComparer(matcher).Compare(
                MakeRun("run-1", previousOccurrences), MakeRun("run-2", currentOccurrences));

            Assert.Equal(2, result.AlternativeCandidates.Count);
            Assert.Equal(1, result.AlternativeCandidates[0].CurrentIndex);
            Assert.Equal(2, result.AlternativeCandidates[1].CurrentIndex);
        }

        [Fact]
        public void Compare_AlternativeCandidate_RetainsIntactAssessmentAndEvidence()
        {
            var previousOccurrences = MakeOccurrences("p", 1);
            var currentOccurrences = MakeOccurrences("c", 2);
            var matcher = new ConfigurableClashMatcher();
            matcher.RespondWith(previousOccurrences[0], currentOccurrences[0], MakeAssessment(previousOccurrences[0], currentOccurrences[0], ClashMatchConfidence.High));
            var alternativeAssessment = MakeAssessment(previousOccurrences[0], currentOccurrences[1], ClashMatchConfidence.High);
            matcher.RespondWith(previousOccurrences[0], currentOccurrences[1], alternativeAssessment);

            var result = new DeterministicClashRunComparer(matcher).Compare(
                MakeRun("run-1", previousOccurrences), MakeRun("run-2", currentOccurrences));

            var alternative = Assert.Single(result.AlternativeCandidates);
            Assert.Same(alternativeAssessment, alternative.Assessment);
            Assert.NotEmpty(alternative.Assessment.Evidence);
        }

        [Fact]
        public void Compare_UnmatchedOccurrence_CanHaveAlternativeCandidate()
        {
            var previousOccurrences = MakeOccurrences("p", 1);
            var currentOccurrences = MakeOccurrences("c", 2);
            var matcher = new ConfigurableClashMatcher();
            matcher.RespondWith(previousOccurrences[0], currentOccurrences[0], MakeAssessment(previousOccurrences[0], currentOccurrences[0], ClashMatchConfidence.High));
            matcher.RespondWith(previousOccurrences[0], currentOccurrences[1], MakeAssessment(previousOccurrences[0], currentOccurrences[1], ClashMatchConfidence.High));

            var result = new DeterministicClashRunComparer(matcher).Compare(
                MakeRun("run-1", previousOccurrences), MakeRun("run-2", currentOccurrences));

            // currentOccurrences[1] is unmatched, yet it still has an alternative candidate -- unmatched != no candidate.
            Assert.Contains(currentOccurrences[1], result.UnmatchedCurrent);
            Assert.Contains(result.AlternativeCandidates, c => c.CurrentIndex == 1);
        }

        // --- 15.9 Unmatched and duplicates ---

        [Fact]
        public void Compare_NoCandidates_AllPreviousAndCurrentAreUnmatched()
        {
            var previousOccurrences = MakeOccurrences("p", 2);
            var currentOccurrences = MakeOccurrences("c", 2);
            var matcher = new ConfigurableClashMatcher();

            var result = new DeterministicClashRunComparer(matcher).Compare(
                MakeRun("run-1", previousOccurrences), MakeRun("run-2", currentOccurrences));

            Assert.Equal(previousOccurrences, result.UnmatchedPrevious);
            Assert.Equal(currentOccurrences, result.UnmatchedCurrent);
        }

        [Fact]
        public void Compare_SelectedMatch_RemovesOnlyInvolvedSlots()
        {
            var previousOccurrences = MakeOccurrences("p", 2);
            var currentOccurrences = MakeOccurrences("c", 2);
            var matcher = new ConfigurableClashMatcher();
            matcher.RespondWith(previousOccurrences[0], currentOccurrences[0], MakeAssessment(previousOccurrences[0], currentOccurrences[0], ClashMatchConfidence.High));

            var result = new DeterministicClashRunComparer(matcher).Compare(
                MakeRun("run-1", previousOccurrences), MakeRun("run-2", currentOccurrences));

            Assert.Equal(new[] { previousOccurrences[1] }, result.UnmatchedPrevious);
            Assert.Equal(new[] { currentOccurrences[1] }, result.UnmatchedCurrent);
        }

        [Fact]
        public void Compare_UnmatchedPreservesOriginalOrder()
        {
            var previousOccurrences = MakeOccurrences("p", 3);
            var currentOccurrences = MakeOccurrences("c", 1);
            var matcher = new ConfigurableClashMatcher();
            matcher.RespondWith(previousOccurrences[1], currentOccurrences[0], MakeAssessment(previousOccurrences[1], currentOccurrences[0], ClashMatchConfidence.High));

            var result = new DeterministicClashRunComparer(matcher).Compare(
                MakeRun("run-1", previousOccurrences), MakeRun("run-2", currentOccurrences));

            Assert.Equal(new[] { previousOccurrences[0], previousOccurrences[2] }, result.UnmatchedPrevious);
        }

        [Fact]
        public void Compare_SameReferenceRepeatedInTwoSlots_IsTrackedByIndex()
        {
            var sharedOccurrence = MakeOccurrence("shared");
            var currentOccurrences = MakeOccurrences("c", 2);
            var matcher = new ConfigurableClashMatcher();
            matcher.RespondWith(sharedOccurrence, currentOccurrences[0], MakeAssessment(sharedOccurrence, currentOccurrences[0], ClashMatchConfidence.High));

            var result = new DeterministicClashRunComparer(matcher).Compare(
                MakeRun("run-1", sharedOccurrence, sharedOccurrence), MakeRun("run-2", currentOccurrences));

            // Only previous index 0 was consumed by the selected match; index 1 (same reference) is unmatched.
            Assert.Single(result.UnmatchedPrevious);
            Assert.Same(sharedOccurrence, result.UnmatchedPrevious[0]);
            Assert.Single(result.SelectedMatches);
            Assert.Equal(0, result.SelectedMatches[0].PreviousIndex);
        }

        [Fact]
        public void Compare_DuplicateSlot_OneMatchedOneUnmatched_DoesNotApplyDistinct()
        {
            var sharedOccurrence = MakeOccurrence("shared");
            var currentOccurrences = MakeOccurrences("c", 1);
            var matcher = new ConfigurableClashMatcher();
            matcher.RespondWith(sharedOccurrence, currentOccurrences[0], MakeAssessment(sharedOccurrence, currentOccurrences[0], ClashMatchConfidence.High));

            var result = new DeterministicClashRunComparer(matcher).Compare(
                MakeRun("run-1", sharedOccurrence, sharedOccurrence), MakeRun("run-2", currentOccurrences));

            Assert.Equal(2, result.Candidates.Count);
            Assert.Single(result.UnmatchedPrevious);
        }

        // --- 15.10 Empty runs ---

        [Fact]
        public void Compare_BothRunsEmpty_ProducesEmptyResult()
        {
            var matcher = new ConfigurableClashMatcher();

            var result = new DeterministicClashRunComparer(matcher).Compare(MakeRun("run-1"), MakeRun("run-2"));

            Assert.Empty(result.Candidates);
            Assert.Empty(result.SelectedMatches);
            Assert.Empty(result.AlternativeCandidates);
            Assert.Empty(result.UnmatchedPrevious);
            Assert.Empty(result.UnmatchedCurrent);
            Assert.Empty(matcher.Calls);
        }

        [Fact]
        public void Compare_EmptyPrevious_AllCurrentAreUnmatched_MatcherNotCalled()
        {
            var currentOccurrences = MakeOccurrences("c", 2);
            var matcher = new ConfigurableClashMatcher();

            var result = new DeterministicClashRunComparer(matcher).Compare(MakeRun("run-1"), MakeRun("run-2", currentOccurrences));

            Assert.Equal(currentOccurrences, result.UnmatchedCurrent);
            Assert.Empty(result.UnmatchedPrevious);
            Assert.Empty(matcher.Calls);
        }

        [Fact]
        public void Compare_EmptyCurrent_AllPreviousAreUnmatched_MatcherNotCalled()
        {
            var previousOccurrences = MakeOccurrences("p", 2);
            var matcher = new ConfigurableClashMatcher();

            var result = new DeterministicClashRunComparer(matcher).Compare(MakeRun("run-1", previousOccurrences), MakeRun("run-2"));

            Assert.Equal(previousOccurrences, result.UnmatchedPrevious);
            Assert.Empty(result.UnmatchedCurrent);
            Assert.Empty(matcher.Calls);
        }

        // --- 15.11 Greedy policy is deterministic but not globally optimal ---

        [Fact]
        public void GreedyPolicy_IsDeterministicButNotGloballyOptimal()
        {
            var previousOccurrences = MakeOccurrences("p", 2);
            var currentOccurrences = MakeOccurrences("c", 2);
            var matcher = new ConfigurableClashMatcher();
            // All High: p0-c0, p0-c1, p1-c0. A globally optimal assignment could pick p0-c1 and p1-c0
            // (matching both previous occurrences), but the greedy policy picks p0-c0 first by index
            // tie-break, leaving p1 and c1 unmatched even though p1-c0 was a viable alternative.
            matcher.RespondWith(previousOccurrences[0], currentOccurrences[0], MakeAssessment(previousOccurrences[0], currentOccurrences[0], ClashMatchConfidence.High));
            matcher.RespondWith(previousOccurrences[0], currentOccurrences[1], MakeAssessment(previousOccurrences[0], currentOccurrences[1], ClashMatchConfidence.High));
            matcher.RespondWith(previousOccurrences[1], currentOccurrences[0], MakeAssessment(previousOccurrences[1], currentOccurrences[0], ClashMatchConfidence.High));

            var result = new DeterministicClashRunComparer(matcher).Compare(
                MakeRun("run-1", previousOccurrences), MakeRun("run-2", currentOccurrences));

            var selected = Assert.Single(result.SelectedMatches);
            Assert.Equal(0, selected.PreviousIndex);
            Assert.Equal(0, selected.CurrentIndex);

            Assert.Equal(2, result.AlternativeCandidates.Count);
            Assert.Contains(result.AlternativeCandidates, c => c.PreviousIndex == 0 && c.CurrentIndex == 1);
            Assert.Contains(result.AlternativeCandidates, c => c.PreviousIndex == 1 && c.CurrentIndex == 0);

            Assert.Equal(new[] { previousOccurrences[1] }, result.UnmatchedPrevious);
            Assert.Equal(new[] { currentOccurrences[1] }, result.UnmatchedCurrent);
        }

        // --- 15.12 Determinism ---

        [Fact]
        public void Compare_IsDeterministic_ForTheSameInputs()
        {
            var previousOccurrences = MakeOccurrences("p", 3);
            var currentOccurrences = MakeOccurrences("c", 3);
            var matcher = new ConfigurableClashMatcher();
            matcher.RespondWith(previousOccurrences[0], currentOccurrences[0], MakeAssessment(previousOccurrences[0], currentOccurrences[0], ClashMatchConfidence.Medium));
            matcher.RespondWith(previousOccurrences[0], currentOccurrences[1], MakeAssessment(previousOccurrences[0], currentOccurrences[1], ClashMatchConfidence.High));
            matcher.RespondWith(previousOccurrences[1], currentOccurrences[1], MakeAssessment(previousOccurrences[1], currentOccurrences[1], ClashMatchConfidence.High));
            matcher.RespondWith(previousOccurrences[2], currentOccurrences[2], MakeAssessment(previousOccurrences[2], currentOccurrences[2], ClashMatchConfidence.Low));

            var comparer = new DeterministicClashRunComparer(matcher);
            var previousRun = MakeRun("run-1", previousOccurrences);
            var currentRun = MakeRun("run-2", currentOccurrences);

            var first = comparer.Compare(previousRun, currentRun);
            var second = comparer.Compare(previousRun, currentRun);

            AssertSameCandidateIndexes(first.Candidates, second.Candidates);
            AssertSameCandidateIndexes(first.SelectedMatches, second.SelectedMatches);
            AssertSameCandidateIndexes(first.AlternativeCandidates, second.AlternativeCandidates);
            Assert.Equal(first.UnmatchedPrevious, second.UnmatchedPrevious);
            Assert.Equal(first.UnmatchedCurrent, second.UnmatchedCurrent);
        }

        private static void AssertSameCandidateIndexes(
            IReadOnlyList<ClashRunMatchCandidate> first, IReadOnlyList<ClashRunMatchCandidate> second)
        {
            Assert.Equal(first.Count, second.Count);
            for (int i = 0; i < first.Count; i++)
            {
                Assert.Equal(first[i].PreviousIndex, second[i].PreviousIndex);
                Assert.Equal(first[i].CurrentIndex, second[i].CurrentIndex);
                Assert.Equal(first[i].Assessment.Confidence, second[i].Assessment.Confidence);
            }
        }
    }
}
