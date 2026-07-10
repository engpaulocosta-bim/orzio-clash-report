using System;

namespace OrzioClashReport.Input.RunManifestJson
{
    /// <summary>Thrown when a run manifest JSON document is malformed, structurally invalid, or fails a Core validation rule.</summary>
    public sealed class RunManifestFormatException : Exception
    {
        public RunManifestFormatException(string message)
            : base(message)
        {
        }

        public RunManifestFormatException(string message, Exception innerException)
            : base(message, innerException)
        {
        }
    }
}
