using System;
using System.IO;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Nodes;
using OrzioClashReport.Cli;

namespace OrzioClashReport.Tests
{
    [Collection("CliConsole")]
    public sealed class CliSnapshotComparisonTests
    {
        private static readonly MethodInfo MainMethod = ResolveMainMethod();
        private const string CompareSnapshotsUsage = "Usage: orzioclash compare-snapshots --previous-snapshot <previous.json> --current-snapshot <current.json> [-o <output.html> | --output <output.html>]";
        private static readonly string ExpectedCompareSummary = string.Join(
            "\n",
            "Previous run: coordination-sample-clash-xml",
            "Current run: coordination-sample-clash-xml",
            "Previous occurrences: 5",
            "Current occurrences: 5",
            "Candidates: 5",
            "Selected matches: 5",
            "Alternative candidates: 0",
            "StillOpen: 5",
            "New: 0",
            "Resolved: 0",
            "Unverifiable: 0");

        private static string SamplesDirectory => Path.Combine(AppContext.BaseDirectory, "samples");

        private static string SampleClashXmlPath => Path.Combine(SamplesDirectory, "sample-clash.xml");

        private static string SampleClashManifestPath => Path.Combine(SamplesDirectory, "sample-clash.run-manifest.json");

        [Fact]
        public void Main_CompareSnapshotsMode_WithPersistedFixtureSnapshots_WritesExactElevenLineSummary()
        {
            string tempDirectory = CreateTempDirectory();
            string previousSnapshotPath = Path.Combine(tempDirectory, "previous.json");
            string currentSnapshotPath = Path.Combine(tempDirectory, "current.json");

            try
            {
                CreateFixtureSnapshot(previousSnapshotPath);
                CreateFixtureSnapshot(currentSnapshotPath);

                var result = InvokeMain(
                    "compare-snapshots",
                    "--previous-snapshot", previousSnapshotPath,
                    "--current-snapshot", currentSnapshotPath);

                Assert.Equal(0, result.ExitCode);
                Assert.Equal(string.Empty, result.StdErr);
                Assert.Equal(ExpectedCompareSummary, result.StdOut);
                Assert.Equal(11, result.StdOut.Split('\n', StringSplitOptions.RemoveEmptyEntries).Length);
            }
            finally
            {
                DeleteFileIfExists(previousSnapshotPath);
                DeleteFileIfExists(currentSnapshotPath);
                DeleteDirectoryIfExists(tempDirectory);
            }
        }

        [Fact]
        public void Main_CompareSnapshotsMode_MatchesCompareStdOutForSameLogicalRuns()
        {
            string tempDirectory = CreateTempDirectory();
            string previousSnapshotPath = Path.Combine(tempDirectory, "previous.json");
            string currentSnapshotPath = Path.Combine(tempDirectory, "current.json");

            try
            {
                CreateFixtureSnapshot(previousSnapshotPath);
                CreateFixtureSnapshot(currentSnapshotPath);

                var sourceResult = InvokeMain(
                    "compare",
                    "--previous-xml", SampleClashXmlPath,
                    "--previous-manifest", SampleClashManifestPath,
                    "--current-xml", SampleClashXmlPath,
                    "--current-manifest", SampleClashManifestPath);

                var snapshotResult = InvokeMain(
                    "compare-snapshots",
                    "--previous-snapshot", previousSnapshotPath,
                    "--current-snapshot", currentSnapshotPath);

                Assert.Equal(0, sourceResult.ExitCode);
                Assert.Equal(0, snapshotResult.ExitCode);
                Assert.Equal(string.Empty, sourceResult.StdErr);
                Assert.Equal(string.Empty, snapshotResult.StdErr);
                Assert.Equal(sourceResult.StdOut, snapshotResult.StdOut, StringComparer.Ordinal);
            }
            finally
            {
                DeleteFileIfExists(previousSnapshotPath);
                DeleteFileIfExists(currentSnapshotPath);
                DeleteDirectoryIfExists(tempDirectory);
            }
        }

        [Fact]
        public void Main_CompareSnapshotsMode_WritesHtmlIdenticalToCompare()
        {
            string tempDirectory = CreateTempDirectory();
            string previousSnapshotPath = Path.Combine(tempDirectory, "previous.json");
            string currentSnapshotPath = Path.Combine(tempDirectory, "current.json");
            string sourceHtmlPath = Path.Combine(tempDirectory, "source-comparison.html");
            string snapshotHtmlPath = Path.Combine(tempDirectory, "snapshot-comparison.html");

            try
            {
                CreateFixtureSnapshot(previousSnapshotPath);
                CreateFixtureSnapshot(currentSnapshotPath);

                var sourceResult = InvokeMain(
                    "compare",
                    "--previous-xml", SampleClashXmlPath,
                    "--previous-manifest", SampleClashManifestPath,
                    "--current-xml", SampleClashXmlPath,
                    "--current-manifest", SampleClashManifestPath,
                    "-o", sourceHtmlPath);

                var snapshotResult = InvokeMain(
                    "compare-snapshots",
                    "--previous-snapshot", previousSnapshotPath,
                    "--current-snapshot", currentSnapshotPath,
                    "-o", snapshotHtmlPath);

                Assert.Equal(0, sourceResult.ExitCode);
                Assert.Equal(0, snapshotResult.ExitCode);
                Assert.Equal(string.Empty, sourceResult.StdErr);
                Assert.Equal(string.Empty, snapshotResult.StdErr);
                Assert.True(File.Exists(sourceHtmlPath));
                Assert.True(File.Exists(snapshotHtmlPath));
                Assert.Equal(File.ReadAllBytes(sourceHtmlPath), File.ReadAllBytes(snapshotHtmlPath));
            }
            finally
            {
                DeleteFileIfExists(previousSnapshotPath);
                DeleteFileIfExists(currentSnapshotPath);
                DeleteFileIfExists(sourceHtmlPath);
                DeleteFileIfExists(snapshotHtmlPath);
                DeleteDirectoryIfExists(tempDirectory);
            }
        }

        [Fact]
        public void Main_CompareSnapshotsMode_DoesNotReorderPreviousAndCurrentByCreatedAt()
        {
            string tempDirectory = CreateTempDirectory();
            string seedSnapshotPath = Path.Combine(tempDirectory, "seed.json");
            string previousSnapshotPath = Path.Combine(tempDirectory, "previous-role.json");
            string currentSnapshotPath = Path.Combine(tempDirectory, "current-role.json");

            try
            {
                CreateFixtureSnapshot(seedSnapshotPath);
                WriteSnapshotCopyWithMetadata(seedSnapshotPath, previousSnapshotPath, "role-previous", "2026-07-14T12:00:00+01:00");
                WriteSnapshotCopyWithMetadata(seedSnapshotPath, currentSnapshotPath, "role-current", "2026-07-13T12:00:00+01:00");

                var result = InvokeMain(
                    "compare-snapshots",
                    "--previous-snapshot", previousSnapshotPath,
                    "--current-snapshot", currentSnapshotPath);

                Assert.Equal(0, result.ExitCode);
                Assert.Equal(string.Empty, result.StdErr);

                string[] lines = result.StdOut.Split('\n', StringSplitOptions.RemoveEmptyEntries);
                Assert.True(lines.Length >= 2);
                Assert.Equal("Previous run: role-previous", lines[0]);
                Assert.Equal("Current run: role-current", lines[1]);
            }
            finally
            {
                DeleteFileIfExists(seedSnapshotPath);
                DeleteFileIfExists(previousSnapshotPath);
                DeleteFileIfExists(currentSnapshotPath);
                DeleteDirectoryIfExists(tempDirectory);
            }
        }

        [Fact]
        public void Main_CompareSnapshotsMode_SameSnapshotInBothRoles_IsAccepted()
        {
            string tempDirectory = CreateTempDirectory();
            string snapshotPath = Path.Combine(tempDirectory, "shared.json");

            try
            {
                CreateFixtureSnapshot(snapshotPath);

                var result = InvokeMain(
                    "compare-snapshots",
                    "--previous-snapshot", snapshotPath,
                    "--current-snapshot", snapshotPath);

                Assert.Equal(0, result.ExitCode);
                Assert.Equal(string.Empty, result.StdErr);
                Assert.Equal(ExpectedCompareSummary, result.StdOut);
            }
            finally
            {
                DeleteFileIfExists(snapshotPath);
                DeleteDirectoryIfExists(tempDirectory);
            }
        }

        [Fact]
        public void Main_CompareSnapshotsMode_CommandName_IsCaseInsensitive()
        {
            string tempDirectory = CreateTempDirectory();
            string previousSnapshotPath = Path.Combine(tempDirectory, "previous.json");
            string currentSnapshotPath = Path.Combine(tempDirectory, "current.json");

            try
            {
                CreateFixtureSnapshot(previousSnapshotPath);
                CreateFixtureSnapshot(currentSnapshotPath);

                var result = InvokeMain(
                    "Compare-Snapshots",
                    "--previous-snapshot", previousSnapshotPath,
                    "--current-snapshot", currentSnapshotPath);

                Assert.Equal(0, result.ExitCode);
                Assert.Equal(string.Empty, result.StdErr);
                Assert.Equal(ExpectedCompareSummary, result.StdOut);
            }
            finally
            {
                DeleteFileIfExists(previousSnapshotPath);
                DeleteFileIfExists(currentSnapshotPath);
                DeleteDirectoryIfExists(tempDirectory);
            }
        }

        [Fact]
        public void Main_CompareSnapshotsMode_MissingPreviousSnapshotOption_ReturnsUsageError()
        {
            string tempDirectory = CreateTempDirectory();
            string currentSnapshotPath = Path.Combine(tempDirectory, "current.json");

            try
            {
                CreateFixtureSnapshot(currentSnapshotPath);

                var result = InvokeMain(
                    "compare-snapshots",
                    "--current-snapshot", currentSnapshotPath);

                Assert.Equal(1, result.ExitCode);
                Assert.Equal(string.Empty, result.StdOut);
                Assert.Contains("Missing required option '--previous-snapshot'.", result.StdErr);
                Assert.Contains(CompareSnapshotsUsage, result.StdErr);
            }
            finally
            {
                DeleteFileIfExists(currentSnapshotPath);
                DeleteDirectoryIfExists(tempDirectory);
            }
        }

        [Fact]
        public void Main_CompareSnapshotsMode_MissingCurrentSnapshotOption_ReturnsUsageError()
        {
            string tempDirectory = CreateTempDirectory();
            string previousSnapshotPath = Path.Combine(tempDirectory, "previous.json");

            try
            {
                CreateFixtureSnapshot(previousSnapshotPath);

                var result = InvokeMain(
                    "compare-snapshots",
                    "--previous-snapshot", previousSnapshotPath);

                Assert.Equal(1, result.ExitCode);
                Assert.Equal(string.Empty, result.StdOut);
                Assert.Contains("Missing required option '--current-snapshot'.", result.StdErr);
                Assert.Contains(CompareSnapshotsUsage, result.StdErr);
            }
            finally
            {
                DeleteFileIfExists(previousSnapshotPath);
                DeleteDirectoryIfExists(tempDirectory);
            }
        }

        [Fact]
        public void Main_CompareSnapshotsMode_DuplicatePreviousSnapshotOption_ReturnsUsageError()
        {
            string tempDirectory = CreateTempDirectory();
            string previousSnapshotPath = Path.Combine(tempDirectory, "previous.json");
            string currentSnapshotPath = Path.Combine(tempDirectory, "current.json");

            try
            {
                CreateFixtureSnapshot(previousSnapshotPath);
                CreateFixtureSnapshot(currentSnapshotPath);

                var result = InvokeMain(
                    "compare-snapshots",
                    "--previous-snapshot", previousSnapshotPath,
                    "--previous-snapshot", previousSnapshotPath,
                    "--current-snapshot", currentSnapshotPath);

                Assert.Equal(1, result.ExitCode);
                Assert.Equal(string.Empty, result.StdOut);
                Assert.Contains("Duplicate option '--previous-snapshot'.", result.StdErr);
                Assert.Contains(CompareSnapshotsUsage, result.StdErr);
            }
            finally
            {
                DeleteFileIfExists(previousSnapshotPath);
                DeleteFileIfExists(currentSnapshotPath);
                DeleteDirectoryIfExists(tempDirectory);
            }
        }

        [Fact]
        public void Main_CompareSnapshotsMode_DuplicateCurrentSnapshotOption_ReturnsUsageError()
        {
            string tempDirectory = CreateTempDirectory();
            string previousSnapshotPath = Path.Combine(tempDirectory, "previous.json");
            string currentSnapshotPath = Path.Combine(tempDirectory, "current.json");

            try
            {
                CreateFixtureSnapshot(previousSnapshotPath);
                CreateFixtureSnapshot(currentSnapshotPath);

                var result = InvokeMain(
                    "compare-snapshots",
                    "--previous-snapshot", previousSnapshotPath,
                    "--current-snapshot", currentSnapshotPath,
                    "--current-snapshot", currentSnapshotPath);

                Assert.Equal(1, result.ExitCode);
                Assert.Equal(string.Empty, result.StdOut);
                Assert.Contains("Duplicate option '--current-snapshot'.", result.StdErr);
                Assert.Contains(CompareSnapshotsUsage, result.StdErr);
            }
            finally
            {
                DeleteFileIfExists(previousSnapshotPath);
                DeleteFileIfExists(currentSnapshotPath);
                DeleteDirectoryIfExists(tempDirectory);
            }
        }

        [Fact]
        public void Main_CompareSnapshotsMode_DuplicateOutputAliases_ReturnsUsageError()
        {
            string tempDirectory = CreateTempDirectory();
            string previousSnapshotPath = Path.Combine(tempDirectory, "previous.json");
            string currentSnapshotPath = Path.Combine(tempDirectory, "current.json");
            string firstOutputPath = Path.Combine(tempDirectory, "first.html");
            string secondOutputPath = Path.Combine(tempDirectory, "second.html");

            try
            {
                CreateFixtureSnapshot(previousSnapshotPath);
                CreateFixtureSnapshot(currentSnapshotPath);

                var result = InvokeMain(
                    "compare-snapshots",
                    "--previous-snapshot", previousSnapshotPath,
                    "--current-snapshot", currentSnapshotPath,
                    "-o", firstOutputPath,
                    "--output", secondOutputPath);

                Assert.Equal(1, result.ExitCode);
                Assert.Equal(string.Empty, result.StdOut);
                Assert.Contains("Duplicate option '-o/--output'.", result.StdErr);
                Assert.Contains(CompareSnapshotsUsage, result.StdErr);
                Assert.False(File.Exists(firstOutputPath));
                Assert.False(File.Exists(secondOutputPath));
            }
            finally
            {
                DeleteFileIfExists(previousSnapshotPath);
                DeleteFileIfExists(currentSnapshotPath);
                DeleteFileIfExists(firstOutputPath);
                DeleteFileIfExists(secondOutputPath);
                DeleteDirectoryIfExists(tempDirectory);
            }
        }

        [Fact]
        public void Main_CompareSnapshotsMode_MissingPreviousSnapshotValue_ReturnsUsageError()
        {
            string tempDirectory = CreateTempDirectory();
            string currentSnapshotPath = Path.Combine(tempDirectory, "current.json");

            try
            {
                CreateFixtureSnapshot(currentSnapshotPath);

                var result = InvokeMain(
                    "compare-snapshots",
                    "--previous-snapshot",
                    "--current-snapshot", currentSnapshotPath);

                Assert.Equal(1, result.ExitCode);
                Assert.Equal(string.Empty, result.StdOut);
                Assert.Contains("Missing value for '--previous-snapshot'.", result.StdErr);
                Assert.Contains(CompareSnapshotsUsage, result.StdErr);
            }
            finally
            {
                DeleteFileIfExists(currentSnapshotPath);
                DeleteDirectoryIfExists(tempDirectory);
            }
        }

        [Fact]
        public void Main_CompareSnapshotsMode_MissingCurrentSnapshotValue_ReturnsUsageError()
        {
            string tempDirectory = CreateTempDirectory();
            string previousSnapshotPath = Path.Combine(tempDirectory, "previous.json");

            try
            {
                CreateFixtureSnapshot(previousSnapshotPath);

                var result = InvokeMain(
                    "compare-snapshots",
                    "--previous-snapshot", previousSnapshotPath,
                    "--current-snapshot");

                Assert.Equal(1, result.ExitCode);
                Assert.Equal(string.Empty, result.StdOut);
                Assert.Contains("Missing value for '--current-snapshot'.", result.StdErr);
                Assert.Contains(CompareSnapshotsUsage, result.StdErr);
            }
            finally
            {
                DeleteFileIfExists(previousSnapshotPath);
                DeleteDirectoryIfExists(tempDirectory);
            }
        }

        [Fact]
        public void Main_CompareSnapshotsMode_MissingOutputValue_ReturnsUsageError()
        {
            string tempDirectory = CreateTempDirectory();
            string previousSnapshotPath = Path.Combine(tempDirectory, "previous.json");
            string currentSnapshotPath = Path.Combine(tempDirectory, "current.json");

            try
            {
                CreateFixtureSnapshot(previousSnapshotPath);
                CreateFixtureSnapshot(currentSnapshotPath);

                var result = InvokeMain(
                    "compare-snapshots",
                    "--previous-snapshot", previousSnapshotPath,
                    "-o",
                    "--current-snapshot", currentSnapshotPath);

                Assert.Equal(1, result.ExitCode);
                Assert.Equal(string.Empty, result.StdOut);
                Assert.Contains("Missing value for '-o'.", result.StdErr);
                Assert.Contains(CompareSnapshotsUsage, result.StdErr);
            }
            finally
            {
                DeleteFileIfExists(previousSnapshotPath);
                DeleteFileIfExists(currentSnapshotPath);
                DeleteDirectoryIfExists(tempDirectory);
            }
        }

        [Fact]
        public void Main_CompareSnapshotsMode_UnknownArgument_ReturnsUsageError()
        {
            string tempDirectory = CreateTempDirectory();
            string previousSnapshotPath = Path.Combine(tempDirectory, "previous.json");
            string currentSnapshotPath = Path.Combine(tempDirectory, "current.json");

            try
            {
                CreateFixtureSnapshot(previousSnapshotPath);
                CreateFixtureSnapshot(currentSnapshotPath);

                var result = InvokeMain(
                    "compare-snapshots",
                    "--previous-snapshot", previousSnapshotPath,
                    "--current-snapshot", currentSnapshotPath,
                    "--foo", "bar");

                Assert.Equal(1, result.ExitCode);
                Assert.Equal(string.Empty, result.StdOut);
                Assert.Contains("Unrecognized compare-snapshots argument '--foo'.", result.StdErr);
                Assert.Contains(CompareSnapshotsUsage, result.StdErr);
            }
            finally
            {
                DeleteFileIfExists(previousSnapshotPath);
                DeleteFileIfExists(currentSnapshotPath);
                DeleteDirectoryIfExists(tempDirectory);
            }
        }

        [Fact]
        public void Main_CompareSnapshotsMode_PositionalSnapshotArgument_IsRejected()
        {
            string tempDirectory = CreateTempDirectory();
            string previousSnapshotPath = Path.Combine(tempDirectory, "previous.json");
            string currentSnapshotPath = Path.Combine(tempDirectory, "current.json");

            try
            {
                CreateFixtureSnapshot(previousSnapshotPath);
                CreateFixtureSnapshot(currentSnapshotPath);

                var result = InvokeMain(
                    "compare-snapshots",
                    previousSnapshotPath,
                    "--current-snapshot", currentSnapshotPath);

                Assert.Equal(1, result.ExitCode);
                Assert.Equal(string.Empty, result.StdOut);
                Assert.Contains($"Unrecognized compare-snapshots argument '{previousSnapshotPath}'.", result.StdErr);
                Assert.Contains(CompareSnapshotsUsage, result.StdErr);
            }
            finally
            {
                DeleteFileIfExists(previousSnapshotPath);
                DeleteFileIfExists(currentSnapshotPath);
                DeleteDirectoryIfExists(tempDirectory);
            }
        }

        [Fact]
        public void Main_CompareSnapshotsMode_MissingPreviousSnapshotFile_ReturnsContextualError()
        {
            string tempDirectory = CreateTempDirectory();
            string currentSnapshotPath = Path.Combine(tempDirectory, "current.json");
            string missingPreviousPath = Path.Combine(tempDirectory, "missing-previous.json");

            try
            {
                CreateFixtureSnapshot(currentSnapshotPath);

                var result = InvokeMain(
                    "compare-snapshots",
                    "--previous-snapshot", missingPreviousPath,
                    "--current-snapshot", currentSnapshotPath);

                Assert.Equal(1, result.ExitCode);
                Assert.Equal(string.Empty, result.StdOut);
                Assert.Contains("Previous snapshot file not found:", result.StdErr);
                Assert.Contains(missingPreviousPath, result.StdErr);
                Assert.DoesNotContain(CompareSnapshotsUsage, result.StdErr, StringComparison.Ordinal);
            }
            finally
            {
                DeleteFileIfExists(currentSnapshotPath);
                DeleteDirectoryIfExists(tempDirectory);
            }
        }

        [Fact]
        public void Main_CompareSnapshotsMode_MissingCurrentSnapshotFile_ReturnsContextualError()
        {
            string tempDirectory = CreateTempDirectory();
            string previousSnapshotPath = Path.Combine(tempDirectory, "previous.json");
            string missingCurrentPath = Path.Combine(tempDirectory, "missing-current.json");

            try
            {
                CreateFixtureSnapshot(previousSnapshotPath);

                var result = InvokeMain(
                    "compare-snapshots",
                    "--previous-snapshot", previousSnapshotPath,
                    "--current-snapshot", missingCurrentPath);

                Assert.Equal(1, result.ExitCode);
                Assert.Equal(string.Empty, result.StdOut);
                Assert.Contains("Current snapshot file not found:", result.StdErr);
                Assert.Contains(missingCurrentPath, result.StdErr);
                Assert.DoesNotContain(CompareSnapshotsUsage, result.StdErr, StringComparison.Ordinal);
            }
            finally
            {
                DeleteFileIfExists(previousSnapshotPath);
                DeleteDirectoryIfExists(tempDirectory);
            }
        }

        [Fact]
        public void Main_CompareSnapshotsMode_InvalidSnapshot_FailsWithoutFallbackOrSuccessSummary()
        {
            string tempDirectory = CreateTempDirectory();
            string previousSnapshotPath = Path.Combine(tempDirectory, "previous.json");
            string invalidCurrentPath = Path.Combine(tempDirectory, "invalid-current.json");

            try
            {
                CreateFixtureSnapshot(previousSnapshotPath);
                File.WriteAllText(invalidCurrentPath, "{ \"schemaVersion\": 1, ");

                var result = InvokeMain(
                    "compare-snapshots",
                    "--previous-snapshot", previousSnapshotPath,
                    "--current-snapshot", invalidCurrentPath);

                Assert.Equal(1, result.ExitCode);
                Assert.DoesNotContain("Previous run:", result.StdOut, StringComparison.Ordinal);
                Assert.DoesNotContain("StillOpen:", result.StdOut, StringComparison.Ordinal);
                Assert.Contains("Failed to compare snapshots:", result.StdErr);
            }
            finally
            {
                DeleteFileIfExists(previousSnapshotPath);
                DeleteFileIfExists(invalidCurrentPath);
                DeleteDirectoryIfExists(tempDirectory);
            }
        }

        [Fact]
        public void Main_CompareSnapshotsMode_OutputAlias_WritesLifecycleHtml()
        {
            string tempDirectory = CreateTempDirectory();
            string previousSnapshotPath = Path.Combine(tempDirectory, "previous.json");
            string currentSnapshotPath = Path.Combine(tempDirectory, "current.json");
            string outputPath = Path.Combine(tempDirectory, "comparison.html");

            try
            {
                CreateFixtureSnapshot(previousSnapshotPath);
                CreateFixtureSnapshot(currentSnapshotPath);

                var result = InvokeMain(
                    "compare-snapshots",
                    "--previous-snapshot", previousSnapshotPath,
                    "--current-snapshot", currentSnapshotPath,
                    "--output", outputPath);

                Assert.Equal(0, result.ExitCode);
                Assert.Equal(string.Empty, result.StdErr);
                Assert.True(File.Exists(outputPath));
                Assert.Equal(
                    ExpectedCompareSummary + "\n" + $"Comparison report written to {outputPath}",
                    result.StdOut);
            }
            finally
            {
                DeleteFileIfExists(previousSnapshotPath);
                DeleteFileIfExists(currentSnapshotPath);
                DeleteFileIfExists(outputPath);
                DeleteDirectoryIfExists(tempDirectory);
            }
        }

        [Fact]
        public void Main_CompareSnapshotsMode_WriteFailure_DoesNotPrintSuccessSummary()
        {
            string tempDirectory = CreateTempDirectory();
            string previousSnapshotPath = Path.Combine(tempDirectory, "previous.json");
            string currentSnapshotPath = Path.Combine(tempDirectory, "current.json");
            string outputPath = Path.Combine(tempDirectory, "missing-parent", "comparison.html");

            try
            {
                CreateFixtureSnapshot(previousSnapshotPath);
                CreateFixtureSnapshot(currentSnapshotPath);

                var result = InvokeMain(
                    "compare-snapshots",
                    "--previous-snapshot", previousSnapshotPath,
                    "--current-snapshot", currentSnapshotPath,
                    "-o", outputPath);

                Assert.Equal(1, result.ExitCode);
                Assert.DoesNotContain("Previous run:", result.StdOut, StringComparison.Ordinal);
                Assert.DoesNotContain("StillOpen:", result.StdOut, StringComparison.Ordinal);
                Assert.DoesNotContain("Comparison report written to", result.StdOut, StringComparison.Ordinal);
                Assert.Contains("Failed to compare snapshots:", result.StdErr);
                Assert.False(File.Exists(outputPath));
            }
            finally
            {
                DeleteFileIfExists(previousSnapshotPath);
                DeleteFileIfExists(currentSnapshotPath);
                DeleteFileIfExists(outputPath);
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
            var originalOut = Console.Out;
            var originalError = Console.Error;
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
            var result = InvokeMain(
                "snapshot",
                "--xml", SampleClashXmlPath,
                "--manifest", SampleClashManifestPath,
                "-o", outputPath);

            Assert.Equal(0, result.ExitCode);
            Assert.Equal(string.Empty, result.StdErr);
            Assert.True(File.Exists(outputPath));
        }

        private static void WriteSnapshotCopyWithMetadata(string sourcePath, string destinationPath, string runId, string createdAt)
        {
            string json = File.ReadAllText(sourcePath);
            JsonObject root = JsonNode.Parse(json)?.AsObject()
                ?? throw new InvalidOperationException("Failed to parse the seed snapshot JSON.");

            root["runId"] = runId;
            root["createdAt"] = createdAt;

            File.WriteAllText(
                destinationPath,
                root.ToJsonString(new JsonSerializerOptions { WriteIndented = true }));
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

        private static string NormalizeLineEndings(string value) =>
            value.ReplaceLineEndings("\n").TrimEnd('\n');
    }
}
