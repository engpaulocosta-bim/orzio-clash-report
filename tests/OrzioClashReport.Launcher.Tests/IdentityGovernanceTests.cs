using System;
using System.Collections.Generic;
using System.Linq;
using OrzioClashReport.Launcher.Application.Engine;
using OrzioClashReport.Launcher.Application.Operations;
using OrzioClashReport.Launcher.Application.Presentation;
using OrzioClashReport.Launcher.Contracts.Operations;

namespace OrzioClashReport.Launcher.Tests
{
    public sealed class IdentityGovernanceTests
    {
        [Fact]
        public void CreateIdentityGovernance()
        {
            string output = TestPaths.Absolute("project/identity-governance.json");

            Assert.Equal(
                new[]
                {
                    "create-identity-governance",
                    "--project-id", "tower-a",
                    "-o", output,
                },
                EngineArgumentBuilder.CreateIdentityGovernance("tower-a", output));
        }

        [Fact]
        public void AConfirmationCarriesThePersistentIdentityIdAfterTheReviewerAlias()
        {
            IReadOnlyList<string> arguments = EngineArgumentBuilder.AppendIdentityDecision(
                "/project/identity-governance.json",
                Confirm());

            Assert.Equal(
                new[]
                {
                    "append-identity-decision",
                    "--governance", "/project/identity-governance.json",
                    "--decision-id", "d-001",
                    "--decision-kind", "ConfirmSameIdentity",
                    "--left-run-id", "run-001",
                    "--left-occurrence-index", "0",
                    "--right-run-id", "run-002",
                    "--right-occurrence-index", "7",
                    "--reviewer-alias", "coordenador-a",
                    "--persistent-identity-id", "clash-42",
                },
                arguments);
        }

        [Fact]
        public void ARejectionNeverCarriesAPersistentIdentityId()
        {
            IReadOnlyList<string> arguments = EngineArgumentBuilder.AppendIdentityDecision(
                "/project/identity-governance.json",
                Reject());

            Assert.Equal(
                new[]
                {
                    "append-identity-decision",
                    "--governance", "/project/identity-governance.json",
                    "--decision-id", "d-002",
                    "--decision-kind", "RejectSameIdentity",
                    "--left-run-id", "run-001",
                    "--left-occurrence-index", "3",
                    "--right-run-id", "run-002",
                    "--right-occurrence-index", "4",
                    "--reviewer-alias", "coordenador-a",
                },
                arguments);

            Assert.DoesNotContain("--persistent-identity-id", arguments);
        }

        [Fact]
        public void AReasonIsOptionalAndComesLast()
        {
            IReadOnlyList<string> withReason = EngineArgumentBuilder.AppendIdentityDecision(
                "/g.json",
                new IdentityDecisionDraft(
                    IdentityDecisionKind.RejectSameIdentity,
                    "d-003", "run-001", 0, "run-002", 1, "alias", null, "elementos diferentes"));

            Assert.Equal("--reason", withReason[withReason.Count - 2]);
            Assert.Equal("elementos diferentes", withReason[withReason.Count - 1]);

            IReadOnlyList<string> withoutReason = EngineArgumentBuilder.AppendIdentityDecision("/g.json", Reject());
            Assert.DoesNotContain("--reason", withoutReason);
        }

        [Fact]
        public void AConfirmationWithoutAPersistentIdentityIdCannotBeConstructed()
        {
            Assert.Throws<ArgumentException>(() => new IdentityDecisionDraft(
                IdentityDecisionKind.ConfirmSameIdentity,
                "d-001", "run-001", 0, "run-002", 1, "alias", persistentIdentityId: null, reason: null));

            Assert.Throws<ArgumentException>(() => new IdentityDecisionDraft(
                IdentityDecisionKind.ConfirmSameIdentity,
                "d-001", "run-001", 0, "run-002", 1, "alias", persistentIdentityId: "   ", reason: null));
        }

        [Fact]
        public void ARejectionWithAPersistentIdentityIdCannotBeConstructed()
        {
            Assert.Throws<ArgumentException>(() => new IdentityDecisionDraft(
                IdentityDecisionKind.RejectSameIdentity,
                "d-001", "run-001", 0, "run-002", 1, "alias", persistentIdentityId: "clash-42", reason: null));
        }

        [Fact]
        public void OccurrenceIndexesAreZeroBasedAndNeverNegative()
        {
            Assert.True(IdentityDecisionDraft.TryParseOccurrenceIndex("0", out int zero));
            Assert.Equal(0, zero);

            Assert.True(IdentityDecisionDraft.TryParseOccurrenceIndex(" 12 ", out int twelve));
            Assert.Equal(12, twelve);

            Assert.False(IdentityDecisionDraft.TryParseOccurrenceIndex("-1", out _));
            Assert.False(IdentityDecisionDraft.TryParseOccurrenceIndex("1.5", out _));
            Assert.False(IdentityDecisionDraft.TryParseOccurrenceIndex("abc", out _));
            Assert.False(IdentityDecisionDraft.TryParseOccurrenceIndex(string.Empty, out _));
            Assert.False(IdentityDecisionDraft.TryParseOccurrenceIndex(null, out _));

            Assert.Throws<ArgumentOutOfRangeException>(() => new IdentityDecisionDraft(
                IdentityDecisionKind.RejectSameIdentity,
                "d-001", "run-001", -1, "run-002", 1, "alias", null, null));
        }

        [Fact]
        public void EveryIdentifyingFieldIsRequired()
        {
            Assert.Throws<ArgumentException>(() => Draft(decisionId: " "));
            Assert.Throws<ArgumentException>(() => Draft(leftRunId: " "));
            Assert.Throws<ArgumentException>(() => Draft(rightRunId: " "));
            Assert.Throws<ArgumentException>(() => Draft(reviewerAlias: " "));
        }

        [Fact]
        public void ValidateIdentityGovernance()
        {
            Assert.Equal(
                new[]
                {
                    "validate-identity-governance",
                    "--project", "/project/project.json",
                    "--governance", "/project/identity-governance.json",
                },
                EngineArgumentBuilder.ValidateIdentityGovernance(
                    "/project/project.json", "/project/identity-governance.json"));
        }

        [Fact]
        public void RenderIdentityGovernanceReport()
        {
            string output = TestPaths.Absolute("project/identity-governance.html");

            Assert.Equal(
                new[]
                {
                    "render-identity-governance-report",
                    "--project", "/project/project.json",
                    "--governance", "/project/identity-governance.json",
                    "-o", output,
                },
                EngineArgumentBuilder.RenderIdentityGovernanceReport(
                    "/project/project.json",
                    "/project/identity-governance.json",
                    output));
        }

        [Fact]
        public void TheCanonicalDecisionValuesAreNeverTranslated()
        {
            Assert.Equal("ConfirmSameIdentity", IdentityDecisionKind.ConfirmSameIdentity.ToString());
            Assert.Equal("RejectSameIdentity", IdentityDecisionKind.RejectSameIdentity.ToString());

            foreach (IdentityDecisionKind kind in Enum.GetValues<IdentityDecisionKind>())
            {
                Assert.Equal(kind.ToString(), IdentityDecisionPresentation.For(kind).CanonicalValue);
            }
        }

        [Fact]
        public void ConfirmAndRejectDifferByGlyphLabelAndSeverityNotByColourAlone()
        {
            IdentityDecisionPresentation confirm =
                IdentityDecisionPresentation.For(IdentityDecisionKind.ConfirmSameIdentity);
            IdentityDecisionPresentation reject =
                IdentityDecisionPresentation.For(IdentityDecisionKind.RejectSameIdentity);

            Assert.NotEqual(confirm.Glyph, reject.Glyph);
            Assert.NotEqual(confirm.Label, reject.Label);
            Assert.NotEqual(confirm.Severity, reject.Severity);

            Assert.True(confirm.RequiresPersistentIdentityId);
            Assert.False(reject.RequiresPersistentIdentityId);

            // The visible label is localised; the value sent to the engine is not.
            Assert.DoesNotContain(confirm.CanonicalValue, confirm.Label, StringComparison.Ordinal);
        }

        [Fact]
        public void TheLabelsAreDistinctAcrossEveryDecisionKind()
        {
            var labels = Enum.GetValues<IdentityDecisionKind>()
                .Select(kind => IdentityDecisionPresentation.For(kind).Label)
                .ToList();

            Assert.Equal(labels.Count, labels.Distinct(StringComparer.Ordinal).Count());
        }

        private static IdentityDecisionDraft Confirm() =>
            new IdentityDecisionDraft(
                IdentityDecisionKind.ConfirmSameIdentity,
                "d-001", "run-001", 0, "run-002", 7, "coordenador-a", "clash-42", null);

        private static IdentityDecisionDraft Reject() =>
            new IdentityDecisionDraft(
                IdentityDecisionKind.RejectSameIdentity,
                "d-002", "run-001", 3, "run-002", 4, "coordenador-a", null, null);

        private static IdentityDecisionDraft Draft(
            string decisionId = "d-001",
            string leftRunId = "run-001",
            string rightRunId = "run-002",
            string reviewerAlias = "alias") =>
            new IdentityDecisionDraft(
                IdentityDecisionKind.RejectSameIdentity,
                decisionId, leftRunId, 0, rightRunId, 1, reviewerAlias, null, null);
    }
}
