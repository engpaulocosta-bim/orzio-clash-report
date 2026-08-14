using System;
using System.Collections.Generic;
using System.Globalization;
using System.Net;
using System.Text;
using OrzioClashReport.Core.Model;

namespace OrzioClashReport.Output.Html
{
    /// <summary>
    /// Adds a deterministic human-confirmed identity section to the existing longitudinal report.
    /// Confirmations are presentation-only and do not modify matching, lifecycle, continuity, or snapshots.
    /// </summary>
    public sealed class GovernedLongitudinalClashReportRenderer
    {
        private const string BodyClose = "</body>\n</html>\n";
        private readonly HtmlLongitudinalClashReportRenderer baseRenderer = new HtmlLongitudinalClashReportRenderer();

        public string Render(
            ClashRunSequencePresentationResult result,
            IReadOnlyList<LongitudinalIdentityConfirmation> confirmations)
        {
            if (result == null)
            {
                throw new ArgumentNullException(nameof(result));
            }

            if (confirmations == null)
            {
                throw new ArgumentNullException(nameof(confirmations));
            }

            string baseHtml = baseRenderer.Render(result);
            int insertionIndex = baseHtml.LastIndexOf(BodyClose, StringComparison.Ordinal);
            if (insertionIndex < 0)
            {
                throw new InvalidOperationException("Longitudinal renderer output does not contain the expected document ending.");
            }

            string section = BuildConfirmationSection(confirmations);
            return baseHtml.Substring(0, insertionIndex) + section + baseHtml.Substring(insertionIndex);
        }

        private static string BuildConfirmationSection(IReadOnlyList<LongitudinalIdentityConfirmation> confirmations)
        {
            var html = new StringBuilder();
            html.Append("<section class=\"human-confirmed-identities-section\">\n");
            html.Append("<h2>Human-confirmed persistent identities</h2>\n");
            html.Append("<p>Only explicit ConfirmSameIdentity decisions are shown. Each row applies only to its two recorded evidence endpoints and does not imply transitivity or automatic identity propagation.</p>\n");

            if (confirmations.Count == 0)
            {
                html.Append("<p class=\"empty-message\">No explicit human identity confirmations.</p>\n");
                html.Append("</section>\n");
                return html.ToString();
            }

            html.Append("<ol class=\"identity-confirmation-list\">\n");
            for (int index = 0; index < confirmations.Count; index++)
            {
                LongitudinalIdentityConfirmation confirmation = confirmations[index];
                html.Append("<li class=\"identity-confirmation\">\n");
                html.Append("<h3>Confirmation ")
                    .Append((index + 1).ToString(CultureInfo.InvariantCulture))
                    .Append("</h3>\n");
                html.Append("<dl>\n");
                AppendRow(html, "Persistent identity id", confirmation.PersistentIdentityId);
                AppendRow(html, "Decision id", confirmation.DecisionId);
                AppendRow(html, "Left evidence", FormatEndpoint(confirmation.LeftEvidence));
                AppendRow(html, "Right evidence", FormatEndpoint(confirmation.RightEvidence));
                AppendRow(html, "Reviewer alias", confirmation.ReviewerAlias);
                AppendRow(html, "Reason", confirmation.Reason ?? "Not provided");
                html.Append("</dl>\n");
                html.Append("</li>\n");
            }

            html.Append("</ol>\n");
            html.Append("</section>\n");
            return html.ToString();
        }

        private static string FormatEndpoint(ClashEvidenceEndpoint endpoint) =>
            $"{endpoint.RunId} occurrence {endpoint.OccurrenceIndex.ToString(CultureInfo.InvariantCulture)}";

        private static void AppendRow(StringBuilder html, string label, string value)
        {
            html.Append("<dt>").Append(Encode(label)).Append("</dt><dd>")
                .Append(Encode(value)).Append("</dd>\n");
        }

        private static string Encode(string value) => WebUtility.HtmlEncode(value ?? string.Empty);
    }
}
