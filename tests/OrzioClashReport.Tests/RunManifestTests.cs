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

        private static readonly DateTimeOffset SampleCreatedAt = new DateTimeOffset(2026, 7, 10, 9, 0, 0, TimeSpan.FromHours(1));

        [Fact]
        public void Constructor_RejectsNullRunId()
        {
            var models = new[] { MakeRevision("Sigma", "Structure", "Sigma_Structure", "R04", "Sigma_Structure_R04.nwc") };

            Assert.Throws<ArgumentNullException>(() => new RunManifest(null!, SampleCreatedAt, models));
        }

        [Theory]
        [InlineData("")]
        [InlineData("   ")]
        public void Constructor_RejectsEmptyOrWhitespaceRunId(string runId)
        {
            var models = new[] { MakeRevision("Sigma", "Structure", "Sigma_Structure", "R04", "Sigma_Structure_R04.nwc") };

            Assert.Throws<ArgumentException>(() => new RunManifest(runId, SampleCreatedAt, models));
        }

        [Fact]
        public void Constructor_TrimsRunId()
        {
            var models = new[] { MakeRevision("Sigma", "Structure", "Sigma_Structure", "R04", "Sigma_Structure_R04.nwc") };

            var manifest = new RunManifest("  run-1  ", SampleCreatedAt, models);

            Assert.Equal("run-1", manifest.RunId);
        }

        [Fact]
        public void Constructor_RejectsNullModels()
        {
            Assert.Throws<ArgumentNullException>(() => new RunManifest("run-1", SampleCreatedAt, null!));
        }

        [Fact]
        public void Constructor_RejectsEmptyModelsList()
        {
            Assert.Throws<ArgumentException>(() => new RunManifest("run-1", SampleCreatedAt, Array.Empty<ModelRevision>()));
        }

        [Fact]
        public void Constructor_RejectsNullItemInModels()
        {
            var models = new ModelRevision?[]
            {
                MakeRevision("Sigma", "Structure", "Sigma_Structure", "R04", "Sigma_Structure_R04.nwc"),
                null,
            };

            Assert.Throws<ArgumentException>(() => new RunManifest("run-1", SampleCreatedAt, models!));
        }

        [Fact]
        public void Constructor_DefensivelyCopiesModelsList()
        {
            var source = new List<ModelRevision>
            {
                MakeRevision("Sigma", "Structure", "Sigma_Structure", "R04", "Sigma_Structure_R04.nwc"),
            };

            var manifest = new RunManifest("run-1", SampleCreatedAt, source);
            source.Clear();

            Assert.Single(manifest.Models);
        }

        [Fact]
        public void Constructor_PreservesDeclaredOrderOfModels()
        {
            var third = MakeRevision("Gamma", "MEP", "Gamma_MEP", "R01", "Gamma_MEP_R01.nwc");
            var first = MakeRevision("Sigma", "Structure", "Sigma_Structure", "R04", "Sigma_Structure_R04.nwc");
            var second = MakeRevision("Alfa", "Piping", "Alfa_Piping", "R04", "Alfa_Piping_R04.nwc");

            var manifest = new RunManifest("run-1", SampleCreatedAt, new[] { third, first, second });

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

            Assert.Throws<ArgumentException>(() => new RunManifest("run-1", SampleCreatedAt, models));
        }

        [Fact]
        public void Constructor_AcceptsDifferentIdentities()
        {
            var models = new[]
            {
                MakeRevision("Sigma", "Structure", "Sigma_Structure", "R04", "Sigma_Structure_R04.nwc"),
                MakeRevision("Alfa", "Piping", "Alfa_Piping", "R04", "Alfa_Piping_R04.nwc"),
            };

            var manifest = new RunManifest("run-1", SampleCreatedAt, models);

            Assert.Equal(2, manifest.Models.Count);
        }

        [Fact]
        public void ToString_IsHumanReadable()
        {
            var models = new[] { MakeRevision("Sigma", "Structure", "Sigma_Structure", "R04", "Sigma_Structure_R04.nwc") };
            var manifest = new RunManifest("run-1", SampleCreatedAt, models);

            Assert.Equal($"run-1 @ {SampleCreatedAt:O} (1 models)", manifest.ToString());
        }
    }
}
