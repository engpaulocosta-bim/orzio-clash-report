using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Text;
using OrzioClashReport.Core.Abstractions;
using OrzioClashReport.Core.Model;

namespace OrzioClashReport.Output.Html
{
    /// <summary>Implements <see cref="IReportRenderer"/> as a single self-contained, light-theme HTML file with deterministic byte output.</summary>
    public sealed class HtmlReportRenderer : IReportRenderer
    {
        private static readonly string[] Palette =
        {
            "#2e7d32", // green
            "#1565c0", // blue
            "#ef6c00", // orange
            "#c62828", // red
            "#6a1b9a", // purple
            "#f9a825"  // yellow
        };

        private static readonly string NormalizedCss = DeterministicHtmlLineEndings.NormalizeTemplateLiteral(Css);

        public string Render(GroupedClashReport report)
        {
            var disciplineColors = BuildDisciplineColorMap(report.Groups);

            var html = new StringBuilder();
            html.Append("<!doctype html>\n<html lang=\"en\">\n<head>\n");
            html.Append("<meta charset=\"utf-8\">\n");
            html.Append("<title>OrzioClashReport</title>\n");
            html.Append("<style>").Append(NormalizedCss).Append("</style>\n");
            html.Append("</head>\n<body>\n");

            AppendHeader(html, report);

            foreach (var group in report.Groups)
            {
                AppendGroup(html, group, disciplineColors);
            }

            html.Append("</body>\n</html>\n");

            return html.ToString();
        }

        private static Dictionary<string, string> BuildDisciplineColorMap(IReadOnlyList<ClashGroup> groups)
        {
            var distinctDisciplines = groups
                .SelectMany(g => new[] { g.DisciplineA, g.DisciplineB })
                .Distinct()
                .OrderBy(name => name, System.StringComparer.Ordinal)
                .ToList();

            var map = new Dictionary<string, string>();
            for (int i = 0; i < distinctDisciplines.Count; i++)
            {
                map[distinctDisciplines[i]] = Palette[i % Palette.Length];
            }

            return map;
        }

        private static void AppendHeader(StringBuilder html, GroupedClashReport report)
        {
            html.Append("<header class=\"report-header\">\n");
            html.Append("<h1>OrzioClashReport</h1>\n");

            if (!string.IsNullOrEmpty(report.Document.SourceName))
            {
                html.Append("<p class=\"source\">Source: ").Append(Encode(report.Document.SourceName!)).Append("</p>\n");
            }

            html.Append("<p class=\"summary\">")
                .Append(report.RawCount.ToString(CultureInfo.InvariantCulture))
                .Append(" raw clashes &rarr; ")
                .Append(report.GroupCount.ToString(CultureInfo.InvariantCulture))
                .Append(" groups</p>\n");
            html.Append("</header>\n");
        }

        private static void AppendGroup(StringBuilder html, ClashGroup group, IReadOnlyDictionary<string, string> disciplineColors)
        {
            html.Append("<section class=\"group\">\n");
            html.Append("<h2>");
            AppendDisciplineBadge(html, group.DisciplineA, disciplineColors);
            html.Append(" &times; ");
            AppendDisciplineBadge(html, group.DisciplineB, disciplineColors);
            html.Append(" <span class=\"level\">").Append(Encode(group.Level ?? "(no level)")).Append("</span>");
            html.Append("</h2>\n");

            html.Append("<p class=\"clash-test\">Clash test: ")
                .Append(Encode(group.ClashTestName ?? "(unnamed test)"))
                .Append("</p>\n");

            html.Append("<p class=\"group-count\">")
                .Append(group.Members.Count.ToString(CultureInfo.InvariantCulture))
                .Append(" clash(es)</p>\n");

            html.Append("<table>\n<thead><tr>")
                .Append("<th>Name</th><th>Status</th><th>Distance</th><th>Point</th>")
                .Append("<th>Element A</th><th>Element B</th>")
                .Append("</tr></thead>\n<tbody>\n");

            foreach (var clash in group.Members)
            {
                html.Append("<tr>");
                html.Append("<td>").Append(Encode(clash.Name ?? "(unnamed)")).Append("</td>");
                html.Append("<td>").Append(Encode(clash.Status.ToString())).Append("</td>");
                html.Append("<td>").Append(FormatDistance(clash.Distance)).Append("</td>");
                html.Append("<td>").Append(FormatPoint(clash.Point)).Append("</td>");
                html.Append("<td>").Append(Encode(clash.ElementA.ElementId)).Append("</td>");
                html.Append("<td>").Append(Encode(clash.ElementB.ElementId)).Append("</td>");
                html.Append("</tr>\n");
            }

            html.Append("</tbody>\n</table>\n");
            html.Append("</section>\n");
        }

        private static void AppendDisciplineBadge(StringBuilder html, string discipline, IReadOnlyDictionary<string, string> disciplineColors)
        {
            string color = disciplineColors.TryGetValue(discipline, out var c) ? c : "#616161";
            html.Append("<span class=\"badge\" style=\"background:").Append(color).Append("\">")
                .Append(Encode(discipline))
                .Append("</span>");
        }

        private static string FormatDistance(double? distance) =>
            distance.HasValue ? distance.Value.ToString("0.000", CultureInfo.InvariantCulture) : "&mdash;";

        private static string FormatPoint(ClashPoint? point) =>
            point.HasValue
                ? string.Format(
                    CultureInfo.InvariantCulture,
                    "{0:0.000}, {1:0.000}, {2:0.000}",
                    point.Value.X, point.Value.Y, point.Value.Z)
                : "&mdash;";

        private static string Encode(string value) => WebUtility.HtmlEncode(value);

        private const string Css = @"
body{font-family:Segoe UI,Arial,sans-serif;background:#fff;color:#212121;margin:2rem;}
.report-header{border-bottom:2px solid #212121;padding-bottom:1rem;margin-bottom:1.5rem;}
.report-header h1{margin:0 0 .25rem 0;}
.summary{font-size:1.1rem;font-weight:600;}
.source{color:#616161;font-size:.9rem;}
.group{margin-bottom:2rem;}
.group h2{font-size:1.1rem;margin-bottom:.25rem;}
.badge{color:#fff;border-radius:.75rem;padding:.15rem .6rem;font-size:.85rem;}
.level{color:#616161;font-weight:normal;font-size:.9rem;}
.clash-test{color:#616161;margin:.25rem 0 0 0;font-size:.9rem;}
.group-count{color:#616161;margin:.25rem 0 .5rem 0;}
table{border-collapse:collapse;width:100%;}
th,td{border:1px solid #e0e0e0;padding:.4rem .6rem;text-align:left;font-size:.9rem;}
th{background:#fafafa;}
";
    }
}
