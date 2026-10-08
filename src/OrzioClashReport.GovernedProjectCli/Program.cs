using OrzioClashReport.Core.Abstractions;
using OrzioClashReport.Core.Analysis;
using OrzioClashReport.Core.Continuity;
using OrzioClashReport.Core.Governance;
using OrzioClashReport.Core.Lifecycle;
using OrzioClashReport.Core.Matching;
using OrzioClashReport.Core.Model;
using OrzioClashReport.Core.Presentation;
using OrzioClashReport.Output.Html;
using OrzioClashReport.Persistence.IdentityGovernanceJson;
using OrzioClashReport.Persistence.ProjectCatalogJson;
using OrzioClashReport.Persistence.RunIndexJson;
using OrzioClashReport.Persistence.RunSnapshotJson;

namespace OrzioClashReport.GovernedProjectCli
{
    internal static class Program
    {
        private const string Usage = "Usage: orzioclash-governed --project <project.json> --governance <identity-governance.json>";

        private static int Main(string[] args)
        {
            if (!TryParse(args, out string projectPath, out string governancePath, out string error))
            {
                Console.Error.WriteLine(error);
                Console.Error.WriteLine(Usage);
                return 1;
            }

            try
            {
                string resolvedProjectPath = Path.GetFullPath(projectPath);
                string resolvedGovernancePath = Path.GetFullPath(governancePath);
                if (!File.Exists(resolvedProjectPath))
                {
                    throw new InvalidOperationException($"Project catalog file not found: {resolvedProjectPath}");
                }

                if (!File.Exists(resolvedGovernancePath))
                {
                    throw new InvalidOperationException($"Identity governance file not found: {resolvedGovernancePath}");
                }

                var projectSerializer = new JsonProjectCatalogSerializer();
                ProjectCatalogDocument project = projectSerializer.Load(resolvedProjectPath);
                var projectPathResolver = new ProjectCatalogPathResolver();
                string resolvedRunIndexPath = projectPathResolver.ResolveReference(resolvedProjectPath, project.RunIndexPath);
                string resolvedReportPath = projectPathResolver.ResolveReference(resolvedProjectPath, project.LongitudinalReportPath);

                IReadOnlyList<CoordinationRun> runs = LoadRuns(resolvedRunIndexPath);
                if (runs.Count < 2)
                {
                    throw new InvalidOperationException("A longitudinal governed report requires at least two indexed snapshots.");
                }

                var governanceSerializer = new JsonIdentityGovernanceSerializer();
                IdentityGovernanceDocument governance = governanceSerializer.Load(resolvedGovernancePath);
                var validator = new DeterministicIdentityGovernanceEvidenceValidator();
                IdentityGovernanceEvidenceValidationResult validation = validator.Validate(project.ProjectId, governance, runs);
                if (!validation.IsValid)
                {
                    string details = string.Join(" | ", validation.Issues.Select(issue => issue.ToString()));
                    throw new InvalidOperationException($"Identity governance evidence validation failed: {details}");
                }

                var confirmationProjector = new DeterministicLongitudinalIdentityConfirmationProjector();
                IReadOnlyList<LongitudinalIdentityConfirmation> confirmations = confirmationProjector.Project(governance);
                ClashRunSequencePresentationResult presentation = CreateLongitudinalPresentation(runs);
                string html = new GovernedLongitudinalClashReportRenderer().Render(presentation, confirmations);
                new DerivedHtmlReportWriter().Write(html, resolvedReportPath);

                Console.WriteLine($"Project: {project.ProjectId}");
                Console.WriteLine($"Indexed snapshots: {runs.Count}");
                Console.WriteLine($"Human-confirmed identity pairs: {confirmations.Count}");
                Console.WriteLine($"Longitudinal report written to {resolvedReportPath}");
                return 0;
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Failed to render governed longitudinal report: {ex.Message}");
                return 1;
            }
        }

        private static IReadOnlyList<CoordinationRun> LoadRuns(string runIndexPath)
        {
            if (!File.Exists(runIndexPath))
            {
                throw new InvalidOperationException($"Run index file not found: {runIndexPath}");
            }

            RunIndexDocument index = new JsonRunIndexSerializer().Load(runIndexPath);
            var pathResolver = new RunIndexSnapshotPathResolver();
            var snapshotSerializer = new JsonCoordinationRunSnapshotSerializer();
            var runs = new List<CoordinationRun>(index.SnapshotPaths.Count);
            foreach (string reference in index.SnapshotPaths)
            {
                string snapshotPath = pathResolver.ResolveReference(runIndexPath, reference);
                runs.Add(snapshotSerializer.Load(snapshotPath));
            }

            return runs.AsReadOnly();
        }

        private static ClashRunSequencePresentationResult CreateLongitudinalPresentation(IReadOnlyList<CoordinationRun> runs)
        {
            IClashMatcher matcher = new ConservativeClashMatcher();
            IClashRunComparer runComparer = new DeterministicClashRunComparer(matcher);
            IClashLifecycleClassifier lifecycleClassifier = new ConservativeClashLifecycleClassifier();
            IClashRunSequenceComparer sequenceComparer =
                new DeterministicAdjacentClashRunSequenceComparer(runComparer, lifecycleClassifier);
            IClashRunSequenceContinuityProjector continuityProjector =
                new DeterministicSelectedMatchContinuityProjector();
            IClashRunSequenceContinuityPathAssembler continuityPathAssembler =
                new DeterministicSelectedMatchContinuityPathAssembler();
            IClashRunSequenceAnalyzer analyzer =
                new DeterministicClashRunSequenceAnalyzer(sequenceComparer, continuityProjector, continuityPathAssembler);

            ClashRunSequenceAnalysisResult analysis = analyzer.Analyze(runs);
            IClashRunSequencePresentationProjector projector =
                new DeterministicClashRunSequencePresentationProjector();
            return projector.Project(analysis);
        }

        private static bool TryParse(
            string[] args,
            out string projectPath,
            out string governancePath,
            out string error)
        {
            projectPath = string.Empty;
            governancePath = string.Empty;
            error = string.Empty;

            for (int i = 0; i < args.Length; i++)
            {
                string arg = args[i];
                if (string.Equals(arg, "--project", StringComparison.Ordinal))
                {
                    if (++i >= args.Length || projectPath.Length > 0)
                    {
                        error = "--project must be supplied exactly once with a value.";
                        return false;
                    }

                    projectPath = args[i];
                }
                else if (string.Equals(arg, "--governance", StringComparison.Ordinal))
                {
                    if (++i >= args.Length || governancePath.Length > 0)
                    {
                        error = "--governance must be supplied exactly once with a value.";
                        return false;
                    }

                    governancePath = args[i];
                }
                else
                {
                    error = $"Unknown argument: {arg}";
                    return false;
                }
            }

            if (string.IsNullOrWhiteSpace(projectPath) || string.IsNullOrWhiteSpace(governancePath))
            {
                error = "Both --project and --governance are required.";
                return false;
            }

            return true;
        }
    }
}
