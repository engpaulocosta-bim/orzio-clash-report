using OrzioClashReport.Core.Model;

namespace OrzioClashReport.Core.Abstractions
{
    /// <summary>
    /// Assembles an already-derived <see cref="ClashRunSequenceContinuityResult"/> into the complete set of
    /// disjoint maximal continuity paths its <see cref="ClashRunSequenceContinuityResult.Links"/> imply: two
    /// links belong to the same path only when one's outgoing selected match is the exact same object reference
    /// as the other's incoming selected match at the immediately following comparison boundary.
    ///
    /// <para>
    /// <b>Input authority.</b> The <paramref name="continuityResult"/> passed to <see cref="Assemble"/> is
    /// already fully derived and independently structurally validated: its <see cref="ClashRunSequenceContinuityResult.Links"/>
    /// are authoritative and are never recomputed by this port. This port does not know about run-index JSON,
    /// snapshot files, or any other persistence format -- it consumes only the in-memory
    /// <see cref="ClashRunSequenceContinuityResult"/>.
    /// </para>
    ///
    /// <para>
    /// <b>Exact selected-match reference connectivity only.</b> Two links connect only when the first link's
    /// <see cref="SelectedMatchContinuityLink.OutgoingComparisonIndex"/> equals the second link's
    /// <see cref="SelectedMatchContinuityLink.IncomingComparisonIndex"/> (consecutive boundaries only) and the
    /// first link's <see cref="SelectedMatchContinuityLink.OutgoingSelectedMatch"/> is the exact same object
    /// reference as the second link's <see cref="SelectedMatchContinuityLink.IncomingSelectedMatch"/>. Connection
    /// is never established by <see cref="CoordinationRun.RunId"/>, <see cref="CoordinationRun.CreatedAt"/>,
    /// candidate indices alone, occurrence reference alone, candidate or assessment value equality, source clash
    /// GUID, confidence, evidence, <c>ToString</c>, hash, or fingerprint.
    /// </para>
    ///
    /// <para>
    /// <b>Complete disjoint partition of maximal paths.</b> Every link in <see cref="ClashRunSequenceContinuityResult.Links"/>
    /// appears in exactly one resulting path, and every resulting path is maximal: it is never split into two
    /// paths where one continuous chain of exact-reference connectivity exists, and it never merges two
    /// genuinely disconnected chains into one.
    /// </para>
    ///
    /// <para>
    /// <b>Canonical path order.</b> Paths are ordered by the position of their first link in
    /// <see cref="ClashRunSequenceContinuityResult.Links"/> -- never by path length, <see cref="CoordinationRun.RunId"/>,
    /// <see cref="CoordinationRun.CreatedAt"/>, confidence, source GUID, or occurrence details.
    /// </para>
    ///
    /// <para>
    /// <b>Purity.</b> Implementations of <see cref="Assemble"/> must be synchronous, perform no I/O, filesystem,
    /// network, or clock access, introduce no randomness, and must not mutate <paramref name="continuityResult"/>
    /// or anything it references. They call no <see cref="IClashMatcher"/>, <see cref="IClashRunComparer"/>,
    /// <see cref="IClashLifecycleClassifier"/>, <see cref="IClashRunSequenceComparer"/>, or
    /// <see cref="IClashRunSequenceContinuityProjector"/> -- matching, run comparison, lifecycle classification,
    /// sequence comparison, and continuity projection have already happened by the time this port runs. For the
    /// same input, the result must be deterministic.
    /// </para>
    /// </summary>
    public interface IClashRunSequenceContinuityPathAssembler
    {
        ClashRunSequenceContinuityPathsResult Assemble(ClashRunSequenceContinuityResult continuityResult);
    }
}
