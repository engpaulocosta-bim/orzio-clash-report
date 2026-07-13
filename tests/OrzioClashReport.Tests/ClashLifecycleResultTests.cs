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
    /// Tests for <see cref="ClashLifecycleEntry"/> and <see cref="ClashLifecycleResult"/> invariants, invoking
    /// their internal constructors through reflection (no InternalsVisibleTo, no visibility increase).
    /// </summary>
    public class ClashLifecycleResultTests
    {
        private static readonly ConstructorInfo EntryConstructor =
            typeof(ClashLifecycleEntry).GetConstructor(
                BindingFlags.Instance | BindingFlags.NonPublic,
                binder: null,
                types: new[]
                {
                    typeof(ClashLifecycleStatus),
                    typeof(int?),
                    typeof(int?),
                    typeof(ClashOccurrence),
                    typeof(ClashOccurrence),
                    typeof(ClashRunMatchCandidate),
                    typeof(IReadOnlyList<ClashLifecycleEvidence>),
                },
                modifiers: null)
            ?? throw new InvalidOperationException("Could not find ClashLifecycleEntry internal constructor.");

        private static readonly ConstructorInfo ResultConstructor =
            typeof(ClashLifecycleResult).GetConstructor(
                BindingFlags.Instance | BindingFlags.NonPublic,
                binder: null,
                types: new[] { typeof(ClashRunMatchResult), typeof(IReadOnlyList<ClashLifecycleEntry>) },
                modifiers: null)
            ?? throw new InvalidOperationException("Could not find ClashLifecycleResult internal constructor.");

        private static ClashLifecycleEntry CreateEntryViaReflection(
            ClashLifecycleStatus status,
            int? previousIndex,
            int? currentIndex,
            ClashOccurrence? previousOccurrence,
            ClashOccurrence? currentOccurrence,
            ClashRunMatchCandidate? selectedMatch,
            IReadOnlyList<ClashLifecycleEvidence> evidence)
        {
            try
            {
                return (ClashLifecycleEntry)EntryConstructor.Invoke(new object?[]
                {
                    status, previousIndex, currentIndex, previousOccurrence, currentOccurrence, selectedMatch, evidence,
                });
            }
            catch (TargetInvocationException ex) when (ex.InnerException != null)
            {
                ExceptionDispatchInfo.Capture(ex.InnerException).Throw();
                throw;
            }
        }

        private static ClashLifecycleResult CreateResultViaReflection(
            ClashRunMatchResult matchResult, IReadOnlyList<ClashLifecycleEntry> entries)
        {
            try
            {
                return (ClashLifecycleResult)ResultConstructor.Invoke(new object[] { matchResult, entries });
            }
            catch (TargetInvocationException ex) when (ex.InnerException != null)
            {
                ExceptionDispatchInfo.Capture(ex.InnerException).Throw();
                throw;
            }
        }

        private static ModelRevision MakeRevision(string company, string discipline, string modelName, string revision) =>
            new ModelRevision(new ModelIdentity(company, discipline, modelName), revision, $"{modelName}_{revision}.nwc", null, null, null);

        private static ClashObject MakeObject(string elementId) => new ClashObject(elementId, null, null, null, null, null);

        private static readonly ModelRevision Sigma = MakeRevision("Sigma", "Structure", "Main", "R04");

        private static ClashOccurrence MakeOccurrence(string clashTestName, string elementIdA, string elementIdB) =>
            new ClashOccurrence(
                clashTestName,
                new ClashResult(clashTestName, ClashStatus.New, null, null, null, MakeObject(elementIdA), MakeObject(elementIdB), "g1"),
                Sigma, Sigma);

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

        private static IReadOnlyList<ClashLifecycleEvidence> Supporting(int count = 1)
        {
            var list = new List<ClashLifecycleEvidence>();
            for (int i = 0; i < count; i++)
            {
                list.Add(new ClashLifecycleEvidence(ClashLifecycleEvidenceKind.SelectedMatch, ClashLifecycleEvidenceVerdict.SupportsClassification, $"support {i}"));
            }

            return list;
        }

        private static ClashLifecycleEvidence Blocking(string description = "blocker") =>
            new ClashLifecycleEvidence(ClashLifecycleEvidenceKind.CandidateAmbiguity, ClashLifecycleEvidenceVerdict.BlocksClassification, description);

        // A previous/current pair with one selected match (High, unambiguous), one unmatched previous whose
        // model+test are present in the current run (verifiable Resolved), and one unmatched current whose
        // model+test are present in the previous run (verifiable New).
        private sealed class Scenario
        {
            public required CoordinationRun PreviousRun { get; init; }
            public required CoordinationRun CurrentRun { get; init; }
            public required ClashOccurrence[] PreviousOccurrences { get; init; }
            public required ClashOccurrence[] CurrentOccurrences { get; init; }
            public required ClashRunMatchResult MatchResult { get; init; }
            public required ClashLifecycleResult BaseResult { get; init; }
        }

        private static Scenario CreateScenario()
        {
            var previousOccurrences = new[]
            {
                MakeOccurrence("Test 1", "p0a", "p0b"),
                MakeOccurrence("Test 1", "p1a", "p1b"),
            };
            var currentOccurrences = new[]
            {
                MakeOccurrence("Test 1", "c0a", "c0b"),
                MakeOccurrence("Test 1", "c1a", "c1b"),
            };

            var previousRun = MakeRun("run-1", previousOccurrences);
            var currentRun = MakeRun("run-2", currentOccurrences);

            var matcher = new ConfigurableClashMatcher();
            matcher.RespondWith(previousOccurrences[0], currentOccurrences[0], MakeAssessment(previousOccurrences[0], currentOccurrences[0], ClashMatchConfidence.High));

            var matchResult = new DeterministicClashRunComparer(matcher).Compare(previousRun, currentRun);
            var baseResult = new ConservativeClashLifecycleClassifier().Classify(matchResult);

            return new Scenario
            {
                PreviousRun = previousRun,
                CurrentRun = currentRun,
                PreviousOccurrences = previousOccurrences,
                CurrentOccurrences = currentOccurrences,
                MatchResult = matchResult,
                BaseResult = baseResult,
            };
        }

        // ===================== ClashLifecycleEntry invariants =====================

        [Fact]
        public void Entry_RejectsNegativePreviousIndex()
        {
            var scenario = CreateScenario();

            Assert.Throws<ArgumentOutOfRangeException>(() => CreateEntryViaReflection(
                ClashLifecycleStatus.Resolved, -1, null, scenario.PreviousOccurrences[1], null, null, Supporting()));
        }

        [Fact]
        public void Entry_RejectsNegativeCurrentIndex()
        {
            var scenario = CreateScenario();

            Assert.Throws<ArgumentOutOfRangeException>(() => CreateEntryViaReflection(
                ClashLifecycleStatus.New, null, -1, null, scenario.CurrentOccurrences[1], null, Supporting()));
        }

        [Fact]
        public void Entry_RejectsIndexWithoutOccurrence()
        {
            Assert.Throws<ArgumentException>(() => CreateEntryViaReflection(
                ClashLifecycleStatus.Unverifiable, 0, null, null, null, null, new[] { Blocking() }));
        }

        [Fact]
        public void Entry_RejectsOccurrenceWithoutIndex()
        {
            var scenario = CreateScenario();

            Assert.Throws<ArgumentException>(() => CreateEntryViaReflection(
                ClashLifecycleStatus.Unverifiable, null, null, scenario.PreviousOccurrences[1], null, null, new[] { Blocking() }));
        }

        [Fact]
        public void Entry_RejectsNeitherSidePresent()
        {
            Assert.Throws<ArgumentException>(() => CreateEntryViaReflection(
                ClashLifecycleStatus.Unverifiable, null, null, null, null, null, new[] { Blocking() }));
        }

        [Fact]
        public void Entry_RejectsUnmatchedWithBothSidesPresent()
        {
            var scenario = CreateScenario();

            Assert.Throws<ArgumentException>(() => CreateEntryViaReflection(
                ClashLifecycleStatus.Unverifiable, 1, 1, scenario.PreviousOccurrences[1], scenario.CurrentOccurrences[1], null, new[] { Blocking() }));
        }

        [Fact]
        public void Entry_RejectsSelectedWithOnlyOneSidePresent()
        {
            var scenario = CreateScenario();
            var selected = scenario.MatchResult.SelectedMatches[0];

            Assert.Throws<ArgumentException>(() => CreateEntryViaReflection(
                ClashLifecycleStatus.StillOpen, selected.PreviousIndex, null, selected.Assessment.PreviousOccurrence, null, selected, Supporting()));
        }

        [Fact]
        public void Entry_RejectsSelectedWithDivergentIndexes()
        {
            var scenario = CreateScenario();
            var selected = scenario.MatchResult.SelectedMatches[0];

            Assert.Throws<ArgumentException>(() => CreateEntryViaReflection(
                ClashLifecycleStatus.StillOpen,
                selected.PreviousIndex,
                selected.CurrentIndex + 1,
                selected.Assessment.PreviousOccurrence,
                scenario.CurrentOccurrences[1],
                selected,
                Supporting()));
        }

        [Fact]
        public void Entry_RejectsSelectedWithDivergentOccurrenceReferences()
        {
            var scenario = CreateScenario();
            var selected = scenario.MatchResult.SelectedMatches[0];
            var wrongOccurrence = MakeOccurrence("Other", "x", "y");

            Assert.Throws<ArgumentException>(() => CreateEntryViaReflection(
                ClashLifecycleStatus.StillOpen,
                selected.PreviousIndex,
                selected.CurrentIndex,
                wrongOccurrence,
                selected.Assessment.CurrentOccurrence,
                selected,
                Supporting()));
        }

        [Fact]
        public void Entry_RejectsNullEvidence()
        {
            var scenario = CreateScenario();
            var selected = scenario.MatchResult.SelectedMatches[0];

            Assert.Throws<ArgumentNullException>(() => CreateEntryViaReflection(
                ClashLifecycleStatus.StillOpen, selected.PreviousIndex, selected.CurrentIndex,
                selected.Assessment.PreviousOccurrence, selected.Assessment.CurrentOccurrence, selected, null!));
        }

        [Fact]
        public void Entry_RejectsEmptyEvidence()
        {
            var scenario = CreateScenario();
            var selected = scenario.MatchResult.SelectedMatches[0];

            Assert.Throws<ArgumentException>(() => CreateEntryViaReflection(
                ClashLifecycleStatus.StillOpen, selected.PreviousIndex, selected.CurrentIndex,
                selected.Assessment.PreviousOccurrence, selected.Assessment.CurrentOccurrence, selected, Array.Empty<ClashLifecycleEvidence>()));
        }

        [Fact]
        public void Entry_RejectsNullItemInEvidence()
        {
            var scenario = CreateScenario();
            var selected = scenario.MatchResult.SelectedMatches[0];
            var evidenceWithNull = new ClashLifecycleEvidence?[] { new ClashLifecycleEvidence(ClashLifecycleEvidenceKind.SelectedMatch, ClashLifecycleEvidenceVerdict.SupportsClassification, "ok"), null };

            Assert.Throws<ArgumentException>(() => CreateEntryViaReflection(
                ClashLifecycleStatus.StillOpen, selected.PreviousIndex, selected.CurrentIndex,
                selected.Assessment.PreviousOccurrence, selected.Assessment.CurrentOccurrence, selected, evidenceWithNull!));
        }

        [Fact]
        public void Entry_RejectsStillOpenWithBlocker()
        {
            var scenario = CreateScenario();
            var selected = scenario.MatchResult.SelectedMatches[0];

            Assert.Throws<ArgumentException>(() => CreateEntryViaReflection(
                ClashLifecycleStatus.StillOpen, selected.PreviousIndex, selected.CurrentIndex,
                selected.Assessment.PreviousOccurrence, selected.Assessment.CurrentOccurrence, selected,
                new[] { Supporting()[0], Blocking() }));
        }

        [Fact]
        public void Entry_RejectsStillOpenWithoutSelectedMatch()
        {
            var scenario = CreateScenario();

            Assert.Throws<ArgumentException>(() => CreateEntryViaReflection(
                ClashLifecycleStatus.StillOpen, 1, null, scenario.PreviousOccurrences[1], null, null, Supporting()));
        }

        [Fact]
        public void Entry_RejectsNewWithPreviousSideAlsoPresent()
        {
            var scenario = CreateScenario();

            Assert.Throws<ArgumentException>(() => CreateEntryViaReflection(
                ClashLifecycleStatus.New, 1, 1, scenario.PreviousOccurrences[1], scenario.CurrentOccurrences[1], null, Supporting()));
        }

        [Fact]
        public void Entry_RejectsNewWithSelectedMatchPresent()
        {
            var scenario = CreateScenario();
            var selected = scenario.MatchResult.SelectedMatches[0];

            Assert.Throws<ArgumentException>(() => CreateEntryViaReflection(
                ClashLifecycleStatus.New, selected.PreviousIndex, selected.CurrentIndex,
                selected.Assessment.PreviousOccurrence, selected.Assessment.CurrentOccurrence, selected, Supporting()));
        }

        [Fact]
        public void Entry_RejectsNewWithBlocker()
        {
            var scenario = CreateScenario();

            Assert.Throws<ArgumentException>(() => CreateEntryViaReflection(
                ClashLifecycleStatus.New, null, 1, null, scenario.CurrentOccurrences[1], null, new[] { Supporting()[0], Blocking() }));
        }

        [Fact]
        public void Entry_RejectsResolvedWithCurrentSideAlsoPresent()
        {
            var scenario = CreateScenario();

            Assert.Throws<ArgumentException>(() => CreateEntryViaReflection(
                ClashLifecycleStatus.Resolved, 1, 1, scenario.PreviousOccurrences[1], scenario.CurrentOccurrences[1], null, Supporting()));
        }

        [Fact]
        public void Entry_RejectsResolvedWithSelectedMatchPresent()
        {
            var scenario = CreateScenario();
            var selected = scenario.MatchResult.SelectedMatches[0];

            Assert.Throws<ArgumentException>(() => CreateEntryViaReflection(
                ClashLifecycleStatus.Resolved, selected.PreviousIndex, selected.CurrentIndex,
                selected.Assessment.PreviousOccurrence, selected.Assessment.CurrentOccurrence, selected, Supporting()));
        }

        [Fact]
        public void Entry_RejectsResolvedWithBlocker()
        {
            var scenario = CreateScenario();

            Assert.Throws<ArgumentException>(() => CreateEntryViaReflection(
                ClashLifecycleStatus.Resolved, 1, null, scenario.PreviousOccurrences[1], null, null, new[] { Supporting()[0], Blocking() }));
        }

        [Fact]
        public void Entry_RejectsUnverifiableWithoutBlocker()
        {
            var scenario = CreateScenario();
            var selected = scenario.MatchResult.SelectedMatches[0];

            Assert.Throws<ArgumentException>(() => CreateEntryViaReflection(
                ClashLifecycleStatus.Unverifiable, selected.PreviousIndex, selected.CurrentIndex,
                selected.Assessment.PreviousOccurrence, selected.Assessment.CurrentOccurrence, selected, Supporting()));
        }

        [Fact]
        public void Entry_AcceptsValidStillOpenShape()
        {
            var scenario = CreateScenario();
            var selected = scenario.MatchResult.SelectedMatches[0];

            var entry = CreateEntryViaReflection(
                ClashLifecycleStatus.StillOpen, selected.PreviousIndex, selected.CurrentIndex,
                selected.Assessment.PreviousOccurrence, selected.Assessment.CurrentOccurrence, selected, Supporting());

            Assert.Equal(ClashLifecycleStatus.StillOpen, entry.Status);
            Assert.Same(selected, entry.SelectedMatch);
        }

        [Fact]
        public void Entry_AcceptsValidNewShape()
        {
            var scenario = CreateScenario();

            var entry = CreateEntryViaReflection(
                ClashLifecycleStatus.New, null, 1, null, scenario.CurrentOccurrences[1], null, Supporting());

            Assert.Equal(ClashLifecycleStatus.New, entry.Status);
            Assert.Null(entry.SelectedMatch);
            Assert.Null(entry.PreviousIndex);
        }

        [Fact]
        public void Entry_AcceptsValidResolvedShape()
        {
            var scenario = CreateScenario();

            var entry = CreateEntryViaReflection(
                ClashLifecycleStatus.Resolved, 1, null, scenario.PreviousOccurrences[1], null, null, Supporting());

            Assert.Equal(ClashLifecycleStatus.Resolved, entry.Status);
            Assert.Null(entry.SelectedMatch);
            Assert.Null(entry.CurrentIndex);
        }

        [Fact]
        public void Entry_AcceptsValidUnverifiableSelectedShape()
        {
            var scenario = CreateScenario();
            var selected = scenario.MatchResult.SelectedMatches[0];

            var entry = CreateEntryViaReflection(
                ClashLifecycleStatus.Unverifiable, selected.PreviousIndex, selected.CurrentIndex,
                selected.Assessment.PreviousOccurrence, selected.Assessment.CurrentOccurrence, selected, new[] { Blocking() });

            Assert.Equal(ClashLifecycleStatus.Unverifiable, entry.Status);
        }

        [Fact]
        public void Entry_AcceptsValidUnverifiableUnmatchedShape()
        {
            var scenario = CreateScenario();

            var entry = CreateEntryViaReflection(
                ClashLifecycleStatus.Unverifiable, 1, null, scenario.PreviousOccurrences[1], null, null, new[] { Blocking() });

            Assert.Equal(ClashLifecycleStatus.Unverifiable, entry.Status);
        }

        [Fact]
        public void Entry_DefensivelyCopiesEvidenceList()
        {
            var scenario = CreateScenario();
            var source = new List<ClashLifecycleEvidence> { new ClashLifecycleEvidence(ClashLifecycleEvidenceKind.SelectedMatch, ClashLifecycleEvidenceVerdict.SupportsClassification, "ok") };

            var entry = CreateEntryViaReflection(
                ClashLifecycleStatus.Resolved, 1, null, scenario.PreviousOccurrences[1], null, null, source);
            source.Clear();

            Assert.Single(entry.Evidence);
        }

        // ===================== ClashLifecycleResult invariants =====================

        [Fact]
        public void Result_RejectsNullMatchResult()
        {
            Assert.Throws<ArgumentNullException>(() => CreateResultViaReflection(null!, Array.Empty<ClashLifecycleEntry>()));
        }

        [Fact]
        public void Result_RejectsNullEntries()
        {
            var scenario = CreateScenario();

            Assert.Throws<ArgumentNullException>(() => CreateResultViaReflection(scenario.MatchResult, null!));
        }

        [Fact]
        public void Result_RejectsNullItemInEntries()
        {
            var scenario = CreateScenario();
            var entriesWithNull = new ClashLifecycleEntry?[] { scenario.BaseResult.Entries[0], null, scenario.BaseResult.Entries[2] };

            Assert.Throws<ArgumentException>(() => CreateResultViaReflection(scenario.MatchResult, entriesWithNull!));
        }

        [Fact]
        public void Result_RejectsWrongEntryCount()
        {
            var scenario = CreateScenario();
            var tooFew = new[] { scenario.BaseResult.Entries[0], scenario.BaseResult.Entries[1] };

            Assert.Throws<ArgumentException>(() => CreateResultViaReflection(scenario.MatchResult, tooFew));
        }

        [Fact]
        public void Result_RejectsOmittedSelectedEntry()
        {
            var scenario = CreateScenario();
            // Selected-match position replaced by a duplicate of the unmatched-previous entry.
            var invalidEntries = new[] { scenario.BaseResult.Entries[1], scenario.BaseResult.Entries[1], scenario.BaseResult.Entries[2] };

            Assert.Throws<ArgumentException>(() => CreateResultViaReflection(scenario.MatchResult, invalidEntries));
        }

        [Fact]
        public void Result_RejectsDuplicatedSelectedEntry()
        {
            var scenario = CreateScenario();
            // Unmatched-previous position replaced by a duplicate of the selected entry -- count stays correct,
            // but the position that must be an unmatched-previous entry now carries a selected one.
            var invalidEntries = new[] { scenario.BaseResult.Entries[0], scenario.BaseResult.Entries[0], scenario.BaseResult.Entries[2] };

            Assert.Throws<ArgumentException>(() => CreateResultViaReflection(scenario.MatchResult, invalidEntries));
        }

        [Fact]
        public void Result_RejectsOmittedUnmatchedPreviousEntry()
        {
            var scenario = CreateScenario();
            // Unmatched-previous position replaced by a duplicate of the unmatched-current entry.
            var invalidEntries = new[] { scenario.BaseResult.Entries[0], scenario.BaseResult.Entries[2], scenario.BaseResult.Entries[2] };

            Assert.Throws<ArgumentException>(() => CreateResultViaReflection(scenario.MatchResult, invalidEntries));
        }

        [Fact]
        public void Result_RejectsOmittedUnmatchedCurrentEntry()
        {
            var scenario = CreateScenario();
            // Unmatched-current position replaced by a duplicate of the unmatched-previous entry.
            var invalidEntries = new[] { scenario.BaseResult.Entries[0], scenario.BaseResult.Entries[1], scenario.BaseResult.Entries[1] };

            Assert.Throws<ArgumentException>(() => CreateResultViaReflection(scenario.MatchResult, invalidEntries));
        }

        [Fact]
        public void Result_RejectsWrongOrder()
        {
            var scenario = CreateScenario();
            // Unmatched previous/current sections swapped.
            var invalidEntries = new[] { scenario.BaseResult.Entries[0], scenario.BaseResult.Entries[2], scenario.BaseResult.Entries[1] };

            Assert.Throws<ArgumentException>(() => CreateResultViaReflection(scenario.MatchResult, invalidEntries));
        }

        [Fact]
        public void Result_RejectsEntryWithWrongOccurrenceReference()
        {
            var scenario = CreateScenario();
            var wrongOccurrence = MakeOccurrence("Other", "x", "y");
            var mismatchedEntry = CreateEntryViaReflection(
                ClashLifecycleStatus.Resolved, 1, null, wrongOccurrence, null, null, Supporting());
            var invalidEntries = new[] { scenario.BaseResult.Entries[0], mismatchedEntry, scenario.BaseResult.Entries[2] };

            Assert.Throws<ArgumentException>(() => CreateResultViaReflection(scenario.MatchResult, invalidEntries));
        }

        [Fact]
        public void Result_AcceptsValidPartition()
        {
            var scenario = CreateScenario();

            var result = CreateResultViaReflection(scenario.MatchResult, scenario.BaseResult.Entries);

            Assert.Equal(3, result.Entries.Count);
        }

        [Fact]
        public void Result_EntriesListCannotBeMutated()
        {
            var scenario = CreateScenario();

            var mutableView = Assert.IsAssignableFrom<IList<ClashLifecycleEntry>>(scenario.BaseResult.Entries);
            Assert.Throws<NotSupportedException>(() => mutableView.Add(scenario.BaseResult.Entries[0]));
        }

        [Fact]
        public void Result_ToString_IncludesRunIdsTotalAndStatusCounts()
        {
            var scenario = CreateScenario();

            string text = scenario.BaseResult.ToString();

            Assert.Contains("run-1", text);
            Assert.Contains("run-2", text);
            Assert.Contains("3", text);
            Assert.Contains("still open", text, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("new", text, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("resolved", text, StringComparison.OrdinalIgnoreCase);
        }
    }
}
