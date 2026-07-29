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
    /// <summary>Tests for the standalone identity-governance review CLI command: strict parsing, read-only evidence gating, safe HTML output, and workspace immutability.</summary>
    [Collection("CliConsole")]
    public sealed class IdentityGovernanceReviewCliTests
    {
        private static readonly MethodInfo MainMethod = ResolveMainMethod();

        private const string Usage =
            "Usage: orzioclash render-identity-governance-report --project <project.json> --governance <identity-governance.json> (-o <identity-governance.html> | --output <identity-governance.html>)";

        private static string SamplesDirectory => Path.Combine(AppContext.BaseDirectory, "samples");

        private static string SampleClashXmlPath => Path.Combine(SamplesDirectory, "sample-clash.xml");

        private static string SampleClashManifestPath => Path.Combine(SamplesDirectory, "sample-clash.run-manifest.json");

        [Fact]
        public void Main_Help_ListsRenderIdentityGovernanceReportCommand()
        {
            var result = InvokeMain("--help");

            Assert.Equal(0, result.ExitCode);
            Assert.Contains("render-identity-governance-report", result.StdOut, StringComparison.Ordinal);
            Assert.Contains(
                "render-identity-governance-report Render one standalone HTML review of explicit human identity decisions.",
                result.StdOut,
                StringComparison.Ordinal);
        }

        [Fact]
        public void Main_MissingProjectOption_ReturnsUsageError()
        {
            var result = InvokeMain(
                "render-identity-governance-report",
                "--governance", "identity-governance.json",
                "-o", "review.html");

            Assert.Equal(1, result.ExitCode);
            Assert.Equal(string.Empty, result.StdOut);
            Assert.Contains("Missing required option '--project'.", result.StdErr, StringComparison.Ordinal);
            Assert.Contains(Usage, result.StdErr, StringComparison.Ordinal);
        }

        [Fact]
        public void Main_MissingGovernanceOption_ReturnsUsageError()
        {
            var result = InvokeMain(
                "render-identity-governance-report",
                "--project", "project.json",
                "-o", "review.html");

            Assert.Equal(1, result.ExitCode);
            Assert.Equal(string.Empty, result.StdOut);
            Assert.Contains("Missing required option '--governance'.", result.StdErr, StringComparison.Ordinal);
            Assert.Contains(Usage, result.StdErr, StringComparison.Ordinal);
        }

        [Fact]
        public void Main_MissingOutputOption_ReturnsUsageError()
        {
            var result = InvokeMain(
                "render-identity-governance-report",
                "--project", "project.json",
                "--governance", "identity-governance.json");

            Assert.Equal(1, result.ExitCode);
            Assert.Equal(string.Empty, result.StdOut);
            Assert.Contains("Missing required option '-o/--output'.", result.StdErr, StringComparison.Ordinal);
            Assert.Contains(Usage, result.StdErr, StringComparison.Ordinal);
        }

        [Fact]
        public void Main_DuplicateOutputAliases_ReturnUsageError()
        {
            var result = InvokeMain(
                "render-identity-governance-report",
                "--project", "project.json",
                "--governance", "identity-governance.json",
                "-o", "one.html",
                "--output", "two.html");

            Assert.Equal(1, result.ExitCode);
            Assert.Equal(string.Empty, result.StdOut);
            Assert.Contains("Duplicate option '-o/--output'.", result.StdErr, StringComparison.Ordinal);
            Assert.Contains(Usage, result.StdErr, StringComparison.Ordinal);
        }

        [Fact]
        public void Main_UnknownOrWrongCaseOption_ReturnsUsageError()
        {
            var result = InvokeMain(
                "render-identity-governance-report",
                "--Project", "project.json",
                "--governance", "identity-governance.json",
                "-o", "review.html");

            Assert.Equal(1, result.ExitCode);
            Assert.Equal(string.Empty, result.StdOut);
            Assert.Contains("Unrecognized render-identity-governance-report argument '--Project'.", result.StdErr, StringComparison.Ordinal);
            Assert.Contains(Usage, result.StdErr, StringComparison.Ordinal);
        }

        [Fact]
        public void Main_OptionLikeValue_ReturnsUsageError()
        {
            var result = InvokeMain(
                "render-identity-governance-report",
                "--project", "-suspicious",
                "--governance", "identity-governance.json",
                "-o", "review.html");

            Assert.Equal(1, result.ExitCode);
            Assert.Equal(string.Empty, result.StdOut);
            Assert.Contains("Missing value for '--project'.", result.StdErr, StringComparison.Ordinal);
            Assert.Contains(Usage, result.StdErr, StringComparison.Ordinal);
        }

        [Fact]
        public void Main_ExtraPositionalArgument_ReturnsUsageError()
        {
            var result = InvokeMain(
                "render-identity-governance-report",
                "--project", "project.json",
                "--governance", "identity-governance.json",
                "-o", "review.html",
                "extra");

            Assert.Equal(1, result.ExitCode);
            Assert.Equal(string.Empty, result.StdOut);
            Assert.Contains("Unrecognized render-identity-governance-report argument 'extra'.", result.StdErr, StringComparison.Ordinal);
            Assert.Contains(Usage, result.StdErr, StringComparison.Ordinal);
        }

        [Fact]
        public void Main_MissingProjectFile_FailsWithoutStackTrace()
        {
            var result = InvokeMain(
                "render-identity-governance-report",
                "--project", "missing-project.json",
                "--governance", "missing-governance.json",
                "-o", "review.html");

            Assert.Equal(1, result.ExitCode);
            Assert.Equal(string.Empty, result.StdOut);
            Assert.StartsWith("Failed to render identity governance review:", result.StdErr, StringComparison.Ordinal);
            Assert.DoesNotContain("   at ", result.StdErr, StringComparison.Ordinal);
        }

        [Fact]
        public void Main_ZeroDecisionGovernance_WritesReviewAndPreservesWorkspaceInputs()
        {
            string tempDirectory = CreateTempDirectory();
            try
            {
                Workspace workspace = CreateWorkspace(tempDirectory, snapshotCount: 3);
                SaveGovernance(workspace.GovernancePath, "coordination-project");

                byte[] projectBefore = File.ReadAllBytes(workspace.ProjectPath);
                byte[] indexBefore = File.ReadAllBytes(workspace.RunIndexPath);
                byte[] governanceBefore = File.ReadAllBytes(workspace.GovernancePath);
                byte[] longReportBefore = File.ReadAllBytes(workspace.LongitudinalReportPath);
                IReadOnlyDictionary<string, byte[]> snapshotsBefore = ReadAllSnapshotBytes(workspace.SnapshotPaths);

                var result = InvokeMain(
                    "render-identity-governance-report",
                    "--project", workspace.ProjectPath,
                    "--governance", workspace.GovernancePath,
                    "-o", workspace.ReviewReportPath);

                Assert.Equal(0, result.ExitCode);
                Assert.Equal(string.Empty, result.StdErr);
                Assert.Equal(
                    "Project: coordination-project\n"
                    + "Indexed runs: 3\n"
                    + "Decisions: 0\n"
                    + "Confirmations: 0\n"
                    + "Rejections: 0\n"
                    + "Evidence endpoints: 0\n"
                    + $"Identity governance review written to {Path.GetFullPath(workspace.ReviewReportPath)}",
                    result.StdOut);

                string html = File.ReadAllText(workspace.ReviewReportPath, Encoding.UTF8);
                Assert.Contains("No human identity decisions have been recorded.", html, StringComparison.Ordinal);
                Assert.DoesNotContain("SENTINEL-REVIEW", html, StringComparison.Ordinal);

                Assert.Equal(projectBefore, File.ReadAllBytes(workspace.ProjectPath));
                Assert.Equal(indexBefore, File.ReadAllBytes(workspace.RunIndexPath));
                Assert.Equal(governanceBefore, File.ReadAllBytes(workspace.GovernancePath));
                Assert.Equal(longReportBefore, File.ReadAllBytes(workspace.LongitudinalReportPath));
                AssertSnapshotBytesEqual(snapshotsBefore, workspace.SnapshotPaths);
                AssertNoDerivedHtmlTempFiles(tempDirectory);
            }
            finally
            {
                DeleteDirectoryIfExists(tempDirectory);
            }
        }

        [Fact]
        public void Main_ConfirmationAndRejection_RenderHtmlAndSafelyReplaceExistingOutput()
        {
            string tempDirectory = CreateTempDirectory();
            try
            {
                Workspace workspace = CreateWorkspace(tempDirectory, snapshotCount: 3);
                SaveGovernance(
                    workspace.GovernancePath,
                    "coordination-project",
                    IdentityGovernanceReviewTestData.Confirm(
                        "decision-001",
                        "run-001",
                        0,
                        "run-003",
                        2,
                        reviewerAlias: "coord<reviewer>",
                        reason: "<script>alert('x')</script>"),
                    IdentityGovernanceReviewTestData.Reject(
                        "decision-002",
                        "run-002",
                        1,
                        "run-003",
                        3,
                        reviewerAlias: "coord-b"));

                File.WriteAllText(workspace.ReviewReportPath, "SENTINEL-REVIEW", new UTF8Encoding(false));

                var result = InvokeMain(
                    "render-identity-governance-report",
                    "--project", workspace.ProjectPath,
                    "--governance", workspace.GovernancePath,
                    "--output", workspace.ReviewReportPath);

                Assert.Equal(0, result.ExitCode);
                Assert.Equal(string.Empty, result.StdErr);
                Assert.Contains("Decisions: 2", result.StdOut, StringComparison.Ordinal);
                Assert.Contains("Confirmations: 1", result.StdOut, StringComparison.Ordinal);
                Assert.Contains("Rejections: 1", result.StdOut, StringComparison.Ordinal);

                byte[] bytes = File.ReadAllBytes(workspace.ReviewReportPath);
                Assert.DoesNotContain((byte)'\r', bytes);
                Assert.False(bytes.Length >= 3 && bytes[0] == 0xEF && bytes[1] == 0xBB && bytes[2] == 0xBF);

                string html = File.ReadAllText(workspace.ReviewReportPath, Encoding.UTF8);
                Assert.DoesNotContain("SENTINEL-REVIEW", html, StringComparison.Ordinal);
                Assert.Contains("decision-001", html, StringComparison.Ordinal);
                Assert.Contains("decision-002", html, StringComparison.Ordinal);
                Assert.Contains("coord&lt;reviewer&gt;", html, StringComparison.Ordinal);
                Assert.Contains("&lt;script&gt;alert(&#39;x&#39;)&lt;/script&gt;", html, StringComparison.Ordinal);
                Assert.DoesNotContain("<script>alert('x')</script>", html, StringComparison.Ordinal);
                Assert.DoesNotContain("SENTINEL-LONGITUDINAL", html, StringComparison.Ordinal);
                Assert.Equal("SENTINEL-LONGITUDINAL", File.ReadAllText(workspace.LongitudinalReportPath), StringComparer.Ordinal);
                AssertNoDerivedHtmlTempFiles(tempDirectory);
            }
            finally
            {
                DeleteDirectoryIfExists(tempDirectory);
            }
        }

        [Fact]
        public void Main_InvalidGovernance_ProjectMismatch_DoesNotCreateOutput()
        {
            string tempDirectory = CreateTempDirectory();
            try
            {
                Workspace workspace = CreateWorkspace(tempDirectory, snapshotCount: 3);
                SaveGovernance(workspace.GovernancePath, "other-project");
                File.Delete(workspace.ReviewReportPath);

                var result = InvokeMain(
                    "render-identity-governance-report",
                    "--project", workspace.ProjectPath,
                    "--governance", workspace.GovernancePath,
                    "-o", workspace.ReviewReportPath);

                Assert.Equal(1, result.ExitCode);
                Assert.Equal(string.Empty, result.StdOut);
                Assert.Contains("Identity governance validation failed.", result.StdErr, StringComparison.Ordinal);
                Assert.Contains("1. Project id mismatch", result.StdErr, StringComparison.Ordinal);
                Assert.DoesNotContain(Usage, result.StdErr, StringComparison.Ordinal);
                Assert.False(File.Exists(workspace.ReviewReportPath));
                AssertNoDerivedHtmlTempFiles(tempDirectory);
            }
            finally
            {
                DeleteDirectoryIfExists(tempDirectory);
            }
        }

        [Fact]
        public void Main_InvalidGovernance_RunNotIndexed_PreservesExistingOutputByteIdentical()
        {
            string tempDirectory = CreateTempDirectory();
            try
            {
                Workspace workspace = CreateWorkspace(tempDirectory, snapshotCount: 2);
                SaveGovernance(
                    workspace.GovernancePath,
                    "coordination-project",
                    IdentityGovernanceReviewTestData.Confirm("decision-001", "missing-run", 0, "run-002", 1));

                byte[] existingBytes = Encoding.UTF8.GetBytes("ORIGINAL-REVIEW");
                File.WriteAllBytes(workspace.ReviewReportPath, existingBytes);

                var result = InvokeMain(
                    "render-identity-governance-report",
                    "--project", workspace.ProjectPath,
                    "--governance", workspace.GovernancePath,
                    "-o", workspace.ReviewReportPath);

                Assert.Equal(1, result.ExitCode);
                Assert.Equal(string.Empty, result.StdOut);
                Assert.Contains("Identity governance validation failed.", result.StdErr, StringComparison.Ordinal);
                Assert.Contains("not indexed", result.StdErr, StringComparison.Ordinal);
                Assert.Equal(existingBytes, File.ReadAllBytes(workspace.ReviewReportPath));
                AssertNoDerivedHtmlTempFiles(tempDirectory);
            }
            finally
            {
                DeleteDirectoryIfExists(tempDirectory);
            }
        }

        [Fact]
        public void Main_InvalidGovernance_OccurrenceIndexOutOfRange_ReturnsValidationIssues()
        {
            string tempDirectory = CreateTempDirectory();
            try
            {
                Workspace workspace = CreateWorkspace(tempDirectory, snapshotCount: 2);
                SaveGovernance(
                    workspace.GovernancePath,
                    "coordination-project",
                    IdentityGovernanceReviewTestData.Confirm("decision-001", "run-001", 999, "run-002", 1));

                var result = InvokeMain(
                    "render-identity-governance-report",
                    "--project", workspace.ProjectPath,
                    "--governance", workspace.GovernancePath,
                    "-o", workspace.ReviewReportPath);

                Assert.Equal(1, result.ExitCode);
                Assert.Equal(string.Empty, result.StdOut);
                Assert.Contains("Identity governance validation failed.", result.StdErr, StringComparison.Ordinal);
                Assert.Contains("999", result.StdErr, StringComparison.Ordinal);
            }
            finally
            {
                DeleteDirectoryIfExists(tempDirectory);
            }
        }

        [Fact]
        public void Main_InvalidGovernance_DuplicateRunId_ReturnsValidationIssues()
        {
            string tempDirectory = CreateTempDirectory();
            try
            {
                Workspace workspace = CreateDuplicateRunIdWorkspace(tempDirectory);
                SaveGovernance(workspace.GovernancePath, "coordination-project");

                var result = InvokeMain(
                    "render-identity-governance-report",
                    "--project", workspace.ProjectPath,
                    "--governance", workspace.GovernancePath,
                    "-o", workspace.ReviewReportPath);

                Assert.Equal(1, result.ExitCode);
                Assert.Equal(string.Empty, result.StdOut);
                Assert.Contains("duplicated", result.StdErr, StringComparison.Ordinal);
            }
            finally
            {
                DeleteDirectoryIfExists(tempDirectory);
            }
        }

        [Fact]
        public void Main_OutputCollisionWithProjectCatalog_IsRejectedWithoutMutatingInputs()
        {
            AssertCollisionFailure(
                outputPathSelector: workspace => workspace.ProjectPath,
                expectedMessageFragment: "same file as the project catalog");
        }

        [Fact]
        public void Main_OutputCollisionWithRunIndex_IsRejectedWithoutMutatingInputs()
        {
            AssertCollisionFailure(
                outputPathSelector: workspace => workspace.RunIndexPath,
                expectedMessageFragment: "same file as the run index");
        }

        [Fact]
        public void Main_OutputCollisionWithSnapshot_IsRejectedWithoutMutatingInputs()
        {
            AssertCollisionFailure(
                outputPathSelector: workspace => workspace.SnapshotPaths[0],
                expectedMessageFragment: "same file as snapshot 1");
        }

        [Fact]
        public void Main_OutputCollisionWithGovernance_IsRejectedWithoutMutatingInputs()
        {
            AssertCollisionFailure(
                outputPathSelector: workspace => workspace.GovernancePath,
                expectedMessageFragment: "same file as the identity governance document");
        }

        [Fact]
        public void Main_OutputCollisionWithLongitudinalReport_IsRejectedWithoutMutatingInputs()
        {
            AssertCollisionFailure(
                outputPathSelector: workspace => workspace.LongitudinalReportPath,
                expectedMessageFragment: "same file as the longitudinal report");
        }

        [Fact]
        public void Main_ExistingVersionCommand_RemainsUnaffected()
        {
            var result = InvokeMain("--version");

            Assert.Equal(0, result.ExitCode);
            Assert.Equal(string.Empty, result.StdErr);
            Assert.Contains("0.1.0-preview.3", result.StdOut, StringComparison.Ordinal);
        }

        private static void AssertCollisionFailure(Func<Workspace, string> outputPathSelector, string expectedMessageFragment)
        {
            string tempDirectory = CreateTempDirectory();
            try
            {
                Workspace workspace = CreateWorkspace(tempDirectory, snapshotCount: 3);
                SaveGovernance(workspace.GovernancePath, "coordination-project");

                byte[] projectBefore = File.ReadAllBytes(workspace.ProjectPath);
                byte[] indexBefore = File.ReadAllBytes(workspace.RunIndexPath);
                byte[] governanceBefore = File.ReadAllBytes(workspace.GovernancePath);
                byte[] longReportBefore = File.ReadAllBytes(workspace.LongitudinalReportPath);
                IReadOnlyDictionary<string, byte[]> snapshotsBefore = ReadAllSnapshotBytes(workspace.SnapshotPaths);

                var result = InvokeMain(
                    "render-identity-governance-report",
                    "--project", workspace.ProjectPath,
                    "--governance", workspace.GovernancePath,
                    "-o", outputPathSelector(workspace));

                Assert.Equal(1, result.ExitCode);
                Assert.Equal(string.Empty, result.StdOut);
                Assert.Contains(expectedMessageFragment, result.StdErr, StringComparison.Ordinal);
                Assert.Equal(projectBefore, File.ReadAllBytes(workspace.ProjectPath));
                Assert.Equal(indexBefore, File.ReadAllBytes(workspace.RunIndexPath));
                Assert.Equal(governanceBefore, File.ReadAllBytes(workspace.GovernancePath));
                Assert.Equal(longReportBefore, File.ReadAllBytes(workspace.LongitudinalReportPath));
                AssertSnapshotBytesEqual(snapshotsBefore, workspace.SnapshotPaths);
                AssertNoDerivedHtmlTempFiles(tempDirectory);
            }
            finally
            {
                DeleteDirectoryIfExists(tempDirectory);
            }
        }

        private static Workspace CreateWorkspace(string tempDirectory, int snapshotCount)
        {
            string projectDirectory = Path.Combine(tempDirectory, "project");
            var runIds = new string[snapshotCount];
            var snapshotPaths = new string[snapshotCount];
            for (int i = 0; i < snapshotCount; i++)
            {
                runIds[i] = $"run-{i + 1:000}";
                snapshotPaths[i] = Path.Combine(projectDirectory, "snapshots", $"run-{i + 1:000}.json");
                CreateFixtureSnapshotWithRunId(snapshotPaths[i], runIds[i]);
            }

            return FinishWorkspace(tempDirectory, snapshotPaths);
        }

        private static Workspace CreateDuplicateRunIdWorkspace(string tempDirectory)
        {
            string projectDirectory = Path.Combine(tempDirectory, "project");
            string[] snapshotPaths =
            {
                Path.Combine(projectDirectory, "snapshots", "run-001.json"),
                Path.Combine(projectDirectory, "snapshots", "run-002.json"),
            };

            CreateFixtureSnapshotWithRunId(snapshotPaths[0], "duplicate-run");
            CreateFixtureSnapshotWithRunId(snapshotPaths[1], "duplicate-run");
            return FinishWorkspace(tempDirectory, snapshotPaths);
        }

        private static Workspace FinishWorkspace(string tempDirectory, IReadOnlyList<string> snapshotPaths)
        {
            string projectPath = Path.Combine(tempDirectory, "project", "project.json");
            string runIndexPath = Path.Combine(tempDirectory, "project", "run-index.json");
            string longitudinalReportPath = Path.Combine(tempDirectory, "project", "reports", "longitudinal.html");
            string governancePath = Path.Combine(tempDirectory, "project", "identity-governance.json");
            string reviewReportPath = Path.Combine(tempDirectory, "project", "reports", "identity-governance.html");

            Directory.CreateDirectory(Path.GetDirectoryName(longitudinalReportPath)!);
            File.WriteAllText(longitudinalReportPath, "SENTINEL-LONGITUDINAL", new UTF8Encoding(false));
            File.WriteAllText(reviewReportPath, "SENTINEL-REVIEW", new UTF8Encoding(false));

            var indexResolver = new RunIndexSnapshotPathResolver();
            var references = new string[snapshotPaths.Count];
            for (int i = 0; i < snapshotPaths.Count; i++)
            {
                references[i] = indexResolver.CreateReference(runIndexPath, snapshotPaths[i]);
            }

            new JsonRunIndexSerializer().Save(new RunIndexDocument(references), runIndexPath);

            var catalogResolver = new ProjectCatalogPathResolver();
            string runIndexReference = catalogResolver.CreateReference(projectPath, runIndexPath);
            string reportReference = catalogResolver.CreateReference(projectPath, longitudinalReportPath);

            new JsonProjectCatalogSerializer().Save(
                new ProjectCatalogDocument("coordination-project", "Coordination Project", runIndexReference, reportReference),
                projectPath);

            return new Workspace(projectPath, runIndexPath, longitudinalReportPath, governancePath, reviewReportPath, snapshotPaths);
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

            string customManifestPath = Path.Combine(parentDirectory ?? Path.GetTempPath(), $"manifest-{Guid.NewGuid():N}.json");
            File.WriteAllText(customManifestPath, customManifestJson, new UTF8Encoding(false));

            var result = InvokeMain(
                "snapshot",
                "--xml", SampleClashXmlPath,
                "--manifest", customManifestPath,
                "-o", outputPath);

            Assert.Equal(0, result.ExitCode);
            Assert.Equal(string.Empty, result.StdErr);
            File.Delete(customManifestPath);
        }

        private static void SaveGovernance(string path, string projectId, params HumanIdentityDecision[] decisions) =>
            new JsonIdentityGovernanceSerializer().Save(new IdentityGovernanceDocument(projectId, decisions), path);

        private static IReadOnlyDictionary<string, byte[]> ReadAllSnapshotBytes(IReadOnlyList<string> snapshotPaths)
        {
            var result = new Dictionary<string, byte[]>(StringComparer.Ordinal);
            for (int i = 0; i < snapshotPaths.Count; i++)
            {
                result[snapshotPaths[i]] = File.ReadAllBytes(snapshotPaths[i]);
            }

            return result;
        }

        private static void AssertSnapshotBytesEqual(IReadOnlyDictionary<string, byte[]> expected, IReadOnlyList<string> snapshotPaths)
        {
            for (int i = 0; i < snapshotPaths.Count; i++)
            {
                Assert.Equal(expected[snapshotPaths[i]], File.ReadAllBytes(snapshotPaths[i]));
            }
        }

        private static void AssertNoDerivedHtmlTempFiles(string rootDirectory)
        {
            string[] tempFiles = Directory.GetFiles(rootDirectory, ".derived-html-report-*.tmp", SearchOption.AllDirectories);
            Assert.Empty(tempFiles);
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
            string path = Path.Combine(Path.GetTempPath(), $"orzio-identity-governance-review-cli-tests-{Guid.NewGuid():N}");
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

        private sealed class Workspace
        {
            public Workspace(
                string projectPath,
                string runIndexPath,
                string longitudinalReportPath,
                string governancePath,
                string reviewReportPath,
                IReadOnlyList<string> snapshotPaths)
            {
                ProjectPath = projectPath;
                RunIndexPath = runIndexPath;
                LongitudinalReportPath = longitudinalReportPath;
                GovernancePath = governancePath;
                ReviewReportPath = reviewReportPath;
                SnapshotPaths = snapshotPaths;
            }

            public string ProjectPath { get; }

            public string RunIndexPath { get; }

            public string LongitudinalReportPath { get; }

            public string GovernancePath { get; }

            public string ReviewReportPath { get; }

            public IReadOnlyList<string> SnapshotPaths { get; }
        }
    }
}
