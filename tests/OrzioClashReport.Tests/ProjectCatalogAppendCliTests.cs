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
    public sealed class ProjectCatalogAppendCliTests
    {
        private static readonly MethodInfo MainMethod = ResolveMainMethod();

        private const string AppendProjectSnapshotUsage =
            "Usage: orzioclash append-project-snapshot --project <project.json> --snapshot <run-snapshot.json>";

        private static string SamplesDirectory => Path.Combine(AppContext.BaseDirectory, "samples");

        private static string SampleClashXmlPath => Path.Combine(SamplesDirectory, "sample-clash.xml");

        private static string SampleClashManifestPath => Path.Combine(SamplesDirectory, "sample-clash.run-manifest.json");

        [Fact]
        public void Main_AppendProjectSnapshotMode_MissingProject_ReturnsUsageError()
        {
            var result = InvokeMain("append-project-snapshot", "--snapshot", "snapshots/run-003.json");

            Assert.Equal(1, result.ExitCode);
            Assert.Equal(string.Empty, result.StdOut);
            Assert.Contains("Missing required option '--project'.", result.StdErr, StringComparison.Ordinal);
            Assert.Contains(AppendProjectSnapshotUsage, result.StdErr, StringComparison.Ordinal);
        }

        [Fact]
        public void Main_AppendProjectSnapshotMode_MissingSnapshot_ReturnsUsageError()
        {
            var result = InvokeMain("append-project-snapshot", "--project", "project.json");

            Assert.Equal(1, result.ExitCode);
            Assert.Equal(string.Empty, result.StdOut);
            Assert.Contains("Missing required option '--snapshot'.", result.StdErr, StringComparison.Ordinal);
            Assert.Contains(AppendProjectSnapshotUsage, result.StdErr, StringComparison.Ordinal);
        }

        [Fact]
        public void Main_AppendProjectSnapshotMode_DuplicateProjectOption_ReturnsUsageError()
        {
            var result = InvokeMain(
                "append-project-snapshot",
                "--project", "first-project.json",
                "--project", "second-project.json",
                "--snapshot", "snapshots/run-003.json");

            Assert.Equal(1, result.ExitCode);
            Assert.Equal(string.Empty, result.StdOut);
            Assert.Contains("Duplicate option '--project'.", result.StdErr, StringComparison.Ordinal);
            Assert.Contains(AppendProjectSnapshotUsage, result.StdErr, StringComparison.Ordinal);
        }

        [Fact]
        public void Main_AppendProjectSnapshotMode_UnknownArgument_ReturnsUsageError()
        {
            var result = InvokeMain(
                "append-project-snapshot",
                "--project", "project.json",
                "--snapshot", "snapshots/run-003.json",
                "--unknown", "value");

            Assert.Equal(1, result.ExitCode);
            Assert.Equal(string.Empty, result.StdOut);
            Assert.Contains("Unrecognized append-project-snapshot argument '--unknown'.", result.StdErr, StringComparison.Ordinal);
            Assert.Contains(AppendProjectSnapshotUsage, result.StdErr, StringComparison.Ordinal);
        }

        [Fact]
        public void Main_AppendProjectSnapshotMode_MissingProjectCatalog_FailsWithEmptyStdout()
        {
            string tempDirectory = CreateTempDirectory();
            string missingProjectPath = Path.Combine(tempDirectory, "project.json");
            string snapshotPath = Path.Combine(tempDirectory, "snapshots", "run-003.json");

            try
            {
                CreateFixtureSnapshot(snapshotPath);

                var result = InvokeMain(
                    "append-project-snapshot",
                    "--project", missingProjectPath,
                    "--snapshot", snapshotPath);

                Assert.Equal(1, result.ExitCode);
                Assert.Equal(string.Empty, result.StdOut);
                Assert.Contains("Failed to append project snapshot:", result.StdErr, StringComparison.Ordinal);
            }
            finally
            {
                DeleteDirectoryIfExists(tempDirectory);
            }
        }

        [Fact]
        public void Main_AppendProjectSnapshotMode_InvalidProjectCatalog_FailsWithEmptyStdout()
        {
            string tempDirectory = CreateTempDirectory();
            string projectPath = Path.Combine(tempDirectory, "project.json");
            string snapshotPath = Path.Combine(tempDirectory, "snapshots", "run-003.json");

            try
            {
                File.WriteAllText(projectPath, "{ \"schemaVersion\": 1, ", new UTF8Encoding(false));
                CreateFixtureSnapshot(snapshotPath);

                var result = InvokeMain(
                    "append-project-snapshot",
                    "--project", projectPath,
                    "--snapshot", snapshotPath);

                Assert.Equal(1, result.ExitCode);
                Assert.Equal(string.Empty, result.StdOut);
                Assert.Contains("Failed to append project snapshot:", result.StdErr, StringComparison.Ordinal);
            }
            finally
            {
                DeleteDirectoryIfExists(tempDirectory);
            }
        }

        [Fact]
        public void Main_AppendProjectSnapshotMode_MissingRunIndex_FailsWithEmptyStdout()
        {
            string tempDirectory = CreateTempDirectory();
            string projectPath = Path.Combine(tempDirectory, "project.json");
            string runIndexPath = Path.Combine(tempDirectory, "run-index.json");
            string reportPath = Path.Combine(tempDirectory, "reports", "longitudinal.html");
            string snapshotPath = Path.Combine(tempDirectory, "snapshots", "run-003.json");

            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(reportPath)!);
                File.WriteAllText(reportPath, "SENTINEL-REPORT", new UTF8Encoding(false));
                SaveProjectCatalogFromPaths(projectPath, runIndexPath, reportPath);
                CreateFixtureSnapshot(snapshotPath);

                var result = InvokeMain(
                    "append-project-snapshot",
                    "--project", projectPath,
                    "--snapshot", snapshotPath);

                Assert.Equal(1, result.ExitCode);
                Assert.Equal(string.Empty, result.StdOut);
                Assert.Contains("Failed to append project snapshot:", result.StdErr, StringComparison.Ordinal);
                Assert.Contains("Run index file not found:", result.StdErr, StringComparison.Ordinal);
            }
            finally
            {
                DeleteDirectoryIfExists(tempDirectory);
            }
        }

        [Fact]
        public void Main_AppendProjectSnapshotMode_InvalidRunIndex_FailsAndPreservesProjectCatalogBytes()
        {
            string tempDirectory = CreateTempDirectory();
            string projectPath = Path.Combine(tempDirectory, "project.json");
            string runIndexPath = Path.Combine(tempDirectory, "run-index.json");
            string reportPath = Path.Combine(tempDirectory, "reports", "longitudinal.html");
            string snapshotPath = Path.Combine(tempDirectory, "snapshots", "run-003.json");

            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(reportPath)!);
                File.WriteAllText(reportPath, "SENTINEL-REPORT", new UTF8Encoding(false));
                File.WriteAllText(runIndexPath, "{ \"schemaVersion\": 1, ", new UTF8Encoding(false));
                SaveProjectCatalogFromPaths(projectPath, runIndexPath, reportPath);
                byte[] projectBefore = File.ReadAllBytes(projectPath);
                CreateFixtureSnapshot(snapshotPath);

                var result = InvokeMain(
                    "append-project-snapshot",
                    "--project", projectPath,
                    "--snapshot", snapshotPath);

                Assert.Equal(1, result.ExitCode);
                Assert.Equal(string.Empty, result.StdOut);
                Assert.Contains("Failed to append project snapshot:", result.StdErr, StringComparison.Ordinal);
                Assert.Equal(projectBefore, File.ReadAllBytes(projectPath));
            }
            finally
            {
                DeleteDirectoryIfExists(tempDirectory);
            }
        }

        [Fact]
        public void Main_AppendProjectSnapshotMode_InvalidExistingSnapshotInIndex_FailsWithEmptyStdout()
        {
            string tempDirectory = CreateTempDirectory();
            string projectPath = Path.Combine(tempDirectory, "project.json");
            string runIndexPath = Path.Combine(tempDirectory, "run-index.json");
            string reportPath = Path.Combine(tempDirectory, "reports", "longitudinal.html");
            string validExistingSnapshotPath = Path.Combine(tempDirectory, "snapshots", "run-001.json");
            string invalidExistingSnapshotPath = Path.Combine(tempDirectory, "snapshots", "invalid-existing.json");
            string newSnapshotPath = Path.Combine(tempDirectory, "snapshots", "run-003.json");

            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(reportPath)!);
                File.WriteAllText(reportPath, "SENTINEL-REPORT", new UTF8Encoding(false));
                CreateFixtureSnapshot(validExistingSnapshotPath);
                Directory.CreateDirectory(Path.GetDirectoryName(invalidExistingSnapshotPath)!);
                File.WriteAllText(invalidExistingSnapshotPath, "{ \"schemaVersion\": 1, ", new UTF8Encoding(false));
                SaveRunIndexWithSerializer(runIndexPath, validExistingSnapshotPath, invalidExistingSnapshotPath);
                SaveProjectCatalogFromPaths(projectPath, runIndexPath, reportPath);
                CreateFixtureSnapshot(newSnapshotPath);

                var result = InvokeMain(
                    "append-project-snapshot",
                    "--project", projectPath,
                    "--snapshot", newSnapshotPath);

                Assert.Equal(1, result.ExitCode);
                Assert.Equal(string.Empty, result.StdOut);
                Assert.Contains("Failed to append project snapshot:", result.StdErr, StringComparison.Ordinal);
            }
            finally
            {
                DeleteDirectoryIfExists(tempDirectory);
            }
        }

        [Fact]
        public void Main_AppendProjectSnapshotMode_MissingNewSnapshot_FailsWithEmptyStdout()
        {
            string tempDirectory = CreateTempDirectory();
            ProjectWorkspace workspace = CreateProjectWorkspace(
                tempDirectory,
                "run-index.json",
                "reports/longitudinal.html",
                "snapshots/run-001.json",
                "snapshots/run-002.json");
            string missingSnapshotPath = Path.Combine(tempDirectory, "snapshots", "missing.json");

            try
            {
                var result = InvokeMain(
                    "append-project-snapshot",
                    "--project", workspace.ProjectPath,
                    "--snapshot", missingSnapshotPath);

                Assert.Equal(1, result.ExitCode);
                Assert.Equal(string.Empty, result.StdOut);
                Assert.Contains("Coordination run snapshot file not found:", result.StdErr, StringComparison.Ordinal);
            }
            finally
            {
                DeleteDirectoryIfExists(tempDirectory);
            }
        }

        [Fact]
        public void Main_AppendProjectSnapshotMode_InvalidNewSnapshot_FailsWithEmptyStdout()
        {
            string tempDirectory = CreateTempDirectory();
            ProjectWorkspace workspace = CreateProjectWorkspace(
                tempDirectory,
                "run-index.json",
                "reports/longitudinal.html",
                "snapshots/run-001.json",
                "snapshots/run-002.json");
            string invalidSnapshotPath = Path.Combine(tempDirectory, "snapshots", "invalid-new.json");

            try
            {
                File.WriteAllText(invalidSnapshotPath, "{ \"schemaVersion\": 1, ", new UTF8Encoding(false));

                var result = InvokeMain(
                    "append-project-snapshot",
                    "--project", workspace.ProjectPath,
                    "--snapshot", invalidSnapshotPath);

                Assert.Equal(1, result.ExitCode);
                Assert.Equal(string.Empty, result.StdOut);
                Assert.Contains("Failed to append project snapshot:", result.StdErr, StringComparison.Ordinal);
            }
            finally
            {
                DeleteDirectoryIfExists(tempDirectory);
            }
        }

        [Fact]
        public void Main_AppendProjectSnapshotMode_NewSnapshotOutsideProjectTree_FailsAndPreservesWorkspaceBytes()
        {
            string tempDirectory = CreateTempDirectory();
            string externalDirectory = Path.Combine(tempDirectory, "external");
            ProjectWorkspace workspace = CreateProjectWorkspace(
                Path.Combine(tempDirectory, "project"),
                "run-index.json",
                "reports/longitudinal.html",
                "snapshots/run-001.json",
                "snapshots/run-002.json");
            string externalSnapshotPath = Path.Combine(externalDirectory, "snapshots", "run-003.json");

            try
            {
                CreateFixtureSnapshot(externalSnapshotPath);

                byte[] projectBefore = File.ReadAllBytes(workspace.ProjectPath);
                byte[] indexBefore = File.ReadAllBytes(workspace.RunIndexPath);
                byte[] firstSnapshotBefore = File.ReadAllBytes(workspace.ExistingSnapshotPaths[0]);
                byte[] secondSnapshotBefore = File.ReadAllBytes(workspace.ExistingSnapshotPaths[1]);
                byte[] externalSnapshotBefore = File.ReadAllBytes(externalSnapshotPath);
                byte[] reportBefore = File.ReadAllBytes(workspace.ReportPath);

                var result = InvokeMain(
                    "append-project-snapshot",
                    "--project", workspace.ProjectPath,
                    "--snapshot", externalSnapshotPath);

                Assert.Equal(1, result.ExitCode);
                Assert.Equal(string.Empty, result.StdOut);
                Assert.Contains("appended snapshot to stay inside the project catalog directory tree", result.StdErr, StringComparison.Ordinal);
                Assert.Equal(projectBefore, File.ReadAllBytes(workspace.ProjectPath));
                Assert.Equal(indexBefore, File.ReadAllBytes(workspace.RunIndexPath));
                Assert.Equal(firstSnapshotBefore, File.ReadAllBytes(workspace.ExistingSnapshotPaths[0]));
                Assert.Equal(secondSnapshotBefore, File.ReadAllBytes(workspace.ExistingSnapshotPaths[1]));
                Assert.Equal(externalSnapshotBefore, File.ReadAllBytes(externalSnapshotPath));
                Assert.Equal(reportBefore, File.ReadAllBytes(workspace.ReportPath));
            }
            finally
            {
                DeleteDirectoryIfExists(tempDirectory);
            }
        }

        [Fact]
        public void Main_AppendProjectSnapshotMode_NewSnapshotEqualToProjectCatalog_IsRejected()
        {
            string tempDirectory = CreateTempDirectory();
            ProjectWorkspace workspace = CreateProjectWorkspace(
                tempDirectory,
                "run-index.json",
                "reports/longitudinal.html",
                "snapshots/run-001.json",
                "snapshots/run-002.json");

            try
            {
                var result = InvokeMain(
                    "append-project-snapshot",
                    "--project", workspace.ProjectPath,
                    "--snapshot", workspace.ProjectPath);

                Assert.Equal(1, result.ExitCode);
                Assert.Equal(string.Empty, result.StdOut);
                Assert.Contains("same file as the project catalog", result.StdErr, StringComparison.Ordinal);
            }
            finally
            {
                DeleteDirectoryIfExists(tempDirectory);
            }
        }

        [Fact]
        public void Main_AppendProjectSnapshotMode_NewSnapshotEqualToRunIndex_IsRejected()
        {
            string tempDirectory = CreateTempDirectory();
            ProjectWorkspace workspace = CreateProjectWorkspace(
                tempDirectory,
                "run-index.json",
                "reports/longitudinal.html",
                "snapshots/run-001.json",
                "snapshots/run-002.json");

            try
            {
                var result = InvokeMain(
                    "append-project-snapshot",
                    "--project", workspace.ProjectPath,
                    "--snapshot", workspace.RunIndexPath);

                Assert.Equal(1, result.ExitCode);
                Assert.Equal(string.Empty, result.StdOut);
                Assert.Contains("same file as the run index", result.StdErr, StringComparison.Ordinal);
            }
            finally
            {
                DeleteDirectoryIfExists(tempDirectory);
            }
        }

        [Fact]
        public void Main_AppendProjectSnapshotMode_NewSnapshotEqualToReportDestination_IsRejected()
        {
            string tempDirectory = CreateTempDirectory();
            ProjectWorkspace workspace = CreateProjectWorkspace(
                tempDirectory,
                "run-index.json",
                "reports/longitudinal.html",
                "snapshots/run-001.json",
                "snapshots/run-002.json");

            try
            {
                var result = InvokeMain(
                    "append-project-snapshot",
                    "--project", workspace.ProjectPath,
                    "--snapshot", workspace.ReportPath);

                Assert.Equal(1, result.ExitCode);
                Assert.Equal(string.Empty, result.StdOut);
                Assert.Contains("same file as the report destination", result.StdErr, StringComparison.Ordinal);
            }
            finally
            {
                DeleteDirectoryIfExists(tempDirectory);
            }
        }

        [Fact]
        public void Main_AppendProjectSnapshotMode_NewSnapshotDirectory_IsRejected()
        {
            string tempDirectory = CreateTempDirectory();
            ProjectWorkspace workspace = CreateProjectWorkspace(
                tempDirectory,
                "run-index.json",
                "reports/longitudinal.html",
                "snapshots/run-001.json",
                "snapshots/run-002.json");
            string newSnapshotDirectory = Path.Combine(tempDirectory, "snapshots", "directory-snapshot");

            try
            {
                Directory.CreateDirectory(newSnapshotDirectory);

                var result = InvokeMain(
                    "append-project-snapshot",
                    "--project", workspace.ProjectPath,
                    "--snapshot", newSnapshotDirectory);

                Assert.Equal(1, result.ExitCode);
                Assert.Equal(string.Empty, result.StdOut);
                Assert.Contains("cannot be an existing directory", result.StdErr, StringComparison.Ordinal);
            }
            finally
            {
                DeleteDirectoryIfExists(tempDirectory);
            }
        }

        [Fact]
        public void Main_AppendProjectSnapshotMode_WithValidInputs_AppendsOnlyAtEndAndPreservesWorkspaceBytes()
        {
            string tempDirectory = CreateTempDirectory();
            ProjectWorkspace workspace = CreateProjectWorkspace(
                tempDirectory,
                "run-index.json",
                "reports/longitudinal.html",
                "snapshots/run-001.json",
                "snapshots/run-002.json");
            string newSnapshotPath = Path.Combine(tempDirectory, "snapshots", "run-003.json");

            try
            {
                CreateFixtureSnapshot(newSnapshotPath);
                byte[] projectBefore = File.ReadAllBytes(workspace.ProjectPath);
                byte[] firstSnapshotBefore = File.ReadAllBytes(workspace.ExistingSnapshotPaths[0]);
                byte[] secondSnapshotBefore = File.ReadAllBytes(workspace.ExistingSnapshotPaths[1]);
                byte[] newSnapshotBefore = File.ReadAllBytes(newSnapshotPath);
                byte[] reportBefore = File.ReadAllBytes(workspace.ReportPath);

                RunIndexDocument beforeIndex = new JsonRunIndexSerializer().Load(workspace.RunIndexPath);

                var result = InvokeMain(
                    "append-project-snapshot",
                    "--project", workspace.ProjectPath,
                    "--snapshot", newSnapshotPath);

                RunIndexDocument afterIndex = new JsonRunIndexSerializer().Load(workspace.RunIndexPath);

                Assert.Equal(0, result.ExitCode);
                Assert.Equal(string.Empty, result.StdErr);
                Assert.Equal(
                    $"Project: example-project\nAppended snapshot: snapshots/run-003.json\nIndexed snapshots: 3\nRun index updated: {workspace.RunIndexPath}",
                    result.StdOut);
                Assert.Equal(beforeIndex.SnapshotPaths[0], afterIndex.SnapshotPaths[0]);
                Assert.Equal(beforeIndex.SnapshotPaths[1], afterIndex.SnapshotPaths[1]);
                Assert.Equal("snapshots/run-003.json", afterIndex.SnapshotPaths[2]);
                Assert.Equal(projectBefore, File.ReadAllBytes(workspace.ProjectPath));
                Assert.Equal(firstSnapshotBefore, File.ReadAllBytes(workspace.ExistingSnapshotPaths[0]));
                Assert.Equal(secondSnapshotBefore, File.ReadAllBytes(workspace.ExistingSnapshotPaths[1]));
                Assert.Equal(newSnapshotBefore, File.ReadAllBytes(newSnapshotPath));
                Assert.Equal(reportBefore, File.ReadAllBytes(workspace.ReportPath));
            }
            finally
            {
                DeleteDirectoryIfExists(tempDirectory);
            }
        }

        [Fact]
        public void Main_AppendProjectSnapshotMode_PreservesDuplicateExistingReferences()
        {
            string tempDirectory = CreateTempDirectory();
            string projectPath = Path.Combine(tempDirectory, "project.json");
            string runIndexPath = Path.Combine(tempDirectory, "run-index.json");
            string reportPath = Path.Combine(tempDirectory, "reports", "longitudinal.html");
            string duplicatedSnapshotPath = Path.Combine(tempDirectory, "snapshots", "run-001.json");
            string secondSnapshotPath = Path.Combine(tempDirectory, "snapshots", "run-002.json");
            string newSnapshotPath = Path.Combine(tempDirectory, "snapshots", "run-003.json");

            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(reportPath)!);
                File.WriteAllText(reportPath, "SENTINEL-REPORT", new UTF8Encoding(false));
                CreateFixtureSnapshot(duplicatedSnapshotPath);
                CreateFixtureSnapshot(secondSnapshotPath);
                CreateFixtureSnapshot(newSnapshotPath);
                SaveRunIndexWithSerializer(runIndexPath, duplicatedSnapshotPath, duplicatedSnapshotPath, secondSnapshotPath);
                SaveProjectCatalogFromPaths(projectPath, runIndexPath, reportPath);

                var result = InvokeMain(
                    "append-project-snapshot",
                    "--project", projectPath,
                    "--snapshot", newSnapshotPath);

                RunIndexDocument index = new JsonRunIndexSerializer().Load(runIndexPath);

                Assert.Equal(0, result.ExitCode);
                Assert.Equal(4, index.SnapshotPaths.Count);
                Assert.Equal("snapshots/run-001.json", index.SnapshotPaths[0]);
                Assert.Equal("snapshots/run-001.json", index.SnapshotPaths[1]);
                Assert.Equal("snapshots/run-002.json", index.SnapshotPaths[2]);
                Assert.Equal("snapshots/run-003.json", index.SnapshotPaths[3]);
            }
            finally
            {
                DeleteDirectoryIfExists(tempDirectory);
            }
        }

        [Fact]
        public void Main_AppendProjectSnapshotMode_SameSnapshotCanBeAppendedAgain()
        {
            string tempDirectory = CreateTempDirectory();
            ProjectWorkspace workspace = CreateProjectWorkspace(
                tempDirectory,
                "run-index.json",
                "reports/longitudinal.html",
                "snapshots/run-001.json",
                "snapshots/run-002.json");
            string newSnapshotPath = Path.Combine(tempDirectory, "snapshots", "run-003.json");

            try
            {
                CreateFixtureSnapshot(newSnapshotPath);

                var firstResult = InvokeMain(
                    "append-project-snapshot",
                    "--project", workspace.ProjectPath,
                    "--snapshot", newSnapshotPath);
                var secondResult = InvokeMain(
                    "append-project-snapshot",
                    "--project", workspace.ProjectPath,
                    "--snapshot", newSnapshotPath);

                RunIndexDocument index = new JsonRunIndexSerializer().Load(workspace.RunIndexPath);

                Assert.Equal(0, firstResult.ExitCode);
                Assert.Equal(0, secondResult.ExitCode);
                Assert.Equal(4, index.SnapshotPaths.Count);
                Assert.Equal("snapshots/run-003.json", index.SnapshotPaths[2]);
                Assert.Equal("snapshots/run-003.json", index.SnapshotPaths[3]);
            }
            finally
            {
                DeleteDirectoryIfExists(tempDirectory);
            }
        }

        [Fact]
        public void Main_AppendProjectSnapshotMode_DistinctSnapshotWithSameRunId_IsAllowed()
        {
            string tempDirectory = CreateTempDirectory();
            ProjectWorkspace workspace = CreateProjectWorkspace(
                tempDirectory,
                "run-index.json",
                "reports/longitudinal.html",
                "snapshots/run-001.json",
                "snapshots/run-002.json");
            string sameRunIdSnapshotPath = Path.Combine(tempDirectory, "snapshots", "another-copy.json");

            try
            {
                CreateFixtureSnapshot(sameRunIdSnapshotPath);

                var result = InvokeMain(
                    "append-project-snapshot",
                    "--project", workspace.ProjectPath,
                    "--snapshot", sameRunIdSnapshotPath);

                RunIndexDocument index = new JsonRunIndexSerializer().Load(workspace.RunIndexPath);

                Assert.Equal(0, result.ExitCode);
                Assert.Equal("snapshots/another-copy.json", index.SnapshotPaths[2]);
            }
            finally
            {
                DeleteDirectoryIfExists(tempDirectory);
            }
        }

        [Fact]
        public void Main_AppendProjectSnapshotMode_SiblingSnapshotDirectory_UsesParentSegmentsAndForwardSlashes()
        {
            string tempDirectory = CreateTempDirectory();
            ProjectWorkspace workspace = CreateProjectWorkspace(
                tempDirectory,
                "indexes/run-index.json",
                "reports/longitudinal.html",
                "snapshots/run-001.json",
                "snapshots/run-002.json");
            string newSnapshotPath = Path.Combine(tempDirectory, "snapshots", "run-003.json");

            try
            {
                CreateFixtureSnapshot(newSnapshotPath);

                var result = InvokeMain(
                    "append-project-snapshot",
                    "--project", workspace.ProjectPath,
                    "--snapshot", newSnapshotPath);

                RunIndexDocument index = new JsonRunIndexSerializer().Load(workspace.RunIndexPath);

                Assert.Equal(0, result.ExitCode);
                Assert.Equal("../snapshots/run-003.json", index.SnapshotPaths[2]);
                Assert.Contains("Appended snapshot: ../snapshots/run-003.json", result.StdOut, StringComparison.Ordinal);
                Assert.DoesNotContain("\\", index.SnapshotPaths[2], StringComparison.Ordinal);
            }
            finally
            {
                DeleteDirectoryIfExists(tempDirectory);
            }
        }

        [Fact]
        public void Main_AppendProjectSnapshotMode_SecondExecutionAddsAnotherEntryWithoutReplacingPriorAppend()
        {
            string tempDirectory = CreateTempDirectory();
            ProjectWorkspace workspace = CreateProjectWorkspace(
                tempDirectory,
                "run-index.json",
                "reports/longitudinal.html",
                "snapshots/run-001.json",
                "snapshots/run-002.json");
            string thirdSnapshotPath = Path.Combine(tempDirectory, "snapshots", "run-003.json");
            string fourthSnapshotPath = Path.Combine(tempDirectory, "snapshots", "run-004.json");

            try
            {
                CreateFixtureSnapshot(thirdSnapshotPath);
                CreateFixtureSnapshot(fourthSnapshotPath);

                InvokeMain(
                    "append-project-snapshot",
                    "--project", workspace.ProjectPath,
                    "--snapshot", thirdSnapshotPath);
                var secondResult = InvokeMain(
                    "append-project-snapshot",
                    "--project", workspace.ProjectPath,
                    "--snapshot", fourthSnapshotPath);

                RunIndexDocument index = new JsonRunIndexSerializer().Load(workspace.RunIndexPath);

                Assert.Equal(0, secondResult.ExitCode);
                Assert.Equal(4, index.SnapshotPaths.Count);
                Assert.Equal("snapshots/run-003.json", index.SnapshotPaths[2]);
                Assert.Equal("snapshots/run-004.json", index.SnapshotPaths[3]);
            }
            finally
            {
                DeleteDirectoryIfExists(tempDirectory);
            }
        }

        [Fact]
        public void Main_AppendProjectSnapshotMode_RenderProjectMatchesCompareIndexAfterAppend()
        {
            string tempDirectory = CreateTempDirectory();
            ProjectWorkspace workspace = CreateProjectWorkspace(
                tempDirectory,
                "run-index.json",
                "reports/longitudinal.html",
                "snapshots/run-001.json",
                "snapshots/run-002.json");
            string thirdSnapshotPath = Path.Combine(tempDirectory, "snapshots", "run-003.json");

            try
            {
                CreateFixtureSnapshot(thirdSnapshotPath);

                var appendResult = InvokeMain(
                    "append-project-snapshot",
                    "--project", workspace.ProjectPath,
                    "--snapshot", thirdSnapshotPath);
                var compareIndexResult = InvokeMain("compare-index", "--index", workspace.RunIndexPath, "-o", workspace.ReportPath);
                string compareIndexHtml = File.ReadAllText(workspace.ReportPath);
                var renderProjectResult = InvokeMain("render-project", "--project", workspace.ProjectPath);

                Assert.Equal(0, appendResult.ExitCode);
                Assert.Equal(0, compareIndexResult.ExitCode);
                Assert.Equal(0, renderProjectResult.ExitCode);
                Assert.Contains("Indexed runs: 3", renderProjectResult.StdOut, StringComparison.Ordinal);
                Assert.Equal(compareIndexResult.StdOut, renderProjectResult.StdOut, StringComparer.Ordinal);
                Assert.Equal(compareIndexHtml, File.ReadAllText(workspace.ReportPath), StringComparer.Ordinal);
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

        private static ProjectWorkspace CreateProjectWorkspace(
            string tempDirectory,
            string runIndexRelativePath,
            string reportRelativePath,
            params string[] existingSnapshotRelativePaths)
        {
            runIndexRelativePath = NormalizeRelativeFilePath(runIndexRelativePath);
            reportRelativePath = NormalizeRelativeFilePath(reportRelativePath);
            string projectPath = Path.Combine(tempDirectory, "project.json");
            string runIndexPath = Path.Combine(tempDirectory, runIndexRelativePath);
            string reportPath = Path.Combine(tempDirectory, reportRelativePath);
            string[] snapshotPaths = new string[existingSnapshotRelativePaths.Length];

            Directory.CreateDirectory(Path.GetDirectoryName(reportPath)!);
            File.WriteAllText(reportPath, "SENTINEL-REPORT", new UTF8Encoding(false));

            for (int i = 0; i < existingSnapshotRelativePaths.Length; i++)
            {
                snapshotPaths[i] = Path.Combine(tempDirectory, NormalizeRelativeFilePath(existingSnapshotRelativePaths[i]));
                if (!File.Exists(snapshotPaths[i]))
                {
                    CreateFixtureSnapshot(snapshotPaths[i]);
                }
            }

            SaveRunIndexWithSerializer(runIndexPath, snapshotPaths);
            SaveProjectCatalogFromPaths(projectPath, runIndexPath, reportPath);

            return new ProjectWorkspace(projectPath, runIndexPath, reportPath, snapshotPaths);
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

        private static void SaveProjectCatalogFromPaths(string projectPath, string runIndexPath, string reportPath)
        {
            string? parentDirectory = Path.GetDirectoryName(projectPath);
            if (!string.IsNullOrEmpty(parentDirectory))
            {
                Directory.CreateDirectory(parentDirectory);
            }

            var resolver = new ProjectCatalogPathResolver();
            string runIndexReference = resolver.CreateReference(projectPath, runIndexPath);
            string reportReference = resolver.CreateReference(projectPath, reportPath);

            new JsonProjectCatalogSerializer().Save(
                new ProjectCatalogDocument(
                    "example-project",
                    "Example Coordination Project",
                    runIndexReference,
                    reportReference),
                projectPath);
        }

        private static string CreateTempDirectory()
        {
            string path = Path.Combine(Path.GetTempPath(), $"orzio-project-append-cli-tests-{Guid.NewGuid():N}");
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

        private static string NormalizeRelativeFilePath(string relativePath) =>
            relativePath
                .Replace('\\', Path.DirectorySeparatorChar)
                .Replace('/', Path.DirectorySeparatorChar);

        private sealed class ProjectWorkspace
        {
            public ProjectWorkspace(string projectPath, string runIndexPath, string reportPath, IReadOnlyList<string> existingSnapshotPaths)
            {
                ProjectPath = projectPath;
                RunIndexPath = runIndexPath;
                ReportPath = reportPath;
                ExistingSnapshotPaths = existingSnapshotPaths;
            }

            public string ProjectPath { get; }

            public string RunIndexPath { get; }

            public string ReportPath { get; }

            public IReadOnlyList<string> ExistingSnapshotPaths { get; }
        }
    }
}
