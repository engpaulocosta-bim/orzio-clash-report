using OrzioClashReport.Core.Model;

namespace OrzioClashReport.Core.Abstractions
{
    /// <summary>
    /// Compares two <see cref="CoordinationRun"/> snapshots and produces an auditable matching result: which
    /// pairs of occurrences are candidates, which subset was selected one-to-one, and which occurrences from
    /// each run were not selected. This is matching, not lifecycle: the result never classifies anything as
    /// new, still open, resolved, reopened, or unverifiable.
    ///
    /// <para>
    /// <b>Explicit roles.</b> <paramref name="previousRun"/> and <paramref name="currentRun"/> in
    /// <see cref="Compare"/> are explicit roles supplied by the caller. Implementations must not infer which
    /// run is "previous" from <see cref="CoordinationRun.CreatedAt"/> or from <see cref="CoordinationRun.RunId"/>
    /// -- the caller decides the order, not the comparer.
    /// </para>
    ///
    /// <para>
    /// <b>Argument contract.</b> Implementations must reject a <c>null</c> <paramref name="previousRun"/> or
    /// <c>null</c> <paramref name="currentRun"/> by throwing <see cref="System.ArgumentNullException"/>.
    /// </para>
    ///
    /// <para>
    /// <b>Purity.</b> Implementations of <see cref="Compare"/> must be synchronous, perform no I/O, filesystem,
    /// network, or clock access, introduce no randomness, and must not mutate either run or its occurrences.
    /// For the same runs and the same implementation/configuration (including whatever
    /// <see cref="IClashMatcher"/> it composes), the result must be deterministic. An implementation may accept
    /// an <see cref="IClashMatcher"/> through its own constructor; this interface has no such dependency in its
    /// signature, and no <c>async</c>, <c>Task</c>, or <see cref="System.Threading.CancellationToken"/> either.
    /// </para>
    /// </summary>
    public interface IClashRunComparer
    {
        ClashRunMatchResult Compare(CoordinationRun previousRun, CoordinationRun currentRun);
    }
}
