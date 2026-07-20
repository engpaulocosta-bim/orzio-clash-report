using System.Collections.Generic;
using OrzioClashReport.Core.Model;

namespace OrzioClashReport.Core.Abstractions
{
    /// <summary>
    /// Orchestrates deterministic longitudinal sequence analysis by composing sequence comparison, continuity
    /// projection, and continuity path assembly over an explicitly ordered list of coordination runs.
    ///
    /// <para>
    /// The order of <paramref name="runs"/> passed to <see cref="Analyze"/> is the only authority. Implementations
    /// must not sort, reorder, deduplicate, infer chronology from timestamps, run ids, revisions, filenames, or
    /// filesystem metadata, or compare non-adjacent runs.
    /// </para>
    ///
    /// <para>
    /// This port only composes the three existing stages:
    /// <see cref="IClashRunSequenceComparer"/>, then <see cref="IClashRunSequenceContinuityProjector"/>, then
    /// <see cref="IClashRunSequenceContinuityPathAssembler"/>. It does not persist derived state and does not
    /// create history, a Clash Ledger, a multi-run lifecycle, or stable/persistent clash identity.
    /// </para>
    ///
    /// <para>
    /// Execution is synchronous and deterministic, with no I/O, filesystem, network, clock access, or
    /// randomness. Implementations must fail fast and must not return a partial
    /// <see cref="ClashRunSequenceAnalysisResult"/>.
    /// </para>
    /// </summary>
    public interface IClashRunSequenceAnalyzer
    {
        ClashRunSequenceAnalysisResult Analyze(IReadOnlyList<CoordinationRun> runs);
    }
}
