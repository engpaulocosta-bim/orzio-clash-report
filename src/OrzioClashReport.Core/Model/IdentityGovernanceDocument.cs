using System;
using System.Collections.Generic;

namespace OrzioClashReport.Core.Model
{
    /// <summary>Immutable project-scoped set of explicit human identity decisions over immutable clash evidence.</summary>
    public sealed class IdentityGovernanceDocument
    {
        public string ProjectId { get; }

        public IReadOnlyList<HumanIdentityDecision> Decisions { get; }

        public IdentityGovernanceDocument(string projectId, IReadOnlyList<HumanIdentityDecision> decisions)
        {
            ProjectId = RequireIdentifier(projectId, nameof(projectId));

            if (decisions == null)
            {
                throw new ArgumentNullException(nameof(decisions));
            }

            var copy = new List<HumanIdentityDecision>(decisions.Count);
            var decisionIds = new HashSet<string>(StringComparer.Ordinal);
            var orderedPairs = new Dictionary<string, string>(StringComparer.Ordinal);

            for (int i = 0; i < decisions.Count; i++)
            {
                HumanIdentityDecision decision = decisions[i]
                    ?? throw new ArgumentException($"Decision at index {i} cannot be null.", nameof(decisions));

                if (!string.Equals(ProjectId, decision.ProjectId, StringComparison.Ordinal))
                {
                    throw new ArgumentException(
                        $"Decision at index {i} targets project '{decision.ProjectId}', which does not match document project '{ProjectId}'.",
                        nameof(decisions));
                }

                if (!decisionIds.Add(decision.DecisionId))
                {
                    throw new ArgumentException(
                        $"Decision id '{decision.DecisionId}' is duplicated.",
                        nameof(decisions));
                }

                string pairKey = BuildOrderedPairKey(decision.LeftEvidence, decision.RightEvidence);
                if (orderedPairs.TryGetValue(pairKey, out string? existingDecisionId))
                {
                    throw new ArgumentException(
                        $"Ordered evidence pair '{decision.LeftEvidence}' -> '{decision.RightEvidence}' is already used by decision '{existingDecisionId}'.",
                        nameof(decisions));
                }

                orderedPairs.Add(pairKey, decision.DecisionId);
                copy.Add(decision);
            }

            Decisions = copy.AsReadOnly();
        }

        /// <summary>Human-readable representation for logs and test output. Not a storage-path contract.</summary>
        public override string ToString() => $"{ProjectId} ({Decisions.Count} decisions)";

        private static string BuildOrderedPairKey(ClashEvidenceEndpoint left, ClashEvidenceEndpoint right) =>
            $"{Segment(left.RunId)}|{left.OccurrenceIndex}|{Segment(right.RunId)}|{right.OccurrenceIndex}";

        private static string Segment(string value) => $"{value.Length}:{value}";

        private static string RequireIdentifier(string value, string paramName)
        {
            if (value == null)
            {
                throw new ArgumentNullException(paramName);
            }

            string trimmed = value.Trim();
            if (trimmed.Length == 0)
            {
                throw new ArgumentException("Value cannot be empty or whitespace.", paramName);
            }

            if (trimmed.Length > HumanIdentityDecision.MaxIdentifierLength)
            {
                throw new ArgumentException(
                    $"Value cannot exceed {HumanIdentityDecision.MaxIdentifierLength} characters after trimming.",
                    paramName);
            }

            return trimmed;
        }
    }
}
