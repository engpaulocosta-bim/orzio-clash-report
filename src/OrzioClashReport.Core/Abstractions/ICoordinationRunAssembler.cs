using OrzioClashReport.Core.Model;

namespace OrzioClashReport.Core.Abstractions
{
    /// <summary>
    /// Converts an already-parsed <see cref="ClashReportDocument"/> plus a declared <see cref="RunManifest"/>
    /// into a validated <see cref="CoordinationRun"/>. An implementation associates each side of every clash
    /// with the exact <see cref="ModelRevision"/> from the manifest that produced it, preserving document
    /// order and A/B orientation; it performs no I/O, is synchronous, pure, and deterministic for the same
    /// inputs; it rejects null arguments; and it never mutates <paramref name="document"/> or
    /// <paramref name="manifest"/>-shaped inputs it consumes. Assembling is not matching, comparing, or
    /// classifying: those remain the responsibility of <see cref="IClashMatcher"/>,
    /// <see cref="IClashRunComparer"/>, and <see cref="IClashLifecycleClassifier"/>. An implementation never
    /// infers a model's revision or identity from a filename or path; the manifest is the sole authority for
    /// that declaration.
    /// </summary>
    public interface ICoordinationRunAssembler
    {
        CoordinationRun Assemble(
            ClashReportDocument document,
            RunManifest manifest);
    }
}
