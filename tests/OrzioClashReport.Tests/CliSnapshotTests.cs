using System;
using System.IO;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Nodes;
using OrzioClashReport.Cli;
using OrzioClashReport.Core.Model;
using OrzioClashReport.Persistence.RunSnapshotJson;

namespace OrzioClashReport.Tests
{
    [Collection("CliConsole")]
    public sealed class CliSnapshotTests
    {
        private static readonly MethodInfo MainMethod = ResolveMainMethod();
        private const string SnapshotUsage = "Usage: orzioclash snapshot --xml <input.xml> --manifest <run-manifest.json> (-o <run-snapshot.json> | --output <run-snapshot.json>)";

        private static string SamplesDirectory => Path.Combine(AppContext.BaseDirectory, "samples");

        private static string SampleClashXmlPath => Path.Combine(SamplesDirectory, "sample-clash.xml");

        private static string SampleClashManifestPath => Path.Combine(SamplesDirectory, "sample-clash.run-manifest.json");

        [Fact]
        public void Main_SnapshotMode_WithRealFixtures_CreatesImmutableSnapshot()
        {
            string tempDirectory = CreateTempDirectory();
            string outputPath = Path.Combine(tempDirectory, "run-snapshot.json");

            try
            {
                var result = InvokeMain(
                    "snapshot",
                    "--xml", SampleClashXmlPath,
                    "--manifest", SampleClashManifestPath,
                    "-o", outputPath);

                Assert.Equal(0, result.ExitCode);
                Assert.Equal(string.Empty, result.StdErr);
                Assert.True(File.Exists(outputPath));
                Assert.Equal(
                    string.Join(
                        "\n",
                        "Run snapshot: coordination-sample-clash-xml",
                        "Models: 2",
                        "Executed clash tests: 2",
                        "Occurrences: 5",
                        $"Snapshot written to {outputPath}"),
                    result.StdOut);

                var serializer = new JsonCoordinationRunSnapshotSerializer();
                CoordinationRun loaded = serializer.Load(outputPath);

                Assert.Equal("coordination-sample-clash-xml", loaded.RunId);
                Assert.Equal(2, loaded.Models.Count);
                Assert.Equal(2, loaded.ExecutedClashTests.Count);
                Assert.Equal(5, loaded.Occurrences.Count);

                string persisted = File.ReadAllText(outputPath);
                Assert.Equal(persisted, serializer.Serialize(loaded), StringComparer.Ordinal);
            }
            finally
            {
                DeleteFileIfExists(outputPath);
                DeleteDirectoryIfExists(tempDirectory);
            }
        }

        [Fact]
        public void Main_SnapshotMode_OutputAlias_CreatesSnapshot()
        {
            string tempDirectory = CreateTempDirectory();
            string outputPath = Path.Combine(tempDirectory, "run-snapshot.json");

            try
            {
                var result = InvokeMain(
                    "snapshot",
                    "--xml", SampleClashXmlPath,
                    "--manifest", SampleClashManifestPath,
                    "--output", outputPath);

                Assert.Equal(0, result.ExitCode);
                Assert.Equal(string.Empty, result.StdErr);
                Assert.True(File.Exists(outputPath));
                Assert.EndsWith($"Snapshot written to {outputPath}", result.StdOut, StringComparison.Ordinal);
            }
            finally
            {
                DeleteFileIfExists(outputPath);
                DeleteDirectoryIfExists(tempDirectory);
            }
        }

        [Fact]
        public void Main_SnapshotMode_CommandName_IsCaseInsensitive()
        {
            string tempDirectory = CreateTempDirectory();
            string outputPath = Path.Combine(tempDirectory, "run-snapshot.json");

            try
            {
                var result = InvokeMain(
                    "SNAPSHOT",
                    "--xml", SampleClashXmlPath,
                    "--manifest", SampleClashManifestPath,
                    "-o", outputPath);

                Assert.Equal(0, result.ExitCode);
                Assert.Equal(string.Empty, result.StdErr);
                Assert.True(File.Exists(outputPath));
            }
            finally
            {
                DeleteFileIfExists(outputPath);
                DeleteDirectoryIfExists(tempDirectory);
            }
        }

        [Fact]
        public void Main_SnapshotMode_MissingXml_ReturnsUsageError()
        {
            string tempDirectory = CreateTempDirectory();
            string outputPath = Path.Combine(tempDirectory, "run-snapshot.json");

            try
            {
                var result = InvokeMain(
                    "snapshot",
                    "--manifest", SampleClashManifestPath,
                    "-o", outputPath);

                Assert.Equal(1, result.ExitCode);
                Assert.Equal(string.Empty, result.StdOut);
                Assert.Contains("Missing required option '--xml'.", result.StdErr);
                Assert.Contains(SnapshotUsage, result.StdErr);
                Assert.False(File.Exists(outputPath));
            }
            finally
            {
                DeleteFileIfExists(outputPath);
                DeleteDirectoryIfExists(tempDirectory);
            }
        }

        [Fact]
        public void Main_SnapshotMode_MissingManifest_ReturnsUsageError()
        {
            string tempDirectory = CreateTempDirectory();
            string outputPath = Path.Combine(tempDirectory, "run-snapshot.json");

            try
            {
                var result = InvokeMain(
                    "snapshot",
                    "--xml", SampleClashXmlPath,
                    "-o", outputPath);

                Assert.Equal(1, result.ExitCode);
                Assert.Equal(string.Empty, result.StdOut);
                Assert.Contains("Missing required option '--manifest'.", result.StdErr);
                Assert.Contains(SnapshotUsage, result.StdErr);
                Assert.False(File.Exists(outputPath));
            }
            finally
            {
                DeleteFileIfExists(outputPath);
                DeleteDirectoryIfExists(tempDirectory);
            }
        }

        [Fact]
        public void Main_SnapshotMode_MissingOutput_ReturnsUsageError()
        {
            var result = InvokeMain(
                "snapshot",
                "--xml", SampleClashXmlPath,
                "--manifest", SampleClashManifestPath);

            Assert.Equal(1, result.ExitCode);
            Assert.Equal(string.Empty, result.StdOut);
            Assert.Contains("Missing required option '-o/--output'.", result.StdErr);
            Assert.Contains(SnapshotUsage, result.StdErr);
        }

        [Fact]
        public void Main_SnapshotMode_DuplicateXml_ReturnsUsageError()
        {
            string tempDirectory = CreateTempDirectory();
            string outputPath = Path.Combine(tempDirectory, "run-snapshot.json");

            try
            {
                var result = InvokeMain(
                    "snapshot",
                    "--xml", SampleClashXmlPath,
                    "--xml", SampleClashXmlPath,
                    "--manifest", SampleClashManifestPath,
                    "-o", outputPath);

                Assert.Equal(1, result.ExitCode);
                Assert.Equal(string.Empty, result.StdOut);
                Assert.Contains("Duplicate option '--xml'.", result.StdErr);
                Assert.Contains(SnapshotUsage, result.StdErr);
                Assert.False(File.Exists(outputPath));
            }
            finally
            {
                DeleteFileIfExists(outputPath);
                DeleteDirectoryIfExists(tempDirectory);
            }
        }

        [Fact]
        public void Main_SnapshotMode_DuplicateManifest_ReturnsUsageError()
        {
            string tempDirectory = CreateTempDirectory();
            string outputPath = Path.Combine(tempDirectory, "run-snapshot.json");

            try
            {
                var result = InvokeMain(
                    "snapshot",
                    "--xml", SampleClashXmlPath,
                    "--manifest", SampleClashManifestPath,
                    "--manifest", SampleClashManifestPath,
                    "-o", outputPath);

                Assert.Equal(1, result.ExitCode);
                Assert.Equal(string.Empty, result.StdOut);
                Assert.Contains("Duplicate option '--manifest'.", result.StdErr);
                Assert.Contains(SnapshotUsage, result.StdErr);
                Assert.False(File.Exists(outputPath));
            }
            finally
            {
                DeleteFileIfExists(outputPath);
                DeleteDirectoryIfExists(tempDirectory);
            }
        }

        [Fact]
        public void Main_SnapshotMode_DuplicateOutputAliases_ReturnsUsageError()
        {
            string tempDirectory = CreateTempDirectory();
            string firstOutputPath = Path.Combine(tempDirectory, "first.json");
            string secondOutputPath = Path.Combine(tempDirectory, "second.json");

            try
            {
                var result = InvokeMain(
                    "snapshot",
                    "--xml", SampleClashXmlPath,
                    "--manifest", SampleClashManifestPath,
                    "-o", firstOutputPath,
                    "--output", secondOutputPath);

                Assert.Equal(1, result.ExitCode);
                Assert.Equal(string.Empty, result.StdOut);
                Assert.Contains("Duplicate option '-o/--output'.", result.StdErr);
                Assert.Contains(SnapshotUsage, result.StdErr);
                Assert.False(File.Exists(firstOutputPath));
                Assert.False(File.Exists(secondOutputPath));
            }
            finally
            {
                DeleteFileIfExists(firstOutputPath);
                DeleteFileIfExists(secondOutputPath);
                DeleteDirectoryIfExists(tempDirectory);
            }
        }

        [Fact]
        public void Main_SnapshotMode_RecognizedOptionAfterXml_IsMissingValue()
        {
            string tempDirectory = CreateTempDirectory();
            string outputPath = Path.Combine(tempDirectory, "run-snapshot.json");

            try
            {
                var result = InvokeMain(
                    "snapshot",
                    "--xml",
                    "--manifest", SampleClashManifestPath,
                    "-o", outputPath);

                Assert.Equal(1, result.ExitCode);
                Assert.Equal(string.Empty, result.StdOut);
                Assert.Contains("Missing value for '--xml'.", result.StdErr);
                Assert.Contains(SnapshotUsage, result.StdErr);
                Assert.False(File.Exists(outputPath));
            }
            finally
            {
                DeleteFileIfExists(outputPath);
                DeleteDirectoryIfExists(tempDirectory);
            }
        }

        [Fact]
        public void Main_SnapshotMode_RecognizedOptionAfterManifest_IsMissingValue()
        {
            string tempDirectory = CreateTempDirectory();
            string outputPath = Path.Combine(tempDirectory, "run-snapshot.json");

            try
            {
                var result = InvokeMain(
                    "snapshot",
                    "--xml", SampleClashXmlPath,
                    "--manifest",
                    "-o", outputPath);

                Assert.Equal(1, result.ExitCode);
                Assert.Equal(string.Empty, result.StdOut);
                Assert.Contains("Missing value for '--manifest'.", result.StdErr);
                Assert.Contains(SnapshotUsage, result.StdErr);
                Assert.False(File.Exists(outputPath));
            }
            finally
            {
                DeleteFileIfExists(outputPath);
                DeleteDirectoryIfExists(tempDirectory);
            }
        }

        [Fact]
        public void Main_SnapshotMode_RecognizedOptionAfterOutput_IsMissingValue()
        {
            var result = InvokeMain(
                "snapshot",
                "--xml", SampleClashXmlPath,
                "-o",
                "--manifest", SampleClashManifestPath);

            Assert.Equal(1, result.ExitCode);
            Assert.Equal(string.Empty, result.StdOut);
            Assert.Contains("Missing value for '-o'.", result.StdErr);
            Assert.Contains(SnapshotUsage, result.StdErr);
        }

        [Fact]
        public void Main_SnapshotMode_UnknownArgument_ReturnsUsageError()
        {
            string tempDirectory = CreateTempDirectory();
            string outputPath = Path.Combine(tempDirectory, "run-snapshot.json");

            try
            {
                var result = InvokeMain(
                    "snapshot",
                    "--xml", SampleClashXmlPath,
                    "--manifest", SampleClashManifestPath,
                    "-o", outputPath,
                    "--foo", "bar");

                Assert.Equal(1, result.ExitCode);
                Assert.Equal(string.Empty, result.StdOut);
                Assert.Contains("Unrecognized snapshot argument '--foo'.", result.StdErr);
                Assert.Contains(SnapshotUsage, result.StdErr);
                Assert.False(File.Exists(outputPath));
            }
            finally
            {
                DeleteFileIfExists(outputPath);
                DeleteDirectoryIfExists(tempDirectory);
            }
        }

        [Fact]
        public void Main_SnapshotMode_PositionalXmlArgument_IsRejected()
        {
            string tempDirectory = CreateTempDirectory();
            string outputPath = Path.Combine(tempDirectory, "run-snapshot.json");

            try
            {
                var result = InvokeMain(
                    "snapshot",
                    SampleClashXmlPath,
                    "--manifest", SampleClashManifestPath,
                    "-o", outputPath);

                Assert.Equal(1, result.ExitCode);
                Assert.Equal(string.Empty, result.StdOut);
                Assert.Contains($"Unrecognized snapshot argument '{SampleClashXmlPath}'.", result.StdErr);
                Assert.Contains(SnapshotUsage, result.StdErr);
                Assert.False(File.Exists(outputPath));
            }
            finally
            {
                DeleteFileIfExists(outputPath);
                DeleteDirectoryIfExists(tempDirectory);
            }
        }

        [Fact]
        public void Main_SnapshotMode_MissingXmlFile_ReturnsContextualError()
        {
            string tempDirectory = CreateTempDirectory();
            string missingXmlPath = Path.Combine(tempDirectory, "missing.xml");
            string outputPath = Path.Combine(tempDirectory, "run-snapshot.json");

            try
            {
                var result = InvokeMain(
                    "snapshot",
                    "--xml", missingXmlPath,
                    "--manifest", SampleClashManifestPath,
                    "-o", outputPath);

                Assert.Equal(1, result.ExitCode);
                Assert.Equal(string.Empty, result.StdOut);
                Assert.Contains("Snapshot XML file not found:", result.StdErr);
                Assert.Contains(missingXmlPath, result.StdErr);
                Assert.DoesNotContain(SnapshotUsage, result.StdErr, StringComparison.Ordinal);
                Assert.False(File.Exists(outputPath));
            }
            finally
            {
                DeleteFileIfExists(outputPath);
                DeleteDirectoryIfExists(tempDirectory);
            }
        }

        [Fact]
        public void Main_SnapshotMode_MissingManifestFile_ReturnsContextualError()
        {
            string tempDirectory = CreateTempDirectory();
            string missingManifestPath = Path.Combine(tempDirectory, "missing.run-manifest.json");
            string outputPath = Path.Combine(tempDirectory, "run-snapshot.json");

            try
            {
                var result = InvokeMain(
                    "snapshot",
                    "--xml", SampleClashXmlPath,
                    "--manifest", missingManifestPath,
                    "-o", outputPath);

                Assert.Equal(1, result.ExitCode);
                Assert.Equal(string.Empty, result.StdOut);
                Assert.Contains("Snapshot manifest file not found:", result.StdErr);
                Assert.Contains(missingManifestPath, result.StdErr);
                Assert.DoesNotContain(SnapshotUsage, result.StdErr, StringComparison.Ordinal);
                Assert.False(File.Exists(outputPath));
            }
            finally
            {
                DeleteFileIfExists(outputPath);
                DeleteDirectoryIfExists(tempDirectory);
            }
        }

        [Fact]
        public void Main_SnapshotMode_ExistingOutput_RefusesOverwriteWithoutSuccessSummary()
        {
            string tempDirectory = CreateTempDirectory();
            string outputPath = Path.Combine(tempDirectory, "existing.json");

            try
            {
                File.WriteAllText(outputPath, "ORIGINAL-SNAPSHOT-SENTINEL");

                var result = InvokeMain(
                    "snapshot",
                    "--xml", SampleClashXmlPath,
                    "--manifest", SampleClashManifestPath,
                    "-o", outputPath);

                Assert.Equal(1, result.ExitCode);
                Assert.Equal(string.Empty, result.StdOut);
                Assert.Contains("Failed to create run snapshot:", result.StdErr);
                Assert.Contains("Coordination run snapshot file already exists:", result.StdErr);
                Assert.Equal("ORIGINAL-SNAPSHOT-SENTINEL", File.ReadAllText(outputPath), StringComparer.Ordinal);
            }
            finally
            {
                DeleteFileIfExists(outputPath);
                DeleteDirectoryIfExists(tempDirectory);
            }
        }

        [Fact]
        public void Main_SnapshotMode_MissingOutputParent_DoesNotPrintSuccessSummary()
        {
            string tempDirectory = CreateTempDirectory();
            string outputPath = Path.Combine(tempDirectory, "missing-parent", "run-snapshot.json");

            try
            {
                var result = InvokeMain(
                    "snapshot",
                    "--xml", SampleClashXmlPath,
                    "--manifest", SampleClashManifestPath,
                    "-o", outputPath);

                Assert.Equal(1, result.ExitCode);
                Assert.Equal(string.Empty, result.StdOut);
                Assert.Contains("Failed to create run snapshot:", result.StdErr);
                Assert.False(File.Exists(outputPath));
            }
            finally
            {
                DeleteFileIfExists(outputPath);
                DeleteDirectoryIfExists(tempDirectory);
            }
        }

        [Fact]
        public void Main_SnapshotMode_IncompatibleManifest_DoesNotCreateSnapshot()
        {
            string tempDirectory = CreateTempDirectory();
            string manifestPath = Path.Combine(tempDirectory, "incompatible.run-manifest.json");
            string outputPath = Path.Combine(tempDirectory, "run-snapshot.json");

            try
            {
                WriteIncompatibleManifestCopy(manifestPath);

                var result = InvokeMain(
                    "snapshot",
                    "--xml", SampleClashXmlPath,
                    "--manifest", manifestPath,
                    "-o", outputPath);

                Assert.Equal(1, result.ExitCode);
                Assert.Equal(string.Empty, result.StdOut);
                Assert.Contains("Failed to create run snapshot:", result.StdErr);
                Assert.Contains("no manifest model matched SourceFileName or SourceFilePath", result.StdErr);
                Assert.False(File.Exists(outputPath));
            }
            finally
            {
                DeleteFileIfExists(manifestPath);
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

        private static void WriteIncompatibleManifestCopy(string destinationPath)
        {
            string json = File.ReadAllText(SampleClashManifestPath);
            JsonObject root = JsonNode.Parse(json)?.AsObject()
                ?? throw new InvalidOperationException("Failed to parse the sample clash manifest fixture.");

            JsonArray models = root["models"]?.AsArray()
                ?? throw new InvalidOperationException("Expected a models array in the sample clash manifest fixture.");

            JsonObject firstModel = models[0]?.AsObject()
                ?? throw new InvalidOperationException("Expected the first model entry in the sample clash manifest fixture.");

            firstModel["sourceFileName"] = "INCOMPATIBLE_SOURCE_MODEL_TOKEN.rvt";

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
