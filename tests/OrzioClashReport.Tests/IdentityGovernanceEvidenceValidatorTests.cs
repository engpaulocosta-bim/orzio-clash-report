using System;
using System.Collections.Generic;
using System.Linq;
using OrzioClashReport.Core.Governance;
using OrzioClashReport.Core.Model;

namespace OrzioClashReport.Tests
{
    /// <summary>
    /// Tests for <see cref="DeterministicIdentityGovernanceEvidenceValidator"/>: read-only validation of an
    /// <see cref="IdentityGovernanceDocument"/>'s project binding and every decision's evidence endpoints against
    /// a set of already-loaded, explicitly ordered indexed <see cref="CoordinationRun"/> snapshots.
    /// </summary>
    public sealed class IdentityGovernanceEvidenceValidatorTests
    {
        private static readonly ModelRevision Sigma =
            new ModelRevision(new ModelIdentity("Sigma", "Structure", "Main"), "R04", "Sigma_Main_R04.nwc", null, null, null);

        private static readonly ExecutedClashTest Test1ForSigma = new ExecutedClashTest("Test 1", Sigma.Identity, Sigma.Identity);

        private static readonly DateTimeOffset DefaultCreatedAt = new DateTimeOffset(2026, 7, 10, 9, 0, 0, TimeSpan.Zero);

        private static ClashObject MakeObject(string elementId) => new ClashObject(elementId, null, null, null, null, null);

        private static ClashOccurrence MakeOccurrence(string tag) =>
            new ClashOccurrence(
                "Test 1",
                new ClashResult("Test 1", ClashStatus.New, null, null, null, MakeObject($"{tag}-a"), MakeObject($"{tag}-b"), null),
                Sigma, Sigma);

        private static RunManifest MakeManifest(string runId) =>
            new RunManifest(runId, DefaultCreatedAt, new[] { Sigma }, new[] { Test1ForSigma });

        private static CoordinationRun MakeRun(string runId, int occurrenceCount)
        {
            var occurrences = new ClashOccurrence[occurrenceCount];
            for (int i = 0; i < occurrenceCount; i++)
            {
                occurrences[i] = MakeOccurrence($"{runId}-occ-{i}");
            }

            return new CoordinationRun(MakeManifest(runId), occurrences);
        }

        private static ClashEvidenceEndpoint Endpoint(string runId, int occurrenceIndex) =>
            new ClashEvidenceEndpoint(runId, occurrenceIndex);

        private static HumanIdentityDecision Confirm(
            string decisionId, string projectId, ClashEvidenceEndpoint left, ClashEvidenceEndpoint right, string persistentIdentityId = "identity-1") =>
            new HumanIdentityDecision(
                decisionId, projectId, HumanIdentityDecisionKind.ConfirmSameIdentity, left, right, persistentIdentityId, "coordinator-a", null);

        private static HumanIdentityDecision Reject(
            string decisionId, string projectId, ClashEvidenceEndpoint left, ClashEvidenceEndpoint right) =>
            new HumanIdentityDecision(
                decisionId, projectId, HumanIdentityDecisionKind.RejectSameIdentity, left, right, null, "coordinator-a", null);

        private static IdentityGovernanceDocument Governance(string projectId, params HumanIdentityDecision[] decisions) =>
            new IdentityGovernanceDocument(projectId, decisions);

        private static DeterministicIdentityGovernanceEvidenceValidator CreateValidator() => new DeterministicIdentityGovernanceEvidenceValidator();

        // ===================== acceptance =====================

        [Fact]
        public void Validate_EmptyDocument_IsValid()
        {
            var runA = MakeRun("run-A", 2);
            var governance = Governance("coordination-project");

            var result = CreateValidator().Validate("coordination-project", governance, new[] { runA });

            Assert.True(result.IsValid);
            Assert.Empty(result.Issues);
        }

        [Fact]
        public void Validate_ValidConfirmation_IsValid()
        {
            var runA = MakeRun("run-A", 3);
            var runB = MakeRun("run-B", 3);
            var governance = Governance(
                "coordination-project",
                Confirm("decision-1", "coordination-project", Endpoint("run-A", 0), Endpoint("run-B", 1)));

            var result = CreateValidator().Validate("coordination-project", governance, new[] { runA, runB });

            Assert.True(result.IsValid);
        }

        [Fact]
        public void Validate_ValidRejection_IsValid()
        {
            var runA = MakeRun("run-A", 3);
            var runB = MakeRun("run-B", 3);
            var governance = Governance(
                "coordination-project",
                Reject("decision-1", "coordination-project", Endpoint("run-A", 0), Endpoint("run-B", 1)));

            var result = CreateValidator().Validate("coordination-project", governance, new[] { runA, runB });

            Assert.True(result.IsValid);
        }

        [Fact]
        public void Validate_DecisionBetweenNonAdjacentRuns_IsValid()
        {
            var runA = MakeRun("run-A", 2);
            var runB = MakeRun("run-B", 2);
            var runC = MakeRun("run-C", 2);
            var governance = Governance(
                "coordination-project",
                Confirm("decision-1", "coordination-project", Endpoint("run-A", 0), Endpoint("run-C", 1)));

            var result = CreateValidator().Validate("coordination-project", governance, new[] { runA, runB, runC });

            Assert.True(result.IsValid);
        }

        // ===================== rejection: project id =====================

        [Fact]
        public void Validate_ProjectIdMismatch_ProducesIssue()
        {
            var runA = MakeRun("run-A", 1);
            var governance = Governance("other-project");

            var result = CreateValidator().Validate("coordination-project", governance, new[] { runA });

            Assert.False(result.IsValid);
            var issue = Assert.Single(result.Issues);
            Assert.Equal(IdentityGovernanceEvidenceValidationIssueKind.ProjectIdMismatch, issue.Kind);
        }

        [Fact]
        public void Validate_ProjectIdComparisonIsOrdinal_CaseDiffersProducesMismatch()
        {
            var runA = MakeRun("run-A", 1);
            var governance = Governance("Coordination-Project");

            var result = CreateValidator().Validate("coordination-project", governance, new[] { runA });

            Assert.False(result.IsValid);
            Assert.Contains(result.Issues, i => i.Kind == IdentityGovernanceEvidenceValidationIssueKind.ProjectIdMismatch);
        }

        // ===================== rejection: run not indexed =====================

        [Fact]
        public void Validate_LeftRunMissing_ProducesRunNotIndexedIssue()
        {
            var runB = MakeRun("run-B", 2);
            var governance = Governance(
                "coordination-project",
                Confirm("decision-1", "coordination-project", Endpoint("run-missing", 0), Endpoint("run-B", 1)));

            var result = CreateValidator().Validate("coordination-project", governance, new[] { runB });

            Assert.False(result.IsValid);
            var issue = Assert.Single(result.Issues);
            Assert.Equal(IdentityGovernanceEvidenceValidationIssueKind.RunNotIndexed, issue.Kind);
            Assert.Equal("decision-1", issue.DecisionId);
            Assert.Equal(IdentityGovernanceEvidenceEndpointSide.Left, issue.Side);
            Assert.Equal("run-missing", issue.RunId);
            Assert.Equal(0, issue.OccurrenceIndex);
        }

        [Fact]
        public void Validate_RightRunMissing_ProducesRunNotIndexedIssue()
        {
            var runA = MakeRun("run-A", 2);
            var governance = Governance(
                "coordination-project",
                Confirm("decision-1", "coordination-project", Endpoint("run-A", 0), Endpoint("run-missing", 1)));

            var result = CreateValidator().Validate("coordination-project", governance, new[] { runA });

            Assert.False(result.IsValid);
            var issue = Assert.Single(result.Issues);
            Assert.Equal(IdentityGovernanceEvidenceValidationIssueKind.RunNotIndexed, issue.Kind);
            Assert.Equal(IdentityGovernanceEvidenceEndpointSide.Right, issue.Side);
            Assert.Equal("run-missing", issue.RunId);
        }

        // ===================== rejection: occurrence index range =====================

        [Fact]
        public void Validate_LeftOccurrenceIndexOutOfRange_ProducesIssue()
        {
            var runA = MakeRun("run-A", 2);
            var runB = MakeRun("run-B", 2);
            var governance = Governance(
                "coordination-project",
                Confirm("decision-1", "coordination-project", Endpoint("run-A", 5), Endpoint("run-B", 0)));

            var result = CreateValidator().Validate("coordination-project", governance, new[] { runA, runB });

            Assert.False(result.IsValid);
            var issue = Assert.Single(result.Issues);
            Assert.Equal(IdentityGovernanceEvidenceValidationIssueKind.OccurrenceIndexOutOfRange, issue.Kind);
            Assert.Equal(IdentityGovernanceEvidenceEndpointSide.Left, issue.Side);
            Assert.Equal("run-A", issue.RunId);
            Assert.Equal(5, issue.OccurrenceIndex);
            Assert.Equal(2, issue.OccurrenceCount);
        }

        [Fact]
        public void Validate_RightOccurrenceIndexEqualsCount_ProducesIssue()
        {
            var runA = MakeRun("run-A", 2);
            var runB = MakeRun("run-B", 2);
            var governance = Governance(
                "coordination-project",
                Confirm("decision-1", "coordination-project", Endpoint("run-A", 0), Endpoint("run-B", 2)));

            var result = CreateValidator().Validate("coordination-project", governance, new[] { runA, runB });

            Assert.False(result.IsValid);
            var issue = Assert.Single(result.Issues);
            Assert.Equal(IdentityGovernanceEvidenceValidationIssueKind.OccurrenceIndexOutOfRange, issue.Kind);
            Assert.Equal(IdentityGovernanceEvidenceEndpointSide.Right, issue.Side);
            Assert.Equal(2, issue.OccurrenceIndex);
            Assert.Equal(2, issue.OccurrenceCount);
        }

        [Fact]
        public void Validate_ZeroOccurrenceSnapshotWithIndexZero_IsInvalid()
        {
            var runA = MakeRun("run-A", 0);
            var runB = MakeRun("run-B", 1);
            var governance = Governance(
                "coordination-project",
                Confirm("decision-1", "coordination-project", Endpoint("run-A", 0), Endpoint("run-B", 0)));

            var result = CreateValidator().Validate("coordination-project", governance, new[] { runA, runB });

            Assert.False(result.IsValid);
            var issue = Assert.Single(result.Issues);
            Assert.Equal(IdentityGovernanceEvidenceValidationIssueKind.OccurrenceIndexOutOfRange, issue.Kind);
            Assert.Equal("run-A", issue.RunId);
            Assert.Equal(0, issue.OccurrenceCount);
        }

        // ===================== rejection: duplicate run id =====================

        [Fact]
        public void Validate_DuplicateRunId_ProducesIssue()
        {
            var runA1 = MakeRun("duplicate-run", 2);
            var runA2 = MakeRun("duplicate-run", 2);
            var governance = Governance("coordination-project");

            var result = CreateValidator().Validate("coordination-project", governance, new[] { runA1, runA2 });

            Assert.False(result.IsValid);
            var issue = Assert.Single(result.Issues);
            Assert.Equal(IdentityGovernanceEvidenceValidationIssueKind.DuplicateIndexedRunId, issue.Kind);
            Assert.Equal("duplicate-run", issue.RunId);
        }

        [Fact]
        public void Validate_TwoDuplications_PreserveOrder()
        {
            var runA1 = MakeRun("run-A", 1);
            var runA2 = MakeRun("run-A", 1);
            var runB1 = MakeRun("run-B", 1);
            var runB2 = MakeRun("run-B", 1);
            var governance = Governance("coordination-project");

            var result = CreateValidator().Validate("coordination-project", governance, new[] { runA1, runA2, runB1, runB2 });

            Assert.Equal(2, result.Issues.Count);
            Assert.Equal(IdentityGovernanceEvidenceValidationIssueKind.DuplicateIndexedRunId, result.Issues[0].Kind);
            Assert.Equal("run-A", result.Issues[0].RunId);
            Assert.Equal(IdentityGovernanceEvidenceValidationIssueKind.DuplicateIndexedRunId, result.Issues[1].Kind);
            Assert.Equal("run-B", result.Issues[1].RunId);
        }

        [Fact]
        public void Validate_EndpointReferencingDuplicateRunId_DoesNotProduceExtraIssue()
        {
            var runA1 = MakeRun("duplicate-run", 2);
            var runA2 = MakeRun("duplicate-run", 2);
            var governance = Governance(
                "coordination-project",
                Confirm("decision-1", "coordination-project", Endpoint("duplicate-run", 0), Endpoint("duplicate-run", 1)));

            var result = CreateValidator().Validate("coordination-project", governance, new[] { runA1, runA2 });

            // Only the duplication issue appears; the endpoint is not additionally reported as RunNotIndexed or
            // resolved arbitrarily against one of the ambiguous runs.
            var issue = Assert.Single(result.Issues);
            Assert.Equal(IdentityGovernanceEvidenceValidationIssueKind.DuplicateIndexedRunId, issue.Kind);
        }

        // ===================== rejection: duplicate run id via repeated instance =====================

        [Fact]
        public void Validate_SameInstanceTwice_ProducesOneDuplicateIssue()
        {
            var run = MakeRun("duplicate-run", 2);

            var result = CreateValidator().Validate("coordination-project", Governance("coordination-project"), new[] { run, run });

            Assert.False(result.IsValid);
            var issue = Assert.Single(result.Issues);
            Assert.Equal(IdentityGovernanceEvidenceValidationIssueKind.DuplicateIndexedRunId, issue.Kind);
            Assert.Equal("duplicate-run", issue.RunId);
        }

        [Fact]
        public void Validate_SameInstanceThreeTimes_ProducesTwoDuplicateIssues()
        {
            var run = MakeRun("duplicate-run", 2);

            var result = CreateValidator().Validate("coordination-project", Governance("coordination-project"), new[] { run, run, run });

            Assert.Equal(2, result.Issues.Count);
            Assert.Equal(IdentityGovernanceEvidenceValidationIssueKind.DuplicateIndexedRunId, result.Issues[0].Kind);
            Assert.Equal("duplicate-run", result.Issues[0].RunId);
            Assert.Equal(IdentityGovernanceEvidenceValidationIssueKind.DuplicateIndexedRunId, result.Issues[1].Kind);
            Assert.Equal("duplicate-run", result.Issues[1].RunId);
        }

        [Fact]
        public void Validate_EndpointReferencingRepeatedInstance_DoesNotProduceExtraIssue()
        {
            var run = MakeRun("duplicate-run", 2);
            var governance = Governance(
                "coordination-project",
                Confirm("decision-1", "coordination-project", Endpoint("duplicate-run", 0), Endpoint("duplicate-run", 1)));

            var result = CreateValidator().Validate("coordination-project", governance, new[] { run, run });

            var issue = Assert.Single(result.Issues);
            Assert.Equal(IdentityGovernanceEvidenceValidationIssueKind.DuplicateIndexedRunId, issue.Kind);
            Assert.False(result.IsValid);
        }

        [Fact]
        public void Validate_RepeatedInstanceMixedWithDistinctObject_ProducesTwoDuplicateIssuesInOrder()
        {
            var runA = MakeRun("duplicate-run", 2);
            var runB = MakeRun("duplicate-run", 2);

            var result = CreateValidator().Validate("coordination-project", Governance("coordination-project"), new[] { runA, runA, runB });

            Assert.Equal(2, result.Issues.Count);
            Assert.Equal(IdentityGovernanceEvidenceValidationIssueKind.DuplicateIndexedRunId, result.Issues[0].Kind);
            Assert.Equal(IdentityGovernanceEvidenceValidationIssueKind.DuplicateIndexedRunId, result.Issues[1].Kind);
        }

        // ===================== ordering =====================

        [Fact]
        public void Validate_MultipleIssues_AppearInDeterministicOrder()
        {
            var runA1 = MakeRun("run-A", 1);
            var runA2 = MakeRun("run-A", 1);
            var runB = MakeRun("run-B", 1);
            var governance = Governance(
                "other-project",
                Confirm("decision-1", "other-project", Endpoint("run-missing", 0), Endpoint("run-B", 5)));

            var result = CreateValidator().Validate("coordination-project", governance, new[] { runA1, runA2, runB });

            Assert.Equal(4, result.Issues.Count);
            Assert.Equal(IdentityGovernanceEvidenceValidationIssueKind.ProjectIdMismatch, result.Issues[0].Kind);
            Assert.Equal(IdentityGovernanceEvidenceValidationIssueKind.DuplicateIndexedRunId, result.Issues[1].Kind);
            Assert.Equal(IdentityGovernanceEvidenceValidationIssueKind.RunNotIndexed, result.Issues[2].Kind);
            Assert.Equal(IdentityGovernanceEvidenceEndpointSide.Left, result.Issues[2].Side);
            Assert.Equal(IdentityGovernanceEvidenceValidationIssueKind.OccurrenceIndexOutOfRange, result.Issues[3].Kind);
            Assert.Equal(IdentityGovernanceEvidenceEndpointSide.Right, result.Issues[3].Side);
        }

        [Fact]
        public void Validate_LeftIssue_AppearsBeforeRightIssue()
        {
            var runA = MakeRun("run-A", 1);
            var runB = MakeRun("run-B", 1);
            var governance = Governance(
                "coordination-project",
                Confirm("decision-1", "coordination-project", Endpoint("run-A", 9), Endpoint("run-B", 9)));

            var result = CreateValidator().Validate("coordination-project", governance, new[] { runA, runB });

            Assert.Equal(2, result.Issues.Count);
            Assert.Equal(IdentityGovernanceEvidenceEndpointSide.Left, result.Issues[0].Side);
            Assert.Equal(IdentityGovernanceEvidenceEndpointSide.Right, result.Issues[1].Side);
        }

        [Fact]
        public void Validate_DecisionsAppearInPersistedOrder()
        {
            var runA = MakeRun("run-A", 1);
            var runB = MakeRun("run-B", 1);
            var governance = Governance(
                "coordination-project",
                Confirm("decision-1", "coordination-project", Endpoint("run-A", 9), Endpoint("run-B", 0)),
                Reject("decision-2", "coordination-project", Endpoint("run-A", 0), Endpoint("run-B", 9)));

            var result = CreateValidator().Validate("coordination-project", governance, new[] { runA, runB });

            Assert.Equal(2, result.Issues.Count);
            Assert.Equal("decision-1", result.Issues[0].DecisionId);
            Assert.Equal("decision-2", result.Issues[1].DecisionId);
        }

        // ===================== ordinal comparisons =====================

        [Fact]
        public void Validate_RunIdComparisonIsOrdinal_CaseDiffersIsNotIndexed()
        {
            var runA = MakeRun("Run-A", 1);
            var governance = Governance(
                "coordination-project",
                Confirm("decision-1", "coordination-project", Endpoint("run-a", 0), Endpoint("Run-A", 0)));

            var result = CreateValidator().Validate("coordination-project", governance, new[] { runA });

            Assert.False(result.IsValid);
            Assert.Contains(result.Issues, i => i.Kind == IdentityGovernanceEvidenceValidationIssueKind.RunNotIndexed && i.RunId == "run-a");
        }

        // ===================== purity / immutability =====================

        [Fact]
        public void Validate_DoesNotMutateInputs()
        {
            var runA = MakeRun("run-A", 2);
            var runB = MakeRun("run-B", 2);
            var runs = new[] { runA, runB };
            var governance = Governance(
                "coordination-project",
                Confirm("decision-1", "coordination-project", Endpoint("run-A", 0), Endpoint("run-B", 1)));
            var decisionsBefore = governance.Decisions.ToList();

            CreateValidator().Validate("coordination-project", governance, runs);

            Assert.Same(runA, runs[0]);
            Assert.Same(runB, runs[1]);
            Assert.Equal(decisionsBefore, governance.Decisions);
            Assert.Equal(2, runA.Occurrences.Count);
            Assert.Equal(2, runB.Occurrences.Count);
        }

        [Fact]
        public void Validate_ResultAndIssuesListAreImmutable()
        {
            var runA = MakeRun("run-A", 1);
            var governance = Governance("other-project");

            var result = CreateValidator().Validate("coordination-project", governance, new[] { runA });

            Assert.Throws<NotSupportedException>(() => ((IList<IdentityGovernanceEvidenceValidationIssue>)result.Issues).Add(result.Issues[0]));
        }
    }
}
