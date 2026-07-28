using System;
using OrzioClashReport.Core.Model;

namespace OrzioClashReport.Tests
{
    public sealed class HumanIdentityDecisionTests
    {
        [Fact]
        public void ConfirmSameIdentity_WithValidFields_IsConstructed()
        {
            var decision = new HumanIdentityDecision(
                "decision-001",
                "coordination-project",
                HumanIdentityDecisionKind.ConfirmSameIdentity,
                Evidence("run-001", 1),
                Evidence("run-002", 2),
                "identity-001",
                "coordinator-a",
                "Confirmed from model context");

            Assert.Equal("decision-001", decision.DecisionId);
            Assert.Equal("coordination-project", decision.ProjectId);
            Assert.Equal(HumanIdentityDecisionKind.ConfirmSameIdentity, decision.DecisionKind);
            Assert.Equal("identity-001", decision.PersistentIdentityId);
            Assert.Equal("coordinator-a", decision.ReviewerAlias);
            Assert.Equal("Confirmed from model context", decision.Reason);
        }

        [Fact]
        public void RejectSameIdentity_WithValidFields_IsConstructedWithoutPersistentIdentity()
        {
            var decision = new HumanIdentityDecision(
                "decision-002",
                "coordination-project",
                HumanIdentityDecisionKind.RejectSameIdentity,
                Evidence("run-001", 3),
                Evidence("run-002", 4),
                null,
                "coordinator-a",
                null);

            Assert.Equal(HumanIdentityDecisionKind.RejectSameIdentity, decision.DecisionKind);
            Assert.Null(decision.PersistentIdentityId);
        }

        [Fact]
        public void ConfirmSameIdentity_WithoutPersistentIdentityId_Fails()
        {
            var ex = Assert.Throws<ArgumentException>(
                () => new HumanIdentityDecision(
                    "decision-001",
                    "coordination-project",
                    HumanIdentityDecisionKind.ConfirmSameIdentity,
                    Evidence("run-001", 1),
                    Evidence("run-002", 2),
                    null,
                    "coordinator-a",
                    null));

            Assert.Contains("requires a persistent identity id", ex.Message, StringComparison.Ordinal);
        }

        [Fact]
        public void RejectSameIdentity_WithPersistentIdentityId_Fails()
        {
            var ex = Assert.Throws<ArgumentException>(
                () => new HumanIdentityDecision(
                    "decision-001",
                    "coordination-project",
                    HumanIdentityDecisionKind.RejectSameIdentity,
                    Evidence("run-001", 1),
                    Evidence("run-002", 2),
                    "identity-001",
                    "coordinator-a",
                    null));

            Assert.Contains("cannot carry a persistent identity id", ex.Message, StringComparison.Ordinal);
        }

        [Fact]
        public void SameEvidenceEndpoints_Fail()
        {
            ClashEvidenceEndpoint evidence = Evidence("run-001", 1);

            var ex = Assert.Throws<ArgumentException>(
                () => new HumanIdentityDecision(
                    "decision-001",
                    "coordination-project",
                    HumanIdentityDecisionKind.RejectSameIdentity,
                    evidence,
                    Evidence("run-001", 1),
                    null,
                    "coordinator-a",
                    null));

            Assert.Contains("must be different", ex.Message, StringComparison.Ordinal);
        }

        [Fact]
        public void EmptyIdentifiers_Fail()
        {
            Assert.Throws<ArgumentException>(() => new ClashEvidenceEndpoint("   ", 1));

            Assert.Throws<ArgumentException>(
                () => new HumanIdentityDecision(
                    "   ",
                    "coordination-project",
                    HumanIdentityDecisionKind.RejectSameIdentity,
                    Evidence("run-001", 1),
                    Evidence("run-002", 2),
                    null,
                    "coordinator-a",
                    null));
        }

        [Fact]
        public void EmptyReviewerAlias_Fails()
        {
            Assert.Throws<ArgumentException>(
                () => new HumanIdentityDecision(
                    "decision-001",
                    "coordination-project",
                    HumanIdentityDecisionKind.RejectSameIdentity,
                    Evidence("run-001", 1),
                    Evidence("run-002", 2),
                    null,
                    "   ",
                    null));
        }

        [Fact]
        public void ReasonAboveLimit_Fails()
        {
            string tooLongReason = new string('r', HumanIdentityDecision.MaxReasonLength + 1);

            Assert.Throws<ArgumentException>(
                () => new HumanIdentityDecision(
                    "decision-001",
                    "coordination-project",
                    HumanIdentityDecisionKind.RejectSameIdentity,
                    Evidence("run-001", 1),
                    Evidence("run-002", 2),
                    null,
                    "coordinator-a",
                    tooLongReason));
        }

        [Fact]
        public void LeftAndRightEvidence_ArePreservedWithoutReordering()
        {
            var left = Evidence("run-z", 9);
            var right = Evidence("run-a", 1);

            var decision = new HumanIdentityDecision(
                "decision-001",
                "coordination-project",
                HumanIdentityDecisionKind.ConfirmSameIdentity,
                left,
                right,
                "identity-001",
                "coordinator-a",
                null);

            Assert.Same(left, decision.LeftEvidence);
            Assert.Same(right, decision.RightEvidence);
            Assert.Equal("run-z", decision.LeftEvidence.RunId);
            Assert.Equal("run-a", decision.RightEvidence.RunId);
        }

        [Fact]
        public void RejectSameIdentity_DoesNotInferIdentityFromWhitespace()
        {
            var decision = new HumanIdentityDecision(
                "decision-001",
                "coordination-project",
                HumanIdentityDecisionKind.RejectSameIdentity,
                Evidence("run-001", 1),
                Evidence("run-002", 2),
                "   ",
                "coordinator-a",
                null);

            Assert.Null(decision.PersistentIdentityId);
        }

        [Fact]
        public void IdentityGovernanceDocument_DivergentProjectId_Fails()
        {
            var decision = new HumanIdentityDecision(
                "decision-001",
                "project-a",
                HumanIdentityDecisionKind.RejectSameIdentity,
                Evidence("run-001", 1),
                Evidence("run-002", 2),
                null,
                "coordinator-a",
                null);

            var ex = Assert.Throws<ArgumentException>(
                () => new IdentityGovernanceDocument("project-b", new[] { decision }));

            Assert.Contains("does not match document project", ex.Message, StringComparison.Ordinal);
        }

        [Fact]
        public void IdentityGovernanceDocument_DuplicateDecisionId_Fails()
        {
            var first = Decision("decision-001", 1, 2);
            var second = Decision("decision-001", 3, 4);

            var ex = Assert.Throws<ArgumentException>(
                () => new IdentityGovernanceDocument("coordination-project", new[] { first, second }));

            Assert.Contains("is duplicated", ex.Message, StringComparison.Ordinal);
        }

        [Fact]
        public void IdentityGovernanceDocument_DuplicateOrderedEvidencePair_Fails()
        {
            var first = Decision("decision-001", 1, 2);
            var second = new HumanIdentityDecision(
                "decision-002",
                "coordination-project",
                HumanIdentityDecisionKind.RejectSameIdentity,
                Evidence("run-001", 1),
                Evidence("run-002", 2),
                null,
                "coordinator-b",
                "Different decision on same pair");

            var ex = Assert.Throws<ArgumentException>(
                () => new IdentityGovernanceDocument("coordination-project", new[] { first, second }));

            Assert.Contains("Ordered evidence pair", ex.Message, StringComparison.Ordinal);
        }

        private static ClashEvidenceEndpoint Evidence(string runId, int occurrenceIndex) =>
            new ClashEvidenceEndpoint(runId, occurrenceIndex);

        private static HumanIdentityDecision Decision(string decisionId, int leftIndex, int rightIndex) =>
            new HumanIdentityDecision(
                decisionId,
                "coordination-project",
                HumanIdentityDecisionKind.ConfirmSameIdentity,
                Evidence("run-001", leftIndex),
                Evidence("run-002", rightIndex),
                "identity-001",
                "coordinator-a",
                "Confirmed");
    }
}
