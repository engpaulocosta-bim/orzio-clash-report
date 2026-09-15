using OrzioClashReport.Core.Governance;
using OrzioClashReport.Core.Model;
using Xunit;

namespace OrzioClashReport.Tests
{
    public sealed class LongitudinalIdentityConfirmationProjectorTests
    {
        [Fact]
        public void Project_IncludesOnlyExplicitConfirmations()
        {
            var confirm = new HumanIdentityDecision(
                "decision-confirm",
                "project-1",
                HumanIdentityDecisionKind.ConfirmSameIdentity,
                new ClashEvidenceEndpoint("run-1", 2),
                new ClashEvidenceEndpoint("run-2", 4),
                "identity-42",
                "coordinator-a",
                "Reviewed in model context");
            var reject = new HumanIdentityDecision(
                "decision-reject",
                "project-1",
                HumanIdentityDecisionKind.RejectSameIdentity,
                new ClashEvidenceEndpoint("run-2", 7),
                new ClashEvidenceEndpoint("run-3", 8),
                null,
                "coordinator-b",
                "Different physical issue");
            var governance = new IdentityGovernanceDocument("project-1", new[] { confirm, reject });

            var projector = new DeterministicLongitudinalIdentityConfirmationProjector();
            var result = projector.Project(governance);

            Assert.Single(result);
            Assert.Equal("decision-confirm", result[0].DecisionId);
            Assert.Equal("identity-42", result[0].PersistentIdentityId);
            Assert.Equal(new ClashEvidenceEndpoint("run-1", 2), result[0].LeftEvidence);
            Assert.Equal(new ClashEvidenceEndpoint("run-2", 4), result[0].RightEvidence);
            Assert.Equal("coordinator-a", result[0].ReviewerAlias);
            Assert.Equal("Reviewed in model context", result[0].Reason);
        }

        [Fact]
        public void Project_DoesNotInferTransitivityAcrossTwoConfirmedPairs()
        {
            var first = new HumanIdentityDecision(
                "decision-1",
                "project-1",
                HumanIdentityDecisionKind.ConfirmSameIdentity,
                new ClashEvidenceEndpoint("run-1", 1),
                new ClashEvidenceEndpoint("run-2", 2),
                "identity-A",
                "coordinator",
                null);
            var second = new HumanIdentityDecision(
                "decision-2",
                "project-1",
                HumanIdentityDecisionKind.ConfirmSameIdentity,
                new ClashEvidenceEndpoint("run-2", 2),
                new ClashEvidenceEndpoint("run-3", 3),
                "identity-A",
                "coordinator",
                null);
            var governance = new IdentityGovernanceDocument("project-1", new[] { first, second });

            var projector = new DeterministicLongitudinalIdentityConfirmationProjector();
            var result = projector.Project(governance);

            Assert.Equal(2, result.Count);
            Assert.DoesNotContain(result, confirmation =>
                confirmation.LeftEvidence.Equals(new ClashEvidenceEndpoint("run-1", 1))
                && confirmation.RightEvidence.Equals(new ClashEvidenceEndpoint("run-3", 3)));
        }

        [Fact]
        public void Project_PreservesDecisionOrderDeterministically()
        {
            var first = Confirmation("decision-b", "run-2", 0, "run-3", 0, "identity-b");
            var second = Confirmation("decision-a", "run-1", 0, "run-2", 0, "identity-a");
            var governance = new IdentityGovernanceDocument("project-1", new[] { first, second });

            var result = new DeterministicLongitudinalIdentityConfirmationProjector().Project(governance);

            Assert.Equal("decision-b", result[0].DecisionId);
            Assert.Equal("decision-a", result[1].DecisionId);
        }

        private static HumanIdentityDecision Confirmation(
            string decisionId,
            string leftRun,
            int leftIndex,
            string rightRun,
            int rightIndex,
            string identityId)
        {
            return new HumanIdentityDecision(
                decisionId,
                "project-1",
                HumanIdentityDecisionKind.ConfirmSameIdentity,
                new ClashEvidenceEndpoint(leftRun, leftIndex),
                new ClashEvidenceEndpoint(rightRun, rightIndex),
                identityId,
                "coordinator",
                null);
        }
    }
}
