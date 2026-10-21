using System;
using OrzioClashReport.Launcher.Application.Localization;
using OrzioClashReport.Launcher.Contracts.Operations;

namespace OrzioClashReport.Launcher.Application.Presentation
{
    /// <summary>
    /// How a confirmation and a rejection are told apart. The two differ by glyph, by label and by
    /// severity: never by colour alone, because these are the decisions with the most weight in the
    /// whole product and the least room for a misread.
    /// </summary>
    /// <remarks>
    /// <see cref="Label"/> and <see cref="Description"/> follow the interface language. The canonical
    /// value, glyph, severity and the persistent-identity rule do not: they are part of the decision.
    /// </remarks>
    public sealed class IdentityDecisionPresentation
    {
        private readonly string _labelKey;
        private readonly string _descriptionKey;

        public IdentityDecisionKind Kind { get; }

        /// <summary>The exact value passed to <c>--decision-kind</c>. Never translated.</summary>
        public string CanonicalValue { get; }

        public string Glyph { get; }
        public string Label => Loc.T(_labelKey);
        public string Description => Loc.T(_descriptionKey);
        public LauncherSeverity Severity { get; }
        public bool RequiresPersistentIdentityId { get; }

        private IdentityDecisionPresentation(
            IdentityDecisionKind kind,
            string glyph,
            string labelKey,
            string descriptionKey,
            LauncherSeverity severity,
            bool requiresPersistentIdentityId)
        {
            Kind = kind;
            CanonicalValue = kind.ToString();
            Glyph = glyph;
            _labelKey = labelKey;
            _descriptionKey = descriptionKey;
            Severity = severity;
            RequiresPersistentIdentityId = requiresPersistentIdentityId;
        }

        public static IdentityDecisionPresentation For(IdentityDecisionKind kind)
        {
            switch (kind)
            {
                case IdentityDecisionKind.ConfirmSameIdentity:
                    return new IdentityDecisionPresentation(
                        kind,
                        "✓",
                        "decision.confirm.label",
                        "decision.confirm.description",
                        LauncherSeverity.Positive,
                        requiresPersistentIdentityId: true);

                case IdentityDecisionKind.RejectSameIdentity:
                    return new IdentityDecisionPresentation(
                        kind,
                        "✕",
                        "decision.reject.label",
                        "decision.reject.description",
                        LauncherSeverity.Critical,
                        requiresPersistentIdentityId: false);

                default:
                    throw new ArgumentOutOfRangeException(nameof(kind), kind, "Unknown identity decision kind.");
            }
        }
    }
}
