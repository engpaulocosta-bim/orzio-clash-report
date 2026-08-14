using System;
using OrzioClashReport.Launcher.Contracts.Operations;

namespace OrzioClashReport.Launcher.Application.Presentation
{
    /// <summary>
    /// How a confirmation and a rejection are told apart. The two differ by glyph, by label and by
    /// severity: never by colour alone, because these are the decisions with the most weight in the
    /// whole product and the least room for a misread.
    /// </summary>
    public sealed class IdentityDecisionPresentation
    {
        public IdentityDecisionKind Kind { get; }

        /// <summary>The exact value passed to <c>--decision-kind</c>. Never translated.</summary>
        public string CanonicalValue { get; }

        public string Glyph { get; }
        public string Label { get; }
        public string Description { get; }
        public LauncherSeverity Severity { get; }
        public bool RequiresPersistentIdentityId { get; }

        private IdentityDecisionPresentation(
            IdentityDecisionKind kind,
            string glyph,
            string label,
            string description,
            LauncherSeverity severity,
            bool requiresPersistentIdentityId)
        {
            Kind = kind;
            CanonicalValue = kind.ToString();
            Glyph = glyph;
            Label = label;
            Description = description;
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
                        "Confirmar mesma identidade",
                        "Regista que estas duas ocorrências são, para si, o mesmo clash. Exige um identificador persistente.",
                        LauncherSeverity.Positive,
                        requiresPersistentIdentityId: true);

                case IdentityDecisionKind.RejectSameIdentity:
                    return new IdentityDecisionPresentation(
                        kind,
                        "✕",
                        "Rejeitar mesma identidade",
                        "Regista que estas duas ocorrências não são o mesmo clash. Nunca transporta identificador persistente.",
                        LauncherSeverity.Critical,
                        requiresPersistentIdentityId: false);

                default:
                    throw new ArgumentOutOfRangeException(nameof(kind), kind, "Unknown identity decision kind.");
            }
        }
    }
}
