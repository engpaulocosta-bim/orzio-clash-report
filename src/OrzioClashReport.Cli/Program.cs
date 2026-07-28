using OrzioClashReport.Core.Abstractions;
using OrzioClashReport.Core.Analysis;
using OrzioClashReport.Core.Assembly;
using OrzioClashReport.Core.Continuity;
using OrzioClashReport.Core.Grouping;
using OrzioClashReport.Core.Lifecycle;
using OrzioClashReport.Core.Matching;
using OrzioClashReport.Core.Model;
using OrzioClashReport.Core.Presentation;
using OrzioClashReport.Input.NavisworksXml;
using OrzioClashReport.Input.RunManifestJson;
using OrzioClashReport.Output.Html;
using OrzioClashReport.Persistence.ProjectCatalogJson;
using OrzioClashReport.Persistence.RunIndexJson;
using OrzioClashReport.Persistence.RunSnapshotJson;
using System.Reflection;

namespace OrzioClashReport.Cli
{
    internal static class Program
    {
        private const string ProductName = "orzioclash";
        private const string LegacyUsage = "Usage: orzioclash <input.xml> -o <output.html>";
        private const string CompareUsage = "Usage: orzioclash compare --previous-xml <previous.xml> --previous-manifest <previous.json> --current-xml <current.xml> --current-manifest <current.json> [-o <output.html> | --output <output.html>]";
        private const string CompareIndexUsage = "Usage: orzioclash compare-index --index <run-index.json> [-o <output.html> | --output <output.html>]";
        private const string CompareSnapshotsUsage = "Usage: orzioclash compare-snapshots --previous-snapshot <previous.json> --current-snapshot <current.json> [-o <output.html> | --output <output.html>]";
        private const string AppendProjectSnapshotUsage = "Usage: orzioclash append-project-snapshot --project <project.json> --snapshot <run-snapshot.json>";
        private const string CreateProjectUsage = "Usage: orzioclash create-project --project-id <project-id> --name <display-name> --index <run-index.json> --report <longitudinal.html> (-o <project.json> | --output <project.json>)";
        private const string IndexSnapshotsUsage = "Usage: orzioclash index-snapshots --snapshot <run-snapshot.json> [--snapshot <run-snapshot.json> ...] (-o <run-index.json> | --output <run-index.json>)";
        private const string RenderProjectUsage = "Usage: orzioclash render-project --project <project.json>";
        private const string SnapshotUsage = "Usage: orzioclash snapshot --xml <input.xml> --manifest <run-manifest.json> (-o <run-snapshot.json> | --output <run-snapshot.json>)";

        private static int Main(string[] args)
        {
            if (args.Length == 1 && string.Equals(args[0], "--help", StringComparison.Ordinal))
            {
                WriteHelp();
                return 0;
            }

            if (args.Length == 1 && string.Equals(args[0], "--version", StringComparison.Ordinal))
            {
                Console.WriteLine($"{ProductName} {GetDisplayVersion()}");
                return 0;
            }

            if (args.Length > 0 && string.Equals(args[0], "index-snapshots", StringComparison.OrdinalIgnoreCase))
            {
                return RunIndexSnapshots(args);
            }

            if (args.Length > 0 && string.Equals(args[0], "compare-index", StringComparison.OrdinalIgnoreCase))
            {
                return RunCompareIndex(args);
            }

            if (args.Length > 0 && string.Equals(args[0], "create-project", StringComparison.OrdinalIgnoreCase))
            {
                return RunCreateProject(args);
            }

            if (args.Length > 0 && string.Equals(args[0], "append-project-snapshot", StringComparison.OrdinalIgnoreCase))
            {
                return RunAppendProjectSnapshot(args);
            }

            if (args.Length > 0 && string.Equals(args[0], "render-project", StringComparison.OrdinalIgnoreCase))
            {
                return RunRenderProject(args);
            }

            if (args.Length > 0 && string.Equals(args[0], "compare-snapshots", StringComparison.OrdinalIgnoreCase))
            {
                return RunCompareSnapshots(args);
            }

            if (args.Length > 0 && string.Equals(args[0], "compare", StringComparison.OrdinalIgnoreCase))
            {
                return RunCompare(args);
            }

            if (args.Length > 0 && string.Equals(args[0], "snapshot", StringComparison.OrdinalIgnoreCase))
            {
                return RunSnapshot(args);
            }

            return RunLegacyReport(args);
        }

        private static void WriteHelp()
        {
            Console.WriteLine("orzioclash - Navisworks Clash Detective report tooling");
            Console.WriteLine();
            Console.WriteLine("Usage:");
            Console.WriteLine("  orzioclash <input.xml> -o <output.html>");
            Console.WriteLine("  orzioclash <command> [options]");
            Console.WriteLine();
            Console.WriteLine("Default workflow:");
            Console.WriteLine("  orzioclash <input.xml> -o <output.html>");
            Console.WriteLine("      Generate the grouped single-run HTML report from one Clash Detective XML export.");
            Console.WriteLine();
            Console.WriteLine("Commands:");
            Console.WriteLine("  compare              Compare previous/current XML exports with explicit manifests.");
            Console.WriteLine("  snapshot             Create one immutable coordination-run snapshot.");
            Console.WriteLine("  compare-snapshots    Compare two persisted snapshots.");
            Console.WriteLine("  index-snapshots      Create an explicitly ordered run index from snapshots.");
            Console.WriteLine("  compare-index        Compare adjacent snapshot pairs from an explicit run index.");
            Console.WriteLine("  create-project       Create one operational project catalog from an existing run index.");
            Console.WriteLine("  append-project-snapshot Append one persisted snapshot to an existing project catalog run index.");
            Console.WriteLine("  render-project       Re-render a project catalog's longitudinal report from immutable snapshots.");
            Console.WriteLine();
            Console.WriteLine("Run-index order is authoritative; runs are never reordered by timestamp, revision, or file name.");
            Console.WriteLine("Longitudinal behavior is experimental until validated on sequential real exports.");
            Console.WriteLine();
            Console.WriteLine("Global options:");
            Console.WriteLine("  --help               Show this help.");
            Console.WriteLine("  --version            Show the application version.");
        }

        private static string GetDisplayVersion()
        {
            string version = typeof(Program).Assembly
                .GetCustomAttribute<AssemblyInformationalVersionAttribute>()?
                .InformationalVersion
                ?? typeof(Program).Assembly.GetName().Version?.ToString()
                ?? "unknown";

            int metadataIndex = version.IndexOf('+');
            return metadataIndex >= 0 ? version.Substring(0, metadataIndex) : version;
        }

        private static int RunLegacyReport(string[] args)
        {
            if (!TryParseLegacyArguments(args, out string inputPath, out string outputPath, out string parseError))
            {
                Console.Error.WriteLine(parseError);
                Console.Error.WriteLine(LegacyUsage);
                return 1;
            }

            var log = new ConsoleAppLog();

            try
            {
                IClashSource source = new NavisworksXmlClashSource(inputPath, log);
                var document = source.Read();

                IClashGrouper grouper = new RuleBasedGrouper(new PathHierarchyDisciplineResolver());
                var report = grouper.Group(document);

                IReportRenderer renderer = new HtmlReportRenderer();
                string html = renderer.Render(report);

                File.WriteAllText(outputPath, html);

                Console.WriteLine($"{report.RawCount} raw clashes -> {report.GroupCount} groups");
                Console.WriteLine($"Report written to {outputPath}");
                return 0;
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Failed to generate report: {ex.Message}");
                return 1;
            }
        }

        private static int RunCompare(string[] args)
        {
            if (!TryParseCompareArguments(args, out CompareCommandOptions options, out string parseError))
            {
                Console.Error.WriteLine(parseError);
                Console.Error.WriteLine(CompareUsage);
                return 1;
            }

            string? missingPathError = ValidateComparePaths(options);
            if (missingPathError != null)
            {
                Console.Error.WriteLine(missingPathError);
                return 1;
            }

            var log = new ConsoleAppLog();

            try
            {
                IClashSource previousSource = new NavisworksXmlClashSource(options.PreviousXmlPath, log);
                var previousDocument = previousSource.Read();

                IClashSource currentSource = new NavisworksXmlClashSource(options.CurrentXmlPath, log);
                var currentDocument = currentSource.Read();

                var manifestSource = new JsonRunManifestSource();
                var previousManifest = manifestSource.Load(options.PreviousManifestPath);
                var currentManifest = manifestSource.Load(options.CurrentManifestPath);

                ICoordinationRunAssembler assembler = new ExactSourceModelCoordinationRunAssembler();
                var previousRun = assembler.Assemble(previousDocument, previousManifest);
                var currentRun = assembler.Assemble(currentDocument, currentManifest);

                return RunDerivedComparison(previousRun, currentRun, options.OutputPath);
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Failed to compare runs: {ex.Message}");
                return 1;
            }
        }

        private static int RunCompareSnapshots(string[] args)
        {
            if (!TryParseCompareSnapshotsArguments(args, out CompareSnapshotsCommandOptions options, out string parseError))
            {
                Console.Error.WriteLine(parseError);
                Console.Error.WriteLine(CompareSnapshotsUsage);
                return 1;
            }

            string? missingPathError = ValidateCompareSnapshotPaths(options);
            if (missingPathError != null)
            {
                Console.Error.WriteLine(missingPathError);
                return 1;
            }

            try
            {
                var serializer = new JsonCoordinationRunSnapshotSerializer();
                CoordinationRun previousRun = serializer.Load(options.PreviousSnapshotPath);
                CoordinationRun currentRun = serializer.Load(options.CurrentSnapshotPath);

                return RunDerivedComparison(previousRun, currentRun, options.OutputPath);
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Failed to compare snapshots: {ex.Message}");
                return 1;
            }
        }

        private static int RunIndexSnapshots(string[] args)
        {
            if (!TryParseIndexSnapshotsArguments(args, out IndexSnapshotsCommandOptions options, out string parseError))
            {
                Console.Error.WriteLine(parseError);
                Console.Error.WriteLine(IndexSnapshotsUsage);
                return 1;
            }

            try
            {
                var snapshotSerializer = new JsonCoordinationRunSnapshotSerializer();
                var pathResolver = new RunIndexSnapshotPathResolver();
                var references = new List<string>(options.SnapshotPaths.Count);

                for (int i = 0; i < options.SnapshotPaths.Count; i++)
                {
                    string snapshotPath = options.SnapshotPaths[i];
                    snapshotSerializer.Load(snapshotPath);
                    references.Add(pathResolver.CreateReference(options.OutputPath, snapshotPath));
                }

                var index = new RunIndexDocument(references);
                var indexSerializer = new JsonRunIndexSerializer();
                indexSerializer.Save(index, options.OutputPath);

                Console.WriteLine($"Indexed snapshots: {references.Count}");
                Console.WriteLine($"Run index written to {options.OutputPath}");
                return 0;
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Failed to create run index: {ex.Message}");
                return 1;
            }
        }

        private static int RunCompareIndex(string[] args)
        {
            if (!TryParseCompareIndexArguments(args, out CompareIndexCommandOptions options, out string parseError))
            {
                Console.Error.WriteLine(parseError);
                Console.Error.WriteLine(CompareIndexUsage);
                return 1;
            }

            try
            {
                ClashRunSequencePresentationResult presentationResult = LoadPresentationResultFromRunIndex(options.IndexPath);
                WriteLongitudinalOutput(presentationResult, options.OutputPath);
                return 0;
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Failed to compare run index: {ex.Message}");
                return 1;
            }
        }

        private static int RunCreateProject(string[] args)
        {
            if (!TryParseCreateProjectArguments(args, out CreateProjectCommandOptions options, out string parseError))
            {
                Console.Error.WriteLine(parseError);
                Console.Error.WriteLine(CreateProjectUsage);
                return 1;
            }

            try
            {
                if (!File.Exists(options.IndexPath))
                {
                    throw new InvalidOperationException($"Run index file not found: {options.IndexPath}");
                }

                var runIndexPathResolver = new RunIndexSnapshotPathResolver();
                var projectCatalogPathResolver = new ProjectCatalogPathResolver();
                string resolvedProjectCatalogPath = Path.GetFullPath(options.OutputPath);
                string resolvedRunIndexPath = Path.GetFullPath(options.IndexPath);
                string resolvedReportPath = Path.GetFullPath(options.ReportPath);

                EnsureParentDirectoryExists(resolvedReportPath, "Project report parent directory not found");

                LoadedRunIndexContext loadedRunIndex = LoadRunIndexContext(resolvedRunIndexPath);
                ValidateProjectCatalogWorkspace(
                    resolvedProjectCatalogPath,
                    loadedRunIndex.ResolvedRunIndexPath,
                    loadedRunIndex.ResolvedSnapshotPaths,
                    resolvedReportPath,
                    projectCatalogPathResolver);

                string runIndexReference = projectCatalogPathResolver.CreateReference(resolvedProjectCatalogPath, loadedRunIndex.ResolvedRunIndexPath);
                string reportReference = projectCatalogPathResolver.CreateReference(resolvedProjectCatalogPath, resolvedReportPath);

                var document = new ProjectCatalogDocument(
                    options.ProjectId,
                    options.DisplayName,
                    runIndexReference,
                    reportReference);

                var serializer = new JsonProjectCatalogSerializer();
                serializer.Save(document, resolvedProjectCatalogPath);

                Console.WriteLine($"Project: {document.ProjectId}");
                Console.WriteLine($"Indexed snapshots: {loadedRunIndex.Runs.Count}");
                Console.WriteLine($"Project catalog written to {resolvedProjectCatalogPath}");
                return 0;
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Failed to create project catalog: {ex.Message}");
                return 1;
            }
        }

        private static int RunRenderProject(string[] args)
        {
            if (!TryParseRenderProjectArguments(args, out RenderProjectCommandOptions options, out string parseError))
            {
                Console.Error.WriteLine(parseError);
                Console.Error.WriteLine(RenderProjectUsage);
                return 1;
            }

            try
            {
                var serializer = new JsonProjectCatalogSerializer();
                ProjectCatalogDocument project = serializer.Load(options.ProjectPath);

                var projectCatalogPathResolver = new ProjectCatalogPathResolver();
                string resolvedProjectCatalogPath = Path.GetFullPath(options.ProjectPath);
                string resolvedRunIndexPath = projectCatalogPathResolver.ResolveReference(resolvedProjectCatalogPath, project.RunIndexPath);
                string resolvedReportPath = projectCatalogPathResolver.ResolveReference(resolvedProjectCatalogPath, project.LongitudinalReportPath);

                EnsureParentDirectoryExists(resolvedReportPath, "Project report parent directory not found");

                LoadedRunIndexContext loadedRunIndex = LoadRunIndexContext(resolvedRunIndexPath);
                ValidateProjectCatalogWorkspace(
                    resolvedProjectCatalogPath,
                    loadedRunIndex.ResolvedRunIndexPath,
                    loadedRunIndex.ResolvedSnapshotPaths,
                    resolvedReportPath,
                    projectCatalogPathResolver);

                ClashRunSequencePresentationResult presentationResult = CreateLongitudinalPresentationResult(loadedRunIndex.Runs);
                WriteLongitudinalOutput(presentationResult, resolvedReportPath);
                return 0;
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Failed to render project: {ex.Message}");
                return 1;
            }
        }

        private static int RunAppendProjectSnapshot(string[] args)
        {
            if (!TryParseAppendProjectSnapshotArguments(args, out AppendProjectSnapshotCommandOptions options, out string parseError))
            {
                Console.Error.WriteLine(parseError);
                Console.Error.WriteLine(AppendProjectSnapshotUsage);
                return 1;
            }

            try
            {
                EnsurePathIsNotExistingDirectory(options.ProjectPath, "Project catalog");

                var projectCatalogSerializer = new JsonProjectCatalogSerializer();
                string resolvedProjectCatalogPath = Path.GetFullPath(options.ProjectPath);
                ProjectCatalogDocument project = projectCatalogSerializer.Load(resolvedProjectCatalogPath);

                var projectCatalogPathResolver = new ProjectCatalogPathResolver();
                string resolvedRunIndexPath = projectCatalogPathResolver.ResolveReference(resolvedProjectCatalogPath, project.RunIndexPath);
                string resolvedReportPath = projectCatalogPathResolver.ResolveReference(resolvedProjectCatalogPath, project.LongitudinalReportPath);

                EnsurePathIsNotExistingDirectory(resolvedRunIndexPath, "Run index");
                LoadedRunIndexContext loadedRunIndex = LoadRunIndexContext(resolvedRunIndexPath, requireComparableSequence: false);

                string resolvedNewSnapshotPath = Path.GetFullPath(options.SnapshotPath);
                ValidateProjectCatalogWorkspace(
                    resolvedProjectCatalogPath,
                    loadedRunIndex.ResolvedRunIndexPath,
                    loadedRunIndex.ResolvedSnapshotPaths,
                    resolvedReportPath,
                    projectCatalogPathResolver,
                    resolvedNewSnapshotPath);

                var snapshotSerializer = new JsonCoordinationRunSnapshotSerializer();
                snapshotSerializer.Load(resolvedNewSnapshotPath);

                string newSnapshotReference = new RunIndexSnapshotPathResolver()
                    .CreateReference(loadedRunIndex.ResolvedRunIndexPath, resolvedNewSnapshotPath);

                var updatedReferences = new List<string>(loadedRunIndex.SnapshotPathReferences.Count + 1);
                for (int i = 0; i < loadedRunIndex.SnapshotPathReferences.Count; i++)
                {
                    updatedReferences.Add(loadedRunIndex.SnapshotPathReferences[i]);
                }

                updatedReferences.Add(newSnapshotReference);

                var updatedIndex = new RunIndexDocument(updatedReferences);
                new RunIndexFileReplacer().ReplaceExisting(updatedIndex, loadedRunIndex.ResolvedRunIndexPath);

                Console.WriteLine($"Project: {project.ProjectId}");
                Console.WriteLine($"Appended snapshot: {newSnapshotReference}");
                Console.WriteLine($"Indexed snapshots: {updatedReferences.Count}");
                Console.WriteLine($"Run index updated: {loadedRunIndex.ResolvedRunIndexPath}");
                return 0;
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Failed to append project snapshot: {ex.Message}");
                return 1;
            }
        }

        private static int RunSnapshot(string[] args)
        {
            if (!TryParseSnapshotArguments(args, out SnapshotCommandOptions options, out string parseError))
            {
                Console.Error.WriteLine(parseError);
                Console.Error.WriteLine(SnapshotUsage);
                return 1;
            }

            string? missingPathError = ValidateSnapshotInputPaths(options);
            if (missingPathError != null)
            {
                Console.Error.WriteLine(missingPathError);
                return 1;
            }

            var log = new ConsoleAppLog();

            try
            {
                IClashSource source = new NavisworksXmlClashSource(options.XmlPath, log);
                var document = source.Read();

                var manifestSource = new JsonRunManifestSource();
                var manifest = manifestSource.Load(options.ManifestPath);

                ICoordinationRunAssembler assembler = new ExactSourceModelCoordinationRunAssembler();
                var run = assembler.Assemble(document, manifest);

                var serializer = new JsonCoordinationRunSnapshotSerializer();
                serializer.Save(run, options.OutputPath);

                Console.WriteLine($"Run snapshot: {run.RunId}");
                Console.WriteLine($"Models: {run.Models.Count}");
                Console.WriteLine($"Executed clash tests: {run.ExecutedClashTests.Count}");
                Console.WriteLine($"Occurrences: {run.Occurrences.Count}");
                Console.WriteLine($"Snapshot written to {options.OutputPath}");
                return 0;
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Failed to create run snapshot: {ex.Message}");
                return 1;
            }
        }

        private static void WriteComparisonSummary(ClashLifecycleResult lifecycleResult)
        {
            int stillOpenCount = lifecycleResult.Entries.Count(entry => entry.Status == ClashLifecycleStatus.StillOpen);
            int newCount = lifecycleResult.Entries.Count(entry => entry.Status == ClashLifecycleStatus.New);
            int resolvedCount = lifecycleResult.Entries.Count(entry => entry.Status == ClashLifecycleStatus.Resolved);
            int unverifiableCount = lifecycleResult.Entries.Count(entry => entry.Status == ClashLifecycleStatus.Unverifiable);

            Console.WriteLine($"Previous run: {lifecycleResult.MatchResult.PreviousRun.RunId}");
            Console.WriteLine($"Current run: {lifecycleResult.MatchResult.CurrentRun.RunId}");
            Console.WriteLine($"Previous occurrences: {lifecycleResult.MatchResult.PreviousRun.Occurrences.Count}");
            Console.WriteLine($"Current occurrences: {lifecycleResult.MatchResult.CurrentRun.Occurrences.Count}");
            Console.WriteLine($"Candidates: {lifecycleResult.MatchResult.Candidates.Count}");
            Console.WriteLine($"Selected matches: {lifecycleResult.MatchResult.SelectedMatches.Count}");
            Console.WriteLine($"Alternative candidates: {lifecycleResult.MatchResult.AlternativeCandidates.Count}");
            Console.WriteLine($"StillOpen: {stillOpenCount}");
            Console.WriteLine($"New: {newCount}");
            Console.WriteLine($"Resolved: {resolvedCount}");
            Console.WriteLine($"Unverifiable: {unverifiableCount}");
        }

        private static void WriteLongitudinalSummary(ClashRunSequencePresentationResult presentationResult)
        {
            Console.WriteLine($"Indexed runs: {presentationResult.RunCount}");
            Console.WriteLine($"Adjacent comparisons: {presentationResult.AdjacentComparisonCount}");
            Console.WriteLine($"Selected matches: {presentationResult.SelectedMatchCount}");
            Console.WriteLine($"Continuity links: {presentationResult.ContinuityLinkCount}");
            Console.WriteLine($"Continuity paths: {presentationResult.ContinuityPathCount}");
            Console.WriteLine($"Standalone selected matches: {presentationResult.StandaloneSelectedMatchCount}");
            Console.WriteLine($"Lifecycle entries: {presentationResult.LifecycleEntryCount}");
            Console.WriteLine($"Non-path lifecycle entries: {presentationResult.NonPathLifecycleEntryCount}");
            Console.WriteLine($"StillOpen: {presentationResult.StillOpenCount}");
            Console.WriteLine($"New: {presentationResult.NewCount}");
            Console.WriteLine($"Resolved: {presentationResult.ResolvedCount}");
            Console.WriteLine($"Unverifiable: {presentationResult.UnverifiableCount}");
        }

        private static ClashRunSequencePresentationResult LoadPresentationResultFromRunIndex(string indexPath)
        {
            LoadedRunIndexContext loadedRunIndex = LoadRunIndexContext(indexPath);
            return CreateLongitudinalPresentationResult(loadedRunIndex.Runs);
        }

        private static LoadedRunIndexContext LoadRunIndexContext(string indexPath) =>
            LoadRunIndexContext(indexPath, requireComparableSequence: true);

        private static LoadedRunIndexContext LoadRunIndexContext(string indexPath, bool requireComparableSequence)
        {
            string resolvedRunIndexPath = Path.GetFullPath(indexPath);
            var indexSerializer = new JsonRunIndexSerializer();
            RunIndexDocument index = indexSerializer.Load(resolvedRunIndexPath);

            if (requireComparableSequence && index.SnapshotPaths.Count < 2)
            {
                throw new InvalidOperationException(
                    "Run index must contain at least two snapshot references to compare adjacent runs.");
            }

            var pathResolver = new RunIndexSnapshotPathResolver();
            var snapshotSerializer = new JsonCoordinationRunSnapshotSerializer();
            var runs = new List<CoordinationRun>(index.SnapshotPaths.Count);
            var resolvedSnapshotPaths = new List<string>(index.SnapshotPaths.Count);
            var snapshotPathReferences = new List<string>(index.SnapshotPaths.Count);

            for (int i = 0; i < index.SnapshotPaths.Count; i++)
            {
                string reference = index.SnapshotPaths[i];
                string resolvedPath = pathResolver.ResolveReference(resolvedRunIndexPath, reference);
                resolvedSnapshotPaths.Add(resolvedPath);
                snapshotPathReferences.Add(reference);
                runs.Add(snapshotSerializer.Load(resolvedPath));
            }

            return new LoadedRunIndexContext(resolvedRunIndexPath, runs, resolvedSnapshotPaths, snapshotPathReferences);
        }

        private static ClashRunSequencePresentationResult CreateLongitudinalPresentationResult(IReadOnlyList<CoordinationRun> runs)
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
            IClashRunSequenceAnalyzer sequenceAnalyzer =
                new DeterministicClashRunSequenceAnalyzer(sequenceComparer, continuityProjector, continuityPathAssembler);

            ClashRunSequenceAnalysisResult analysisResult = sequenceAnalyzer.Analyze(runs);
            IClashRunSequencePresentationProjector presentationProjector =
                new DeterministicClashRunSequencePresentationProjector();
            return presentationProjector.Project(analysisResult);
        }

        private static void WriteLongitudinalOutput(ClashRunSequencePresentationResult presentationResult, string? outputPath)
        {
            if (outputPath != null)
            {
                string html = new HtmlLongitudinalClashReportRenderer().Render(presentationResult);
                File.WriteAllText(outputPath, html);
            }

            WriteLongitudinalSummary(presentationResult);

            foreach (var transition in presentationResult.Transitions)
            {
                Console.WriteLine($"Comparison {transition.ComparisonIndex + 1}/{presentationResult.AdjacentComparisonCount}");
                WriteComparisonSummary(transition.Comparison);
            }

            if (outputPath != null)
            {
                Console.WriteLine($"Longitudinal report written to {outputPath}");
            }
        }

        private static void EnsureParentDirectoryExists(string filePath, string errorPrefix)
        {
            string fullPath = Path.GetFullPath(filePath);
            string? parentDirectory = Path.GetDirectoryName(fullPath);

            if (string.IsNullOrEmpty(parentDirectory) || !Directory.Exists(parentDirectory))
            {
                throw new InvalidOperationException($"{errorPrefix}: {parentDirectory ?? fullPath}");
            }
        }

        private static void EnsurePathIsNotExistingDirectory(string path, string label)
        {
            string fullPath = Path.GetFullPath(path);
            if (Directory.Exists(fullPath))
            {
                throw new InvalidOperationException($"{label} path cannot be an existing directory: {fullPath}");
            }
        }

        private static void ValidateProjectCatalogWorkspace(
            string projectCatalogFilePath,
            string resolvedRunIndexPath,
            IReadOnlyList<string> resolvedSnapshotPaths,
            string reportDestinationPath,
            ProjectCatalogPathResolver projectCatalogPathResolver,
            string? appendedSnapshotPath = null)
        {
            string resolvedProjectCatalogPath = Path.GetFullPath(projectCatalogFilePath);
            string projectCatalogDirectory = Path.GetDirectoryName(resolvedProjectCatalogPath)
                ?? throw new InvalidOperationException(
                    $"Project catalog file path '{projectCatalogFilePath}' does not have a parent directory.");
            string resolvedReportPath = Path.GetFullPath(reportDestinationPath);

            if (Directory.Exists(resolvedReportPath))
            {
                throw new InvalidOperationException(
                    $"Project report destination cannot be an existing directory: {resolvedReportPath}");
            }

            if (!IsPathWithinDirectory(projectCatalogDirectory, resolvedRunIndexPath))
            {
                throw new InvalidOperationException(
                    $"Project catalog workflow requires the run index to stay inside the project catalog directory tree: {resolvedRunIndexPath}");
            }

            if (!IsPathWithinDirectory(projectCatalogDirectory, resolvedReportPath))
            {
                throw new InvalidOperationException(
                    $"Project catalog workflow requires the report destination to stay inside the project catalog directory tree: {resolvedReportPath}");
            }

            for (int i = 0; i < resolvedSnapshotPaths.Count; i++)
            {
                string resolvedSnapshotPath = Path.GetFullPath(resolvedSnapshotPaths[i]);
                if (!IsPathWithinDirectory(projectCatalogDirectory, resolvedSnapshotPath))
                {
                    throw new InvalidOperationException(
                        $"Project catalog workflow requires all resolved snapshots to stay inside the project catalog directory tree: {resolvedSnapshotPath}");
                }

                projectCatalogPathResolver.CreateReference(resolvedProjectCatalogPath, resolvedSnapshotPath);

                if (PathsEqual(resolvedReportPath, resolvedSnapshotPath))
                {
                    throw new InvalidOperationException(
                        $"Project report destination must not be the same file as snapshot {i + 1}: {resolvedReportPath}");
                }
            }

            if (PathsEqual(resolvedReportPath, resolvedProjectCatalogPath))
            {
                throw new InvalidOperationException(
                    $"Project report destination must not be the same file as the project catalog: {resolvedReportPath}");
            }

            if (PathsEqual(resolvedReportPath, resolvedRunIndexPath))
            {
                throw new InvalidOperationException(
                    $"Project report destination must not be the same file as the run index: {resolvedReportPath}");
            }

            if (appendedSnapshotPath == null)
            {
                return;
            }

            string resolvedAppendedSnapshotPath = Path.GetFullPath(appendedSnapshotPath);
            if (Directory.Exists(resolvedAppendedSnapshotPath))
            {
                throw new InvalidOperationException(
                    $"Appended snapshot path cannot be an existing directory: {resolvedAppendedSnapshotPath}");
            }

            if (!IsPathWithinDirectory(projectCatalogDirectory, resolvedAppendedSnapshotPath))
            {
                throw new InvalidOperationException(
                    $"Project catalog workflow requires the appended snapshot to stay inside the project catalog directory tree: {resolvedAppendedSnapshotPath}");
            }

            if (PathsEqual(resolvedAppendedSnapshotPath, resolvedProjectCatalogPath))
            {
                throw new InvalidOperationException(
                    $"Appended snapshot must not be the same file as the project catalog: {resolvedAppendedSnapshotPath}");
            }

            if (PathsEqual(resolvedAppendedSnapshotPath, resolvedRunIndexPath))
            {
                throw new InvalidOperationException(
                    $"Appended snapshot must not be the same file as the run index: {resolvedAppendedSnapshotPath}");
            }

            if (PathsEqual(resolvedAppendedSnapshotPath, resolvedReportPath))
            {
                throw new InvalidOperationException(
                    $"Appended snapshot must not be the same file as the report destination: {resolvedAppendedSnapshotPath}");
            }
        }

        private static bool PathsEqual(string leftPath, string rightPath)
        {
            string resolvedLeft = Path.GetFullPath(leftPath);
            string resolvedRight = Path.GetFullPath(rightPath);
            StringComparison comparison = OperatingSystem.IsWindows() ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;
            return string.Equals(resolvedLeft, resolvedRight, comparison);
        }

        private static bool IsPathWithinDirectory(string directoryPath, string candidatePath)
        {
            string resolvedDirectoryPath = Path.GetFullPath(directoryPath)
                .TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)
                + Path.DirectorySeparatorChar;
            string resolvedCandidatePath = Path.GetFullPath(candidatePath);
            StringComparison comparison = OperatingSystem.IsWindows() ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;
            return resolvedCandidatePath.StartsWith(resolvedDirectoryPath, comparison);
        }

        private static ClashLifecycleResult CreateDerivedComparison(CoordinationRun previousRun, CoordinationRun currentRun)
        {
            IClashMatcher matcher = new ConservativeClashMatcher();
            IClashRunComparer comparer = new DeterministicClashRunComparer(matcher);
            var matchResult = comparer.Compare(previousRun, currentRun);

            IClashLifecycleClassifier lifecycleClassifier = new ConservativeClashLifecycleClassifier();
            return lifecycleClassifier.Classify(matchResult);
        }

        private static int RunDerivedComparison(CoordinationRun previousRun, CoordinationRun currentRun, string? outputPath)
        {
            ClashLifecycleResult lifecycleResult = CreateDerivedComparison(previousRun, currentRun);

            if (outputPath != null)
            {
                string html = new HtmlLifecycleReportRenderer().Render(lifecycleResult);
                File.WriteAllText(outputPath, html);
            }

            WriteComparisonSummary(lifecycleResult);

            if (outputPath != null)
            {
                Console.WriteLine($"Comparison report written to {outputPath}");
            }

            return 0;
        }

        private static bool TryParseLegacyArguments(
            string[] args, out string inputPath, out string outputPath, out string error)
        {
            inputPath = string.Empty;
            outputPath = "report.html";
            error = string.Empty;

            if (args.Length == 0)
            {
                error = "Missing input file.";
                return false;
            }

            inputPath = args[0];

            for (int i = 1; i < args.Length; i++)
            {
                if (args[i] == "-o" || args[i] == "--output")
                {
                    if (i + 1 >= args.Length)
                    {
                        error = $"Missing value for '{args[i]}'.";
                        return false;
                    }

                    outputPath = args[i + 1];
                    i++;
                }
                else
                {
                    error = $"Unrecognized argument '{args[i]}'.";
                    return false;
                }
            }

            if (!File.Exists(inputPath))
            {
                error = $"Input file not found: {inputPath}";
                return false;
            }

            return true;
        }

        private static bool TryParseCompareArguments(
            string[] args, out CompareCommandOptions options, out string error)
        {
            options = CompareCommandOptions.Empty;
            error = string.Empty;

            string? previousXmlPath = null;
            string? previousManifestPath = null;
            string? currentXmlPath = null;
            string? currentManifestPath = null;
            string? outputPath = null;

            for (int i = 1; i < args.Length; i++)
            {
                string argument = args[i];
                if (!IsRecognizedCompareOption(argument))
                {
                    error = $"Unrecognized compare argument '{argument}'.";
                    return false;
                }

                if (i + 1 >= args.Length
                    || string.IsNullOrWhiteSpace(args[i + 1])
                    || IsRecognizedCompareOption(args[i + 1]))
                {
                    error = $"Missing value for '{argument}'.";
                    return false;
                }

                string value = args[i + 1];
                switch (argument)
                {
                    case "--previous-xml":
                        if (previousXmlPath != null)
                        {
                            error = "Duplicate option '--previous-xml'.";
                            return false;
                        }

                        previousXmlPath = value;
                        break;
                    case "--previous-manifest":
                        if (previousManifestPath != null)
                        {
                            error = "Duplicate option '--previous-manifest'.";
                            return false;
                        }

                        previousManifestPath = value;
                        break;
                    case "--current-xml":
                        if (currentXmlPath != null)
                        {
                            error = "Duplicate option '--current-xml'.";
                            return false;
                        }

                        currentXmlPath = value;
                        break;
                    case "--current-manifest":
                        if (currentManifestPath != null)
                        {
                            error = "Duplicate option '--current-manifest'.";
                            return false;
                        }

                        currentManifestPath = value;
                        break;
                    case "-o":
                    case "--output":
                        if (outputPath != null)
                        {
                            error = "Duplicate option '-o/--output'.";
                            return false;
                        }

                        outputPath = value;
                        break;
                    default:
                        error = $"Unrecognized compare argument '{argument}'.";
                        return false;
                }

                i++;
            }

            if (previousXmlPath == null)
            {
                error = "Missing required option '--previous-xml'.";
                return false;
            }

            if (previousManifestPath == null)
            {
                error = "Missing required option '--previous-manifest'.";
                return false;
            }

            if (currentXmlPath == null)
            {
                error = "Missing required option '--current-xml'.";
                return false;
            }

            if (currentManifestPath == null)
            {
                error = "Missing required option '--current-manifest'.";
                return false;
            }

            options = new CompareCommandOptions(previousXmlPath, previousManifestPath, currentXmlPath, currentManifestPath, outputPath);
            return true;
        }

        private static bool TryParseSnapshotArguments(
            string[] args, out SnapshotCommandOptions options, out string error)
        {
            options = SnapshotCommandOptions.Empty;
            error = string.Empty;

            string? xmlPath = null;
            string? manifestPath = null;
            string? outputPath = null;

            for (int i = 1; i < args.Length; i++)
            {
                string argument = args[i];
                if (!IsRecognizedSnapshotOption(argument))
                {
                    error = $"Unrecognized snapshot argument '{argument}'.";
                    return false;
                }

                if (i + 1 >= args.Length
                    || string.IsNullOrWhiteSpace(args[i + 1])
                    || IsRecognizedSnapshotOption(args[i + 1]))
                {
                    error = $"Missing value for '{argument}'.";
                    return false;
                }

                string value = args[i + 1];
                switch (argument)
                {
                    case "--xml":
                        if (xmlPath != null)
                        {
                            error = "Duplicate option '--xml'.";
                            return false;
                        }

                        xmlPath = value;
                        break;
                    case "--manifest":
                        if (manifestPath != null)
                        {
                            error = "Duplicate option '--manifest'.";
                            return false;
                        }

                        manifestPath = value;
                        break;
                    case "-o":
                    case "--output":
                        if (outputPath != null)
                        {
                            error = "Duplicate option '-o/--output'.";
                            return false;
                        }

                        outputPath = value;
                        break;
                    default:
                        error = $"Unrecognized snapshot argument '{argument}'.";
                        return false;
                }

                i++;
            }

            if (xmlPath == null)
            {
                error = "Missing required option '--xml'.";
                return false;
            }

            if (manifestPath == null)
            {
                error = "Missing required option '--manifest'.";
                return false;
            }

            if (outputPath == null)
            {
                error = "Missing required option '-o/--output'.";
                return false;
            }

            options = new SnapshotCommandOptions(xmlPath, manifestPath, outputPath);
            return true;
        }

        private static bool TryParseIndexSnapshotsArguments(
            string[] args, out IndexSnapshotsCommandOptions options, out string error)
        {
            options = IndexSnapshotsCommandOptions.Empty;
            error = string.Empty;

            var snapshotPaths = new List<string>();
            string? outputPath = null;

            for (int i = 1; i < args.Length; i++)
            {
                string argument = args[i];
                if (!IsRecognizedIndexSnapshotsOption(argument))
                {
                    error = $"Unrecognized index-snapshots argument '{argument}'.";
                    return false;
                }

                if (i + 1 >= args.Length
                    || string.IsNullOrWhiteSpace(args[i + 1])
                    || IsRecognizedIndexSnapshotsOption(args[i + 1]))
                {
                    error = $"Missing value for '{argument}'.";
                    return false;
                }

                string value = args[i + 1];
                switch (argument)
                {
                    case "--snapshot":
                        snapshotPaths.Add(value);
                        break;
                    case "-o":
                    case "--output":
                        if (outputPath != null)
                        {
                            error = "Duplicate option '-o/--output'.";
                            return false;
                        }

                        outputPath = value;
                        break;
                    default:
                        error = $"Unrecognized index-snapshots argument '{argument}'.";
                        return false;
                }

                i++;
            }

            if (snapshotPaths.Count == 0)
            {
                error = "Missing required option '--snapshot'.";
                return false;
            }

            if (outputPath == null)
            {
                error = "Missing required option '-o/--output'.";
                return false;
            }

            options = new IndexSnapshotsCommandOptions(snapshotPaths, outputPath);
            return true;
        }

        private static bool TryParseCreateProjectArguments(
            string[] args, out CreateProjectCommandOptions options, out string error)
        {
            options = CreateProjectCommandOptions.Empty;
            error = string.Empty;

            string? projectId = null;
            string? displayName = null;
            string? indexPath = null;
            string? reportPath = null;
            string? outputPath = null;

            for (int i = 1; i < args.Length; i++)
            {
                string argument = args[i];
                if (!IsRecognizedCreateProjectOption(argument))
                {
                    error = $"Unrecognized create-project argument '{argument}'.";
                    return false;
                }

                if (i + 1 >= args.Length
                    || string.IsNullOrWhiteSpace(args[i + 1])
                    || IsRecognizedCreateProjectOption(args[i + 1]))
                {
                    error = $"Missing value for '{argument}'.";
                    return false;
                }

                string value = args[i + 1];
                switch (argument)
                {
                    case "--project-id":
                        if (projectId != null)
                        {
                            error = "Duplicate option '--project-id'.";
                            return false;
                        }

                        projectId = value;
                        break;
                    case "--name":
                        if (displayName != null)
                        {
                            error = "Duplicate option '--name'.";
                            return false;
                        }

                        displayName = value;
                        break;
                    case "--index":
                        if (indexPath != null)
                        {
                            error = "Duplicate option '--index'.";
                            return false;
                        }

                        indexPath = value;
                        break;
                    case "--report":
                        if (reportPath != null)
                        {
                            error = "Duplicate option '--report'.";
                            return false;
                        }

                        reportPath = value;
                        break;
                    case "-o":
                    case "--output":
                        if (outputPath != null)
                        {
                            error = "Duplicate option '-o/--output'.";
                            return false;
                        }

                        outputPath = value;
                        break;
                    default:
                        error = $"Unrecognized create-project argument '{argument}'.";
                        return false;
                }

                i++;
            }

            if (projectId == null)
            {
                error = "Missing required option '--project-id'.";
                return false;
            }

            if (displayName == null)
            {
                error = "Missing required option '--name'.";
                return false;
            }

            if (indexPath == null)
            {
                error = "Missing required option '--index'.";
                return false;
            }

            if (reportPath == null)
            {
                error = "Missing required option '--report'.";
                return false;
            }

            if (outputPath == null)
            {
                error = "Missing required option '-o/--output'.";
                return false;
            }

            options = new CreateProjectCommandOptions(projectId, displayName, indexPath, reportPath, outputPath);
            return true;
        }

        private static bool TryParseCompareIndexArguments(
            string[] args, out CompareIndexCommandOptions options, out string error)
        {
            options = CompareIndexCommandOptions.Empty;
            error = string.Empty;

            string? indexPath = null;
            string? outputPath = null;

            for (int i = 1; i < args.Length; i++)
            {
                string argument = args[i];
                if (!IsRecognizedCompareIndexOption(argument))
                {
                    error = $"Unrecognized compare-index argument '{argument}'.";
                    return false;
                }

                if (i + 1 >= args.Length
                    || string.IsNullOrWhiteSpace(args[i + 1])
                    || IsRecognizedCompareIndexOption(args[i + 1]))
                {
                    error = $"Missing value for '{argument}'.";
                    return false;
                }

                string value = args[i + 1];
                switch (argument)
                {
                    case "--index":
                        if (indexPath != null)
                        {
                            error = "Duplicate option '--index'.";
                            return false;
                        }

                        indexPath = value;
                        break;
                    case "-o":
                    case "--output":
                        if (outputPath != null)
                        {
                            error = "Duplicate option '-o/--output'.";
                            return false;
                        }

                        outputPath = value;
                        break;
                    default:
                        error = $"Unrecognized compare-index argument '{argument}'.";
                        return false;
                }

                i++;
            }

            if (indexPath == null)
            {
                error = "Missing required option '--index'.";
                return false;
            }

            options = new CompareIndexCommandOptions(indexPath, outputPath);
            return true;
        }

        private static bool TryParseCompareSnapshotsArguments(
            string[] args, out CompareSnapshotsCommandOptions options, out string error)
        {
            options = CompareSnapshotsCommandOptions.Empty;
            error = string.Empty;

            string? previousSnapshotPath = null;
            string? currentSnapshotPath = null;
            string? outputPath = null;

            for (int i = 1; i < args.Length; i++)
            {
                string argument = args[i];
                if (!IsRecognizedCompareSnapshotsOption(argument))
                {
                    error = $"Unrecognized compare-snapshots argument '{argument}'.";
                    return false;
                }

                if (i + 1 >= args.Length
                    || string.IsNullOrWhiteSpace(args[i + 1])
                    || IsRecognizedCompareSnapshotsOption(args[i + 1]))
                {
                    error = $"Missing value for '{argument}'.";
                    return false;
                }

                string value = args[i + 1];
                switch (argument)
                {
                    case "--previous-snapshot":
                        if (previousSnapshotPath != null)
                        {
                            error = "Duplicate option '--previous-snapshot'.";
                            return false;
                        }

                        previousSnapshotPath = value;
                        break;
                    case "--current-snapshot":
                        if (currentSnapshotPath != null)
                        {
                            error = "Duplicate option '--current-snapshot'.";
                            return false;
                        }

                        currentSnapshotPath = value;
                        break;
                    case "-o":
                    case "--output":
                        if (outputPath != null)
                        {
                            error = "Duplicate option '-o/--output'.";
                            return false;
                        }

                        outputPath = value;
                        break;
                    default:
                        error = $"Unrecognized compare-snapshots argument '{argument}'.";
                        return false;
                }

                i++;
            }

            if (previousSnapshotPath == null)
            {
                error = "Missing required option '--previous-snapshot'.";
                return false;
            }

            if (currentSnapshotPath == null)
            {
                error = "Missing required option '--current-snapshot'.";
                return false;
            }

            options = new CompareSnapshotsCommandOptions(previousSnapshotPath, currentSnapshotPath, outputPath);
            return true;
        }

        private static bool TryParseRenderProjectArguments(
            string[] args, out RenderProjectCommandOptions options, out string error)
        {
            options = RenderProjectCommandOptions.Empty;
            error = string.Empty;

            string? projectPath = null;

            for (int i = 1; i < args.Length; i++)
            {
                string argument = args[i];
                if (!IsRecognizedRenderProjectOption(argument))
                {
                    error = $"Unrecognized render-project argument '{argument}'.";
                    return false;
                }

                if (i + 1 >= args.Length
                    || string.IsNullOrWhiteSpace(args[i + 1])
                    || IsRecognizedRenderProjectOption(args[i + 1]))
                {
                    error = $"Missing value for '{argument}'.";
                    return false;
                }

                if (projectPath != null)
                {
                    error = "Duplicate option '--project'.";
                    return false;
                }

                projectPath = args[i + 1];
                i++;
            }

            if (projectPath == null)
            {
                error = "Missing required option '--project'.";
                return false;
            }

            options = new RenderProjectCommandOptions(projectPath);
            return true;
        }

        private static bool TryParseAppendProjectSnapshotArguments(
            string[] args, out AppendProjectSnapshotCommandOptions options, out string error)
        {
            options = AppendProjectSnapshotCommandOptions.Empty;
            error = string.Empty;

            string? projectPath = null;
            string? snapshotPath = null;

            for (int i = 1; i < args.Length; i++)
            {
                string argument = args[i];
                if (!IsRecognizedAppendProjectSnapshotOption(argument))
                {
                    error = $"Unrecognized append-project-snapshot argument '{argument}'.";
                    return false;
                }

                if (i + 1 >= args.Length
                    || string.IsNullOrWhiteSpace(args[i + 1])
                    || IsRecognizedAppendProjectSnapshotOption(args[i + 1]))
                {
                    error = $"Missing value for '{argument}'.";
                    return false;
                }

                string value = args[i + 1];
                switch (argument)
                {
                    case "--project":
                        if (projectPath != null)
                        {
                            error = "Duplicate option '--project'.";
                            return false;
                        }

                        projectPath = value;
                        break;
                    case "--snapshot":
                        if (snapshotPath != null)
                        {
                            error = "Duplicate option '--snapshot'.";
                            return false;
                        }

                        snapshotPath = value;
                        break;
                    default:
                        error = $"Unrecognized append-project-snapshot argument '{argument}'.";
                        return false;
                }

                i++;
            }

            if (projectPath == null)
            {
                error = "Missing required option '--project'.";
                return false;
            }

            if (snapshotPath == null)
            {
                error = "Missing required option '--snapshot'.";
                return false;
            }

            options = new AppendProjectSnapshotCommandOptions(projectPath, snapshotPath);
            return true;
        }

        private static bool IsRecognizedCompareOption(string argument) =>
            argument == "--previous-xml"
            || argument == "--previous-manifest"
            || argument == "--current-xml"
            || argument == "--current-manifest"
            || argument == "-o"
            || argument == "--output";

        private static bool IsRecognizedSnapshotOption(string argument) =>
            argument == "--xml"
            || argument == "--manifest"
            || argument == "-o"
            || argument == "--output";

        private static bool IsRecognizedCreateProjectOption(string argument) =>
            argument == "--project-id"
            || argument == "--name"
            || argument == "--index"
            || argument == "--report"
            || argument == "-o"
            || argument == "--output";

        private static bool IsRecognizedCompareIndexOption(string argument) =>
            argument == "--index"
            || argument == "-o"
            || argument == "--output";

        private static bool IsRecognizedCompareSnapshotsOption(string argument) =>
            argument == "--previous-snapshot"
            || argument == "--current-snapshot"
            || argument == "-o"
            || argument == "--output";

        private static bool IsRecognizedIndexSnapshotsOption(string argument) =>
            argument == "--snapshot"
            || argument == "-o"
            || argument == "--output";

        private static bool IsRecognizedRenderProjectOption(string argument) =>
            argument == "--project";

        private static bool IsRecognizedAppendProjectSnapshotOption(string argument) =>
            argument == "--project"
            || argument == "--snapshot";

        private static string? ValidateComparePaths(CompareCommandOptions options)
        {
            if (!File.Exists(options.PreviousXmlPath))
            {
                return $"Previous XML file not found: {options.PreviousXmlPath}";
            }

            if (!File.Exists(options.PreviousManifestPath))
            {
                return $"Previous manifest file not found: {options.PreviousManifestPath}";
            }

            if (!File.Exists(options.CurrentXmlPath))
            {
                return $"Current XML file not found: {options.CurrentXmlPath}";
            }

            if (!File.Exists(options.CurrentManifestPath))
            {
                return $"Current manifest file not found: {options.CurrentManifestPath}";
            }

            return null;
        }

        private static string? ValidateSnapshotInputPaths(SnapshotCommandOptions options)
        {
            if (!File.Exists(options.XmlPath))
            {
                return $"Snapshot XML file not found: {options.XmlPath}";
            }

            if (!File.Exists(options.ManifestPath))
            {
                return $"Snapshot manifest file not found: {options.ManifestPath}";
            }

            return null;
        }

        private static string? ValidateCompareSnapshotPaths(CompareSnapshotsCommandOptions options)
        {
            if (!File.Exists(options.PreviousSnapshotPath))
            {
                return $"Previous snapshot file not found: {options.PreviousSnapshotPath}";
            }

            if (!File.Exists(options.CurrentSnapshotPath))
            {
                return $"Current snapshot file not found: {options.CurrentSnapshotPath}";
            }

            return null;
        }

        private sealed class CompareCommandOptions
        {
            public static readonly CompareCommandOptions Empty = new CompareCommandOptions(string.Empty, string.Empty, string.Empty, string.Empty, null);

            public CompareCommandOptions(
                string previousXmlPath,
                string previousManifestPath,
                string currentXmlPath,
                string currentManifestPath,
                string? outputPath)
            {
                PreviousXmlPath = previousXmlPath;
                PreviousManifestPath = previousManifestPath;
                CurrentXmlPath = currentXmlPath;
                CurrentManifestPath = currentManifestPath;
                OutputPath = outputPath;
            }

            public string PreviousXmlPath { get; }
            public string PreviousManifestPath { get; }
            public string CurrentXmlPath { get; }
            public string CurrentManifestPath { get; }
            public string? OutputPath { get; }
        }

        private sealed class SnapshotCommandOptions
        {
            public static readonly SnapshotCommandOptions Empty = new SnapshotCommandOptions(string.Empty, string.Empty, string.Empty);

            public SnapshotCommandOptions(string xmlPath, string manifestPath, string outputPath)
            {
                XmlPath = xmlPath;
                ManifestPath = manifestPath;
                OutputPath = outputPath;
            }

            public string XmlPath { get; }
            public string ManifestPath { get; }
            public string OutputPath { get; }
        }

        private sealed class CompareSnapshotsCommandOptions
        {
            public static readonly CompareSnapshotsCommandOptions Empty = new CompareSnapshotsCommandOptions(string.Empty, string.Empty, null);

            public CompareSnapshotsCommandOptions(string previousSnapshotPath, string currentSnapshotPath, string? outputPath)
            {
                PreviousSnapshotPath = previousSnapshotPath;
                CurrentSnapshotPath = currentSnapshotPath;
                OutputPath = outputPath;
            }

            public string PreviousSnapshotPath { get; }
            public string CurrentSnapshotPath { get; }
            public string? OutputPath { get; }
        }

        private sealed class CreateProjectCommandOptions
        {
            public static readonly CreateProjectCommandOptions Empty =
                new CreateProjectCommandOptions(string.Empty, string.Empty, string.Empty, string.Empty, string.Empty);

            public CreateProjectCommandOptions(
                string projectId,
                string displayName,
                string indexPath,
                string reportPath,
                string outputPath)
            {
                ProjectId = projectId;
                DisplayName = displayName;
                IndexPath = indexPath;
                ReportPath = reportPath;
                OutputPath = outputPath;
            }

            public string ProjectId { get; }
            public string DisplayName { get; }
            public string IndexPath { get; }
            public string ReportPath { get; }
            public string OutputPath { get; }
        }

        private sealed class CompareIndexCommandOptions
        {
            public static readonly CompareIndexCommandOptions Empty = new CompareIndexCommandOptions(string.Empty, null);

            public CompareIndexCommandOptions(string indexPath, string? outputPath)
            {
                IndexPath = indexPath;
                OutputPath = outputPath;
            }

            public string IndexPath { get; }
            public string? OutputPath { get; }
        }

        private sealed class IndexSnapshotsCommandOptions
        {
            public static readonly IndexSnapshotsCommandOptions Empty = new IndexSnapshotsCommandOptions(Array.Empty<string>(), string.Empty);

            public IndexSnapshotsCommandOptions(IReadOnlyList<string> snapshotPaths, string outputPath)
            {
                SnapshotPaths = snapshotPaths;
                OutputPath = outputPath;
            }

            public IReadOnlyList<string> SnapshotPaths { get; }
            public string OutputPath { get; }
        }

        private sealed class LoadedRunIndexContext
        {
            public LoadedRunIndexContext(
                string resolvedRunIndexPath,
                IReadOnlyList<CoordinationRun> runs,
                IReadOnlyList<string> resolvedSnapshotPaths,
                IReadOnlyList<string> snapshotPathReferences)
            {
                ResolvedRunIndexPath = resolvedRunIndexPath;
                Runs = runs;
                ResolvedSnapshotPaths = resolvedSnapshotPaths;
                SnapshotPathReferences = snapshotPathReferences;
            }

            public string ResolvedRunIndexPath { get; }
            public IReadOnlyList<CoordinationRun> Runs { get; }
            public IReadOnlyList<string> ResolvedSnapshotPaths { get; }
            public IReadOnlyList<string> SnapshotPathReferences { get; }
        }

        private sealed class RenderProjectCommandOptions
        {
            public static readonly RenderProjectCommandOptions Empty = new RenderProjectCommandOptions(string.Empty);

            public RenderProjectCommandOptions(string projectPath)
            {
                ProjectPath = projectPath;
            }

            public string ProjectPath { get; }
        }

        private sealed class AppendProjectSnapshotCommandOptions
        {
            public static readonly AppendProjectSnapshotCommandOptions Empty =
                new AppendProjectSnapshotCommandOptions(string.Empty, string.Empty);

            public AppendProjectSnapshotCommandOptions(string projectPath, string snapshotPath)
            {
                ProjectPath = projectPath;
                SnapshotPath = snapshotPath;
            }

            public string ProjectPath { get; }
            public string SnapshotPath { get; }
        }
    }
}
