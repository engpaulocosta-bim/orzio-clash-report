using System;
using System.IO;
using System.Reflection;
using System.Text;
using OrzioClashReport.Cli;
using OrzioClashReport.Core.Model;
using OrzioClashReport.Persistence.IdentityGovernanceJson;

namespace OrzioClashReport.Tests
{
    [Collection("CliConsole")]
    public sealed class IdentityGovernanceCliTests
    {
        private static readonly MethodInfo MainMethod = ResolveMainMethod();

        private const string CreateIdentityGovernanceUsage =
            "Usage: orzioclash create-identity-governance --project-id <project-id> (-o <identity-governance.json> | --output <identity-governance.json>)";

        private const string AppendIdentityDecisionUsage =
            "Usage: orzioclash append-identity-decision --governance <identity-governance.json> --decision-id <decision-id> --decision-kind <ConfirmSameIdentity|RejectSameIdentity> --left-run-id <run-id> --left-occurrence-index <zero-based-index> --right-run-id <run-id> --right-occurrence-index <zero-based-index> --reviewer-alias <alias> [--persistent-identity-id <persistent-id>] [--reason <text>]";

        private const string EmptyGovernanceExpectedJson = """
        {
          "schemaVersion": 1,
          "projectId": "coordination-project",
          "decisions": []
        }
        """;

        private const string ConfirmGovernanceExpectedJson = """
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
                "occurrenceIndex": 4
              },
              "rightEvidence": {
                "runId": "run-002",
                "occurrenceIndex": 7
              },
              "persistentIdentityId": "identity-001",
              "reviewerAlias": "coordinator-a",
              "reason": "Confirmed from model context"
            }
          ]
        }
        """;

        private const string RejectGovernanceExpectedJson = """
        {
          "schemaVersion": 1,
          "projectId": "coordination-project",
          "decisions": [
            {
              "decisionId": "decision-002",
              "projectId": "coordination-project",
              "decisionKind": "RejectSameIdentity",
              "leftEvidence": {
                "runId": "run-002",
                "occurrenceIndex": 9
              },
              "rightEvidence": {
                "runId": "run-003",
                "occurrenceIndex": 3
              },
              "reviewerAlias": "coordinator-a",
              "reason": "Different physical clashes"
            }
          ]
        }
        """;

        [Fact]
        public void Main_CreateIdentityGovernanceMode_WithFreshOutput_CreatesExactEmptyJson()
        {
            string tempDirectory = CreateTempDirectory();
            string outputPath = Path.Combine(tempDirectory, "identity-governance.json");

            try
            {
                var result = InvokeMain(
                    "create-identity-governance",
                    "--project-id", "coordination-project",
                    "-o", outputPath);

                Assert.Equal(0, result.ExitCode);
                Assert.Equal(string.Empty, result.StdErr);
                Assert.Equal(
                    $"Project: coordination-project\nDecisions: 0\nIdentity governance written to {outputPath}",
                    result.StdOut);
                Assert.Equal(EmptyGovernanceExpectedJson.Replace("\r\n", "\n") + "\n", File.ReadAllText(outputPath), StringComparer.Ordinal);
            }
            finally
            {
                DeleteDirectoryIfExists(tempDirectory);
            }
        }

        [Fact]
        public void Main_CreateIdentityGovernanceMode_ExistingOutput_IsRefused()
        {
            string tempDirectory = CreateTempDirectory();
            string outputPath = Path.Combine(tempDirectory, "identity-governance.json");

            try
            {
                File.WriteAllText(outputPath, "ORIGINAL", new UTF8Encoding(false));

                var result = InvokeMain(
                    "create-identity-governance",
                    "--project-id", "coordination-project",
                    "-o", outputPath);

                Assert.Equal(1, result.ExitCode);
                Assert.Equal(string.Empty, result.StdOut);
                Assert.Contains("Failed to create identity governance: Identity governance file already exists.", result.StdErr, StringComparison.Ordinal);
                Assert.DoesNotContain(outputPath, result.StdErr, StringComparison.OrdinalIgnoreCase);
                Assert.Equal("ORIGINAL", File.ReadAllText(outputPath), StringComparer.Ordinal);
            }
            finally
            {
                DeleteDirectoryIfExists(tempDirectory);
            }
        }

        [Fact]
        public void Main_CreateIdentityGovernanceMode_UnknownOptionLikeValueForProjectId_IsRejectedAsMissingValue()
        {
            string tempDirectory = CreateTempDirectory();
            string outputPath = Path.Combine(tempDirectory, "identity-governance.json");

            try
            {
                var result = InvokeMain(
                    "create-identity-governance",
                    "--project-id", "--unknown",
                    "-o", outputPath);

                Assert.Equal(1, result.ExitCode);
                Assert.Equal(string.Empty, result.StdOut);
                Assert.Contains("Missing value for '--project-id'.", result.StdErr, StringComparison.Ordinal);
                Assert.Contains(CreateIdentityGovernanceUsage, result.StdErr, StringComparison.Ordinal);
                Assert.False(File.Exists(outputPath));
            }
            finally
            {
                DeleteDirectoryIfExists(tempDirectory);
            }
        }

        [Fact]
        public void Main_CreateIdentityGovernanceMode_ShortOptionLikeValueForProjectId_IsRejectedAsMissingValue()
        {
            string tempDirectory = CreateTempDirectory();
            string outputPath = Path.Combine(tempDirectory, "identity-governance.json");

            try
            {
                var result = InvokeMain(
                    "create-identity-governance",
                    "--project-id", "-x",
                    "-o", outputPath);

                Assert.Equal(1, result.ExitCode);
                Assert.Equal(string.Empty, result.StdOut);
                Assert.Contains("Missing value for '--project-id'.", result.StdErr, StringComparison.Ordinal);
                Assert.Contains(CreateIdentityGovernanceUsage, result.StdErr, StringComparison.Ordinal);
                Assert.False(File.Exists(outputPath));
            }
            finally
            {
                DeleteDirectoryIfExists(tempDirectory);
            }
        }

        [Fact]
        public void Main_AppendIdentityDecisionMode_ConfirmDecision_AppendsAndWritesExactJson()
        {
            string tempDirectory = CreateTempDirectory();
            string governancePath = Path.Combine(tempDirectory, "identity-governance.json");

            try
            {
                SaveEmptyGovernance(governancePath, "coordination-project");

                var result = InvokeMain(
                    "append-identity-decision",
                    "--governance", governancePath,
                    "--decision-id", "decision-001",
                    "--decision-kind", "ConfirmSameIdentity",
                    "--left-run-id", "run-001",
                    "--left-occurrence-index", "4",
                    "--right-run-id", "run-002",
                    "--right-occurrence-index", "7",
                    "--persistent-identity-id", "identity-001",
                    "--reviewer-alias", "coordinator-a",
                    "--reason", "Confirmed from model context");

                Assert.Equal(0, result.ExitCode);
                Assert.Equal(string.Empty, result.StdErr);
                Assert.Equal(
                    $"Project: coordination-project\nDecision: decision-001\nDecision kind: ConfirmSameIdentity\nDecisions: 1\nIdentity governance updated: {governancePath}",
                    result.StdOut);
                Assert.Equal(ConfirmGovernanceExpectedJson.Replace("\r\n", "\n") + "\n", File.ReadAllText(governancePath), StringComparer.Ordinal);
            }
            finally
            {
                DeleteDirectoryIfExists(tempDirectory);
            }
        }

        [Fact]
        public void Main_AppendIdentityDecisionMode_RejectDecision_AppendsAndWritesExactJson()
        {
            string tempDirectory = CreateTempDirectory();
            string governancePath = Path.Combine(tempDirectory, "identity-governance.json");

            try
            {
                SaveEmptyGovernance(governancePath, "coordination-project");

                var result = InvokeMain(
                    "append-identity-decision",
                    "--governance", governancePath,
                    "--decision-id", "decision-002",
                    "--decision-kind", "RejectSameIdentity",
                    "--left-run-id", "run-002",
                    "--left-occurrence-index", "9",
                    "--right-run-id", "run-003",
                    "--right-occurrence-index", "3",
                    "--reviewer-alias", "coordinator-a",
                    "--reason", "Different physical clashes");

                Assert.Equal(0, result.ExitCode);
                Assert.Equal(string.Empty, result.StdErr);
                Assert.Equal(
                    $"Project: coordination-project\nDecision: decision-002\nDecision kind: RejectSameIdentity\nDecisions: 1\nIdentity governance updated: {governancePath}",
                    result.StdOut);
                Assert.Equal(RejectGovernanceExpectedJson.Replace("\r\n", "\n") + "\n", File.ReadAllText(governancePath), StringComparer.Ordinal);
            }
            finally
            {
                DeleteDirectoryIfExists(tempDirectory);
            }
        }

        [Fact]
        public void Main_AppendIdentityDecisionMode_PreservesPreviousDecisionsAndAppendsAtEnd()
        {
            string tempDirectory = CreateTempDirectory();
            string governancePath = Path.Combine(tempDirectory, "identity-governance.json");

            try
            {
                new JsonIdentityGovernanceSerializer().Save(BuildExistingGovernance(), governancePath);

                var result = InvokeMain(
                    "append-identity-decision",
                    "--governance", governancePath,
                    "--decision-id", "decision-002",
                    "--decision-kind", "RejectSameIdentity",
                    "--left-run-id", "run-002",
                    "--left-occurrence-index", "9",
                    "--right-run-id", "run-003",
                    "--right-occurrence-index", "3",
                    "--reviewer-alias", "coordinator-a",
                    "--reason", "Different physical clashes");

                Assert.Equal(0, result.ExitCode);

                IdentityGovernanceDocument loaded = new JsonIdentityGovernanceSerializer().Load(governancePath);
                Assert.Equal(2, loaded.Decisions.Count);
                Assert.Equal("decision-000", loaded.Decisions[0].DecisionId);
                Assert.Equal("decision-002", loaded.Decisions[1].DecisionId);
            }
            finally
            {
                DeleteDirectoryIfExists(tempDirectory);
            }
        }

        [Fact]
        public void Main_AppendIdentityDecisionMode_UsesProjectIdLoadedFromFile()
        {
            string tempDirectory = CreateTempDirectory();
            string governancePath = Path.Combine(tempDirectory, "identity-governance.json");

            try
            {
                SaveEmptyGovernance(governancePath, "loaded-project");

                var result = InvokeMain(
                    "append-identity-decision",
                    "--governance", governancePath,
                    "--decision-id", "decision-001",
                    "--decision-kind", "ConfirmSameIdentity",
                    "--left-run-id", "run-010",
                    "--left-occurrence-index", "1",
                    "--right-run-id", "run-011",
                    "--right-occurrence-index", "2",
                    "--persistent-identity-id", "identity-010",
                    "--reviewer-alias", "coordinator-a");

                Assert.Equal(0, result.ExitCode);
                Assert.Contains("Project: loaded-project", result.StdOut, StringComparison.Ordinal);

                IdentityGovernanceDocument loaded = new JsonIdentityGovernanceSerializer().Load(governancePath);
                Assert.Equal("loaded-project", loaded.ProjectId);
                Assert.Equal("loaded-project", loaded.Decisions[0].ProjectId);
            }
            finally
            {
                DeleteDirectoryIfExists(tempDirectory);
            }
        }

        [Fact]
        public void Main_AppendIdentityDecisionMode_ProjectIdOption_IsRejected()
        {
            var result = InvokeMain(
                "append-identity-decision",
                "--governance", "identity-governance.json",
                "--project-id", "coordination-project",
                "--decision-id", "decision-001",
                "--decision-kind", "ConfirmSameIdentity",
                "--left-run-id", "run-001",
                "--left-occurrence-index", "1",
                "--right-run-id", "run-002",
                "--right-occurrence-index", "2",
                "--persistent-identity-id", "identity-001",
                "--reviewer-alias", "coordinator-a");

            Assert.Equal(1, result.ExitCode);
            Assert.Equal(string.Empty, result.StdOut);
            Assert.Contains("Unrecognized append-identity-decision argument '--project-id'.", result.StdErr, StringComparison.Ordinal);
            Assert.Contains(AppendIdentityDecisionUsage, result.StdErr, StringComparison.Ordinal);
        }

        [Fact]
        public void Main_AppendIdentityDecisionMode_ConfirmWithoutPersistentIdentity_ReturnsUsageError()
        {
            var result = InvokeMain(
                "append-identity-decision",
                "--governance", "identity-governance.json",
                "--decision-id", "decision-001",
                "--decision-kind", "ConfirmSameIdentity",
                "--left-run-id", "run-001",
                "--left-occurrence-index", "1",
                "--right-run-id", "run-002",
                "--right-occurrence-index", "2",
                "--reviewer-alias", "coordinator-a");

            Assert.Equal(1, result.ExitCode);
            Assert.Equal(string.Empty, result.StdOut);
            Assert.Contains("Option '--persistent-identity-id' is required when '--decision-kind' is 'ConfirmSameIdentity'.", result.StdErr, StringComparison.Ordinal);
            Assert.Contains(AppendIdentityDecisionUsage, result.StdErr, StringComparison.Ordinal);
        }

        [Fact]
        public void Main_AppendIdentityDecisionMode_RejectWithPersistentIdentity_ReturnsUsageError()
        {
            var result = InvokeMain(
                "append-identity-decision",
                "--governance", "identity-governance.json",
                "--decision-id", "decision-001",
                "--decision-kind", "RejectSameIdentity",
                "--left-run-id", "run-001",
                "--left-occurrence-index", "1",
                "--right-run-id", "run-002",
                "--right-occurrence-index", "2",
                "--persistent-identity-id", "identity-001",
                "--reviewer-alias", "coordinator-a");

            Assert.Equal(1, result.ExitCode);
            Assert.Equal(string.Empty, result.StdOut);
            Assert.Contains("Option '--persistent-identity-id' is not allowed when '--decision-kind' is 'RejectSameIdentity'.", result.StdErr, StringComparison.Ordinal);
            Assert.Contains(AppendIdentityDecisionUsage, result.StdErr, StringComparison.Ordinal);
        }

        [Fact]
        public void Main_AppendIdentityDecisionMode_DifferentDecisionKindCasing_ReturnsUsageError()
        {
            var result = InvokeMain(
                "append-identity-decision",
                "--governance", "identity-governance.json",
                "--decision-id", "decision-001",
                "--decision-kind", "confirmsameidentity",
                "--left-run-id", "run-001",
                "--left-occurrence-index", "1",
                "--right-run-id", "run-002",
                "--right-occurrence-index", "2",
                "--persistent-identity-id", "identity-001",
                "--reviewer-alias", "coordinator-a");

            Assert.Equal(1, result.ExitCode);
            Assert.Equal(string.Empty, result.StdOut);
            Assert.Contains("Option '--decision-kind' must be 'ConfirmSameIdentity' or 'RejectSameIdentity'.", result.StdErr, StringComparison.Ordinal);
            Assert.Contains(AppendIdentityDecisionUsage, result.StdErr, StringComparison.Ordinal);
        }

        [Fact]
        public void Main_AppendIdentityDecisionMode_NegativeOccurrenceIndex_ReturnsUsageError()
        {
            var result = InvokeMain(
                "append-identity-decision",
                "--governance", "identity-governance.json",
                "--decision-id", "decision-001",
                "--decision-kind", "ConfirmSameIdentity",
                "--left-run-id", "run-001",
                "--left-occurrence-index", "-1",
                "--right-run-id", "run-002",
                "--right-occurrence-index", "2",
                "--persistent-identity-id", "identity-001",
                "--reviewer-alias", "coordinator-a");

            Assert.Equal(1, result.ExitCode);
            Assert.Equal(string.Empty, result.StdOut);
            Assert.Contains("Option '--left-occurrence-index' must be a non-negative decimal integer.", result.StdErr, StringComparison.Ordinal);
            Assert.DoesNotContain("Missing value for '--left-occurrence-index'.", result.StdErr, StringComparison.Ordinal);
            Assert.Contains(AppendIdentityDecisionUsage, result.StdErr, StringComparison.Ordinal);
        }

        [Fact]
        public void Main_AppendIdentityDecisionMode_NonNumericOccurrenceIndex_ReturnsUsageError()
        {
            var result = InvokeMain(
                "append-identity-decision",
                "--governance", "identity-governance.json",
                "--decision-id", "decision-001",
                "--decision-kind", "ConfirmSameIdentity",
                "--left-run-id", "run-001",
                "--left-occurrence-index", "1.5",
                "--right-run-id", "run-002",
                "--right-occurrence-index", "2",
                "--persistent-identity-id", "identity-001",
                "--reviewer-alias", "coordinator-a");

            Assert.Equal(1, result.ExitCode);
            Assert.Equal(string.Empty, result.StdOut);
            Assert.Contains("Option '--left-occurrence-index' must be a non-negative decimal integer.", result.StdErr, StringComparison.Ordinal);
            Assert.Contains(AppendIdentityDecisionUsage, result.StdErr, StringComparison.Ordinal);
        }

        [Fact]
        public void Main_AppendIdentityDecisionMode_OverflowOccurrenceIndex_ReturnsUsageError()
        {
            var result = InvokeMain(
                "append-identity-decision",
                "--governance", "identity-governance.json",
                "--decision-id", "decision-001",
                "--decision-kind", "ConfirmSameIdentity",
                "--left-run-id", "run-001",
                "--left-occurrence-index", "2147483648",
                "--right-run-id", "run-002",
                "--right-occurrence-index", "2",
                "--persistent-identity-id", "identity-001",
                "--reviewer-alias", "coordinator-a");

            Assert.Equal(1, result.ExitCode);
            Assert.Equal(string.Empty, result.StdOut);
            Assert.Contains("Option '--left-occurrence-index' must be a non-negative decimal integer.", result.StdErr, StringComparison.Ordinal);
            Assert.Contains(AppendIdentityDecisionUsage, result.StdErr, StringComparison.Ordinal);
        }

        [Fact]
        public void Main_AppendIdentityDecisionMode_UnknownOption_ReturnsUsageErrorWithoutStackTrace()
        {
            var result = InvokeMain(
                "append-identity-decision",
                "--governance", "identity-governance.json",
                "--decision-id", "decision-001",
                "--decision-kind", "ConfirmSameIdentity",
                "--left-run-id", "run-001",
                "--left-occurrence-index", "1",
                "--right-run-id", "run-002",
                "--right-occurrence-index", "2",
                "--persistent-identity-id", "identity-001",
                "--reviewer-alias", "coordinator-a",
                "--unknown", "value");

            Assert.Equal(1, result.ExitCode);
            Assert.Equal(string.Empty, result.StdOut);
            Assert.Contains("Unrecognized append-identity-decision argument '--unknown'.", result.StdErr, StringComparison.Ordinal);
            Assert.Contains(AppendIdentityDecisionUsage, result.StdErr, StringComparison.Ordinal);
            Assert.DoesNotContain("Unhandled exception", result.StdErr, StringComparison.Ordinal);
            Assert.DoesNotContain("   at ", result.StdErr, StringComparison.Ordinal);
        }

        [Fact]
        public void Main_AppendIdentityDecisionMode_DuplicateOption_ReturnsUsageError()
        {
            var result = InvokeMain(
                "append-identity-decision",
                "--governance", "first.json",
                "--governance", "second.json",
                "--decision-id", "decision-001",
                "--decision-kind", "ConfirmSameIdentity",
                "--left-run-id", "run-001",
                "--left-occurrence-index", "1",
                "--right-run-id", "run-002",
                "--right-occurrence-index", "2",
                "--persistent-identity-id", "identity-001",
                "--reviewer-alias", "coordinator-a");

            Assert.Equal(1, result.ExitCode);
            Assert.Equal(string.Empty, result.StdOut);
            Assert.Contains("Duplicate option '--governance'.", result.StdErr, StringComparison.Ordinal);
            Assert.Contains(AppendIdentityDecisionUsage, result.StdErr, StringComparison.Ordinal);
        }

        [Fact]
        public void Main_AppendIdentityDecisionMode_MissingRequiredField_ReturnsUsageError()
        {
            var result = InvokeMain(
                "append-identity-decision",
                "--governance", "identity-governance.json",
                "--decision-kind", "ConfirmSameIdentity",
                "--left-run-id", "run-001",
                "--left-occurrence-index", "1",
                "--right-run-id", "run-002",
                "--right-occurrence-index", "2",
                "--persistent-identity-id", "identity-001",
                "--reviewer-alias", "coordinator-a");

            Assert.Equal(1, result.ExitCode);
            Assert.Equal(string.Empty, result.StdOut);
            Assert.Contains("Missing required option '--decision-id'.", result.StdErr, StringComparison.Ordinal);
            Assert.Contains(AppendIdentityDecisionUsage, result.StdErr, StringComparison.Ordinal);
        }

        [Fact]
        public void Main_AppendIdentityDecisionMode_UnknownOptionLikeValueForReviewerAlias_IsRejectedWithoutTouchingFile()
        {
            string tempDirectory = CreateTempDirectory();
            string governancePath = Path.Combine(tempDirectory, "identity-governance.json");

            try
            {
                SaveEmptyGovernance(governancePath, "coordination-project");
                byte[] originalBytes = File.ReadAllBytes(governancePath);

                var result = InvokeMain(
                    "append-identity-decision",
                    "--governance", governancePath,
                    "--decision-id", "decision-001",
                    "--decision-kind", "ConfirmSameIdentity",
                    "--left-run-id", "run-001",
                    "--left-occurrence-index", "1",
                    "--right-run-id", "run-002",
                    "--right-occurrence-index", "2",
                    "--persistent-identity-id", "identity-001",
                    "--reviewer-alias", "--unknown");

                Assert.Equal(1, result.ExitCode);
                Assert.Equal(string.Empty, result.StdOut);
                Assert.Contains("Missing value for '--reviewer-alias'.", result.StdErr, StringComparison.Ordinal);
                Assert.Contains(AppendIdentityDecisionUsage, result.StdErr, StringComparison.Ordinal);
                Assert.Equal(originalBytes, File.ReadAllBytes(governancePath));
                AssertNoReplacementTempFiles(tempDirectory);
            }
            finally
            {
                DeleteDirectoryIfExists(tempDirectory);
            }
        }

        [Fact]
        public void Main_AppendIdentityDecisionMode_ShortOptionLikeValueForReviewerAlias_IsRejectedWithoutTouchingFile()
        {
            string tempDirectory = CreateTempDirectory();
            string governancePath = Path.Combine(tempDirectory, "identity-governance.json");

            try
            {
                SaveEmptyGovernance(governancePath, "coordination-project");
                byte[] originalBytes = File.ReadAllBytes(governancePath);

                var result = InvokeMain(
                    "append-identity-decision",
                    "--governance", governancePath,
                    "--decision-id", "decision-001",
                    "--decision-kind", "ConfirmSameIdentity",
                    "--left-run-id", "run-001",
                    "--left-occurrence-index", "1",
                    "--right-run-id", "run-002",
                    "--right-occurrence-index", "2",
                    "--persistent-identity-id", "identity-001",
                    "--reviewer-alias", "-x");

                Assert.Equal(1, result.ExitCode);
                Assert.Equal(string.Empty, result.StdOut);
                Assert.Contains("Missing value for '--reviewer-alias'.", result.StdErr, StringComparison.Ordinal);
                Assert.Contains(AppendIdentityDecisionUsage, result.StdErr, StringComparison.Ordinal);
                Assert.Equal(originalBytes, File.ReadAllBytes(governancePath));
                AssertNoReplacementTempFiles(tempDirectory);
            }
            finally
            {
                DeleteDirectoryIfExists(tempDirectory);
            }
        }

        [Fact]
        public void Main_AppendIdentityDecisionMode_UnknownOptionLikeValueForDecisionId_FailsBeforeLoadingOrReplacing()
        {
            string tempDirectory = CreateTempDirectory();
            string governancePath = Path.Combine(tempDirectory, "identity-governance.json");

            try
            {
                SaveEmptyGovernance(governancePath, "coordination-project");
                byte[] originalBytes = File.ReadAllBytes(governancePath);

                var result = InvokeMain(
                    "append-identity-decision",
                    "--governance", governancePath,
                    "--decision-id", "--unknown",
                    "--decision-kind", "ConfirmSameIdentity",
                    "--left-run-id", "run-001",
                    "--left-occurrence-index", "1",
                    "--right-run-id", "run-002",
                    "--right-occurrence-index", "2",
                    "--persistent-identity-id", "identity-001",
                    "--reviewer-alias", "coordinator-a");

                Assert.Equal(1, result.ExitCode);
                Assert.Equal(string.Empty, result.StdOut);
                Assert.Contains("Missing value for '--decision-id'.", result.StdErr, StringComparison.Ordinal);
                Assert.Contains(AppendIdentityDecisionUsage, result.StdErr, StringComparison.Ordinal);
                Assert.Equal(originalBytes, File.ReadAllBytes(governancePath));
                AssertNoReplacementTempFiles(tempDirectory);
            }
            finally
            {
                DeleteDirectoryIfExists(tempDirectory);
            }
        }

        [Fact]
        public void Main_AppendIdentityDecisionMode_UnknownOptionLikeValueForReason_IsRejectedWithoutTouchingFile()
        {
            string tempDirectory = CreateTempDirectory();
            string governancePath = Path.Combine(tempDirectory, "identity-governance.json");

            try
            {
                SaveEmptyGovernance(governancePath, "coordination-project");
                byte[] originalBytes = File.ReadAllBytes(governancePath);

                var result = InvokeMain(
                    "append-identity-decision",
                    "--governance", governancePath,
                    "--decision-id", "decision-001",
                    "--decision-kind", "ConfirmSameIdentity",
                    "--left-run-id", "run-001",
                    "--left-occurrence-index", "1",
                    "--right-run-id", "run-002",
                    "--right-occurrence-index", "2",
                    "--persistent-identity-id", "identity-001",
                    "--reviewer-alias", "coordinator-a",
                    "--reason", "--unknown");

                Assert.Equal(1, result.ExitCode);
                Assert.Equal(string.Empty, result.StdOut);
                Assert.Contains("Missing value for '--reason'.", result.StdErr, StringComparison.Ordinal);
                Assert.Contains(AppendIdentityDecisionUsage, result.StdErr, StringComparison.Ordinal);
                Assert.Equal(originalBytes, File.ReadAllBytes(governancePath));
                AssertNoReplacementTempFiles(tempDirectory);
            }
            finally
            {
                DeleteDirectoryIfExists(tempDirectory);
            }
        }

        [Fact]
        public void Main_AppendIdentityDecisionMode_DuplicateDecisionIdConflict_PreservesOriginalBytesAndLeavesNoTemporaryFile()
        {
            string tempDirectory = CreateTempDirectory();
            string governancePath = Path.Combine(tempDirectory, "identity-governance.json");

            try
            {
                new JsonIdentityGovernanceSerializer().Save(BuildExistingGovernance(), governancePath);
                byte[] originalBytes = File.ReadAllBytes(governancePath);

                var result = InvokeMain(
                    "append-identity-decision",
                    "--governance", governancePath,
                    "--decision-id", "decision-000",
                    "--decision-kind", "RejectSameIdentity",
                    "--left-run-id", "run-002",
                    "--left-occurrence-index", "9",
                    "--right-run-id", "run-003",
                    "--right-occurrence-index", "3",
                    "--reviewer-alias", "coordinator-a",
                    "--reason", "Different physical clashes");

                Assert.Equal(1, result.ExitCode);
                Assert.Equal(string.Empty, result.StdOut);
                Assert.Contains("Failed to append identity decision: Decision id 'decision-000' is duplicated.", result.StdErr, StringComparison.Ordinal);
                Assert.Equal(originalBytes, File.ReadAllBytes(governancePath));
                AssertNoReplacementTempFiles(tempDirectory);
            }
            finally
            {
                DeleteDirectoryIfExists(tempDirectory);
            }
        }

        [Fact]
        public void Main_AppendIdentityDecisionMode_DuplicateOrderedPairConflict_PreservesOriginalBytes()
        {
            string tempDirectory = CreateTempDirectory();
            string governancePath = Path.Combine(tempDirectory, "identity-governance.json");

            try
            {
                new JsonIdentityGovernanceSerializer().Save(BuildExistingGovernance(), governancePath);
                byte[] originalBytes = File.ReadAllBytes(governancePath);

                var result = InvokeMain(
                    "append-identity-decision",
                    "--governance", governancePath,
                    "--decision-id", "decision-002",
                    "--decision-kind", "RejectSameIdentity",
                    "--left-run-id", "run-001",
                    "--left-occurrence-index", "1",
                    "--right-run-id", "run-002",
                    "--right-occurrence-index", "2",
                    "--reviewer-alias", "coordinator-a");

                Assert.Equal(1, result.ExitCode);
                Assert.Equal(string.Empty, result.StdOut);
                Assert.Contains("Ordered evidence pair", result.StdErr, StringComparison.Ordinal);
                Assert.Equal(originalBytes, File.ReadAllBytes(governancePath));
            }
            finally
            {
                DeleteDirectoryIfExists(tempDirectory);
            }
        }

        [Fact]
        public void Main_CreateIdentityGovernanceMode_MissingProjectId_ReturnsUsageError()
        {
            var result = InvokeMain("create-identity-governance", "-o", "identity-governance.json");

            Assert.Equal(1, result.ExitCode);
            Assert.Equal(string.Empty, result.StdOut);
            Assert.Contains("Missing required option '--project-id'.", result.StdErr, StringComparison.Ordinal);
            Assert.Contains(CreateIdentityGovernanceUsage, result.StdErr, StringComparison.Ordinal);
        }

        private static MethodInfo ResolveMainMethod()
        {
            var programType = typeof(ConsoleAppLog).Assembly.GetType("OrzioClashReport.Cli.Program", throwOnError: true)!;
            var method = programType.GetMethod("Main", BindingFlags.Static | BindingFlags.NonPublic);
            Assert.NotNull(method);
            return method!;
        }

        private static (int ExitCode, string StdOut, string StdErr) InvokeMain(params string[] args)
        {
            TextWriter originalOut = Console.Out;
            TextWriter originalError = Console.Error;
            var stdoutWriter = new StringWriter();
            var stderrWriter = new StringWriter();

            try
            {
                Console.SetOut(stdoutWriter);
                Console.SetError(stderrWriter);

                object? result;
                try
                {
                    result = MainMethod.Invoke(null, new object[] { args });
                }
                catch (TargetInvocationException ex) when (ex.InnerException != null)
                {
                    throw ex.InnerException;
                }

                return (
                    result is int exitCode ? exitCode : throw new InvalidOperationException("Program.Main did not return an int."),
                    NormalizeLineEndings(stdoutWriter.ToString()),
                    NormalizeLineEndings(stderrWriter.ToString()));
            }
            finally
            {
                Console.SetOut(originalOut);
                Console.SetError(originalError);
                stdoutWriter.Dispose();
                stderrWriter.Dispose();
            }
        }

        private static void SaveEmptyGovernance(string outputPath, string projectId) =>
            new JsonIdentityGovernanceSerializer().Save(
                new IdentityGovernanceDocument(projectId, Array.Empty<HumanIdentityDecision>()),
                outputPath);

        private static IdentityGovernanceDocument BuildExistingGovernance() =>
            new IdentityGovernanceDocument(
                "coordination-project",
                new[]
                {
                    new HumanIdentityDecision(
                        "decision-000",
                        "coordination-project",
                        HumanIdentityDecisionKind.ConfirmSameIdentity,
                        new ClashEvidenceEndpoint("run-001", 1),
                        new ClashEvidenceEndpoint("run-002", 2),
                        "identity-000",
                        "coordinator-a",
                        "Confirmed from model context"),
                });

        private static void AssertNoReplacementTempFiles(string directoryPath)
        {
            string[] files = Directory.GetFiles(directoryPath, ".identity-governance-replace-*.tmp");
            Assert.Empty(files);
        }

        private static string NormalizeLineEndings(string value) =>
            value.ReplaceLineEndings("\n").TrimEnd('\n');

        private static string CreateTempDirectory()
        {
            string path = Path.Combine(Path.GetTempPath(), $"orzio-clash-report-tests-{Guid.NewGuid():N}");
            Directory.CreateDirectory(path);
            return path;
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
