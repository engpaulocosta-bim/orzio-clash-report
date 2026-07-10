using OrzioClashReport.Core.Model;

namespace OrzioClashReport.Core.Abstractions
{
    /// <summary>
    /// Classifies the lifecycle of every slot in an already-produced <see cref="ClashRunMatchResult"/>: each
    /// selected match, each unmatched previous occurrence, and each unmatched current occurrence.
    ///
    /// <para>
    /// This port consumes matching that has already happened. Implementations must not run
    /// <see cref="IClashMatcher"/> or <see cref="IClashRunComparer"/> themselves, must not recompute the
    /// one-to-one selection, and must not promote an alternative candidate into a selected match or otherwise
    /// alter <paramref name="matchResult"/> or any of its candidates.
    /// </para>
    ///
    /// <para>
    /// Implementations must reject a <c>null</c> <paramref name="matchResult"/> by throwing
    /// <see cref="System.ArgumentNullException"/>, and the resulting classification must be conservative:
    /// ambiguous or under-evidenced slots are reported as <see cref="ClashLifecycleStatus.Unverifiable"/> rather
    /// than guessed. The operation must be synchronous, pure, and deterministic -- no I/O, no <c>async</c>, no
    /// <see cref="System.Threading.CancellationToken"/>.
    /// </para>
    /// </summary>
    public interface IClashLifecycleClassifier
    {
        ClashLifecycleResult Classify(ClashRunMatchResult matchResult);
    }
}
