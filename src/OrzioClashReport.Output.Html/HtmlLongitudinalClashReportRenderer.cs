using System;
using System.Globalization;
using System.Net;
using System.Text;
using OrzioClashReport.Core.Model;

namespace OrzioClashReport.Output.Html
{
    /// <summary>Renders a deterministic, self-contained longitudinal HTML report from an existing presentation result.</summary>
    public sealed class HtmlLongitudinalClashReportRenderer
    {
        public string Render(ClashRunSequencePresentationResult result)
        {
            if (result == null)
            {
                throw new ArgumentNullException(nameof(result));
            }

            var html = new StringBuilder();
            html.Append("<!doctype html>\n<html lang=\"en\">\n<head>\n");
            html.Append("<meta charset=\"utf-8\">\n");
            html.Append("<title>Orzio Clash Longitudinal Report</title>\n");
            html.Append("<style>").Append(Css).Append("</style>\n");
            html.Append("</head>\n<body>\n");

            AppendHeader(html, result);
            AppendLongitudinalSummary(html, result);
            AppendInterpretationWarning(html);
            AppendRunSequence(html, result);
            AppendContinuityPaths(html, result);
            AppendStandaloneSelectedMatches(html, result);
            AppendNonPathLifecycleEntries(html, result);
            AppendTransitions(html, result);
            AppendClassificationNote(html);

            html.Append("</body>\n</html>\n");
            return html.ToString();
        }

        private static void AppendHeader(StringBuilder html, ClashRunSequencePresentationResult result)
        {
            html.Append("<header class=\"longitudinal-header\">\n");
            html.Append("<h1>Orzio Clash Longitudinal Report</h1>\n");
            html.Append("<p>The run order below was explicitly declared by the run index.</p>\n");
            html.Append("<ol class=\"run-flow\">\n");
            foreach (var run in result.Runs)
            {
                html.Append("<li>").Append(Encode(run.RunId)).Append("</li>\n");
            }

            html.Append("</ol>\n");
            html.Append("</header>\n");
        }

        private static void AppendLongitudinalSummary(StringBuilder html, ClashRunSequencePresentationResult result)
        {
            html.Append("<section class=\"longitudinal-summary-section\">\n");
            html.Append("<h2>Longitudinal summary</h2>\n");
            html.Append("<div class=\"summary-grid\">\n");
            AppendSummaryCard(html, "Indexed runs", result.RunCount);
            AppendSummaryCard(html, "Adjacent comparisons", result.AdjacentComparisonCount);
            AppendSummaryCard(html, "Selected matches", result.SelectedMatchCount);
            AppendSummaryCard(html, "Continuity links", result.ContinuityLinkCount);
            AppendSummaryCard(html, "Continuity paths", result.ContinuityPathCount);
            AppendSummaryCard(html, "Standalone selected matches", result.StandaloneSelectedMatchCount);
            AppendSummaryCard(html, "Lifecycle entries", result.LifecycleEntryCount);
            AppendSummaryCard(html, "Non-path lifecycle entries", result.NonPathLifecycleEntryCount);
            AppendSummaryCard(html, "StillOpen", result.StillOpenCount);
            AppendSummaryCard(html, "New", result.NewCount);
            AppendSummaryCard(html, "Resolved", result.ResolvedCount);
            AppendSummaryCard(html, "Unverifiable", result.UnverifiableCount);
            html.Append("</div>\n");
            html.Append("</section>\n");
        }

        private static void AppendSummaryCard(StringBuilder html, string label, int value)
        {
            html.Append("<article class=\"summary-card\">");
            html.Append("<h3>").Append(label).Append("</h3>");
            html.Append("<p>").Append(value.ToString(CultureInfo.InvariantCulture)).Append("</p>");
            html.Append("</article>\n");
        }

        private static void AppendInterpretationWarning(StringBuilder html)
        {
            html.Append("<section class=\"interpretation-warning\">\n");
            html.Append("<h2>Interpretation warnings</h2>\n");
            html.Append("<ul>\n");
            html.Append("<li>Continuity paths are derived from selected matches recalculated in adjacent comparisons.</li>\n");
            html.Append("<li>Continuity is not a persistent or stable identity for a clash.</li>\n");
            html.Append("<li>High confidence is not human confirmation.</li>\n");
            html.Append("<li>Unverifiable means available evidence or candidate competition blocks safe automatic classification.</li>\n");
            html.Append("<li>This report does not implement Clash Ledger, Reopened, or aggregate multi-run lifecycle.</li>\n");
            html.Append("<li>Source clash GUID is evidence only.</li>\n");
            html.Append("</ul>\n");
            html.Append("</section>\n");
        }

        private static void AppendRunSequence(StringBuilder html, ClashRunSequencePresentationResult result)
        {
            html.Append("<section class=\"run-sequence-section\">\n");
            html.Append("<h2>Run sequence</h2>\n");
            for (int i = 0; i < result.Runs.Count; i++)
            {
                AppendRun(html, result.Runs[i], i);
            }

            html.Append("</section>\n");
        }

        private static void AppendRun(StringBuilder html, CoordinationRun run, int runIndex)
        {
            html.Append("<article class=\"run-card\">\n");
            html.Append("<h3>Run ").Append(ToOrdinal(runIndex)).Append("</h3>\n");
            html.Append("<p class=\"ordinal-note\">Presentation ordinal only; not an ID.</p>\n");
            html.Append("<dl>\n");
            AppendDefinitionRow(html, "Run ID", Encode(run.RunId));
            AppendDefinitionRow(html, "Created at", Encode(run.CreatedAt.ToString("O", CultureInfo.InvariantCulture)));
            AppendDefinitionRow(html, "Occurrences", FormatCount(run.Occurrences.Count));
            AppendDefinitionRow(html, "Executed clash tests", FormatCount(run.ExecutedClashTests.Count));
            html.Append("</dl>\n");
            html.Append("<h4>Declared model revisions</h4>\n");
            html.Append("<ol class=\"model-list\">\n");
            foreach (var model in run.Models)
            {
                html.Append("<li>");
                html.Append("<span class=\"model-label\">").Append(FormatModel(model)).Append("</span>");
                html.Append("<span class=\"model-source\">SourceFileName: ").Append(Encode(model.SourceFileName)).Append("</span>");
                html.Append("</li>\n");
            }

            html.Append("</ol>\n");
            html.Append("</article>\n");
        }

        private static void AppendContinuityPaths(StringBuilder html, ClashRunSequencePresentationResult result)
        {
            html.Append("<section class=\"continuity-paths-section\">\n");
            html.Append("<h2>Continuity paths</h2>\n");
            if (result.PathPresentations.Count == 0)
            {
                html.Append("<p class=\"empty-message\">Zero continuity paths.</p>\n");
            }
            else
            {
                for (int i = 0; i < result.PathPresentations.Count; i++)
                {
                    var pathPresentation = result.PathPresentations[i];
                    var path = pathPresentation.ContinuityPath;
                    html.Append("<article class=\"continuity-path\">\n");
                    html.Append("<h3>Continuity path ").Append(ToOrdinal(i)).Append("</h3>\n");
                    html.Append("<p class=\"ordinal-note\">This is a presentation ordinal, not an identifier for the path.</p>\n");
                    html.Append("<dl>\n");
                    AppendDefinitionRow(html, "Start run", ToOrdinal(path.StartRunIndex));
                    AppendDefinitionRow(html, "End run", ToOrdinal(path.EndRunIndex));
                    AppendDefinitionRow(html, "Start comparison", ToOrdinal(path.StartComparisonIndex));
                    AppendDefinitionRow(html, "End comparison", ToOrdinal(path.EndComparisonIndex));
                    AppendDefinitionRow(html, "Links", FormatCount(path.Links.Count));
                    AppendDefinitionRow(html, "Selected matches", FormatCount(path.SelectedMatches.Count));
                    html.Append("</dl>\n");
                    html.Append("<ol class=\"entry-list\">\n");
                    foreach (var entry in pathPresentation.SelectedMatchEntries)
                    {
                        AppendLifecycleEntry(html, result, entry, "path-selected-match");
                    }

                    html.Append("</ol>\n");
                    html.Append("</article>\n");
                }
            }

            html.Append("</section>\n");
        }

        private static void AppendStandaloneSelectedMatches(StringBuilder html, ClashRunSequencePresentationResult result)
        {
            html.Append("<section class=\"standalone-selected-matches-section\">\n");
            html.Append("<h2>Standalone selected matches</h2>\n");
            if (result.StandaloneSelectedMatches.Count == 0)
            {
                html.Append("<p class=\"empty-message\">No standalone selected matches.</p>\n");
            }
            else
            {
                html.Append("<ol class=\"entry-list\">\n");
                foreach (var entry in result.StandaloneSelectedMatches)
                {
                    AppendLifecycleEntry(html, result, entry, "standalone-selected-match");
                }

                html.Append("</ol>\n");
            }

            html.Append("</section>\n");
        }

        private static void AppendNonPathLifecycleEntries(StringBuilder html, ClashRunSequencePresentationResult result)
        {
            html.Append("<section class=\"non-path-lifecycle-section\">\n");
            html.Append("<h2>Lifecycle entries outside continuity paths</h2>\n");
            if (result.NonPathLifecycleEntries.Count == 0)
            {
                html.Append("<p class=\"empty-message\">No lifecycle entries outside continuity paths.</p>\n");
            }
            else
            {
                html.Append("<ol class=\"entry-list\">\n");
                foreach (var entry in result.NonPathLifecycleEntries)
                {
                    html.Append("<li class=\"non-path-entry\">\n");
                    AppendEntryCoordinates(html, result, entry);
                    html.Append("<p>Status: ").Append(Encode(entry.LifecycleEntry.Status.ToString())).Append("</p>\n");
                    html.Append("<p>Has selected match: ")
                        .Append(entry.LifecycleEntry.SelectedMatch == null ? "No" : "Yes")
                        .Append("</p>\n");
                    html.Append("</li>\n");
                }

                html.Append("</ol>\n");
            }

            html.Append("</section>\n");
        }

        private static void AppendTransitions(StringBuilder html, ClashRunSequencePresentationResult result)
        {
            html.Append("<section class=\"transition-sections\">\n");
            html.Append("<h2>Adjacent transitions</h2>\n");
            foreach (var transition in result.Transitions)
            {
                AppendTransition(html, result, transition);
            }

            html.Append("</section>\n");
        }

        private static void AppendTransition(
            StringBuilder html,
            ClashRunSequencePresentationResult result,
            ClashRunSequenceTransitionPresentation transition)
        {
            var comparison = transition.Comparison;
            int stillOpen = 0;
            int newCount = 0;
            int resolved = 0;
            int unverifiable = 0;

            foreach (var entry in transition.LifecycleEntries)
            {
                switch (entry.LifecycleEntry.Status)
                {
                    case ClashLifecycleStatus.StillOpen:
                        stillOpen++;
                        break;
                    case ClashLifecycleStatus.New:
                        newCount++;
                        break;
                    case ClashLifecycleStatus.Resolved:
                        resolved++;
                        break;
                    case ClashLifecycleStatus.Unverifiable:
                        unverifiable++;
                        break;
                }
            }

            html.Append("<article class=\"transition-section\">\n");
            html.Append("<h3>Transition ")
                .Append(ToOrdinal(transition.ComparisonIndex))
                .Append(" of ")
                .Append(result.AdjacentComparisonCount.ToString(CultureInfo.InvariantCulture))
                .Append("</h3>\n");
            html.Append("<p class=\"run-pair\">")
                .Append(Encode(comparison.MatchResult.PreviousRun.RunId))
                .Append(" -&gt; ")
                .Append(Encode(comparison.MatchResult.CurrentRun.RunId))
                .Append("</p>\n");
            html.Append("<dl>\n");
            AppendDefinitionRow(html, "Previous occurrences", FormatCount(comparison.MatchResult.PreviousRun.Occurrences.Count));
            AppendDefinitionRow(html, "Current occurrences", FormatCount(comparison.MatchResult.CurrentRun.Occurrences.Count));
            AppendDefinitionRow(html, "Candidates", FormatCount(comparison.MatchResult.Candidates.Count));
            AppendDefinitionRow(html, "Selected matches", FormatCount(comparison.MatchResult.SelectedMatches.Count));
            AppendDefinitionRow(html, "Alternative candidates", FormatCount(comparison.MatchResult.AlternativeCandidates.Count));
            AppendDefinitionRow(html, "StillOpen", FormatCount(stillOpen));
            AppendDefinitionRow(html, "New", FormatCount(newCount));
            AppendDefinitionRow(html, "Resolved", FormatCount(resolved));
            AppendDefinitionRow(html, "Unverifiable", FormatCount(unverifiable));
            html.Append("</dl>\n");
            html.Append("<ol class=\"entry-list\">\n");
            foreach (var entry in transition.LifecycleEntries)
            {
                AppendLifecycleEntry(html, result, entry, "transition-lifecycle-entry");
            }

            html.Append("</ol>\n");
            html.Append("</article>\n");
        }

        private static void AppendLifecycleEntry(
            StringBuilder html,
            ClashRunSequencePresentationResult result,
            ClashRunSequenceLifecycleEntryPresentation entry,
            string cssClass)
        {
            html.Append("<li class=\"").Append(cssClass).Append(" status-")
                .Append(Encode(entry.LifecycleEntry.Status.ToString().ToLowerInvariant()))
                .Append("\">\n");
            AppendEntryCoordinates(html, result, entry);
            html.Append("<p class=\"entry-status\">Status: ")
                .Append(Encode(entry.LifecycleEntry.Status.ToString()))
                .Append("</p>\n");
            AppendOccurrencePanel(html, "Previous occurrence", entry.LifecycleEntry.PreviousOccurrence);
            AppendOccurrencePanel(html, "Current occurrence", entry.LifecycleEntry.CurrentOccurrence);

            if (entry.LifecycleEntry.SelectedMatch != null)
            {
                AppendSelectedMatch(html, entry.LifecycleEntry.SelectedMatch);
            }
            else
            {
                html.Append("<p class=\"missing-selected-match\">No selected match.</p>\n");
            }

            AppendLifecycleEvidence(html, entry.LifecycleEntry.Evidence);

            if (entry.LifecycleEntry.SelectedMatch != null)
            {
                AppendMatchEvidence(html, entry.LifecycleEntry.SelectedMatch.Assessment.Evidence);
            }

            if (string.Equals(cssClass, "standalone-selected-match", StringComparison.Ordinal))
            {
                html.Append("<p class=\"standalone-note\">This selected match does not participate in any continuity link or path.</p>\n");
            }

            html.Append("</li>\n");
        }

        private static void AppendEntryCoordinates(
            StringBuilder html,
            ClashRunSequencePresentationResult result,
            ClashRunSequenceLifecycleEntryPresentation entry)
        {
            html.Append("<header class=\"entry-header\">\n");
            html.Append("<h4>Transition ")
                .Append(ToOrdinal(entry.ComparisonIndex))
                .Append(" / Entry ")
                .Append(ToOrdinal(entry.EntryIndex))
                .Append("</h4>\n");
            html.Append("<p>")
                .Append(Encode(entry.Comparison.MatchResult.PreviousRun.RunId))
                .Append(" -&gt; ")
                .Append(Encode(entry.Comparison.MatchResult.CurrentRun.RunId))
                .Append("</p>\n");
            html.Append("<p>Slots: previous")
                .Append(FormatSlot(entry.LifecycleEntry.PreviousIndex))
                .Append(" / current")
                .Append(FormatSlot(entry.LifecycleEntry.CurrentIndex))
                .Append("</p>\n");
            if (entry.ContinuityPath == null)
            {
                html.Append("<p>Continuity path: none</p>\n");
            }
            else
            {
                html.Append("<p>Continuity path: ")
                    .Append(ResolvePathOrdinal(result, entry.ContinuityPath))
                    .Append(" (presentation ordinal only)</p>\n");
            }

            html.Append("</header>\n");
        }

        private static void AppendOccurrencePanel(StringBuilder html, string label, ClashOccurrence? occurrence)
        {
            html.Append("<section class=\"occurrence-panel\">\n");
            html.Append("<h5>").Append(label).Append("</h5>\n");
            if (occurrence == null)
            {
                html.Append("<p>No ").Append(label.ToLowerInvariant()).Append(".</p>\n");
                html.Append("</section>\n");
                return;
            }

            html.Append("<dl>\n");
            AppendDefinitionRow(html, "Clash test", Encode(occurrence.ClashTestName));
            AppendDefinitionRow(html, "Clash name", Encode(occurrence.Clash.Name ?? "(unnamed clash)"));
            AppendDefinitionRow(html, "Raw clash status", Encode(occurrence.Clash.Status.ToString()));
            AppendDefinitionRow(html, "Model A", FormatModel(occurrence.ModelA));
            AppendDefinitionRow(html, "Model B", FormatModel(occurrence.ModelB));
            AppendDefinitionRow(html, "Element A", FormatElement(occurrence.Clash.ElementA));
            AppendDefinitionRow(html, "Element B", FormatElement(occurrence.Clash.ElementB));
            AppendDefinitionRow(html, "Distance", FormatDistance(occurrence.Clash.Distance));
            AppendDefinitionRow(html, "Point", FormatPoint(occurrence.Clash.Point));
            AppendDefinitionRow(html, "Source GUID (evidence only)", FormatOptionalEncoded(occurrence.Clash.Guid));
            html.Append("</dl>\n");
            html.Append("</section>\n");
        }

        private static void AppendSelectedMatch(StringBuilder html, ClashRunMatchCandidate selectedMatch)
        {
            html.Append("<section class=\"selected-match-section\">\n");
            html.Append("<h5>Selected match</h5>\n");
            html.Append("<dl>\n");
            AppendDefinitionRow(html, "Previous slot", FormatCount(selectedMatch.PreviousIndex));
            AppendDefinitionRow(html, "Current slot", FormatCount(selectedMatch.CurrentIndex));
            AppendDefinitionRow(html, "Confidence", Encode(selectedMatch.Assessment.Confidence.ToString()));
            html.Append("</dl>\n");
            html.Append("</section>\n");
        }

        private static void AppendLifecycleEvidence(
            StringBuilder html,
            System.Collections.Generic.IReadOnlyList<ClashLifecycleEvidence> evidenceItems)
        {
            html.Append("<section class=\"evidence-section lifecycle-evidence\">\n");
            html.Append("<h5>Lifecycle evidence</h5>\n");
            html.Append("<ol>\n");
            foreach (var evidence in evidenceItems)
            {
                html.Append("<li>");
                html.Append(Encode(evidence.Kind.ToString()))
                    .Append(" / ")
                    .Append(Encode(evidence.Verdict.ToString()))
                    .Append(": ")
                    .Append(Encode(evidence.Description));
                html.Append("</li>\n");
            }

            html.Append("</ol>\n");
            html.Append("</section>\n");
        }

        private static void AppendMatchEvidence(
            StringBuilder html,
            System.Collections.Generic.IReadOnlyList<MatchEvidence> evidenceItems)
        {
            html.Append("<section class=\"evidence-section match-evidence\">\n");
            html.Append("<h5>Match evidence</h5>\n");
            html.Append("<ol>\n");
            foreach (var evidence in evidenceItems)
            {
                html.Append("<li>");
                html.Append(Encode(evidence.Kind.ToString()))
                    .Append(" / ")
                    .Append(Encode(evidence.Verdict.ToString()))
                    .Append(": ")
                    .Append(Encode(evidence.Description));
                html.Append("<dl>");
                AppendDefinitionRow(html, "Previous value", FormatOptionalEncoded(evidence.PreviousValue));
                AppendDefinitionRow(html, "Current value", FormatOptionalEncoded(evidence.CurrentValue));
                html.Append("</dl>");
                html.Append("</li>\n");
            }

            html.Append("</ol>\n");
            html.Append("</section>\n");
        }

        private static void AppendClassificationNote(StringBuilder html)
        {
            html.Append("<footer class=\"longitudinal-classification-note\">\n");
            html.Append("<h2>Classification note</h2>\n");
            html.Append("<p>Results are derived from explicitly supplied runs, and the run index order is the only sequence authority.</p>\n");
            html.Append("<p>Confidence is evidence-based, not human confirmation, and source GUID is evidence only.</p>\n");
            html.Append("<p>Continuity paths are recalculable and not persisted.</p>\n");
            html.Append("<p>This report has no Reopened classification, persisted clash identifier, Clash Ledger, or aggregate multi-run lifecycle.</p>\n");
            html.Append("</footer>\n");
        }

        private static void AppendDefinitionRow(StringBuilder html, string label, string value)
        {
            html.Append("<dt>").Append(label).Append("</dt><dd>").Append(value).Append("</dd>\n");
        }

        private static string ResolvePathOrdinal(ClashRunSequencePresentationResult result, SelectedMatchContinuityPath path)
        {
            for (int i = 0; i < result.PathPresentations.Count; i++)
            {
                if (ReferenceEquals(result.PathPresentations[i].ContinuityPath, path))
                {
                    return ToOrdinal(i);
                }
            }

            throw new InvalidOperationException("Lifecycle entry references a continuity path that is not present in the presentation result.");
        }

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
                : "none";

        private static string FormatPoint(ClashPoint? point) =>
            point.HasValue
                ? string.Format(
                    CultureInfo.InvariantCulture,
                    "{0:0.000}, {1:0.000}, {2:0.000}",
                    point.Value.X,
                    point.Value.Y,
                    point.Value.Z)
                : "none";

        private static string FormatOptionalEncoded(string? value) =>
            string.IsNullOrWhiteSpace(value) ? "none" : Encode(value!);

        private static string FormatSlot(int? index) =>
            index.HasValue
                ? "[" + index.Value.ToString(CultureInfo.InvariantCulture) + "]"
                : "[-]";

        private static string FormatCount(int value) => value.ToString(CultureInfo.InvariantCulture);

        private static string ToOrdinal(int zeroBasedIndex) => (zeroBasedIndex + 1).ToString(CultureInfo.InvariantCulture);

        private static string Encode(string value) => WebUtility.HtmlEncode(value);

        private const string Css = @"
*{box-sizing:border-box;}
body{margin:0;background:#f6f5f2;color:#1d2733;font-family:system-ui,-apple-system,BlinkMacSystemFont,""Segoe UI"",sans-serif;line-height:1.45;}
body{max-width:1280px;margin:0 auto;padding:28px 20px;}
h1,h2,h3,h4,h5,p,ol,ul,dl{margin-top:0;}
h1{font-size:2rem;}
h2{font-size:1.35rem;border-bottom:2px solid #d8dee7;padding-bottom:8px;}
h3{font-size:1.1rem;}
section,article,footer,header{overflow-wrap:anywhere;}
.longitudinal-header,.interpretation-warning,.longitudinal-classification-note{background:#fff;border:1px solid #d8dee7;padding:20px;margin-bottom:22px;}
.run-flow{display:flex;flex-wrap:wrap;gap:10px;padding-left:24px;margin-bottom:0;}
.run-flow li{padding:6px 10px;background:#eef6ff;border:1px solid #bdd7f5;}
.longitudinal-summary-section,.run-sequence-section,.continuity-paths-section,.standalone-selected-matches-section,.non-path-lifecycle-section,.transition-sections{margin-bottom:24px;}
.summary-grid{display:grid;grid-template-columns:repeat(4,minmax(0,1fr));gap:12px;}
.summary-card,.run-card,.continuity-path,.transition-section,.path-selected-match,.standalone-selected-match,.transition-lifecycle-entry,.non-path-entry{background:#fff;border:1px solid #d8dee7;padding:16px;margin-bottom:14px;}
.summary-card h3{font-size:.95rem;margin-bottom:6px;color:#4b5563;}
.summary-card p{font-size:1.6rem;font-weight:800;margin:0;}
dl{display:grid;grid-template-columns:minmax(150px,240px) minmax(0,1fr);gap:6px 12px;}
dt{font-weight:700;color:#334155;}
dd{margin:0;}
.model-list li{margin-bottom:10px;}
.model-label,.model-source{display:block;}
.model-source{color:#52606d;font-size:.94rem;}
.ordinal-note,.standalone-note,.empty-message{color:#475569;font-style:italic;}
.entry-list{padding-left:22px;}
.entry-header{border-bottom:1px solid #e3e8ef;margin-bottom:12px;}
.entry-status{font-weight:700;}
.occurrence-panel,.selected-match-section,.evidence-section{border:1px solid #e3e8ef;background:#fbfcfe;padding:12px;margin:10px 0;}
.status-stillopen{border-left:6px solid #2563eb;}
.status-new{border-left:6px solid #f97316;}
.status-resolved{border-left:6px solid #15803d;}
.status-unverifiable{border-left:6px solid #64748b;}
.run-pair{font-weight:700;}
@media (max-width:860px){
  body{padding:18px 12px;}
  .summary-grid{grid-template-columns:repeat(2,minmax(0,1fr));}
  dl{grid-template-columns:1fr;}
}
@media (max-width:520px){
  .summary-grid{grid-template-columns:1fr;}
}
@media print{
  body{background:#fff;padding:0;}
  .summary-card,.run-card,.continuity-path,.transition-section,.path-selected-match,.standalone-selected-match,.transition-lifecycle-entry,.non-path-entry{break-inside:avoid;}
}
";
    }
}
