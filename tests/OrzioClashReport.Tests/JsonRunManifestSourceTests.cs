using System;
using System.IO;
using OrzioClashReport.Input.RunManifestJson;

namespace OrzioClashReport.Tests
{
    /// <summary>Integration tests for JsonRunManifestSource: parsing and validating run manifest JSON into a Core RunManifest.</summary>
    public class JsonRunManifestSourceTests : IDisposable
    {
        private readonly string _tempFilePath = Path.Combine(Path.GetTempPath(), $"orzioclash-manifest-{Guid.NewGuid():N}.json");

        public void Dispose()
        {
            if (File.Exists(_tempFilePath))
            {
                File.Delete(_tempFilePath);
            }
        }

        private static string ValidSingleModelJson() => """
            {
              "schemaVersion": 1,
              "runId": "run-1",
              "createdAt": "2026-07-10T09:00:00+01:00",
              "models": [
                {
                  "company": "Sigma",
                  "discipline": "Structure",
                  "modelName": "Sigma_Structure",
                  "revision": "R04",
                  "sourceFileName": "Sigma_Structure_R04.nwc"
                }
              ]
            }
            """;

        [Fact]
        public void Parse_LoadsValidManifest()
        {
            var manifest = new JsonRunManifestSource().Parse(ValidSingleModelJson());

            Assert.Equal("run-1", manifest.RunId);
            Assert.Equal(new DateTimeOffset(2026, 7, 10, 9, 0, 0, TimeSpan.FromHours(1)), manifest.CreatedAt);
            var model = Assert.Single(manifest.Models);
            Assert.Equal("Sigma", model.Identity.Company);
            Assert.Equal("R04", model.Revision);
        }

        [Fact]
        public void Load_LoadsSyntheticSampleFixture()
        {
            string samplePath = Path.Combine(AppContext.BaseDirectory, "samples", "run-manifest.sample.json");

            var manifest = new JsonRunManifestSource().Load(samplePath);

            Assert.Equal("coordination-2026-07-10-0900", manifest.RunId);
            Assert.Equal(3, manifest.Models.Count);
            Assert.Equal("Sigma_Structure", manifest.Models[0].Identity.ModelName);
            Assert.Equal("Beta_Architecture", manifest.Models[1].Identity.ModelName);
            Assert.Equal("Alfa_Piping", manifest.Models[2].Identity.ModelName);
        }

        [Fact]
        public void Parse_MissingSchemaVersion_Fails()
        {
            const string json = """
                {
                  "runId": "run-1",
                  "createdAt": "2026-07-10T09:00:00+01:00",
                  "models": [
                    { "company": "Sigma", "discipline": "Structure", "modelName": "Sigma_Structure", "revision": "R04", "sourceFileName": "Sigma_Structure_R04.nwc" }
                  ]
                }
                """;

            var ex = Assert.Throws<RunManifestFormatException>(() => new JsonRunManifestSource().Parse(json));
            Assert.Contains("schemaVersion", ex.Message);
        }

        [Fact]
        public void Parse_UnsupportedSchemaVersion_Fails()
        {
            const string json = """
                {
                  "schemaVersion": 2,
                  "runId": "run-1",
                  "createdAt": "2026-07-10T09:00:00+01:00",
                  "models": [
                    { "company": "Sigma", "discipline": "Structure", "modelName": "Sigma_Structure", "revision": "R04", "sourceFileName": "Sigma_Structure_R04.nwc" }
                  ]
                }
                """;

            var ex = Assert.Throws<RunManifestFormatException>(() => new JsonRunManifestSource().Parse(json));
            Assert.Contains("2", ex.Message);
            Assert.Contains("1", ex.Message);
        }

        [Fact]
        public void Parse_MissingRunId_Fails()
        {
            const string json = """
                {
                  "schemaVersion": 1,
                  "createdAt": "2026-07-10T09:00:00+01:00",
                  "models": [
                    { "company": "Sigma", "discipline": "Structure", "modelName": "Sigma_Structure", "revision": "R04", "sourceFileName": "Sigma_Structure_R04.nwc" }
                  ]
                }
                """;

            var ex = Assert.Throws<RunManifestFormatException>(() => new JsonRunManifestSource().Parse(json));
            Assert.Contains("runId", ex.Message);
        }

        [Fact]
        public void Parse_MissingCreatedAt_Fails()
        {
            const string json = """
                {
                  "schemaVersion": 1,
                  "runId": "run-1",
                  "models": [
                    { "company": "Sigma", "discipline": "Structure", "modelName": "Sigma_Structure", "revision": "R04", "sourceFileName": "Sigma_Structure_R04.nwc" }
                  ]
                }
                """;

            var ex = Assert.Throws<RunManifestFormatException>(() => new JsonRunManifestSource().Parse(json));
            Assert.Contains("createdAt", ex.Message);
        }

        [Fact]
        public void Parse_MissingModels_Fails()
        {
            const string json = """
                {
                  "schemaVersion": 1,
                  "runId": "run-1",
                  "createdAt": "2026-07-10T09:00:00+01:00"
                }
                """;

            var ex = Assert.Throws<RunManifestFormatException>(() => new JsonRunManifestSource().Parse(json));
            Assert.Contains("models", ex.Message);
        }

        [Fact]
        public void Parse_NullModels_Fails()
        {
            const string json = """
                {
                  "schemaVersion": 1,
                  "runId": "run-1",
                  "createdAt": "2026-07-10T09:00:00+01:00",
                  "models": null
                }
                """;

            var ex = Assert.Throws<RunManifestFormatException>(() => new JsonRunManifestSource().Parse(json));
            Assert.Contains("models", ex.Message);
        }

        [Fact]
        public void Parse_EmptyModels_Fails()
        {
            const string json = """
                {
                  "schemaVersion": 1,
                  "runId": "run-1",
                  "createdAt": "2026-07-10T09:00:00+01:00",
                  "models": []
                }
                """;

            Assert.Throws<RunManifestFormatException>(() => new JsonRunManifestSource().Parse(json));
        }

        [Fact]
        public void Parse_NullModelItem_Fails()
        {
            const string json = """
                {
                  "schemaVersion": 1,
                  "runId": "run-1",
                  "createdAt": "2026-07-10T09:00:00+01:00",
                  "models": [ null ]
                }
                """;

            var ex = Assert.Throws<RunManifestFormatException>(() => new JsonRunManifestSource().Parse(json));
            Assert.Contains("index 0", ex.Message);
        }

        [Fact]
        public void Parse_ModelMissingRequiredField_FailsWithIndex()
        {
            const string json = """
                {
                  "schemaVersion": 1,
                  "runId": "run-1",
                  "createdAt": "2026-07-10T09:00:00+01:00",
                  "models": [
                    { "company": "Sigma", "discipline": "Structure", "modelName": "Sigma_Structure", "sourceFileName": "Sigma_Structure_R04.nwc" }
                  ]
                }
                """;

            var ex = Assert.Throws<RunManifestFormatException>(() => new JsonRunManifestSource().Parse(json));
            Assert.Contains("index 0", ex.Message);
            Assert.Contains("revision", ex.Message);
        }

        [Fact]
        public void Parse_UnknownRootProperty_Fails()
        {
            const string json = """
                {
                  "schemaVersion": 1,
                  "runId": "run-1",
                  "createdAt": "2026-07-10T09:00:00+01:00",
                  "unexpected": true,
                  "models": [
                    { "company": "Sigma", "discipline": "Structure", "modelName": "Sigma_Structure", "revision": "R04", "sourceFileName": "Sigma_Structure_R04.nwc" }
                  ]
                }
                """;

            Assert.Throws<RunManifestFormatException>(() => new JsonRunManifestSource().Parse(json));
        }

        [Fact]
        public void Parse_UnknownModelProperty_Fails()
        {
            const string json = """
                {
                  "schemaVersion": 1,
                  "runId": "run-1",
                  "createdAt": "2026-07-10T09:00:00+01:00",
                  "models": [
                    { "company": "Sigma", "discipline": "Structure", "modelName": "Sigma_Structure", "revision": "R04", "sourceFileName": "Sigma_Structure_R04.nwc", "revison": "typo" }
                  ]
                }
                """;

            Assert.Throws<RunManifestFormatException>(() => new JsonRunManifestSource().Parse(json));
        }

        [Fact]
        public void Parse_MalformedJson_FailsWithContext()
        {
            const string json = "{ \"schemaVersion\": 1, \"runId\": ";

            var ex = Assert.Throws<RunManifestFormatException>(() => new JsonRunManifestSource().Parse(json));
            Assert.Contains("line", ex.Message, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public void Parse_CreatedAtWithoutOffset_Fails()
        {
            const string json = """
                {
                  "schemaVersion": 1,
                  "runId": "run-1",
                  "createdAt": "2026-07-10T09:00:00",
                  "models": [
                    { "company": "Sigma", "discipline": "Structure", "modelName": "Sigma_Structure", "revision": "R04", "sourceFileName": "Sigma_Structure_R04.nwc" }
                  ]
                }
                """;

            Assert.Throws<RunManifestFormatException>(() => new JsonRunManifestSource().Parse(json));
        }

        [Fact]
        public void Parse_PublishedAtWithoutOffset_Fails()
        {
            const string json = """
                {
                  "schemaVersion": 1,
                  "runId": "run-1",
                  "createdAt": "2026-07-10T09:00:00+01:00",
                  "models": [
                    { "company": "Sigma", "discipline": "Structure", "modelName": "Sigma_Structure", "revision": "R04", "sourceFileName": "Sigma_Structure_R04.nwc", "publishedAt": "2026-07-10T09:00:00" }
                  ]
                }
                """;

            Assert.Throws<RunManifestFormatException>(() => new JsonRunManifestSource().Parse(json));
        }

        [Fact]
        public void Parse_CreatedAtWithZ_Works()
        {
            const string json = """
                {
                  "schemaVersion": 1,
                  "runId": "run-1",
                  "createdAt": "2026-07-10T08:00:00Z",
                  "models": [
                    { "company": "Sigma", "discipline": "Structure", "modelName": "Sigma_Structure", "revision": "R04", "sourceFileName": "Sigma_Structure_R04.nwc" }
                  ]
                }
                """;

            var manifest = new JsonRunManifestSource().Parse(json);

            Assert.Equal(TimeSpan.Zero, manifest.CreatedAt.Offset);
        }

        [Fact]
        public void Parse_CreatedAtWithExplicitOffset_Works()
        {
            var manifest = new JsonRunManifestSource().Parse(ValidSingleModelJson());

            Assert.Equal(TimeSpan.FromHours(1), manifest.CreatedAt.Offset);
        }

        [Fact]
        public void Parse_MissingOptionalFields_BecomeNull()
        {
            var manifest = new JsonRunManifestSource().Parse(ValidSingleModelJson());

            var model = Assert.Single(manifest.Models);
            Assert.Null(model.SourceFilePath);
            Assert.Null(model.ContentHash);
            Assert.Null(model.PublishedAt);
        }

        [Fact]
        public void Parse_WhitespaceOptionalFields_BecomeNullViaModelRevisionNormalization()
        {
            const string json = """
                {
                  "schemaVersion": 1,
                  "runId": "run-1",
                  "createdAt": "2026-07-10T09:00:00+01:00",
                  "models": [
                    { "company": "Sigma", "discipline": "Structure", "modelName": "Sigma_Structure", "revision": "R04", "sourceFileName": "Sigma_Structure_R04.nwc", "sourceFilePath": "   ", "contentHash": "   " }
                  ]
                }
                """;

            var manifest = new JsonRunManifestSource().Parse(json);

            var model = Assert.Single(manifest.Models);
            Assert.Null(model.SourceFilePath);
            Assert.Null(model.ContentHash);
        }

        [Fact]
        public void Parse_DuplicateIdentityDifferingOnlyByCase_Fails()
        {
            const string json = """
                {
                  "schemaVersion": 1,
                  "runId": "run-1",
                  "createdAt": "2026-07-10T09:00:00+01:00",
                  "models": [
                    { "company": "Sigma", "discipline": "Structure", "modelName": "Main", "revision": "R03", "sourceFileName": "Main_R03.nwc" },
                    { "company": "sigma", "discipline": "structure", "modelName": "main", "revision": "R04", "sourceFileName": "Main_R04.nwc" }
                  ]
                }
                """;

            Assert.Throws<RunManifestFormatException>(() => new JsonRunManifestSource().Parse(json));
        }

        [Fact]
        public void Parse_RevisionRemainsOpaque_NeverInferredFromSourceFileName()
        {
            const string json = """
                {
                  "schemaVersion": 1,
                  "runId": "run-1",
                  "createdAt": "2026-07-10T09:00:00+01:00",
                  "models": [
                    { "company": "Sigma", "discipline": "Structure", "modelName": "Sigma_Structure", "revision": "ISSUE-A", "sourceFileName": "Sigma_Structure_R99.nwc" }
                  ]
                }
                """;

            var manifest = new JsonRunManifestSource().Parse(json);

            var model = Assert.Single(manifest.Models);
            Assert.Equal("ISSUE-A", model.Revision);
            Assert.Equal("Sigma_Structure_R99.nwc", model.SourceFileName);
        }

        [Fact]
        public void Parse_SourceFileNameWithoutExtension_IsAccepted()
        {
            const string json = """
                {
                  "schemaVersion": 1,
                  "runId": "run-1",
                  "createdAt": "2026-07-10T09:00:00+01:00",
                  "models": [
                    { "company": "Sigma", "discipline": "Structure", "modelName": "Sigma_Structure", "revision": "R04", "sourceFileName": "Sigma_Structure_R04" }
                  ]
                }
                """;

            var manifest = new JsonRunManifestSource().Parse(json);

            Assert.Equal("Sigma_Structure_R04", Assert.Single(manifest.Models).SourceFileName);
        }

        [Fact]
        public void Load_RejectsEmptyPath()
        {
            Assert.Throws<RunManifestFormatException>(() => new JsonRunManifestSource().Load("   "));
        }

        [Fact]
        public void Load_ReportsMissingFile()
        {
            string missingPath = Path.Combine(Path.GetTempPath(), $"orzioclash-missing-{Guid.NewGuid():N}.json");

            var ex = Assert.Throws<RunManifestFormatException>(() => new JsonRunManifestSource().Load(missingPath));
            Assert.Contains(missingPath, ex.Message);
        }

        [Fact]
        public void Load_ReadsFileFromDisk()
        {
            File.WriteAllText(_tempFilePath, ValidSingleModelJson());

            var manifest = new JsonRunManifestSource().Load(_tempFilePath);

            Assert.Equal("run-1", manifest.RunId);
        }

        [Fact]
        public void Parse_RejectsEmptyJson()
        {
            Assert.Throws<RunManifestFormatException>(() => new JsonRunManifestSource().Parse("   "));
        }

        // --- Duplicate JSON property rejection ---

        [Fact]
        public void Parse_DuplicateSchemaVersionAtRoot_Fails()
        {
            const string json = """
                {
                  "schemaVersion": 1,
                  "schemaVersion": 1,
                  "runId": "run-1",
                  "createdAt": "2026-07-10T09:00:00+01:00",
                  "models": [
                    { "company": "Sigma", "discipline": "Structure", "modelName": "Sigma_Structure", "revision": "R04", "sourceFileName": "Sigma_Structure_R04.nwc" }
                  ]
                }
                """;

            var ex = Assert.Throws<RunManifestFormatException>(() => new JsonRunManifestSource().Parse(json));
            Assert.Contains("Duplicate", ex.Message);
            Assert.Contains("schemaVersion", ex.Message);
            Assert.Contains("root", ex.Message);
        }

        [Fact]
        public void Parse_DuplicateRunIdAtRoot_Fails()
        {
            const string json = """
                {
                  "schemaVersion": 1,
                  "runId": "run-1",
                  "runId": "run-2",
                  "createdAt": "2026-07-10T09:00:00+01:00",
                  "models": [
                    { "company": "Sigma", "discipline": "Structure", "modelName": "Sigma_Structure", "revision": "R04", "sourceFileName": "Sigma_Structure_R04.nwc" }
                  ]
                }
                """;

            var ex = Assert.Throws<RunManifestFormatException>(() => new JsonRunManifestSource().Parse(json));
            Assert.Contains("Duplicate", ex.Message);
            Assert.Contains("runId", ex.Message);
            Assert.Contains("root", ex.Message);
        }

        [Fact]
        public void Parse_DuplicateCreatedAtAtRoot_Fails()
        {
            const string json = """
                {
                  "schemaVersion": 1,
                  "runId": "run-1",
                  "createdAt": "2026-07-10T09:00:00+01:00",
                  "createdAt": "2026-07-11T09:00:00+01:00",
                  "models": [
                    { "company": "Sigma", "discipline": "Structure", "modelName": "Sigma_Structure", "revision": "R04", "sourceFileName": "Sigma_Structure_R04.nwc" }
                  ]
                }
                """;

            var ex = Assert.Throws<RunManifestFormatException>(() => new JsonRunManifestSource().Parse(json));
            Assert.Contains("Duplicate", ex.Message);
            Assert.Contains("createdAt", ex.Message);
            Assert.Contains("root", ex.Message);
        }

        [Fact]
        public void Parse_DuplicateModelsAtRoot_Fails()
        {
            const string json = """
                {
                  "schemaVersion": 1,
                  "runId": "run-1",
                  "createdAt": "2026-07-10T09:00:00+01:00",
                  "models": [
                    { "company": "Sigma", "discipline": "Structure", "modelName": "Sigma_Structure", "revision": "R04", "sourceFileName": "Sigma_Structure_R04.nwc" }
                  ],
                  "models": [
                    { "company": "Sigma", "discipline": "Structure", "modelName": "Sigma_Structure", "revision": "R05", "sourceFileName": "Sigma_Structure_R05.nwc" }
                  ]
                }
                """;

            var ex = Assert.Throws<RunManifestFormatException>(() => new JsonRunManifestSource().Parse(json));
            Assert.Contains("Duplicate", ex.Message);
            Assert.Contains("models", ex.Message);
            Assert.Contains("root", ex.Message);
        }

        [Fact]
        public void Parse_DuplicateRevisionInsideModel_Fails()
        {
            const string json = """
                {
                  "schemaVersion": 1,
                  "runId": "run-1",
                  "createdAt": "2026-07-10T09:00:00+01:00",
                  "models": [
                    { "company": "Sigma", "discipline": "Structure", "modelName": "Sigma_Structure", "revision": "R04", "revision": "R05", "sourceFileName": "Sigma_Structure_R04.nwc" }
                  ]
                }
                """;

            var ex = Assert.Throws<RunManifestFormatException>(() => new JsonRunManifestSource().Parse(json));
            Assert.Contains("Duplicate", ex.Message);
            Assert.Contains("revision", ex.Message);
            Assert.Contains("models[0]", ex.Message);
        }

        [Fact]
        public void Parse_DuplicateSourceFileNameInsideModel_Fails()
        {
            const string json = """
                {
                  "schemaVersion": 1,
                  "runId": "run-1",
                  "createdAt": "2026-07-10T09:00:00+01:00",
                  "models": [
                    { "company": "Sigma", "discipline": "Structure", "modelName": "Sigma_Structure", "revision": "R04", "sourceFileName": "Sigma_Structure_R04.nwc", "sourceFileName": "Sigma_Structure_R04b.nwc" }
                  ]
                }
                """;

            var ex = Assert.Throws<RunManifestFormatException>(() => new JsonRunManifestSource().Parse(json));
            Assert.Contains("Duplicate", ex.Message);
            Assert.Contains("sourceFileName", ex.Message);
            Assert.Contains("models[0]", ex.Message);
        }

        [Fact]
        public void Parse_DifferentCaseRunIdKeys_AreNotDuplicates_ButFailAsUnknownProperty()
        {
            const string json = """
                {
                  "schemaVersion": 1,
                  "runId": "run-1",
                  "RunId": "run-1-again",
                  "createdAt": "2026-07-10T09:00:00+01:00",
                  "models": [
                    { "company": "Sigma", "discipline": "Structure", "modelName": "Sigma_Structure", "revision": "R04", "sourceFileName": "Sigma_Structure_R04.nwc" }
                  ]
                }
                """;

            var ex = Assert.Throws<RunManifestFormatException>(() => new JsonRunManifestSource().Parse(json));
            Assert.DoesNotContain("Duplicate", ex.Message);
        }
    }
}
