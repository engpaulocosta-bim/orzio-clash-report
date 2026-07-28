using System;

namespace OrzioClashReport.Persistence.IdentityGovernanceJson
{
    /// <summary>Thrown when an identity-governance JSON document or its save/load path context is malformed or invalid.</summary>
    public sealed class IdentityGovernanceFormatException : Exception
    {
        public IdentityGovernanceFormatException(string message)
            : base(message)
        {
        }

        public IdentityGovernanceFormatException(string message, Exception innerException)
            : base(message, innerException)
        {
        }
    }
}
