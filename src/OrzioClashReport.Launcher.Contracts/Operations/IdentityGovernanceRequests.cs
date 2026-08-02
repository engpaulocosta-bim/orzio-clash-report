using System;

namespace OrzioClashReport.Launcher.Contracts.Operations
{
    /// <summary>
    /// The two explicit human identity decisions the engine accepts. These names are the canonical
    /// values passed to <c>--decision-kind</c>; they are never translated. Only the visible label
    /// shown in the user interface may be localized.
    /// </summary>
    public enum HumanIdentityDecisionKind
    {
        /// <summary>A human confirmed the two endpoints are the same persistent clash.</summary>
        ConfirmSameIdentity = 0,

        /// <summary>A human rejected that the two endpoints are the same clash.</summary>
        RejectSameIdentity = 1,
    }

    /// <summary>
    /// One immutable evidence endpoint: a run id plus a zero-based occurrence index inside that run's
    /// snapshot. The launcher validates the operational format only; the engine validates existence.
    /// </summary>
    public sealed class IdentityEvidenceEndpoint
    {
        public IdentityEvidenceEndpoint(string runId, int occurrenceIndex)
        {
            RunId = RequestValues.RequiredText(runId, nameof(runId));
            OccurrenceIndex = RequestValues.RequiredOccurrenceIndex(occurrenceIndex, nameof(occurrenceIndex));
        }

        public string RunId { get; }

        /// <summary>Zero-based occurrence index within the referenced run snapshot.</summary>
        public int OccurrenceIndex { get; }
    }

    /// <summary>One empty project-scoped identity-governance document.</summary>
    public sealed class CreateIdentityGovernanceRequest : LauncherOperationRequest
    {
        public CreateIdentityGovernanceRequest(string projectId, string outputGovernancePath)
        {
            ProjectId = RequestValues.RequiredText(projectId, nameof(projectId));
            OutputGovernancePath =
                RequestValues.RequiredAbsoluteOutputPath(outputGovernancePath, nameof(outputGovernancePath));
        }

        public string ProjectId { get; }

        public string OutputGovernancePath { get; }

        public override LauncherOperationKind Kind => LauncherOperationKind.CreateIdentityGovernance;

        public override string? OutputPath => OutputGovernancePath;

        public override LauncherOperationRequest WithOutputPath(string outputPath) =>
            new CreateIdentityGovernanceRequest(ProjectId, outputPath);
    }

    /// <summary>
    /// Appends exactly one explicit human decision to an existing governance document.
    /// A confirmation always carries a persistent identity id; a rejection never does.
    /// An algorithmic suggestion is never a decision, and this request cannot express one.
    /// </summary>
    public sealed class AppendIdentityDecisionRequest : LauncherOperationRequest
    {
        private AppendIdentityDecisionRequest(
            string governancePath,
            string decisionId,
            HumanIdentityDecisionKind decisionKind,
            IdentityEvidenceEndpoint left,
            IdentityEvidenceEndpoint right,
            string reviewerAlias,
            string? persistentIdentityId,
            string? reason)
        {
            GovernancePath = governancePath;
            DecisionId = decisionId;
            DecisionKind = decisionKind;
            Left = left;
            Right = right;
            ReviewerAlias = reviewerAlias;
            PersistentIdentityId = persistentIdentityId;
            Reason = reason;
        }

        public string GovernancePath { get; }

        public string DecisionId { get; }

        public HumanIdentityDecisionKind DecisionKind { get; }

        public IdentityEvidenceEndpoint Left { get; }

        public IdentityEvidenceEndpoint Right { get; }

        public string ReviewerAlias { get; }

        /// <summary>Set only for <see cref="HumanIdentityDecisionKind.ConfirmSameIdentity"/>.</summary>
        public string? PersistentIdentityId { get; }

        public string? Reason { get; }

        public override LauncherOperationKind Kind => LauncherOperationKind.AppendIdentityDecision;

        /// <summary>Builds a confirmation. The persistent identity id is mandatory.</summary>
        public static AppendIdentityDecisionRequest Confirm(
            string governancePath,
            string decisionId,
            IdentityEvidenceEndpoint left,
            IdentityEvidenceEndpoint right,
            string reviewerAlias,
            string persistentIdentityId,
            string? reason = null)
        {
            return new AppendIdentityDecisionRequest(
                RequestValues.RequiredPath(governancePath, nameof(governancePath)),
                RequestValues.RequiredText(decisionId, nameof(decisionId)),
                HumanIdentityDecisionKind.ConfirmSameIdentity,
                RequireEndpoint(left, nameof(left)),
                RequireEndpoint(right, nameof(right)),
                RequestValues.RequiredText(reviewerAlias, nameof(reviewerAlias)),
                RequestValues.RequiredText(persistentIdentityId, nameof(persistentIdentityId)),
                RequestValues.OptionalText(reason));
        }

        /// <summary>Builds a rejection. A rejection can never carry a persistent identity id.</summary>
        public static AppendIdentityDecisionRequest Reject(
            string governancePath,
            string decisionId,
            IdentityEvidenceEndpoint left,
            IdentityEvidenceEndpoint right,
            string reviewerAlias,
            string? reason = null)
        {
            return new AppendIdentityDecisionRequest(
                RequestValues.RequiredPath(governancePath, nameof(governancePath)),
                RequestValues.RequiredText(decisionId, nameof(decisionId)),
                HumanIdentityDecisionKind.RejectSameIdentity,
                RequireEndpoint(left, nameof(left)),
                RequireEndpoint(right, nameof(right)),
                RequestValues.RequiredText(reviewerAlias, nameof(reviewerAlias)),
                persistentIdentityId: null,
                RequestValues.OptionalText(reason));
        }

        private static IdentityEvidenceEndpoint RequireEndpoint(IdentityEvidenceEndpoint endpoint, string parameterName)
        {
            if (endpoint == null)
            {
                throw new ArgumentNullException(parameterName);
            }

            return endpoint;
        }
    }

    /// <summary>Read-only evidence validation of a governance document against one project's snapshots.</summary>
    public sealed class ValidateIdentityGovernanceRequest : LauncherOperationRequest
    {
        public ValidateIdentityGovernanceRequest(string projectPath, string governancePath)
        {
            ProjectPath = RequestValues.RequiredPath(projectPath, nameof(projectPath));
            GovernancePath = RequestValues.RequiredPath(governancePath, nameof(governancePath));
        }

        public string ProjectPath { get; }

        public string GovernancePath { get; }

        public override LauncherOperationKind Kind => LauncherOperationKind.ValidateIdentityGovernance;
    }

    /// <summary>One standalone HTML review of the persisted explicit human decisions.</summary>
    public sealed class RenderIdentityGovernanceReportRequest : LauncherOperationRequest
    {
        public RenderIdentityGovernanceReportRequest(
            string projectPath, string governancePath, string outputHtmlPath)
        {
            ProjectPath = RequestValues.RequiredPath(projectPath, nameof(projectPath));
            GovernancePath = RequestValues.RequiredPath(governancePath, nameof(governancePath));
            OutputHtmlPath = RequestValues.RequiredAbsoluteOutputPath(outputHtmlPath, nameof(outputHtmlPath));
        }

        public string ProjectPath { get; }

        public string GovernancePath { get; }

        public string OutputHtmlPath { get; }

        public override LauncherOperationKind Kind => LauncherOperationKind.RenderIdentityGovernanceReport;

        public override string? OutputPath => OutputHtmlPath;

        public override LauncherOperationRequest WithOutputPath(string outputPath) =>
            new RenderIdentityGovernanceReportRequest(ProjectPath, GovernancePath, outputPath);
    }
}
