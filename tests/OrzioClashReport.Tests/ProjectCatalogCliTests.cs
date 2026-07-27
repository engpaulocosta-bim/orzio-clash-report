using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using OrzioClashReport.Cli;
using OrzioClashReport.Persistence.ProjectCatalogJson;
using OrzioClashReport.Persistence.RunIndexJson;

namespace OrzioClashReport.Tests
{
    [Collection("CliConsole")]
    public sealed class ProjectCatalogCliTests
    {
        private static readonly MethodInfo MainMethod = ResolveMainMethod();
        private const string CreateProjectUsage = "Usage: orzioclash create-project --project-id <project-id> --name <display-name> --index <run-index.json> --report <longitudinal.html> (-o <project.json> | --output <project.json>)";
        private const string RenderProjectUsage = "Usage: orzioclash render-project --project <project.json>";

        private static string SamplesDirectory => Path.Combine(AppContext.BaseDirectory, "samples");

        private static string SampleClashXmlPath => Path.Combine(SamplesDirectory, "sample-clash.xml");

        private static string SampleClashManifestPath => Path.Combine(SamplesDirectory, "sample-clash.run-manifest.json");

        [Fact]
        public void Main_CreateProjectMode_MissingProjectId_ReturnsUsageError()
        {
            var result = InvokeMain(
                "create-project",
                "--name", "Example Coordination Project",
                "--index", "run-index.json",
                "--report", "reports/longitudinal.html",
                "-o", "project.json");

            Assert.Equal(1, result.ExitCode);
            Assert.Equal(string.Empty, result.StdOut);
            Assert.Contains("Missing required option '--project-id'.", result.StdErr, StringComparison.Ordinal);
            Assert.Contains(CreateProjectUsage, result.StdErr, StringComparison.Ordinal);
        }

        [Fact]
        public void Main_CreateProjectMode_DuplicateOutputAliases_ReturnsUsageError()
        {
            var result = InvokeMain(
                "create-project",
                "--project-id", "example-project",
                "--name", "Example Coordination Project",
                "--index", "run-index.json",
                "--report", "reports/longitudinal.html",
                "-o", "first.json",
                "--output", "second.json");

            Assert.Equal(1, result.ExitCode);
            Assert.Equal(string.Empty, result.StdOut);
            Assert.Contains("Duplicate option '-o/--output'.", result.StdErr, StringComparison.Ordinal);
            Assert.Contains(CreateProjectUsage, result.StdErr, StringComparison.Ordinal);
        }

        [Fact]
        public void Main_CreateProjectMode_UnknownArgument_IsRejected()
        {
            var result = InvokeMain(
                "create-project",
                "--project-id", "example-project",
                "--name", "Example Coordination Project",
                "--index", "run-index.json",
                "--report", "reports/longitudinal.html",
                "--unknown", "value",
                "-o", "project.json");

            Assert.Equal(1, result.ExitCode);
            Assert.Equal(string.Empty, result.StdOut);
            Assert.Contains("Unrecognized create-project argument '--unknown'.", result.StdErr, StringComparison.Ordinal);
            Assert.Contains(CreateProjectUsage, result.StdErr, StringComparison.Ordinal);
        }

        [Fact]
        public void Main_CreateProjectMode_MissingIndexFile_FailsWithoutCreatingOutput()
        {
            string tempDirectory = CreateTempDirectory();
            string outputPath = Path.Combine(tempDirectory, "project", "project.json");
            string reportDirectory = Path.Combine(tempDirectory, "project", "reports");
            string reportPath = Path.Combine(reportDirectory, "longitudinal.html");
            string missingIndexPath = Path.Combine(tempDirectory, "project", "run-index.json");

            try
            {
                Directory.CreateDirectory(reportDirectory);

                var result = InvokeMain(
                    "create-project",
                    "--project-id", "example-project",
                    "--name", "Example Coordination Project",
                    "--index", missingIndexPath,
                    "--report", reportPath,
                    "-o", outputPath);

                Assert.Equal(1, result.ExitCode);
                Assert.Equal(string.Empty, result.StdOut);
                Assert.Contains("Failed to create project catalog:", result.StdErr, StringComparison.Ordinal);
                Assert.False(File.Exists(outputPath));
            }
            finally
            {
                DeleteDirectoryIfExists(tempDirectory);
            }
        }

        [Fact]
        public void Main_CreateProjectMode_InvalidIndex_FailsWithoutCreatingOutput()
        {
            string tempDirectory = CreateTempDirectory();
            string projectDirectory = Path.Combine(tempDirectory, "project");
            string outputPath = Path.Combine(projectDirectory, "project.json");
            string indexPath = Path.Combine(projectDirectory, "run-index.json");
            string reportDirectory = Path.Combine(projectDirectory, "reports");
            string reportPath = Path.Combine(reportDirectory, "longitudinal.html");

            try
            {
                Directory.CreateDirectory(reportDirectory);
                File.WriteAllText(indexPath, "{ \"schemaVersion\": 1, ", new UTF8Encoding(false));

                var result = InvokeMain(
                    "create-project",
                    "--project-id", "example-project",
                    "--name", "Example Coordination Project",
                    "--index", indexPath,
                    "--report", reportPath,
                    "-o", outputPath);

                Assert.Equal(1, result.ExitCode);
                Assert.Equal(string.Empty, result.StdOut);
                Assert.Contains("Failed to create project catalog:", result.StdErr, StringComparison.Ordinal);
                Assert.False(File.Exists(outputPath));
            }
            finally
            {
                DeleteDirectoryIfExists(tempDirectory);
            }
        }

        [Fact]
        public void Main_CreateProjectMode_RunIndexWithSingleSnapshot_FailsWithoutCreatingOutput()
        {
            string tempDirectory = CreateTempDirectory();
            string projectDirectory = Path.Combine(tempDirectory, "project");
            string snapshotPath = Path.Combine(projectDirectory, "snapshots", "run-001.json");
            string indexPath = Path.Combine(projectDirectory, "run-index.json");
            string reportDirectory = Path.Combine(projectDirectory, "reports");
            string reportPath = Path.Combine(reportDirectory, "longitudinal.html");
            string outputPath = Path.Combine(projectDirectory, "project.json");

            try
            {
                Directory.CreateDirectory(reportDirectory);
                CreateFixtureSnapshot(snapshotPath);
                SaveRunIndexWithSerializer(indexPath, snapshotPath);

                var result = InvokeMain(
                    "create-project",
                    "--project-id", "example-project",
                    "--name", "Example Coordination Project",
                    "--index", indexPath,
                    "--report", reportPath,
                    "-o", outputPath);

                Assert.Equal(1, result.ExitCode);
                Assert.Equal(string.Empty, result.StdOut);
                Assert.Contains("at least two snapshot references", result.StdErr, StringComparison.Ordinal);
                Assert.False(File.Exists(outputPath));
            }
            finally
            {
                DeleteDirectoryIfExists(tempDirectory);
            }
        }

        [Fact]
        public void Main_CreateProjectMode_InvalidSnapshot_FailsWithoutCreatingOutput()
        {
            string tempDirectory = CreateTempDirectory();
            string projectDirectory = Path.Combine(tempDirectory, "project");
            string firstSnapshotPath = Path.Combine(projectDirectory, "snapshots", "first.json");
            string invalidSnapshotPath = Path.Combine(projectDirectory, "snapshots", "invalid.json");
            string indexPath = Path.Combine(projectDirectory, "run-index.json");
            string reportDirectory = Path.Combine(projectDirectory, "reports");
            string reportPath = Path.Combine(reportDirectory, "longitudinal.html");
            string outputPath = Path.Combine(projectDirectory, "project.json");

            try
            {
                Directory.CreateDirectory(reportDirectory);
                CreateFixtureSnapshot(firstSnapshotPath);
                Directory.CreateDirectory(Path.GetDirectoryName(invalidSnapshotPath)!);
                File.WriteAllText(invalidSnapshotPath, "{ \"schemaVersion\": 1, ", new UTF8Encoding(false));
                SaveRunIndexWithSerializer(indexPath, firstSnapshotPath, invalidSnapshotPath);

                var result = InvokeMain(
                    "create-project",
                    "--project-id", "example-project",
                    "--name", "Example Coordination Project",
                    "--index", indexPath,
                    "--report", reportPath,
                    "-o", outputPath);

                Assert.Equal(1, result.ExitCode);
                Assert.Equal(string.Empty, result.StdOut);
                Assert.Contains("Failed to create project catalog:", result.StdErr, StringComparison.Ordinal);
                Assert.False(File.Exists(outputPath));
            }
            finally
            {
                DeleteDirectoryIfExists(tempDirectory);
            }
        }

        [Fact]
        public void Main_CreateProjectMode_TargetOutsideProjectDirectory_FailsWithoutCreatingOutput()
        {
            string tempDirectory = CreateTempDirectory();
            string projectDirectory = Path.Combine(tempDirectory, "project");
            string externalDirectory = Path.Combine(tempDirectory, "external");
            string firstSnapshotPath = Path.Combine(externalDirectory, "snapshots", "first.json");
            string secondSnapshotPath = Path.Combine(externalDirectory, "snapshots", "second.json");
            string indexPath = Path.Combine(externalDirectory, "run-index.json");
            string reportDirectory = Path.Combine(projectDirectory, "reports");
            string reportPath = Path.Combine(reportDirectory, "longitudinal.html");
            string outputPath = Path.Combine(projectDirectory, "project.json");

            try
            {
                Directory.CreateDirectory(reportDirectory);
                CreateFixtureSnapshot(firstSnapshotPath);
                CreateFixtureSnapshot(secondSnapshotPath);
                SaveRunIndexWithSerializer(indexPath, firstSnapshotPath, secondSnapshotPath);

                var result = InvokeMain(
                    "create-project",
                    "--project-id", "example-project",
                    "--name", "Example Coordination Project",
                    "--index", indexPath,
                    "--report", reportPath,
                    "-o", outputPath);

                Assert.Equal(1, result.ExitCode);
                Assert.Equal(string.Empty, result.StdOut);
                Assert.Contains("must be inside project catalog directory", result.StdErr, StringComparison.Ordinal);
                Assert.False(File.Exists(outputPath));
            }
            finally
            {
                DeleteDirectoryIfExists(tempDirectory);
            }
        }

        [Fact]
        public void Main_CreateProjectMode_ReportParentMissing_FailsWithoutCreatingOutput()
        {
            string tempDirectory = CreateTempDirectory();
            string projectDirectory = Path.Combine(tempDirectory, "project");
            string firstSnapshotPath = Path.Combine(projectDirectory, "snapshots", "first.json");
            string secondSnapshotPath = Path.Combine(projectDirectory, "snapshots", "second.json");
            string indexPath = Path.Combine(projectDirectory, "run-index.json");
            string reportPath = Path.Combine(projectDirectory, "missing-reports", "longitudinal.html");
            string outputPath = Path.Combine(projectDirectory, "project.json");

            try
            {
                CreateFixtureSnapshot(firstSnapshotPath);
                CreateFixtureSnapshot(secondSnapshotPath);
                SaveRunIndexWithSerializer(indexPath, firstSnapshotPath, secondSnapshotPath);

                var result = InvokeMain(
                    "create-project",
                    "--project-id", "example-project",
                    "--name", "Example Coordination Project",
                    "--index", indexPath,
                    "--report", reportPath,
                    "-o", outputPath);

                Assert.Equal(1, result.ExitCode);
                Assert.Equal(string.Empty, result.StdOut);
                Assert.Contains("Project report parent directory not found", result.StdErr, StringComparison.Ordinal);
                Assert.False(File.Exists(outputPath));
            }
            finally
            {
                DeleteDirectoryIfExists(tempDirectory);
            }
        }

        [Fact]
        public void Main_CreateProjectMode_ExistingOutput_IsRefusedAndPreservesOriginalBytes()
        {
            string tempDirectory = CreateTempDirectory();
            string projectDirectory = Path.Combine(tempDirectory, "project");
            string firstSnapshotPath = Path.Combine(projectDirectory, "snapshots", "first.json");
            string secondSnapshotPath = Path.Combine(projectDirectory, "snapshots", "second.json");
            string indexPath = Path.Combine(projectDirectory, "run-index.json");
            string reportDirectory = Path.Combine(projectDirectory, "reports");
            string reportPath = Path.Combine(reportDirectory, "longitudinal.html");
            string outputPath = Path.Combine(projectDirectory, "project.json");

            try
            {
                Directory.CreateDirectory(reportDirectory);
                CreateFixtureSnapshot(firstSnapshotPath);
                CreateFixtureSnapshot(secondSnapshotPath);
                SaveRunIndexWithSerializer(indexPath, firstSnapshotPath, secondSnapshotPath);
                File.WriteAllText(outputPath, "ORIGINAL-PROJECT-CATALOG", new UTF8Encoding(false));

                var result = InvokeMain(
                    "create-project",
                    "--project-id", "example-project",
                    "--name", "Example Coordination Project",
                    "--index", indexPath,
                    "--report", reportPath,
                    "-o", outputPath);

                Assert.Equal(1, result.ExitCode);
                Assert.Equal(string.Empty, result.StdOut);
                Assert.Contains("Project catalog file already exists:", result.StdErr, StringComparison.Ordinal);
                Assert.Equal("ORIGINAL-PROJECT-CATALOG", File.ReadAllText(outputPath), StringComparer.Ordinal);
            }
            finally
            {
                DeleteDirectoryIfExists(tempDirectory);
            }
        }

        [Fact]
        public void Main_CreateProjectMode_WithValidInputs_CreatesCanonicalJsonAndExactStdout()
        {
            string tempDirectory = CreateTempDirectory();
            string projectDirectory = Path.Combine(tempDirectory, "project");
            string firstSnapshotPath = Path.Combine(projectDirectory, "snapshots", "first.json");
            string secondSnapshotPath = Path.Combine(projectDirectory, "snapshots", "second.json");
            string indexPath = Path.Combine(projectDirectory, "run-index.json");
            string reportDirectory = Path.Combine(projectDirectory, "reports");
            string reportPath = Path.Combine(reportDirectory, "longitudinal.html");
            string outputPath = Path.Combine(projectDirectory, "project.json");

            try
            {
                Directory.CreateDirectory(reportDirectory);
                CreateFixtureSnapshot(firstSnapshotPath);
                CreateFixtureSnapshot(secondSnapshotPath);
                SaveRunIndexWithSerializer(indexPath, firstSnapshotPath, secondSnapshotPath);

                var result = InvokeMain(
                    "create-project",
                    "--project-id", "example-project",
                    "--name", "Example Coordination Project",
                    "--index", indexPath,
                    "--report", reportPath,
                    "-o", outputPath);

                Assert.Equal(0, result.ExitCode);
                Assert.Equal(string.Empty, result.StdErr);
                Assert.Equal(
                    $"Project: example-project\nIndexed snapshots: 2\nProject catalog written to {outputPath}",
                    result.StdOut);

                string expectedJson = """
                {
                  "schemaVersion": 1,
                  "projectId": "example-project",
                  "displayName": "Example Coordination Project",
                  "runIndexPath": "run-index.json",
                  "longitudinalReportPath": "reports/longitudinal.html"
                }
                """.Replace("\r\n", "\n") + "\n";

                Assert.Equal(expectedJson, File.ReadAllText(outputPath), StringComparer.Ordinal);
            }
            finally
            {
                DeleteDirectoryIfExists(tempDirectory);
            }
        }

        [Fact]
        public void Main_RenderProjectMode_MissingProjectOption_ReturnsUsageError()
        {
            var result = InvokeMain("render-project");

            Assert.Equal(1, result.ExitCode);
            Assert.Equal(string.Empty, result.StdOut);
            Assert.Contains("Missing required option '--project'.", result.StdErr, StringComparison.Ordinal);
            Assert.Contains(RenderProjectUsage, result.StdErr, StringComparison.Ordinal);
        }

        [Fact]
        public void Main_RenderProjectMode_InvalidProjectJson_FailsWithoutStdout()
        {
            string tempDirectory = CreateTempDirectory();
            string projectPath = Path.Combine(tempDirectory, "project.json");

            try
            {
                File.WriteAllText(projectPath, "{ \"schemaVersion\": 1, ", new UTF8Encoding(false));

                var result = InvokeMain("render-project", "--project", projectPath);

                Assert.Equal(1, result.ExitCode);
                Assert.Equal(string.Empty, result.StdOut);
                Assert.Contains("Failed to render project:", result.StdErr, StringComparison.Ordinal);
            }
            finally
            {
                DeleteDirectoryIfExists(tempDirectory);
            }
        }

        [Fact]
        public void Main_RenderProjectMode_MissingSnapshotOrReportParent_FailsWithoutPartialStdout()
        {
            string tempDirectory = CreateTempDirectory();
            string projectDirectory = Path.Combine(tempDirectory, "project");
            string missingSnapshotPath = Path.Combine(projectDirectory, "snapshots", "missing.json");
            string secondSnapshotPath = Path.Combine(projectDirectory, "snapshots", "second.json");
            string indexPath = Path.Combine(projectDirectory, "run-index.json");
            string projectPath = Path.Combine(projectDirectory, "project.json");

            try
            {
                CreateFixtureSnapshot(secondSnapshotPath);
                SaveRunIndexWithSerializer(indexPath, missingSnapshotPath, secondSnapshotPath);

                var serializer = new JsonProjectCatalogSerializer();
                serializer.Save(
                    new ProjectCatalogDocument(
                        "example-project",
                        "Example Coordination Project",
                        "run-index.json",
                        "reports/longitudinal.html"),
                    projectPath);

                var result = InvokeMain("render-project", "--project", projectPath);

                Assert.Equal(1, result.ExitCode);
                Assert.Equal(string.Empty, result.StdOut);
                Assert.Contains("Failed to render project:", result.StdErr, StringComparison.Ordinal);
            }
            finally
            {
                DeleteDirectoryIfExists(tempDirectory);
            }
        }

        [Fact]
        public void Main_RenderProjectMode_ReusesCompareIndexPipelineAndPreservesEvidenceFiles()
        {
            string tempDirectory = CreateTempDirectory();
            string projectDirectory = Path.Combine(tempDirectory, "project");
            string firstSnapshotPath = Path.Combine(projectDirectory, "snapshots", "first.json");
            string secondSnapshotPath = Path.Combine(projectDirectory, "snapshots", "second.json");
            string thirdSnapshotPath = Path.Combine(projectDirectory, "snapshots", "third.json");
            string indexPath = Path.Combine(projectDirectory, "run-index.json");
            string reportDirectory = Path.Combine(projectDirectory, "reports");
            string reportPath = Path.Combine(reportDirectory, "longitudinal.html");
            string projectPath = Path.Combine(projectDirectory, "project.json");

            try
            {
                Directory.CreateDirectory(reportDirectory);
                CreateFixtureSnapshot(firstSnapshotPath);
                CreateFixtureSnapshot(secondSnapshotPath);
                CreateFixtureSnapshot(thirdSnapshotPath);
                SaveRunIndexWithSerializer(indexPath, firstSnapshotPath, secondSnapshotPath, thirdSnapshotPath);

                var createProjectResult = InvokeMain(
                    "create-project",
                    "--project-id", "example-project",
                    "--name", "Example Coordination Project",
                    "--index", indexPath,
                    "--report", reportPath,
                    "-o", projectPath);

                Assert.Equal(0, createProjectResult.ExitCode);

                byte[] projectBefore = File.ReadAllBytes(projectPath);
                byte[] indexBefore = File.ReadAllBytes(indexPath);
                byte[] snapshotOneBefore = File.ReadAllBytes(firstSnapshotPath);
                byte[] snapshotTwoBefore = File.ReadAllBytes(secondSnapshotPath);
                byte[] snapshotThreeBefore = File.ReadAllBytes(thirdSnapshotPath);

                File.WriteAllText(reportPath, "SENTINEL-OLD-REPORT", new UTF8Encoding(false));

                var compareIndexResult = InvokeMain("compare-index", "--index", indexPath, "-o", reportPath);
                string compareIndexStdOut = compareIndexResult.StdOut;
                string compareIndexHtml = File.ReadAllText(reportPath);

                File.WriteAllText(reportPath, "SENTINEL-OLD-REPORT", new UTF8Encoding(false));

                var renderProjectResult = InvokeMain("render-project", "--project", projectPath);
                string renderProjectHtml = File.ReadAllText(reportPath);

                Assert.Equal(0, compareIndexResult.ExitCode);
                Assert.Equal(0, renderProjectResult.ExitCode);
                Assert.Equal(string.Empty, compareIndexResult.StdErr);
                Assert.Equal(string.Empty, renderProjectResult.StdErr);
                Assert.Equal(compareIndexStdOut, renderProjectResult.StdOut, StringComparer.Ordinal);
                Assert.Equal(compareIndexHtml, renderProjectHtml, StringComparer.Ordinal);
                Assert.DoesNotContain("SENTINEL-OLD-REPORT", renderProjectHtml, StringComparison.Ordinal);

                string secondRenderHtmlBefore = renderProjectHtml;
                var secondRenderProjectResult = InvokeMain("render-project", "--project", projectPath);
                string secondRenderHtml = File.ReadAllText(reportPath);

                Assert.Equal(0, secondRenderProjectResult.ExitCode);
                Assert.Equal(renderProjectResult.StdOut, secondRenderProjectResult.StdOut, StringComparer.Ordinal);
                Assert.Equal(secondRenderHtmlBefore, secondRenderHtml, StringComparer.Ordinal);

                Assert.Equal(projectBefore, File.ReadAllBytes(projectPath));
                Assert.Equal(indexBefore, File.ReadAllBytes(indexPath));
                Assert.Equal(snapshotOneBefore, File.ReadAllBytes(firstSnapshotPath));
                Assert.Equal(snapshotTwoBefore, File.ReadAllBytes(secondSnapshotPath));
                Assert.Equal(snapshotThreeBefore, File.ReadAllBytes(thirdSnapshotPath));
            }
            finally
            {
                DeleteDirectoryIfExists(tempDirectory);
            }
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

        private static void CreateFixtureSnapshot(string outputPath)
        {
            string? parentDirectory = Path.GetDirectoryName(outputPath);
            if (!string.IsNullOrEmpty(parentDirectory))
            {
                Directory.CreateDirectory(parentDirectory);
            }

            var result = InvokeMain(
                "snapshot",
                "--xml", SampleClashXmlPath,
                "--manifest", SampleClashManifestPath,
                "-o", outputPath);

            Assert.Equal(0, result.ExitCode);
            Assert.Equal(string.Empty, result.StdErr);
            Assert.True(File.Exists(outputPath));
        }

        private static void SaveRunIndexWithSerializer(string outputPath, params string[] snapshotPaths)
        {
            string? parentDirectory = Path.GetDirectoryName(outputPath);
            if (!string.IsNullOrEmpty(parentDirectory))
            {
                Directory.CreateDirectory(parentDirectory);
            }

            var resolver = new RunIndexSnapshotPathResolver();
            var references = new string[snapshotPaths.Length];
            for (int i = 0; i < snapshotPaths.Length; i++)
            {
                references[i] = resolver.CreateReference(outputPath, snapshotPaths[i]);
            }

            new JsonRunIndexSerializer().Save(new RunIndexDocument(references), outputPath);
        }

        private static string CreateTempDirectory()
        {
            string path = Path.Combine(Path.GetTempPath(), $"orzio-project-catalog-cli-tests-{Guid.NewGuid():N}");
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

        private static string NormalizeLineEndings(string value) =>
            value.ReplaceLineEndings("\n").TrimEnd('\n');
    }
}
