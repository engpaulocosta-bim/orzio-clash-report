using OrzioClashReport.Launcher.Application.Localization;
using System;
using System.Globalization;
using OrzioClashReport.Launcher.Contracts.Operations;

namespace OrzioClashReport.Launcher.Application.Operations
{
    /// <summary>
    /// One human identity decision, validated for shape only. Whether the runs and occurrences it
    /// points at actually exist is the engine's question, answered by
    /// <c>validate-identity-governance</c>; the launcher never resolves evidence itself and never
    /// turns an algorithmic suggestion into a decision.
    /// </summary>
    public sealed class IdentityDecisionDraft
    {
        public IdentityDecisionKind Kind { get; }
        public string DecisionId { get; }
        public string LeftRunId { get; }
        public int LeftOccurrenceIndex { get; }
        public string RightRunId { get; }
        public int RightOccurrenceIndex { get; }
        public string ReviewerAlias { get; }
        public string? PersistentIdentityId { get; }
        public string? Reason { get; }

        public IdentityDecisionDraft(
            IdentityDecisionKind kind,
            string decisionId,
            string leftRunId,
            int leftOccurrenceIndex,
            string rightRunId,
            int rightOccurrenceIndex,
            string reviewerAlias,
            string? persistentIdentityId,
            string? reason)
        {
            if (!Enum.IsDefined(typeof(IdentityDecisionKind), kind))
            {
                throw new ArgumentOutOfRangeException(nameof(kind), kind, "Unknown identity decision kind.");
            }

            Require(decisionId, nameof(decisionId), Loc.T("draft.error.decisionId"));
            Require(leftRunId, nameof(leftRunId), Loc.T("draft.error.leftRunId"));
            Require(rightRunId, nameof(rightRunId), Loc.T("draft.error.rightRunId"));
            Require(reviewerAlias, nameof(reviewerAlias), Loc.T("draft.error.reviewerAlias"));

            RequireOccurrenceIndex(leftOccurrenceIndex, nameof(leftOccurrenceIndex));
            RequireOccurrenceIndex(rightOccurrenceIndex, nameof(rightOccurrenceIndex));

            string? persistent = Trimmed(persistentIdentityId);

            if (kind == IdentityDecisionKind.ConfirmSameIdentity && persistent == null)
            {
                throw new ArgumentException(
                    Loc.T("draft.error.confirmNeedsIdentity"), nameof(persistentIdentityId));
            }

            if (kind == IdentityDecisionKind.RejectSameIdentity && persistent != null)
            {
                // A rejection asserts that these are not the same clash. Carrying an identity id would
                // record the opposite of what the reviewer decided.
                throw new ArgumentException(
                    Loc.T("draft.error.rejectCarriesIdentity"), nameof(persistentIdentityId));
            }

            Kind = kind;
            DecisionId = decisionId.Trim();
            LeftRunId = leftRunId.Trim();
            LeftOccurrenceIndex = leftOccurrenceIndex;
            RightRunId = rightRunId.Trim();
            RightOccurrenceIndex = rightOccurrenceIndex;
            ReviewerAlias = reviewerAlias.Trim();
            PersistentIdentityId = persistent;
            Reason = Trimmed(reason);
        }

        /// <summary>
        /// Parses an occurrence index as the engine defines it: zero-based, never negative. This is a
        /// format check only — whether the index is inside the run is validated against the snapshots.
        /// </summary>
        public static bool TryParseOccurrenceIndex(string? text, out int index)
        {
            index = -1;

            if (string.IsNullOrWhiteSpace(text))
            {
                return false;
            }

            if (!int.TryParse(text!.Trim(), NumberStyles.None, CultureInfo.InvariantCulture, out int parsed))
            {
                return false;
            }

            if (parsed < 0)
            {
                return false;
            }

            index = parsed;
            return true;
        }

        private static void Require(string value, string parameterName, string message)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                throw new ArgumentException(message, parameterName);
            }
        }

        private static void RequireOccurrenceIndex(int index, string parameterName)
        {
            if (index < 0)
            {
                throw new ArgumentOutOfRangeException(
                    parameterName, index, Loc.T("draft.error.negativeOccurrenceIndex"));
            }
        }

        private static string? Trimmed(string? value) =>
            string.IsNullOrWhiteSpace(value) ? null : value!.Trim();
    }
}
