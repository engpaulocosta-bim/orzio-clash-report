using OrzioClashReport.Core.Abstractions;
using OrzioClashReport.Core.Grouping;
using OrzioClashReport.Input.NavisworksXml;
using OrzioClashReport.Output.Html;

namespace OrzioClashReport.Cli
{
    internal static class Program
    {
        private static int Main(string[] args)
        {
            if (!TryParseArguments(args, out string inputPath, out string outputPath, out string parseError))
            {
                Console.Error.WriteLine(parseError);
                Console.Error.WriteLine("Usage: orzioclash <input.xml> -o <output.html>");
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

        private static bool TryParseArguments(
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
    }
}
