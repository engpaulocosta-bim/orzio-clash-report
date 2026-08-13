using System;
using OrzioClashReport.Launcher.Contracts.Engine;

namespace OrzioClashReport.Launcher.Application.Presentation
{
    /// <summary>
    /// How one <see cref="EngineStatusKind"/> is presented. Glyph and label are both required and both
    /// unique across states, so the badge never communicates through colour alone.
    /// </summary>
    public sealed class EngineStatusPresentation
    {
        public EngineStatusKind Status { get; }
        public string Glyph { get; }
        public string Label { get; }
        public string Explanation { get; }
        public LauncherSeverity Severity { get; }

        private EngineStatusPresentation(
            EngineStatusKind status,
            string glyph,
            string label,
            string explanation,
            LauncherSeverity severity)
        {
            Status = status;
            Glyph = glyph;
            Label = label;
            Explanation = explanation;
            Severity = severity;
        }

        public static EngineStatusPresentation For(EngineStatusKind status)
        {
            switch (status)
            {
                case EngineStatusKind.Checking:
                    return new EngineStatusPresentation(
                        status,
                        "⋯",
                        "A verificar",
                        "A confirmar a versão e a integridade do motor.",
                        LauncherSeverity.Neutral);

                case EngineStatusKind.Ready:
                    return new EngineStatusPresentation(
                        status,
                        "✓",
                        "Motor pronto",
                        "O motor respondeu com a versão esperada e passou a verificação de integridade.",
                        LauncherSeverity.Positive);

                case EngineStatusKind.VersionMismatch:
                    return new EngineStatusPresentation(
                        status,
                        "≠",
                        "Versão diferente",
                        "O motor instalado não é a versão com que esta aplicação foi empacotada. "
                        + "Reinstale para voltar a um par verificado.",
                        LauncherSeverity.Caution);

                case EngineStatusKind.IntegrityFailure:
                    return new EngineStatusPresentation(
                        status,
                        "⚠",
                        "Integridade falhou",
                        "O executável do motor não corresponde ao SHA-256 registado na instalação. "
                        + "Reinstale a aplicação.",
                        LauncherSeverity.Critical);

                case EngineStatusKind.Missing:
                    return new EngineStatusPresentation(
                        status,
                        "✕",
                        "Motor não encontrado",
                        "Não existe motor na pasta de instalação. Reinstale a aplicação.",
                        LauncherSeverity.Critical);

                case EngineStatusKind.Unsupported:
                    return new EngineStatusPresentation(
                        status,
                        "?",
                        "Motor não suportado",
                        "O motor não respondeu no formato publicado. Este sistema pode não conseguir executá-lo.",
                        LauncherSeverity.Critical);

                default:
                    throw new ArgumentOutOfRangeException(nameof(status), status, "Unknown engine status.");
            }
        }
    }
}
