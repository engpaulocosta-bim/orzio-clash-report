using System;
using System.Collections.Generic;
using OrzioClashReport.Core.Governance;
using OrzioClashReport.Core.Model;
using OrzioClashReport.Core.Presentation;

namespace OrzioClashReport.Tests
{
    /// <summary>Tests for the deterministic identity-governance review presenter: pure endpoint resolution and immutable operational projection only.</summary>
    public sealed class IdentityGovernanceReviewPresenterTests
    {
        [Fact]
        public void Present_EmptyDocument_ProducesZeroDecisionPresentation()
        {
            var runs = IdentityGovernanceReviewTestData.CreateRuns();
            var governance = IdentityGovernanceReviewTestData.CreateGovernance();

            var presentation = Sut().Present("coordination-project", "Coordination Project", governance, runs);

            Assert.Equal("coordination-project", presentation.ProjectId);
            Assert.Equal("Coordination Project", presentation.ProjectDisplayName);
            Assert.Equal(3, presentation.IndexedRunCount);
            Assert.Equal(0, presentation.DecisionCount);
            Assert.Equal(0, presentation.ConfirmationCount);
            Assert.Equal(0, presentation.RejectionCount);
            Assert.Equal(0, presentation.EvidenceEndpointCount);
            Assert.Empty(presentation.Decisions);
        }

        [Fact]
        public void Present_ConfirmationAndRejection_PreservesDecisionOrderAndEndpointSides()
        {
            var runs = IdentityGovernanceReviewTestData.CreateRuns();
            var governance = IdentityGovernanceReviewTestData.CreateGovernance(
                IdentityGovernanceReviewTestData.Confirm("decision-001", "run-001", 0, "run-003", 2),
                IdentityGovernanceReviewTestData.Reject("decision-002", "run-002", 1, "run-003", 3));

            var presentation = Sut().Present("coordination-project", "Coordination Project", governance, runs);

            Assert.Equal(2, presentation.DecisionCount);
            Assert.Equal(1, presentation.ConfirmationCount);
            Assert.Equal(1, presentation.RejectionCount);
            Assert.Equal(4, presentation.EvidenceEndpointCount);

            Assert.Equal("decision-001", presentation.Decisions[0].DecisionId);
            Assert.Equal("decision-002", presentation.Decisions[1].DecisionId);
            Assert.Equal(IdentityGovernanceEvidenceEndpointSide.Left, presentation.Decisions[0].LeftEndpoint.Side);
            Assert.Equal(IdentityGovernanceEvidenceEndpointSide.Right, presentation.Decisions[0].RightEndpoint.Side);
            Assert.Equal(IdentityGovernanceEvidenceEndpointSide.Left, presentation.Decisions[1].LeftEndpoint.Side);
            Assert.Equal(IdentityGovernanceEvidenceEndpointSide.Right, presentation.Decisions[1].RightEndpoint.Side);
        }

        [Fact]
        public void Present_ResolvesNonAdjacentRunsAndCorrectOccurrenceValues()
        {
            var runs = IdentityGovernanceReviewTestData.CreateRuns();
            var governance = IdentityGovernanceReviewTestData.CreateGovernance(
                IdentityGovernanceReviewTestData.Confirm(
                    "decision-001",
                    "run-001",
                    1,
                    "run-003",
                    2,
                    reason: "Check the lineage"));

            var presentation = Sut().Present("coordination-project", "Coordination Project", governance, runs);
            IdentityGovernanceReviewDecisionPresentation decision = presentation.Decisions[0];

            Assert.Equal("identity-001", decision.PersistentIdentityId);
            Assert.Equal("Check the lineage", decision.Reason);

            Assert.Equal("run-001", decision.LeftEndpoint.RunId);
            Assert.Equal(1, decision.LeftEndpoint.OccurrenceIndex);
            Assert.Equal(new DateTimeOffset(2026, 7, 10, 9, 0, 0, TimeSpan.Zero), decision.LeftEndpoint.RunCreatedAt);
            Assert.Equal("HVAC vs Structure", decision.LeftEndpoint.ClashTestName);
            Assert.Equal(ClashStatus.Active, decision.LeftEndpoint.ClashStatus);
            Assert.Equal("Sigma A / Structure / Core A @ RA1", decision.LeftEndpoint.ModelADisplay);
            Assert.Equal("Alfa A / HVAC / Services A @ RA2", decision.LeftEndpoint.ModelBDisplay);
            Assert.Equal("A-002", decision.LeftEndpoint.ElementAId);
            Assert.Equal("Pipe 02", decision.LeftEndpoint.ElementAName);
            Assert.Null(decision.LeftEndpoint.ElementALevel);
            Assert.Null(decision.LeftEndpoint.ElementASourceModel);
            Assert.Equal("B-002", decision.LeftEndpoint.ElementBId);
            Assert.Null(decision.LeftEndpoint.ElementBName);
            Assert.Equal("Level 02", decision.LeftEndpoint.ElementBLevel);
            Assert.Null(decision.LeftEndpoint.ElementBSourceModel);

            Assert.Equal("run-003", decision.RightEndpoint.RunId);
            Assert.Equal(2, decision.RightEndpoint.OccurrenceIndex);
            Assert.Equal(new DateTimeOffset(2026, 7, 24, 9, 0, 0, TimeSpan.Zero), decision.RightEndpoint.RunCreatedAt);
            Assert.Equal(ClashStatus.Reviewed, decision.RightEndpoint.ClashStatus);
            Assert.Equal("A-003", decision.RightEndpoint.ElementAId);
            Assert.Null(decision.RightEndpoint.ElementAName);
            Assert.Equal("Level 03", decision.RightEndpoint.ElementALevel);
            Assert.Equal("HVAC-C", decision.RightEndpoint.ElementASourceModel);
        }

        [Fact]
        public void Present_ReasonAbsentAndRejectDecision_PreserveNullOptionalFields()
        {
            var runs = IdentityGovernanceReviewTestData.CreateRuns();
            var governance = IdentityGovernanceReviewTestData.CreateGovernance(
                IdentityGovernanceReviewTestData.Reject("decision-001", "run-002", 0, "run-003", 1));

            var presentation = Sut().Present("coordination-project", "Coordination Project", governance, runs);
            IdentityGovernanceReviewDecisionPresentation decision = presentation.Decisions[0];

            Assert.Equal(HumanIdentityDecisionKind.RejectSameIdentity, decision.DecisionKind);
            Assert.Null(decision.PersistentIdentityId);
            Assert.Null(decision.Reason);
        }

        [Fact]
        public void Present_DoesNotMutateInputs_AndReturnsReadOnlyCollections()
        {
            var runs = IdentityGovernanceReviewTestData.CreateRuns();
            var governance = IdentityGovernanceReviewTestData.CreateGovernance(
                IdentityGovernanceReviewTestData.Confirm("decision-001", "run-001", 0, "run-002", 1));

            string originalRunId = runs[0].RunId;
            int originalDecisionCount = governance.Decisions.Count;

            var presentation = Sut().Present("coordination-project", "Coordination Project", governance, runs);

            Assert.Equal(originalRunId, runs[0].RunId);
            Assert.Equal(originalDecisionCount, governance.Decisions.Count);
            Assert.Throws<NotSupportedException>(() =>
                ((IList<IdentityGovernanceReviewDecisionPresentation>)presentation.Decisions).Add(presentation.Decisions[0]));
        }

        [Fact]
        public void Present_DuplicateIndexedRunId_IsRejectedDefensively()
        {
            CoordinationRun duplicatedRun = IdentityGovernanceReviewTestData.CreateRuns()[0];
            var runs = new[] { duplicatedRun, duplicatedRun };
            var governance = IdentityGovernanceReviewTestData.CreateGovernance();

            var ex = Assert.Throws<InvalidOperationException>(() =>
                Sut().Present("coordination-project", "Coordination Project", governance, runs));

            Assert.Contains("duplicate run id", ex.Message, StringComparison.Ordinal);
        }

        private static DeterministicIdentityGovernanceReviewPresenter Sut() =>
            new DeterministicIdentityGovernanceReviewPresenter();
    }
}
