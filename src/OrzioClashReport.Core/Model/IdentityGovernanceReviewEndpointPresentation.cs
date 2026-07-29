using System;
using OrzioClashReport.Core.Governance;

namespace OrzioClashReport.Core.Model
{
    /// <summary>Immutable operational review view of one resolved governance evidence endpoint.</summary>
    public sealed class IdentityGovernanceReviewEndpointPresentation
    {
        public IdentityGovernanceEvidenceEndpointSide Side { get; }

        public string RunId { get; }

        public int OccurrenceIndex { get; }

        public DateTimeOffset RunCreatedAt { get; }

        public string ClashTestName { get; }

        public ClashStatus ClashStatus { get; }

        public string ModelADisplay { get; }

        public string ModelBDisplay { get; }

        public string ElementAId { get; }

        public string? ElementAName { get; }

        public string? ElementALevel { get; }

        public string? ElementASourceModel { get; }

        public string ElementBId { get; }

        public string? ElementBName { get; }

        public string? ElementBLevel { get; }

        public string? ElementBSourceModel { get; }

        public IdentityGovernanceReviewEndpointPresentation(
            IdentityGovernanceEvidenceEndpointSide side,
            string runId,
            int occurrenceIndex,
            DateTimeOffset runCreatedAt,
            string clashTestName,
            ClashStatus clashStatus,
            string modelADisplay,
            string modelBDisplay,
            string elementAId,
            string? elementAName,
            string? elementALevel,
            string? elementASourceModel,
            string elementBId,
            string? elementBName,
            string? elementBLevel,
            string? elementBSourceModel)
        {
            if (occurrenceIndex < 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(occurrenceIndex),
                    occurrenceIndex,
                    "Occurrence index cannot be negative.");
            }

            Side = side;
            RunId = RequireNonBlank(runId, nameof(runId));
            OccurrenceIndex = occurrenceIndex;
            RunCreatedAt = runCreatedAt;
            ClashTestName = RequireNonBlank(clashTestName, nameof(clashTestName));
            ClashStatus = clashStatus;
            ModelADisplay = RequireNonBlank(modelADisplay, nameof(modelADisplay));
            ModelBDisplay = RequireNonBlank(modelBDisplay, nameof(modelBDisplay));
            ElementAId = RequireNonBlank(elementAId, nameof(elementAId));
            ElementAName = NormalizeOptional(elementAName);
            ElementALevel = NormalizeOptional(elementALevel);
            ElementASourceModel = NormalizeOptional(elementASourceModel);
            ElementBId = RequireNonBlank(elementBId, nameof(elementBId));
            ElementBName = NormalizeOptional(elementBName);
            ElementBLevel = NormalizeOptional(elementBLevel);
            ElementBSourceModel = NormalizeOptional(elementBSourceModel);
        }

        private static string RequireNonBlank(string value, string paramName)
        {
            if (value == null)
            {
                throw new ArgumentNullException(paramName);
            }

            string trimmed = value.Trim();
            if (trimmed.Length == 0)
            {
                throw new ArgumentException("Value cannot be empty or whitespace.", paramName);
            }

            return trimmed;
        }

        private static string? NormalizeOptional(string? value)
        {
            if (value == null)
            {
                return null;
            }

            string trimmed = value.Trim();
            return trimmed.Length == 0 ? null : trimmed;
        }
    }
}
