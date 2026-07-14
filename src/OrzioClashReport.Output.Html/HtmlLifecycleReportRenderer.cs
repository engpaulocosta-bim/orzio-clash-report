using System;
using System.Globalization;
using System.Net;
using System.Text;
using OrzioClashReport.Core.Model;

namespace OrzioClashReport.Output.Html
{
    /// <summary>Renders a deterministic, self-contained lifecycle comparison HTML report from an existing <see cref="ClashLifecycleResult"/>.</summary>
    public sealed class HtmlLifecycleReportRenderer
    {
        public string Render(ClashLifecycleResult result)
        {
            if (result == null)
            {
                throw new ArgumentNullException(nameof(result));
            }

            int stillOpenCount = 0;
            int newCount = 0;
            int resolvedCount = 0;
            int unverifiableCount = 0;

            foreach (var entry in result.Entries)
            {
                switch (entry.Status)
                {
                    case ClashLifecycleStatus.StillOpen:
                        stillOpenCount++;
                        break;
                    case ClashLifecycleStatus.New:
                        newCount++;
                        break;
                    case ClashLifecycleStatus.Resolved:
                        resolvedCount++;
                        break;
                    case ClashLifecycleStatus.Unverifiable:
                        unverifiableCount++;
                        break;
                }
            }

            var html = new StringBuilder();
            html.Append("<!doctype html>\n<html lang=\"en\">\n<head>\n");
            html.Append("<meta charset=\"utf-8\">\n");
            html.Append("<title>Orzio Clash Comparison</title>\n");
            html.Append("<style>").Append(Css).Append("</style>\n");
            html.Append("</head>\n<body>\n");

            AppendHeader(html, result);
            AppendRunComparison(html, result.MatchResult.PreviousRun, result.MatchResult.CurrentRun);
            AppendLifecycleSummary(html, stillOpenCount, newCount, resolvedCount, unverifiableCount);
            AppendMatchingSummary(html, result.MatchResult);
            AppendLifecycleEntries(html, result);
            AppendClassificationNote(html);

            html.Append("</body>\n</html>\n");
            return html.ToString();
        }

        private static void AppendHeader(StringBuilder html, ClashLifecycleResult result)
        {
            html.Append("<header class=\"comparison-header\">\n");
            html.Append("<h1>Orzio Clash Comparison</h1>\n");
            html.Append("<p class=\"comparison-flow-label\">Previous run &rarr; Current run</p>\n");
            html.Append("<p class=\"comparison-flow\">")
                .Append(Encode(result.MatchResult.PreviousRun.RunId))
                .Append(" &rarr; ")
                .Append(Encode(result.MatchResult.CurrentRun.RunId))
                .Append("</p>\n");
            html.Append("</header>\n");
        }

        private static void AppendRunComparison(StringBuilder html, CoordinationRun previousRun, CoordinationRun currentRun)
        {
            html.Append("<section class=\"run-comparison-section\">\n");
            html.Append("<h2>Run comparison</h2>\n");
            html.Append("<div class=\"run-comparison\">\n");
            AppendRunCard(html, "Previous run", "run-card previous-run", previousRun);
            AppendRunCard(html, "Current run", "run-card current-run", currentRun);
            html.Append("</div>\n");
            html.Append("</section>\n");
        }

        private static void AppendRunCard(StringBuilder html, string title, string cssClass, CoordinationRun run)
        {
            html.Append("<article class=\"").Append(cssClass).Append("\">\n");
            html.Append("<h3>").Append(title).Append("</h3>\n");
            html.Append("<dl class=\"run-metadata\">\n");
            AppendDefinitionRow(html, "Run ID", Encode(run.RunId));
            AppendDefinitionRow(html, "Created at", Encode(run.CreatedAt.ToString("O", CultureInfo.InvariantCulture)));
            AppendDefinitionRow(html, "Occurrences", run.Occurrences.Count.ToString(CultureInfo.InvariantCulture));
            AppendDefinitionRow(html, "Executed clash tests", run.ExecutedClashTests.Count.ToString(CultureInfo.InvariantCulture));
            html.Append("</dl>\n");
            html.Append("<div class=\"run-models\">\n");
            html.Append("<h4>Models</h4>\n");
            html.Append("<ul>\n");
            foreach (var model in run.Models)
            {
                html.Append("<li>");
                html.Append("<div class=\"model-label\">").Append(FormatModel(model)).Append("</div>");
                html.Append("<div class=\"model-source\">Source file: ").Append(Encode(model.SourceFileName)).Append("</div>");
                html.Append("</li>\n");
            }

            html.Append("</ul>\n");
            html.Append("</div>\n");
            html.Append("</article>\n");
        }

        private static void AppendLifecycleSummary(
            StringBuilder html,
            int stillOpenCount,
            int newCount,
            int resolvedCount,
            int unverifiableCount)
        {
            html.Append("<section class=\"lifecycle-summary-section\">\n");
            html.Append("<h2>Lifecycle summary</h2>\n");
            html.Append("<div class=\"summary-grid\">\n");
            AppendSummaryCard(html, "StillOpen", "status-stillopen", stillOpenCount);
            AppendSummaryCard(html, "New", "status-new", newCount);
            AppendSummaryCard(html, "Resolved", "status-resolved", resolvedCount);
            AppendSummaryCard(html, "Unverifiable", "status-unverifiable", unverifiableCount);
            html.Append("</div>\n");
            html.Append("</section>\n");
        }

        private static void AppendSummaryCard(StringBuilder html, string label, string statusClass, int count)
        {
            html.Append("<article class=\"summary-card ").Append(statusClass).Append("\">");
            html.Append("<h3>").Append(label).Append("</h3>");
            html.Append("<p class=\"summary-count\">").Append(count.ToString(CultureInfo.InvariantCulture)).Append("</p>");
            html.Append("</article>\n");
        }

        private static void AppendMatchingSummary(StringBuilder html, ClashRunMatchResult matchResult)
        {
            html.Append("<section class=\"matching-summary-section\">\n");
            html.Append("<h2>Matching summary</h2>\n");
            html.Append("<div class=\"matching-summary-grid\">\n");
            AppendMetricCard(html, "Previous occurrences", matchResult.PreviousRun.Occurrences.Count);
            AppendMetricCard(html, "Current occurrences", matchResult.CurrentRun.Occurrences.Count);
            AppendMetricCard(html, "Candidates", matchResult.Candidates.Count);
            AppendMetricCard(html, "Selected matches", matchResult.SelectedMatches.Count);
            AppendMetricCard(html, "Alternative candidates", matchResult.AlternativeCandidates.Count);
            html.Append("</div>\n");
            html.Append("</section>\n");
        }

        private static void AppendMetricCard(StringBuilder html, string label, int count)
        {
            html.Append("<article class=\"metric-card\">");
            html.Append("<h3>").Append(label).Append("</h3>");
            html.Append("<p class=\"metric-count\">").Append(count.ToString(CultureInfo.InvariantCulture)).Append("</p>");
            html.Append("</article>\n");
        }

        private static void AppendLifecycleEntries(StringBuilder html, ClashLifecycleResult result)
        {
            html.Append("<main class=\"lifecycle-entries\">\n");
            foreach (var entry in result.Entries)
            {
                AppendLifecycleEntry(html, entry);
            }

            html.Append("</main>\n");
        }

        private static void AppendLifecycleEntry(StringBuilder html, ClashLifecycleEntry entry)
        {
            string statusClass = GetStatusClass(entry.Status);
            html.Append("<section class=\"lifecycle-entry ").Append(statusClass).Append("\">\n");
            html.Append("<header class=\"lifecycle-entry-header\">\n");
            html.Append("<h2>").Append(Encode(entry.Status.ToString())).Append("</h2>\n");
            html.Append("<p class=\"slot-reference\">")
                .Append(Encode(FormatSlot("previous", entry.PreviousIndex)))
                .Append(" &times; ")
                .Append(Encode(FormatSlot("current", entry.CurrentIndex)))
                .Append("</p>\n");
            html.Append("</header>\n");

            html.Append("<div class=\"occurrence-grid\">\n");
            AppendOccurrencePanel(html, "Previous occurrence", "occurrence-panel previous-occurrence", entry.PreviousOccurrence, isPrevious: true);
            AppendOccurrencePanel(html, "Current occurrence", "occurrence-panel current-occurrence", entry.CurrentOccurrence, isPrevious: false);
            html.Append("</div>\n");

            if (entry.SelectedMatch != null)
            {
                AppendSelectedMatch(html, entry.SelectedMatch);
            }

            AppendLifecycleEvidence(html, entry.Evidence);

            if (entry.SelectedMatch != null)
            {
                AppendMatchEvidence(html, entry.SelectedMatch.Assessment.Evidence);
            }

            html.Append("</section>\n");
        }

        private static void AppendOccurrencePanel(
            StringBuilder html,
            string title,
            string cssClass,
            ClashOccurrence? occurrence,
            bool isPrevious)
        {
            if (occurrence == null)
            {
                html.Append("<article class=\"").Append(cssClass).Append(" missing-occurrence\">\n");
                html.Append("<h3>").Append(title).Append("</h3>\n");
                html.Append("<p>").Append(isPrevious ? "No previous occurrence" : "No current occurrence").Append("</p>\n");
                html.Append("</article>\n");
                return;
            }

            html.Append("<article class=\"").Append(cssClass).Append("\">\n");
            html.Append("<h3>").Append(title).Append("</h3>\n");
            html.Append("<dl class=\"occurrence-metadata\">\n");
            AppendDefinitionRow(html, "Clash test", Encode(occurrence.ClashTestName));
            AppendDefinitionRow(html, "Clash name", Encode(occurrence.Clash.Name ?? "(unnamed clash)"));
            AppendDefinitionRow(html, "Model A", FormatModel(occurrence.ModelA));
            AppendDefinitionRow(html, "Model B", FormatModel(occurrence.ModelB));
            AppendDefinitionRow(html, "Element A", FormatElement(occurrence.Clash.ElementA));
            AppendDefinitionRow(html, "Element B", FormatElement(occurrence.Clash.ElementB));
            AppendDefinitionRow(html, "Distance", FormatDistance(occurrence.Clash.Distance));
            AppendDefinitionRow(html, "Point", FormatPoint(occurrence.Clash.Point));
            AppendDefinitionRow(html, "Source GUID (evidence only)", FormatOptionalEncoded(occurrence.Clash.Guid));
            html.Append("</dl>\n");
            html.Append("</article>\n");
        }

        private static void AppendSelectedMatch(StringBuilder html, ClashRunMatchCandidate selectedMatch)
        {
            html.Append("<section class=\"selected-match-section\">\n");
            html.Append("<h3>Selected match</h3>\n");
            html.Append("<p class=\"selected-match-confidence\">Confidence: ")
                .Append(Encode(selectedMatch.Assessment.Confidence.ToString()))
                .Append("</p>\n");
            html.Append("</section>\n");
        }

        private static void AppendLifecycleEvidence(StringBuilder html, System.Collections.Generic.IReadOnlyList<ClashLifecycleEvidence> evidenceItems)
        {
            html.Append("<details class=\"evidence-section lifecycle-evidence\">\n");
            html.Append("<summary>Lifecycle evidence (")
                .Append(evidenceItems.Count.ToString(CultureInfo.InvariantCulture))
                .Append(")</summary>\n");
            html.Append("<ol class=\"evidence-list\">\n");
            foreach (var evidence in evidenceItems)
            {
                html.Append("<li class=\"evidence-item ")
                    .Append(GetLifecycleVerdictClass(evidence.Verdict))
                    .Append("\">\n");
                html.Append("<p><strong>Kind:</strong> ").Append(Encode(evidence.Kind.ToString())).Append("</p>\n");
                html.Append("<p><strong>Verdict:</strong> ").Append(Encode(evidence.Verdict.ToString())).Append("</p>\n");
                html.Append("<p><strong>Description:</strong> ").Append(Encode(evidence.Description)).Append("</p>\n");
                html.Append("</li>\n");
            }

            html.Append("</ol>\n");
            html.Append("</details>\n");
        }

        private static void AppendMatchEvidence(StringBuilder html, System.Collections.Generic.IReadOnlyList<MatchEvidence> evidenceItems)
        {
            html.Append("<details class=\"evidence-section match-evidence\">\n");
            html.Append("<summary>Match evidence (")
                .Append(evidenceItems.Count.ToString(CultureInfo.InvariantCulture))
                .Append(")</summary>\n");
            html.Append("<ol class=\"evidence-list\">\n");
            foreach (var evidence in evidenceItems)
            {
                html.Append("<li class=\"evidence-item ")
                    .Append(GetMatchVerdictClass(evidence.Verdict))
                    .Append("\">\n");
                html.Append("<p><strong>Kind:</strong> ").Append(Encode(evidence.Kind.ToString())).Append("</p>\n");
                html.Append("<p><strong>Verdict:</strong> ").Append(Encode(evidence.Verdict.ToString())).Append("</p>\n");
                html.Append("<p><strong>Description:</strong> ").Append(Encode(evidence.Description)).Append("</p>\n");
                html.Append("<p><strong>Previous value:</strong> ").Append(FormatOptionalEncoded(evidence.PreviousValue)).Append("</p>\n");
                html.Append("<p><strong>Current value:</strong> ").Append(FormatOptionalEncoded(evidence.CurrentValue)).Append("</p>\n");
                html.Append("</li>\n");
            }

            html.Append("</ol>\n");
            html.Append("</details>\n");
        }

        private static void AppendClassificationNote(StringBuilder html)
        {
            html.Append("<footer class=\"classification-note\">\n");
            html.Append("<h2>Classification note</h2>\n");
            html.Append("<p>Lifecycle classifications are based on two explicitly supplied coordination runs.</p>\n");
            html.Append("<p>Match confidence is evidence-based and is not human confirmation.</p>\n");
            html.Append("<p>Source clash GUIDs are evidence only and are not treated as stable clash identities.</p>\n");
            html.Append("<p>This report does not classify Reopened clashes and does not assign persistent clash IDs.</p>\n");
            html.Append("</footer>\n");
        }

        private static void AppendDefinitionRow(StringBuilder html, string label, string value)
        {
            html.Append("<dt>").Append(label).Append("</dt><dd>").Append(value).Append("</dd>\n");
        }

        private static string FormatSlot(string side, int? index) =>
            index.HasValue
                ? string.Format(CultureInfo.InvariantCulture, "{0}[{1}]", side, index.Value)
                : side + "[-]";

        private static string FormatModel(ModelRevision model) =>
            Encode(
                string.Format(
                    CultureInfo.InvariantCulture,
                    "{0} / {1} / {2} @ {3}",
                    model.Identity.Company,
                    model.Identity.Discipline,
                    model.Identity.ModelName,
                    model.Revision));

        private static string FormatElement(ClashObject element)
        {
            var text = new StringBuilder();
            text.Append(Encode(element.ElementId));

            if (!string.IsNullOrWhiteSpace(element.ElementName))
            {
                text.Append("<br>Name: ").Append(Encode(element.ElementName!));
            }

            if (!string.IsNullOrWhiteSpace(element.Level))
            {
                text.Append("<br>Level: ").Append(Encode(element.Level!));
            }

            return text.ToString();
        }

        private static string FormatDistance(double? distance) =>
            distance.HasValue
                ? distance.Value.ToString("0.000", CultureInfo.InvariantCulture)
                : "&mdash;";

        private static string FormatPoint(ClashPoint? point) =>
            point.HasValue
                ? string.Format(
                    CultureInfo.InvariantCulture,
                    "{0:0.000}, {1:0.000}, {2:0.000}",
                    point.Value.X,
                    point.Value.Y,
                    point.Value.Z)
                : "&mdash;";

        private static string FormatOptionalEncoded(string? value) =>
            string.IsNullOrWhiteSpace(value) ? "&mdash;" : Encode(value!);

        private static string Encode(string value) => WebUtility.HtmlEncode(value);

        private static string GetStatusClass(ClashLifecycleStatus status)
        {
            switch (status)
            {
                case ClashLifecycleStatus.StillOpen:
                    return "status-stillopen";
                case ClashLifecycleStatus.New:
                    return "status-new";
                case ClashLifecycleStatus.Resolved:
                    return "status-resolved";
                case ClashLifecycleStatus.Unverifiable:
                    return "status-unverifiable";
                default:
                    throw new ArgumentOutOfRangeException(nameof(status), status, "Unsupported lifecycle status.");
            }
        }

        private static string GetLifecycleVerdictClass(ClashLifecycleEvidenceVerdict verdict)
        {
            switch (verdict)
            {
                case ClashLifecycleEvidenceVerdict.SupportsClassification:
                    return "verdict-supportsclassification";
                case ClashLifecycleEvidenceVerdict.BlocksClassification:
                    return "verdict-blocksclassification";
                default:
                    throw new ArgumentOutOfRangeException(nameof(verdict), verdict, "Unsupported lifecycle evidence verdict.");
            }
        }

        private static string GetMatchVerdictClass(MatchEvidenceVerdict verdict)
        {
            switch (verdict)
            {
                case MatchEvidenceVerdict.Supports:
                    return "verdict-supports";
                case MatchEvidenceVerdict.Contradicts:
                    return "verdict-contradicts";
                case MatchEvidenceVerdict.Unavailable:
                    return "verdict-unavailable";
                default:
                    throw new ArgumentOutOfRangeException(nameof(verdict), verdict, "Unsupported match evidence verdict.");
            }
        }

        private const string Css = @"
body{margin:0;background:#f5f2ea;color:#1f2933;font-family:system-ui,-apple-system,BlinkMacSystemFont,""Segoe UI"",sans-serif;line-height:1.5;}
body{max-width:1200px;padding:32px 24px;margin:0 auto;}
h1,h2,h3,h4,p,ol,ul,dl{margin-top:0;}
h2{font-size:1.3rem;}
.comparison-header{background:linear-gradient(135deg,#fef3c7,#fffaf0);border:1px solid #d9c7a1;border-radius:20px;padding:24px;margin-bottom:24px;box-shadow:0 12px 30px rgba(77,58,27,.08);}
.comparison-header h1{font-size:2rem;margin-bottom:8px;}
.comparison-flow-label{font-size:.9rem;font-weight:700;letter-spacing:.04em;text-transform:uppercase;color:#8a6b32;margin-bottom:4px;}
.comparison-flow{font-size:1.05rem;font-weight:600;overflow-wrap:anywhere;}
.run-comparison-section,.lifecycle-summary-section,.matching-summary-section,.classification-note{margin-bottom:24px;}
.run-comparison{display:grid;grid-template-columns:repeat(2,minmax(0,1fr));gap:18px;}
.run-card,.summary-card,.metric-card,.lifecycle-entry{background:#fff;border:1px solid #d7dde5;border-radius:18px;box-shadow:0 10px 24px rgba(15,23,42,.05);}
.run-card{padding:20px;}
.previous-run{border-top:4px solid #1d4ed8;}
.current-run{border-top:4px solid #ea580c;}
.run-metadata,.occurrence-metadata{display:grid;grid-template-columns:minmax(140px,180px) minmax(0,1fr);gap:6px 12px;margin-bottom:16px;}
.run-metadata dt,.occurrence-metadata dt{font-weight:700;color:#334155;}
.run-metadata dd,.occurrence-metadata dd{margin:0;overflow-wrap:anywhere;}
.run-models h4{margin-bottom:10px;}
.run-models ul{padding-left:18px;margin-bottom:0;}
.run-models li{margin-bottom:10px;overflow-wrap:anywhere;}
.model-label{font-weight:600;}
.model-source{font-size:.95rem;color:#52606d;}
.summary-grid,.matching-summary-grid{display:grid;grid-template-columns:repeat(4,minmax(0,1fr));gap:16px;}
.matching-summary-grid{grid-template-columns:repeat(5,minmax(0,1fr));}
.summary-card,.metric-card{padding:18px;text-align:center;}
.summary-card h3,.metric-card h3{font-size:1rem;margin-bottom:10px;}
.summary-count,.metric-count{font-size:2rem;font-weight:800;margin:0;}
.status-stillopen{border-left:8px solid #2563eb;}
.status-new{border-left:8px solid #f97316;}
.status-resolved{border-left:8px solid #15803d;}
.status-unverifiable{border-left:8px solid #64748b;}
.lifecycle-entries{display:grid;gap:20px;margin-bottom:24px;}
.lifecycle-entry{padding:20px;}
.lifecycle-entry-header{display:flex;justify-content:space-between;gap:12px;align-items:flex-start;margin-bottom:16px;}
.lifecycle-entry-header h2{margin-bottom:0;}
.slot-reference{font-family:Consolas,""SFMono-Regular"",Menlo,monospace;font-size:.95rem;color:#475569;overflow-wrap:anywhere;}
.occurrence-grid{display:grid;grid-template-columns:repeat(2,minmax(0,1fr));gap:16px;margin-bottom:16px;}
.occurrence-panel{padding:16px;border:1px solid #d7dde5;border-radius:14px;background:#fcfcfd;}
.occurrence-panel h3{margin-bottom:12px;}
.previous-occurrence{background:linear-gradient(180deg,#eff6ff,#fcfcfd);}
.current-occurrence{background:linear-gradient(180deg,#fff7ed,#fcfcfd);}
.missing-occurrence{display:flex;flex-direction:column;justify-content:center;}
.selected-match-section{margin-bottom:16px;padding:14px 16px;border-radius:14px;background:#f8fafc;border:1px solid #d7dde5;}
.selected-match-section h3{margin-bottom:8px;}
.selected-match-confidence{margin-bottom:0;font-weight:700;}
.evidence-section{margin-bottom:16px;border:1px solid #d7dde5;border-radius:14px;background:#f8fafc;padding:0;}
.evidence-section summary{cursor:pointer;font-weight:700;padding:14px 16px;}
.evidence-list{margin:0;padding:0 16px 16px 36px;}
.evidence-item{padding:12px 0;overflow-wrap:anywhere;}
.evidence-item + .evidence-item{border-top:1px solid #e5e7eb;}
.verdict-supportsclassification,.verdict-supports{border-left:4px solid #15803d;padding-left:12px;}
.verdict-blocksclassification,.verdict-contradicts{border-left:4px solid #c2410c;padding-left:12px;}
.verdict-unavailable{border-left:4px solid #64748b;padding-left:12px;}
.classification-note{padding:20px;background:#fff;border:1px solid #d7dde5;border-radius:18px;}
.classification-note p:last-child{margin-bottom:0;}
@media (max-width:900px){
  body{padding:20px 16px;}
  .run-comparison,.summary-grid,.matching-summary-grid,.occurrence-grid{grid-template-columns:1fr;}
  .lifecycle-entry-header{flex-direction:column;}
}
@media print{
  body{background:#fff;padding:0;}
  .comparison-header,.run-card,.summary-card,.metric-card,.lifecycle-entry,.classification-note,.occurrence-panel,.selected-match-section,.evidence-section{box-shadow:none;}
  .comparison-header,.run-card,.summary-card,.metric-card,.lifecycle-entry,.classification-note{break-inside:avoid;}
}
";
    }
}
