using System;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using OrzioClashReport.Launcher.Contracts.Engine;
using OrzioClashReport.Launcher.Contracts.Ports;
using OrzioClashReport.Launcher.Desktop.Localization;

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
        private string? _expectedVersion;

        public EngineStatusViewModel(IEngineProbe probe)
        {
            _probe = probe ?? throw new ArgumentNullException(nameof(probe));
        }

        public string Glyph => GlyphFor(Status);

        public string Label => LabelFor(Status);

        public string Description => DescriptionFor(Status, Version, ExpectedVersion);

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
            ExpectedVersion = null;

            EngineProbeResult result = await _probe.ProbeAsync(cancellationToken).ConfigureAwait(true);

            Version = result.Version;
            ExpectedVersion = result.ExpectedVersion;
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

        partial void OnExpectedVersionChanged(string? value)
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

        internal static string LabelKeyFor(EngineStatus status)
        {
            switch (status)
            {
                case EngineStatus.Checking:
                    return "Engine.Label.Checking";
                case EngineStatus.Ready:
                    return "Engine.Label.Ready";
                case EngineStatus.VersionMismatch:
                    return "Engine.Label.VersionMismatch";
                case EngineStatus.IntegrityFailure:
                    return "Engine.Label.IntegrityFailure";
                case EngineStatus.Missing:
                    return "Engine.Label.Missing";
                case EngineStatus.Unsupported:
                    return "Engine.Label.Unsupported";
                default:
                    throw new ArgumentOutOfRangeException(nameof(status), status, "Unknown engine status.");
            }
        }

        internal static string LabelFor(EngineStatus status) => Text(LabelKeyFor(status));

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

        internal static string DescriptionFor(EngineStatus status, string? version, string? expectedVersion)
        {
            switch (status)
            {
                case EngineStatus.Checking:
                    return Text("Engine.Description.Checking");
                case EngineStatus.Ready:
                    return version == null
                        ? Text("Engine.Description.Ready")
                        : Text("Engine.Description.ReadyWithVersion", version);
                case EngineStatus.VersionMismatch:
                    return version != null && expectedVersion != null
                        ? Text("Engine.Description.VersionMismatchDetail", version, expectedVersion)
                        : Text("Engine.Description.VersionMismatch");
                case EngineStatus.IntegrityFailure:
                    return Text("Engine.Description.IntegrityFailure");
                case EngineStatus.Missing:
                    return Text("Engine.Description.Missing");
                case EngineStatus.Unsupported:
                    return Text("Engine.Description.Unsupported");
                default:
                    throw new ArgumentOutOfRangeException(nameof(status), status, "Unknown engine status.");
            }
        }
    }
}
