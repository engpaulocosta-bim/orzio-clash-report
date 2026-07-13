using System;
using System.Collections.Generic;
using OrzioClashReport.Core.Model;

namespace OrzioClashReport.Tests
{
    /// <summary>Unit tests for RunManifest: the Core's immutable, validated declaration of a coordination run.</summary>
    public class RunManifestTests
    {
        private static ModelRevision MakeRevision(
            string company, string discipline, string modelName, string revision, string sourceFileName) =>
            new ModelRevision(
                new ModelIdentity(company, discipline, modelName), revision, sourceFileName, null, null, null);

        private static ExecutedClashTest TestFor(string name, ModelRevision modelA, ModelRevision modelB) =>
            new ExecutedClashTest(name, modelA.Identity, modelB.Identity);

        private static readonly DateTimeOffset SampleCreatedAt = new DateTimeOffset(2026, 7, 10, 9, 0, 0, TimeSpan.FromHours(1));

        private static readonly ExecutedClashTest[] NoTests = Array.Empty<ExecutedClashTest>();

        [Fact]
        public void Constructor_RejectsNullRunId()
        {
            var models = new[] { MakeRevision("Sigma", "Structure", "Sigma_Structure", "R04", "Sigma_Structure_R04.nwc") };

            Assert.Throws<ArgumentNullException>(() => new RunManifest(null!, SampleCreatedAt, models, NoTests));
        }

        [Theory]
        [InlineData("")]
        [InlineData("   ")]
        public void Constructor_RejectsEmptyOrWhitespaceRunId(string runId)
        {
            var models = new[] { MakeRevision("Sigma", "Structure", "Sigma_Structure", "R04", "Sigma_Structure_R04.nwc") };

            Assert.Throws<ArgumentException>(() => new RunManifest(runId, SampleCreatedAt, models, NoTests));
        }

        [Fact]
        public void Constructor_TrimsRunId()
        {
            var models = new[] { MakeRevision("Sigma", "Structure", "Sigma_Structure", "R04", "Sigma_Structure_R04.nwc") };

            var manifest = new RunManifest("  run-1  ", SampleCreatedAt, models, NoTests);

            Assert.Equal("run-1", manifest.RunId);
        }

        [Fact]
        public void Constructor_RejectsNullModels()
        {
            Assert.Throws<ArgumentNullException>(() => new RunManifest("run-1", SampleCreatedAt, null!, NoTests));
        }

        [Fact]
        public void Constructor_RejectsEmptyModelsList()
        {
            Assert.Throws<ArgumentException>(() => new RunManifest("run-1", SampleCreatedAt, Array.Empty<ModelRevision>(), NoTests));
        }

        [Fact]
        public void Constructor_RejectsNullItemInModels()
        {
            var models = new ModelRevision?[]
            {
                MakeRevision("Sigma", "Structure", "Sigma_Structure", "R04", "Sigma_Structure_R04.nwc"),
                null,
            };

            Assert.Throws<ArgumentException>(() => new RunManifest("run-1", SampleCreatedAt, models!, NoTests));
        }

        [Fact]
        public void Constructor_DefensivelyCopiesModelsList()
        {
            var source = new List<ModelRevision>
            {
                MakeRevision("Sigma", "Structure", "Sigma_Structure", "R04", "Sigma_Structure_R04.nwc"),
            };

            var manifest = new RunManifest("run-1", SampleCreatedAt, source, NoTests);
            source.Clear();

            Assert.Single(manifest.Models);
        }

        [Fact]
        public void Constructor_PreservesDeclaredOrderOfModels()
        {
            var third = MakeRevision("Gamma", "MEP", "Gamma_MEP", "R01", "Gamma_MEP_R01.nwc");
            var first = MakeRevision("Sigma", "Structure", "Sigma_Structure", "R04", "Sigma_Structure_R04.nwc");
            var second = MakeRevision("Alfa", "Piping", "Alfa_Piping", "R04", "Alfa_Piping_R04.nwc");

            var manifest = new RunManifest("run-1", SampleCreatedAt, new[] { third, first, second }, NoTests);

            Assert.Same(third, manifest.Models[0]);
            Assert.Same(first, manifest.Models[1]);
            Assert.Same(second, manifest.Models[2]);
        }

        [Fact]
        public void Constructor_RejectsDuplicateModelIdentityIgnoringCase()
        {
            var models = new[]
            {
                MakeRevision("Sigma", "Structure", "Main", "R03", "Main_R03.nwc"),
                MakeRevision("sigma", "structure", "main", "R04", "Main_R04.nwc"),
            };

            Assert.Throws<ArgumentException>(() => new RunManifest("run-1", SampleCreatedAt, models, NoTests));
        }

        [Fact]
        public void Constructor_AcceptsDifferentIdentities()
        {
            var models = new[]
            {
                MakeRevision("Sigma", "Structure", "Sigma_Structure", "R04", "Sigma_Structure_R04.nwc"),
                MakeRevision("Alfa", "Piping", "Alfa_Piping", "R04", "Alfa_Piping_R04.nwc"),
            };

            var manifest = new RunManifest("run-1", SampleCreatedAt, models, NoTests);

            Assert.Equal(2, manifest.Models.Count);
        }

        [Fact]
        public void ToString_IsHumanReadable()
        {
            var models = new[] { MakeRevision("Sigma", "Structure", "Sigma_Structure", "R04", "Sigma_Structure_R04.nwc") };
            var manifest = new RunManifest("run-1", SampleCreatedAt, models, NoTests);

            Assert.Equal($"run-1 @ {SampleCreatedAt:O} (1 models, 0 executed clash tests)", manifest.ToString());
        }

        // ===================== ExecutedClashTests: null/empty/copy/order =====================

        [Fact]
        public void Constructor_RejectsNullExecutedClashTests()
        {
            var models = new[] { MakeRevision("Sigma", "Structure", "Sigma_Structure", "R04", "Sigma_Structure_R04.nwc") };

            Assert.Throws<ArgumentNullException>(() => new RunManifest("run-1", SampleCreatedAt, models, null!));
        }

        [Fact]
        public void Constructor_AcceptsEmptyExecutedClashTestsList()
        {
            var models = new[] { MakeRevision("Sigma", "Structure", "Sigma_Structure", "R04", "Sigma_Structure_R04.nwc") };

            var manifest = new RunManifest("run-1", SampleCreatedAt, models, NoTests);

            Assert.Empty(manifest.ExecutedClashTests);
        }

        [Fact]
        public void Constructor_RejectsNullItemInExecutedClashTests()
        {
            var sigma = MakeRevision("Sigma", "Structure", "Sigma_Structure", "R04", "Sigma_Structure_R04.nwc");
            var models = new[] { sigma };
            var tests = new ExecutedClashTest?[] { TestFor("Test 1", sigma, sigma), null };

            Assert.Throws<ArgumentException>(() => new RunManifest("run-1", SampleCreatedAt, models, tests!));
        }

        [Fact]
        public void Constructor_DefensivelyCopiesExecutedClashTestsList()
        {
            var sigma = MakeRevision("Sigma", "Structure", "Sigma_Structure", "R04", "Sigma_Structure_R04.nwc");
            var models = new[] { sigma };
            var source = new List<ExecutedClashTest> { TestFor("Test 1", sigma, sigma) };

            var manifest = new RunManifest("run-1", SampleCreatedAt, models, source);
            source.Clear();

            Assert.Single(manifest.ExecutedClashTests);
        }

        [Fact]
        public void Constructor_PreservesDeclaredOrderOfExecutedClashTests()
        {
            var sigma = MakeRevision("Sigma", "Structure", "Sigma_Structure", "R04", "Sigma_Structure_R04.nwc");
            var alfa = MakeRevision("Alfa", "HVAC", "Alfa_HVAC", "R07", "Alfa_HVAC_R07.nwc");
            var models = new[] { sigma, alfa };
            var second = TestFor("Test B", sigma, alfa);
            var first = TestFor("Test A", sigma, alfa);

            var manifest = new RunManifest("run-1", SampleCreatedAt, models, new[] { second, first });

            Assert.Same(second, manifest.ExecutedClashTests[0]);
            Assert.Same(first, manifest.ExecutedClashTests[1]);
        }

        // ===================== ExecutedClashTests: model references =====================

        [Fact]
        public void Constructor_RejectsExecutedClashTest_WithModelANotDeclared()
        {
            var sigma = MakeRevision("Sigma", "Structure", "Sigma_Structure", "R04", "Sigma_Structure_R04.nwc");
            var alfa = MakeRevision("Alfa", "HVAC", "Alfa_HVAC", "R07", "Alfa_HVAC_R07.nwc");
            var models = new[] { sigma };

            Assert.Throws<ArgumentException>(() => new RunManifest("run-1", SampleCreatedAt, models, new[] { TestFor("Test 1", alfa, sigma) }));
        }

        [Fact]
        public void Constructor_RejectsExecutedClashTest_WithModelBNotDeclared()
        {
            var sigma = MakeRevision("Sigma", "Structure", "Sigma_Structure", "R04", "Sigma_Structure_R04.nwc");
            var alfa = MakeRevision("Alfa", "HVAC", "Alfa_HVAC", "R07", "Alfa_HVAC_R07.nwc");
            var models = new[] { sigma };

            Assert.Throws<ArgumentException>(() => new RunManifest("run-1", SampleCreatedAt, models, new[] { TestFor("Test 1", sigma, alfa) }));
        }

        [Fact]
        public void Constructor_AcceptsExecutedClashTest_WithEquivalentIdentity_DifferingOnlyByCase()
        {
            var sigma = MakeRevision("Sigma", "Structure", "Sigma_Structure", "R04", "Sigma_Structure_R04.nwc");
            var models = new[] { sigma };
            var caseVariant = new ExecutedClashTest(
                "Test 1", new ModelIdentity("sigma", "structure", "sigma_structure"), new ModelIdentity("SIGMA", "STRUCTURE", "SIGMA_STRUCTURE"));

            var manifest = new RunManifest("run-1", SampleCreatedAt, models, new[] { caseVariant });

            Assert.Single(manifest.ExecutedClashTests);
        }

        [Fact]
        public void Constructor_ExecutedClashTest_IsRevisionFree_DoesNotParticipateInDeclaredModelRevision()
        {
            var sigmaR04 = MakeRevision("Sigma", "Structure", "Sigma_Structure", "R04", "Sigma_Structure_R04.nwc");
            var models = new[] { sigmaR04 };
            var identityOnly = new ExecutedClashTest("Test 1", sigmaR04.Identity, sigmaR04.Identity);

            var manifest = new RunManifest("run-1", SampleCreatedAt, models, new[] { identityOnly });

            Assert.Single(manifest.ExecutedClashTests);
        }

        // ===================== ExecutedClashTests: duplicates =====================

        [Fact]
        public void Constructor_RejectsDuplicateExecutedClashTest_SameNameAndDirectPair()
        {
            var sigma = MakeRevision("Sigma", "Structure", "Sigma_Structure", "R04", "Sigma_Structure_R04.nwc");
            var alfa = MakeRevision("Alfa", "HVAC", "Alfa_HVAC", "R07", "Alfa_HVAC_R07.nwc");
            var models = new[] { sigma, alfa };
            var tests = new[]
            {
                TestFor("HVAC vs Structure", alfa, sigma),
                TestFor("HVAC vs Structure", alfa, sigma),
            };

            Assert.Throws<ArgumentException>(() => new RunManifest("run-1", SampleCreatedAt, models, tests));
        }

        [Fact]
        public void Constructor_RejectsDuplicateExecutedClashTest_CaseOnlyNameDifference()
        {
            var sigma = MakeRevision("Sigma", "Structure", "Sigma_Structure", "R04", "Sigma_Structure_R04.nwc");
            var alfa = MakeRevision("Alfa", "HVAC", "Alfa_HVAC", "R07", "Alfa_HVAC_R07.nwc");
            var models = new[] { sigma, alfa };
            var tests = new[]
            {
                TestFor("HVAC vs Structure", alfa, sigma),
                TestFor("hvac VS structure", alfa, sigma),
            };

            Assert.Throws<ArgumentException>(() => new RunManifest("run-1", SampleCreatedAt, models, tests));
        }

        [Fact]
        public void Constructor_RejectsDuplicateExecutedClashTest_AbInverted()
        {
            var sigma = MakeRevision("Sigma", "Structure", "Sigma_Structure", "R04", "Sigma_Structure_R04.nwc");
            var alfa = MakeRevision("Alfa", "HVAC", "Alfa_HVAC", "R07", "Alfa_HVAC_R07.nwc");
            var models = new[] { sigma, alfa };
            var tests = new[]
            {
                TestFor("HVAC vs Structure", sigma, alfa),
                TestFor("HVAC vs Structure", alfa, sigma),
            };

            Assert.Throws<ArgumentException>(() => new RunManifest("run-1", SampleCreatedAt, models, tests));
        }

        [Fact]
        public void Constructor_RejectsDuplicateExecutedClashTest_SelfClash()
        {
            var sigma = MakeRevision("Sigma", "Structure", "Sigma_Structure", "R04", "Sigma_Structure_R04.nwc");
            var models = new[] { sigma };
            var tests = new[]
            {
                TestFor("Self Clash", sigma, sigma),
                TestFor("Self Clash", sigma, sigma),
            };

            Assert.Throws<ArgumentException>(() => new RunManifest("run-1", SampleCreatedAt, models, tests));
        }

        [Fact]
        public void Constructor_AcceptsSameName_WithDifferentModelPair()
        {
            var sigma = MakeRevision("Sigma", "Structure", "Sigma_Structure", "R04", "Sigma_Structure_R04.nwc");
            var alfa = MakeRevision("Alfa", "HVAC", "Alfa_HVAC", "R07", "Alfa_HVAC_R07.nwc");
            var beta = MakeRevision("Beta", "Architecture", "Beta_Architecture", "R10", "Beta_Architecture_R10.nwc");
            var models = new[] { sigma, alfa, beta };
            var tests = new[]
            {
                TestFor("Coordination", sigma, alfa),
                TestFor("Coordination", sigma, beta),
            };

            var manifest = new RunManifest("run-1", SampleCreatedAt, models, tests);

            Assert.Equal(2, manifest.ExecutedClashTests.Count);
        }

        [Fact]
        public void Constructor_AcceptsSamePair_WithDifferentName()
        {
            var sigma = MakeRevision("Sigma", "Structure", "Sigma_Structure", "R04", "Sigma_Structure_R04.nwc");
            var alfa = MakeRevision("Alfa", "HVAC", "Alfa_HVAC", "R07", "Alfa_HVAC_R07.nwc");
            var models = new[] { sigma, alfa };
            var tests = new[]
            {
                TestFor("HVAC vs Structure - Round 1", sigma, alfa),
                TestFor("HVAC vs Structure - Round 2", sigma, alfa),
            };

            var manifest = new RunManifest("run-1", SampleCreatedAt, models, tests);

            Assert.Equal(2, manifest.ExecutedClashTests.Count);
        }

        [Fact]
        public void ToString_IncludesExecutedClashTestCount()
        {
            var sigma = MakeRevision("Sigma", "Structure", "Sigma_Structure", "R04", "Sigma_Structure_R04.nwc");
            var alfa = MakeRevision("Alfa", "HVAC", "Alfa_HVAC", "R07", "Alfa_HVAC_R07.nwc");
            var models = new[] { sigma, alfa };

            var manifest = new RunManifest("run-1", SampleCreatedAt, models, new[] { TestFor("Test 1", sigma, alfa) });

            Assert.Contains("1 executed clash tests", manifest.ToString());
        }
    }
}
