using System;
using System.Linq;
using OrzioClashReport.Core.Abstractions;
using OrzioClashReport.Core.Matching;
using OrzioClashReport.Core.Model;

namespace OrzioClashReport.Tests
{
    /// <summary>
    /// Tests for ConservativeClashMatcher: the first concrete pairwise matcher, requiring matching clash-test
    /// name, revision-free model-identity pair, and model-aligned element-id pair, with source GUID as
    /// supplemental-only evidence affecting only confidence.
    /// </summary>
    public class ConservativeClashMatcherTests
    {
        private static ModelRevision MakeRevision(
            string company, string discipline, string modelName, string revision,
            string? sourceFileName = null, string? sourceFilePath = null, string? contentHash = null, DateTimeOffset? publishedAt = null) =>
            new ModelRevision(
                new ModelIdentity(company, discipline, modelName), revision, sourceFileName ?? $"{modelName}_{revision}.nwc",
                sourceFilePath, contentHash, publishedAt);

        private static ClashObject MakeObject(string elementId) => new ClashObject(elementId, null, null, null, null, null);

        private static ClashResult MakeClash(string elementIdA, string elementIdB, string? guid, string? name = "Clash1") =>
            new ClashResult(name, ClashStatus.New, null, null, null, MakeObject(elementIdA), MakeObject(elementIdB), guid);

        private static ClashOccurrence MakeOccurrence(
            string clashTestName, ModelRevision modelA, ModelRevision modelB, string elementIdA, string elementIdB, string? guid = "g1") =>
            new ClashOccurrence(clashTestName, MakeClash(elementIdA, elementIdB, guid), modelA, modelB);

        private static readonly ModelRevision SigmaStructurePrevious = MakeRevision("Sigma", "Structure", "Main", "R03");
        private static readonly ModelRevision SigmaStructureCurrent = MakeRevision("Sigma", "Structure", "Main", "R04");
        private static readonly ModelRevision AlfaHvacPrevious = MakeRevision("Alfa", "HVAC", "Main", "R07");
        private static readonly ModelRevision AlfaHvacCurrent = MakeRevision("Alfa", "HVAC", "Main", "R08");

        private static ClashOccurrence DefaultPrevious(string? guid = "g1") =>
            MakeOccurrence("Test 1", SigmaStructurePrevious, AlfaHvacPrevious, "elem-A", "elem-B", guid);

        private static ClashOccurrence DefaultCurrent(string? guid = "g1") =>
            MakeOccurrence("Test 1", SigmaStructureCurrent, AlfaHvacCurrent, "elem-A", "elem-B", guid);

        private static readonly ConservativeClashMatcher Matcher = new ConservativeClashMatcher();

        // --- 15.1 Arguments and basic contract ---

        [Fact]
        public void Assess_RejectsNullPreviousOccurrence()
        {
            Assert.Throws<ArgumentNullException>(() => Matcher.Assess(null!, DefaultCurrent()));
        }

        [Fact]
        public void Assess_RejectsNullCurrentOccurrence()
        {
            Assert.Throws<ArgumentNullException>(() => Matcher.Assess(DefaultPrevious(), null!));
        }

        [Fact]
        public void ConservativeClashMatcher_ImplementsIClashMatcher()
        {
            Assert.IsAssignableFrom<IClashMatcher>(Matcher);
        }

        [Fact]
        public void Assess_PreservesPreviousOccurrenceReference()
        {
            var previous = DefaultPrevious();
            var result = Matcher.Assess(previous, DefaultCurrent());

            Assert.Same(previous, result!.PreviousOccurrence);
        }

        [Fact]
        public void Assess_PreservesCurrentOccurrenceReference()
        {
            var current = DefaultCurrent();
            var result = Matcher.Assess(DefaultPrevious(), current);

            Assert.Same(current, result!.CurrentOccurrence);
        }

        // --- 15.2 Clash test name ---

        [Fact]
        public void Assess_ExactSameClashTestName_ProducesCandidate()
        {
            var result = Matcher.Assess(DefaultPrevious(), DefaultCurrent());

            Assert.NotNull(result);
        }

        [Fact]
        public void Assess_ClashTestNameDifferingOnlyByCase_ProducesCandidate()
        {
            var previous = MakeOccurrence("hvac VS structure", SigmaStructurePrevious, AlfaHvacPrevious, "elem-A", "elem-B");
            var current = MakeOccurrence("HVAC vs Structure", SigmaStructureCurrent, AlfaHvacCurrent, "elem-A", "elem-B");

            var result = Matcher.Assess(previous, current);

            Assert.NotNull(result);
        }

        [Fact]
        public void Assess_DifferentClashTestName_ReturnsNull()
        {
            var previous = MakeOccurrence("HVAC vs Structure", SigmaStructurePrevious, AlfaHvacPrevious, "elem-A", "elem-B");
            var current = MakeOccurrence("Electrical vs Structure", SigmaStructureCurrent, AlfaHvacCurrent, "elem-A", "elem-B");

            Assert.Null(Matcher.Assess(previous, current));
        }

        [Fact]
        public void Assess_DifferentPunctuationInClashTestName_ReturnsNull()
        {
            var previous = MakeOccurrence("HVAC vs Structure", SigmaStructurePrevious, AlfaHvacPrevious, "elem-A", "elem-B");
            var current = MakeOccurrence("HVAC-Structure", SigmaStructureCurrent, AlfaHvacCurrent, "elem-A", "elem-B");

            Assert.Null(Matcher.Assess(previous, current));
        }

        [Fact]
        public void Assess_DifferentNumberInClashTestName_ReturnsNull()
        {
            var previous = MakeOccurrence("Test 01", SigmaStructurePrevious, AlfaHvacPrevious, "elem-A", "elem-B");
            var current = MakeOccurrence("Test 1", SigmaStructureCurrent, AlfaHvacCurrent, "elem-A", "elem-B");

            Assert.Null(Matcher.Assess(previous, current));
        }

        // --- 15.3 Revisions and models ---

        [Fact]
        public void Assess_SameModelIdentityWithDifferentRevisions_ProducesCandidate()
        {
            // SigmaStructurePrevious is R03, SigmaStructureCurrent is R04 -- same identity, different revision.
            var result = Matcher.Assess(DefaultPrevious(), DefaultCurrent());

            Assert.NotNull(result);
        }

        [Fact]
        public void Assess_DifferentSourceFileName_DoesNotPreventCandidate()
        {
            var modelA = MakeRevision("Sigma", "Structure", "Main", "R04", sourceFileName: "Different.nwc");
            var previous = MakeOccurrence("Test 1", SigmaStructurePrevious, AlfaHvacPrevious, "elem-A", "elem-B");
            var current = MakeOccurrence("Test 1", modelA, AlfaHvacCurrent, "elem-A", "elem-B");

            Assert.NotNull(Matcher.Assess(previous, current));
        }

        [Fact]
        public void Assess_DifferentSourceFilePath_DoesNotPreventCandidate()
        {
            var modelA = MakeRevision("Sigma", "Structure", "Main", "R04", sourceFilePath: "C:\\different\\path.nwc");
            var previous = MakeOccurrence("Test 1", SigmaStructurePrevious, AlfaHvacPrevious, "elem-A", "elem-B");
            var current = MakeOccurrence("Test 1", modelA, AlfaHvacCurrent, "elem-A", "elem-B");

            Assert.NotNull(Matcher.Assess(previous, current));
        }

        [Fact]
        public void Assess_DifferentContentHash_DoesNotPreventCandidate()
        {
            var modelA = MakeRevision("Sigma", "Structure", "Main", "R04", contentHash: "deadbeef");
            var previous = MakeOccurrence("Test 1", SigmaStructurePrevious, AlfaHvacPrevious, "elem-A", "elem-B");
            var current = MakeOccurrence("Test 1", modelA, AlfaHvacCurrent, "elem-A", "elem-B");

            Assert.NotNull(Matcher.Assess(previous, current));
        }

        [Fact]
        public void Assess_DifferentPublishedAt_DoesNotPreventCandidate()
        {
            var modelA = MakeRevision("Sigma", "Structure", "Main", "R04", publishedAt: new DateTimeOffset(2026, 7, 10, 9, 0, 0, TimeSpan.Zero));
            var previous = MakeOccurrence("Test 1", SigmaStructurePrevious, AlfaHvacPrevious, "elem-A", "elem-B");
            var current = MakeOccurrence("Test 1", modelA, AlfaHvacCurrent, "elem-A", "elem-B");

            Assert.NotNull(Matcher.Assess(previous, current));
        }

        [Fact]
        public void Assess_DifferentModelA_ReturnsNull()
        {
            var differentModelA = MakeRevision("Beta", "Architecture", "Main", "R01");
            var previous = DefaultPrevious();
            var current = MakeOccurrence("Test 1", differentModelA, AlfaHvacCurrent, "elem-A", "elem-B");

            Assert.Null(Matcher.Assess(previous, current));
        }

        [Fact]
        public void Assess_DifferentModelB_ReturnsNull()
        {
            var differentModelB = MakeRevision("Beta", "Architecture", "Main", "R01");
            var previous = DefaultPrevious();
            var current = MakeOccurrence("Test 1", SigmaStructureCurrent, differentModelB, "elem-A", "elem-B");

            Assert.Null(Matcher.Assess(previous, current));
        }

        [Fact]
        public void Assess_SwappedModelPair_ProducesCandidate()
        {
            var previous = MakeOccurrence("Test 1", SigmaStructurePrevious, AlfaHvacPrevious, "elem-S", "elem-H");
            // Models swapped: current.ModelA is Alfa/HVAC, current.ModelB is Sigma/Structure.
            var current = MakeOccurrence("Test 1", AlfaHvacCurrent, SigmaStructureCurrent, "elem-H", "elem-S");

            Assert.NotNull(Matcher.Assess(previous, current));
        }

        // --- 15.4 Element identifiers ---

        [Fact]
        public void Assess_EqualElementIdsInDirectAlignment_ProducesCandidate()
        {
            Assert.NotNull(Matcher.Assess(DefaultPrevious(), DefaultCurrent()));
        }

        [Fact]
        public void Assess_EqualElementIdsInSwappedAlignment_ProducesCandidate()
        {
            var previous = MakeOccurrence("Test 1", SigmaStructurePrevious, AlfaHvacPrevious, "elem-S", "elem-H");
            var current = MakeOccurrence("Test 1", AlfaHvacCurrent, SigmaStructureCurrent, "elem-H", "elem-S");

            Assert.NotNull(Matcher.Assess(previous, current));
        }

        [Fact]
        public void Assess_ElementIdsWithOuterWhitespace_AreTrimmed()
        {
            var previous = MakeOccurrence("Test 1", SigmaStructurePrevious, AlfaHvacPrevious, "  elem-A  ", "elem-B");
            var current = MakeOccurrence("Test 1", SigmaStructureCurrent, AlfaHvacCurrent, "elem-A", "  elem-B  ");

            Assert.NotNull(Matcher.Assess(previous, current));
        }

        [Fact]
        public void Assess_EmptyElementId_ReturnsNull()
        {
            var previous = MakeOccurrence("Test 1", SigmaStructurePrevious, AlfaHvacPrevious, "", "elem-B");
            var current = MakeOccurrence("Test 1", SigmaStructureCurrent, AlfaHvacCurrent, "elem-A", "elem-B");

            Assert.Null(Matcher.Assess(previous, current));
        }

        [Fact]
        public void Assess_WhitespaceElementId_ReturnsNull()
        {
            var previous = MakeOccurrence("Test 1", SigmaStructurePrevious, AlfaHvacPrevious, "   ", "elem-B");
            var current = MakeOccurrence("Test 1", SigmaStructureCurrent, AlfaHvacCurrent, "elem-A", "elem-B");

            Assert.Null(Matcher.Assess(previous, current));
        }

        [Fact]
        public void Assess_ElementIdDifferingOnlyByCase_ReturnsNull()
        {
            var previous = MakeOccurrence("Test 1", SigmaStructurePrevious, AlfaHvacPrevious, "Elem-A", "elem-B");
            var current = MakeOccurrence("Test 1", SigmaStructureCurrent, AlfaHvacCurrent, "elem-a", "elem-B");

            Assert.Null(Matcher.Assess(previous, current));
        }

        [Fact]
        public void Assess_DifferentElementIdA_ReturnsNull()
        {
            var previous = MakeOccurrence("Test 1", SigmaStructurePrevious, AlfaHvacPrevious, "elem-A1", "elem-B");
            var current = MakeOccurrence("Test 1", SigmaStructureCurrent, AlfaHvacCurrent, "elem-A2", "elem-B");

            Assert.Null(Matcher.Assess(previous, current));
        }

        [Fact]
        public void Assess_DifferentElementIdB_ReturnsNull()
        {
            var previous = MakeOccurrence("Test 1", SigmaStructurePrevious, AlfaHvacPrevious, "elem-A", "elem-B1");
            var current = MakeOccurrence("Test 1", SigmaStructureCurrent, AlfaHvacCurrent, "elem-A", "elem-B2");

            Assert.Null(Matcher.Assess(previous, current));
        }

        [Fact]
        public void Assess_SameElementIdSetSwappedBetweenDifferentModels_WithModelsNotSwapped_ReturnsNull()
        {
            // Previous: Structure/element-S, HVAC/element-H (models on same sides as current)
            var previous = MakeOccurrence("Test 1", SigmaStructurePrevious, AlfaHvacPrevious, "element-S", "element-H");
            // Current: Structure/element-H, HVAC/element-S -- models stayed on the same side, but ids were swapped
            // between the two different models. This must be rejected even though the id *set* is identical.
            var current = MakeOccurrence("Test 1", SigmaStructureCurrent, AlfaHvacCurrent, "element-H", "element-S");

            Assert.Null(Matcher.Assess(previous, current));
        }

        // --- 15.5 Self-clash ---

        [Fact]
        public void Assess_SameModelIdentityBothSides_IdsInSameOrder_ProducesCandidate()
        {
            var selfModelPrevious = MakeRevision("Sigma", "Structure", "Main", "R03");
            var selfModelCurrent = MakeRevision("Sigma", "Structure", "Main", "R04");
            var previous = MakeOccurrence("Test 1", selfModelPrevious, selfModelPrevious, "elem-1", "elem-2");
            var current = MakeOccurrence("Test 1", selfModelCurrent, selfModelCurrent, "elem-1", "elem-2");

            Assert.NotNull(Matcher.Assess(previous, current));
        }

        [Fact]
        public void Assess_SameModelIdentityBothSides_IdsSwapped_ProducesCandidate()
        {
            var selfModelPrevious = MakeRevision("Sigma", "Structure", "Main", "R03");
            var selfModelCurrent = MakeRevision("Sigma", "Structure", "Main", "R04");
            var previous = MakeOccurrence("Test 1", selfModelPrevious, selfModelPrevious, "elem-1", "elem-2");
            var current = MakeOccurrence("Test 1", selfModelCurrent, selfModelCurrent, "elem-2", "elem-1");

            Assert.NotNull(Matcher.Assess(previous, current));
        }

        [Fact]
        public void Assess_SameModelIdentityBothSides_OnlyOneIdDifferent_ReturnsNull()
        {
            var selfModelPrevious = MakeRevision("Sigma", "Structure", "Main", "R03");
            var selfModelCurrent = MakeRevision("Sigma", "Structure", "Main", "R04");
            var previous = MakeOccurrence("Test 1", selfModelPrevious, selfModelPrevious, "elem-1", "elem-2");
            var current = MakeOccurrence("Test 1", selfModelCurrent, selfModelCurrent, "elem-1", "elem-3");

            Assert.Null(Matcher.Assess(previous, current));
        }

        // --- 15.6 GUID and confidence ---

        [Fact]
        public void Assess_EqualGuids_ProduceHigh()
        {
            var result = Matcher.Assess(DefaultPrevious("guid-1"), DefaultCurrent("guid-1"));

            Assert.Equal(ClashMatchConfidence.High, result!.Confidence);
        }

        [Fact]
        public void Assess_GuidsDifferingOnlyByCase_ProduceMedium()
        {
            var result = Matcher.Assess(DefaultPrevious("GUID-1"), DefaultCurrent("guid-1"));

            Assert.Equal(ClashMatchConfidence.Medium, result!.Confidence);
        }

        [Fact]
        public void Assess_DifferentGuids_ProduceMedium()
        {
            var result = Matcher.Assess(DefaultPrevious("guid-1"), DefaultCurrent("guid-2"));

            Assert.Equal(ClashMatchConfidence.Medium, result!.Confidence);
        }

        [Fact]
        public void Assess_MissingPreviousGuid_ProducesMedium()
        {
            var result = Matcher.Assess(DefaultPrevious(null), DefaultCurrent("guid-1"));

            Assert.Equal(ClashMatchConfidence.Medium, result!.Confidence);
        }

        [Fact]
        public void Assess_MissingCurrentGuid_ProducesMedium()
        {
            var result = Matcher.Assess(DefaultPrevious("guid-1"), DefaultCurrent(null));

            Assert.Equal(ClashMatchConfidence.Medium, result!.Confidence);
        }

        [Fact]
        public void Assess_BothGuidsMissing_ProducesMedium()
        {
            var result = Matcher.Assess(DefaultPrevious(null), DefaultCurrent(null));

            Assert.Equal(ClashMatchConfidence.Medium, result!.Confidence);
        }

        [Fact]
        public void Assess_EqualGuidAlone_DoesNotOverrideDifferentClashTestName()
        {
            var previous = MakeOccurrence("HVAC vs Structure", SigmaStructurePrevious, AlfaHvacPrevious, "elem-A", "elem-B", "same-guid");
            var current = MakeOccurrence("Electrical vs Structure", SigmaStructureCurrent, AlfaHvacCurrent, "elem-A", "elem-B", "same-guid");

            Assert.Null(Matcher.Assess(previous, current));
        }

        [Fact]
        public void Assess_EqualGuidAlone_DoesNotOverrideDifferentModelPair()
        {
            var differentModel = MakeRevision("Beta", "Architecture", "Main", "R01");
            var previous = MakeOccurrence("Test 1", SigmaStructurePrevious, AlfaHvacPrevious, "elem-A", "elem-B", "same-guid");
            var current = MakeOccurrence("Test 1", SigmaStructureCurrent, differentModel, "elem-A", "elem-B", "same-guid");

            Assert.Null(Matcher.Assess(previous, current));
        }

        [Fact]
        public void Assess_EqualGuidAlone_DoesNotOverrideDifferentElementPair()
        {
            var previous = MakeOccurrence("Test 1", SigmaStructurePrevious, AlfaHvacPrevious, "elem-A1", "elem-B", "same-guid");
            var current = MakeOccurrence("Test 1", SigmaStructureCurrent, AlfaHvacCurrent, "elem-A2", "elem-B", "same-guid");

            Assert.Null(Matcher.Assess(previous, current));
        }

        [Fact]
        public void Assess_NeverProducesLowConfidence()
        {
            var scenarios = new[]
            {
                Matcher.Assess(DefaultPrevious("guid-1"), DefaultCurrent("guid-1")),
                Matcher.Assess(DefaultPrevious("guid-1"), DefaultCurrent("guid-2")),
                Matcher.Assess(DefaultPrevious(null), DefaultCurrent(null)),
            };

            Assert.All(scenarios, s => Assert.NotEqual(ClashMatchConfidence.Low, s!.Confidence));
        }

        // --- 15.7 Evidence ---

        [Fact]
        public void Assess_ContainsExactlyFourEvidenceItems()
        {
            var result = Matcher.Assess(DefaultPrevious(), DefaultCurrent());

            Assert.Equal(4, result!.Evidence.Count);
        }

        [Fact]
        public void Assess_EvidenceOrderIsClashTestNameThenModelIdentityThenElementIdentifierThenGuid()
        {
            var result = Matcher.Assess(DefaultPrevious(), DefaultCurrent());

            Assert.Equal(MatchEvidenceKind.ClashTestName, result!.Evidence[0].Kind);
            Assert.Equal(MatchEvidenceKind.ModelIdentityPair, result.Evidence[1].Kind);
            Assert.Equal(MatchEvidenceKind.ElementIdentifierPair, result.Evidence[2].Kind);
            Assert.Equal(MatchEvidenceKind.SourceClashGuid, result.Evidence[3].Kind);
        }

        [Fact]
        public void Assess_ThreeRequiredEvidenceItemsSupportTheCandidate()
        {
            var result = Matcher.Assess(DefaultPrevious(), DefaultCurrent());

            Assert.Equal(MatchEvidenceVerdict.Supports, result!.Evidence[0].Verdict);
            Assert.Equal(MatchEvidenceVerdict.Supports, result.Evidence[1].Verdict);
            Assert.Equal(MatchEvidenceVerdict.Supports, result.Evidence[2].Verdict);
        }

        [Fact]
        public void Assess_ElementIdentifierEvidence_PreservesOriginalABOrder()
        {
            var previous = MakeOccurrence("Test 1", SigmaStructurePrevious, AlfaHvacPrevious, "elem-S", "elem-H");
            var current = MakeOccurrence("Test 1", AlfaHvacCurrent, SigmaStructureCurrent, "elem-H", "elem-S");

            var result = Matcher.Assess(previous, current);

            Assert.Equal("elem-S | elem-H", result!.Evidence[2].PreviousValue);
            Assert.Equal("elem-H | elem-S", result.Evidence[2].CurrentValue);
        }

        [Fact]
        public void Assess_ModelIdentityEvidenceDescription_IndicatesDirect_WhenAligned()
        {
            var result = Matcher.Assess(DefaultPrevious(), DefaultCurrent());

            Assert.Contains("direct", result!.Evidence[1].Description, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public void Assess_ModelIdentityEvidenceDescription_IndicatesSwapped_WhenInverted()
        {
            var previous = MakeOccurrence("Test 1", SigmaStructurePrevious, AlfaHvacPrevious, "elem-S", "elem-H");
            var current = MakeOccurrence("Test 1", AlfaHvacCurrent, SigmaStructureCurrent, "elem-H", "elem-S");

            var result = Matcher.Assess(previous, current);

            Assert.Contains("swapped", result!.Evidence[1].Description, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public void Assess_SelfClashSwapped_IsDescribedWithoutAssertingASingleOrientation()
        {
            var selfModelPrevious = MakeRevision("Sigma", "Structure", "Main", "R03");
            var selfModelCurrent = MakeRevision("Sigma", "Structure", "Main", "R04");
            var previous = MakeOccurrence("Test 1", selfModelPrevious, selfModelPrevious, "elem-1", "elem-2");
            var current = MakeOccurrence("Test 1", selfModelCurrent, selfModelCurrent, "elem-2", "elem-1");

            var result = Matcher.Assess(previous, current);

            Assert.Contains("ambiguous", result!.Evidence[1].Description, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("ambiguous", result.Evidence[2].Description, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("direct", result.Evidence[1].Description, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("swapped", result.Evidence[1].Description, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public void Assess_GuidEvidence_IncludesNormalizedValues()
        {
            var previous = MakeOccurrence("Test 1", SigmaStructurePrevious, AlfaHvacPrevious, "elem-A", "elem-B", "  guid-1  ");
            var current = MakeOccurrence("Test 1", SigmaStructureCurrent, AlfaHvacCurrent, "elem-A", "elem-B", "guid-1");

            var result = Matcher.Assess(previous, current);

            Assert.Equal("guid-1", result!.Evidence[3].PreviousValue);
            Assert.Equal("guid-1", result.Evidence[3].CurrentValue);
        }

        [Fact]
        public void Assess_GuidEvidence_DocumentsThatItIsNotStableIdentity()
        {
            var result = Matcher.Assess(DefaultPrevious(), DefaultCurrent());

            string description = result!.Evidence[3].Description;
            Assert.Contains("supplemental", description, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("not been proven", description, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public void Assess_DoesNotProduceSpatialPositionEvidence()
        {
            var result = Matcher.Assess(DefaultPrevious(), DefaultCurrent());

            Assert.DoesNotContain(result!.Evidence, e => e.Kind == MatchEvidenceKind.SpatialPosition);
        }

        [Fact]
        public void Assess_DoesNotProduceElementMetadataEvidence()
        {
            var result = Matcher.Assess(DefaultPrevious(), DefaultCurrent());

            Assert.DoesNotContain(result!.Evidence, e => e.Kind == MatchEvidenceKind.ElementMetadata);
        }

        // --- 15.8 Determinism ---

        [Fact]
        public void Assess_IsDeterministic_ForTheSameInputs()
        {
            var previous = DefaultPrevious();
            var current = DefaultCurrent();

            var first = Matcher.Assess(previous, current);
            var second = Matcher.Assess(previous, current);

            Assert.Equal(first!.Confidence, second!.Confidence);
            Assert.Equal(first.Evidence.Count, second.Evidence.Count);

            for (int i = 0; i < first.Evidence.Count; i++)
            {
                Assert.Equal(first.Evidence[i].Kind, second.Evidence[i].Kind);
                Assert.Equal(first.Evidence[i].Verdict, second.Evidence[i].Verdict);
                Assert.Equal(first.Evidence[i].Description, second.Evidence[i].Description);
                Assert.Equal(first.Evidence[i].PreviousValue, second.Evidence[i].PreviousValue);
                Assert.Equal(first.Evidence[i].CurrentValue, second.Evidence[i].CurrentValue);
            }
        }
    }
}
