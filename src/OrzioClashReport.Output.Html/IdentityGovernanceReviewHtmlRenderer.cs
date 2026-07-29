using System;
using System.Globalization;
using System.Net;
using System.Text;
using OrzioClashReport.Core.Governance;
using OrzioClashReport.Core.Model;

namespace OrzioClashReport.Output.Html
{
    /// <summary>Renders a deterministic, self-contained HTML review of explicit human identity-governance decisions.</summary>
    public sealed class IdentityGovernanceReviewHtmlRenderer
    {
        private static readonly string NormalizedCss = DeterministicHtmlLineEndings.NormalizeTemplateLiteral(Css);

        public string Render(IdentityGovernanceReviewPresentation presentation)
        {
            if (presentation == null)
            {
                throw new ArgumentNullException(nameof(presentation));
            }

            var html = new StringBuilder();
            html.Append("<!doctype html>\n<html lang=\"en\">\n<head>\n");
            html.Append("<meta charset=\"utf-8\">\n");
            html.Append("<title>Identity Governance Review</title>\n");
            html.Append("<style>").Append(NormalizedCss).Append("</style>\n");
            html.Append("</head>\n<body>\n");

            AppendHeader(html, presentation);
            AppendSummary(html, presentation);
            AppendNotes(html);
            AppendDecisions(html, presentation);

            html.Append("</body>\n</html>\n");
            return html.ToString();
        }

        private static void AppendHeader(StringBuilder html, IdentityGovernanceReviewPresentation presentation)
        {
            html.Append("<header class=\"page-header\">\n");
            html.Append("<p class=\"eyebrow\">Standalone source-only review</p>\n");
            html.Append("<h1>Identity Governance Review</h1>\n");
            html.Append("<dl class=\"project-metadata\">\n");
            AppendDefinitionRow(html, "Project name", Encode(presentation.ProjectDisplayName));
            AppendDefinitionRow(html, "Project id", Encode(presentation.ProjectId));
            AppendDefinitionRow(html, "Indexed runs", FormatCount(presentation.IndexedRunCount));
            AppendDefinitionRow(html, "Decisions", FormatCount(presentation.DecisionCount));
            AppendDefinitionRow(html, "Confirmations", FormatCount(presentation.ConfirmationCount));
            AppendDefinitionRow(html, "Rejections", FormatCount(presentation.RejectionCount));
            AppendDefinitionRow(html, "Evidence endpoints", FormatCount(presentation.EvidenceEndpointCount));
            html.Append("</dl>\n");
            html.Append("</header>\n");
        }

        private static void AppendSummary(StringBuilder html, IdentityGovernanceReviewPresentation presentation)
        {
            html.Append("<section class=\"summary-section\">\n");
            html.Append("<h2>Operational summary</h2>\n");
            html.Append("<div class=\"summary-grid\">\n");
            AppendSummaryCard(html, "Indexed runs", presentation.IndexedRunCount);
            AppendSummaryCard(html, "Decisions", presentation.DecisionCount);
            AppendSummaryCard(html, "Confirmations", presentation.ConfirmationCount);
            AppendSummaryCard(html, "Rejections", presentation.RejectionCount);
            AppendSummaryCard(html, "Evidence endpoints", presentation.EvidenceEndpointCount);
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

        private static void AppendNotes(StringBuilder html)
        {
            html.Append("<section class=\"note-section\">\n");
            html.Append("<h2>Interpretation notes</h2>\n");
            html.Append("<ul>\n");
            html.Append("<li>This report is a derived, regenerable review artifact. It is not primary evidence.</li>\n");
            html.Append("<li>Snapshots remain immutable evidence, and human governance remains explicit source state.</li>\n");
            html.Append("<li>This report does not infer identity, build a graph, implement transitivity, Clash Ledger, or Reopened.</li>\n");
            html.Append("<li>Separate decisions are shown exactly as persisted and are never grouped by persistent identity.</li>\n");
            html.Append("</ul>\n");
            html.Append("</section>\n");
        }

        private static void AppendDecisions(StringBuilder html, IdentityGovernanceReviewPresentation presentation)
        {
            html.Append("<main class=\"decision-list\">\n");
            if (presentation.Decisions.Count == 0)
            {
                html.Append("<section class=\"empty-state\">\n");
                html.Append("<h2>No decisions</h2>\n");
                html.Append("<p>No human identity decisions have been recorded.</p>\n");
                html.Append("</section>\n");
                html.Append("</main>\n");
                return;
            }

            foreach (var decision in presentation.Decisions)
            {
                AppendDecision(html, decision);
            }

            html.Append("</main>\n");
        }

        private static void AppendDecision(StringBuilder html, IdentityGovernanceReviewDecisionPresentation decision)
        {
            string kindClass = decision.DecisionKind == HumanIdentityDecisionKind.ConfirmSameIdentity
                ? "decision-confirm"
                : "decision-reject";

            html.Append("<section class=\"decision-card ").Append(kindClass).Append("\">\n");
            html.Append("<header class=\"decision-header\">\n");
            html.Append("<div>\n");
            html.Append("<p class=\"decision-number\">Decision ")
                .Append(decision.DecisionNumber.ToString(CultureInfo.InvariantCulture))
                .Append("</p>\n");
            html.Append("<h2>").Append(Encode(decision.DecisionId)).Append("</h2>\n");
            html.Append("</div>\n");
            html.Append("<p class=\"decision-kind-label\">")
                .Append(Encode(GetDecisionLabel(decision.DecisionKind)))
                .Append(" <span class=\"decision-kind-code\">")
                .Append(Encode(decision.DecisionKind.ToString()))
                .Append("</span></p>\n");
            html.Append("</header>\n");

            html.Append("<dl class=\"decision-metadata\">\n");
            AppendDefinitionRow(html, "Decision kind", Encode(decision.DecisionKind.ToString()));
            AppendDefinitionRow(html, "Reviewer alias", Encode(decision.ReviewerAlias));
            AppendDefinitionRow(html, "Reason", FormatOptionalEncoded(decision.Reason));
            AppendDefinitionRow(html, "Persistent identity id", FormatOptionalEncoded(decision.PersistentIdentityId));
            html.Append("</dl>\n");

            html.Append("<div class=\"endpoint-grid\">\n");
            AppendEndpoint(html, decision.LeftEndpoint);
            AppendEndpoint(html, decision.RightEndpoint);
            html.Append("</div>\n");
            html.Append("</section>\n");
        }

        private static void AppendEndpoint(StringBuilder html, IdentityGovernanceReviewEndpointPresentation endpoint)
        {
            html.Append("<article class=\"endpoint-card endpoint-")
                .Append(endpoint.Side == IdentityGovernanceEvidenceEndpointSide.Left ? "left" : "right")
                .Append("\">\n");
            html.Append("<h3>").Append(Encode(endpoint.Side.ToString())).Append(" endpoint</h3>\n");
            html.Append("<dl class=\"endpoint-metadata\">\n");
            AppendDefinitionRow(html, "Run id", Encode(endpoint.RunId));
            AppendDefinitionRow(html, "Occurrence index", FormatCount(endpoint.OccurrenceIndex));
            AppendDefinitionRow(html, "Run created at", Encode(endpoint.RunCreatedAt.ToString("O", CultureInfo.InvariantCulture)));
            AppendDefinitionRow(html, "Clash test", Encode(endpoint.ClashTestName));
            AppendDefinitionRow(html, "Clash status", Encode(endpoint.ClashStatus.ToString()));
            AppendDefinitionRow(html, "Model A", Encode(endpoint.ModelADisplay));
            AppendDefinitionRow(html, "Model B", Encode(endpoint.ModelBDisplay));
            AppendDefinitionRow(html, "Element A id", Encode(endpoint.ElementAId));
            AppendDefinitionRow(html, "Element A name", FormatOptionalEncoded(endpoint.ElementAName));
            AppendDefinitionRow(html, "Element A level", FormatOptionalEncoded(endpoint.ElementALevel));
            AppendDefinitionRow(html, "Element A source model", FormatOptionalEncoded(endpoint.ElementASourceModel));
            AppendDefinitionRow(html, "Element B id", Encode(endpoint.ElementBId));
            AppendDefinitionRow(html, "Element B name", FormatOptionalEncoded(endpoint.ElementBName));
            AppendDefinitionRow(html, "Element B level", FormatOptionalEncoded(endpoint.ElementBLevel));
            AppendDefinitionRow(html, "Element B source model", FormatOptionalEncoded(endpoint.ElementBSourceModel));
            html.Append("</dl>\n");
            html.Append("</article>\n");
        }

        private static string GetDecisionLabel(HumanIdentityDecisionKind kind)
        {
            switch (kind)
            {
                case HumanIdentityDecisionKind.ConfirmSameIdentity:
                    return "Confirmation";
                case HumanIdentityDecisionKind.RejectSameIdentity:
                    return "Rejection";
                default:
                    throw new ArgumentOutOfRangeException(nameof(kind), kind, "Unsupported human identity decision kind.");
            }
        }

        private static void AppendDefinitionRow(StringBuilder html, string label, string value)
        {
            html.Append("<dt>").Append(label).Append("</dt><dd>").Append(value).Append("</dd>\n");
        }

        private static string FormatOptionalEncoded(string? value) =>
            string.IsNullOrWhiteSpace(value) ? "Not provided" : Encode(value!);

        private static string FormatCount(int value) => value.ToString(CultureInfo.InvariantCulture);

        private static string Encode(string value) => WebUtility.HtmlEncode(value);

        private const string Css = @"
*{box-sizing:border-box;}
body{margin:0;background:#f4f1ea;color:#1f2933;font-family:system-ui,-apple-system,BlinkMacSystemFont,""Segoe UI"",sans-serif;line-height:1.5;}
body{max-width:1280px;margin:0 auto;padding:28px 20px;}
h1,h2,h3,p,ul,dl{margin-top:0;}
section,article,header,main{overflow-wrap:anywhere;}
.page-header{background:linear-gradient(135deg,#fff6d8,#fefbf3);border:1px solid #dccb9b;border-radius:22px;padding:24px;margin-bottom:24px;box-shadow:0 12px 28px rgba(79,61,28,.08);}
.eyebrow{margin-bottom:8px;font-size:.82rem;font-weight:800;letter-spacing:.08em;text-transform:uppercase;color:#8a6b32;}
.page-header h1{margin-bottom:18px;font-size:2rem;}
.project-metadata,.decision-metadata,.endpoint-metadata{display:grid;grid-template-columns:minmax(150px,210px) minmax(0,1fr);gap:6px 12px;}
.project-metadata dt,.decision-metadata dt,.endpoint-metadata dt{font-weight:700;color:#334155;}
.project-metadata dd,.decision-metadata dd,.endpoint-metadata dd{margin:0;}
.summary-section,.note-section{margin-bottom:24px;}
.summary-grid{display:grid;grid-template-columns:repeat(5,minmax(0,1fr));gap:14px;}
.summary-card,.note-section,.decision-card,.endpoint-card,.empty-state{background:#fff;border:1px solid #d7dde5;border-radius:18px;box-shadow:0 10px 24px rgba(15,23,42,.05);}
.summary-card{padding:18px;text-align:center;}
.summary-card h3{font-size:.98rem;margin-bottom:8px;color:#52606d;}
.summary-card p{margin:0;font-size:1.9rem;font-weight:800;}
.note-section{padding:20px;}
.note-section ul{margin-bottom:0;padding-left:20px;}
.decision-list{display:grid;gap:20px;}
.decision-card{padding:20px;border-top:6px solid #64748b;}
.decision-confirm{border-top-color:#15803d;}
.decision-reject{border-top-color:#b45309;}
.decision-header{display:flex;justify-content:space-between;gap:16px;align-items:flex-start;margin-bottom:16px;}
.decision-number{margin-bottom:6px;font-size:.85rem;font-weight:800;letter-spacing:.04em;text-transform:uppercase;color:#52606d;}
.decision-header h2{margin-bottom:0;font-size:1.35rem;}
.decision-kind-label{margin:0;display:inline-flex;align-items:center;gap:10px;padding:8px 12px;border-radius:999px;font-weight:800;background:#f8fafc;border:1px solid #d7dde5;text-transform:uppercase;font-size:.85rem;letter-spacing:.03em;}
.decision-kind-code{font-family:Consolas,""SFMono-Regular"",Menlo,monospace;font-size:.8rem;text-transform:none;letter-spacing:0;}
.decision-metadata{margin-bottom:18px;}
.endpoint-grid{display:grid;grid-template-columns:repeat(2,minmax(0,1fr));gap:16px;}
.endpoint-card{padding:16px;background:#fcfcfd;}
.endpoint-left{background:linear-gradient(180deg,#f8fbff,#fcfcfd);}
.endpoint-right{background:linear-gradient(180deg,#fff8ef,#fcfcfd);}
.endpoint-card h3{margin-bottom:12px;}
.empty-state{padding:24px;}
.empty-state h2{margin-bottom:8px;}
@media (max-width:980px){
  body{padding:20px 16px;}
  .summary-grid{grid-template-columns:repeat(2,minmax(0,1fr));}
  .endpoint-grid{grid-template-columns:1fr;}
  .decision-header{flex-direction:column;}
}
@media (max-width:560px){
  .summary-grid{grid-template-columns:1fr;}
  .project-metadata,.decision-metadata,.endpoint-metadata{grid-template-columns:1fr;}
}
@media print{
  body{background:#fff;padding:0;}
  .page-header,.summary-card,.note-section,.decision-card,.endpoint-card,.empty-state{box-shadow:none;}
  .page-header,.decision-card,.endpoint-card,.empty-state{break-inside:avoid;}
}
";
    }
}
