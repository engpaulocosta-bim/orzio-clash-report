using System;
using OrzioClashReport.Core.Model;
using OrzioClashReport.Core.Presentation;
using OrzioClashReport.Output.Html;

namespace OrzioClashReport.Tests
{
    /// <summary>Tests for the standalone identity-governance review HTML renderer: deterministic, self-contained, encoded output only.</summary>
    public sealed class IdentityGovernanceReviewHtmlRendererTests
    {
        [Fact]
        public void Render_EmptyDocument_ShowsEmptyStateAndSelfContainedShell()
        {
            IdentityGovernanceReviewPresentation presentation = CreatePresentation();

            string html = Sut().Render(presentation);

            Assert.StartsWith("<!doctype html>\n<html lang=\"en\">", html, StringComparison.Ordinal);
            Assert.Contains("<title>Identity Governance Review</title>", html, StringComparison.Ordinal);
            Assert.Contains("<style>", html, StringComparison.Ordinal);
            Assert.DoesNotContain("<script", html, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("http://", html, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("https://", html, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("No human identity decisions have been recorded.", html, StringComparison.Ordinal);
        }

        [Fact]
        public void Render_ConfirmationAndRejection_PreserveOrderAndTextualLabels()
        {
            IdentityGovernanceReviewPresentation presentation = CreatePresentation(
                IdentityGovernanceReviewTestData.Confirm("decision-001", "run-001", 0, "run-002", 1),
                IdentityGovernanceReviewTestData.Reject("decision-002", "run-002", 2, "run-003", 3));

            string html = Sut().Render(presentation);

            int firstDecision = html.IndexOf("decision-001", StringComparison.Ordinal);
            int secondDecision = html.IndexOf("decision-002", StringComparison.Ordinal);

            Assert.True(firstDecision >= 0);
            Assert.True(secondDecision > firstDecision);
            Assert.Contains("Confirmation", html, StringComparison.Ordinal);
            Assert.Contains("ConfirmSameIdentity", html, StringComparison.Ordinal);
            Assert.Contains("Rejection", html, StringComparison.Ordinal);
            Assert.Contains("RejectSameIdentity", html, StringComparison.Ordinal);
            Assert.Contains(">Left endpoint<", html, StringComparison.Ordinal);
            Assert.Contains(">Right endpoint<", html, StringComparison.Ordinal);
        }

        [Fact]
        public void Render_EncodesDynamicContentAndUsesNeutralOptionalText()
        {
            IdentityGovernanceReviewPresentation presentation = CreatePresentation(
                IdentityGovernanceReviewTestData.Confirm(
                    "decision-<one>",
                    "run-001",
                    1,
                    "run-003",
                    2,
                    reviewerAlias: "<img src=x onerror=alert(1)>",
                    reason: "<script>alert('x')</script> & \" '"));

            string html = Sut().Render(presentation);

            Assert.Contains("decision-&lt;one&gt;", html, StringComparison.Ordinal);
            Assert.Contains("&lt;img src=x onerror=alert(1)&gt;", html, StringComparison.Ordinal);
            Assert.Contains("&lt;script&gt;alert(&#39;x&#39;)&lt;/script&gt; &amp; &quot; &#39;", html, StringComparison.Ordinal);
            Assert.DoesNotContain("<script>alert('x')</script>", html, StringComparison.Ordinal);
            Assert.DoesNotContain("<img src=x onerror=alert(1)>", html, StringComparison.Ordinal);
            Assert.Contains(">Not provided<", html, StringComparison.Ordinal);
        }

        [Fact]
        public void Render_PrivateSourceModelPaths_AreExcludedWhileSafeFieldsRemainVisible()
        {
            IdentityGovernanceReviewPresentation presentation = CreatePresentation(
                IdentityGovernanceReviewTestData.CreateRunsWithPrivateSourceModels(),
                IdentityGovernanceReviewTestData.Confirm("decision-001", "run-001", 0, "run-003", 2));

            string html = Sut().Render(presentation);

            Assert.DoesNotContain("C:\\Clients", html, StringComparison.Ordinal);
            Assert.DoesNotContain("\\\\fileserver", html, StringComparison.Ordinal);
            Assert.DoesNotContain("/srv/customer", html, StringComparison.Ordinal);
            Assert.DoesNotContain("Model-A.nwc", html, StringComparison.Ordinal);
            Assert.DoesNotContain("Element A source model", html, StringComparison.Ordinal);
            Assert.DoesNotContain("Element B source model", html, StringComparison.Ordinal);
            Assert.Contains("Sigma A / Structure / Core A @ RA1", html, StringComparison.Ordinal);
            Assert.Contains("Alfa A / HVAC / Services A @ RA2", html, StringComparison.Ordinal);
            Assert.Contains("A-001", html, StringComparison.Ordinal);
            Assert.Contains("Duct 01", html, StringComparison.Ordinal);
            Assert.Contains("Level 01", html, StringComparison.Ordinal);
        }

        [Fact]
        public void Render_ConfirmationOnly_ShowsPersistentIdentityIdAndEscapesValue()
        {
            IdentityGovernanceReviewPresentation presentation = CreatePresentation(
                IdentityGovernanceReviewTestData.Confirm(
                    "decision-001",
                    "run-001",
                    0,
                    "run-003",
                    1,
                    persistentIdentityId: "identity-<one>&\"'"));

            string html = Sut().Render(presentation);

            Assert.Contains("Persistent identity id", html, StringComparison.Ordinal);
            Assert.Contains("identity-&lt;one&gt;&amp;&quot;&#39;", html, StringComparison.Ordinal);
        }

        [Fact]
        public void Render_RejectionOnly_OmitsPersistentIdentityIdAndPreservesNotProvidedReason()
        {
            IdentityGovernanceReviewPresentation presentation = CreatePresentation(
                IdentityGovernanceReviewTestData.Reject("decision-001", "run-001", 0, "run-003", 1));

            string html = Sut().Render(presentation);

            Assert.DoesNotContain("Persistent identity id", html, StringComparison.Ordinal);
            Assert.Contains(">Not provided<", html, StringComparison.Ordinal);
        }

        [Fact]
        public void Render_IsDeterministicAndLfOnly()
        {
            IdentityGovernanceReviewPresentation presentation = CreatePresentation(
                IdentityGovernanceReviewTestData.Confirm("decision-001", "run-001", 0, "run-003", 1));

            string first = Sut().Render(presentation);
            string second = Sut().Render(presentation);

            Assert.Equal(first, second, StringComparer.Ordinal);
            Assert.DoesNotContain("\r", first, StringComparison.Ordinal);
            Assert.EndsWith("\n", first, StringComparison.Ordinal);
            Assert.DoesNotContain("generated-at", first, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("username", first, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("C:\\", first, StringComparison.OrdinalIgnoreCase);
        }

        private static IdentityGovernanceReviewPresentation CreatePresentation(params HumanIdentityDecision[] decisions)
        {
            return CreatePresentation(IdentityGovernanceReviewTestData.CreateRuns(), decisions);
        }

        private static IdentityGovernanceReviewPresentation CreatePresentation(
            System.Collections.Generic.IReadOnlyList<CoordinationRun> runs,
            params HumanIdentityDecision[] decisions)
        {
            var governance = IdentityGovernanceReviewTestData.CreateGovernance(decisions);
            return new DeterministicIdentityGovernanceReviewPresenter().Present(
                "coordination-project",
                "Coordination <Project> & Review",
                governance,
                runs);
        }

        private static IdentityGovernanceReviewHtmlRenderer Sut() => new IdentityGovernanceReviewHtmlRenderer();
    }
}
