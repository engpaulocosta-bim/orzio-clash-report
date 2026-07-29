using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using OrzioClashReport.Cli;
using OrzioClashReport.Core.Model;
using OrzioClashReport.Persistence.IdentityGovernanceJson;
using OrzioClashReport.Persistence.ProjectCatalogJson;
using OrzioClashReport.Persistence.RunIndexJson;

namespace OrzioClashReport.Tests
{
    /// <summary>
    /// Tests for the <c>validate-identity-governance</c> CLI command: read-only evidence validation of an
    /// explicit human identity-governance document against one project's indexed, immutable snapshots. Every
    /// scenario proves the command never writes, replaces, or creates any file.
    /// </summary>
    [Collection("CliConsole")]
    public sealed class IdentityGovernanceValidationCliTests
    {
        private static readonly MethodInfo MainMethod = ResolveMainMethod();

        private const string ValidateIdentityGovernanceUsage =
            "Usage: orzioclash validate-identity-governance --project <project.json> --governance <identity-governance.json>";

        private static string SamplesDirectory => Path.Combine(AppContext.BaseDirectory, "samples");

        private static string SampleClashXmlPath => Path.Combine(SamplesDirectory, "sample-clash.xml");

        private static string SampleClashManifestPath => Path.Combine(SamplesDirectory, "sample-clash.run-manifest.json");

        // ===================== parsing =====================

        [Fact]
        public void Main_Help_ListsValidateIdentityGovernanceCommand()
        {
            var result = InvokeMain("--help");

            Assert.Equal(0, result.ExitCode);
            Assert.Contains("validate-identity-governance", result.StdOut, StringComparison.Ordinal);
            Assert.Contains(
                "validate-identity-governance Validate explicit decisions against one project's indexed snapshots.",
                result.StdOut,
                StringComparison.Ordinal);
        }

        [Fact]
        public void Main_MissingProjectOption_ReturnsUsageError()
        {
            var result = InvokeMain("validate-identity-governance", "--governance", "identity-governance.json");

            Assert.Equal(1, result.ExitCode);
            Assert.Equal(string.Empty, result.StdOut);
            Assert.Contains("Missing required option '--project'.", result.StdErr, StringComparison.Ordinal);
            Assert.Contains(ValidateIdentityGovernanceUsage, result.StdErr, StringComparison.Ordinal);
        }

        [Fact]
        public void Main_MissingGovernanceOption_ReturnsUsageError()
        {
            var result = InvokeMain("validate-identity-governance", "--project", "project.json");

            Assert.Equal(1, result.ExitCode);
            Assert.Equal(string.Empty, result.StdOut);
            Assert.Contains("Missing required option '--governance'.", result.StdErr, StringComparison.Ordinal);
            Assert.Contains(ValidateIdentityGovernanceUsage, result.StdErr, StringComparison.Ordinal);
        }

        [Fact]
        public void Main_UnknownOption_ReturnsUsageError()
        {
            var result = InvokeMain(
                "validate-identity-governance",
                "--project", "project.json",
                "--governance", "identity-governance.json",
                "--unknown", "value");

            Assert.Equal(1, result.ExitCode);
            Assert.Equal(string.Empty, result.StdOut);
            Assert.Contains("Unrecognized validate-identity-governance argument '--unknown'.", result.StdErr, StringComparison.Ordinal);
        }

        [Fact]
        public void Main_DuplicateProjectOption_ReturnsUsageError()
        {
            var result = InvokeMain(
                "validate-identity-governance",
                "--project", "a.json",
                "--project", "b.json",
                "--governance", "identity-governance.json");

            Assert.Equal(1, result.ExitCode);
            Assert.Contains("Duplicate option '--project'.", result.StdErr, StringComparison.Ordinal);
        }

        [Fact]
        public void Main_DuplicateGovernanceOption_ReturnsUsageError()
        {
            var result = InvokeMain(
                "validate-identity-governance",
                "--project", "project.json",
                "--governance", "a.json",
                "--governance", "b.json");

            Assert.Equal(1, result.ExitCode);
            Assert.Contains("Duplicate option '--governance'.", result.StdErr, StringComparison.Ordinal);
        }

        [Fact]
        public void Main_OptionLikeValue_ReturnsUsageError()
        {
            var result = InvokeMain(
                "validate-identity-governance",
                "--project", "-suspicious-value",
                "--governance", "identity-governance.json");

            Assert.Equal(1, result.ExitCode);
            Assert.Contains("Missing value for '--project'.", result.StdErr, StringComparison.Ordinal);
        }

        [Fact]
        public void Main_ExtraPositionalArgument_ReturnsUsageError()
        {
            var result = InvokeMain(
                "validate-identity-governance",
                "--project", "project.json",
                "--governance", "identity-governance.json",
                "extra-positional");

            Assert.Equal(1, result.ExitCode);
            Assert.Contains("Unrecognized validate-identity-governance argument 'extra-positional'.", result.StdErr, StringComparison.Ordinal);
        }

        // ===================== load failure =====================

        [Fact]
        public void Main_MissingProjectFile_FailsWithoutStackTrace()
        {
            var result = InvokeMain(
                "validate-identity-governance",
                "--project", "does-not-exist-project.json",
                "--governance", "does-not-exist-governance.json");

            Assert.Equal(1, result.ExitCode);
            Assert.Equal(string.Empty, result.StdOut);
            Assert.StartsWith("Failed to validate identity governance:", result.StdErr, StringComparison.Ordinal);
            Assert.DoesNotContain("   at ", result.StdErr, StringComparison.Ordinal);
            Assert.DoesNotContain("StackTrace", result.StdErr, StringComparison.Ordinal);
            Assert.DoesNotContain("Exception", result.StdErr, StringComparison.Ordinal);
        }

        // ===================== valid workspaces =====================

        [Fact]
        public void Main_ValidWorkspace_ZeroDecisions_ReturnsZeroWithExactStdout()
        {
            string tempDirectory = CreateTempDirectory();
            try
            {
                var workspace = CreateWorkspace(tempDirectory, snapshotCount: 3);
                SaveGovernance(workspace.GovernancePath, "coordination-project");

                var result = InvokeMain(
                    "validate-identity-governance", "--project", workspace.ProjectPath, "--governance", workspace.GovernancePath);

                Assert.Equal(0, result.ExitCode);
                Assert.Equal(string.Empty, result.StdErr);
                string expected =
                    "Project: coordination-project\n"
                    + "Indexed runs: 3\n"
                    + "Decisions: 0\n"
                    + "Confirmations: 0\n"
                    + "Rejections: 0\n"
                    + "Evidence endpoints: 0\n"
                    + "Identity governance validation passed.";
                Assert.Equal(expected, result.StdOut);
            }
            finally
            {
                DeleteDirectoryIfExists(tempDirectory);
            }
        }

        [Fact]
        public void Main_ValidWorkspace_ConfirmationAndRejection_ReturnsZeroWithExactStdout()
        {
            string tempDirectory = CreateTempDirectory();
            try
            {
                var workspace = CreateWorkspace(tempDirectory, snapshotCount: 3);
                SaveGovernance(
                    workspace.GovernancePath,
                    "coordination-project",
                    Confirm("decision-1", "coordination-project", workspace.RunIds[0], 0, workspace.RunIds[1], 1),
                    Reject("decision-2", "coordination-project", workspace.RunIds[1], 2, workspace.RunIds[2], 3));

                var result = InvokeMain(
                    "validate-identity-governance", "--project", workspace.ProjectPath, "--governance", workspace.GovernancePath);

                Assert.Equal(0, result.ExitCode);
                Assert.Equal(string.Empty, result.StdErr);
                string expected =
                    "Project: coordination-project\n"
                    + "Indexed runs: 3\n"
                    + "Decisions: 2\n"
                    + "Confirmations: 1\n"
                    + "Rejections: 1\n"
                    + "Evidence endpoints: 4\n"
                    + "Identity governance validation passed.";
                Assert.Equal(expected, result.StdOut);
            }
            finally
            {
                DeleteDirectoryIfExists(tempDirectory);
            }
        }

        [Fact]
        public void Main_SingleSnapshotWorkspace_ReturnsZero()
        {
            string tempDirectory = CreateTempDirectory();
            try
            {
                var workspace = CreateWorkspace(tempDirectory, snapshotCount: 1);
                SaveGovernance(
                    workspace.GovernancePath,
                    "coordination-project",
                    Confirm("decision-1", "coordination-project", workspace.RunIds[0], 0, workspace.RunIds[0], 1));

                var result = InvokeMain(
                    "validate-identity-governance", "--project", workspace.ProjectPath, "--governance", workspace.GovernancePath);

                Assert.Equal(0, result.ExitCode);
                Assert.Contains("Indexed runs: 1", result.StdOut, StringComparison.Ordinal);
                Assert.Contains("Identity governance validation passed.", result.StdOut, StringComparison.Ordinal);
            }
            finally
            {
                DeleteDirectoryIfExists(tempDirectory);
            }
        }

        // ===================== invalid: project id mismatch =====================

        [Fact]
        public void Main_ProjectIdMismatch_ReturnsOneWithEmptyStdout()
        {
            string tempDirectory = CreateTempDirectory();
            try
            {
                var workspace = CreateWorkspace(tempDirectory, snapshotCount: 2);
                SaveGovernance(workspace.GovernancePath, "other-project");

                var result = InvokeMain(
                    "validate-identity-governance", "--project", workspace.ProjectPath, "--governance", workspace.GovernancePath);

                Assert.Equal(1, result.ExitCode);
                Assert.Equal(string.Empty, result.StdOut);
                Assert.Contains("Identity governance validation failed.", result.StdErr, StringComparison.Ordinal);
                Assert.Contains("Issues: 1", result.StdErr, StringComparison.Ordinal);
                Assert.Contains("1. Project id mismatch", result.StdErr, StringComparison.Ordinal);
            }
            finally
            {
                DeleteDirectoryIfExists(tempDirectory);
            }
        }

        // ===================== invalid: run not indexed =====================

        [Fact]
        public void Main_RunNotIndexed_ReturnsOne()
        {
            string tempDirectory = CreateTempDirectory();
            try
            {
                var workspace = CreateWorkspace(tempDirectory, snapshotCount: 2);
                SaveGovernance(
                    workspace.GovernancePath,
                    "coordination-project",
                    Confirm("decision-1", "coordination-project", "run-missing", 0, workspace.RunIds[1], 1));

                var result = InvokeMain(
                    "validate-identity-governance", "--project", workspace.ProjectPath, "--governance", workspace.GovernancePath);

                Assert.Equal(1, result.ExitCode);
                Assert.Equal(string.Empty, result.StdOut);
                Assert.Contains("Issues: 1", result.StdErr, StringComparison.Ordinal);
                Assert.Contains("run-missing", result.StdErr, StringComparison.Ordinal);
                Assert.Contains("not indexed", result.StdErr, StringComparison.Ordinal);
            }
            finally
            {
                DeleteDirectoryIfExists(tempDirectory);
            }
        }

        // ===================== invalid: occurrence index out of range =====================

        [Fact]
        public void Main_OccurrenceIndexOutOfRange_ReturnsOne()
        {
            string tempDirectory = CreateTempDirectory();
            try
            {
                var workspace = CreateWorkspace(tempDirectory, snapshotCount: 2);
                SaveGovernance(
                    workspace.GovernancePath,
                    "coordination-project",
                    Confirm("decision-1", "coordination-project", workspace.RunIds[0], 999, workspace.RunIds[1], 0));

                var result = InvokeMain(
                    "validate-identity-governance", "--project", workspace.ProjectPath, "--governance", workspace.GovernancePath);

                Assert.Equal(1, result.ExitCode);
                Assert.Equal(string.Empty, result.StdOut);
                Assert.Contains("Issues: 1", result.StdErr, StringComparison.Ordinal);
                Assert.Contains("999", result.StdErr, StringComparison.Ordinal);
            }
            finally
            {
                DeleteDirectoryIfExists(tempDirectory);
            }
        }

        // ===================== invalid: duplicate indexed run id =====================

        [Fact]
        public void Main_DuplicateIndexedRunId_ReturnsOne()
        {
            string tempDirectory = CreateTempDirectory();
            try
            {
                var workspace = CreateDuplicateRunIdWorkspace(tempDirectory);
                SaveGovernance(workspace.GovernancePath, "coordination-project");

                var result = InvokeMain(
                    "validate-identity-governance", "--project", workspace.ProjectPath, "--governance", workspace.GovernancePath);

                Assert.Equal(1, result.ExitCode);
                Assert.Equal(string.Empty, result.StdOut);
                Assert.Contains("Issues: 1", result.StdErr, StringComparison.Ordinal);
                Assert.Contains("duplicated", result.StdErr, StringComparison.Ordinal);
            }
            finally
            {
                DeleteDirectoryIfExists(tempDirectory);
            }
        }

        // ===================== ordering =====================

        [Fact]
        public void Main_MultipleIssues_AppearInDefinedOrder()
        {
            string tempDirectory = CreateTempDirectory();
            try
            {
                var workspace = CreateWorkspace(tempDirectory, snapshotCount: 2);
                SaveGovernance(
                    workspace.GovernancePath,
                    "other-project",
                    Confirm("decision-1", "other-project", "run-missing", 0, workspace.RunIds[1], 999));

                var result = InvokeMain(
                    "validate-identity-governance", "--project", workspace.ProjectPath, "--governance", workspace.GovernancePath);

                Assert.Equal(1, result.ExitCode);
                Assert.Equal(string.Empty, result.StdOut);
                Assert.Contains("Issues: 3", result.StdErr, StringComparison.Ordinal);

                int mismatchPosition = result.StdErr.IndexOf("1. Project id mismatch", StringComparison.Ordinal);
                int runNotIndexedPosition = result.StdErr.IndexOf("run-missing", StringComparison.Ordinal);
                int outOfRangePosition = result.StdErr.IndexOf("999", StringComparison.Ordinal);

                Assert.True(mismatchPosition >= 0);
                Assert.True(runNotIndexedPosition > mismatchPosition);
                Assert.True(outOfRangePosition > runNotIndexedPosition);
            }
            finally
            {
                DeleteDirectoryIfExists(tempDirectory);
            }
        }

        // ===================== read-only proof =====================

        [Fact]
        public void Main_ValidRun_DoesNotModifyAnyWorkspaceFileOrCreateNewFiles()
        {
            string tempDirectory = CreateTempDirectory();
            try
            {
                var workspace = CreateWorkspace(tempDirectory, snapshotCount: 2);
                SaveGovernance(
                    workspace.GovernancePath,
                    "coordination-project",
                    Confirm("decision-1", "coordination-project", workspace.RunIds[0], 0, workspace.RunIds[1], 1));

                var filesBefore = ListFiles(tempDirectory);
                var bytesBefore = ReadAllFileBytes(filesBefore);

                var result = InvokeMain(
                    "validate-identity-governance", "--project", workspace.ProjectPath, "--governance", workspace.GovernancePath);

                Assert.Equal(0, result.ExitCode);

                var filesAfter = ListFiles(tempDirectory);
                Assert.Equal(filesBefore, filesAfter);

                var bytesAfter = ReadAllFileBytes(filesAfter);
                foreach (var path in filesBefore)
                {
                    Assert.Equal(bytesBefore[path], bytesAfter[path]);
                }
            }
            finally
            {
                DeleteDirectoryIfExists(tempDirectory);
            }
        }

        [Fact]
        public void Main_InvalidRun_DoesNotModifyAnyWorkspaceFileOrCreateNewFiles()
        {
            string tempDirectory = CreateTempDirectory();
            try
            {
                var workspace = CreateWorkspace(tempDirectory, snapshotCount: 2);
                SaveGovernance(workspace.GovernancePath, "other-project");

                var filesBefore = ListFiles(tempDirectory);
                var bytesBefore = ReadAllFileBytes(filesBefore);

                var result = InvokeMain(
                    "validate-identity-governance", "--project", workspace.ProjectPath, "--governance", workspace.GovernancePath);

                Assert.Equal(1, result.ExitCode);

                var filesAfter = ListFiles(tempDirectory);
                Assert.Equal(filesBefore, filesAfter);

                var bytesAfter = ReadAllFileBytes(filesAfter);
                foreach (var path in filesBefore)
                {
                    Assert.Equal(bytesBefore[path], bytesAfter[path]);
                }
            }
            finally
            {
                DeleteDirectoryIfExists(tempDirectory);
            }
        }

        // ===================== existing commands unaffected =====================

        [Fact]
        public void Main_ExistingVersionCommand_RemainsUnaffected()
        {
            var result = InvokeMain("--version");

            Assert.Equal(0, result.ExitCode);
            Assert.Equal(string.Empty, result.StdErr);
            Assert.Contains("0.1.0-preview.3", result.StdOut, StringComparison.Ordinal);
        }

        [Fact]
        public void Main_ExistingSnapshotCommand_RemainsUnaffected()
        {
            var result = InvokeMain("snapshot");

            Assert.Equal(1, result.ExitCode);
            Assert.Contains("Missing required option '--xml'.", result.StdErr, StringComparison.Ordinal);
        }

        // ===================== helpers =====================

        private static HumanIdentityDecision Confirm(
            string decisionId, string projectId, string leftRunId, int leftIndex, string rightRunId, int rightIndex) =>
            new HumanIdentityDecision(
                decisionId,
                projectId,
                HumanIdentityDecisionKind.ConfirmSameIdentity,
                new ClashEvidenceEndpoint(leftRunId, leftIndex),
                new ClashEvidenceEndpoint(rightRunId, rightIndex),
                "identity-1",
                "coordinator-a",
                null);

        private static HumanIdentityDecision Reject(
            string decisionId, string projectId, string leftRunId, int leftIndex, string rightRunId, int rightIndex) =>
            new HumanIdentityDecision(
                decisionId,
                projectId,
                HumanIdentityDecisionKind.RejectSameIdentity,
                new ClashEvidenceEndpoint(leftRunId, leftIndex),
                new ClashEvidenceEndpoint(rightRunId, rightIndex),
                null,
                "coordinator-a",
                null);

        private static void SaveGovernance(string path, string projectId, params HumanIdentityDecision[] decisions) =>
            new JsonIdentityGovernanceSerializer().Save(new IdentityGovernanceDocument(projectId, decisions), path);

        private static ValidationWorkspace CreateWorkspace(string tempDirectory, int snapshotCount)
        {
            var runIds = new string[snapshotCount];
            var snapshotPaths = new string[snapshotCount];
            for (int i = 0; i < snapshotCount; i++)
            {
                runIds[i] = $"run-{i + 1:000}";
                snapshotPaths[i] = Path.Combine(tempDirectory, "snapshots", $"run-{i + 1:000}.json");
                CreateFixtureSnapshotWithRunId(snapshotPaths[i], runIds[i]);
            }

            return FinishWorkspace(tempDirectory, snapshotPaths, runIds);
        }

        private static ValidationWorkspace CreateDuplicateRunIdWorkspace(string tempDirectory)
        {
            string[] runIds = { "duplicate-run", "duplicate-run" };
            string[] snapshotPaths =
            {
                Path.Combine(tempDirectory, "snapshots", "run-001.json"),
                Path.Combine(tempDirectory, "snapshots", "run-002.json"),
            };

            CreateFixtureSnapshotWithRunId(snapshotPaths[0], runIds[0]);
            CreateFixtureSnapshotWithRunId(snapshotPaths[1], runIds[1]);

            return FinishWorkspace(tempDirectory, snapshotPaths, runIds);
        }

        private static ValidationWorkspace FinishWorkspace(string tempDirectory, string[] snapshotPaths, string[] runIds)
        {
            string projectPath = Path.Combine(tempDirectory, "project.json");
            string runIndexPath = Path.Combine(tempDirectory, "run-index.json");
            string reportPath = Path.Combine(tempDirectory, "reports", "longitudinal.html");
            string governancePath = Path.Combine(tempDirectory, "identity-governance.json");

            Directory.CreateDirectory(Path.GetDirectoryName(reportPath)!);
            File.WriteAllText(reportPath, "SENTINEL-REPORT", new UTF8Encoding(false));

            var indexResolver = new RunIndexSnapshotPathResolver();
            var references = new string[snapshotPaths.Length];
            for (int i = 0; i < snapshotPaths.Length; i++)
            {
                references[i] = indexResolver.CreateReference(runIndexPath, snapshotPaths[i]);
            }

            new JsonRunIndexSerializer().Save(new RunIndexDocument(references), runIndexPath);

            var catalogResolver = new ProjectCatalogPathResolver();
            string runIndexReference = catalogResolver.CreateReference(projectPath, runIndexPath);
            string reportReference = catalogResolver.CreateReference(projectPath, reportPath);

            new JsonProjectCatalogSerializer().Save(
                new ProjectCatalogDocument("coordination-project", "Coordination Project", runIndexReference, reportReference),
                projectPath);

            return new ValidationWorkspace(projectPath, runIndexPath, reportPath, governancePath, snapshotPaths, runIds);
        }

        private static void CreateFixtureSnapshotWithRunId(string outputPath, string runId)
        {
            string? parentDirectory = Path.GetDirectoryName(outputPath);
            if (!string.IsNullOrEmpty(parentDirectory))
            {
                Directory.CreateDirectory(parentDirectory);
            }

            string manifestJson = File.ReadAllText(SampleClashManifestPath, Encoding.UTF8);
            string customManifestJson = manifestJson.Replace(
                "\"runId\": \"coordination-sample-clash-xml\"",
                $"\"runId\": \"{runId}\"",
                StringComparison.Ordinal);
            Assert.NotEqual(manifestJson, customManifestJson);

            string customManifestPath = Path.Combine(
                string.IsNullOrEmpty(parentDirectory) ? Path.GetTempPath() : parentDirectory,
                $"manifest-{Guid.NewGuid():N}.json");
            File.WriteAllText(customManifestPath, customManifestJson, new UTF8Encoding(false));

            var result = InvokeMain(
                "snapshot",
                "--xml", SampleClashXmlPath,
                "--manifest", customManifestPath,
                "-o", outputPath);

            Assert.Equal(0, result.ExitCode);
            Assert.Equal(string.Empty, result.StdErr);
            Assert.True(File.Exists(outputPath));

            File.Delete(customManifestPath);
        }

        private static List<string> ListFiles(string directory) =>
            Directory.GetFiles(directory, "*", SearchOption.AllDirectories).OrderBy(f => f, StringComparer.Ordinal).ToList();

        private static Dictionary<string, byte[]> ReadAllFileBytes(IReadOnlyList<string> paths)
        {
            var result = new Dictionary<string, byte[]>(StringComparer.Ordinal);
            foreach (var path in paths)
            {
                result[path] = File.ReadAllBytes(path);
            }

            return result;
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

        private static string NormalizeLineEndings(string value) =>
            value.ReplaceLineEndings("\n").TrimEnd('\n');

        private static string CreateTempDirectory()
        {
            string path = Path.Combine(Path.GetTempPath(), $"orzio-identity-governance-validation-cli-tests-{Guid.NewGuid():N}");
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

        private sealed class ValidationWorkspace
        {
            public ValidationWorkspace(
                string projectPath,
                string runIndexPath,
                string reportPath,
                string governancePath,
                IReadOnlyList<string> snapshotPaths,
                IReadOnlyList<string> runIds)
            {
                ProjectPath = projectPath;
                RunIndexPath = runIndexPath;
                ReportPath = reportPath;
                GovernancePath = governancePath;
                SnapshotPaths = snapshotPaths;
                RunIds = runIds;
            }

            public string ProjectPath { get; }

            public string RunIndexPath { get; }

            public string ReportPath { get; }

            public string GovernancePath { get; }

            public IReadOnlyList<string> SnapshotPaths { get; }

            public IReadOnlyList<string> RunIds { get; }
        }
    }
}
