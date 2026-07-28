using System;
using System.IO;
using System.Text;
using OrzioClashReport.Core.Model;
using OrzioClashReport.Persistence.IdentityGovernanceJson;

namespace OrzioClashReport.Tests
{
    public sealed class IdentityGovernanceFileReplacerTests
    {
        private static JsonIdentityGovernanceSerializer Sut() => new JsonIdentityGovernanceSerializer();

        private static IdentityGovernanceFileReplacer Replacer() => new IdentityGovernanceFileReplacer();

        [Fact]
        public void ReplaceExisting_ExistingPath_IsReplacedWithDeterministicJson()
        {
            string tempDirectory = CreateTempDirectory();
            string outputPath = Path.Combine(tempDirectory, "identity-governance.json");
            IdentityGovernanceDocument updatedDocument = BuildTwoDecisionDocument();

            try
            {
                File.WriteAllText(outputPath, "ORIGINAL", new UTF8Encoding(false));

                Replacer().ReplaceExisting(updatedDocument, outputPath);

                Assert.Equal(Sut().Serialize(updatedDocument), File.ReadAllText(outputPath), StringComparer.Ordinal);
            }
            finally
            {
                DeleteFileIfExists(outputPath);
                DeleteDirectoryIfExists(tempDirectory);
            }
        }

        [Fact]
        public void ReplaceExisting_PreservesExistingDecisionsAndAppendsNewDecisionAtEnd()
        {
            string tempDirectory = CreateTempDirectory();
            string outputPath = Path.Combine(tempDirectory, "identity-governance.json");

            try
            {
                Sut().Save(BuildOneDecisionDocument(), outputPath);

                Replacer().ReplaceExisting(BuildTwoDecisionDocument(), outputPath);

                IdentityGovernanceDocument loaded = Sut().Load(outputPath);
                Assert.Equal(2, loaded.Decisions.Count);
                Assert.Equal("decision-001", loaded.Decisions[0].DecisionId);
                Assert.Equal("decision-002", loaded.Decisions[1].DecisionId);
            }
            finally
            {
                DeleteFileIfExists(outputPath);
                DeleteDirectoryIfExists(tempDirectory);
            }
        }

        [Fact]
        public void ReplaceExisting_WritesUtf8WithoutBom()
        {
            string tempDirectory = CreateTempDirectory();
            string outputPath = Path.Combine(tempDirectory, "identity-governance.json");

            try
            {
                File.WriteAllText(outputPath, "ORIGINAL", new UTF8Encoding(false));

                Replacer().ReplaceExisting(BuildOneDecisionDocument(), outputPath);

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
        public void ReplaceExisting_UsesLfLineEndingsOnly()
        {
            string tempDirectory = CreateTempDirectory();
            string outputPath = Path.Combine(tempDirectory, "identity-governance.json");

            try
            {
                File.WriteAllText(outputPath, "ORIGINAL", new UTF8Encoding(false));

                Replacer().ReplaceExisting(BuildOneDecisionDocument(), outputPath);

                string json = File.ReadAllText(outputPath);
                Assert.DoesNotContain("\r", json, StringComparison.Ordinal);
            }
            finally
            {
                DeleteFileIfExists(outputPath);
                DeleteDirectoryIfExists(tempDirectory);
            }
        }

        [Fact]
        public void ReplaceExisting_EndsWithExactlyOneTrailingLf()
        {
            string tempDirectory = CreateTempDirectory();
            string outputPath = Path.Combine(tempDirectory, "identity-governance.json");

            try
            {
                File.WriteAllText(outputPath, "ORIGINAL", new UTF8Encoding(false));

                Replacer().ReplaceExisting(BuildOneDecisionDocument(), outputPath);

                string json = File.ReadAllText(outputPath);
                Assert.EndsWith("\n", json, StringComparison.Ordinal);
                Assert.False(json.EndsWith("\n\n", StringComparison.Ordinal));
            }
            finally
            {
                DeleteFileIfExists(outputPath);
                DeleteDirectoryIfExists(tempDirectory);
            }
        }

        [Fact]
        public void ReplaceExisting_MissingDestination_IsRejectedWithoutExposingPath()
        {
            string tempDirectory = CreateTempDirectory();
            string outputPath = Path.Combine(tempDirectory, "identity-governance.json");

            try
            {
                var ex = Assert.Throws<IdentityGovernanceFormatException>(
                    () => Replacer().ReplaceExisting(BuildOneDecisionDocument(), outputPath));

                Assert.Equal("Identity governance file not found.", ex.Message);
                Assert.DoesNotContain(outputPath, ex.Message, StringComparison.OrdinalIgnoreCase);
                Assert.False(File.Exists(outputPath));
            }
            finally
            {
                DeleteDirectoryIfExists(tempDirectory);
            }
        }

        [Fact]
        public void ReplaceExisting_DestinationDirectory_IsRejectedWithoutExposingPath()
        {
            string tempDirectory = CreateTempDirectory();

            try
            {
                var ex = Assert.Throws<IdentityGovernanceFormatException>(
                    () => Replacer().ReplaceExisting(BuildOneDecisionDocument(), tempDirectory));

                Assert.Equal("Identity governance destination cannot be an existing directory.", ex.Message);
                Assert.DoesNotContain(tempDirectory, ex.Message, StringComparison.OrdinalIgnoreCase);
                AssertNoReplacementTempFiles(tempDirectory);
            }
            finally
            {
                DeleteDirectoryIfExists(tempDirectory);
            }
        }

        [Fact]
        public void ReplaceExisting_SerializationFailure_PreservesOriginalBytes()
        {
            string tempDirectory = CreateTempDirectory();
            string outputPath = Path.Combine(tempDirectory, "identity-governance.json");
            byte[] originalBytes = Encoding.UTF8.GetBytes("ORIGINAL-IDENTITY-GOVERNANCE");

            try
            {
                File.WriteAllBytes(outputPath, originalBytes);

                var ex = Assert.Throws<IdentityGovernanceFormatException>(
                    () => Replacer().ReplaceExisting(BuildInvalidUtf16Document(), outputPath));

                Assert.Equal("Field 'decisions[0].decisionId' contains invalid UTF-16 data.", ex.Message);
                Assert.Equal(originalBytes, File.ReadAllBytes(outputPath));
            }
            finally
            {
                DeleteFileIfExists(outputPath);
                DeleteDirectoryIfExists(tempDirectory);
            }
        }

        [Fact]
        public void ReplaceExisting_SerializationFailure_LeavesNoTemporaryFile()
        {
            string tempDirectory = CreateTempDirectory();
            string outputPath = Path.Combine(tempDirectory, "identity-governance.json");

            try
            {
                File.WriteAllText(outputPath, "ORIGINAL", new UTF8Encoding(false));

                Assert.Throws<IdentityGovernanceFormatException>(
                    () => Replacer().ReplaceExisting(BuildInvalidUtf16Document(), outputPath));

                AssertNoReplacementTempFiles(tempDirectory);
            }
            finally
            {
                DeleteFileIfExists(outputPath);
                DeleteDirectoryIfExists(tempDirectory);
            }
        }

        [Fact]
        public void ReplaceExisting_Success_LeavesNoTemporaryFile()
        {
            string tempDirectory = CreateTempDirectory();
            string outputPath = Path.Combine(tempDirectory, "identity-governance.json");

            try
            {
                File.WriteAllText(outputPath, "ORIGINAL", new UTF8Encoding(false));

                Replacer().ReplaceExisting(BuildOneDecisionDocument(), outputPath);

                AssertNoReplacementTempFiles(tempDirectory);
            }
            finally
            {
                DeleteFileIfExists(outputPath);
                DeleteDirectoryIfExists(tempDirectory);
            }
        }

        private static IdentityGovernanceDocument BuildOneDecisionDocument() =>
            new IdentityGovernanceDocument(
                "coordination-project",
                new[]
                {
                    new HumanIdentityDecision(
                        "decision-001",
                        "coordination-project",
                        HumanIdentityDecisionKind.ConfirmSameIdentity,
                        new ClashEvidenceEndpoint("run-001", 1),
                        new ClashEvidenceEndpoint("run-002", 2),
                        "identity-001",
                        "coordinator-a",
                        "Confirmed from model context"),
                });

        private static IdentityGovernanceDocument BuildTwoDecisionDocument() =>
            new IdentityGovernanceDocument(
                "coordination-project",
                new[]
                {
                    BuildOneDecisionDocument().Decisions[0],
                    new HumanIdentityDecision(
                        "decision-002",
                        "coordination-project",
                        HumanIdentityDecisionKind.RejectSameIdentity,
                        new ClashEvidenceEndpoint("run-003", 3),
                        new ClashEvidenceEndpoint("run-004", 4),
                        null,
                        "coordinator-b",
                        "Different physical clashes"),
                });

        private static IdentityGovernanceDocument BuildInvalidUtf16Document()
        {
            string invalidDecisionId = new string(new[] { 'A', '\uD800', 'B' });

            return new IdentityGovernanceDocument(
                "coordination-project",
                new[]
                {
                    new HumanIdentityDecision(
                        invalidDecisionId,
                        "coordination-project",
                        HumanIdentityDecisionKind.ConfirmSameIdentity,
                        new ClashEvidenceEndpoint("run-001", 1),
                        new ClashEvidenceEndpoint("run-002", 2),
                        "identity-001",
                        "coordinator-a",
                        "Confirmed from model context"),
                });
        }

        private static void AssertNoReplacementTempFiles(string directoryPath)
        {
            string[] files = Directory.GetFiles(directoryPath, ".identity-governance-replace-*.tmp");
            Assert.Empty(files);
        }

        private static string CreateTempDirectory()
        {
            string path = Path.Combine(Path.GetTempPath(), $"orzio-clash-report-tests-{Guid.NewGuid():N}");
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
