using OrzioClashReport.Core.Abstractions;
using OrzioClashReport.Core.Assembly;
using OrzioClashReport.Core.Grouping;
using OrzioClashReport.Core.Lifecycle;
using OrzioClashReport.Core.Matching;
using OrzioClashReport.Core.Model;
using OrzioClashReport.Input.NavisworksXml;
using OrzioClashReport.Input.RunManifestJson;
using OrzioClashReport.Output.Html;

namespace OrzioClashReport.Cli
{
    internal static class Program
    {
        private const string LegacyUsage = "Usage: orzioclash <input.xml> -o <output.html>";
        private const string CompareUsage = "Usage: orzioclash compare --previous-xml <previous.xml> --previous-manifest <previous.json> --current-xml <current.xml> --current-manifest <current.json>";

        private static int Main(string[] args)
        {
            if (args.Length > 0 && string.Equals(args[0], "compare", StringComparison.OrdinalIgnoreCase))
            {
                return RunCompare(args);
            }

            return RunLegacyReport(args);
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

                IClashMatcher matcher = new ConservativeClashMatcher();
                IClashRunComparer comparer = new DeterministicClashRunComparer(matcher);
                var matchResult = comparer.Compare(previousRun, currentRun);

                IClashLifecycleClassifier lifecycleClassifier = new ConservativeClashLifecycleClassifier();
                var lifecycleResult = lifecycleClassifier.Classify(matchResult);

                WriteComparisonSummary(lifecycleResult);
                return 0;
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Failed to compare runs: {ex.Message}");
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

            for (int i = 1; i < args.Length; i++)
            {
                string argument = args[i];
                if (!IsRecognizedCompareOption(argument))
                {
                    error = $"Unrecognized compare argument '{argument}'.";
                    return false;
                }

                if (i + 1 >= args.Length || string.IsNullOrWhiteSpace(args[i + 1]))
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

            options = new CompareCommandOptions(previousXmlPath, previousManifestPath, currentXmlPath, currentManifestPath);
            return true;
        }

        private static bool IsRecognizedCompareOption(string argument) =>
            argument == "--previous-xml"
            || argument == "--previous-manifest"
            || argument == "--current-xml"
            || argument == "--current-manifest";

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

        private sealed class CompareCommandOptions
        {
            public static readonly CompareCommandOptions Empty = new CompareCommandOptions(string.Empty, string.Empty, string.Empty, string.Empty);

            public CompareCommandOptions(
                string previousXmlPath,
                string previousManifestPath,
                string currentXmlPath,
                string currentManifestPath)
            {
                PreviousXmlPath = previousXmlPath;
                PreviousManifestPath = previousManifestPath;
                CurrentXmlPath = currentXmlPath;
                CurrentManifestPath = currentManifestPath;
            }

            public string PreviousXmlPath { get; }
            public string PreviousManifestPath { get; }
            public string CurrentXmlPath { get; }
            public string CurrentManifestPath { get; }
        }
    }
}
