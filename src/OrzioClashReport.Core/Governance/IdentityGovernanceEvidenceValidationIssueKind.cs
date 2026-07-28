namespace OrzioClashReport.Core.Governance
{
    /// <summary>Enumerates the explicit kinds of read-only evidence validation issue this stage can detect.</summary>
    public enum IdentityGovernanceEvidenceValidationIssueKind
    {
        /// <summary>The governance document's declared project id does not match the expected project id.</summary>
        ProjectIdMismatch,

        /// <summary>Two or more indexed snapshots share the same run id, making run-id resolution ambiguous.</summary>
        DuplicateIndexedRunId,

        /// <summary>An evidence endpoint references a run id that is not indexed exactly once.</summary>
        RunNotIndexed,

        /// <summary>An evidence endpoint references an occurrence index outside its run's occurrence range.</summary>
        OccurrenceIndexOutOfRange,
    }
}
