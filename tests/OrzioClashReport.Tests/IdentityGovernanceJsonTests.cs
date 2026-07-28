using System;
using System.IO;
using System.Text;
using OrzioClashReport.Core.Model;
using OrzioClashReport.Persistence.IdentityGovernanceJson;

namespace OrzioClashReport.Tests
{
    public sealed class IdentityGovernanceJsonTests
    {
        private const string ConfirmExpectedJson = """
        {
          "schemaVersion": 1,
          "projectId": "coordination-project",
          "decisions": [
            {
              "decisionId": "decision-001",
              "projectId": "coordination-project",
              "decisionKind": "ConfirmSameIdentity",
              "leftEvidence": {
                "runId": "run-001",
                "occurrenceIndex": 1
              },
              "rightEvidence": {
                "runId": "run-002",
                "occurrenceIndex": 2
              },
              "persistentIdentityId": "identity-001",
              "reviewerAlias": "coordinator-a",
              "reason": "Confirmed from model context"
            }
          ]
        }
        """;

        private const string RejectExpectedJson = """
        {
          "schemaVersion": 1,
          "projectId": "coordination-project",
          "decisions": [
            {
              "decisionId": "decision-002",
              "projectId": "coordination-project",
              "decisionKind": "RejectSameIdentity",
              "leftEvidence": {
                "runId": "run-010",
                "occurrenceIndex": 4
              },
              "rightEvidence": {
                "runId": "run-011",
                "occurrenceIndex": 5
              },
              "reviewerAlias": "coordinator-b"
            }
          ]
        }
        """;

        private static JsonIdentityGovernanceSerializer Sut() => new JsonIdentityGovernanceSerializer();

        [Fact]
        public void Serialize_ConfirmDecision_MatchesExactExpectedJson()
        {
            string json = Sut().Serialize(BuildConfirmDocument());

            Assert.Equal(ConfirmExpectedJson.Replace("\r\n", "\n") + "\n", json, StringComparer.Ordinal);
        }

        [Fact]
        public void ParseSerializeRoundTrip_ConfirmDecision_PreservesFields()
        {
            IdentityGovernanceDocument loaded = Sut().Parse(Sut().Serialize(BuildConfirmDocument()));

            Assert.Equal("coordination-project", loaded.ProjectId);
            Assert.Single(loaded.Decisions);
            Assert.Equal(HumanIdentityDecisionKind.ConfirmSameIdentity, loaded.Decisions[0].DecisionKind);
            Assert.Equal("identity-001", loaded.Decisions[0].PersistentIdentityId);
        }

        [Fact]
        public void ParseSerializeRoundTrip_RejectDecision_PreservesFields()
        {
            IdentityGovernanceDocument loaded = Sut().Parse(Sut().Serialize(BuildRejectDocument()));

            Assert.Single(loaded.Decisions);
            Assert.Equal(HumanIdentityDecisionKind.RejectSameIdentity, loaded.Decisions[0].DecisionKind);
            Assert.Null(loaded.Decisions[0].PersistentIdentityId);
        }

        [Fact]
        public void Serialize_RepeatedCalls_AreOrdinalIdentical()
        {
            IdentityGovernanceDocument document = BuildConfirmDocument();

            string first = Sut().Serialize(document);
            string second = Sut().Serialize(document);

            Assert.Equal(first, second, StringComparer.Ordinal);
        }

        [Fact]
        public void Serialize_UsesLfLineEndingsOnly()
        {
            string json = Sut().Serialize(BuildConfirmDocument());

            Assert.DoesNotContain("\r", json, StringComparison.Ordinal);
        }

        [Fact]
        public void Serialize_EndsWithExactlyOneTrailingLf()
        {
            string json = Sut().Serialize(BuildConfirmDocument());

            Assert.EndsWith("\n", json, StringComparison.Ordinal);
            Assert.False(json.EndsWith("\n\n", StringComparison.Ordinal));
        }

        [Fact]
        public void Save_WritesUtf8WithoutBom()
        {
            string tempDirectory = CreateTempDirectory();
            string outputPath = Path.Combine(tempDirectory, "identity-governance.json");

            try
            {
                Sut().Save(BuildConfirmDocument(), outputPath);

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
            string outputPath = Path.Combine(tempDirectory, "identity-governance.json");

            try
            {
                File.WriteAllText(outputPath, "ORIGINAL", new UTF8Encoding(false));

                var ex = Assert.Throws<IdentityGovernanceFormatException>(() => Sut().Save(BuildConfirmDocument(), outputPath));

                Assert.Contains("Identity governance file already exists:", ex.Message, StringComparison.Ordinal);
            }
            finally
            {
                DeleteFileIfExists(outputPath);
                DeleteDirectoryIfExists(tempDirectory);
            }
        }

        [Fact]
        public void Parse_UnknownSchemaVersion_Fails()
        {
            var ex = Assert.Throws<IdentityGovernanceFormatException>(
                () => Sut().Parse(ConfirmExpectedJson.Replace("\"schemaVersion\": 1", "\"schemaVersion\": 2", StringComparison.Ordinal)));

            Assert.Contains("Unsupported identity governance schemaVersion '2'", ex.Message, StringComparison.Ordinal);
        }

        [Fact]
        public void Parse_UnknownDecisionKind_Fails()
        {
            string json = ConfirmExpectedJson.Replace(
                "\"ConfirmSameIdentity\"",
                "\"MaybeSameIdentity\"",
                StringComparison.Ordinal);

            var ex = Assert.Throws<IdentityGovernanceFormatException>(() => Sut().Parse(json));

            Assert.Contains("Human identity decision kind 'MaybeSameIdentity' is not recognized", ex.Message, StringComparison.Ordinal);
        }

        [Fact]
        public void Parse_MissingRequiredProperty_Fails()
        {
            string json = """
            {
              "schemaVersion": 1,
              "projectId": "coordination-project"
            }
            """;

            var ex = Assert.Throws<IdentityGovernanceFormatException>(() => Sut().Parse(json));

            Assert.Contains("'decisions' is required", ex.Message, StringComparison.Ordinal);
        }

        [Fact]
        public void Parse_DuplicateDecisionId_Fails()
        {
            string json = """
            {
              "schemaVersion": 1,
              "projectId": "coordination-project",
              "decisions": [
                {
                  "decisionId": "decision-001",
                  "projectId": "coordination-project",
                  "decisionKind": "ConfirmSameIdentity",
                  "leftEvidence": { "runId": "run-001", "occurrenceIndex": 1 },
                  "rightEvidence": { "runId": "run-002", "occurrenceIndex": 2 },
                  "persistentIdentityId": "identity-001",
                  "reviewerAlias": "coordinator-a"
                },
                {
                  "decisionId": "decision-001",
                  "projectId": "coordination-project",
                  "decisionKind": "RejectSameIdentity",
                  "leftEvidence": { "runId": "run-010", "occurrenceIndex": 3 },
                  "rightEvidence": { "runId": "run-011", "occurrenceIndex": 4 },
                  "reviewerAlias": "coordinator-b"
                }
              ]
            }
            """;

            var ex = Assert.Throws<IdentityGovernanceFormatException>(() => Sut().Parse(json));

            Assert.Contains("Decision id 'decision-001' is duplicated", ex.Message, StringComparison.Ordinal);
        }

        [Fact]
        public void Parse_ConflictingOrderedEvidencePair_Fails()
        {
            string json = """
            {
              "schemaVersion": 1,
              "projectId": "coordination-project",
              "decisions": [
                {
                  "decisionId": "decision-001",
                  "projectId": "coordination-project",
                  "decisionKind": "ConfirmSameIdentity",
                  "leftEvidence": { "runId": "run-001", "occurrenceIndex": 1 },
                  "rightEvidence": { "runId": "run-002", "occurrenceIndex": 2 },
                  "persistentIdentityId": "identity-001",
                  "reviewerAlias": "coordinator-a"
                },
                {
                  "decisionId": "decision-002",
                  "projectId": "coordination-project",
                  "decisionKind": "RejectSameIdentity",
                  "leftEvidence": { "runId": "run-001", "occurrenceIndex": 1 },
                  "rightEvidence": { "runId": "run-002", "occurrenceIndex": 2 },
                  "reviewerAlias": "coordinator-b"
                }
              ]
            }
            """;

            var ex = Assert.Throws<IdentityGovernanceFormatException>(() => Sut().Parse(json));

            Assert.Contains("Ordered evidence pair", ex.Message, StringComparison.Ordinal);
        }

        [Fact]
        public void Parse_DivergentProjectId_Fails()
        {
            string json = """
            {
              "schemaVersion": 1,
              "projectId": "coordination-project",
              "decisions": [
                {
                  "decisionId": "decision-001",
                  "projectId": "other-project",
                  "decisionKind": "ConfirmSameIdentity",
                  "leftEvidence": {
                    "runId": "run-001",
                    "occurrenceIndex": 1
                  },
                  "rightEvidence": {
                    "runId": "run-002",
                    "occurrenceIndex": 2
                  },
                  "persistentIdentityId": "identity-001",
                  "reviewerAlias": "coordinator-a",
                  "reason": "Confirmed from model context"
                }
              ]
            }
            """;

            var ex = Assert.Throws<IdentityGovernanceFormatException>(() => Sut().Parse(json));

            Assert.Contains("does not match document project", ex.Message, StringComparison.Ordinal);
        }

        [Fact]
        public void Parse_MalformedJson_FailsWithDeterministicSafeMessage()
        {
            var ex = Assert.Throws<IdentityGovernanceFormatException>(() => Sut().Parse("{ \"schemaVersion\": 1, "));

            Assert.StartsWith("Identity governance JSON is malformed or invalid", ex.Message, StringComparison.Ordinal);
            Assert.DoesNotContain("C:\\", ex.Message, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public void Parse_UnknownRootProperty_Fails()
        {
            string json = ConfirmExpectedJson.Replace(
                "\"projectId\": \"coordination-project\",",
                "\"projectId\": \"coordination-project\",\n  \"unknownRoot\": true,",
                StringComparison.Ordinal);

            Assert.Throws<IdentityGovernanceFormatException>(() => Sut().Parse(json));
        }

        [Fact]
        public void Parse_DuplicateRootProperty_Fails()
        {
            string json = """
            {
              "schemaVersion": 1,
              "schemaVersion": 1,
              "projectId": "coordination-project",
              "decisions": []
            }
            """;

            var ex = Assert.Throws<IdentityGovernanceFormatException>(() => Sut().Parse(json));

            Assert.Contains("Duplicate JSON property 'schemaVersion' at root.", ex.Message, StringComparison.Ordinal);
        }

        private static IdentityGovernanceDocument BuildConfirmDocument() =>
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

        private static IdentityGovernanceDocument BuildRejectDocument() =>
            new IdentityGovernanceDocument(
                "coordination-project",
                new[]
                {
                    new HumanIdentityDecision(
                        "decision-002",
                        "coordination-project",
                        HumanIdentityDecisionKind.RejectSameIdentity,
                        new ClashEvidenceEndpoint("run-010", 4),
                        new ClashEvidenceEndpoint("run-011", 5),
                        null,
                        "coordinator-b",
                        null),
                });

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
