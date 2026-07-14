using System;

namespace OrzioClashReport.Persistence.RunSnapshotJson
{
    /// <summary>
    /// Thrown when a coordination run snapshot JSON document is malformed, uses an unsupported schema, is
    /// missing required fields, carries invalid model indexes or duplicate property-entry keys, fails a Core
    /// validation rule while the run is rebuilt, or when a snapshot Save/Load hits a format-context failure
    /// (including an attempt to overwrite an existing snapshot path). It is deliberately distinct from the run
    /// manifest adapter's exception: the two JSON adapters are independent.
    /// </summary>
    public sealed class CoordinationRunSnapshotFormatException : Exception
    {
        public CoordinationRunSnapshotFormatException(string message)
            : base(message)
        {
        }

        public CoordinationRunSnapshotFormatException(string message, Exception innerException)
            : base(message, innerException)
        {
        }
    }
}
