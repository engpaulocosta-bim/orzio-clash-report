using System.Collections.Generic;
using OrzioClashReport.Core.Model;

namespace OrzioClashReport.Core.Governance
{
    /// <summary>
    /// Validates an already-loaded <see cref="IdentityGovernanceDocument"/>'s project binding and every decision's
    /// evidence endpoints against an already-loaded, explicitly ordered set of indexed <see cref="CoordinationRun"/>
    /// snapshots.
    ///
    /// <para>
    /// <b>Read-only evidence validation, not identity inference.</b> Implementations validate only that
    /// <paramref name="expectedProjectId"/> matches <see cref="IdentityGovernanceDocument.ProjectId"/>, and that
    /// each decision's <see cref="ClashEvidenceEndpoint"/> resolves to a real occurrence slot inside exactly one
    /// indexed run. They never infer, propagate, or persist identity; never validate matcher candidacy,
    /// run adjacency, left/right ordering intent, transitivity across decisions, graph conflicts, identity
    /// merges, reopening, decision supersession, reviewer identity, timestamps, or responsibility; and never
    /// project decisions into a report or create a Clash Ledger.
    /// </para>
    ///
    /// <para>
    /// <b>Explicit inputs, no I/O.</b> Implementations receive the governance document and indexed runs already
    /// loaded by the caller. They perform no file-system, JSON, CLI, project-catalog, run-index, snapshot, or
    /// HTML I/O themselves, and depend on no Navisworks type.
    /// </para>
    ///
    /// <para>
    /// <b>Works for any indexed-run count.</b> Implementations must work correctly with zero decisions, exactly
    /// one indexed run, or many indexed runs, and must never require at least two indexed runs -- this is
    /// evidence validation, not longitudinal comparison.
    /// </para>
    ///
    /// <para>
    /// <b>Purity and determinism.</b> Implementations must be synchronous, perform no I/O, filesystem, network,
    /// or clock access, introduce no randomness, and must not mutate <paramref name="governance"/> or
    /// <paramref name="indexedRuns"/>. For the same inputs, the result must be deterministic.
    /// </para>
    /// </summary>
    public interface IIdentityGovernanceEvidenceValidator
    {
        IdentityGovernanceEvidenceValidationResult Validate(
            string expectedProjectId,
            IdentityGovernanceDocument governance,
            IReadOnlyList<CoordinationRun> indexedRuns);
    }
}
