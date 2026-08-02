using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using OrzioClashReport.Launcher.Contracts.Operations;
using OrzioClashReport.Launcher.Contracts.Ports;

namespace OrzioClashReport.Launcher.Infrastructure.Engine
{
    /// <summary>
    /// Turns a typed request into the exact argument vector the engine's published contracts define.
    /// Nothing here is inferred: every flag, its spelling, and its position come from a contract the
    /// engine already documents and tests. The result is never joined into a command line.
    /// </summary>
    public sealed class EngineArgumentBuilder : IEngineArgumentBuilder
    {
        public IReadOnlyList<string> Build(LauncherOperationRequest request)
        {
            if (request == null)
            {
                throw new ArgumentNullException(nameof(request));
            }

            switch (request)
            {
                case QuickReportRequest quickReport:
                    // The XML is positional and there is no subcommand for the single-run workflow.
                    return Freeze(new[] { quickReport.InputXmlPath, "-o", quickReport.OutputHtmlPath });

                case CreateSnapshotRequest snapshot:
                    return Freeze(new[]
                    {
                        "snapshot",
                        "--xml", snapshot.XmlPath,
                        "--manifest", snapshot.ManifestPath,
                        "-o", snapshot.OutputSnapshotPath,
                    });

                case CompareRunsRequest compare:
                    return Freeze(new[]
                    {
                        "compare",
                        "--previous-xml", compare.PreviousXmlPath,
                        "--previous-manifest", compare.PreviousManifestPath,
                        "--current-xml", compare.CurrentXmlPath,
                        "--current-manifest", compare.CurrentManifestPath,
                        "-o", compare.OutputHtmlPath,
                    });

                case CompareSnapshotsRequest compareSnapshots:
                    return Freeze(new[]
                    {
                        "compare-snapshots",
                        "--previous-snapshot", compareSnapshots.PreviousSnapshotPath,
                        "--current-snapshot", compareSnapshots.CurrentSnapshotPath,
                        "-o", compareSnapshots.OutputHtmlPath,
                    });

                case IndexSnapshotsRequest index:
                    return BuildIndexSnapshots(index);

                case CompareIndexRequest compareIndex:
                    return Freeze(new[]
                    {
                        "compare-index",
                        "--index", compareIndex.IndexPath,
                        "-o", compareIndex.OutputHtmlPath,
                    });

                case CreateProjectRequest createProject:
                    return Freeze(new[]
                    {
                        "create-project",
                        "--project-id", createProject.ProjectId,
                        "--name", createProject.DisplayName,
                        "--index", createProject.IndexPath,
                        "--report", createProject.ReportPath,
                        "-o", createProject.OutputProjectPath,
                    });

                case AppendProjectSnapshotRequest append:
                    // No -o: this command mutates only the run index the catalog already points at.
                    return Freeze(new[]
                    {
                        "append-project-snapshot",
                        "--project", append.ProjectPath,
                        "--snapshot", append.SnapshotPath,
                    });

                case RenderProjectRequest render:
                    // No -o: the destination comes from the existing catalog, never from the launcher.
                    return Freeze(new[] { "render-project", "--project", render.ProjectPath });

                case CreateIdentityGovernanceRequest createGovernance:
                    return Freeze(new[]
                    {
                        "create-identity-governance",
                        "--project-id", createGovernance.ProjectId,
                        "-o", createGovernance.OutputGovernancePath,
                    });

                case AppendIdentityDecisionRequest decision:
                    return BuildAppendIdentityDecision(decision);

                case ValidateIdentityGovernanceRequest validate:
                    return Freeze(new[]
                    {
                        "validate-identity-governance",
                        "--project", validate.ProjectPath,
                        "--governance", validate.GovernancePath,
                    });

                case RenderIdentityGovernanceReportRequest review:
                    return Freeze(new[]
                    {
                        "render-identity-governance-report",
                        "--project", review.ProjectPath,
                        "--governance", review.GovernancePath,
                        "-o", review.OutputHtmlPath,
                    });

                default:
                    throw new ArgumentOutOfRangeException(
                        nameof(request), request.Kind, "No argument vector is defined for this operation.");
            }
        }

        private static IReadOnlyList<string> BuildIndexSnapshots(IndexSnapshotsRequest request)
        {
            var arguments = new List<string>((request.SnapshotPaths.Count * 2) + 3) { "index-snapshots" };

            // The declared order is the index's order. Nothing is sorted and nothing is deduplicated.
            foreach (string snapshot in request.SnapshotPaths)
            {
                arguments.Add("--snapshot");
                arguments.Add(snapshot);
            }

            arguments.Add("-o");
            arguments.Add(request.OutputIndexPath);

            return Freeze(arguments);
        }

        private static IReadOnlyList<string> BuildAppendIdentityDecision(AppendIdentityDecisionRequest request)
        {
            var arguments = new List<string>(20)
            {
                "append-identity-decision",
                "--governance", request.GovernancePath,
                "--decision-id", request.DecisionId,
                "--decision-kind", CanonicalDecisionKind(request.DecisionKind),
                "--left-run-id", request.Left.RunId,
                "--left-occurrence-index", Index(request.Left.OccurrenceIndex),
                "--right-run-id", request.Right.RunId,
                "--right-occurrence-index", Index(request.Right.OccurrenceIndex),
                "--reviewer-alias", request.ReviewerAlias,
            };

            // A confirmation always carries the persistent identity id; a rejection never does.
            if (request.DecisionKind == HumanIdentityDecisionKind.ConfirmSameIdentity)
            {
                if (request.PersistentIdentityId == null)
                {
                    throw new InvalidOperationException(
                        "A confirmation must carry a persistent identity id.");
                }

                arguments.Add("--persistent-identity-id");
                arguments.Add(request.PersistentIdentityId);
            }
            else if (request.PersistentIdentityId != null)
            {
                throw new InvalidOperationException(
                    "A rejection must never carry a persistent identity id.");
            }

            if (request.Reason != null)
            {
                arguments.Add("--reason");
                arguments.Add(request.Reason);
            }

            return Freeze(arguments);
        }

        /// <summary>
        /// The value the engine accepts, verbatim. The visible label may be localized; this may not.
        /// </summary>
        internal static string CanonicalDecisionKind(HumanIdentityDecisionKind kind)
        {
            switch (kind)
            {
                case HumanIdentityDecisionKind.ConfirmSameIdentity:
                    return "ConfirmSameIdentity";
                case HumanIdentityDecisionKind.RejectSameIdentity:
                    return "RejectSameIdentity";
                default:
                    throw new ArgumentOutOfRangeException(nameof(kind), kind, "Unknown decision kind.");
            }
        }

        private static string Index(int value) => value.ToString(CultureInfo.InvariantCulture);

        private static IReadOnlyList<string> Freeze(IList<string> arguments) =>
            new ReadOnlyCollection<string>(arguments is List<string> list ? list : new List<string>(arguments));
    }
}
