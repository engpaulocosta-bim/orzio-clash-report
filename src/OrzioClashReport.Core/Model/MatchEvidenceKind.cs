namespace OrzioClashReport.Core.Model
{
    /// <summary>The category of signal a <see cref="MatchEvidence"/> item compares between two clash occurrences.</summary>
    public enum MatchEvidenceKind
    {
        /// <summary>
        /// Compares the clash GUID reported by the source. Equal GUIDs are a potentially strong signal, but are
        /// not treated as a stable identity until validated on sequential real exports.
        /// </summary>
        SourceClashGuid,

        /// <summary>Compares the clash test name that produced each occurrence.</summary>
        ClashTestName,

        /// <summary>Compares the revision-free <see cref="ModelIdentity"/> pair involved. Does not compare <see cref="ModelRevision"/> as a stable identity.</summary>
        ModelIdentityPair,

        /// <summary>
        /// Compares whatever element identifiers are available on the A/B sides. Does not assume in this step
        /// that an element id or a property named "GUID" is universally stable.
        /// </summary>
        ElementIdentifierPair,

        /// <summary>Compares spatial position or proximity. No tolerance is defined at this step.</summary>
        SpatialPosition,

        /// <summary>Compares auxiliary element metadata such as category, type, level, or names. No weighting is defined at this step.</summary>
        ElementMetadata
    }
}
