using System;

namespace OrzioClashReport.Persistence.RunIndexJson
{
    /// <summary>Thrown when a run-index JSON document or run-index file operation violates the strict adapter contract.</summary>
    public sealed class RunIndexFormatException : Exception
    {
        public RunIndexFormatException(string message)
            : base(message)
        {
        }

        public RunIndexFormatException(string message, Exception innerException)
            : base(message, innerException)
        {
        }
    }
}
