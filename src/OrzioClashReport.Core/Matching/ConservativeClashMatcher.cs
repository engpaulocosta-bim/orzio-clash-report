using System;
using OrzioClashReport.Core.Abstractions;
using OrzioClashReport.Core.Model;

namespace OrzioClashReport.Core.Matching
{
    /// <summary>
    /// A conservative, pairwise <see cref="IClashMatcher"/>: produces a candidate only when the clash test
    /// name, the revision-free model-identity pair, and the model-aligned element-id pair all agree. The
    /// source clash GUID is supplemental evidence only -- an equal GUID can raise the resulting confidence
    /// from <see cref="ClashMatchConfidence.Medium"/> to <see cref="ClashMatchConfidence.High"/>, but an
    /// unequal or missing GUID never creates or destroys a candidate on its own. This matcher never returns
    /// <see cref="ClashMatchConfidence.Low"/>: it only accepts pairs where all three required signals agree.
    /// </summary>
    public sealed class ConservativeClashMatcher : IClashMatcher
    {
        private enum ModelAlignment
        {
            None,
            Direct,
            Swapped,
            Ambiguous
        }

        public ClashMatchAssessment? Assess(ClashOccurrence previousOccurrence, ClashOccurrence currentOccurrence)
        {
            if (previousOccurrence == null)
            {
                throw new ArgumentNullException(nameof(previousOccurrence));
            }

            if (currentOccurrence == null)
            {
                throw new ArgumentNullException(nameof(currentOccurrence));
            }

            if (!string.Equals(previousOccurrence.ClashTestName, currentOccurrence.ClashTestName, StringComparison.OrdinalIgnoreCase))
            {
                return null;
            }

            var modelAlignment = DetermineModelAlignment(previousOccurrence, currentOccurrence);
            if (modelAlignment == ModelAlignment.None)
            {
                return null;
            }

            string? previousElementA = NormalizeToNullIfBlank(previousOccurrence.Clash.ElementA.ElementId);
            string? previousElementB = NormalizeToNullIfBlank(previousOccurrence.Clash.ElementB.ElementId);
            string? currentElementA = NormalizeToNullIfBlank(currentOccurrence.Clash.ElementA.ElementId);
            string? currentElementB = NormalizeToNullIfBlank(currentOccurrence.Clash.ElementB.ElementId);

            if (previousElementA == null || previousElementB == null || currentElementA == null || currentElementB == null)
            {
                return null;
            }

            bool directElementsMatch = modelAlignment != ModelAlignment.Swapped
                && string.Equals(previousElementA, currentElementA, StringComparison.Ordinal)
                && string.Equals(previousElementB, currentElementB, StringComparison.Ordinal);

            bool swappedElementsMatch = modelAlignment != ModelAlignment.Direct
                && string.Equals(previousElementA, currentElementB, StringComparison.Ordinal)
                && string.Equals(previousElementB, currentElementA, StringComparison.Ordinal);

            if (!directElementsMatch && !swappedElementsMatch)
            {
                return null;
            }

            var clashTestEvidence = new MatchEvidence(
                MatchEvidenceKind.ClashTestName,
                MatchEvidenceVerdict.Supports,
                "Clash test names match using ordinal case-insensitive comparison.",
                previousOccurrence.ClashTestName,
                currentOccurrence.ClashTestName);

            string previousModelPair = $"{previousOccurrence.ModelA.Identity} | {previousOccurrence.ModelB.Identity}";
            string currentModelPair = $"{currentOccurrence.ModelA.Identity} | {currentOccurrence.ModelB.Identity}";
            var modelIdentityEvidence = new MatchEvidence(
                MatchEvidenceKind.ModelIdentityPair,
                MatchEvidenceVerdict.Supports,
                DescribeModelAlignment(modelAlignment),
                previousModelPair,
                currentModelPair);

            string previousElementPair = $"{previousElementA} | {previousElementB}";
            string currentElementPair = $"{currentElementA} | {currentElementB}";
            var elementIdentifierEvidence = new MatchEvidence(
                MatchEvidenceKind.ElementIdentifierPair,
                MatchEvidenceVerdict.Supports,
                DescribeElementAlignment(modelAlignment),
                previousElementPair,
                currentElementPair);

            var guidEvidence = BuildGuidEvidence(previousOccurrence, currentOccurrence, out var guidVerdict);

            var confidence = guidVerdict == MatchEvidenceVerdict.Supports
                ? ClashMatchConfidence.High
                : ClashMatchConfidence.Medium;

            return new ClashMatchAssessment(
                previousOccurrence,
                currentOccurrence,
                confidence,
                new[] { clashTestEvidence, modelIdentityEvidence, elementIdentifierEvidence, guidEvidence });
        }

        private static ModelAlignment DetermineModelAlignment(ClashOccurrence previous, ClashOccurrence current)
        {
            bool direct = previous.ModelA.Identity.Equals(current.ModelA.Identity)
                && previous.ModelB.Identity.Equals(current.ModelB.Identity);
            bool swapped = previous.ModelA.Identity.Equals(current.ModelB.Identity)
                && previous.ModelB.Identity.Equals(current.ModelA.Identity);

            if (direct && swapped)
            {
                return ModelAlignment.Ambiguous;
            }

            if (direct)
            {
                return ModelAlignment.Direct;
            }

            if (swapped)
            {
                return ModelAlignment.Swapped;
            }

            return ModelAlignment.None;
        }

        private static string DescribeModelAlignment(ModelAlignment alignment) => alignment switch
        {
            ModelAlignment.Direct => "Revision-free model identities match directly (A to A, B to B) across the two exports.",
            ModelAlignment.Swapped => "Revision-free model identities match with the A/B sides swapped across the two exports.",
            ModelAlignment.Ambiguous => "Both sides of this clash share the same revision-free model identity (self-clash); "
                + "the A/B orientation between the two exports is ambiguous.",
            _ => throw new ArgumentOutOfRangeException(nameof(alignment), alignment, "Unreachable: caller only reaches this with a matched alignment.")
        };

        private static string DescribeElementAlignment(ModelAlignment alignment) => alignment switch
        {
            ModelAlignment.Direct => "Element identifiers match directly (A to A, B to B), consistent with the accepted model alignment.",
            ModelAlignment.Swapped => "Element identifiers match with the A/B sides swapped, consistent with the accepted model alignment.",
            ModelAlignment.Ambiguous => "Element identifiers match under an ambiguous self-clash model alignment; "
                + "a single A/B orientation is not assumed.",
            _ => throw new ArgumentOutOfRangeException(nameof(alignment), alignment, "Unreachable: caller only reaches this with a matched alignment.")
        };

        private static MatchEvidence BuildGuidEvidence(
            ClashOccurrence previousOccurrence, ClashOccurrence currentOccurrence, out MatchEvidenceVerdict verdict)
        {
            string? previousGuid = NormalizeToNullIfBlank(previousOccurrence.Clash.Guid);
            string? currentGuid = NormalizeToNullIfBlank(currentOccurrence.Clash.Guid);

            string description;
            if (previousGuid == null || currentGuid == null)
            {
                verdict = MatchEvidenceVerdict.Unavailable;
                description = "Source clash GUID is missing on one or both sides. It is supplemental evidence only "
                    + "and has not been proven as stable cross-run identity.";
            }
            else if (string.Equals(previousGuid, currentGuid, StringComparison.Ordinal))
            {
                verdict = MatchEvidenceVerdict.Supports;
                description = "Source clash GUIDs are equal. Source GUID is supplemental evidence only "
                    + "and has not been proven as stable cross-run identity.";
            }
            else
            {
                verdict = MatchEvidenceVerdict.Contradicts;
                description = "Source clash GUIDs differ. Source GUID is supplemental evidence only "
                    + "and has not been proven as stable cross-run identity.";
            }

            return new MatchEvidence(MatchEvidenceKind.SourceClashGuid, verdict, description, previousGuid, currentGuid);
        }

        private static string? NormalizeToNullIfBlank(string? value)
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
