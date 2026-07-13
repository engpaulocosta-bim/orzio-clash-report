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
              "schemaVersion": 2,
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
              ],
              "executedClashTests": []
            }
            """;

        private static string ValidTwoModelJsonWithTest() => """
            {
              "schemaVersion": 2,
              "runId": "run-1",
              "createdAt": "2026-07-10T09:00:00+01:00",
              "models": [
                { "company": "Sigma", "discipline": "Structure", "modelName": "Sigma_Structure", "revision": "R04", "sourceFileName": "Sigma_Structure_R04.nwc" },
                { "company": "Alfa", "discipline": "HVAC", "modelName": "Alfa_HVAC", "revision": "R07", "sourceFileName": "Alfa_HVAC_R07.nwc" }
              ],
              "executedClashTests": [
                {
                  "name": "HVAC vs Structure",
                  "modelA": { "company": "Sigma", "discipline": "Structure", "modelName": "Sigma_Structure" },
                  "modelB": { "company": "Alfa", "discipline": "HVAC", "modelName": "Alfa_HVAC" }
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
            Assert.Empty(manifest.ExecutedClashTests);
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
            Assert.Equal(3, manifest.ExecutedClashTests.Count);
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
                  ],
                  "executedClashTests": []
                }
                """;

            var ex = Assert.Throws<RunManifestFormatException>(() => new JsonRunManifestSource().Parse(json));
            Assert.Contains("schemaVersion", ex.Message);
        }

        [Fact]
        public void Parse_SchemaVersion1_Fails()
        {
            const string json = """
                {
                  "schemaVersion": 1,
                  "runId": "run-1",
                  "createdAt": "2026-07-10T09:00:00+01:00",
                  "models": [
                    { "company": "Sigma", "discipline": "Structure", "modelName": "Sigma_Structure", "revision": "R04", "sourceFileName": "Sigma_Structure_R04.nwc" }
                  ]
                }
                """;

            var ex = Assert.Throws<RunManifestFormatException>(() => new JsonRunManifestSource().Parse(json));
            Assert.Contains("1", ex.Message);
            Assert.Contains("2", ex.Message);
        }

        [Fact]
        public void Parse_UnsupportedSchemaVersion3_Fails()
        {
            const string json = """
                {
                  "schemaVersion": 3,
                  "runId": "run-1",
                  "createdAt": "2026-07-10T09:00:00+01:00",
                  "models": [
                    { "company": "Sigma", "discipline": "Structure", "modelName": "Sigma_Structure", "revision": "R04", "sourceFileName": "Sigma_Structure_R04.nwc" }
                  ],
                  "executedClashTests": []
                }
                """;

            var ex = Assert.Throws<RunManifestFormatException>(() => new JsonRunManifestSource().Parse(json));
            Assert.Contains("3", ex.Message);
            Assert.Contains("2", ex.Message);
        }

        [Fact]
        public void Parse_MissingRunId_Fails()
        {
            const string json = """
                {
                  "schemaVersion": 2,
                  "createdAt": "2026-07-10T09:00:00+01:00",
                  "models": [
                    { "company": "Sigma", "discipline": "Structure", "modelName": "Sigma_Structure", "revision": "R04", "sourceFileName": "Sigma_Structure_R04.nwc" }
                  ],
                  "executedClashTests": []
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
                  "schemaVersion": 2,
                  "runId": "run-1",
                  "models": [
                    { "company": "Sigma", "discipline": "Structure", "modelName": "Sigma_Structure", "revision": "R04", "sourceFileName": "Sigma_Structure_R04.nwc" }
                  ],
                  "executedClashTests": []
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
                  "schemaVersion": 2,
                  "runId": "run-1",
                  "createdAt": "2026-07-10T09:00:00+01:00",
                  "executedClashTests": []
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
                  "schemaVersion": 2,
                  "runId": "run-1",
                  "createdAt": "2026-07-10T09:00:00+01:00",
                  "models": null,
                  "executedClashTests": []
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
                  "schemaVersion": 2,
                  "runId": "run-1",
                  "createdAt": "2026-07-10T09:00:00+01:00",
                  "models": [],
                  "executedClashTests": []
                }
                """;

            Assert.Throws<RunManifestFormatException>(() => new JsonRunManifestSource().Parse(json));
        }

        [Fact]
        public void Parse_NullModelItem_Fails()
        {
            const string json = """
                {
                  "schemaVersion": 2,
                  "runId": "run-1",
                  "createdAt": "2026-07-10T09:00:00+01:00",
                  "models": [ null ],
                  "executedClashTests": []
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
                  "schemaVersion": 2,
                  "runId": "run-1",
                  "createdAt": "2026-07-10T09:00:00+01:00",
                  "models": [
                    { "company": "Sigma", "discipline": "Structure", "modelName": "Sigma_Structure", "sourceFileName": "Sigma_Structure_R04.nwc" }
                  ],
                  "executedClashTests": []
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
                  "schemaVersion": 2,
                  "runId": "run-1",
                  "createdAt": "2026-07-10T09:00:00+01:00",
                  "unexpected": true,
                  "models": [
                    { "company": "Sigma", "discipline": "Structure", "modelName": "Sigma_Structure", "revision": "R04", "sourceFileName": "Sigma_Structure_R04.nwc" }
                  ],
                  "executedClashTests": []
                }
                """;

            Assert.Throws<RunManifestFormatException>(() => new JsonRunManifestSource().Parse(json));
        }

        [Fact]
        public void Parse_UnknownModelProperty_Fails()
        {
            const string json = """
                {
                  "schemaVersion": 2,
                  "runId": "run-1",
                  "createdAt": "2026-07-10T09:00:00+01:00",
                  "models": [
                    { "company": "Sigma", "discipline": "Structure", "modelName": "Sigma_Structure", "revision": "R04", "sourceFileName": "Sigma_Structure_R04.nwc", "revison": "typo" }
                  ],
                  "executedClashTests": []
                }
                """;

            Assert.Throws<RunManifestFormatException>(() => new JsonRunManifestSource().Parse(json));
        }

        [Fact]
        public void Parse_MalformedJson_FailsWithContext()
        {
            const string json = "{ \"schemaVersion\": 2, \"runId\": ";

            var ex = Assert.Throws<RunManifestFormatException>(() => new JsonRunManifestSource().Parse(json));
            Assert.Contains("line", ex.Message, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public void Parse_CreatedAtWithoutOffset_Fails()
        {
            const string json = """
                {
                  "schemaVersion": 2,
                  "runId": "run-1",
                  "createdAt": "2026-07-10T09:00:00",
                  "models": [
                    { "company": "Sigma", "discipline": "Structure", "modelName": "Sigma_Structure", "revision": "R04", "sourceFileName": "Sigma_Structure_R04.nwc" }
                  ],
                  "executedClashTests": []
                }
                """;

            Assert.Throws<RunManifestFormatException>(() => new JsonRunManifestSource().Parse(json));
        }

        [Fact]
        public void Parse_PublishedAtWithoutOffset_Fails()
        {
            const string json = """
                {
                  "schemaVersion": 2,
                  "runId": "run-1",
                  "createdAt": "2026-07-10T09:00:00+01:00",
                  "models": [
                    { "company": "Sigma", "discipline": "Structure", "modelName": "Sigma_Structure", "revision": "R04", "sourceFileName": "Sigma_Structure_R04.nwc", "publishedAt": "2026-07-10T09:00:00" }
                  ],
                  "executedClashTests": []
                }
                """;

            Assert.Throws<RunManifestFormatException>(() => new JsonRunManifestSource().Parse(json));
        }

        [Fact]
        public void Parse_CreatedAtWithZ_Works()
        {
            const string json = """
                {
                  "schemaVersion": 2,
                  "runId": "run-1",
                  "createdAt": "2026-07-10T08:00:00Z",
                  "models": [
                    { "company": "Sigma", "discipline": "Structure", "modelName": "Sigma_Structure", "revision": "R04", "sourceFileName": "Sigma_Structure_R04.nwc" }
                  ],
                  "executedClashTests": []
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
                  "schemaVersion": 2,
                  "runId": "run-1",
                  "createdAt": "2026-07-10T09:00:00+01:00",
                  "models": [
                    { "company": "Sigma", "discipline": "Structure", "modelName": "Sigma_Structure", "revision": "R04", "sourceFileName": "Sigma_Structure_R04.nwc", "sourceFilePath": "   ", "contentHash": "   " }
                  ],
                  "executedClashTests": []
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
                  "schemaVersion": 2,
                  "runId": "run-1",
                  "createdAt": "2026-07-10T09:00:00+01:00",
                  "models": [
                    { "company": "Sigma", "discipline": "Structure", "modelName": "Main", "revision": "R03", "sourceFileName": "Main_R03.nwc" },
                    { "company": "sigma", "discipline": "structure", "modelName": "main", "revision": "R04", "sourceFileName": "Main_R04.nwc" }
                  ],
                  "executedClashTests": []
                }
                """;

            Assert.Throws<RunManifestFormatException>(() => new JsonRunManifestSource().Parse(json));
        }

        [Fact]
        public void Parse_RevisionRemainsOpaque_NeverInferredFromSourceFileName()
        {
            const string json = """
                {
                  "schemaVersion": 2,
                  "runId": "run-1",
                  "createdAt": "2026-07-10T09:00:00+01:00",
                  "models": [
                    { "company": "Sigma", "discipline": "Structure", "modelName": "Sigma_Structure", "revision": "ISSUE-A", "sourceFileName": "Sigma_Structure_R99.nwc" }
                  ],
                  "executedClashTests": []
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
                  "schemaVersion": 2,
                  "runId": "run-1",
                  "createdAt": "2026-07-10T09:00:00+01:00",
                  "models": [
                    { "company": "Sigma", "discipline": "Structure", "modelName": "Sigma_Structure", "revision": "R04", "sourceFileName": "Sigma_Structure_R04" }
                  ],
                  "executedClashTests": []
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
                  "schemaVersion": 2,
                  "schemaVersion": 2,
                  "runId": "run-1",
                  "createdAt": "2026-07-10T09:00:00+01:00",
                  "models": [
                    { "company": "Sigma", "discipline": "Structure", "modelName": "Sigma_Structure", "revision": "R04", "sourceFileName": "Sigma_Structure_R04.nwc" }
                  ],
                  "executedClashTests": []
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
                  "schemaVersion": 2,
                  "runId": "run-1",
                  "runId": "run-2",
                  "createdAt": "2026-07-10T09:00:00+01:00",
                  "models": [
                    { "company": "Sigma", "discipline": "Structure", "modelName": "Sigma_Structure", "revision": "R04", "sourceFileName": "Sigma_Structure_R04.nwc" }
                  ],
                  "executedClashTests": []
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
                  "schemaVersion": 2,
                  "runId": "run-1",
                  "createdAt": "2026-07-10T09:00:00+01:00",
                  "createdAt": "2026-07-11T09:00:00+01:00",
                  "models": [
                    { "company": "Sigma", "discipline": "Structure", "modelName": "Sigma_Structure", "revision": "R04", "sourceFileName": "Sigma_Structure_R04.nwc" }
                  ],
                  "executedClashTests": []
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
                  "schemaVersion": 2,
                  "runId": "run-1",
                  "createdAt": "2026-07-10T09:00:00+01:00",
                  "models": [
                    { "company": "Sigma", "discipline": "Structure", "modelName": "Sigma_Structure", "revision": "R04", "sourceFileName": "Sigma_Structure_R04.nwc" }
                  ],
                  "models": [
                    { "company": "Sigma", "discipline": "Structure", "modelName": "Sigma_Structure", "revision": "R05", "sourceFileName": "Sigma_Structure_R05.nwc" }
                  ],
                  "executedClashTests": []
                }
                """;

            var ex = Assert.Throws<RunManifestFormatException>(() => new JsonRunManifestSource().Parse(json));
            Assert.Contains("Duplicate", ex.Message);
            Assert.Contains("models", ex.Message);
            Assert.Contains("root", ex.Message);
        }

        [Fact]
        public void Parse_DuplicateExecutedClashTestsAtRoot_Fails()
        {
            const string json = """
                {
                  "schemaVersion": 2,
                  "runId": "run-1",
                  "createdAt": "2026-07-10T09:00:00+01:00",
                  "models": [
                    { "company": "Sigma", "discipline": "Structure", "modelName": "Sigma_Structure", "revision": "R04", "sourceFileName": "Sigma_Structure_R04.nwc" }
                  ],
                  "executedClashTests": [],
                  "executedClashTests": []
                }
                """;

            var ex = Assert.Throws<RunManifestFormatException>(() => new JsonRunManifestSource().Parse(json));
            Assert.Contains("Duplicate", ex.Message);
            Assert.Contains("executedClashTests", ex.Message);
            Assert.Contains("root", ex.Message);
        }

        [Fact]
        public void Parse_DuplicateRevisionInsideModel_Fails()
        {
            const string json = """
                {
                  "schemaVersion": 2,
                  "runId": "run-1",
                  "createdAt": "2026-07-10T09:00:00+01:00",
                  "models": [
                    { "company": "Sigma", "discipline": "Structure", "modelName": "Sigma_Structure", "revision": "R04", "revision": "R05", "sourceFileName": "Sigma_Structure_R04.nwc" }
                  ],
                  "executedClashTests": []
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
                  "schemaVersion": 2,
                  "runId": "run-1",
                  "createdAt": "2026-07-10T09:00:00+01:00",
                  "models": [
                    { "company": "Sigma", "discipline": "Structure", "modelName": "Sigma_Structure", "revision": "R04", "sourceFileName": "Sigma_Structure_R04.nwc", "sourceFileName": "Sigma_Structure_R04b.nwc" }
                  ],
                  "executedClashTests": []
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
                  "schemaVersion": 2,
                  "runId": "run-1",
                  "RunId": "run-1-again",
                  "createdAt": "2026-07-10T09:00:00+01:00",
                  "models": [
                    { "company": "Sigma", "discipline": "Structure", "modelName": "Sigma_Structure", "revision": "R04", "sourceFileName": "Sigma_Structure_R04.nwc" }
                  ],
                  "executedClashTests": []
                }
                """;

            var ex = Assert.Throws<RunManifestFormatException>(() => new JsonRunManifestSource().Parse(json));
            Assert.DoesNotContain("Duplicate", ex.Message);
        }

        // --- executedClashTests: presence and shape ---

        [Fact]
        public void Parse_MissingExecutedClashTests_Fails()
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
            Assert.Contains("executedClashTests", ex.Message);
        }

        [Fact]
        public void Parse_NullExecutedClashTests_Fails()
        {
            const string json = """
                {
                  "schemaVersion": 2,
                  "runId": "run-1",
                  "createdAt": "2026-07-10T09:00:00+01:00",
                  "models": [
                    { "company": "Sigma", "discipline": "Structure", "modelName": "Sigma_Structure", "revision": "R04", "sourceFileName": "Sigma_Structure_R04.nwc" }
                  ],
                  "executedClashTests": null
                }
                """;

            var ex = Assert.Throws<RunManifestFormatException>(() => new JsonRunManifestSource().Parse(json));
            Assert.Contains("executedClashTests", ex.Message);
        }

        [Fact]
        public void Parse_EmptyExecutedClashTestsArray_IsAccepted()
        {
            var manifest = new JsonRunManifestSource().Parse(ValidSingleModelJson());

            Assert.Empty(manifest.ExecutedClashTests);
        }

        [Fact]
        public void Parse_NullExecutedClashTestItem_Fails()
        {
            const string json = """
                {
                  "schemaVersion": 2,
                  "runId": "run-1",
                  "createdAt": "2026-07-10T09:00:00+01:00",
                  "models": [
                    { "company": "Sigma", "discipline": "Structure", "modelName": "Sigma_Structure", "revision": "R04", "sourceFileName": "Sigma_Structure_R04.nwc" }
                  ],
                  "executedClashTests": [ null ]
                }
                """;

            var ex = Assert.Throws<RunManifestFormatException>(() => new JsonRunManifestSource().Parse(json));
            Assert.Contains("executedClashTests[0]", ex.Message);
        }

        [Fact]
        public void Parse_ExecutedClashTestMissingName_Fails()
        {
            const string json = """
                {
                  "schemaVersion": 2,
                  "runId": "run-1",
                  "createdAt": "2026-07-10T09:00:00+01:00",
                  "models": [
                    { "company": "Sigma", "discipline": "Structure", "modelName": "Sigma_Structure", "revision": "R04", "sourceFileName": "Sigma_Structure_R04.nwc" }
                  ],
                  "executedClashTests": [
                    {
                      "modelA": { "company": "Sigma", "discipline": "Structure", "modelName": "Sigma_Structure" },
                      "modelB": { "company": "Sigma", "discipline": "Structure", "modelName": "Sigma_Structure" }
                    }
                  ]
                }
                """;

            var ex = Assert.Throws<RunManifestFormatException>(() => new JsonRunManifestSource().Parse(json));
            Assert.Contains("executedClashTests[0]", ex.Message);
            Assert.Contains("name", ex.Message);
        }

        [Fact]
        public void Parse_ExecutedClashTestNullName_Fails()
        {
            const string json = """
                {
                  "schemaVersion": 2,
                  "runId": "run-1",
                  "createdAt": "2026-07-10T09:00:00+01:00",
                  "models": [
                    { "company": "Sigma", "discipline": "Structure", "modelName": "Sigma_Structure", "revision": "R04", "sourceFileName": "Sigma_Structure_R04.nwc" }
                  ],
                  "executedClashTests": [
                    {
                      "name": null,
                      "modelA": { "company": "Sigma", "discipline": "Structure", "modelName": "Sigma_Structure" },
                      "modelB": { "company": "Sigma", "discipline": "Structure", "modelName": "Sigma_Structure" }
                    }
                  ]
                }
                """;

            var ex = Assert.Throws<RunManifestFormatException>(() => new JsonRunManifestSource().Parse(json));
            Assert.Contains("name", ex.Message);
        }

        [Fact]
        public void Parse_ExecutedClashTestEmptyName_Fails()
        {
            const string json = """
                {
                  "schemaVersion": 2,
                  "runId": "run-1",
                  "createdAt": "2026-07-10T09:00:00+01:00",
                  "models": [
                    { "company": "Sigma", "discipline": "Structure", "modelName": "Sigma_Structure", "revision": "R04", "sourceFileName": "Sigma_Structure_R04.nwc" }
                  ],
                  "executedClashTests": [
                    {
                      "name": "   ",
                      "modelA": { "company": "Sigma", "discipline": "Structure", "modelName": "Sigma_Structure" },
                      "modelB": { "company": "Sigma", "discipline": "Structure", "modelName": "Sigma_Structure" }
                    }
                  ]
                }
                """;

            Assert.Throws<RunManifestFormatException>(() => new JsonRunManifestSource().Parse(json));
        }

        [Fact]
        public void Parse_ExecutedClashTestMissingModelA_Fails()
        {
            const string json = """
                {
                  "schemaVersion": 2,
                  "runId": "run-1",
                  "createdAt": "2026-07-10T09:00:00+01:00",
                  "models": [
                    { "company": "Sigma", "discipline": "Structure", "modelName": "Sigma_Structure", "revision": "R04", "sourceFileName": "Sigma_Structure_R04.nwc" }
                  ],
                  "executedClashTests": [
                    {
                      "name": "Self Clash",
                      "modelB": { "company": "Sigma", "discipline": "Structure", "modelName": "Sigma_Structure" }
                    }
                  ]
                }
                """;

            var ex = Assert.Throws<RunManifestFormatException>(() => new JsonRunManifestSource().Parse(json));
            Assert.Contains("executedClashTests[0].modelA", ex.Message);
        }

        [Fact]
        public void Parse_ExecutedClashTestNullModelA_Fails()
        {
            const string json = """
                {
                  "schemaVersion": 2,
                  "runId": "run-1",
                  "createdAt": "2026-07-10T09:00:00+01:00",
                  "models": [
                    { "company": "Sigma", "discipline": "Structure", "modelName": "Sigma_Structure", "revision": "R04", "sourceFileName": "Sigma_Structure_R04.nwc" }
                  ],
                  "executedClashTests": [
                    {
                      "name": "Self Clash",
                      "modelA": null,
                      "modelB": { "company": "Sigma", "discipline": "Structure", "modelName": "Sigma_Structure" }
                    }
                  ]
                }
                """;

            var ex = Assert.Throws<RunManifestFormatException>(() => new JsonRunManifestSource().Parse(json));
            Assert.Contains("executedClashTests[0].modelA", ex.Message);
        }

        [Fact]
        public void Parse_ExecutedClashTestMissingModelB_Fails()
        {
            const string json = """
                {
                  "schemaVersion": 2,
                  "runId": "run-1",
                  "createdAt": "2026-07-10T09:00:00+01:00",
                  "models": [
                    { "company": "Sigma", "discipline": "Structure", "modelName": "Sigma_Structure", "revision": "R04", "sourceFileName": "Sigma_Structure_R04.nwc" }
                  ],
                  "executedClashTests": [
                    {
                      "name": "Self Clash",
                      "modelA": { "company": "Sigma", "discipline": "Structure", "modelName": "Sigma_Structure" }
                    }
                  ]
                }
                """;

            var ex = Assert.Throws<RunManifestFormatException>(() => new JsonRunManifestSource().Parse(json));
            Assert.Contains("executedClashTests[0].modelB", ex.Message);
        }

        [Fact]
        public void Parse_ExecutedClashTestNullModelB_Fails()
        {
            const string json = """
                {
                  "schemaVersion": 2,
                  "runId": "run-1",
                  "createdAt": "2026-07-10T09:00:00+01:00",
                  "models": [
                    { "company": "Sigma", "discipline": "Structure", "modelName": "Sigma_Structure", "revision": "R04", "sourceFileName": "Sigma_Structure_R04.nwc" }
                  ],
                  "executedClashTests": [
                    {
                      "name": "Self Clash",
                      "modelA": { "company": "Sigma", "discipline": "Structure", "modelName": "Sigma_Structure" },
                      "modelB": null
                    }
                  ]
                }
                """;

            var ex = Assert.Throws<RunManifestFormatException>(() => new JsonRunManifestSource().Parse(json));
            Assert.Contains("executedClashTests[0].modelB", ex.Message);
        }

        [Fact]
        public void Parse_ExecutedClashTestModelA_MissingCompany_Fails()
        {
            const string json = """
                {
                  "schemaVersion": 2,
                  "runId": "run-1",
                  "createdAt": "2026-07-10T09:00:00+01:00",
                  "models": [
                    { "company": "Sigma", "discipline": "Structure", "modelName": "Sigma_Structure", "revision": "R04", "sourceFileName": "Sigma_Structure_R04.nwc" }
                  ],
                  "executedClashTests": [
                    {
                      "name": "Self Clash",
                      "modelA": { "discipline": "Structure", "modelName": "Sigma_Structure" },
                      "modelB": { "company": "Sigma", "discipline": "Structure", "modelName": "Sigma_Structure" }
                    }
                  ]
                }
                """;

            var ex = Assert.Throws<RunManifestFormatException>(() => new JsonRunManifestSource().Parse(json));
            Assert.Contains("executedClashTests[0].modelA", ex.Message);
        }

        [Fact]
        public void Parse_ExecutedClashTestModelA_MissingDiscipline_Fails()
        {
            const string json = """
                {
                  "schemaVersion": 2,
                  "runId": "run-1",
                  "createdAt": "2026-07-10T09:00:00+01:00",
                  "models": [
                    { "company": "Sigma", "discipline": "Structure", "modelName": "Sigma_Structure", "revision": "R04", "sourceFileName": "Sigma_Structure_R04.nwc" }
                  ],
                  "executedClashTests": [
                    {
                      "name": "Self Clash",
                      "modelA": { "company": "Sigma", "modelName": "Sigma_Structure" },
                      "modelB": { "company": "Sigma", "discipline": "Structure", "modelName": "Sigma_Structure" }
                    }
                  ]
                }
                """;

            var ex = Assert.Throws<RunManifestFormatException>(() => new JsonRunManifestSource().Parse(json));
            Assert.Contains("executedClashTests[0].modelA", ex.Message);
        }

        [Fact]
        public void Parse_ExecutedClashTestModelA_MissingModelName_Fails()
        {
            const string json = """
                {
                  "schemaVersion": 2,
                  "runId": "run-1",
                  "createdAt": "2026-07-10T09:00:00+01:00",
                  "models": [
                    { "company": "Sigma", "discipline": "Structure", "modelName": "Sigma_Structure", "revision": "R04", "sourceFileName": "Sigma_Structure_R04.nwc" }
                  ],
                  "executedClashTests": [
                    {
                      "name": "Self Clash",
                      "modelA": { "company": "Sigma", "discipline": "Structure" },
                      "modelB": { "company": "Sigma", "discipline": "Structure", "modelName": "Sigma_Structure" }
                    }
                  ]
                }
                """;

            var ex = Assert.Throws<RunManifestFormatException>(() => new JsonRunManifestSource().Parse(json));
            Assert.Contains("executedClashTests[0].modelA", ex.Message);
        }

        [Fact]
        public void Parse_ExecutedClashTestModelB_MissingCompany_Fails()
        {
            const string json = """
                {
                  "schemaVersion": 2,
                  "runId": "run-1",
                  "createdAt": "2026-07-10T09:00:00+01:00",
                  "models": [
                    { "company": "Sigma", "discipline": "Structure", "modelName": "Sigma_Structure", "revision": "R04", "sourceFileName": "Sigma_Structure_R04.nwc" }
                  ],
                  "executedClashTests": [
                    {
                      "name": "Self Clash",
                      "modelA": { "company": "Sigma", "discipline": "Structure", "modelName": "Sigma_Structure" },
                      "modelB": { "discipline": "Structure", "modelName": "Sigma_Structure" }
                    }
                  ]
                }
                """;

            var ex = Assert.Throws<RunManifestFormatException>(() => new JsonRunManifestSource().Parse(json));
            Assert.Contains("executedClashTests[0].modelB", ex.Message);
        }

        [Fact]
        public void Parse_ExecutedClashTestModelB_MissingDiscipline_Fails()
        {
            const string json = """
                {
                  "schemaVersion": 2,
                  "runId": "run-1",
                  "createdAt": "2026-07-10T09:00:00+01:00",
                  "models": [
                    { "company": "Sigma", "discipline": "Structure", "modelName": "Sigma_Structure", "revision": "R04", "sourceFileName": "Sigma_Structure_R04.nwc" }
                  ],
                  "executedClashTests": [
                    {
                      "name": "Self Clash",
                      "modelA": { "company": "Sigma", "discipline": "Structure", "modelName": "Sigma_Structure" },
                      "modelB": { "company": "Sigma", "modelName": "Sigma_Structure" }
                    }
                  ]
                }
                """;

            var ex = Assert.Throws<RunManifestFormatException>(() => new JsonRunManifestSource().Parse(json));
            Assert.Contains("executedClashTests[0].modelB", ex.Message);
        }

        [Fact]
        public void Parse_ExecutedClashTestModelB_MissingModelName_Fails()
        {
            const string json = """
                {
                  "schemaVersion": 2,
                  "runId": "run-1",
                  "createdAt": "2026-07-10T09:00:00+01:00",
                  "models": [
                    { "company": "Sigma", "discipline": "Structure", "modelName": "Sigma_Structure", "revision": "R04", "sourceFileName": "Sigma_Structure_R04.nwc" }
                  ],
                  "executedClashTests": [
                    {
                      "name": "Self Clash",
                      "modelA": { "company": "Sigma", "discipline": "Structure", "modelName": "Sigma_Structure" },
                      "modelB": { "company": "Sigma", "discipline": "Structure" }
                    }
                  ]
                }
                """;

            var ex = Assert.Throws<RunManifestFormatException>(() => new JsonRunManifestSource().Parse(json));
            Assert.Contains("executedClashTests[0].modelB", ex.Message);
        }

        [Fact]
        public void Parse_ExecutedClashTestUnknownProperty_Fails()
        {
            const string json = """
                {
                  "schemaVersion": 2,
                  "runId": "run-1",
                  "createdAt": "2026-07-10T09:00:00+01:00",
                  "models": [
                    { "company": "Sigma", "discipline": "Structure", "modelName": "Sigma_Structure", "revision": "R04", "sourceFileName": "Sigma_Structure_R04.nwc" }
                  ],
                  "executedClashTests": [
                    {
                      "name": "Self Clash",
                      "modelA": { "company": "Sigma", "discipline": "Structure", "modelName": "Sigma_Structure" },
                      "modelB": { "company": "Sigma", "discipline": "Structure", "modelName": "Sigma_Structure" },
                      "unexpected": true
                    }
                  ]
                }
                """;

            Assert.Throws<RunManifestFormatException>(() => new JsonRunManifestSource().Parse(json));
        }

        [Fact]
        public void Parse_ExecutedClashTestModelA_UnknownProperty_Fails()
        {
            const string json = """
                {
                  "schemaVersion": 2,
                  "runId": "run-1",
                  "createdAt": "2026-07-10T09:00:00+01:00",
                  "models": [
                    { "company": "Sigma", "discipline": "Structure", "modelName": "Sigma_Structure", "revision": "R04", "sourceFileName": "Sigma_Structure_R04.nwc" }
                  ],
                  "executedClashTests": [
                    {
                      "name": "Self Clash",
                      "modelA": { "company": "Sigma", "discipline": "Structure", "modelName": "Sigma_Structure", "revision": "R04" },
                      "modelB": { "company": "Sigma", "discipline": "Structure", "modelName": "Sigma_Structure" }
                    }
                  ]
                }
                """;

            Assert.Throws<RunManifestFormatException>(() => new JsonRunManifestSource().Parse(json));
        }

        [Fact]
        public void Parse_ExecutedClashTestModelB_UnknownProperty_Fails()
        {
            const string json = """
                {
                  "schemaVersion": 2,
                  "runId": "run-1",
                  "createdAt": "2026-07-10T09:00:00+01:00",
                  "models": [
                    { "company": "Sigma", "discipline": "Structure", "modelName": "Sigma_Structure", "revision": "R04", "sourceFileName": "Sigma_Structure_R04.nwc" }
                  ],
                  "executedClashTests": [
                    {
                      "name": "Self Clash",
                      "modelA": { "company": "Sigma", "discipline": "Structure", "modelName": "Sigma_Structure" },
                      "modelB": { "company": "Sigma", "discipline": "Structure", "modelName": "Sigma_Structure", "sourceFileName": "x.nwc" }
                    }
                  ]
                }
                """;

            Assert.Throws<RunManifestFormatException>(() => new JsonRunManifestSource().Parse(json));
        }

        [Fact]
        public void Parse_ExecutedClashTestDuplicatePropertyInItem_Fails()
        {
            const string json = """
                {
                  "schemaVersion": 2,
                  "runId": "run-1",
                  "createdAt": "2026-07-10T09:00:00+01:00",
                  "models": [
                    { "company": "Sigma", "discipline": "Structure", "modelName": "Sigma_Structure", "revision": "R04", "sourceFileName": "Sigma_Structure_R04.nwc" }
                  ],
                  "executedClashTests": [
                    {
                      "name": "Self Clash",
                      "name": "Self Clash Again",
                      "modelA": { "company": "Sigma", "discipline": "Structure", "modelName": "Sigma_Structure" },
                      "modelB": { "company": "Sigma", "discipline": "Structure", "modelName": "Sigma_Structure" }
                    }
                  ]
                }
                """;

            var ex = Assert.Throws<RunManifestFormatException>(() => new JsonRunManifestSource().Parse(json));
            Assert.Contains("Duplicate", ex.Message);
            Assert.Contains("name", ex.Message);
            Assert.Contains("executedClashTests[0]", ex.Message);
        }

        [Fact]
        public void Parse_ExecutedClashTestDuplicateNestedPropertyInModelA_Fails()
        {
            const string json = """
                {
                  "schemaVersion": 2,
                  "runId": "run-1",
                  "createdAt": "2026-07-10T09:00:00+01:00",
                  "models": [
                    { "company": "Sigma", "discipline": "Structure", "modelName": "Sigma_Structure", "revision": "R04", "sourceFileName": "Sigma_Structure_R04.nwc" }
                  ],
                  "executedClashTests": [
                    {
                      "name": "Self Clash",
                      "modelA": { "company": "Sigma", "company": "Sigma2", "discipline": "Structure", "modelName": "Sigma_Structure" },
                      "modelB": { "company": "Sigma", "discipline": "Structure", "modelName": "Sigma_Structure" }
                    }
                  ]
                }
                """;

            var ex = Assert.Throws<RunManifestFormatException>(() => new JsonRunManifestSource().Parse(json));
            Assert.Contains("Duplicate", ex.Message);
            Assert.Contains("company", ex.Message);
            Assert.Contains("executedClashTests[0].modelA", ex.Message);
        }

        [Fact]
        public void Parse_ExecutedClashTestDuplicateNestedPropertyInModelB_Fails()
        {
            const string json = """
                {
                  "schemaVersion": 2,
                  "runId": "run-1",
                  "createdAt": "2026-07-10T09:00:00+01:00",
                  "models": [
                    { "company": "Sigma", "discipline": "Structure", "modelName": "Sigma_Structure", "revision": "R04", "sourceFileName": "Sigma_Structure_R04.nwc" }
                  ],
                  "executedClashTests": [
                    {
                      "name": "Self Clash",
                      "modelA": { "company": "Sigma", "discipline": "Structure", "modelName": "Sigma_Structure" },
                      "modelB": { "company": "Sigma", "discipline": "Structure", "discipline": "Structure2", "modelName": "Sigma_Structure" }
                    }
                  ]
                }
                """;

            var ex = Assert.Throws<RunManifestFormatException>(() => new JsonRunManifestSource().Parse(json));
            Assert.Contains("Duplicate", ex.Message);
            Assert.Contains("discipline", ex.Message);
            Assert.Contains("executedClashTests[0].modelB", ex.Message);
        }

        [Fact]
        public void Parse_ExecutedClashTest_WrongCaseProperty_FailsAsUnknown()
        {
            const string json = """
                {
                  "schemaVersion": 2,
                  "runId": "run-1",
                  "createdAt": "2026-07-10T09:00:00+01:00",
                  "models": [
                    { "company": "Sigma", "discipline": "Structure", "modelName": "Sigma_Structure", "revision": "R04", "sourceFileName": "Sigma_Structure_R04.nwc" }
                  ],
                  "executedClashTests": [
                    {
                      "Name": "Self Clash",
                      "modelA": { "company": "Sigma", "discipline": "Structure", "modelName": "Sigma_Structure" },
                      "modelB": { "company": "Sigma", "discipline": "Structure", "modelName": "Sigma_Structure" }
                    }
                  ]
                }
                """;

            var ex = Assert.Throws<RunManifestFormatException>(() => new JsonRunManifestSource().Parse(json));
            Assert.DoesNotContain("Duplicate", ex.Message);
        }

        // --- executedClashTests: Core validation surfaced through the adapter ---

        [Fact]
        public void Parse_ExecutedClashTest_ReferencingUndeclaredModel_Fails()
        {
            const string json = """
                {
                  "schemaVersion": 2,
                  "runId": "run-1",
                  "createdAt": "2026-07-10T09:00:00+01:00",
                  "models": [
                    { "company": "Sigma", "discipline": "Structure", "modelName": "Sigma_Structure", "revision": "R04", "sourceFileName": "Sigma_Structure_R04.nwc" }
                  ],
                  "executedClashTests": [
                    {
                      "name": "HVAC vs Structure",
                      "modelA": { "company": "Sigma", "discipline": "Structure", "modelName": "Sigma_Structure" },
                      "modelB": { "company": "Alfa", "discipline": "HVAC", "modelName": "Alfa_HVAC" }
                    }
                  ]
                }
                """;

            var ex = Assert.Throws<RunManifestFormatException>(() => new JsonRunManifestSource().Parse(json));
            Assert.Contains("not declared", ex.Message);
        }

        [Fact]
        public void Parse_DuplicateExecutedClashTest_DirectPair_Fails()
        {
            const string json = """
                {
                  "schemaVersion": 2,
                  "runId": "run-1",
                  "createdAt": "2026-07-10T09:00:00+01:00",
                  "models": [
                    { "company": "Sigma", "discipline": "Structure", "modelName": "Sigma_Structure", "revision": "R04", "sourceFileName": "Sigma_Structure_R04.nwc" },
                    { "company": "Alfa", "discipline": "HVAC", "modelName": "Alfa_HVAC", "revision": "R07", "sourceFileName": "Alfa_HVAC_R07.nwc" }
                  ],
                  "executedClashTests": [
                    {
                      "name": "HVAC vs Structure",
                      "modelA": { "company": "Sigma", "discipline": "Structure", "modelName": "Sigma_Structure" },
                      "modelB": { "company": "Alfa", "discipline": "HVAC", "modelName": "Alfa_HVAC" }
                    },
                    {
                      "name": "HVAC vs Structure",
                      "modelA": { "company": "Sigma", "discipline": "Structure", "modelName": "Sigma_Structure" },
                      "modelB": { "company": "Alfa", "discipline": "HVAC", "modelName": "Alfa_HVAC" }
                    }
                  ]
                }
                """;

            var ex = Assert.Throws<RunManifestFormatException>(() => new JsonRunManifestSource().Parse(json));
            Assert.Contains("Duplicate executed clash test", ex.Message);
        }

        [Fact]
        public void Parse_DuplicateExecutedClashTest_InvertedPair_Fails()
        {
            const string json = """
                {
                  "schemaVersion": 2,
                  "runId": "run-1",
                  "createdAt": "2026-07-10T09:00:00+01:00",
                  "models": [
                    { "company": "Sigma", "discipline": "Structure", "modelName": "Sigma_Structure", "revision": "R04", "sourceFileName": "Sigma_Structure_R04.nwc" },
                    { "company": "Alfa", "discipline": "HVAC", "modelName": "Alfa_HVAC", "revision": "R07", "sourceFileName": "Alfa_HVAC_R07.nwc" }
                  ],
                  "executedClashTests": [
                    {
                      "name": "HVAC vs Structure",
                      "modelA": { "company": "Sigma", "discipline": "Structure", "modelName": "Sigma_Structure" },
                      "modelB": { "company": "Alfa", "discipline": "HVAC", "modelName": "Alfa_HVAC" }
                    },
                    {
                      "name": "HVAC vs Structure",
                      "modelA": { "company": "Alfa", "discipline": "HVAC", "modelName": "Alfa_HVAC" },
                      "modelB": { "company": "Sigma", "discipline": "Structure", "modelName": "Sigma_Structure" }
                    }
                  ]
                }
                """;

            var ex = Assert.Throws<RunManifestFormatException>(() => new JsonRunManifestSource().Parse(json));
            Assert.Contains("Duplicate executed clash test", ex.Message);
        }

        [Fact]
        public void Parse_SelfClashExecutedClashTest_IsValid()
        {
            const string json = """
                {
                  "schemaVersion": 2,
                  "runId": "run-1",
                  "createdAt": "2026-07-10T09:00:00+01:00",
                  "models": [
                    { "company": "Sigma", "discipline": "Structure", "modelName": "Sigma_Structure", "revision": "R04", "sourceFileName": "Sigma_Structure_R04.nwc" }
                  ],
                  "executedClashTests": [
                    {
                      "name": "Self Clash",
                      "modelA": { "company": "Sigma", "discipline": "Structure", "modelName": "Sigma_Structure" },
                      "modelB": { "company": "Sigma", "discipline": "Structure", "modelName": "Sigma_Structure" }
                    }
                  ]
                }
                """;

            var manifest = new JsonRunManifestSource().Parse(json);

            var test = Assert.Single(manifest.ExecutedClashTests);
            Assert.Equal(test.ModelA, test.ModelB);
        }

        [Fact]
        public void Parse_PreservesDeclaredOrderOfExecutedClashTests()
        {
            const string json = """
                {
                  "schemaVersion": 2,
                  "runId": "run-1",
                  "createdAt": "2026-07-10T09:00:00+01:00",
                  "models": [
                    { "company": "Sigma", "discipline": "Structure", "modelName": "Sigma_Structure", "revision": "R04", "sourceFileName": "Sigma_Structure_R04.nwc" },
                    { "company": "Alfa", "discipline": "HVAC", "modelName": "Alfa_HVAC", "revision": "R07", "sourceFileName": "Alfa_HVAC_R07.nwc" }
                  ],
                  "executedClashTests": [
                    {
                      "name": "Second Test",
                      "modelA": { "company": "Sigma", "discipline": "Structure", "modelName": "Sigma_Structure" },
                      "modelB": { "company": "Alfa", "discipline": "HVAC", "modelName": "Alfa_HVAC" }
                    },
                    {
                      "name": "First Test",
                      "modelA": { "company": "Sigma", "discipline": "Structure", "modelName": "Sigma_Structure" },
                      "modelB": { "company": "Alfa", "discipline": "HVAC", "modelName": "Alfa_HVAC" }
                    }
                  ]
                }
                """;

            var manifest = new JsonRunManifestSource().Parse(json);

            Assert.Equal("Second Test", manifest.ExecutedClashTests[0].Name);
            Assert.Equal("First Test", manifest.ExecutedClashTests[1].Name);
        }

        [Fact]
        public void Parse_BuildsExecutedClashTestsCorrectly()
        {
            var manifest = new JsonRunManifestSource().Parse(ValidTwoModelJsonWithTest());

            var test = Assert.Single(manifest.ExecutedClashTests);
            Assert.Equal("HVAC vs Structure", test.Name);
            Assert.Equal("Sigma", test.ModelA.Company);
            Assert.Equal("Structure", test.ModelA.Discipline);
            Assert.Equal("Alfa", test.ModelB.Company);
            Assert.Equal("HVAC", test.ModelB.Discipline);
        }
    }
}
