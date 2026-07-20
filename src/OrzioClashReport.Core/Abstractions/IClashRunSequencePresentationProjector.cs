using OrzioClashReport.Core.Model;

namespace OrzioClashReport.Core.Abstractions
{
    /// <summary>
    /// Projects an already-complete <see cref="ClashRunSequenceAnalysisResult"/> onto a lossless presentation
    /// view: ordered runs, adjacent pairwise comparisons, continuity links, continuity paths, every lifecycle
    /// entry, and the entries/selected matches that fall outside any continuity path -- indexed and related for
    /// a future <c>compare-index</c> integration and longitudinal HTML rendering.
    ///
    /// <para>
    /// <b>Single authority.</b> The <paramref name="analysisResult"/> passed to <see cref="Project"/> is the
    /// only authority.
    /// </para>
    ///
    /// <para>
    /// <b>No recomputation.</b> No matching, lifecycle classification, continuity link, or continuity path is
    /// recomputed.
    /// </para>
    ///
    /// <para>
    /// <b>Indexing only.</b> The projection only indexes and relates the exact references it is given.
    /// </para>
    ///
    /// <para>
    /// <b>Order preservation.</b> The order of runs, comparisons, entries, links, and paths in
    /// <paramref name="analysisResult"/> is preserved exactly in the projected result.
    /// </para>
    ///
    /// <para>
    /// <b>Completeness.</b> No lifecycle entry and no selected match can disappear.
    /// </para>
    ///
    /// <para>
    /// <b>Not identity.</b> The result is derived presentation, not history, a ledger, or persistent clash
    /// identity.
    /// </para>
    ///
    /// <para>
    /// <b>Purity.</b> Implementations of <see cref="Project"/> must be synchronous, deterministic, and perform
    /// no I/O.
    /// </para>
    /// </summary>
    public interface IClashRunSequencePresentationProjector
    {
        ClashRunSequencePresentationResult Project(ClashRunSequenceAnalysisResult analysisResult);
    }
}
