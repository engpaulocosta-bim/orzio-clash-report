using System;

namespace OrzioClashReport.Persistence.ProjectCatalogJson
{
    /// <summary>Represents a strict project-catalog JSON format or persistence error.</summary>
    public sealed class ProjectCatalogFormatException : Exception
    {
        public ProjectCatalogFormatException(string message)
            : base(message)
        {
        }

        public ProjectCatalogFormatException(string message, Exception innerException)
            : base(message, innerException)
        {
        }
    }
}
