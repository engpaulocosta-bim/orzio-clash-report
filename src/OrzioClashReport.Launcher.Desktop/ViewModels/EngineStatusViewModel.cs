using System;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using OrzioClashReport.Launcher.Contracts.Engine;
using OrzioClashReport.Launcher.Contracts.Ports;

namespace OrzioClashReport.Launcher.Desktop.ViewModels
{
    /// <summary>
    /// The engine's state as the shell presents it. Every state carries a glyph and a sentence, so it
    /// is never distinguishable by colour alone, and the launcher never claims an engine is usable
    /// before the probe says so.
    /// </summary>
    public sealed partial class EngineStatusViewModel : ViewModelBase
    {
        private readonly IEngineProbe _probe;

        [ObservableProperty]
        private EngineStatus _status = EngineStatus.Checking;

        [ObservableProperty]
        private string? _version;

        [ObservableProperty]
        private string? _detail;

        public EngineStatusViewModel(IEngineProbe probe)
        {
            _probe = probe ?? throw new ArgumentNullException(nameof(probe));
        }

        public string Glyph => GlyphFor(Status);

        public string Label => LabelFor(Status);

        public string Description => DescriptionFor(Status, Version, Detail);

        public StatusSeverity Severity => SeverityFor(Status);

        public bool IsNeutral => Severity == StatusSeverity.Neutral;

        public bool IsPositive => Severity == StatusSeverity.Positive;

        public bool IsCaution => Severity == StatusSeverity.Caution;

        public bool IsCritical => Severity == StatusSeverity.Critical;

        /// <summary>Whether operations may be started at all.</summary>
        public bool CanRunOperations => Status == EngineStatus.Ready;

        [RelayCommand]
        public async Task RefreshAsync(CancellationToken cancellationToken)
        {
            Status = EngineStatus.Checking;
            Version = null;
            Detail = null;

            EngineProbeResult result = await _probe.ProbeAsync(cancellationToken).ConfigureAwait(true);

            Version = result.Version;
            Detail = result.Status == EngineStatus.VersionMismatch
                ? BuildMismatchDetail(result)
                : result.Detail;
            Status = result.Status;
        }

        partial void OnStatusChanged(EngineStatus value)
        {
            _ = value;
            RaiseDerived();
        }

        partial void OnVersionChanged(string? value)
        {
            _ = value;
            OnPropertyChanged(nameof(Description));
        }

        partial void OnDetailChanged(string? value)
        {
            _ = value;
            OnPropertyChanged(nameof(Description));
        }

        private void RaiseDerived()
        {
            OnPropertyChanged(nameof(Glyph));
            OnPropertyChanged(nameof(Label));
            OnPropertyChanged(nameof(Description));
            OnPropertyChanged(nameof(Severity));
            OnPropertyChanged(nameof(IsNeutral));
            OnPropertyChanged(nameof(IsPositive));
            OnPropertyChanged(nameof(IsCaution));
            OnPropertyChanged(nameof(IsCritical));
            OnPropertyChanged(nameof(CanRunOperations));
        }

        private static string BuildMismatchDetail(EngineProbeResult result)
        {
            return $"O motor instalado reporta {result.Version} e esta aplicação espera {result.ExpectedVersion}.";
        }

        internal static string GlyphFor(EngineStatus status)
        {
            switch (status)
            {
                case EngineStatus.Checking:
                    return "◌";
                case EngineStatus.Ready:
                    return "✓";
                case EngineStatus.VersionMismatch:
                    return "≠";
                case EngineStatus.IntegrityFailure:
                    return "✕";
                case EngineStatus.Missing:
                    return "∅";
                case EngineStatus.Unsupported:
                    return "!";
                default:
                    throw new ArgumentOutOfRangeException(nameof(status), status, "Unknown engine status.");
            }
        }

        internal static string LabelFor(EngineStatus status)
        {
            switch (status)
            {
                case EngineStatus.Checking:
                    return "A verificar";
                case EngineStatus.Ready:
                    return "Motor pronto";
                case EngineStatus.VersionMismatch:
                    return "Versão diferente";
                case EngineStatus.IntegrityFailure:
                    return "Integridade falhou";
                case EngineStatus.Missing:
                    return "Motor em falta";
                case EngineStatus.Unsupported:
                    return "Motor não suportado";
                default:
                    throw new ArgumentOutOfRangeException(nameof(status), status, "Unknown engine status.");
            }
        }

        internal static StatusSeverity SeverityFor(EngineStatus status)
        {
            switch (status)
            {
                case EngineStatus.Checking:
                    return StatusSeverity.Neutral;
                case EngineStatus.Ready:
                    return StatusSeverity.Positive;
                case EngineStatus.VersionMismatch:
                case EngineStatus.Unsupported:
                    return StatusSeverity.Caution;
                case EngineStatus.IntegrityFailure:
                case EngineStatus.Missing:
                    return StatusSeverity.Critical;
                default:
                    throw new ArgumentOutOfRangeException(nameof(status), status, "Unknown engine status.");
            }
        }

        internal static string DescriptionFor(EngineStatus status, string? version, string? detail)
        {
            if (!string.IsNullOrWhiteSpace(detail))
            {
                return detail!;
            }

            switch (status)
            {
                case EngineStatus.Checking:
                    return "A verificar o motor instalado.";
                case EngineStatus.Ready:
                    return version == null
                        ? "O motor está pronto."
                        : $"O motor {version} está pronto e verificado.";
                case EngineStatus.VersionMismatch:
                    return "O motor instalado não é a versão que esta aplicação espera.";
                case EngineStatus.IntegrityFailure:
                    return "O motor instalado não corresponde à assinatura registada no empacotamento.";
                case EngineStatus.Missing:
                    return "Não foi encontrado nenhum motor nesta instalação.";
                case EngineStatus.Unsupported:
                    return "O motor não respondeu à verificação de versão de forma reconhecível.";
                default:
                    throw new ArgumentOutOfRangeException(nameof(status), status, "Unknown engine status.");
            }
        }
    }
}
