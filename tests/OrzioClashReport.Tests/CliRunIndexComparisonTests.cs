using System;
using System.Collections.Generic;
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
    public sealed class CliRunIndexComparisonTests
    {
        private static readonly MethodInfo MainMethod = ResolveMainMethod();
        private const string CompareIndexUsage = "Usage: orzioclash compare-index --index <run-index.json>";
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
        public void Main_CompareIndexMode_WithThreeFreshSnapshots_WritesExpectedTwentySixLineStructure()
        {
            string tempDirectory = CreateTempDirectory();
            string firstSnapshotPath = Path.Combine(tempDirectory, "snapshots", "first.json");
            string secondSnapshotPath = Path.Combine(tempDirectory, "snapshots", "second.json");
            string thirdSnapshotPath = Path.Combine(tempDirectory, "snapshots", "third.json");
            string indexPath = Path.Combine(tempDirectory, "run-index.json");

            try
            {
                CreateFixtureSnapshot(firstSnapshotPath);
                CreateFixtureSnapshot(secondSnapshotPath);
                CreateFixtureSnapshot(thirdSnapshotPath);
                CreateRunIndexWithCommand(indexPath, firstSnapshotPath, secondSnapshotPath, thirdSnapshotPath);

                var result = InvokeMain("compare-index", "--index", indexPath);

                Assert.Equal(0, result.ExitCode);
                Assert.Equal(string.Empty, result.StdErr);
                Assert.DoesNotContain("Comparison report written to", result.StdOut, StringComparison.Ordinal);

                string[] lines = result.StdOut.Split('\n', StringSplitOptions.None);
                Assert.Equal(26, lines.Length);
                Assert.Equal("Indexed runs: 3", lines[0]);
                Assert.Equal("Adjacent comparisons: 2", lines[1]);
                Assert.Equal("Comparison 1/2", lines[2]);
                Assert.Equal("Comparison 2/2", lines[14]);
                Assert.DoesNotContain(lines, static line => line.Length == 0);
            }
            finally
            {
                DeleteDirectoryIfExists(tempDirectory);
            }
        }

        [Fact]
        public void Main_CompareIndexMode_UsesDeclaredAdjacentPairsWithoutSkippingFromFirstToThird()
        {
            string tempDirectory = CreateTempDirectory();
            string seedSnapshotPath = Path.Combine(tempDirectory, "seed.json");
            string firstSnapshotPath = Path.Combine(tempDirectory, "snapshots", "run-a.json");
            string secondSnapshotPath = Path.Combine(tempDirectory, "snapshots", "run-b.json");
            string thirdSnapshotPath = Path.Combine(tempDirectory, "snapshots", "run-c.json");
            string indexPath = Path.Combine(tempDirectory, "run-index.json");

            try
            {
                CreateFixtureSnapshot(seedSnapshotPath);
                WriteSnapshotCopyWithMetadata(seedSnapshotPath, firstSnapshotPath, "run-A", "2026-07-15T09:00:00+01:00");
                WriteSnapshotCopyWithMetadata(seedSnapshotPath, secondSnapshotPath, "run-B", "2026-07-15T10:00:00+01:00");
                WriteSnapshotCopyWithMetadata(seedSnapshotPath, thirdSnapshotPath, "run-C", "2026-07-15T11:00:00+01:00");
                CreateRunIndexWithCommand(indexPath, firstSnapshotPath, secondSnapshotPath, thirdSnapshotPath);

                var result = InvokeMain("compare-index", "--index", indexPath);

                Assert.Equal(0, result.ExitCode);
                Assert.Equal(string.Empty, result.StdErr);

                string[] lines = result.StdOut.Split('\n', StringSplitOptions.None);
                Assert.Equal("Comparison 1/2", lines[2]);
                Assert.Equal("Previous run: run-A", lines[3]);
                Assert.Equal("Current run: run-B", lines[4]);
                Assert.Equal("Comparison 2/2", lines[14]);
                Assert.Equal("Previous run: run-B", lines[15]);
                Assert.Equal("Current run: run-C", lines[16]);
                Assert.DoesNotContain("Previous run: run-A\nCurrent run: run-C", result.StdOut, StringComparison.Ordinal);
            }
            finally
            {
                DeleteDirectoryIfExists(tempDirectory);
            }
        }

        [Fact]
        public void Main_CompareIndexMode_MatchesCompareSnapshotsSummaryAfterThreeLineHeader()
        {
            string tempDirectory = CreateTempDirectory();
            string previousSnapshotPath = Path.Combine(tempDirectory, "snapshots", "previous.json");
            string currentSnapshotPath = Path.Combine(tempDirectory, "snapshots", "current.json");
            string indexPath = Path.Combine(tempDirectory, "run-index.json");

            try
            {
                CreateFixtureSnapshot(previousSnapshotPath);
                CreateFixtureSnapshot(currentSnapshotPath);
                CreateRunIndexWithCommand(indexPath, previousSnapshotPath, currentSnapshotPath);

                var snapshotResult = InvokeMain(
                    "compare-snapshots",
                    "--previous-snapshot", previousSnapshotPath,
                    "--current-snapshot", currentSnapshotPath);

                var indexResult = InvokeMain("compare-index", "--index", indexPath);

                Assert.Equal(0, snapshotResult.ExitCode);
                Assert.Equal(0, indexResult.ExitCode);
                Assert.Equal(string.Empty, snapshotResult.StdErr);
                Assert.Equal(string.Empty, indexResult.StdErr);

                string[] indexLines = indexResult.StdOut.Split('\n', StringSplitOptions.None);
                Assert.Equal(14, indexLines.Length);

                string compareIndexSummary = string.Join("\n", indexLines[3..]);
                Assert.Equal(snapshotResult.StdOut, compareIndexSummary, StringComparer.Ordinal);
            }
            finally
            {
                DeleteDirectoryIfExists(tempDirectory);
            }
        }

        [Fact]
        public void Main_CompareIndexMode_DoesNotReorderDeclaredOrderByCreatedAt()
        {
            string tempDirectory = CreateTempDirectory();
            string seedSnapshotPath = Path.Combine(tempDirectory, "seed.json");
            string laterCreatedPath = Path.Combine(tempDirectory, "snapshots", "later.json");
            string earlierCreatedPath = Path.Combine(tempDirectory, "snapshots", "earlier.json");
            string indexPath = Path.Combine(tempDirectory, "run-index.json");

            try
            {
                CreateFixtureSnapshot(seedSnapshotPath);
                WriteSnapshotCopyWithMetadata(seedSnapshotPath, laterCreatedPath, "run-later", "2026-07-15T12:00:00+01:00");
                WriteSnapshotCopyWithMetadata(seedSnapshotPath, earlierCreatedPath, "run-earlier", "2026-07-14T12:00:00+01:00");
                CreateRunIndexWithCommand(indexPath, laterCreatedPath, earlierCreatedPath);

                var result = InvokeMain("compare-index", "--index", indexPath);

                Assert.Equal(0, result.ExitCode);
                Assert.Equal(string.Empty, result.StdErr);

                string[] lines = result.StdOut.Split('\n', StringSplitOptions.None);
                Assert.Equal("Previous run: run-later", lines[3]);
                Assert.Equal("Current run: run-earlier", lines[4]);
            }
            finally
            {
                DeleteDirectoryIfExists(tempDirectory);
            }
        }

        [Fact]
        public void Main_CompareIndexMode_DoesNotReorderDeclaredOrderByRunId()
        {
            string tempDirectory = CreateTempDirectory();
            string seedSnapshotPath = Path.Combine(tempDirectory, "seed.json");
            string zRunPath = Path.Combine(tempDirectory, "snapshots", "z-run.json");
            string aRunPath = Path.Combine(tempDirectory, "snapshots", "a-run.json");
            string indexPath = Path.Combine(tempDirectory, "run-index.json");

            try
            {
                CreateFixtureSnapshot(seedSnapshotPath);
                WriteSnapshotCopyWithMetadata(seedSnapshotPath, zRunPath, "zzz-run", "2026-07-14T12:00:00+01:00");
                WriteSnapshotCopyWithMetadata(seedSnapshotPath, aRunPath, "aaa-run", "2026-07-14T12:00:00+01:00");
                CreateRunIndexWithCommand(indexPath, zRunPath, aRunPath);

                var result = InvokeMain("compare-index", "--index", indexPath);

                Assert.Equal(0, result.ExitCode);
                Assert.Equal(string.Empty, result.StdErr);

                string[] lines = result.StdOut.Split('\n', StringSplitOptions.None);
                Assert.Equal("Previous run: zzz-run", lines[3]);
                Assert.Equal("Current run: aaa-run", lines[4]);
            }
            finally
            {
                DeleteDirectoryIfExists(tempDirectory);
            }
        }

        [Fact]
        public void Main_CompareIndexMode_DuplicateSnapshotReferences_AreComparedAdjacently()
        {
            string tempDirectory = CreateTempDirectory();
            string snapshotPath = Path.Combine(tempDirectory, "snapshots", "shared.json");
            string indexPath = Path.Combine(tempDirectory, "run-index.json");

            try
            {
                CreateFixtureSnapshot(snapshotPath);
                CreateRunIndexWithCommand(indexPath, snapshotPath, snapshotPath);

                var result = InvokeMain("compare-index", "--index", indexPath);

                Assert.Equal(0, result.ExitCode);
                Assert.Equal(string.Empty, result.StdErr);

                string[] lines = result.StdOut.Split('\n', StringSplitOptions.None);
                Assert.Equal(14, lines.Length);
                Assert.Equal("Indexed runs: 2", lines[0]);
                Assert.Equal("Adjacent comparisons: 1", lines[1]);
                Assert.Equal("Comparison 1/1", lines[2]);
                Assert.Equal(ExpectedCompareSummary, string.Join("\n", lines[3..]), StringComparer.Ordinal);
            }
            finally
            {
                DeleteDirectoryIfExists(tempDirectory);
            }
        }

        [Fact]
        public void Main_CompareIndexMode_CommandName_IsCaseInsensitive()
        {
            string tempDirectory = CreateTempDirectory();
            string firstSnapshotPath = Path.Combine(tempDirectory, "snapshots", "first.json");
            string secondSnapshotPath = Path.Combine(tempDirectory, "snapshots", "second.json");
            string indexPath = Path.Combine(tempDirectory, "run-index.json");

            try
            {
                CreateFixtureSnapshot(firstSnapshotPath);
                CreateFixtureSnapshot(secondSnapshotPath);
                CreateRunIndexWithCommand(indexPath, firstSnapshotPath, secondSnapshotPath);

                var result = InvokeMain("COMPARE-INDEX", "--index", indexPath);

                Assert.Equal(0, result.ExitCode);
                Assert.Equal(string.Empty, result.StdErr);
            }
            finally
            {
                DeleteDirectoryIfExists(tempDirectory);
            }
        }

        [Fact]
        public void Main_CompareIndexMode_MissingIndexOption_ReturnsUsageError()
        {
            var result = InvokeMain("compare-index");

            Assert.Equal(1, result.ExitCode);
            Assert.Equal(string.Empty, result.StdOut);
            Assert.Contains("Missing required option '--index'.", result.StdErr);
            Assert.Contains(CompareIndexUsage, result.StdErr);
        }

        [Fact]
        public void Main_CompareIndexMode_DuplicateIndexOption_ReturnsUsageError()
        {
            var result = InvokeMain(
                "compare-index",
                "--index", "first.json",
                "--index", "second.json");

            Assert.Equal(1, result.ExitCode);
            Assert.Equal(string.Empty, result.StdOut);
            Assert.Contains("Duplicate option '--index'.", result.StdErr);
            Assert.Contains(CompareIndexUsage, result.StdErr);
        }

        [Fact]
        public void Main_CompareIndexMode_MissingIndexValue_ReturnsUsageError()
        {
            var result = InvokeMain("compare-index", "--index");

            Assert.Equal(1, result.ExitCode);
            Assert.Equal(string.Empty, result.StdOut);
            Assert.Contains("Missing value for '--index'.", result.StdErr);
            Assert.Contains(CompareIndexUsage, result.StdErr);
        }

        [Fact]
        public void Main_CompareIndexMode_RecognizedOptionAsNextToken_CountsAsMissingValue()
        {
            var result = InvokeMain(
                "compare-index",
                "--index",
                "--index", "run-index.json");

            Assert.Equal(1, result.ExitCode);
            Assert.Equal(string.Empty, result.StdOut);
            Assert.Contains("Missing value for '--index'.", result.StdErr);
            Assert.Contains(CompareIndexUsage, result.StdErr);
        }

        [Fact]
        public void Main_CompareIndexMode_UnsupportedOutputOption_IsRejectedAsUnknownArgument()
        {
            var result = InvokeMain(
                "compare-index",
                "--index", "run-index.json",
                "-o", "comparison.html");

            Assert.Equal(1, result.ExitCode);
            Assert.Equal(string.Empty, result.StdOut);
            Assert.Contains("Unrecognized compare-index argument '-o'.", result.StdErr);
            Assert.Contains(CompareIndexUsage, result.StdErr);
        }

        [Fact]
        public void Main_CompareIndexMode_UnknownLongArgument_IsRejectedAsUnknownArgument()
        {
            var result = InvokeMain(
                "compare-index",
                "--index", "run-index.json",
                "--bogus", "value");

            Assert.Equal(1, result.ExitCode);
            Assert.Equal(string.Empty, result.StdOut);
            Assert.Contains("Unrecognized compare-index argument '--bogus'.", result.StdErr);
            Assert.Contains(CompareIndexUsage, result.StdErr);
        }

        [Fact]
        public void Main_CompareIndexMode_LongOutputOption_IsRejectedAsUnknownArgument()
        {
            var result = InvokeMain(
                "compare-index",
                "--index", "run-index.json",
                "--output", "comparison.html");

            Assert.Equal(1, result.ExitCode);
            Assert.Equal(string.Empty, result.StdOut);
            Assert.Contains("Unrecognized compare-index argument '--output'.", result.StdErr);
            Assert.Contains(CompareIndexUsage, result.StdErr);
        }

        [Fact]
        public void Main_CompareIndexMode_PositionalIndexPath_IsRejected()
        {
            var result = InvokeMain("compare-index", "run-index.json");

            Assert.Equal(1, result.ExitCode);
            Assert.Equal(string.Empty, result.StdOut);
            Assert.Contains("Unrecognized compare-index argument 'run-index.json'.", result.StdErr);
            Assert.Contains(CompareIndexUsage, result.StdErr);
        }

        [Fact]
        public void Main_CompareIndexMode_MissingRunIndexFile_FailsWithoutStdOut()
        {
            string tempDirectory = CreateTempDirectory();
            string missingIndexPath = Path.Combine(tempDirectory, "missing-run-index.json");

            try
            {
                var result = InvokeMain("compare-index", "--index", missingIndexPath);

                Assert.Equal(1, result.ExitCode);
                Assert.Equal(string.Empty, result.StdOut);
                Assert.Contains("Failed to compare run index:", result.StdErr);
                Assert.Contains("Run index file not found:", result.StdErr, StringComparison.Ordinal);
                Assert.Contains(missingIndexPath, result.StdErr, StringComparison.Ordinal);
            }
            finally
            {
                DeleteDirectoryIfExists(tempDirectory);
            }
        }

        [Fact]
        public void Main_CompareIndexMode_MalformedRunIndexJson_FailsWithoutStdOut()
        {
            string tempDirectory = CreateTempDirectory();
            string indexPath = Path.Combine(tempDirectory, "malformed-run-index.json");

            try
            {
                Directory.CreateDirectory(tempDirectory);
                File.WriteAllText(indexPath, "{\n  \"schemaVersion\": 1,\n  \"snapshotPaths\":", new UTF8Encoding(false));

                var result = InvokeMain("compare-index", "--index", indexPath);

                Assert.Equal(1, result.ExitCode);
                Assert.Equal(string.Empty, result.StdOut);
                Assert.Contains("Failed to compare run index:", result.StdErr);
            }
            finally
            {
                DeleteDirectoryIfExists(tempDirectory);
            }
        }

        [Fact]
        public void Main_CompareIndexMode_StrictInvalidRunIndexJson_FailsWithoutStdOut()
        {
            string tempDirectory = CreateTempDirectory();
            string indexPath = Path.Combine(tempDirectory, "strict-invalid-run-index.json");

            try
            {
                WriteStrictInvalidRunIndexJson(indexPath);

                var result = InvokeMain("compare-index", "--index", indexPath);

                Assert.Equal(1, result.ExitCode);
                Assert.Equal(string.Empty, result.StdOut);
                Assert.Contains("Failed to compare run index:", result.StdErr);
            }
            finally
            {
                DeleteDirectoryIfExists(tempDirectory);
            }
        }

        [Fact]
        public void Main_CompareIndexMode_SingleRunIndex_FailsWithoutStdOutWhileSerializerLoadRemainsValid()
        {
            string tempDirectory = CreateTempDirectory();
            string snapshotPath = Path.Combine(tempDirectory, "snapshots", "only.json");
            string indexPath = Path.Combine(tempDirectory, "run-index.json");

            try
            {
                CreateFixtureSnapshot(snapshotPath);
                CreateRunIndexWithCommand(indexPath, snapshotPath);

                RunIndexDocument index = new JsonRunIndexSerializer().Load(indexPath);
                Assert.Single(index.SnapshotPaths);

                var result = InvokeMain("compare-index", "--index", indexPath);

                Assert.Equal(1, result.ExitCode);
                Assert.Equal(string.Empty, result.StdOut);
                Assert.Contains("Failed to compare run index:", result.StdErr);
                Assert.Contains(
                    "Run index must contain at least two snapshot references to compare adjacent runs.",
                    result.StdErr,
                    StringComparison.Ordinal);
            }
            finally
            {
                DeleteDirectoryIfExists(tempDirectory);
            }
        }

        [Fact]
        public void Main_CompareIndexMode_MissingLaterSnapshot_FailsAtomicallyWithoutPartialSummary()
        {
            string tempDirectory = CreateTempDirectory();
            string firstSnapshotPath = Path.Combine(tempDirectory, "snapshots", "valid-snapshot-1.json");
            string secondSnapshotPath = Path.Combine(tempDirectory, "snapshots", "valid-snapshot-2.json");
            string missingSnapshotPath = Path.Combine(tempDirectory, "snapshots", "missing-snapshot-3.json");
            string indexPath = Path.Combine(tempDirectory, "run-index.json");

            try
            {
                CreateFixtureSnapshot(firstSnapshotPath);
                CreateFixtureSnapshot(secondSnapshotPath);
                CreateRunIndexWithSerializer(indexPath, firstSnapshotPath, secondSnapshotPath, missingSnapshotPath);

                var result = InvokeMain("compare-index", "--index", indexPath);

                Assert.Equal(1, result.ExitCode);
                Assert.Equal(string.Empty, result.StdOut);
                Assert.Contains("Failed to compare run index:", result.StdErr);
                Assert.DoesNotContain("Indexed runs:", result.StdErr, StringComparison.Ordinal);
                Assert.DoesNotContain("Adjacent comparisons:", result.StdErr, StringComparison.Ordinal);
                Assert.DoesNotContain("Comparison 1/2", result.StdErr, StringComparison.Ordinal);
                Assert.DoesNotContain("Previous run:", result.StdErr, StringComparison.Ordinal);
            }
            finally
            {
                DeleteDirectoryIfExists(tempDirectory);
            }
        }

        [Fact]
        public void Main_CompareIndexMode_MissingFirstSnapshot_FailsAtomicallyWithoutPartialSummary()
        {
            string tempDirectory = CreateTempDirectory();
            string missingFirstSnapshotPath = Path.Combine(tempDirectory, "snapshots", "missing-first.json");
            string validSecondSnapshotPath = Path.Combine(tempDirectory, "snapshots", "valid-second.json");
            string indexPath = Path.Combine(tempDirectory, "run-index.json");

            try
            {
                CreateFixtureSnapshot(validSecondSnapshotPath);
                CreateRunIndexWithSerializer(indexPath, missingFirstSnapshotPath, validSecondSnapshotPath);

                var result = InvokeMain("compare-index", "--index", indexPath);

                Assert.Equal(1, result.ExitCode);
                Assert.Equal(string.Empty, result.StdOut);
                Assert.Contains("Failed to compare run index:", result.StdErr);
                Assert.DoesNotContain("Indexed runs:", result.StdOut, StringComparison.Ordinal);
                Assert.DoesNotContain("Adjacent comparisons:", result.StdOut, StringComparison.Ordinal);
                Assert.DoesNotContain("Comparison 1/1", result.StdOut, StringComparison.Ordinal);
                Assert.DoesNotContain("Previous run:", result.StdOut, StringComparison.Ordinal);
            }
            finally
            {
                DeleteDirectoryIfExists(tempDirectory);
            }
        }

        [Fact]
        public void Main_CompareIndexMode_InvalidFirstSnapshot_FailsAtomicallyWithoutPartialSummary()
        {
            string tempDirectory = CreateTempDirectory();
            string invalidFirstSnapshotPath = Path.Combine(tempDirectory, "snapshots", "invalid-first.json");
            string validSecondSnapshotPath = Path.Combine(tempDirectory, "snapshots", "valid-second.json");
            string indexPath = Path.Combine(tempDirectory, "run-index.json");

            try
            {
                WriteInvalidSnapshotFile(invalidFirstSnapshotPath);
                CreateFixtureSnapshot(validSecondSnapshotPath);
                CreateRunIndexWithSerializer(indexPath, invalidFirstSnapshotPath, validSecondSnapshotPath);

                var result = InvokeMain("compare-index", "--index", indexPath);

                Assert.Equal(1, result.ExitCode);
                Assert.Equal(string.Empty, result.StdOut);
                Assert.Contains("Failed to compare run index:", result.StdErr);
                Assert.DoesNotContain("Indexed runs:", result.StdOut, StringComparison.Ordinal);
                Assert.DoesNotContain("Adjacent comparisons:", result.StdOut, StringComparison.Ordinal);
                Assert.DoesNotContain("Comparison 1/1", result.StdOut, StringComparison.Ordinal);
                Assert.DoesNotContain("Previous run:", result.StdOut, StringComparison.Ordinal);
            }
            finally
            {
                DeleteDirectoryIfExists(tempDirectory);
            }
        }

        [Fact]
        public void Main_CompareIndexMode_InvalidMiddleSnapshot_FailsAtomicallyWithoutPartialSummary()
        {
            string tempDirectory = CreateTempDirectory();
            string validFirstSnapshotPath = Path.Combine(tempDirectory, "snapshots", "valid-a.json");
            string invalidMiddleSnapshotPath = Path.Combine(tempDirectory, "snapshots", "invalid-b.json");
            string validFinalSnapshotPath = Path.Combine(tempDirectory, "snapshots", "valid-c.json");
            string indexPath = Path.Combine(tempDirectory, "run-index.json");

            try
            {
                CreateFixtureSnapshot(validFirstSnapshotPath);
                WriteInvalidSnapshotFile(invalidMiddleSnapshotPath);
                CreateFixtureSnapshot(validFinalSnapshotPath);
                CreateRunIndexWithSerializer(indexPath, validFirstSnapshotPath, invalidMiddleSnapshotPath, validFinalSnapshotPath);

                var result = InvokeMain("compare-index", "--index", indexPath);

                Assert.Equal(1, result.ExitCode);
                Assert.Equal(string.Empty, result.StdOut);
                Assert.Contains("Failed to compare run index:", result.StdErr);
                Assert.DoesNotContain("Indexed runs:", result.StdOut, StringComparison.Ordinal);
                Assert.DoesNotContain("Adjacent comparisons:", result.StdOut, StringComparison.Ordinal);
                Assert.DoesNotContain("Comparison 1/2", result.StdOut, StringComparison.Ordinal);
                Assert.DoesNotContain("Previous run:", result.StdOut, StringComparison.Ordinal);
            }
            finally
            {
                DeleteDirectoryIfExists(tempDirectory);
            }
        }

        [Fact]
        public void Main_CompareIndexMode_InvalidFinalSnapshot_FailsAtomicallyWithoutPartialSummary()
        {
            string tempDirectory = CreateTempDirectory();
            string validFirstSnapshotPath = Path.Combine(tempDirectory, "snapshots", "valid-a.json");
            string validMiddleSnapshotPath = Path.Combine(tempDirectory, "snapshots", "valid-b.json");
            string invalidFinalSnapshotPath = Path.Combine(tempDirectory, "snapshots", "invalid-c.json");
            string indexPath = Path.Combine(tempDirectory, "run-index.json");

            try
            {
                CreateFixtureSnapshot(validFirstSnapshotPath);
                CreateFixtureSnapshot(validMiddleSnapshotPath);
                WriteInvalidSnapshotFile(invalidFinalSnapshotPath);
                CreateRunIndexWithSerializer(indexPath, validFirstSnapshotPath, validMiddleSnapshotPath, invalidFinalSnapshotPath);

                var result = InvokeMain("compare-index", "--index", indexPath);

                Assert.Equal(1, result.ExitCode);
                Assert.Equal(string.Empty, result.StdOut);
                Assert.Contains("Failed to compare run index:", result.StdErr);
                Assert.DoesNotContain("Indexed runs:", result.StdOut, StringComparison.Ordinal);
                Assert.DoesNotContain("Adjacent comparisons:", result.StdOut, StringComparison.Ordinal);
                Assert.DoesNotContain("Comparison 1/2", result.StdOut, StringComparison.Ordinal);
                Assert.DoesNotContain("Previous run:", result.StdOut, StringComparison.Ordinal);
            }
            finally
            {
                DeleteDirectoryIfExists(tempDirectory);
            }
        }

        [Fact]
        public void Main_CompareIndexMode_ResolvesParentRelativeSnapshotReferencesFromIndexDirectory()
        {
            string tempDirectory = CreateTempDirectory();
            string firstSnapshotPath = Path.Combine(tempDirectory, "snapshots", "A.json");
            string secondSnapshotPath = Path.Combine(tempDirectory, "snapshots", "B.json");
            string indexPath = Path.Combine(tempDirectory, "history", "run-index.json");

            try
            {
                CreateFixtureSnapshot(firstSnapshotPath);
                CreateFixtureSnapshot(secondSnapshotPath);
                SaveRunIndexWithReferences(indexPath, "../snapshots/A.json", "../snapshots/B.json");

                var result = InvokeMain("compare-index", "--index", indexPath);

                Assert.Equal(0, result.ExitCode);
                Assert.Equal(string.Empty, result.StdErr);

                string[] lines = result.StdOut.Split('\n', StringSplitOptions.None);
                Assert.Equal(14, lines.Length);
                Assert.Equal("Indexed runs: 2", lines[0]);
                Assert.Equal("Adjacent comparisons: 1", lines[1]);
                Assert.Equal("Comparison 1/1", lines[2]);
            }
            finally
            {
                DeleteDirectoryIfExists(tempDirectory);
            }
        }

        [Fact]
        public void Main_CompareIndexMode_DuplicateReferenceSequence_PreservesDeclaredPositions()
        {
            string tempDirectory = CreateTempDirectory();
            string seedSnapshotPath = Path.Combine(tempDirectory, "seed.json");
            string firstSnapshotPath = Path.Combine(tempDirectory, "snapshots", "run-a.json");
            string secondSnapshotPath = Path.Combine(tempDirectory, "snapshots", "run-b.json");
            string indexPath = Path.Combine(tempDirectory, "run-index.json");

            try
            {
                CreateFixtureSnapshot(seedSnapshotPath);
                WriteSnapshotCopyWithMetadata(seedSnapshotPath, firstSnapshotPath, "run-A", "2026-07-15T09:00:00+01:00");
                WriteSnapshotCopyWithMetadata(seedSnapshotPath, secondSnapshotPath, "run-B", "2026-07-15T10:00:00+01:00");
                CreateRunIndexWithCommand(indexPath, firstSnapshotPath, firstSnapshotPath, secondSnapshotPath);

                var result = InvokeMain("compare-index", "--index", indexPath);

                Assert.Equal(0, result.ExitCode);
                Assert.Equal(string.Empty, result.StdErr);

                string[] lines = result.StdOut.Split('\n', StringSplitOptions.None);
                Assert.Equal("Comparison 1/2", lines[2]);
                Assert.Equal("Previous run: run-A", lines[3]);
                Assert.Equal("Current run: run-A", lines[4]);
                Assert.Equal("Comparison 2/2", lines[14]);
                Assert.Equal("Previous run: run-A", lines[15]);
                Assert.Equal("Current run: run-B", lines[16]);
            }
            finally
            {
                DeleteDirectoryIfExists(tempDirectory);
            }
        }

        [Fact]
        public void Main_CompareIndexMode_DuplicateRunIds_DoNotTriggerRunDeduplication()
        {
            string tempDirectory = CreateTempDirectory();
            string seedSnapshotPath = Path.Combine(tempDirectory, "seed.json");
            string firstSnapshotPath = Path.Combine(tempDirectory, "snapshots", "first.json");
            string secondSnapshotPath = Path.Combine(tempDirectory, "snapshots", "second.json");
            string thirdSnapshotPath = Path.Combine(tempDirectory, "snapshots", "third.json");
            string indexPath = Path.Combine(tempDirectory, "run-index.json");

            try
            {
                CreateFixtureSnapshot(seedSnapshotPath);
                WriteSnapshotCopyWithMetadata(seedSnapshotPath, firstSnapshotPath, "duplicate-run", "2026-07-15T09:00:00+01:00");
                WriteSnapshotCopyWithMetadata(seedSnapshotPath, secondSnapshotPath, "duplicate-run", "2026-07-15T10:00:00+01:00");
                WriteSnapshotCopyWithMetadata(seedSnapshotPath, thirdSnapshotPath, "duplicate-run", "2026-07-15T11:00:00+01:00");
                CreateRunIndexWithCommand(indexPath, firstSnapshotPath, secondSnapshotPath, thirdSnapshotPath);

                var result = InvokeMain("compare-index", "--index", indexPath);

                Assert.Equal(0, result.ExitCode);
                Assert.Equal(string.Empty, result.StdErr);
                Assert.Equal(2, CountExactLine(result.StdOut, "Previous run: duplicate-run"));
                Assert.Equal(2, CountExactLine(result.StdOut, "Current run: duplicate-run"));

                string[] lines = result.StdOut.Split('\n', StringSplitOptions.None);
                Assert.Equal("Indexed runs: 3", lines[0]);
                Assert.Equal("Adjacent comparisons: 2", lines[1]);
                Assert.Equal("Comparison 1/2", lines[2]);
                Assert.Equal("Comparison 2/2", lines[14]);
            }
            finally
            {
                DeleteDirectoryIfExists(tempDirectory);
            }
        }

        private static void WriteStrictInvalidRunIndexJson(string outputPath)
        {
            string? parentDirectory = Path.GetDirectoryName(outputPath);
            if (!string.IsNullOrEmpty(parentDirectory))
            {
                Directory.CreateDirectory(parentDirectory);
            }

            const string json = """
            {
              "schemaVersion": 1,
              "snapshotPaths": [
                "run-a.json",
                "run-b.json"
              ],
              "unknownRoot": true
            }
            """;

            File.WriteAllText(outputPath, json, new UTF8Encoding(false));
        }

        private static void WriteInvalidSnapshotFile(string outputPath)
        {
            string? parentDirectory = Path.GetDirectoryName(outputPath);
            if (!string.IsNullOrEmpty(parentDirectory))
            {
                Directory.CreateDirectory(parentDirectory);
            }

            File.WriteAllText(outputPath, "{ \"schemaVersion\": 1, ", new UTF8Encoding(false));
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

        private static void CreateRunIndexWithCommand(string outputPath, params string[] snapshotPaths)
        {
            var args = new List<string> { "index-snapshots" };
            for (int i = 0; i < snapshotPaths.Length; i++)
            {
                args.Add("--snapshot");
                args.Add(snapshotPaths[i]);
            }

            args.Add("-o");
            args.Add(outputPath);

            var result = InvokeMain(args.ToArray());
            Assert.Equal(0, result.ExitCode);
            Assert.Equal(string.Empty, result.StdErr);
            Assert.True(File.Exists(outputPath));
        }

        private static void CreateRunIndexWithSerializer(string outputPath, params string[] snapshotPaths)
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

        private static void SaveRunIndexWithReferences(string outputPath, params string[] references)
        {
            string? parentDirectory = Path.GetDirectoryName(outputPath);
            if (!string.IsNullOrEmpty(parentDirectory))
            {
                Directory.CreateDirectory(parentDirectory);
            }

            new JsonRunIndexSerializer().Save(new RunIndexDocument(references), outputPath);
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

        private static int CountExactLine(string text, string expectedLine)
        {
            int count = 0;
            string[] lines = text.Split('\n', StringSplitOptions.None);
            for (int i = 0; i < lines.Length; i++)
            {
                if (string.Equals(lines[i], expectedLine, StringComparison.Ordinal))
                {
                    count++;
                }
            }

            return count;
        }

        private static string NormalizeLineEndings(string value) =>
            value.ReplaceLineEndings("\n").TrimEnd('\n');
    }
}
