using System;
using System.Collections.Generic;
using OrzioClashReport.Core.Model;

namespace OrzioClashReport.Core.Governance
{
    /// <summary>
    /// Converts explicit ConfirmSameIdentity decisions into presentation records.
    /// Rejected decisions are omitted and no relationship is created beyond each recorded pair.
    /// </summary>
    public sealed class DeterministicLongitudinalIdentityConfirmationProjector
    {
        public IReadOnlyList<LongitudinalIdentityConfirmation> Project(IdentityGovernanceDocument governance)
        {
            if (governance == null)
            {
                throw new ArgumentNullException(nameof(governance));
            }

            var confirmations = new List<LongitudinalIdentityConfirmation>();
            foreach (HumanIdentityDecision decision in governance.Decisions)
            {
                if (decision.DecisionKind != HumanIdentityDecisionKind.ConfirmSameIdentity)
                {
                    continue;
                }

                if (decision.PersistentIdentityId == null)
                {
                    throw new InvalidOperationException(
                        $"Confirmation decision '{decision.DecisionId}' has no persistent identity id.");
                }

                confirmations.Add(new LongitudinalIdentityConfirmation(
                    decision.DecisionId,
                    decision.PersistentIdentityId,
                    decision.LeftEvidence,
                    decision.RightEvidence,
                    decision.ReviewerAlias,
                    decision.Reason));
            }

            return confirmations.AsReadOnly();
        }
    }
}
