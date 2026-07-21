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
        private const string IndexSnapshotsUsage = "Usage: orzioclash index-snapshots --snapshot <run-snapshot.json> [--snapshot <run-snapshot.json> ...] (-o <run-index.json> | --output <run-index.json>)";
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
            Console.WriteLine("Commands:");
            Console.WriteLine("  single-run report    Generate grouped HTML from one Clash Detective XML export.");
            Console.WriteLine("  compare              Compare previous/current XML exports with explicit manifests.");
            Console.WriteLine("  snapshot             Create one immutable coordination-run snapshot.");
            Console.WriteLine("  compare-snapshots    Compare two persisted snapshots.");
            Console.WriteLine("  index-snapshots      Create an explicitly ordered run index from snapshots.");
            Console.WriteLine("  compare-index        Compare adjacent snapshot pairs from an explicit run index.");
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
                var indexSerializer = new JsonRunIndexSerializer();
                RunIndexDocument index = indexSerializer.Load(options.IndexPath);

                if (index.SnapshotPaths.Count < 2)
                {
                    throw new InvalidOperationException(
                        "Run index must contain at least two snapshot references to compare adjacent runs.");
                }

                var pathResolver = new RunIndexSnapshotPathResolver();
                var snapshotSerializer = new JsonCoordinationRunSnapshotSerializer();
                var runs = new List<CoordinationRun>(index.SnapshotPaths.Count);

                for (int i = 0; i < index.SnapshotPaths.Count; i++)
                {
                    string resolvedPath = pathResolver.ResolveReference(options.IndexPath, index.SnapshotPaths[i]);
                    runs.Add(snapshotSerializer.Load(resolvedPath));
                }

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
                ClashRunSequencePresentationResult presentationResult = presentationProjector.Project(analysisResult);

                if (options.OutputPath != null)
                {
                    string html = new HtmlLongitudinalClashReportRenderer().Render(presentationResult);
                    File.WriteAllText(options.OutputPath, html);
                }

                WriteLongitudinalSummary(presentationResult);

                foreach (var transition in presentationResult.Transitions)
                {
                    Console.WriteLine($"Comparison {transition.ComparisonIndex + 1}/{presentationResult.AdjacentComparisonCount}");
                    WriteComparisonSummary(transition.Comparison);
                }

                if (options.OutputPath != null)
                {
                    Console.WriteLine($"Longitudinal report written to {options.OutputPath}");
                }

                return 0;
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Failed to compare run index: {ex.Message}");
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
    }
}
