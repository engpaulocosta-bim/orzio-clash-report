using System;
using System.IO;
using System.Reflection;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using OrzioClashReport.Cli;
using OrzioClashReport.Persistence.RunIndexJson;

namespace OrzioClashReport.Tests
{
    [Collection("CliConsole")]
    public sealed class CliRunIndexTests
    {
        private static readonly MethodInfo MainMethod = ResolveMainMethod();
        private const string IndexSnapshotsUsage = "Usage: orzioclash index-snapshots --snapshot <run-snapshot.json> [--snapshot <run-snapshot.json> ...] (-o <run-index.json> | --output <run-index.json>)";

        private static string SamplesDirectory => Path.Combine(AppContext.BaseDirectory, "samples");

        private static string SampleClashXmlPath => Path.Combine(SamplesDirectory, "sample-clash.xml");

        private static string SampleClashManifestPath => Path.Combine(SamplesDirectory, "sample-clash.run-manifest.json");

        [Fact]
        public void Main_IndexSnapshotsMode_WithTwoFreshValidSnapshots_CreatesOrderedRunIndex()
        {
            string tempDirectory = CreateTempDirectory();
            string firstSnapshotPath = Path.Combine(tempDirectory, "snapshots", "first.json");
            string secondSnapshotPath = Path.Combine(tempDirectory, "snapshots", "second.json");
            string outputPath = Path.Combine(tempDirectory, "run-index.json");

            try
            {
                CreateFixtureSnapshot(firstSnapshotPath);
                CreateFixtureSnapshot(secondSnapshotPath);

                var result = InvokeMain(
                    "index-snapshots",
                    "--snapshot", firstSnapshotPath,
                    "--snapshot", secondSnapshotPath,
                    "-o", outputPath);

                Assert.Equal(0, result.ExitCode);
                Assert.Equal(string.Empty, result.StdErr);
                Assert.Equal(
                    $"Indexed snapshots: 2\nRun index written to {outputPath}",
                    result.StdOut);
                Assert.True(File.Exists(outputPath));

                RunIndexDocument index = new JsonRunIndexSerializer().Load(outputPath);
                Assert.Equal(2, index.SnapshotPaths.Count);
                Assert.Equal("snapshots/first.json", index.SnapshotPaths[0]);
                Assert.Equal("snapshots/second.json", index.SnapshotPaths[1]);

                var resolver = new RunIndexSnapshotPathResolver();
                AssertPathsEqual(firstSnapshotPath, resolver.ResolveReference(outputPath, index.SnapshotPaths[0]));
                AssertPathsEqual(secondSnapshotPath, resolver.ResolveReference(outputPath, index.SnapshotPaths[1]));
            }
            finally
            {
                DeleteDirectoryIfExists(tempDirectory);
            }
        }

        [Fact]
        public void Main_IndexSnapshotsMode_PreservesCliArgumentOrder()
        {
            string tempDirectory = CreateTempDirectory();
            string firstSnapshotPath = Path.Combine(tempDirectory, "snapshots", "z-second.json");
            string secondSnapshotPath = Path.Combine(tempDirectory, "snapshots", "a-first.json");
            string outputPath = Path.Combine(tempDirectory, "run-index.json");

            try
            {
                CreateFixtureSnapshot(firstSnapshotPath);
                CreateFixtureSnapshot(secondSnapshotPath);

                var result = InvokeMain(
                    "index-snapshots",
                    "--snapshot", firstSnapshotPath,
                    "--snapshot", secondSnapshotPath,
                    "-o", outputPath);

                Assert.Equal(0, result.ExitCode);

                RunIndexDocument index = new JsonRunIndexSerializer().Load(outputPath);
                Assert.Equal("snapshots/z-second.json", index.SnapshotPaths[0]);
                Assert.Equal("snapshots/a-first.json", index.SnapshotPaths[1]);
            }
            finally
            {
                DeleteDirectoryIfExists(tempDirectory);
            }
        }

        [Fact]
        public void Main_IndexSnapshotsMode_DoesNotReorderByCreatedAt()
        {
            string tempDirectory = CreateTempDirectory();
            string seedSnapshotPath = Path.Combine(tempDirectory, "seed.json");
            string laterCreatedPath = Path.Combine(tempDirectory, "snapshots", "later.json");
            string earlierCreatedPath = Path.Combine(tempDirectory, "snapshots", "earlier.json");
            string outputPath = Path.Combine(tempDirectory, "run-index.json");

            try
            {
                CreateFixtureSnapshot(seedSnapshotPath);
                WriteSnapshotCopyWithMetadata(seedSnapshotPath, laterCreatedPath, "run-later", "2026-07-15T12:00:00+01:00");
                WriteSnapshotCopyWithMetadata(seedSnapshotPath, earlierCreatedPath, "run-earlier", "2026-07-14T12:00:00+01:00");

                var result = InvokeMain(
                    "index-snapshots",
                    "--snapshot", laterCreatedPath,
                    "--snapshot", earlierCreatedPath,
                    "-o", outputPath);

                Assert.Equal(0, result.ExitCode);

                RunIndexDocument index = new JsonRunIndexSerializer().Load(outputPath);
                var resolver = new RunIndexSnapshotPathResolver();
                AssertPathsEqual(laterCreatedPath, resolver.ResolveReference(outputPath, index.SnapshotPaths[0]));
                AssertPathsEqual(earlierCreatedPath, resolver.ResolveReference(outputPath, index.SnapshotPaths[1]));
            }
            finally
            {
                DeleteDirectoryIfExists(tempDirectory);
            }
        }

        [Fact]
        public void Main_IndexSnapshotsMode_DoesNotReorderByRunId()
        {
            string tempDirectory = CreateTempDirectory();
            string seedSnapshotPath = Path.Combine(tempDirectory, "seed.json");
            string zRunPath = Path.Combine(tempDirectory, "snapshots", "z-run.json");
            string aRunPath = Path.Combine(tempDirectory, "snapshots", "a-run.json");
            string outputPath = Path.Combine(tempDirectory, "run-index.json");

            try
            {
                CreateFixtureSnapshot(seedSnapshotPath);
                WriteSnapshotCopyWithMetadata(seedSnapshotPath, zRunPath, "zzz-run", "2026-07-14T12:00:00+01:00");
                WriteSnapshotCopyWithMetadata(seedSnapshotPath, aRunPath, "aaa-run", "2026-07-14T12:00:00+01:00");

                var result = InvokeMain(
                    "index-snapshots",
                    "--snapshot", zRunPath,
                    "--snapshot", aRunPath,
                    "-o", outputPath);

                Assert.Equal(0, result.ExitCode);

                RunIndexDocument index = new JsonRunIndexSerializer().Load(outputPath);
                var resolver = new RunIndexSnapshotPathResolver();
                AssertPathsEqual(zRunPath, resolver.ResolveReference(outputPath, index.SnapshotPaths[0]));
                AssertPathsEqual(aRunPath, resolver.ResolveReference(outputPath, index.SnapshotPaths[1]));
            }
            finally
            {
                DeleteDirectoryIfExists(tempDirectory);
            }
        }

        [Fact]
        public void Main_IndexSnapshotsMode_DuplicateSnapshotArguments_ArePreserved()
        {
            string tempDirectory = CreateTempDirectory();
            string snapshotPath = Path.Combine(tempDirectory, "snapshots", "shared.json");
            string outputPath = Path.Combine(tempDirectory, "run-index.json");

            try
            {
                CreateFixtureSnapshot(snapshotPath);

                var result = InvokeMain(
                    "index-snapshots",
                    "--snapshot", snapshotPath,
                    "--snapshot", snapshotPath,
                    "-o", outputPath);

                Assert.Equal(0, result.ExitCode);

                RunIndexDocument index = new JsonRunIndexSerializer().Load(outputPath);
                Assert.Equal(2, index.SnapshotPaths.Count);
                Assert.Equal("snapshots/shared.json", index.SnapshotPaths[0]);
                Assert.Equal("snapshots/shared.json", index.SnapshotPaths[1]);
            }
            finally
            {
                DeleteDirectoryIfExists(tempDirectory);
            }
        }

        [Fact]
        public void Main_IndexSnapshotsMode_CommandName_IsCaseInsensitive()
        {
            string tempDirectory = CreateTempDirectory();
            string firstSnapshotPath = Path.Combine(tempDirectory, "snapshots", "first.json");
            string outputPath = Path.Combine(tempDirectory, "run-index.json");

            try
            {
                CreateFixtureSnapshot(firstSnapshotPath);

                var result = InvokeMain(
                    "INDEX-SNAPSHOTS",
                    "--snapshot", firstSnapshotPath,
                    "-o", outputPath);

                Assert.Equal(0, result.ExitCode);
                Assert.Equal(string.Empty, result.StdErr);
                Assert.True(File.Exists(outputPath));
            }
            finally
            {
                DeleteDirectoryIfExists(tempDirectory);
            }
        }

        [Fact]
        public void Main_IndexSnapshotsMode_MissingSnapshotOption_ReturnsUsageError()
        {
            string tempDirectory = CreateTempDirectory();
            string outputPath = Path.Combine(tempDirectory, "run-index.json");

            try
            {
                var result = InvokeMain(
                    "index-snapshots",
                    "-o", outputPath);

                Assert.Equal(1, result.ExitCode);
                Assert.Equal(string.Empty, result.StdOut);
                Assert.Contains("Missing required option '--snapshot'.", result.StdErr);
                Assert.Contains(IndexSnapshotsUsage, result.StdErr);
            }
            finally
            {
                DeleteDirectoryIfExists(tempDirectory);
            }
        }

        [Fact]
        public void Main_IndexSnapshotsMode_MissingOutput_ReturnsUsageError()
        {
            string tempDirectory = CreateTempDirectory();
            string snapshotPath = Path.Combine(tempDirectory, "snapshots", "first.json");

            try
            {
                CreateFixtureSnapshot(snapshotPath);

                var result = InvokeMain(
                    "index-snapshots",
                    "--snapshot", snapshotPath);

                Assert.Equal(1, result.ExitCode);
                Assert.Equal(string.Empty, result.StdOut);
                Assert.Contains("Missing required option '-o/--output'.", result.StdErr);
                Assert.Contains(IndexSnapshotsUsage, result.StdErr);
            }
            finally
            {
                DeleteDirectoryIfExists(tempDirectory);
            }
        }

        [Fact]
        public void Main_IndexSnapshotsMode_DuplicateOutputAliases_ReturnsUsageError()
        {
            string tempDirectory = CreateTempDirectory();
            string snapshotPath = Path.Combine(tempDirectory, "snapshots", "first.json");
            string firstOutputPath = Path.Combine(tempDirectory, "first-index.json");
            string secondOutputPath = Path.Combine(tempDirectory, "second-index.json");

            try
            {
                CreateFixtureSnapshot(snapshotPath);

                var result = InvokeMain(
                    "index-snapshots",
                    "--snapshot", snapshotPath,
                    "-o", firstOutputPath,
                    "--output", secondOutputPath);

                Assert.Equal(1, result.ExitCode);
                Assert.Equal(string.Empty, result.StdOut);
                Assert.Contains("Duplicate option '-o/--output'.", result.StdErr);
                Assert.Contains(IndexSnapshotsUsage, result.StdErr);
                Assert.False(File.Exists(firstOutputPath));
                Assert.False(File.Exists(secondOutputPath));
            }
            finally
            {
                DeleteDirectoryIfExists(tempDirectory);
            }
        }

        [Fact]
        public void Main_IndexSnapshotsMode_MissingSnapshotValue_ReturnsUsageError()
        {
            string tempDirectory = CreateTempDirectory();
            string outputPath = Path.Combine(tempDirectory, "run-index.json");

            try
            {
                var result = InvokeMain(
                    "index-snapshots",
                    "--snapshot",
                    "-o", outputPath);

                Assert.Equal(1, result.ExitCode);
                Assert.Equal(string.Empty, result.StdOut);
                Assert.Contains("Missing value for '--snapshot'.", result.StdErr);
                Assert.Contains(IndexSnapshotsUsage, result.StdErr);
            }
            finally
            {
                DeleteDirectoryIfExists(tempDirectory);
            }
        }

        [Fact]
        public void Main_IndexSnapshotsMode_MissingOutputValue_ReturnsUsageError()
        {
            string tempDirectory = CreateTempDirectory();
            string snapshotPath = Path.Combine(tempDirectory, "snapshots", "first.json");

            try
            {
                CreateFixtureSnapshot(snapshotPath);

                var result = InvokeMain(
                    "index-snapshots",
                    "--snapshot", snapshotPath,
                    "-o");

                Assert.Equal(1, result.ExitCode);
                Assert.Equal(string.Empty, result.StdOut);
                Assert.Contains("Missing value for '-o'.", result.StdErr);
                Assert.Contains(IndexSnapshotsUsage, result.StdErr);
            }
            finally
            {
                DeleteDirectoryIfExists(tempDirectory);
            }
        }

        [Fact]
        public void Main_IndexSnapshotsMode_RecognizedOptionAsNextToken_CountsAsMissingValue()
        {
            string tempDirectory = CreateTempDirectory();
            string outputPath = Path.Combine(tempDirectory, "run-index.json");

            try
            {
                var result = InvokeMain(
                    "index-snapshots",
                    "--snapshot",
                    "--output", outputPath);

                Assert.Equal(1, result.ExitCode);
                Assert.Equal(string.Empty, result.StdOut);
                Assert.Contains("Missing value for '--snapshot'.", result.StdErr);
                Assert.Contains(IndexSnapshotsUsage, result.StdErr);
            }
            finally
            {
                DeleteDirectoryIfExists(tempDirectory);
            }
        }

        [Fact]
        public void Main_IndexSnapshotsMode_UnknownArgument_IsRejected()
        {
            string tempDirectory = CreateTempDirectory();
            string snapshotPath = Path.Combine(tempDirectory, "snapshots", "first.json");
            string outputPath = Path.Combine(tempDirectory, "run-index.json");

            try
            {
                CreateFixtureSnapshot(snapshotPath);

                var result = InvokeMain(
                    "index-snapshots",
                    "--snapshot", snapshotPath,
                    "--foo", "bar",
                    "-o", outputPath);

                Assert.Equal(1, result.ExitCode);
                Assert.Equal(string.Empty, result.StdOut);
                Assert.Contains("Unrecognized index-snapshots argument '--foo'.", result.StdErr);
                Assert.Contains(IndexSnapshotsUsage, result.StdErr);
            }
            finally
            {
                DeleteDirectoryIfExists(tempDirectory);
            }
        }

        [Fact]
        public void Main_IndexSnapshotsMode_PositionalSnapshotArgument_IsRejected()
        {
            string tempDirectory = CreateTempDirectory();
            string snapshotPath = Path.Combine(tempDirectory, "snapshots", "first.json");
            string outputPath = Path.Combine(tempDirectory, "run-index.json");

            try
            {
                CreateFixtureSnapshot(snapshotPath);

                var result = InvokeMain(
                    "index-snapshots",
                    snapshotPath,
                    "-o", outputPath);

                Assert.Equal(1, result.ExitCode);
                Assert.Equal(string.Empty, result.StdOut);
                Assert.Contains($"Unrecognized index-snapshots argument '{snapshotPath}'.", result.StdErr);
                Assert.Contains(IndexSnapshotsUsage, result.StdErr);
            }
            finally
            {
                DeleteDirectoryIfExists(tempDirectory);
            }
        }

        [Fact]
        public void Main_IndexSnapshotsMode_MissingFirstSnapshotFile_Fails()
        {
            string tempDirectory = CreateTempDirectory();
            string missingSnapshotPath = Path.Combine(tempDirectory, "missing.json");
            string outputPath = Path.Combine(tempDirectory, "run-index.json");

            try
            {
                var result = InvokeMain(
                    "index-snapshots",
                    "--snapshot", missingSnapshotPath,
                    "-o", outputPath);

                Assert.Equal(1, result.ExitCode);
                Assert.Equal(string.Empty, result.StdOut);
                Assert.Contains("Failed to create run index:", result.StdErr);
                Assert.False(File.Exists(outputPath));
            }
            finally
            {
                DeleteDirectoryIfExists(tempDirectory);
            }
        }

        [Fact]
        public void Main_IndexSnapshotsMode_MissingLaterSnapshotFile_Fails()
        {
            string tempDirectory = CreateTempDirectory();
            string validSnapshotPath = Path.Combine(tempDirectory, "snapshots", "first.json");
            string missingSnapshotPath = Path.Combine(tempDirectory, "snapshots", "missing.json");
            string outputPath = Path.Combine(tempDirectory, "run-index.json");

            try
            {
                CreateFixtureSnapshot(validSnapshotPath);

                var result = InvokeMain(
                    "index-snapshots",
                    "--snapshot", validSnapshotPath,
                    "--snapshot", missingSnapshotPath,
                    "-o", outputPath);

                Assert.Equal(1, result.ExitCode);
                Assert.Equal(string.Empty, result.StdOut);
                Assert.Contains("Failed to create run index:", result.StdErr);
                Assert.False(File.Exists(outputPath));
            }
            finally
            {
                DeleteDirectoryIfExists(tempDirectory);
            }
        }

        [Fact]
        public void Main_IndexSnapshotsMode_InvalidFirstSnapshot_Fails()
        {
            string tempDirectory = CreateTempDirectory();
            string invalidSnapshotPath = Path.Combine(tempDirectory, "invalid.json");
            string outputPath = Path.Combine(tempDirectory, "run-index.json");

            try
            {
                File.WriteAllText(invalidSnapshotPath, "{ \"schemaVersion\": 1, ");

                var result = InvokeMain(
                    "index-snapshots",
                    "--snapshot", invalidSnapshotPath,
                    "-o", outputPath);

                Assert.Equal(1, result.ExitCode);
                Assert.Equal(string.Empty, result.StdOut);
                Assert.Contains("Failed to create run index:", result.StdErr);
                Assert.False(File.Exists(outputPath));
            }
            finally
            {
                DeleteDirectoryIfExists(tempDirectory);
            }
        }

        [Fact]
        public void Main_IndexSnapshotsMode_InvalidLaterSnapshot_FailsWithoutCreatingIndexOrSuccessSummary()
        {
            string tempDirectory = CreateTempDirectory();
            string validSnapshotPath = Path.Combine(tempDirectory, "snapshots", "first.json");
            string invalidSnapshotPath = Path.Combine(tempDirectory, "snapshots", "invalid.json");
            string outputPath = Path.Combine(tempDirectory, "run-index.json");

            try
            {
                CreateFixtureSnapshot(validSnapshotPath);
                File.WriteAllText(invalidSnapshotPath, "{ \"schemaVersion\": 1, ");

                var result = InvokeMain(
                    "index-snapshots",
                    "--snapshot", validSnapshotPath,
                    "--snapshot", invalidSnapshotPath,
                    "-o", outputPath);

                Assert.Equal(1, result.ExitCode);
                Assert.DoesNotContain("Indexed snapshots:", result.StdOut, StringComparison.Ordinal);
                Assert.DoesNotContain("Run index written to", result.StdOut, StringComparison.Ordinal);
                Assert.Contains("Failed to create run index:", result.StdErr);
                Assert.False(File.Exists(outputPath));
            }
            finally
            {
                DeleteDirectoryIfExists(tempDirectory);
            }
        }

        [Fact]
        public void Main_IndexSnapshotsMode_OutputAlias_Works()
        {
            string tempDirectory = CreateTempDirectory();
            string snapshotPath = Path.Combine(tempDirectory, "snapshots", "first.json");
            string outputPath = Path.Combine(tempDirectory, "run-index.json");

            try
            {
                CreateFixtureSnapshot(snapshotPath);

                var result = InvokeMain(
                    "index-snapshots",
                    "--snapshot", snapshotPath,
                    "--output", outputPath);

                Assert.Equal(0, result.ExitCode);
                Assert.Equal(string.Empty, result.StdErr);
                Assert.True(File.Exists(outputPath));
            }
            finally
            {
                DeleteDirectoryIfExists(tempDirectory);
            }
        }

        [Fact]
        public void Main_IndexSnapshotsMode_ExistingOutput_IsRefusedAndNotModified()
        {
            string tempDirectory = CreateTempDirectory();
            string snapshotPath = Path.Combine(tempDirectory, "snapshots", "first.json");
            string outputPath = Path.Combine(tempDirectory, "run-index.json");

            try
            {
                CreateFixtureSnapshot(snapshotPath);
                File.WriteAllText(outputPath, "ORIGINAL-INDEX-SENTINEL", new UTF8Encoding(false));

                var result = InvokeMain(
                    "index-snapshots",
                    "--snapshot", snapshotPath,
                    "-o", outputPath);

                Assert.Equal(1, result.ExitCode);
                Assert.Equal(string.Empty, result.StdOut);
                Assert.Contains("Failed to create run index:", result.StdErr);
                Assert.Equal("ORIGINAL-INDEX-SENTINEL", File.ReadAllText(outputPath), StringComparer.Ordinal);
            }
            finally
            {
                DeleteDirectoryIfExists(tempDirectory);
            }
        }

        [Fact]
        public void Main_IndexSnapshotsMode_MissingParentOutputDirectory_FailsAndIsNotCreated()
        {
            string tempDirectory = CreateTempDirectory();
            string snapshotPath = Path.Combine(tempDirectory, "snapshots", "first.json");
            string missingParentDirectory = Path.Combine(tempDirectory, "missing-parent");
            string outputPath = Path.Combine(missingParentDirectory, "run-index.json");

            try
            {
                CreateFixtureSnapshot(snapshotPath);

                var result = InvokeMain(
                    "index-snapshots",
                    "--snapshot", snapshotPath,
                    "-o", outputPath);

                Assert.Equal(1, result.ExitCode);
                Assert.Equal(string.Empty, result.StdOut);
                Assert.Contains("Failed to create run index:", result.StdErr);
                Assert.False(File.Exists(outputPath));
                Assert.False(Directory.Exists(missingParentDirectory));
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

        private static void WriteSnapshotCopyWithMetadata(string sourcePath, string destinationPath, string runId, string createdAt)
        {
            string? parentDirectory = Path.GetDirectoryName(destinationPath);
            if (!string.IsNullOrEmpty(parentDirectory))
            {
                Directory.CreateDirectory(parentDirectory);
            }

            string json = File.ReadAllText(sourcePath);
            JsonObject root = JsonNode.Parse(json)?.AsObject()
                ?? throw new InvalidOperationException("Failed to parse the seed snapshot JSON.");

            root["runId"] = runId;
            root["createdAt"] = createdAt;

            File.WriteAllText(
                destinationPath,
                root.ToJsonString(new JsonSerializerOptions { WriteIndented = true }),
                new UTF8Encoding(false));
        }

        private static void AssertPathsEqual(string expectedPath, string actualPath)
        {
            string expectedFullPath = Path.GetFullPath(expectedPath);
            string actualFullPath = Path.GetFullPath(actualPath);
            StringComparer comparer = OperatingSystem.IsWindows() ? StringComparer.OrdinalIgnoreCase : StringComparer.Ordinal;
            Assert.Equal(expectedFullPath, actualFullPath, comparer);
        }

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

        private static string NormalizeLineEndings(string value) =>
            value.ReplaceLineEndings("\n").TrimEnd('\n');
    }
}
