using System;

namespace OrzioClashReport.Core.Model
{
    /// <summary>
    /// Thrown when an <see cref="Abstractions.ICoordinationRunAssembler"/> fails to transform an
    /// already-valid <see cref="ClashReportDocument"/> into a <see cref="CoordinationRun"/> using a declared
    /// <see cref="RunManifest"/> -- for example, an unresolvable or ambiguous <see cref="ClashObject.SourceModel"/>,
    /// a blank clash-test batch name, or a domain invariant rejected by <see cref="ClashOccurrence"/> or
    /// <see cref="CoordinationRun"/> itself. Not used for XML parsing failures or run manifest JSON parsing
    /// failures; those remain the responsibility of their own adapters.
    /// </summary>
    public sealed class CoordinationRunAssemblyException : Exception
    {
        public CoordinationRunAssemblyException(string message)
            : base(message)
        {
        }

        public CoordinationRunAssemblyException(string message, Exception innerException)
            : base(message, innerException)
        {
        }
    }
}
