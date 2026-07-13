using System;
using System.Collections.Generic;
using System.Linq;
using OrzioClashReport.Core.Abstractions;
using OrzioClashReport.Core.Lifecycle;
using OrzioClashReport.Core.Matching;
using OrzioClashReport.Core.Model;

namespace OrzioClashReport.Tests
{
    /// <summary>
    /// Tests for ConservativeClashLifecycleClassifier: selected-match classification (StillOpen/Unverifiable),
    /// unmatched-previous classification (Resolved/Unverifiable), unmatched-current classification
    /// (New/Unverifiable), empty runs, duplicate slot handling, determinism, explicit executed-clash-test
    /// coverage (including zero-occurrence declarations), and end-to-end integration with the real
    /// ConservativeClashMatcher and DeterministicClashRunComparer.
    /// </summary>
    public class ConservativeClashLifecycleClassifierTests
    {
        private static ModelRevision MakeRevision(
            string company, string discipline, string modelName, string revision,
            string? sourceFileName = null, string? sourceFilePath = null, string? contentHash = null, DateTimeOffset? publishedAt = null) =>
            new ModelRevision(
                new ModelIdentity(company, discipline, modelName), revision, sourceFileName ?? $"{modelName}_{revision}.nwc",
                sourceFilePath, contentHash, publishedAt);

        private static ClashObject MakeObject(string elementId) => new ClashObject(elementId, null, null, null, null, null);

        private static readonly ModelRevision Sigma = MakeRevision("Sigma", "Structure", "Main", "R04");
        private static readonly ModelRevision Alfa = MakeRevision("Alfa", "HVAC", "Main", "R07");
        private static readonly ModelRevision Beta = MakeRevision("Beta", "Architecture", "Main", "R10");

        private static ClashOccurrence MakeOccurrence(
            string clashTestName, ModelRevision modelA, ModelRevision modelB,
            string elementIdA = "a", string elementIdB = "b", string? guid = "g1", ClashStatus rawStatus = ClashStatus.New) =>
            new ClashOccurrence(
                clashTestName,
                new ClashResult(clashTestName, rawStatus, null, null, null, MakeObject(elementIdA), MakeObject(elementIdB), guid),
                modelA, modelB);

        private static ExecutedClashTest TestFor(string name, ModelRevision modelA, ModelRevision modelB) =>
            new ExecutedClashTest(name, modelA.Identity, modelB.Identity);

        private static readonly ExecutedClashTest StandardTest = TestFor("Test 1", Sigma, Alfa);
        private static readonly ExecutedClashTest SelfClashTest = TestFor("Test 1", Sigma, Sigma);

        private static RunManifest MakeManifest(string runId, ModelRevision[] models, ExecutedClashTest[] executedClashTests) =>
            new RunManifest(runId, new DateTimeOffset(2026, 7, 10, 9, 0, 0, TimeSpan.Zero), models, executedClashTests);

        private static CoordinationRun MakeRun(
            string runId, ModelRevision[] models, ExecutedClashTest[] executedClashTests, params ClashOccurrence[] occurrences) =>
            new CoordinationRun(MakeManifest(runId, models, executedClashTests), occurrences);

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

        private static readonly ConservativeClashLifecycleClassifier Classifier = new ConservativeClashLifecycleClassifier();

        private static ClashLifecycleResult ClassifyWith(
            ConfigurableClashMatcher matcher, CoordinationRun previousRun, CoordinationRun currentRun)
        {
            var matchResult = new DeterministicClashRunComparer(matcher).Compare(previousRun, currentRun);
            return Classifier.Classify(matchResult);
        }

        // ===================== 16.4 Selected lifecycle =====================

        [Fact]
        public void Selected_HighWithoutAlternatives_IsStillOpen()
        {
            var p = MakeOccurrence("Test 1", Sigma, Alfa);
            var c = MakeOccurrence("Test 1", Sigma, Alfa);
            var matcher = new ConfigurableClashMatcher();
            matcher.RespondWith(p, c, MakeAssessment(p, c, ClashMatchConfidence.High));

            var result = ClassifyWith(
                matcher,
                MakeRun("run-1", new[] { Sigma, Alfa }, new[] { StandardTest }, p),
                MakeRun("run-2", new[] { Sigma, Alfa }, new[] { StandardTest }, c));

            var entry = Assert.Single(result.Entries);
            Assert.Equal(ClashLifecycleStatus.StillOpen, entry.Status);
        }

        [Fact]
        public void Selected_MediumWithoutAlternatives_IsStillOpen()
        {
            var p = MakeOccurrence("Test 1", Sigma, Alfa);
            var c = MakeOccurrence("Test 1", Sigma, Alfa);
            var matcher = new ConfigurableClashMatcher();
            matcher.RespondWith(p, c, MakeAssessment(p, c, ClashMatchConfidence.Medium));

            var result = ClassifyWith(
                matcher,
                MakeRun("run-1", new[] { Sigma, Alfa }, new[] { StandardTest }, p),
                MakeRun("run-2", new[] { Sigma, Alfa }, new[] { StandardTest }, c));

            var entry = Assert.Single(result.Entries);
            Assert.Equal(ClashLifecycleStatus.StillOpen, entry.Status);
        }

        [Fact]
        public void Selected_LowWithoutAlternatives_IsUnverifiable()
        {
            var p = MakeOccurrence("Test 1", Sigma, Alfa);
            var c = MakeOccurrence("Test 1", Sigma, Alfa);
            var matcher = new ConfigurableClashMatcher();
            matcher.RespondWith(p, c, MakeAssessment(p, c, ClashMatchConfidence.Low));

            var result = ClassifyWith(
                matcher,
                MakeRun("run-1", new[] { Sigma, Alfa }, new[] { StandardTest }, p),
                MakeRun("run-2", new[] { Sigma, Alfa }, new[] { StandardTest }, c));

            var entry = Assert.Single(result.Entries);
            Assert.Equal(ClashLifecycleStatus.Unverifiable, entry.Status);
        }

        [Fact]
        public void Selected_AlternativeSharingPreviousIndex_IsUnverifiable()
        {
            // Only p0 exists on the previous side, so any losing alternative for a different current
            // occurrence necessarily shares the selected match's PreviousIndex.
            var p0 = MakeOccurrence("Test 1", Sigma, Alfa, "p0a", "p0b");
            var c0 = MakeOccurrence("Test 1", Sigma, Alfa, "c0a", "c0b");
            var c1 = MakeOccurrence("Test 1", Sigma, Alfa, "c1a", "c1b");
            var matcher = new ConfigurableClashMatcher();
            matcher.RespondWith(p0, c0, MakeAssessment(p0, c0, ClashMatchConfidence.High));
            matcher.RespondWith(p0, c1, MakeAssessment(p0, c1, ClashMatchConfidence.Medium));

            var result = ClassifyWith(
                matcher,
                MakeRun("run-1", new[] { Sigma, Alfa }, new[] { StandardTest }, p0),
                MakeRun("run-2", new[] { Sigma, Alfa }, new[] { StandardTest }, c0, c1));

            var selectedEntry = result.Entries.Single(e => e.SelectedMatch != null);
            Assert.Equal(0, selectedEntry.CurrentIndex);
            Assert.Equal(ClashLifecycleStatus.Unverifiable, selectedEntry.Status);
        }

        [Fact]
        public void Selected_AlternativeSharingCurrentIndex_IsUnverifiable()
        {
            // Only c0 exists on the current side, so any losing alternative from a different previous
            // occurrence necessarily shares the selected match's CurrentIndex.
            var p0 = MakeOccurrence("Test 1", Sigma, Alfa, "p0a", "p0b");
            var p1 = MakeOccurrence("Test 1", Sigma, Alfa, "p1a", "p1b");
            var c0 = MakeOccurrence("Test 1", Sigma, Alfa, "c0a", "c0b");
            var matcher = new ConfigurableClashMatcher();
            matcher.RespondWith(p0, c0, MakeAssessment(p0, c0, ClashMatchConfidence.High));
            matcher.RespondWith(p1, c0, MakeAssessment(p1, c0, ClashMatchConfidence.Medium));

            var result = ClassifyWith(
                matcher,
                MakeRun("run-1", new[] { Sigma, Alfa }, new[] { StandardTest }, p0, p1),
                MakeRun("run-2", new[] { Sigma, Alfa }, new[] { StandardTest }, c0));

            var selectedEntry = result.Entries.Single(e => e.SelectedMatch != null);
            Assert.Equal(0, selectedEntry.PreviousIndex);
            Assert.Equal(ClashLifecycleStatus.Unverifiable, selectedEntry.Status);
        }

        [Fact]
        public void Selected_UnrelatedAlternative_DoesNotBlockThatSelected()
        {
            var p0 = MakeOccurrence("Test 1", Sigma, Alfa, "p0a", "p0b");
            var p1 = MakeOccurrence("Test 1", Sigma, Alfa, "p1a", "p1b");
            var p2 = MakeOccurrence("Test 1", Sigma, Alfa, "p2a", "p2b");
            var c0 = MakeOccurrence("Test 1", Sigma, Alfa, "c0a", "c0b");
            var c1 = MakeOccurrence("Test 1", Sigma, Alfa, "c1a", "c1b");
            var matcher = new ConfigurableClashMatcher();
            matcher.RespondWith(p0, c0, MakeAssessment(p0, c0, ClashMatchConfidence.High));
            matcher.RespondWith(p1, c1, MakeAssessment(p1, c1, ClashMatchConfidence.High));
            matcher.RespondWith(p2, c1, MakeAssessment(p2, c1, ClashMatchConfidence.High));

            var result = ClassifyWith(
                matcher,
                MakeRun("run-1", new[] { Sigma, Alfa }, new[] { StandardTest }, p0, p1, p2),
                MakeRun("run-2", new[] { Sigma, Alfa }, new[] { StandardTest }, c0, c1));

            // p0-c0 is selected and shares no index with the p2-c1 alternative (which lost to p1-c1).
            var targetEntry = result.Entries.Single(e => e.SelectedMatch != null && e.PreviousIndex == 0);
            Assert.Equal(ClashLifecycleStatus.StillOpen, targetEntry.Status);
        }

        [Fact]
        public void Selected_EvidenceOrder_IsSelectedMatchThenConfidenceThenAmbiguity()
        {
            var p = MakeOccurrence("Test 1", Sigma, Alfa);
            var c = MakeOccurrence("Test 1", Sigma, Alfa);
            var matcher = new ConfigurableClashMatcher();
            matcher.RespondWith(p, c, MakeAssessment(p, c, ClashMatchConfidence.High));

            var result = ClassifyWith(
                matcher,
                MakeRun("run-1", new[] { Sigma, Alfa }, new[] { StandardTest }, p),
                MakeRun("run-2", new[] { Sigma, Alfa }, new[] { StandardTest }, c));

            var entry = Assert.Single(result.Entries);
            Assert.Equal(3, entry.Evidence.Count);
            Assert.Equal(ClashLifecycleEvidenceKind.SelectedMatch, entry.Evidence[0].Kind);
            Assert.Equal(ClashLifecycleEvidenceKind.MatchConfidence, entry.Evidence[1].Kind);
            Assert.Equal(ClashLifecycleEvidenceKind.CandidateAmbiguity, entry.Evidence[2].Kind);
        }

        [Fact]
        public void Selected_ConfidenceEvidence_MentionsConfidenceLevel()
        {
            var p = MakeOccurrence("Test 1", Sigma, Alfa);
            var c = MakeOccurrence("Test 1", Sigma, Alfa);
            var matcher = new ConfigurableClashMatcher();
            matcher.RespondWith(p, c, MakeAssessment(p, c, ClashMatchConfidence.High));

            var result = ClassifyWith(
                matcher,
                MakeRun("run-1", new[] { Sigma, Alfa }, new[] { StandardTest }, p),
                MakeRun("run-2", new[] { Sigma, Alfa }, new[] { StandardTest }, c));

            Assert.Contains("High", result.Entries[0].Evidence[1].Description);
        }

        [Fact]
        public void Selected_AmbiguityEvidence_MentionsAlternativeCount()
        {
            var p0 = MakeOccurrence("Test 1", Sigma, Alfa, "p0a", "p0b");
            var p1 = MakeOccurrence("Test 1", Sigma, Alfa, "p1a", "p1b");
            var c0 = MakeOccurrence("Test 1", Sigma, Alfa, "c0a", "c0b");
            var matcher = new ConfigurableClashMatcher();
            matcher.RespondWith(p0, c0, MakeAssessment(p0, c0, ClashMatchConfidence.High));
            matcher.RespondWith(p1, c0, MakeAssessment(p1, c0, ClashMatchConfidence.Medium));

            var result = ClassifyWith(
                matcher,
                MakeRun("run-1", new[] { Sigma, Alfa }, new[] { StandardTest }, p0, p1),
                MakeRun("run-2", new[] { Sigma, Alfa }, new[] { StandardTest }, c0));

            var selectedEntry = result.Entries.Single(e => e.SelectedMatch != null);
            Assert.Contains("1", selectedEntry.Evidence[2].Description);
        }

        [Fact]
        public void Selected_PreservesCandidateAndOccurrenceReferences()
        {
            var p = MakeOccurrence("Test 1", Sigma, Alfa);
            var c = MakeOccurrence("Test 1", Sigma, Alfa);
            var matcher = new ConfigurableClashMatcher();
            matcher.RespondWith(p, c, MakeAssessment(p, c, ClashMatchConfidence.High));

            var matchResult = new DeterministicClashRunComparer(matcher).Compare(
                MakeRun("run-1", new[] { Sigma, Alfa }, new[] { StandardTest }, p),
                MakeRun("run-2", new[] { Sigma, Alfa }, new[] { StandardTest }, c));
            var result = Classifier.Classify(matchResult);

            var entry = Assert.Single(result.Entries);
            Assert.Same(matchResult.SelectedMatches[0], entry.SelectedMatch);
            Assert.Same(p, entry.PreviousOccurrence);
            Assert.Same(c, entry.CurrentOccurrence);
        }

        // ===================== 16.5 Resolved =====================

        [Fact]
        public void UnmatchedPrevious_NoAlternatives_ModelsAndTestPresent_IsResolved()
        {
            var previousOccurrence = MakeOccurrence("Test 1", Sigma, Alfa);
            var currentTestPresence = MakeOccurrence("Test 1", Sigma, Alfa, "x", "y");
            var matcher = new ConfigurableClashMatcher();

            var result = ClassifyWith(
                matcher,
                MakeRun("run-1", new[] { Sigma, Alfa }, new[] { StandardTest }, previousOccurrence),
                MakeRun("run-2", new[] { Sigma, Alfa }, new[] { StandardTest }, currentTestPresence));

            var entry = result.Entries.Single(e => e.PreviousOccurrence == previousOccurrence);
            Assert.Equal(ClashLifecycleStatus.Resolved, entry.Status);
        }

        [Fact]
        public void UnmatchedPrevious_ModelAAbsentFromCurrent_IsUnverifiable()
        {
            var previousOccurrence = MakeOccurrence("Test 1", Sigma, Alfa);
            var currentTestPresence = MakeOccurrence("Test 1", Alfa, Alfa, "x", "y");
            var matcher = new ConfigurableClashMatcher();

            var result = ClassifyWith(
                matcher,
                MakeRun("run-1", new[] { Sigma, Alfa }, new[] { StandardTest }, previousOccurrence),
                MakeRun("run-2", new[] { Alfa }, new[] { TestFor("Test 1", Alfa, Alfa) }, currentTestPresence));

            var entry = result.Entries.Single(e => e.PreviousOccurrence == previousOccurrence);
            Assert.Equal(ClashLifecycleStatus.Unverifiable, entry.Status);
        }

        [Fact]
        public void UnmatchedPrevious_ModelBAbsentFromCurrent_IsUnverifiable()
        {
            var previousOccurrence = MakeOccurrence("Test 1", Sigma, Alfa);
            var currentTestPresence = MakeOccurrence("Test 1", Sigma, Sigma, "x", "y");
            var matcher = new ConfigurableClashMatcher();

            var result = ClassifyWith(
                matcher,
                MakeRun("run-1", new[] { Sigma, Alfa }, new[] { StandardTest }, previousOccurrence),
                MakeRun("run-2", new[] { Sigma }, new[] { SelfClashTest }, currentTestPresence));

            var entry = result.Entries.Single(e => e.PreviousOccurrence == previousOccurrence);
            Assert.Equal(ClashLifecycleStatus.Unverifiable, entry.Status);
        }

        [Fact]
        public void UnmatchedPrevious_TestNotObserved_IsUnverifiable()
        {
            var previousOccurrence = MakeOccurrence("Test 1", Sigma, Alfa);
            var currentOtherTest = MakeOccurrence("Test 2", Sigma, Alfa, "x", "y");
            var matcher = new ConfigurableClashMatcher();

            var result = ClassifyWith(
                matcher,
                MakeRun("run-1", new[] { Sigma, Alfa }, new[] { StandardTest }, previousOccurrence),
                MakeRun("run-2", new[] { Sigma, Alfa }, new[] { TestFor("Test 2", Sigma, Alfa) }, currentOtherTest));

            var entry = result.Entries.Single(e => e.PreviousOccurrence == previousOccurrence);
            Assert.Equal(ClashLifecycleStatus.Unverifiable, entry.Status);
        }

        [Fact]
        public void UnmatchedPrevious_AlternativeReferencingItsPreviousIndex_IsUnverifiable()
        {
            // p0-c0 Medium and p1-c0 High compete; p1-c0 wins, p0-c0 becomes an alternative referencing p0's index.
            var p0 = MakeOccurrence("Test 1", Sigma, Alfa, "p0a", "p0b");
            var p1 = MakeOccurrence("Test 1", Sigma, Alfa, "p1a", "p1b");
            var c0 = MakeOccurrence("Test 1", Sigma, Alfa, "c0a", "c0b");
            var matcher = new ConfigurableClashMatcher();
            matcher.RespondWith(p0, c0, MakeAssessment(p0, c0, ClashMatchConfidence.Medium));
            matcher.RespondWith(p1, c0, MakeAssessment(p1, c0, ClashMatchConfidence.High));

            var result = ClassifyWith(
                matcher,
                MakeRun("run-1", new[] { Sigma, Alfa }, new[] { StandardTest }, p0, p1),
                MakeRun("run-2", new[] { Sigma, Alfa }, new[] { StandardTest }, c0));

            var unmatchedEntry = result.Entries.Single(e => e.PreviousOccurrence == p0);
            Assert.Equal(ClashLifecycleStatus.Unverifiable, unmatchedEntry.Status);
        }

        [Fact]
        public void UnmatchedPrevious_CaseOnlyDifferenceInTestName_StillObserved()
        {
            var previousOccurrence = MakeOccurrence("Test 1", Sigma, Alfa);
            var currentTestPresence = MakeOccurrence("TEST 1", Sigma, Alfa, "x", "y");
            var matcher = new ConfigurableClashMatcher();

            var result = ClassifyWith(
                matcher,
                MakeRun("run-1", new[] { Sigma, Alfa }, new[] { StandardTest }, previousOccurrence),
                MakeRun("run-2", new[] { Sigma, Alfa }, new[] { StandardTest }, currentTestPresence));

            var entry = result.Entries.Single(e => e.PreviousOccurrence == previousOccurrence);
            Assert.Equal(ClashLifecycleStatus.Resolved, entry.Status);
        }

        [Fact]
        public void UnmatchedPrevious_PunctuationDifferenceInTestName_IsNotTheSameTest()
        {
            var previousOccurrence = MakeOccurrence("HVAC vs Structure", Sigma, Alfa);
            var currentTestPresence = MakeOccurrence("HVAC-Structure", Sigma, Alfa, "x", "y");
            var matcher = new ConfigurableClashMatcher();

            var result = ClassifyWith(
                matcher,
                MakeRun("run-1", new[] { Sigma, Alfa }, new[] { TestFor("HVAC vs Structure", Sigma, Alfa) }, previousOccurrence),
                MakeRun("run-2", new[] { Sigma, Alfa }, new[] { TestFor("HVAC-Structure", Sigma, Alfa) }, currentTestPresence));

            var entry = result.Entries.Single(e => e.PreviousOccurrence == previousOccurrence);
            Assert.Equal(ClashLifecycleStatus.Unverifiable, entry.Status);
        }

        [Fact]
        public void UnmatchedPrevious_NumberDifferenceInTestName_IsNotTheSameTest()
        {
            var previousOccurrence = MakeOccurrence("Test 01", Sigma, Alfa);
            var currentTestPresence = MakeOccurrence("Test 1", Sigma, Alfa, "x", "y");
            var matcher = new ConfigurableClashMatcher();

            var result = ClassifyWith(
                matcher,
                MakeRun("run-1", new[] { Sigma, Alfa }, new[] { TestFor("Test 01", Sigma, Alfa) }, previousOccurrence),
                MakeRun("run-2", new[] { Sigma, Alfa }, new[] { StandardTest }, currentTestPresence));

            var entry = result.Entries.Single(e => e.PreviousOccurrence == previousOccurrence);
            Assert.Equal(ClashLifecycleStatus.Unverifiable, entry.Status);
        }

        [Fact]
        public void UnmatchedPrevious_DifferentRevisionInCurrent_StillPresent_IsResolved()
        {
            var sigmaR05 = MakeRevision("Sigma", "Structure", "Main", "R05");
            var alfaR08 = MakeRevision("Alfa", "HVAC", "Main", "R08");
            var previousOccurrence = MakeOccurrence("Test 1", Sigma, Alfa);
            var currentTestPresence = MakeOccurrence("Test 1", sigmaR05, alfaR08, "x", "y");
            var matcher = new ConfigurableClashMatcher();

            var result = ClassifyWith(
                matcher,
                MakeRun("run-1", new[] { Sigma, Alfa }, new[] { StandardTest }, previousOccurrence),
                MakeRun("run-2", new[] { sigmaR05, alfaR08 }, new[] { StandardTest }, currentTestPresence));

            var entry = result.Entries.Single(e => e.PreviousOccurrence == previousOccurrence);
            Assert.Equal(ClashLifecycleStatus.Resolved, entry.Status);
        }

        [Fact]
        public void UnmatchedPrevious_DifferentFileMetadataInCurrent_DoesNotPrevent()
        {
            var sigmaOtherFile = MakeRevision("Sigma", "Structure", "Main", "R04", sourceFileName: "Different.nwc", contentHash: "deadbeef");
            var previousOccurrence = MakeOccurrence("Test 1", Sigma, Alfa);
            var currentTestPresence = MakeOccurrence("Test 1", sigmaOtherFile, Alfa, "x", "y");
            var matcher = new ConfigurableClashMatcher();

            var result = ClassifyWith(
                matcher,
                MakeRun("run-1", new[] { Sigma, Alfa }, new[] { StandardTest }, previousOccurrence),
                MakeRun("run-2", new[] { sigmaOtherFile, Alfa }, new[] { StandardTest }, currentTestPresence));

            var entry = result.Entries.Single(e => e.PreviousOccurrence == previousOccurrence);
            Assert.Equal(ClashLifecycleStatus.Resolved, entry.Status);
        }

        [Fact]
        public void UnmatchedPrevious_SelfClash_IsVerifiable()
        {
            var previousOccurrence = MakeOccurrence("Test 1", Sigma, Sigma);
            var currentTestPresence = MakeOccurrence("Test 1", Sigma, Sigma, "x", "y");
            var matcher = new ConfigurableClashMatcher();

            var result = ClassifyWith(
                matcher,
                MakeRun("run-1", new[] { Sigma }, new[] { SelfClashTest }, previousOccurrence),
                MakeRun("run-2", new[] { Sigma }, new[] { SelfClashTest }, currentTestPresence));

            var entry = result.Entries.Single(e => e.PreviousOccurrence == previousOccurrence);
            Assert.Equal(ClashLifecycleStatus.Resolved, entry.Status);
        }

        [Fact]
        public void UnmatchedPrevious_RawClashStatusDoesNotAffectClassification()
        {
            var currentTestPresence = MakeOccurrence("Test 1", Sigma, Alfa, "x", "y");
            var matcher = new ConfigurableClashMatcher();

            ClashLifecycleStatus ClassifyWithRawStatus(ClashStatus rawStatus)
            {
                var previousOccurrence = MakeOccurrence("Test 1", Sigma, Alfa, "p", "q", "g1", rawStatus);
                var result = ClassifyWith(
                    matcher,
                    MakeRun("run-1", new[] { Sigma, Alfa }, new[] { StandardTest }, previousOccurrence),
                    MakeRun("run-2", new[] { Sigma, Alfa }, new[] { StandardTest }, currentTestPresence));
                return result.Entries.Single(e => e.PreviousOccurrence == previousOccurrence).Status;
            }

            var newStatus = ClassifyWithRawStatus(ClashStatus.New);
            var activeStatus = ClassifyWithRawStatus(ClashStatus.Active);
            var resolvedRawStatus = ClassifyWithRawStatus(ClashStatus.Resolved);

            Assert.Equal(ClashLifecycleStatus.Resolved, newStatus);
            Assert.Equal(ClashLifecycleStatus.Resolved, activeStatus);
            Assert.Equal(ClashLifecycleStatus.Resolved, resolvedRawStatus);
        }

        [Fact]
        public void Resolved_RequiredScenario_TwoClashesSameTest_OnePersistsOneDisappears()
        {
            var persisting = MakeOccurrence("Test 1", Sigma, Alfa, "persist-a", "persist-b");
            var disappearing = MakeOccurrence("Test 1", Sigma, Alfa, "gone-a", "gone-b");
            var current = MakeOccurrence("Test 1", Sigma, Alfa, "persist-a", "persist-b");

            var matcher = new ConfigurableClashMatcher();
            matcher.RespondWith(persisting, current, MakeAssessment(persisting, current, ClashMatchConfidence.High));

            var result = ClassifyWith(
                matcher,
                MakeRun("run-1", new[] { Sigma, Alfa }, new[] { StandardTest }, persisting, disappearing),
                MakeRun("run-2", new[] { Sigma, Alfa }, new[] { StandardTest }, current));

            var persistingEntry = result.Entries.Single(e => e.SelectedMatch != null);
            var disappearingEntry = result.Entries.Single(e => e.PreviousOccurrence == disappearing);

            Assert.Equal(ClashLifecycleStatus.StillOpen, persistingEntry.Status);
            Assert.Equal(ClashLifecycleStatus.Resolved, disappearingEntry.Status);
        }

        // ===================== 16.6 New (symmetric to Resolved) =====================

        [Fact]
        public void UnmatchedCurrent_NoAlternatives_ModelsAndTestPresent_IsNew()
        {
            var previousTestPresence = MakeOccurrence("Test 1", Sigma, Alfa, "x", "y");
            var currentOccurrence = MakeOccurrence("Test 1", Sigma, Alfa);
            var matcher = new ConfigurableClashMatcher();

            var result = ClassifyWith(
                matcher,
                MakeRun("run-1", new[] { Sigma, Alfa }, new[] { StandardTest }, previousTestPresence),
                MakeRun("run-2", new[] { Sigma, Alfa }, new[] { StandardTest }, currentOccurrence));

            var entry = result.Entries.Single(e => e.CurrentOccurrence == currentOccurrence);
            Assert.Equal(ClashLifecycleStatus.New, entry.Status);
        }

        [Fact]
        public void UnmatchedCurrent_ModelAAbsentFromPrevious_IsUnverifiable()
        {
            var previousTestPresence = MakeOccurrence("Test 1", Alfa, Alfa, "x", "y");
            var currentOccurrence = MakeOccurrence("Test 1", Sigma, Alfa);
            var matcher = new ConfigurableClashMatcher();

            var result = ClassifyWith(
                matcher,
                MakeRun("run-1", new[] { Alfa }, new[] { TestFor("Test 1", Alfa, Alfa) }, previousTestPresence),
                MakeRun("run-2", new[] { Sigma, Alfa }, new[] { StandardTest }, currentOccurrence));

            var entry = result.Entries.Single(e => e.CurrentOccurrence == currentOccurrence);
            Assert.Equal(ClashLifecycleStatus.Unverifiable, entry.Status);
        }

        [Fact]
        public void UnmatchedCurrent_ModelBAbsentFromPrevious_IsUnverifiable()
        {
            var previousTestPresence = MakeOccurrence("Test 1", Sigma, Sigma, "x", "y");
            var currentOccurrence = MakeOccurrence("Test 1", Sigma, Alfa);
            var matcher = new ConfigurableClashMatcher();

            var result = ClassifyWith(
                matcher,
                MakeRun("run-1", new[] { Sigma }, new[] { SelfClashTest }, previousTestPresence),
                MakeRun("run-2", new[] { Sigma, Alfa }, new[] { StandardTest }, currentOccurrence));

            var entry = result.Entries.Single(e => e.CurrentOccurrence == currentOccurrence);
            Assert.Equal(ClashLifecycleStatus.Unverifiable, entry.Status);
        }

        [Fact]
        public void UnmatchedCurrent_TestNotObserved_IsUnverifiable()
        {
            var previousOtherTest = MakeOccurrence("Test 2", Sigma, Alfa, "x", "y");
            var currentOccurrence = MakeOccurrence("Test 1", Sigma, Alfa);
            var matcher = new ConfigurableClashMatcher();

            var result = ClassifyWith(
                matcher,
                MakeRun("run-1", new[] { Sigma, Alfa }, new[] { TestFor("Test 2", Sigma, Alfa) }, previousOtherTest),
                MakeRun("run-2", new[] { Sigma, Alfa }, new[] { StandardTest }, currentOccurrence));

            var entry = result.Entries.Single(e => e.CurrentOccurrence == currentOccurrence);
            Assert.Equal(ClashLifecycleStatus.Unverifiable, entry.Status);
        }

        [Fact]
        public void UnmatchedCurrent_AlternativeReferencingItsCurrentIndex_IsUnverifiable()
        {
            var p0 = MakeOccurrence("Test 1", Sigma, Alfa, "p0a", "p0b");
            var c0 = MakeOccurrence("Test 1", Sigma, Alfa, "c0a", "c0b");
            var c1 = MakeOccurrence("Test 1", Sigma, Alfa, "c1a", "c1b");
            var matcher = new ConfigurableClashMatcher();
            matcher.RespondWith(p0, c0, MakeAssessment(p0, c0, ClashMatchConfidence.High));
            matcher.RespondWith(p0, c1, MakeAssessment(p0, c1, ClashMatchConfidence.Medium));

            var result = ClassifyWith(
                matcher,
                MakeRun("run-1", new[] { Sigma, Alfa }, new[] { StandardTest }, p0),
                MakeRun("run-2", new[] { Sigma, Alfa }, new[] { StandardTest }, c0, c1));

            var unmatchedEntry = result.Entries.Single(e => e.CurrentOccurrence == c1);
            Assert.Equal(ClashLifecycleStatus.Unverifiable, unmatchedEntry.Status);
        }

        [Fact]
        public void UnmatchedCurrent_CaseOnlyDifferenceInTestName_StillObserved()
        {
            var previousTestPresence = MakeOccurrence("TEST 1", Sigma, Alfa, "x", "y");
            var currentOccurrence = MakeOccurrence("Test 1", Sigma, Alfa);
            var matcher = new ConfigurableClashMatcher();

            var result = ClassifyWith(
                matcher,
                MakeRun("run-1", new[] { Sigma, Alfa }, new[] { StandardTest }, previousTestPresence),
                MakeRun("run-2", new[] { Sigma, Alfa }, new[] { StandardTest }, currentOccurrence));

            var entry = result.Entries.Single(e => e.CurrentOccurrence == currentOccurrence);
            Assert.Equal(ClashLifecycleStatus.New, entry.Status);
        }

        [Fact]
        public void UnmatchedCurrent_DifferentRevisionInPrevious_StillPresent_IsNew()
        {
            var sigmaR05 = MakeRevision("Sigma", "Structure", "Main", "R05");
            var previousTestPresence = MakeOccurrence("Test 1", sigmaR05, Alfa, "x", "y");
            var currentOccurrence = MakeOccurrence("Test 1", Sigma, Alfa);
            var matcher = new ConfigurableClashMatcher();

            var result = ClassifyWith(
                matcher,
                MakeRun("run-1", new[] { sigmaR05, Alfa }, new[] { StandardTest }, previousTestPresence),
                MakeRun("run-2", new[] { Sigma, Alfa }, new[] { StandardTest }, currentOccurrence));

            var entry = result.Entries.Single(e => e.CurrentOccurrence == currentOccurrence);
            Assert.Equal(ClashLifecycleStatus.New, entry.Status);
        }

        [Fact]
        public void UnmatchedCurrent_SelfClash_IsVerifiable()
        {
            var previousTestPresence = MakeOccurrence("Test 1", Sigma, Sigma, "x", "y");
            var currentOccurrence = MakeOccurrence("Test 1", Sigma, Sigma);
            var matcher = new ConfigurableClashMatcher();

            var result = ClassifyWith(
                matcher,
                MakeRun("run-1", new[] { Sigma }, new[] { SelfClashTest }, previousTestPresence),
                MakeRun("run-2", new[] { Sigma }, new[] { SelfClashTest }, currentOccurrence));

            var entry = result.Entries.Single(e => e.CurrentOccurrence == currentOccurrence);
            Assert.Equal(ClashLifecycleStatus.New, entry.Status);
        }

        [Fact]
        public void UnmatchedCurrent_RawClashStatusDoesNotAffectClassification()
        {
            var previousTestPresence = MakeOccurrence("Test 1", Sigma, Alfa, "x", "y");
            var matcher = new ConfigurableClashMatcher();

            ClashLifecycleStatus ClassifyWithRawStatus(ClashStatus rawStatus)
            {
                var currentOccurrence = MakeOccurrence("Test 1", Sigma, Alfa, "p", "q", "g1", rawStatus);
                var result = ClassifyWith(
                    matcher,
                    MakeRun("run-1", new[] { Sigma, Alfa }, new[] { StandardTest }, previousTestPresence),
                    MakeRun("run-2", new[] { Sigma, Alfa }, new[] { StandardTest }, currentOccurrence));
                return result.Entries.Single(e => e.CurrentOccurrence == currentOccurrence).Status;
            }

            Assert.Equal(ClashLifecycleStatus.New, ClassifyWithRawStatus(ClashStatus.New));
            Assert.Equal(ClashLifecycleStatus.New, ClassifyWithRawStatus(ClashStatus.Active));
            Assert.Equal(ClashLifecycleStatus.New, ClassifyWithRawStatus(ClashStatus.Resolved));
        }

        [Fact]
        public void New_RequiredScenario_AdditionalClashAppears_WhileAnotherOfSameTestExistedInPrevious()
        {
            var existing = MakeOccurrence("Test 1", Sigma, Alfa, "existing-a", "existing-b");
            var existingCurrentMatch = MakeOccurrence("Test 1", Sigma, Alfa, "existing-a", "existing-b");
            var additional = MakeOccurrence("Test 1", Sigma, Alfa, "new-a", "new-b");

            var matcher = new ConfigurableClashMatcher();
            matcher.RespondWith(existing, existingCurrentMatch, MakeAssessment(existing, existingCurrentMatch, ClashMatchConfidence.High));

            var result = ClassifyWith(
                matcher,
                MakeRun("run-1", new[] { Sigma, Alfa }, new[] { StandardTest }, existing),
                MakeRun("run-2", new[] { Sigma, Alfa }, new[] { StandardTest }, existingCurrentMatch, additional));

            var additionalEntry = result.Entries.Single(e => e.CurrentOccurrence == additional);
            Assert.Equal(ClashLifecycleStatus.New, additionalEntry.Status);
        }

        // ===================== 16.7 Empty runs =====================

        [Fact]
        public void Classify_BothRunsEmpty_ProducesZeroEntries()
        {
            var matcher = new ConfigurableClashMatcher();

            var result = ClassifyWith(
                matcher,
                MakeRun("run-1", new[] { Sigma }, Array.Empty<ExecutedClashTest>()),
                MakeRun("run-2", new[] { Sigma }, Array.Empty<ExecutedClashTest>()));

            Assert.Empty(result.Entries);
        }

        [Fact]
        public void Classify_EmptyPreviousRun_CurrentOccurrence_IsUnverifiableNotNew()
        {
            var currentOccurrence = MakeOccurrence("Test 1", Sigma, Alfa);
            var matcher = new ConfigurableClashMatcher();

            var result = ClassifyWith(
                matcher,
                MakeRun("run-1", new[] { Sigma, Alfa }, Array.Empty<ExecutedClashTest>()),
                MakeRun("run-2", new[] { Sigma, Alfa }, new[] { StandardTest }, currentOccurrence));

            var entry = Assert.Single(result.Entries);
            Assert.Equal(ClashLifecycleStatus.Unverifiable, entry.Status);
            Assert.NotEqual(ClashLifecycleStatus.New, entry.Status);
        }

        [Fact]
        public void Classify_EmptyCurrentRun_PreviousOccurrence_IsUnverifiableNotResolved()
        {
            var previousOccurrence = MakeOccurrence("Test 1", Sigma, Alfa);
            var matcher = new ConfigurableClashMatcher();

            var result = ClassifyWith(
                matcher,
                MakeRun("run-1", new[] { Sigma, Alfa }, new[] { StandardTest }, previousOccurrence),
                MakeRun("run-2", new[] { Sigma, Alfa }, Array.Empty<ExecutedClashTest>()));

            var entry = Assert.Single(result.Entries);
            Assert.Equal(ClashLifecycleStatus.Unverifiable, entry.Status);
            Assert.NotEqual(ClashLifecycleStatus.Resolved, entry.Status);
        }

        // ===================== 16.8 Duplicate slots =====================

        [Fact]
        public void Classify_SameReferenceInTwoSlots_ProducesDistinctEntriesByIndex_OneSelectedOneUnmatched()
        {
            var shared = MakeOccurrence("Test 1", Sigma, Alfa, "shared-a", "shared-b");
            var current = MakeOccurrence("Test 1", Sigma, Alfa, "shared-a", "shared-b");
            var matcher = new ConfigurableClashMatcher();
            matcher.RespondWith(shared, current, MakeAssessment(shared, current, ClashMatchConfidence.High));

            var result = ClassifyWith(
                matcher,
                MakeRun("run-1", new[] { Sigma, Alfa }, new[] { StandardTest }, shared, shared),
                MakeRun("run-2", new[] { Sigma, Alfa }, new[] { StandardTest }, current));

            Assert.Equal(2, result.Entries.Count);
            var selectedEntry = result.Entries.Single(e => e.SelectedMatch != null);
            var unmatchedEntry = result.Entries.Single(e => e.SelectedMatch == null);
            Assert.Equal(0, selectedEntry.PreviousIndex);
            Assert.Equal(1, unmatchedEntry.PreviousIndex);
            Assert.Same(shared, unmatchedEntry.PreviousOccurrence);
        }

        [Fact]
        public void Classify_DuplicateSlots_PreservesOrder_NoDistinctApplied()
        {
            var shared = MakeOccurrence("Test 1", Sigma, Alfa, "shared-a", "shared-b");
            var matcher = new ConfigurableClashMatcher();

            var result = ClassifyWith(
                matcher,
                MakeRun("run-1", new[] { Sigma, Alfa }, new[] { StandardTest }, shared, shared),
                MakeRun("run-2", new[] { Sigma, Alfa }, Array.Empty<ExecutedClashTest>()));

            Assert.Equal(2, result.Entries.Count);
            Assert.Equal(0, result.Entries[0].PreviousIndex);
            Assert.Equal(1, result.Entries[1].PreviousIndex);
        }

        // ===================== 16.9 Order and determinism =====================

        [Fact]
        public void Classify_EntryOrder_IsSelectedThenUnmatchedPreviousThenUnmatchedCurrent()
        {
            var p0 = MakeOccurrence("Test 1", Sigma, Alfa, "p0a", "p0b");
            var p1 = MakeOccurrence("Test 1", Sigma, Alfa, "p1a", "p1b");
            var c0 = MakeOccurrence("Test 1", Sigma, Alfa, "c0a", "c0b");
            var c1 = MakeOccurrence("Test 1", Sigma, Alfa, "c1a", "c1b");
            var matcher = new ConfigurableClashMatcher();
            matcher.RespondWith(p0, c0, MakeAssessment(p0, c0, ClashMatchConfidence.High));

            var result = ClassifyWith(
                matcher,
                MakeRun("run-1", new[] { Sigma, Alfa }, new[] { StandardTest }, p0, p1),
                MakeRun("run-2", new[] { Sigma, Alfa }, new[] { StandardTest }, c0, c1));

            Assert.Equal(3, result.Entries.Count);
            Assert.NotNull(result.Entries[0].SelectedMatch);
            Assert.Null(result.Entries[1].SelectedMatch);
            Assert.True(result.Entries[1].PreviousIndex.HasValue);
            Assert.Null(result.Entries[2].SelectedMatch);
            Assert.True(result.Entries[2].CurrentIndex.HasValue);
        }

        [Fact]
        public void Classify_IsDeterministic_ForTheSameMatchResult()
        {
            var p0 = MakeOccurrence("Test 1", Sigma, Alfa, "p0a", "p0b");
            var p1 = MakeOccurrence("Test 1", Sigma, Alfa, "p1a", "p1b");
            var c0 = MakeOccurrence("Test 1", Sigma, Alfa, "c0a", "c0b");
            var c1 = MakeOccurrence("Test 1", Sigma, Alfa, "c1a", "c1b");
            var matcher = new ConfigurableClashMatcher();
            matcher.RespondWith(p0, c0, MakeAssessment(p0, c0, ClashMatchConfidence.High));

            var matchResult = new DeterministicClashRunComparer(matcher).Compare(
                MakeRun("run-1", new[] { Sigma, Alfa }, new[] { StandardTest }, p0, p1),
                MakeRun("run-2", new[] { Sigma, Alfa }, new[] { StandardTest }, c0, c1));

            var first = Classifier.Classify(matchResult);
            var second = Classifier.Classify(matchResult);

            Assert.Equal(first.Entries.Count, second.Entries.Count);
            for (int i = 0; i < first.Entries.Count; i++)
            {
                Assert.Equal(first.Entries[i].Status, second.Entries[i].Status);
                Assert.Equal(first.Entries[i].PreviousIndex, second.Entries[i].PreviousIndex);
                Assert.Equal(first.Entries[i].CurrentIndex, second.Entries[i].CurrentIndex);
                Assert.Equal(first.Entries[i].Evidence.Count, second.Entries[i].Evidence.Count);
            }
        }

        // ===================== Explicit executed-clash-test coverage =====================

        [Fact]
        public void ZeroResult_UnmatchedPrevious_CurrentDeclaresTestWithNoOccurrence_IsResolved()
        {
            var previousOccurrence = MakeOccurrence("Test 1", Sigma, Alfa);
            var matcher = new ConfigurableClashMatcher();

            var result = ClassifyWith(
                matcher,
                MakeRun("run-1", new[] { Sigma, Alfa }, new[] { StandardTest }, previousOccurrence),
                MakeRun("run-2", new[] { Sigma, Alfa }, new[] { StandardTest }));

            var entry = Assert.Single(result.Entries);
            Assert.Equal(ClashLifecycleStatus.Resolved, entry.Status);
        }

        [Fact]
        public void ZeroResult_UnmatchedCurrent_PreviousDeclaresTestWithNoOccurrence_IsNew()
        {
            var currentOccurrence = MakeOccurrence("Test 1", Sigma, Alfa);
            var matcher = new ConfigurableClashMatcher();

            var result = ClassifyWith(
                matcher,
                MakeRun("run-1", new[] { Sigma, Alfa }, new[] { StandardTest }),
                MakeRun("run-2", new[] { Sigma, Alfa }, new[] { StandardTest }, currentOccurrence));

            var entry = Assert.Single(result.Entries);
            Assert.Equal(ClashLifecycleStatus.New, entry.Status);
        }

        [Fact]
        public void DeclaredTest_SameNameDeclaredForDifferentPair_IsUnverifiable()
        {
            var previousOccurrence = MakeOccurrence("Test 1", Sigma, Alfa);
            var matcher = new ConfigurableClashMatcher();

            var result = ClassifyWith(
                matcher,
                MakeRun("run-1", new[] { Sigma, Alfa }, new[] { StandardTest }, previousOccurrence),
                MakeRun("run-2", new[] { Sigma, Alfa, Beta }, new[] { TestFor("Test 1", Sigma, Beta) }));

            var entry = Assert.Single(result.Entries);
            Assert.Equal(ClashLifecycleStatus.Unverifiable, entry.Status);
        }

        [Fact]
        public void DeclaredTest_SamePairDeclaredWithDifferentName_IsUnverifiable()
        {
            var previousOccurrence = MakeOccurrence("Test 1", Sigma, Alfa);
            var matcher = new ConfigurableClashMatcher();

            var result = ClassifyWith(
                matcher,
                MakeRun("run-1", new[] { Sigma, Alfa }, new[] { StandardTest }, previousOccurrence),
                MakeRun("run-2", new[] { Sigma, Alfa }, new[] { TestFor("Test 2", Sigma, Alfa) }));

            var entry = Assert.Single(result.Entries);
            Assert.Equal(ClashLifecycleStatus.Unverifiable, entry.Status);
        }

        [Fact]
        public void DeclaredTest_AbInvertedDeclaration_IsVerifiable()
        {
            var previousOccurrence = MakeOccurrence("Test 1", Sigma, Alfa);
            var matcher = new ConfigurableClashMatcher();

            var result = ClassifyWith(
                matcher,
                MakeRun("run-1", new[] { Sigma, Alfa }, new[] { StandardTest }, previousOccurrence),
                MakeRun("run-2", new[] { Sigma, Alfa }, new[] { TestFor("Test 1", Alfa, Sigma) }));

            var entry = Assert.Single(result.Entries);
            Assert.Equal(ClashLifecycleStatus.Resolved, entry.Status);
        }

        [Fact]
        public void DeclaredTest_CaseOnlyNameDifference_IsVerifiable()
        {
            var previousOccurrence = MakeOccurrence("Test 1", Sigma, Alfa);
            var matcher = new ConfigurableClashMatcher();

            var result = ClassifyWith(
                matcher,
                MakeRun("run-1", new[] { Sigma, Alfa }, new[] { StandardTest }, previousOccurrence),
                MakeRun("run-2", new[] { Sigma, Alfa }, new[] { TestFor("TEST 1", Sigma, Alfa) }));

            var entry = Assert.Single(result.Entries);
            Assert.Equal(ClashLifecycleStatus.Resolved, entry.Status);
        }

        [Fact]
        public void DeclaredTest_EmptyExecutedClashTestsList_IsUnverifiable()
        {
            var previousOccurrence = MakeOccurrence("Test 1", Sigma, Alfa);
            var matcher = new ConfigurableClashMatcher();

            var result = ClassifyWith(
                matcher,
                MakeRun("run-1", new[] { Sigma, Alfa }, new[] { StandardTest }, previousOccurrence),
                MakeRun("run-2", new[] { Sigma, Alfa }, Array.Empty<ExecutedClashTest>()));

            var entry = Assert.Single(result.Entries);
            Assert.Equal(ClashLifecycleStatus.Unverifiable, entry.Status);
        }

        // ===================== 16.10 Real Core integration =====================

        [Fact]
        public void Integration_OneClashPersists_IsStillOpen()
        {
            var previousOccurrence = MakeOccurrence("Test 1", Sigma, Alfa, "elem-a", "elem-b", "guid-1");
            var currentOccurrence = MakeOccurrence("Test 1", Sigma, Alfa, "elem-a", "elem-b", "guid-1");

            var matchResult = new DeterministicClashRunComparer(new ConservativeClashMatcher()).Compare(
                MakeRun("run-1", new[] { Sigma, Alfa }, new[] { StandardTest }, previousOccurrence),
                MakeRun("run-2", new[] { Sigma, Alfa }, new[] { StandardTest }, currentOccurrence));
            var result = Classifier.Classify(matchResult);

            var entry = Assert.Single(result.Entries);
            Assert.Equal(ClashLifecycleStatus.StillOpen, entry.Status);
        }

        [Fact]
        public void Integration_TwoClashesSameTest_OnePersistsOneDisappears_ModelsStillDeclared_ProducesStillOpenAndResolved()
        {
            var persisting = MakeOccurrence("Test 1", Sigma, Alfa, "persist-a", "persist-b", "guid-1");
            var disappearing = MakeOccurrence("Test 1", Sigma, Alfa, "gone-a", "gone-b", "guid-2");
            var current = MakeOccurrence("Test 1", Sigma, Alfa, "persist-a", "persist-b", "guid-1");

            var matchResult = new DeterministicClashRunComparer(new ConservativeClashMatcher()).Compare(
                MakeRun("run-1", new[] { Sigma, Alfa }, new[] { StandardTest }, persisting, disappearing),
                MakeRun("run-2", new[] { Sigma, Alfa }, new[] { StandardTest }, current));
            var result = Classifier.Classify(matchResult);

            var persistingEntry = result.Entries.Single(e => e.SelectedMatch != null);
            var disappearingEntry = result.Entries.Single(e => e.PreviousOccurrence == disappearing);

            Assert.Equal(ClashLifecycleStatus.StillOpen, persistingEntry.Status);
            Assert.Equal(ClashLifecycleStatus.Resolved, disappearingEntry.Status);
        }

        [Fact]
        public void Integration_ClashDisappears_CurrentDeclaresTestWithZeroOccurrences_IsResolved()
        {
            var previousOccurrence = MakeOccurrence("Test 1", Sigma, Alfa, "elem-a", "elem-b", "guid-1");

            var matchResult = new DeterministicClashRunComparer(new ConservativeClashMatcher()).Compare(
                MakeRun("run-1", new[] { Sigma, Alfa }, new[] { StandardTest }, previousOccurrence),
                MakeRun("run-2", new[] { Sigma, Alfa }, new[] { StandardTest }));
            var result = Classifier.Classify(matchResult);

            var entry = Assert.Single(result.Entries);
            Assert.Equal(ClashLifecycleStatus.Resolved, entry.Status);
        }

        [Fact]
        public void Integration_ClashAppears_PreviousDeclaredTestWithZeroOccurrences_IsNew()
        {
            var currentOccurrence = MakeOccurrence("Test 1", Sigma, Alfa, "elem-a", "elem-b", "guid-1");

            var matchResult = new DeterministicClashRunComparer(new ConservativeClashMatcher()).Compare(
                MakeRun("run-1", new[] { Sigma, Alfa }, new[] { StandardTest }),
                MakeRun("run-2", new[] { Sigma, Alfa }, new[] { StandardTest }, currentOccurrence));
            var result = Classifier.Classify(matchResult);

            var entry = Assert.Single(result.Entries);
            Assert.Equal(ClashLifecycleStatus.New, entry.Status);
        }
    }
}
