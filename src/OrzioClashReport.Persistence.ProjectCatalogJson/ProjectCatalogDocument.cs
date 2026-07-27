using System;

namespace OrzioClashReport.Persistence.ProjectCatalogJson
{
    /// <summary>Represents one immutable operational project catalog that references a run index and a report destination.</summary>
    public sealed class ProjectCatalogDocument
    {
        public ProjectCatalogDocument(
            string projectId,
            string displayName,
            string runIndexPath,
            string longitudinalReportPath)
        {
            ProjectId = ValidateHumanValue(projectId, nameof(projectId));
            DisplayName = ValidateHumanValue(displayName, nameof(displayName));

            if (runIndexPath == null)
            {
                throw new ArgumentNullException(nameof(runIndexPath));
            }

            if (longitudinalReportPath == null)
            {
                throw new ArgumentNullException(nameof(longitudinalReportPath));
            }

            ProjectCatalogPathReferenceValidator.EnsureCanonicalReferenceForConstructor(runIndexPath, nameof(runIndexPath));
            ProjectCatalogPathReferenceValidator.EnsureCanonicalReferenceForConstructor(longitudinalReportPath, nameof(longitudinalReportPath));

            if (string.Equals(runIndexPath, longitudinalReportPath, StringComparison.Ordinal))
            {
                throw new ArgumentException(
                    "Run index path and longitudinal report path must be different.",
                    nameof(longitudinalReportPath));
            }

            RunIndexPath = runIndexPath;
            LongitudinalReportPath = longitudinalReportPath;
        }

        public string ProjectId { get; }

        public string DisplayName { get; }

        public string RunIndexPath { get; }

        public string LongitudinalReportPath { get; }

        private static string ValidateHumanValue(string value, string parameterName)
        {
            if (value == null)
            {
                throw new ArgumentNullException(parameterName);
            }

            if (value.Trim().Length == 0)
            {
                throw new ArgumentException("Value cannot be empty or whitespace.", parameterName);
            }

            if (value.Length != value.Trim().Length)
            {
                throw new ArgumentException(
                    $"Value for '{parameterName}' cannot have leading or trailing whitespace.",
                    parameterName);
            }

            return value;
        }
    }
}
