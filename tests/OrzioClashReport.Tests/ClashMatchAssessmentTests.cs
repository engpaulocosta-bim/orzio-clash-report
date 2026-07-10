using System;
using System.Collections.Generic;
using OrzioClashReport.Core.Model;

namespace OrzioClashReport.Tests
{
    /// <summary>Unit tests for ClashMatchAssessment: an auditable candidate evaluation of two clash occurrences, backed by ordered evidence.</summary>
    public class ClashMatchAssessmentTests
    {
        private static ModelRevision MakeRevision(string company, string discipline, string modelName, string revision) =>
            new ModelRevision(new ModelIdentity(company, discipline, modelName), revision, $"{modelName}_{revision}.nwc", null, null, null);

        private static ClashObject MakeObject(string id) => new ClashObject(id, null, null, null, null, null);

        private static ClashResult MakeClash(string? name = "Clash1", string? guid = "g1") =>
            new ClashResult(name, ClashStatus.New, null, null, null, MakeObject("a"), MakeObject("b"), guid);

        private static ClashOccurrence MakeOccurrence(string clashTestName = "Test 1", string? name = "Clash1", string? guid = "g1") =>
            new ClashOccurrence(
                clashTestName, MakeClash(name, guid),
                MakeRevision("Sigma", "Structure", "Main", "R04"),
                MakeRevision("Alfa", "HVAC", "Main", "R07"));

        private static MatchEvidence MakeEvidence(
            MatchEvidenceKind kind = MatchEvidenceKind.ClashTestName,
            MatchEvidenceVerdict verdict = MatchEvidenceVerdict.Supports,
            string description = "description") =>
            new MatchEvidence(kind, verdict, description, null, null);

        [Fact]
        public void Constructor_RejectsNullPreviousOccurrence()
        {
            Assert.Throws<ArgumentNullException>(
                () => new ClashMatchAssessment(null!, MakeOccurrence(), ClashMatchConfidence.Medium, new[] { MakeEvidence() }));
        }

        [Fact]
        public void Constructor_RejectsNullCurrentOccurrence()
        {
            Assert.Throws<ArgumentNullException>(
                () => new ClashMatchAssessment(MakeOccurrence(), null!, ClashMatchConfidence.Medium, new[] { MakeEvidence() }));
        }

        [Fact]
        public void Constructor_RejectsUndefinedConfidence()
        {
            Assert.Throws<ArgumentException>(
                () => new ClashMatchAssessment(MakeOccurrence(), MakeOccurrence(), (ClashMatchConfidence)999, new[] { MakeEvidence() }));
        }

        [Fact]
        public void Constructor_RejectsNullEvidence()
        {
            Assert.Throws<ArgumentNullException>(
                () => new ClashMatchAssessment(MakeOccurrence(), MakeOccurrence(), ClashMatchConfidence.Medium, null!));
        }

        [Fact]
        public void Constructor_RejectsEmptyEvidence()
        {
            Assert.Throws<ArgumentException>(
                () => new ClashMatchAssessment(MakeOccurrence(), MakeOccurrence(), ClashMatchConfidence.Medium, Array.Empty<MatchEvidence>()));
        }

        [Fact]
        public void Constructor_RejectsNullItemInEvidence()
        {
            var evidence = new MatchEvidence?[] { MakeEvidence(), null };

            Assert.Throws<ArgumentException>(
                () => new ClashMatchAssessment(MakeOccurrence(), MakeOccurrence(), ClashMatchConfidence.Medium, evidence!));
        }

        [Fact]
        public void Constructor_DefensivelyCopiesEvidenceList()
        {
            var source = new List<MatchEvidence> { MakeEvidence() };

            var assessment = new ClashMatchAssessment(MakeOccurrence(), MakeOccurrence(), ClashMatchConfidence.Medium, source);
            source.Clear();

            Assert.Single(assessment.Evidence);
        }

        [Fact]
        public void Constructor_PreservesEvidenceOrder()
        {
            var first = MakeEvidence(MatchEvidenceKind.SourceClashGuid, MatchEvidenceVerdict.Supports, "guids equal");
            var second = MakeEvidence(MatchEvidenceKind.SpatialPosition, MatchEvidenceVerdict.Contradicts, "far apart");

            var assessment = new ClashMatchAssessment(MakeOccurrence(), MakeOccurrence(), ClashMatchConfidence.Medium, new[] { first, second });

            Assert.Same(first, assessment.Evidence[0]);
            Assert.Same(second, assessment.Evidence[1]);
        }

        [Fact]
        public void Constructor_PreservesPreviousOccurrenceReference()
        {
            var previous = MakeOccurrence();
            var current = MakeOccurrence();

            var assessment = new ClashMatchAssessment(previous, current, ClashMatchConfidence.Medium, new[] { MakeEvidence() });

            Assert.Same(previous, assessment.PreviousOccurrence);
        }

        [Fact]
        public void Constructor_PreservesCurrentOccurrenceReference()
        {
            var previous = MakeOccurrence();
            var current = MakeOccurrence();

            var assessment = new ClashMatchAssessment(previous, current, ClashMatchConfidence.Medium, new[] { MakeEvidence() });

            Assert.Same(current, assessment.CurrentOccurrence);
        }

        [Fact]
        public void Constructor_PreservesConfidence()
        {
            var assessment = new ClashMatchAssessment(MakeOccurrence(), MakeOccurrence(), ClashMatchConfidence.High, new[] { MakeEvidence() });

            Assert.Equal(ClashMatchConfidence.High, assessment.Confidence);
        }

        [Fact]
        public void Constructor_AcceptsDuplicateEvidence()
        {
            var evidence = MakeEvidence();

            var assessment = new ClashMatchAssessment(MakeOccurrence(), MakeOccurrence(), ClashMatchConfidence.Medium, new[] { evidence, evidence });

            Assert.Equal(2, assessment.Evidence.Count);
        }

        [Fact]
        public void Constructor_AcceptsMixedVerdicts()
        {
            var supports = MakeEvidence(MatchEvidenceKind.SourceClashGuid, MatchEvidenceVerdict.Supports);
            var contradicts = MakeEvidence(MatchEvidenceKind.SpatialPosition, MatchEvidenceVerdict.Contradicts);

            var assessment = new ClashMatchAssessment(
                MakeOccurrence(), MakeOccurrence(), ClashMatchConfidence.Medium, new[] { supports, contradicts });

            Assert.Equal(2, assessment.Evidence.Count);
        }

        [Fact]
        public void Constructor_AcceptsHighConfidence_WithoutGuidEvidence()
        {
            var evidence = MakeEvidence(MatchEvidenceKind.ElementMetadata, MatchEvidenceVerdict.Supports, "same category and level");

            var assessment = new ClashMatchAssessment(MakeOccurrence(), MakeOccurrence(), ClashMatchConfidence.High, new[] { evidence });

            Assert.Equal(ClashMatchConfidence.High, assessment.Confidence);
        }

        [Fact]
        public void Constructor_AcceptsLowConfidence_WithFavorableGuidEvidence()
        {
            var evidence = MakeEvidence(MatchEvidenceKind.SourceClashGuid, MatchEvidenceVerdict.Supports, "source GUIDs are equal");

            var assessment = new ClashMatchAssessment(MakeOccurrence(), MakeOccurrence(), ClashMatchConfidence.Low, new[] { evidence });

            Assert.Equal(ClashMatchConfidence.Low, assessment.Confidence);
        }

        [Fact]
        public void Constructor_AcceptsSameOccurrenceInstanceOnBothSides()
        {
            var occurrence = MakeOccurrence();

            var assessment = new ClashMatchAssessment(occurrence, occurrence, ClashMatchConfidence.Medium, new[] { MakeEvidence() });

            Assert.Same(occurrence, assessment.PreviousOccurrence);
            Assert.Same(occurrence, assessment.CurrentOccurrence);
        }

        [Fact]
        public void Constructor_DoesNotAlterOccurrenceAOrBOrder()
        {
            var previous = MakeOccurrence();
            var current = MakeOccurrence();

            var assessment = new ClashMatchAssessment(previous, current, ClashMatchConfidence.Medium, new[] { MakeEvidence() });

            Assert.Same(previous.ModelA, assessment.PreviousOccurrence.ModelA);
            Assert.Same(previous.ModelB, assessment.PreviousOccurrence.ModelB);
            Assert.Same(current.ModelA, assessment.CurrentOccurrence.ModelA);
            Assert.Same(current.ModelB, assessment.CurrentOccurrence.ModelB);
        }

        [Fact]
        public void ToString_IncludesConfidence()
        {
            var assessment = new ClashMatchAssessment(MakeOccurrence(), MakeOccurrence(), ClashMatchConfidence.High, new[] { MakeEvidence() });

            Assert.Contains("High", assessment.ToString());
        }

        [Fact]
        public void ToString_IncludesEvidenceCount()
        {
            var assessment = new ClashMatchAssessment(
                MakeOccurrence(), MakeOccurrence(), ClashMatchConfidence.Medium, new[] { MakeEvidence(), MakeEvidence() });

            Assert.Contains("2", assessment.ToString());
        }

        [Fact]
        public void ToString_IncludesPreviousOccurrence()
        {
            var previous = MakeOccurrence(clashTestName: "Previous Test");
            var assessment = new ClashMatchAssessment(previous, MakeOccurrence(), ClashMatchConfidence.Medium, new[] { MakeEvidence() });

            Assert.Contains("Previous Test", assessment.ToString());
        }

        [Fact]
        public void ToString_IncludesCurrentOccurrence()
        {
            var current = MakeOccurrence(clashTestName: "Current Test");
            var assessment = new ClashMatchAssessment(MakeOccurrence(), current, ClashMatchConfidence.Medium, new[] { MakeEvidence() });

            Assert.Contains("Current Test", assessment.ToString());
        }

        [Fact]
        public void Constructor_DoesNotComputeConfidence_SameOccurrencesAndEvidenceCanYieldEitherLowOrHigh()
        {
            var previous = MakeOccurrence();
            var current = MakeOccurrence();
            var evidence = new[] { MakeEvidence(MatchEvidenceKind.SourceClashGuid, MatchEvidenceVerdict.Supports, "source GUIDs are equal") };

            var lowAssessment = new ClashMatchAssessment(previous, current, ClashMatchConfidence.Low, evidence);
            var highAssessment = new ClashMatchAssessment(previous, current, ClashMatchConfidence.High, evidence);

            Assert.Equal(ClashMatchConfidence.Low, lowAssessment.Confidence);
            Assert.Equal(ClashMatchConfidence.High, highAssessment.Confidence);
        }

        [Fact]
        public void Constructor_PreservesMixedEvidence_InDeclaredOrder()
        {
            var guidEvidence = MakeEvidence(MatchEvidenceKind.SourceClashGuid, MatchEvidenceVerdict.Supports, "source GUIDs are equal");
            var spatialEvidence = MakeEvidence(MatchEvidenceKind.SpatialPosition, MatchEvidenceVerdict.Contradicts, "points are far apart");
            var metadataEvidence = MakeEvidence(MatchEvidenceKind.ElementMetadata, MatchEvidenceVerdict.Unavailable, "no metadata reported");

            var assessment = new ClashMatchAssessment(
                MakeOccurrence(), MakeOccurrence(), ClashMatchConfidence.Medium,
                new[] { guidEvidence, spatialEvidence, metadataEvidence });

            Assert.Equal(3, assessment.Evidence.Count);
            Assert.Same(guidEvidence, assessment.Evidence[0]);
            Assert.Same(spatialEvidence, assessment.Evidence[1]);
            Assert.Same(metadataEvidence, assessment.Evidence[2]);
        }
    }
}
