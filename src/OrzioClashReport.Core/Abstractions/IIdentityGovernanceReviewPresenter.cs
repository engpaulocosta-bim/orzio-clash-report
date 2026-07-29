using System.Collections.Generic;
using OrzioClashReport.Core.Model;

namespace OrzioClashReport.Core.Abstractions
{
    /// <summary>Projects validated identity-governance decisions and indexed runs onto a deterministic operational review presentation.</summary>
    public interface IIdentityGovernanceReviewPresenter
    {
        IdentityGovernanceReviewPresentation Present(
            string projectId,
            string projectDisplayName,
            IdentityGovernanceDocument governance,
            IReadOnlyList<CoordinationRun> indexedRuns);
    }
}
