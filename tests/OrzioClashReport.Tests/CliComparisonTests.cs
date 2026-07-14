using System;
using System.IO;
using System.Reflection;
using System.Text.Json.Nodes;
using OrzioClashReport.Cli;

namespace OrzioClashReport.Tests
{
    [CollectionDefinition("CliConsole", DisableParallelization = true)]
    public sealed class CliConsoleCollectionDefinition
    {
    }

    [Collection("CliConsole")]
    public sealed class CliComparisonTests
    {
        private static readonly MethodInfo MainMethod = ResolveMainMethod();

        private static string SamplesDirectory => Path.Combine(AppContext.BaseDirectory, "samples");

        private static string SampleClashXmlPath => Path.Combine(SamplesDirectory, "sample-clash.xml");

        private static string SampleClashManifestPath => Path.Combine(SamplesDirectory, "sample-clash.run-manifest.json");

        private static string SampleRunManifestPath => Path.Combine(SamplesDirectory, "run-manifest.sample.json");

        [Fact]
        public void Main_LegacyMode_ContinuesWorking()
        {
            string tempDirectory = CreateTempDirectory();
            string outputPath = Path.Combine(tempDirectory, "report.html");

            try
            {
                var result = InvokeMain(SampleClashXmlPath, "-o", outputPath);

                Assert.Equal(0, result.ExitCode);
                Assert.True(File.Exists(outputPath));
                Assert.Contains("raw clashes ->", result.StdOut);
                Assert.Contains("Report written to", result.StdOut);
                Assert.Equal(string.Empty, result.StdErr);
            }
            finally
            {
                DeleteFileIfExists(outputPath);
                DeleteDirectoryIfExists(tempDirectory);
            }
        }

        [Fact]
        public void Main_CompareMode_WithRealFixtures_WritesDeterministicSummary()
        {
            var result = InvokeMain(
                "compare",
                "--previous-xml", SampleClashXmlPath,
                "--previous-manifest", SampleClashManifestPath,
                "--current-xml", SampleClashXmlPath,
                "--current-manifest", SampleClashManifestPath);

            Assert.Equal(0, result.ExitCode);
            Assert.Equal(string.Empty, result.StdErr);
            Assert.Equal(
                string.Join(
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
                    "Unverifiable: 0"),
                NormalizeLineEndings(result.StdOut));
        }

        [Fact]
        public void Main_CompareMode_DoesNotReorderPreviousAndCurrentByTimestamp()
        {
            string tempDirectory = CreateTempDirectory();
            string previousManifestPath = Path.Combine(tempDirectory, "previous.run-manifest.json");
            string currentManifestPath = Path.Combine(tempDirectory, "current.run-manifest.json");

            try
            {
                WriteManifestCopy(previousManifestPath, "role-previous", "2026-07-14T12:00:00+01:00");
                WriteManifestCopy(currentManifestPath, "role-current", "2026-07-13T12:00:00+01:00");

                var result = InvokeMain(
                    "compare",
                    "--previous-xml", SampleClashXmlPath,
                    "--previous-manifest", previousManifestPath,
                    "--current-xml", SampleClashXmlPath,
                    "--current-manifest", currentManifestPath);

                Assert.Equal(0, result.ExitCode);
                Assert.Equal(string.Empty, result.StdErr);

                string[] lines = NormalizeLineEndings(result.StdOut).Split('\n', StringSplitOptions.RemoveEmptyEntries);
                Assert.True(lines.Length >= 2);
                Assert.Equal("Previous run: role-previous", lines[0]);
                Assert.Equal("Current run: role-current", lines[1]);
            }
            finally
            {
                DeleteFileIfExists(previousManifestPath);
                DeleteFileIfExists(currentManifestPath);
                DeleteDirectoryIfExists(tempDirectory);
            }
        }

        [Fact]
        public void Main_CompareMode_MissingRequiredOption_ReturnsUsageError()
        {
            var result = InvokeMain(
                "compare",
                "--previous-xml", SampleClashXmlPath,
                "--previous-manifest", SampleClashManifestPath,
                "--current-xml", SampleClashXmlPath);

            Assert.Equal(1, result.ExitCode);
            Assert.Equal(string.Empty, result.StdOut);
            Assert.Contains("Missing required option '--current-manifest'.", result.StdErr);
            Assert.Contains("Usage: orzioclash compare --previous-xml <previous.xml> --previous-manifest <previous.json> --current-xml <current.xml> --current-manifest <current.json>", result.StdErr);
        }

        [Fact]
        public void Main_CompareMode_DuplicateOption_ReturnsUsageError()
        {
            var result = InvokeMain(
                "compare",
                "--previous-xml", SampleClashXmlPath,
                "--previous-xml", SampleClashXmlPath,
                "--previous-manifest", SampleClashManifestPath,
                "--current-xml", SampleClashXmlPath,
                "--current-manifest", SampleClashManifestPath);

            Assert.Equal(1, result.ExitCode);
            Assert.Equal(string.Empty, result.StdOut);
            Assert.Contains("Duplicate option '--previous-xml'.", result.StdErr);
            Assert.Contains("Usage: orzioclash compare --previous-xml <previous.xml> --previous-manifest <previous.json> --current-xml <current.xml> --current-manifest <current.json>", result.StdErr);
        }

        [Fact]
        public void Main_CompareMode_MissingValue_ReturnsUsageError()
        {
            var result = InvokeMain(
                "compare",
                "--previous-xml", SampleClashXmlPath,
                "--previous-manifest", SampleClashManifestPath,
                "--current-xml", SampleClashXmlPath,
                "--current-manifest");

            Assert.Equal(1, result.ExitCode);
            Assert.Equal(string.Empty, result.StdOut);
            Assert.Contains("Missing value for '--current-manifest'.", result.StdErr);
            Assert.Contains("Usage: orzioclash compare --previous-xml <previous.xml> --previous-manifest <previous.json> --current-xml <current.xml> --current-manifest <current.json>", result.StdErr);
        }

        [Fact]
        public void Main_CompareMode_RecognizedOptionAfterPreviousXml_IsMissingValue()
        {
            var result = InvokeMain(
                "compare",
                "--previous-xml",
                "--current-xml", SampleClashXmlPath,
                "--previous-manifest", SampleClashManifestPath,
                "--current-manifest", SampleClashManifestPath);

            Assert.Equal(1, result.ExitCode);
            Assert.Equal(string.Empty, result.StdOut);
            Assert.Contains("Missing value for '--previous-xml'.", result.StdErr);
            Assert.Contains("Usage: orzioclash compare --previous-xml <previous.xml> --previous-manifest <previous.json> --current-xml <current.xml> --current-manifest <current.json>", result.StdErr);
        }

        [Fact]
        public void Main_CompareMode_RecognizedOptionAfterPreviousManifest_IsMissingValue()
        {
            var result = InvokeMain(
                "compare",
                "--previous-xml", SampleClashXmlPath,
                "--previous-manifest",
                "--current-manifest", SampleClashManifestPath,
                "--current-xml", SampleClashXmlPath);

            Assert.Equal(1, result.ExitCode);
            Assert.Equal(string.Empty, result.StdOut);
            Assert.Contains("Missing value for '--previous-manifest'.", result.StdErr);
            Assert.Contains("Usage: orzioclash compare --previous-xml <previous.xml> --previous-manifest <previous.json> --current-xml <current.xml> --current-manifest <current.json>", result.StdErr);
        }

        [Fact]
        public void Main_CompareMode_UnknownArgument_ReturnsUsageError()
        {
            var result = InvokeMain(
                "compare",
                "--previous-xml", SampleClashXmlPath,
                "--previous-manifest", SampleClashManifestPath,
                "--current-xml", SampleClashXmlPath,
                "--current-manifest", SampleClashManifestPath,
                "--foo", "bar");

            Assert.Equal(1, result.ExitCode);
            Assert.Equal(string.Empty, result.StdOut);
            Assert.Contains("Unrecognized compare argument '--foo'.", result.StdErr);
            Assert.Contains("Usage: orzioclash compare --previous-xml <previous.xml> --previous-manifest <previous.json> --current-xml <current.xml> --current-manifest <current.json>", result.StdErr);
        }

        [Fact]
        public void Main_CompareMode_OutputOptionIsRejected()
        {
            string tempDirectory = CreateTempDirectory();
            string outputPath = Path.Combine(tempDirectory, "result.html");

            try
            {
                var result = InvokeMain(
                    "compare",
                    "--previous-xml", SampleClashXmlPath,
                    "--previous-manifest", SampleClashManifestPath,
                    "--current-xml", SampleClashXmlPath,
                    "--current-manifest", SampleClashManifestPath,
                    "-o", outputPath);

                Assert.Equal(1, result.ExitCode);
                Assert.Equal(string.Empty, result.StdOut);
                Assert.Contains("Unrecognized compare argument '-o'.", result.StdErr);
                Assert.False(File.Exists(outputPath));
            }
            finally
            {
                DeleteFileIfExists(outputPath);
                DeleteDirectoryIfExists(tempDirectory);
            }
        }

        [Fact]
        public void Main_CompareMode_MissingCurrentXmlFile_ReturnsContextualError()
        {
            string missingPath = Path.Combine(Path.GetTempPath(), $"orzio-clash-report-missing-{Guid.NewGuid():N}.xml");

            var result = InvokeMain(
                "compare",
                "--previous-xml", SampleClashXmlPath,
                "--previous-manifest", SampleClashManifestPath,
                "--current-xml", missingPath,
                "--current-manifest", SampleClashManifestPath);

            Assert.Equal(1, result.ExitCode);
            Assert.Equal(string.Empty, result.StdOut);
            Assert.Contains("Current XML file not found:", result.StdErr);
            Assert.Contains(missingPath, result.StdErr);
            Assert.DoesNotContain("Usage: orzioclash compare", result.StdErr);
        }

        [Fact]
        public void Main_CompareMode_IncompatibleManifestFailsComparison()
        {
            var result = InvokeMain(
                "compare",
                "--previous-xml", SampleClashXmlPath,
                "--previous-manifest", SampleClashManifestPath,
                "--current-xml", SampleClashXmlPath,
                "--current-manifest", SampleRunManifestPath);

            Assert.Equal(1, result.ExitCode);
            Assert.DoesNotContain("Previous run:", result.StdOut);
            Assert.Contains("Failed to compare runs:", result.StdErr);
            Assert.Contains("no manifest model matched SourceFileName or SourceFilePath", result.StdErr);
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

        private static void WriteManifestCopy(string destinationPath, string runId, string createdAt)
        {
            string json = File.ReadAllText(SampleClashManifestPath);
            JsonObject root = JsonNode.Parse(json)?.AsObject()
                ?? throw new InvalidOperationException("Failed to parse the sample clash manifest fixture.");

            root["runId"] = runId;
            root["createdAt"] = createdAt;

            File.WriteAllText(destinationPath, root.ToJsonString(new System.Text.Json.JsonSerializerOptions { WriteIndented = true }));
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
