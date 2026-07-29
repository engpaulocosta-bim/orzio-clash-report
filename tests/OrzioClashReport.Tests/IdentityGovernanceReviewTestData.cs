using System;
using System.Collections.Generic;
using OrzioClashReport.Core.Model;

namespace OrzioClashReport.Tests
{
    internal static class IdentityGovernanceReviewTestData
    {
        public static IReadOnlyList<CoordinationRun> CreateRuns()
        {
            return new[]
            {
                CreateRun("run-001", new DateTimeOffset(2026, 7, 10, 9, 0, 0, TimeSpan.Zero), "A"),
                CreateRun("run-002", new DateTimeOffset(2026, 7, 17, 9, 0, 0, TimeSpan.Zero), "B"),
                CreateRun("run-003", new DateTimeOffset(2026, 7, 24, 9, 0, 0, TimeSpan.Zero), "C"),
            };
        }

        public static IReadOnlyList<CoordinationRun> CreateRunsWithPrivateSourceModels()
        {
            return new[]
            {
                CreateRun(
                    "run-001",
                    new DateTimeOffset(2026, 7, 10, 9, 0, 0, TimeSpan.Zero),
                    "A",
                    @"C:\Clients\Acme\Secret\Model-A.nwc",
                    @"\\fileserver\private\Model-B.nwc",
                    "/srv/customer/private/Model-C.nwc"),
                CreateRun(
                    "run-002",
                    new DateTimeOffset(2026, 7, 17, 9, 0, 0, TimeSpan.Zero),
                    "B",
                    @"C:\Clients\Acme\Secret\Model-A.nwc",
                    @"\\fileserver\private\Model-B.nwc",
                    "/srv/customer/private/Model-C.nwc"),
                CreateRun(
                    "run-003",
                    new DateTimeOffset(2026, 7, 24, 9, 0, 0, TimeSpan.Zero),
                    "C",
                    @"C:\Clients\Acme\Secret\Model-A.nwc",
                    @"\\fileserver\private\Model-B.nwc",
                    "/srv/customer/private/Model-C.nwc"),
            };
        }

        public static IdentityGovernanceDocument CreateGovernance(params HumanIdentityDecision[] decisions) =>
            new IdentityGovernanceDocument("coordination-project", decisions);

        public static HumanIdentityDecision Confirm(
            string decisionId,
            string leftRunId,
            int leftOccurrenceIndex,
            string rightRunId,
            int rightOccurrenceIndex,
            string reviewerAlias = "coordinator-a",
            string? reason = null,
            string persistentIdentityId = "identity-001") =>
            new HumanIdentityDecision(
                decisionId,
                "coordination-project",
                HumanIdentityDecisionKind.ConfirmSameIdentity,
                new ClashEvidenceEndpoint(leftRunId, leftOccurrenceIndex),
                new ClashEvidenceEndpoint(rightRunId, rightOccurrenceIndex),
                persistentIdentityId,
                reviewerAlias,
                reason);

        public static HumanIdentityDecision Reject(
            string decisionId,
            string leftRunId,
            int leftOccurrenceIndex,
            string rightRunId,
            int rightOccurrenceIndex,
            string reviewerAlias = "coordinator-a",
            string? reason = null) =>
            new HumanIdentityDecision(
                decisionId,
                "coordination-project",
                HumanIdentityDecisionKind.RejectSameIdentity,
                new ClashEvidenceEndpoint(leftRunId, leftOccurrenceIndex),
                new ClashEvidenceEndpoint(rightRunId, rightOccurrenceIndex),
                null,
                reviewerAlias,
                reason);

        private static CoordinationRun CreateRun(string runId, DateTimeOffset createdAt, string suffix)
        {
            return CreateRun(runId, createdAt, suffix, "HVAC-A", "Structure-A", "HVAC-C");
        }

        private static CoordinationRun CreateRun(
            string runId,
            DateTimeOffset createdAt,
            string suffix,
            string firstElementASourceModel,
            string firstElementBSourceModel,
            string thirdElementASourceModel)
        {
            var modelAIdentity = new ModelIdentity($"Sigma {suffix}", "Structure", $"Core {suffix}");
            var modelBIdentity = new ModelIdentity($"Alfa {suffix}", "HVAC", $"Services {suffix}");
            var modelA = new ModelRevision(modelAIdentity, $"R{suffix}1", $"sigma-{suffix}.nwc", null, null, null);
            var modelB = new ModelRevision(modelBIdentity, $"R{suffix}2", $"alfa-{suffix}.nwc", null, null, null);
            var manifest = new RunManifest(
                runId,
                createdAt,
                new[] { modelA, modelB },
                new[] { new ExecutedClashTest("HVAC vs Structure", modelAIdentity, modelBIdentity) });

            var occurrences = new List<ClashOccurrence>
            {
                CreateOccurrence("HVAC vs Structure", modelA, modelB, "A-001", "Duct 01", "Level 01", firstElementASourceModel, "B-001", "Beam 01", "Level 01", firstElementBSourceModel, ClashStatus.New),
                CreateOccurrence("HVAC vs Structure", modelA, modelB, "A-002", "Pipe 02", null, null, "B-002", null, "Level 02", null, ClashStatus.Active),
                CreateOccurrence("HVAC vs Structure", modelA, modelB, "A-003", null, "Level 03", thirdElementASourceModel, "B-003", "Column 03", null, "Structure-C", ClashStatus.Reviewed),
                CreateOccurrence("HVAC vs Structure", modelA, modelB, "A-004", "Tray 04", "Level 04", "HVAC-D", "B-004", "Slab 04", "Level 04", "Structure-D", ClashStatus.Resolved),
            };

            return new CoordinationRun(manifest, occurrences);
        }

        private static ClashOccurrence CreateOccurrence(
            string clashTestName,
            ModelRevision modelA,
            ModelRevision modelB,
            string elementAId,
            string? elementAName,
            string? elementALevel,
            string? elementASourceModel,
            string elementBId,
            string? elementBName,
            string? elementBLevel,
            string? elementBSourceModel,
            ClashStatus status)
        {
            var clash = new ClashResult(
                name: null,
                status,
                distance: null,
                gridLocation: null,
                point: null,
                elementA: new ClashObject(elementAId, elementAName, elementALevel, elementASourceModel, null, null),
                elementB: new ClashObject(elementBId, elementBName, elementBLevel, elementBSourceModel, null, null),
                guid: null);

            return new ClashOccurrence(clashTestName, clash, modelA, modelB);
        }
    }
}
