using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Text;
using OrzioClashReport.Core.Abstractions;
using OrzioClashReport.Core.Model;

namespace OrzioClashReport.Output.Html
{
    /// <summary>
    /// Implements <see cref="IReportRenderer"/> as a single self-contained HTML file with
    /// deterministic byte output.
    ///
    /// The document is written to be forwarded: it is headed by the model it came from, states the
    /// units its distances are in, and prints without splitting a clash group across two pages. The
    /// clock is a constructor argument rather than an ambient read, so the same report rendered
    /// twice is the same bytes twice.
    /// </summary>
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

        private readonly ReportTexts _texts;
        private readonly DateTimeOffset? _generatedAt;

        /// <summary>Renders in English without a generation timestamp.</summary>
        public HtmlReportRenderer()
            : this(ReportLanguage.English, null)
        {
        }

        /// <param name="language">The language the report writes its own wording in.</param>
        /// <param name="generatedAt">
        /// When the report was produced, shown in UTC. Null omits the line entirely rather than
        /// inventing a time, which is also what keeps a golden comparison byte-stable.
        /// </param>
        public HtmlReportRenderer(ReportLanguage language, DateTimeOffset? generatedAt)
        {
            _texts = new ReportTexts(language);
            _generatedAt = generatedAt;
        }

        public string Render(GroupedClashReport report)
        {
            if (report == null)
            {
                throw new ArgumentNullException(nameof(report));
            }

            var disciplineColors = BuildDisciplineColorMap(report.Groups);

            var html = new StringBuilder();
            html.Append("<!doctype html>\n<html lang=\"").Append(_texts.HtmlLanguageTag).Append("\">\n<head>\n");
            html.Append("<meta charset=\"utf-8\">\n");
            html.Append("<meta name=\"viewport\" content=\"width=device-width, initial-scale=1\">\n");
            html.Append("<title>").Append(DocumentTitleMarkup(report)).Append("</title>\n");
            html.Append("<style>").Append(NormalizedCss).Append("</style>\n");
            html.Append("</head>\n<body>\n");

            AppendHeader(html, report);

            foreach (var group in report.Groups)
            {
                AppendGroup(html, group, disciplineColors, report.Document.Units);
            }

            html.Append("</body>\n</html>\n");

            return html.ToString();
        }

        /// <summary>
        /// What the reader should see named at the top: the model this came from, not the tool that
        /// produced it. Any directory part is dropped, so a source recorded as a full path cannot
        /// carry a folder chain into a document that gets forwarded.
        /// </summary>
        private string DocumentTitle(GroupedClashReport report, out bool isFallback)
        {
            isFallback = true;

            string? source = report.Document.SourceName;
            if (string.IsNullOrWhiteSpace(source))
            {
                return _texts.DocumentTitle;
            }

            string leaf = LeafName(source!);
            int dot = leaf.LastIndexOf('.');
            string stem = dot > 0 ? leaf.Substring(0, dot) : leaf;

            if (stem.Length == 0)
            {
                return _texts.DocumentTitle;
            }

            isFallback = false;
            return stem;
        }

        /// <summary>
        /// The title ready to be written: the report's own wording stands as it is, a name taken
        /// from the export is escaped first.
        /// </summary>
        private string DocumentTitleMarkup(GroupedClashReport report)
        {
            string title = DocumentTitle(report, out bool isFallback);
            return isFallback ? title : Encode(title);
        }

        private static string LeafName(string value)
        {
            int separator = value.LastIndexOfAny(new[] { '\\', '/' });
            return separator >= 0 ? value.Substring(separator + 1) : value;
        }

        private static Dictionary<string, string> BuildDisciplineColorMap(IReadOnlyList<ClashGroup> groups)
        {
            var distinctDisciplines = groups
                .SelectMany(g => new[] { g.DisciplineA, g.DisciplineB })
                .Distinct()
                .OrderBy(name => name, StringComparer.Ordinal)
                .ToList();

            var map = new Dictionary<string, string>();
            for (int i = 0; i < distinctDisciplines.Count; i++)
            {
                map[distinctDisciplines[i]] = Palette[i % Palette.Length];
            }

            return map;
        }

        private void AppendHeader(StringBuilder html, GroupedClashReport report)
        {
            _ = DocumentTitle(report, out bool isFallback);

            html.Append("<header class=\"report-header\">\n");
            html.Append("<h1>").Append(DocumentTitleMarkup(report)).Append("</h1>\n");

            // When the title already had to fall back to the generic wording, repeating it as a
            // subtitle would say the same thing twice.
            if (!isFallback)
            {
                html.Append("<p class=\"subtitle\">").Append(_texts.Subtitle).Append("</p>\n");
            }

            AppendMetadata(html, report);

            html.Append("<p class=\"summary\">")
                .Append(_texts.Summary(report.RawCount, report.GroupCount))
                .Append("</p>\n");
            html.Append("</header>\n");
        }

        /// <summary>
        /// The provenance a reader needs to trust the page: which model, which clash tests, which
        /// unit, and when. Every line is omitted when the source did not state it.
        /// </summary>
        private void AppendMetadata(StringBuilder html, GroupedClashReport report)
        {
            var rows = new List<KeyValuePair<string, string>>();

            if (!string.IsNullOrWhiteSpace(report.Document.SourceName))
            {
                rows.Add(new KeyValuePair<string, string>(
                    _texts.SourceModelLabel, LeafName(report.Document.SourceName!)));
            }

            string tests = string.Join(", ", report.Document.Batches
                .Select(batch => batch.Name)
                .Where(name => !string.IsNullOrWhiteSpace(name))
                .Select(name => name!)
                .ToArray());

            if (tests.Length > 0)
            {
                rows.Add(new KeyValuePair<string, string>(_texts.ClashTestsLabel, tests));
            }

            if (!string.IsNullOrWhiteSpace(report.Document.Units))
            {
                rows.Add(new KeyValuePair<string, string>(_texts.UnitsLabel, report.Document.Units!));
            }

            if (_generatedAt.HasValue)
            {
                rows.Add(new KeyValuePair<string, string>(_texts.GeneratedLabel, FormatInstant(_generatedAt.Value)));
            }

            if (rows.Count == 0)
            {
                return;
            }

            html.Append("<dl class=\"meta\">\n");
            foreach (var row in rows)
            {
                // The label is the report's own word and is written as it stands; the value came
                // from the export and is escaped.
                html.Append("<dt>").Append(row.Key).Append("</dt>");
                html.Append("<dd>").Append(Encode(row.Value)).Append("</dd>\n");
            }

            html.Append("</dl>\n");
        }

        private static string FormatInstant(DateTimeOffset instant) =>
            instant.ToUniversalTime().ToString("yyyy-MM-dd HH:mm 'UTC'", CultureInfo.InvariantCulture);

        private void AppendGroup(
            StringBuilder html,
            ClashGroup group,
            IReadOnlyDictionary<string, string> disciplineColors,
            string? units)
        {
            html.Append("<section class=\"group\">\n");
            html.Append("<h2>");
            AppendDisciplineBadge(html, group.DisciplineA, disciplineColors);
            html.Append(" &times; ");
            AppendDisciplineBadge(html, group.DisciplineB, disciplineColors);
            html.Append(" <span class=\"level\">")
                .Append(group.Level == null ? _texts.NoLevel : Encode(group.Level))
                .Append("</span>");
            html.Append("</h2>\n");

            html.Append("<p class=\"clash-test\">")
                .Append(_texts.ClashTestLabel)
                .Append(": ")
                .Append(group.ClashTestName == null ? _texts.UnnamedTest : Encode(group.ClashTestName))
                .Append("</p>\n");

            html.Append("<p class=\"group-count\">")
                .Append(_texts.GroupCount(group.Members.Count))
                .Append("</p>\n");

            html.Append("<table>\n<thead><tr>")
                .Append("<th>").Append(_texts.ColumnName).Append("</th>")
                .Append("<th>").Append(_texts.ColumnStatus).Append("</th>")
                .Append("<th>").Append(DistanceColumnHeading(units)).Append("</th>")
                .Append("<th>").Append(PointColumnHeading(units)).Append("</th>")
                .Append("<th>").Append(_texts.ColumnElementA).Append("</th>")
                .Append("<th>").Append(_texts.ColumnElementB).Append("</th>")
                .Append("</tr></thead>\n<tbody>\n");

            foreach (var clash in group.Members)
            {
                html.Append("<tr>");
                html.Append("<td>").Append(clash.Name == null ? _texts.Unnamed : Encode(clash.Name)).Append("</td>");

                // The status is the source's own vocabulary and is never translated: it is what the
                // coordination record says, in whichever language the page is written.
                html.Append("<td>").Append(Encode(clash.Status.ToString())).Append("</td>");
                html.Append("<td class=\"numeric\">").Append(FormatDistance(clash.Distance)).Append("</td>");
                html.Append("<td class=\"numeric\">").Append(FormatPoint(clash.Point)).Append("</td>");
                html.Append("<td>").Append(Encode(clash.ElementA.ElementId)).Append("</td>");
                html.Append("<td>").Append(Encode(clash.ElementB.ElementId)).Append("</td>");
                html.Append("</tr>\n");
            }

            html.Append("</tbody>\n</table>\n");
            html.Append("</section>\n");
        }

        // The heading is the report's own word; the unit beside it came from the export and is
        // escaped before it is placed next to it.
        private string DistanceColumnHeading(string? units) => Heading(_texts.ColumnDistance, units);

        private string PointColumnHeading(string? units) => Heading(_texts.ColumnPoint, units);

        private static string Heading(string label, string? units) =>
            string.IsNullOrWhiteSpace(units) ? label : label + " (" + Encode(units!) + ")";

        private static void AppendDisciplineBadge(StringBuilder html, string discipline, IReadOnlyDictionary<string, string> disciplineColors)
        {
            // The colour still distinguishes the discipline by its full value, so two different
            // models that happen to share a file name stay visibly different.
            string color = disciplineColors.TryGetValue(discipline, out var c) ? c : "#616161";
            html.Append("<span class=\"badge\" style=\"background:").Append(color).Append("\">")
                .Append(Encode(DisciplineLabel(discipline)))
                .Append("</span>");
        }

        /// <summary>
        /// How a discipline is shown. The resolver reports whatever the export's path hierarchy or
        /// source-file property held, and in a real export that is frequently the nested model's
        /// absolute path on the machine that produced it. A report is forwarded to other companies,
        /// so only the model's own name is shown; the value itself is left untouched.
        /// </summary>
        private static string DisciplineLabel(string discipline)
        {
            if (string.IsNullOrWhiteSpace(discipline))
            {
                return discipline;
            }

            string leaf = LeafName(discipline);
            int dot = leaf.LastIndexOf('.');
            string stem = dot > 0 ? leaf.Substring(0, dot) : leaf;

            return stem.Length == 0 ? leaf : stem;
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
.report-header h1{margin:0;font-size:1.6rem;overflow-wrap:anywhere;}
.subtitle{color:#616161;margin:.15rem 0 0 0;font-size:1rem;}
.meta{display:grid;grid-template-columns:auto 1fr;gap:.15rem 1rem;margin:.9rem 0 0 0;font-size:.9rem;}
.meta dt{color:#616161;}
.meta dd{margin:0;overflow-wrap:anywhere;}
.summary{font-size:1.1rem;font-weight:600;margin:.9rem 0 0 0;}
.group{margin-bottom:2rem;}
.group h2{font-size:1.1rem;margin-bottom:.25rem;}
.badge{color:#fff;border-radius:.75rem;padding:.15rem .6rem;font-size:.85rem;}
.level{color:#616161;font-weight:normal;font-size:.9rem;}
.clash-test{color:#616161;margin:.25rem 0 0 0;font-size:.9rem;}
.group-count{color:#616161;margin:.25rem 0 .5rem 0;}
table{border-collapse:collapse;width:100%;}
th,td{border:1px solid #e0e0e0;padding:.4rem .6rem;text-align:left;font-size:.9rem;}
th{background:#fafafa;}
td.numeric{font-variant-numeric:tabular-nums;white-space:nowrap;}
@media print{
body{margin:0;font-size:10pt;}
.report-header{page-break-after:avoid;break-after:avoid;}
.group{page-break-inside:avoid;break-inside:avoid;margin-bottom:1.2rem;}
.group h2{page-break-after:avoid;break-after:avoid;}
thead{display:table-header-group;}
tr{page-break-inside:avoid;break-inside:avoid;}
.badge{color:#000;background:transparent!important;border:1px solid #000;border-radius:.75rem;}
th{background:#f2f2f2;}
th,td{border:1px solid #999;}
}
";
    }
}
