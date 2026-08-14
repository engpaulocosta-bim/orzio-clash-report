using System;
using System.Collections.Generic;
using System.IO;

namespace OrzioClashReport.Launcher.Application.Engine
{
    /// <summary>
    /// Builds the exact argument vector for each published engine command. This is the only place in
    /// the launcher that knows what the engine's flags are called, and it produces a list of strings —
    /// never a command line. Every vector here mirrors a contract already published by the CLI; no
    /// flag, subcommand, or ordering is invented.
    /// </summary>
    public static class EngineArgumentBuilder
    {
        /// <summary>
        /// Single-run report. The XML input is positional and there is no subcommand, exactly as the
        /// engine's legacy contract defines it.
        /// </summary>
        public static IReadOnlyList<string> QuickReport(string inputXmlPath, string outputHtmlPath)
        {
            RequireInput(inputXmlPath, nameof(inputXmlPath));
            RequireAbsoluteOutput(outputHtmlPath, nameof(outputHtmlPath));

            return new[] { inputXmlPath, "-o", outputHtmlPath };
        }

        /// <summary>Create one immutable coordination-run snapshot from an export plus its declared manifest.</summary>
        public static IReadOnlyList<string> Snapshot(string xmlPath, string manifestPath, string outputPath)
        {
            RequireInput(xmlPath, nameof(xmlPath));
            RequireInput(manifestPath, nameof(manifestPath));
            RequireAbsoluteOutput(outputPath, nameof(outputPath));

            return new[] { "snapshot", "--xml", xmlPath, "--manifest", manifestPath, "-o", outputPath };
        }

        /// <summary>
        /// Compare two runs from their exports and manifests. Previous and current are explicit roles:
        /// the launcher never decides which is which from a date, a revision, or a file name.
        /// </summary>
        public static IReadOnlyList<string> Compare(
            string previousXmlPath,
            string previousManifestPath,
            string currentXmlPath,
            string currentManifestPath,
            string outputPath)
        {
            RequireInput(previousXmlPath, nameof(previousXmlPath));
            RequireInput(previousManifestPath, nameof(previousManifestPath));
            RequireInput(currentXmlPath, nameof(currentXmlPath));
            RequireInput(currentManifestPath, nameof(currentManifestPath));
            RequireAbsoluteOutput(outputPath, nameof(outputPath));

            return new[]
            {
                "compare",
                "--previous-xml", previousXmlPath,
                "--previous-manifest", previousManifestPath,
                "--current-xml", currentXmlPath,
                "--current-manifest", currentManifestPath,
                "-o", outputPath,
            };
        }

        /// <summary>Compare two persisted snapshots in the explicit previous/current roles given.</summary>
        public static IReadOnlyList<string> CompareSnapshots(
            string previousSnapshotPath, string currentSnapshotPath, string outputPath)
        {
            RequireInput(previousSnapshotPath, nameof(previousSnapshotPath));
            RequireInput(currentSnapshotPath, nameof(currentSnapshotPath));
            RequireAbsoluteOutput(outputPath, nameof(outputPath));

            return new[]
            {
                "compare-snapshots",
                "--previous-snapshot", previousSnapshotPath,
                "--current-snapshot", currentSnapshotPath,
                "-o", outputPath,
            };
        }

        /// <summary>
        /// Create an explicitly ordered run index. The caller's order is the only order: this method
        /// emits one <c>--snapshot</c> per entry in the exact sequence supplied, never sorting by date,
        /// name or revision, and never removing a repeated entry.
        /// </summary>
        public static IReadOnlyList<string> IndexSnapshots(
            IReadOnlyList<string> snapshotPaths, string outputPath)
        {
            if (snapshotPaths == null)
            {
                throw new ArgumentNullException(nameof(snapshotPaths));
            }

            if (snapshotPaths.Count == 0)
            {
                throw new ArgumentException("At least one snapshot is required.", nameof(snapshotPaths));
            }

            RequireAbsoluteOutput(outputPath, nameof(outputPath));

            var arguments = new List<string> { "index-snapshots" };

            for (int i = 0; i < snapshotPaths.Count; i++)
            {
                RequireInput(snapshotPaths[i], nameof(snapshotPaths));

                arguments.Add("--snapshot");
                arguments.Add(snapshotPaths[i]);
            }

            arguments.Add("-o");
            arguments.Add(outputPath);

            return arguments;
        }

        /// <summary>Traverse the adjacent pairs of an already-ordered run index.</summary>
        public static IReadOnlyList<string> CompareIndex(string indexPath, string outputPath)
        {
            RequireInput(indexPath, nameof(indexPath));
            RequireAbsoluteOutput(outputPath, nameof(outputPath));

            return new[] { "compare-index", "--index", indexPath, "-o", outputPath };
        }

        /// <summary>Create one operational project catalog around an existing run index.</summary>
        public static IReadOnlyList<string> CreateProject(
            string projectId, string displayName, string indexPath, string reportPath, string outputPath)
        {
            RequireInput(projectId, nameof(projectId));
            RequireInput(displayName, nameof(displayName));
            RequireInput(indexPath, nameof(indexPath));
            RequireInput(reportPath, nameof(reportPath));
            RequireAbsoluteOutput(outputPath, nameof(outputPath));

            return new[]
            {
                "create-project",
                "--project-id", projectId,
                "--name", displayName,
                "--index", indexPath,
                "--report", reportPath,
                "-o", outputPath,
            };
        }

        /// <summary>
        /// Append one snapshot to a project's run index. There is no <c>-o</c>: the engine owns the
        /// index it updates, and the report is not regenerated as a side effect.
        /// </summary>
        public static IReadOnlyList<string> AppendProjectSnapshot(string projectPath, string snapshotPath)
        {
            RequireInput(projectPath, nameof(projectPath));
            RequireInput(snapshotPath, nameof(snapshotPath));

            return new[] { "append-project-snapshot", "--project", projectPath, "--snapshot", snapshotPath };
        }

        /// <summary>
        /// Re-render a project's longitudinal report. There is no <c>-o</c>: the destination comes from
        /// the project catalog the engine already owns, and the launcher never supplies one.
        /// </summary>
        public static IReadOnlyList<string> RenderProject(string projectPath)
        {
            RequireInput(projectPath, nameof(projectPath));

            return new[] { "render-project", "--project", projectPath };
        }

        internal static void RequireInput(string value, string parameterName)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                throw new ArgumentException("An input path is required.", parameterName);
            }
        }

        /// <summary>
        /// Every <c>-o</c> destination is absolute. A relative destination would be resolved against
        /// the process working directory, which is not something a user can see or predict.
        /// </summary>
        internal static void RequireAbsoluteOutput(string value, string parameterName)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                throw new ArgumentException("An output path is required.", parameterName);
            }

            if (!Path.IsPathFullyQualified(value))
            {
                throw new ArgumentException("The output path must be absolute.", parameterName);
            }
        }
    }
}
