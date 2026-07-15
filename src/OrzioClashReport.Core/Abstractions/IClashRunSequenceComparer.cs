using System.Collections.Generic;
using OrzioClashReport.Core.Model;

namespace OrzioClashReport.Core.Abstractions
{
    /// <summary>
    /// Compares an explicitly ordered sequence of <see cref="CoordinationRun"/>s by traversing adjacent pairs
    /// only, producing an immutable <see cref="ClashRunSequenceComparisonResult"/>.
    ///
    /// <para>
    /// <b>Caller-declared order.</b> The order of <paramref name="runs"/> passed to <see cref="Compare"/> is
    /// explicit and authoritative. Implementations must not reorder the sequence by
    /// <see cref="CoordinationRun.CreatedAt"/>, <see cref="CoordinationRun.RunId"/>, revision, source filename,
    /// or any filesystem metadata. This port does not know about run-index JSON or any other persistence
    /// format; it receives only the sequence already ordered by the caller.
    /// </para>
    ///
    /// <para>
    /// <b>Adjacent pairs only.</b> For a sequence <c>A, B, C, D</c>, implementations compare exactly
    /// <c>A -&gt; B</c>, <c>B -&gt; C</c>, <c>C -&gt; D</c>. Non-adjacent pairs such as <c>A -&gt; C</c> or
    /// <c>A -&gt; D</c>, and reversed pairs such as <c>B -&gt; A</c>, are never compared.
    /// </para>
    ///
    /// <para>
    /// <b>Argument contract.</b> Implementations must reject a <c>null</c> <paramref name="runs"/> by throwing
    /// <see cref="System.ArgumentNullException"/>, and must reject a <paramref name="runs"/> with fewer than two
    /// entries, or containing any <c>null</c> entry, by throwing <see cref="System.ArgumentException"/>.
    /// </para>
    ///
    /// <para>
    /// <b>Duplicates allowed.</b> A sequence may repeat the same <see cref="CoordinationRun"/> reference at more
    /// than one position (for example <c>A, A, B</c>); implementations must not deduplicate by reference,
    /// <see cref="CoordinationRun.RunId"/>, <see cref="CoordinationRun.CreatedAt"/>, or content.
    /// </para>
    ///
    /// <para>
    /// <b>Purity.</b> Implementations of <see cref="Compare"/> must be synchronous, perform no I/O, filesystem,
    /// network, or clock access, introduce no randomness, and must not mutate any run in <paramref name="runs"/>.
    /// For the same ordered sequence and the same implementation/configuration, the result must be
    /// deterministic.
    /// </para>
    /// </summary>
    public interface IClashRunSequenceComparer
    {
        ClashRunSequenceComparisonResult Compare(IReadOnlyList<CoordinationRun> runs);
    }
}
