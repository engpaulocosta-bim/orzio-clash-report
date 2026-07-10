using OrzioClashReport.Core.Model;

namespace OrzioClashReport.Core.Abstractions
{
    /// <summary>
    /// Assesses whether a single previous/current pair of <see cref="ClashOccurrence"/>s is a candidate match,
    /// producing an auditable confidence and evidence, or refusing to produce one.
    ///
    /// This port is deliberately pairwise: it evaluates exactly one ordered pair. It does not take a
    /// <see cref="CoordinationRun"/>, does not take a collection of occurrences, and does not traverse two
    /// runs. Traversing the previous and current run's occurrences, calling this matcher over candidate pairs,
    /// preventing an occurrence from being consumed by more than one final match, resolving conflicts between
    /// competing candidates, deciding which match "wins", and deriving any lifecycle classification (new,
    /// still open, resolved, reopened, unverifiable, or otherwise) are the responsibility of a future run
    /// comparer, not of this port.
    ///
    /// <para>
    /// <b>Return value.</b> A non-null <see cref="ClashMatchAssessment"/> means the implementation considers
    /// the pair a candidate relationship and could record at least one auditable <see cref="MatchEvidence"/>
    /// for it -- the assessment's <see cref="ClashMatchConfidence"/> may still be
    /// <see cref="ClashMatchConfidence.Low"/>. <c>null</c> means the implementation refuses to produce a
    /// candidate assessment at all for this pair (for example: insufficient evidence, clearly incompatible
    /// signals, or an unmet precondition of the strategy). <c>null</c> is not the same as
    /// <see cref="ClashMatchConfidence.Low"/>, and it is not the same as
    /// <see cref="MatchEvidenceVerdict.Unavailable"/> -- <see cref="MatchEvidenceVerdict.Unavailable"/> is the
    /// verdict of one piece of evidence inside an assessment that still exists; <c>null</c> means no assessment
    /// exists at all.
    /// </para>
    ///
    /// <para>
    /// <b>Reference preservation.</b> When an implementation returns a non-null assessment, it must construct
    /// it from the exact <paramref name="previousOccurrence"/> and <paramref name="currentOccurrence"/>
    /// instances it received, unchanged: <c>ReferenceEquals(result.PreviousOccurrence, previousOccurrence)</c>
    /// and <c>ReferenceEquals(result.CurrentOccurrence, currentOccurrence)</c> must both hold. Implementations
    /// must not swap previous/current, normalize the A/B sides, clone either occurrence, or substitute a
    /// different occurrence.
    /// </para>
    ///
    /// <para>
    /// <b>Argument contract.</b> Implementations must reject a <c>null</c> <paramref name="previousOccurrence"/>
    /// or <c>null</c> <paramref name="currentOccurrence"/> by throwing <see cref="System.ArgumentNullException"/>.
    /// This interface cannot enforce that itself; it is a documented obligation on every implementation.
    /// </para>
    ///
    /// <para>
    /// <b>Purity.</b> Implementations of <see cref="Assess"/> must be synchronous, perform no I/O, filesystem,
    /// network, or clock access, introduce no randomness, and must not mutate either occurrence. For the same
    /// implementation/configuration and the same immutable input occurrences, the result must be deterministic.
    /// Any configuration a strategy needs belongs in that implementation's constructor, not in this method's
    /// signature.
    /// </para>
    /// </summary>
    public interface IClashMatcher
    {
        ClashMatchAssessment? Assess(ClashOccurrence previousOccurrence, ClashOccurrence currentOccurrence);
    }
}
