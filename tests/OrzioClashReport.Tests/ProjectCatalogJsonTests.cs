using System;
using System.IO;
using System.Text;
using OrzioClashReport.Persistence.ProjectCatalogJson;

namespace OrzioClashReport.Tests
{
    public sealed class ProjectCatalogJsonTests
    {
        private static JsonProjectCatalogSerializer Sut() => new JsonProjectCatalogSerializer();

        private static ProjectCatalogPathResolver Resolver() => new ProjectCatalogPathResolver();

        private static ProjectCatalogDocument BuildMinimalDocument() =>
            new ProjectCatalogDocument(
                "example-project",
                "Example Coordination Project",
                "run-index.json",
                "reports/longitudinal.html");

        private const string MinimalExpectedJson = """
        {
          "schemaVersion": 1,
          "projectId": "example-project",
          "displayName": "Example Coordination Project",
          "runIndexPath": "run-index.json",
          "longitudinalReportPath": "reports/longitudinal.html"
        }
        """;

        [Fact]
        public void Serialize_MinimalValidDocument_MatchesExactExpectedJson()
        {
            string json = Sut().Serialize(BuildMinimalDocument());

            Assert.Equal(MinimalExpectedJson.Replace("\r\n", "\n") + "\n", json, StringComparer.Ordinal);
        }

        [Fact]
        public void Serialize_RepeatedCalls_AreOrdinalIdentical()
        {
            var document = BuildMinimalDocument();

            string first = Sut().Serialize(document);
            string second = Sut().Serialize(document);

            Assert.Equal(first, second, StringComparer.Ordinal);
        }

        [Fact]
        public void Serialize_UsesLfLineEndingsOnly()
        {
            string json = Sut().Serialize(BuildMinimalDocument());

            Assert.DoesNotContain("\r", json, StringComparison.Ordinal);
        }

        [Fact]
        public void Serialize_EndsWithExactlyOneTrailingLf()
        {
            string json = Sut().Serialize(BuildMinimalDocument());

            Assert.EndsWith("\n", json, StringComparison.Ordinal);
            Assert.False(json.EndsWith("\n\n", StringComparison.Ordinal));
        }

        [Fact]
        public void Save_WritesUtf8WithoutBom()
        {
            string tempDirectory = CreateTempDirectory();
            string outputPath = Path.Combine(tempDirectory, "project.json");

            try
            {
                Sut().Save(BuildMinimalDocument(), outputPath);

                byte[] bytes = File.ReadAllBytes(outputPath);
                Assert.False(bytes.Length >= 3 && bytes[0] == 0xEF && bytes[1] == 0xBB && bytes[2] == 0xBF);
            }
            finally
            {
                DeleteFileIfExists(outputPath);
                DeleteDirectoryIfExists(tempDirectory);
            }
        }

        [Fact]
        public void Save_ExistingPath_IsRefused()
        {
            string tempDirectory = CreateTempDirectory();
            string outputPath = Path.Combine(tempDirectory, "project.json");

            try
            {
                File.WriteAllText(outputPath, "ORIGINAL", new UTF8Encoding(false));

                var ex = Assert.Throws<ProjectCatalogFormatException>(() => Sut().Save(BuildMinimalDocument(), outputPath));

                Assert.Contains("Project catalog file already exists:", ex.Message, StringComparison.Ordinal);
                Assert.Equal("ORIGINAL", File.ReadAllText(outputPath), StringComparer.Ordinal);
            }
            finally
            {
                DeleteFileIfExists(outputPath);
                DeleteDirectoryIfExists(tempDirectory);
            }
        }

        [Fact]
        public void Save_SerializationFailure_CreatesNoDestinationFile()
        {
            string tempDirectory = CreateTempDirectory();
            string outputPath = Path.Combine(tempDirectory, "project.json");
            var invalidDocument = new ProjectCatalogDocument(
                new string(new[] { '\ud800' }),
                "Example Coordination Project",
                "run-index.json",
                "reports/longitudinal.html");

            try
            {
                Exception? ex = Record.Exception(() => Sut().Save(invalidDocument, outputPath));

                Assert.NotNull(ex);
                Assert.False(File.Exists(outputPath));
            }
            finally
            {
                DeleteFileIfExists(outputPath);
                DeleteDirectoryIfExists(tempDirectory);
            }
        }

        [Fact]
        public void Parse_SerializeRoundTrip_PreservesAllFields()
        {
            ProjectCatalogDocument loaded = Sut().Parse(Sut().Serialize(BuildMinimalDocument()));

            Assert.Equal("example-project", loaded.ProjectId);
            Assert.Equal("Example Coordination Project", loaded.DisplayName);
            Assert.Equal("run-index.json", loaded.RunIndexPath);
            Assert.Equal("reports/longitudinal.html", loaded.LongitudinalReportPath);
        }

        [Fact]
        public void Serialize_AfterRoundTrip_RemainsOrdinalIdentical()
        {
            string original = Sut().Serialize(BuildMinimalDocument());
            string roundTrip = Sut().Serialize(Sut().Parse(original));

            Assert.Equal(original, roundTrip, StringComparer.Ordinal);
        }

        [Fact]
        public void ProjectCatalogDocument_NullProjectId_IsRejected()
        {
            Assert.Throws<ArgumentNullException>(
                () => new ProjectCatalogDocument(null!, "Display", "run-index.json", "reports/longitudinal.html"));
        }

        [Fact]
        public void ProjectCatalogDocument_WhitespaceDisplayName_IsRejected()
        {
            Assert.Throws<ArgumentException>(
                () => new ProjectCatalogDocument("example-project", "   ", "run-index.json", "reports/longitudinal.html"));
        }

        [Fact]
        public void ProjectCatalogDocument_LeadingOrTrailingWhitespace_IsRejected()
        {
            Assert.Throws<ArgumentException>(
                () => new ProjectCatalogDocument(" example-project", "Display", "run-index.json", "reports/longitudinal.html"));
            Assert.Throws<ArgumentException>(
                () => new ProjectCatalogDocument("example-project", "Display ", "run-index.json", "reports/longitudinal.html"));
        }

        [Theory]
        [InlineData("\" run-index.json\"")]
        [InlineData("\"run-index.json \"")]
        public void Parse_RunIndexPathWithLeadingOrTrailingWhitespace_IsRejected(string runIndexPathJson)
        {
            var ex = Assert.Throws<ProjectCatalogFormatException>(
                () => Sut().Parse(BuildJson("1", "\"example-project\"", "\"Example Coordination Project\"", runIndexPathJson, "\"reports/longitudinal.html\"")));

            Assert.Contains("leading or trailing whitespace", ex.Message, StringComparison.Ordinal);
        }

        [Theory]
        [InlineData("\" reports/longitudinal.html\"")]
        [InlineData("\"reports/longitudinal.html \"")]
        public void Parse_ReportPathWithLeadingOrTrailingWhitespace_IsRejected(string reportPathJson)
        {
            var ex = Assert.Throws<ProjectCatalogFormatException>(
                () => Sut().Parse(BuildJson("1", "\"example-project\"", "\"Example Coordination Project\"", "\"run-index.json\"", reportPathJson)));

            Assert.Contains("leading or trailing whitespace", ex.Message, StringComparison.Ordinal);
        }

        [Fact]
        public void Parse_MissingSchemaVersion_IsRejected()
        {
            string json = """
            {
              "projectId": "example-project",
              "displayName": "Example Coordination Project",
              "runIndexPath": "run-index.json",
              "longitudinalReportPath": "reports/longitudinal.html"
            }
            """;

            var ex = Assert.Throws<ProjectCatalogFormatException>(() => Sut().Parse(json));
            Assert.Contains("'schemaVersion' is required", ex.Message, StringComparison.Ordinal);
        }

        [Fact]
        public void Parse_UnsupportedSchemaVersion_IsRejected()
        {
            var ex = Assert.Throws<ProjectCatalogFormatException>(() => Sut().Parse(BuildJson("2", "\"example-project\"", "\"Example Coordination Project\"", "\"run-index.json\"", "\"reports/longitudinal.html\"")));
            Assert.Contains("Unsupported project catalog schemaVersion '2'", ex.Message, StringComparison.Ordinal);
        }

        [Fact]
        public void Parse_WrongPropertyCasing_IsRejected()
        {
            string json = """
            {
              "schemaVersion": 1,
              "ProjectId": "example-project",
              "displayName": "Example Coordination Project",
              "runIndexPath": "run-index.json",
              "longitudinalReportPath": "reports/longitudinal.html"
            }
            """;

            Assert.Throws<ProjectCatalogFormatException>(() => Sut().Parse(json));
        }

        [Fact]
        public void Parse_UnknownRootProperty_IsRejected()
        {
            string json = """
            {
              "schemaVersion": 1,
              "projectId": "example-project",
              "displayName": "Example Coordination Project",
              "runIndexPath": "run-index.json",
              "longitudinalReportPath": "reports/longitudinal.html",
              "unknownRoot": true
            }
            """;

            Assert.Throws<ProjectCatalogFormatException>(() => Sut().Parse(json));
        }

        [Fact]
        public void Parse_DuplicateProperties_AreRejected()
        {
            string json = """
            {
              "schemaVersion": 1,
              "projectId": "example-project",
              "projectId": "again",
              "displayName": "Example Coordination Project",
              "runIndexPath": "run-index.json",
              "longitudinalReportPath": "reports/longitudinal.html"
            }
            """;

            var ex = Assert.Throws<ProjectCatalogFormatException>(() => Sut().Parse(json));
            Assert.Contains("Duplicate JSON property 'projectId' at root.", ex.Message, StringComparison.Ordinal);
        }

        [Theory]
        [InlineData("\"/run-index.json\"")]
        [InlineData("\"//server/share/run-index.json\"")]
        [InlineData("\"C:/run-index.json\"")]
        [InlineData("\"C:\\\\run-index.json\"")]
        [InlineData("\"runs/\"")]
        [InlineData("\"runs//run-index.json\"")]
        [InlineData("\"./run-index.json\"")]
        [InlineData("\"../run-index.json\"")]
        public void Parse_InvalidRunIndexPathReference_IsRejected(string runIndexPathJson)
        {
            Assert.Throws<ProjectCatalogFormatException>(
                () => Sut().Parse(BuildJson("1", "\"example-project\"", "\"Example Coordination Project\"", runIndexPathJson, "\"reports/longitudinal.html\"")));
        }

        [Fact]
        public void Parse_RunIndexPathEqualToReportPath_IsRejected()
        {
            var ex = Assert.Throws<ProjectCatalogFormatException>(
                () => Sut().Parse(BuildJson("1", "\"example-project\"", "\"Example Coordination Project\"", "\"same.json\"", "\"same.json\"")));

            Assert.Contains("Run index path and longitudinal report path must be different", ex.Message, StringComparison.Ordinal);
        }

        [Fact]
        public void Resolver_CreateReference_CreatesCanonicalRelativePath()
        {
            string tempDirectory = CreateTempDirectory();

            try
            {
                string projectPath = Path.Combine(tempDirectory, "workspace", "project.json");
                string targetPath = Path.Combine(tempDirectory, "workspace", "reports", "longitudinal.html");

                string reference = Resolver().CreateReference(projectPath, targetPath);

                Assert.Equal("reports/longitudinal.html", reference);
            }
            finally
            {
                DeleteDirectoryIfExists(tempDirectory);
            }
        }

        [Fact]
        public void Resolver_CreateReference_RejectsTargetOutsideProjectDirectory()
        {
            string tempDirectory = CreateTempDirectory();

            try
            {
                string projectPath = Path.Combine(tempDirectory, "workspace", "project.json");
                string targetPath = Path.Combine(tempDirectory, "outside", "run-index.json");

                var ex = Assert.Throws<ProjectCatalogFormatException>(() => Resolver().CreateReference(projectPath, targetPath));

                Assert.Contains("must be inside project catalog directory", ex.Message, StringComparison.Ordinal);
            }
            finally
            {
                DeleteDirectoryIfExists(tempDirectory);
            }
        }

        [Fact]
        public void Resolver_ResolveReference_WorksAfterMovingEntireWorkspace()
        {
            string firstRoot = CreateTempDirectory();
            string secondRoot = CreateTempDirectory();

            try
            {
                string firstProjectPath = Path.Combine(firstRoot, "workspace", "project.json");
                string firstReportPath = Path.Combine(firstRoot, "workspace", "reports", "longitudinal.html");
                string reference = Resolver().CreateReference(firstProjectPath, firstReportPath);

                string movedProjectPath = Path.Combine(secondRoot, "workspace", "project.json");
                string resolved = Resolver().ResolveReference(movedProjectPath, reference);
                string expected = Path.Combine(secondRoot, "workspace", "reports", "longitudinal.html");

                AssertPathsEqual(expected, resolved);
            }
            finally
            {
                DeleteDirectoryIfExists(firstRoot);
                DeleteDirectoryIfExists(secondRoot);
            }
        }

        [Fact]
        public void Load_MissingFile_IsRejected()
        {
            string path = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}.json");

            var ex = Assert.Throws<ProjectCatalogFormatException>(() => Sut().Load(path));

            Assert.Contains("Project catalog file not found:", ex.Message, StringComparison.Ordinal);
        }

        private static string BuildJson(
            string schemaVersion,
            string projectId,
            string displayName,
            string runIndexPath,
            string longitudinalReportPath) =>
            $$"""
            {
              "schemaVersion": {{schemaVersion}},
              "projectId": {{projectId}},
              "displayName": {{displayName}},
              "runIndexPath": {{runIndexPath}},
              "longitudinalReportPath": {{longitudinalReportPath}}
            }
            """;

        private static void AssertPathsEqual(string expectedPath, string actualPath)
        {
            string expectedFullPath = Path.GetFullPath(expectedPath);
            string actualFullPath = Path.GetFullPath(actualPath);
            StringComparer comparer = OperatingSystem.IsWindows() ? StringComparer.OrdinalIgnoreCase : StringComparer.Ordinal;
            Assert.Equal(expectedFullPath, actualFullPath, comparer);
        }

        private static string CreateTempDirectory()
        {
            string path = Path.Combine(Path.GetTempPath(), $"orzio-project-catalog-tests-{Guid.NewGuid():N}");
            Directory.CreateDirectory(path);
            return path;
        }

        private static void DeleteFileIfExists(string path)
        {
            if (!string.IsNullOrEmpty(path) && File.Exists(path))
            {
                File.Delete(path);
            }
        }

        private static void DeleteDirectoryIfExists(string path)
        {
            if (!string.IsNullOrEmpty(path) && Directory.Exists(path))
            {
                Directory.Delete(path, recursive: true);
            }
        }
    }
}
