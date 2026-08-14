using System;
using System.Collections.Generic;
using OrzioClashReport.Launcher.Application.Engine;

namespace OrzioClashReport.Launcher.Tests
{
    /// <summary>
    /// One test per published command, comparing the argument vector element by element. These are
    /// the launcher's contract with the engine: if one of them has to change, the engine's published
    /// CLI changed, and that is a decision, not a refactor.
    /// </summary>
    public sealed class EngineArgumentBuilderTests
    {
        private static readonly string Output = TestPaths.Absolute("reports/output.html");

        [Fact]
        public void QuickReportPutsTheXmlFirstAndHasNoSubcommand()
        {
            Assert.Equal(
                new[] { "/inputs/run.xml", "-o", Output },
                EngineArgumentBuilder.QuickReport("/inputs/run.xml", Output));
        }

        [Fact]
        public void Snapshot()
        {
            string output = TestPaths.Absolute("snapshots/run-001.json");

            Assert.Equal(
                new[]
                {
                    "snapshot",
                    "--xml", "/inputs/run.xml",
                    "--manifest", "/inputs/run.manifest.json",
                    "-o", output,
                },
                EngineArgumentBuilder.Snapshot("/inputs/run.xml", "/inputs/run.manifest.json", output));
        }

        [Fact]
        public void Compare()
        {
            Assert.Equal(
                new[]
                {
                    "compare",
                    "--previous-xml", "/inputs/previous.xml",
                    "--previous-manifest", "/inputs/previous.json",
                    "--current-xml", "/inputs/current.xml",
                    "--current-manifest", "/inputs/current.json",
                    "-o", Output,
                },
                EngineArgumentBuilder.Compare(
                    "/inputs/previous.xml",
                    "/inputs/previous.json",
                    "/inputs/current.xml",
                    "/inputs/current.json",
                    Output));
        }

        [Fact]
        public void CompareSnapshots()
        {
            Assert.Equal(
                new[]
                {
                    "compare-snapshots",
                    "--previous-snapshot", "/snapshots/run-001.json",
                    "--current-snapshot", "/snapshots/run-002.json",
                    "-o", Output,
                },
                EngineArgumentBuilder.CompareSnapshots(
                    "/snapshots/run-001.json", "/snapshots/run-002.json", Output));
        }

        [Fact]
        public void IndexSnapshotsEmitsOneFlagPerEntryInTheDeclaredOrder()
        {
            string output = TestPaths.Absolute("project/run-index.json");

            Assert.Equal(
                new[]
                {
                    "index-snapshots",
                    "--snapshot", "/snapshots/run-003.json",
                    "--snapshot", "/snapshots/run-001.json",
                    "--snapshot", "/snapshots/run-002.json",
                    "-o", output,
                },
                EngineArgumentBuilder.IndexSnapshots(
                    new[] { "/snapshots/run-003.json", "/snapshots/run-001.json", "/snapshots/run-002.json" },
                    output));
        }

        [Fact]
        public void IndexSnapshotsNeverSortsAndNeverDeduplicates()
        {
            // Deliberately in an order no automatic rule would produce, with a repeat in the middle.
            var declared = new[]
            {
                "/snapshots/run-c.json",
                "/snapshots/run-a.json",
                "/snapshots/run-c.json",
                "/snapshots/run-b.json",
            };

            IReadOnlyList<string> arguments =
                EngineArgumentBuilder.IndexSnapshots(declared, TestPaths.Absolute("project/run-index.json"));

            var emitted = new List<string>();
            for (int i = 0; i < arguments.Count - 1; i++)
            {
                if (arguments[i] == "--snapshot")
                {
                    emitted.Add(arguments[i + 1]);
                }
            }

            Assert.Equal(declared, emitted);
        }

        [Fact]
        public void CompareIndex()
        {
            Assert.Equal(
                new[] { "compare-index", "--index", "/project/run-index.json", "-o", Output },
                EngineArgumentBuilder.CompareIndex("/project/run-index.json", Output));
        }

        [Fact]
        public void CreateProject()
        {
            string output = TestPaths.Absolute("project/project.json");

            Assert.Equal(
                new[]
                {
                    "create-project",
                    "--project-id", "tower-a",
                    "--name", "Tower A",
                    "--index", "/project/run-index.json",
                    "--report", "/project/longitudinal.html",
                    "-o", output,
                },
                EngineArgumentBuilder.CreateProject(
                    "tower-a", "Tower A", "/project/run-index.json", "/project/longitudinal.html", output));
        }

        [Fact]
        public void AppendProjectSnapshotHasNoOutputOption()
        {
            IReadOnlyList<string> arguments =
                EngineArgumentBuilder.AppendProjectSnapshot("/project/project.json", "/snapshots/run-004.json");

            Assert.Equal(
                new[]
                {
                    "append-project-snapshot",
                    "--project", "/project/project.json",
                    "--snapshot", "/snapshots/run-004.json",
                },
                arguments);

            Assert.DoesNotContain("-o", arguments);
            Assert.DoesNotContain("--output", arguments);
        }

        [Fact]
        public void RenderProjectHasNoOutputOption()
        {
            IReadOnlyList<string> arguments = EngineArgumentBuilder.RenderProject("/project/project.json");

            Assert.Equal(new[] { "render-project", "--project", "/project/project.json" }, arguments);

            Assert.DoesNotContain("-o", arguments);
            Assert.DoesNotContain("--output", arguments);
        }

        [Fact]
        public void EveryOutputMustBeAbsolute()
        {
            Assert.Throws<ArgumentException>(() => EngineArgumentBuilder.QuickReport("/inputs/run.xml", "report.html"));
            Assert.Throws<ArgumentException>(() => EngineArgumentBuilder.Snapshot("a", "b", "snapshot.json"));
            Assert.Throws<ArgumentException>(() => EngineArgumentBuilder.CompareIndex("/index.json", "report.html"));
            Assert.Throws<ArgumentException>(() => EngineArgumentBuilder.IndexSnapshots(new[] { "a" }, "index.json"));
        }

        [Fact]
        public void EveryRequiredInputMustBePresent()
        {
            Assert.Throws<ArgumentException>(() => EngineArgumentBuilder.QuickReport(string.Empty, Output));
            Assert.Throws<ArgumentException>(() => EngineArgumentBuilder.Snapshot(" ", "/manifest.json", "/out.json"));
            Assert.Throws<ArgumentException>(() => EngineArgumentBuilder.RenderProject(string.Empty));
            Assert.Throws<ArgumentException>(() => EngineArgumentBuilder.IndexSnapshots(Array.Empty<string>(), "/out.json"));
        }
    }
}
