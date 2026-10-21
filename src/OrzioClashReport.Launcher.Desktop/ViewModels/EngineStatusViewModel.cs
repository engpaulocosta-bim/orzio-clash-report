using CommunityToolkit.Mvvm.ComponentModel;
using OrzioClashReport.Launcher.Desktop.Localization;
using OrzioClashReport.Launcher.Application.Presentation;
using OrzioClashReport.Launcher.Contracts.Engine;

namespace OrzioClashReport.Launcher.Desktop.ViewModels
{
    /// <summary>
    /// Presents the engine state. Glyph and label always come from
    /// <see cref="EngineStatusPresentation"/>, so the badge is readable without perceiving colour.
    /// </summary>
    public sealed partial class EngineStatusViewModel : LocalizedObservableObject
    {
        // The state, not its text: glyph, label and explanation are read from the presentation each time,
        // so the badge follows the interface language. A detail the probe wrote is kept as it was written.
        private EngineStatusKind _status = EngineStatusKind.Checking;
        private string? _detail;

        [ObservableProperty]
        private string _reportedVersion = string.Empty;

        [ObservableProperty]
        private string _expectedVersion = string.Empty;

        [ObservableProperty]
        private bool _isNeutral;

        [ObservableProperty]
        private bool _isPositive;

        [ObservableProperty]
        private bool _isCaution;

        [ObservableProperty]
        private bool _isCritical;

        [ObservableProperty]
        private bool _isReady;

        public EngineStatusViewModel()
        {
            Apply(EngineStatusKind.Checking);
        }

        public string Glyph => EngineStatusPresentation.For(_status).Glyph;

        public string Label => EngineStatusPresentation.For(_status).Label;

        public string Explanation => _detail ?? EngineStatusPresentation.For(_status).Explanation;

        public void Update(EngineInfo info)
        {
            Apply(info.Status);

            ReportedVersion = info.ReportedVersion ?? string.Empty;
            ExpectedVersion = info.ExpectedVersion;
            IsReady = info.IsReady;

            if (info.Detail.Length > 0)
            {
                _detail = info.Detail;
                OnPropertyChanged(nameof(Explanation));
            }
        }

        private void Apply(EngineStatusKind status)
        {
            EngineStatusPresentation presentation = EngineStatusPresentation.For(status);

            _status = status;
            _detail = null;
            OnPropertyChanged(nameof(Glyph));
            OnPropertyChanged(nameof(Label));
            OnPropertyChanged(nameof(Explanation));

            IsNeutral = presentation.Severity == LauncherSeverity.Neutral;
            IsPositive = presentation.Severity == LauncherSeverity.Positive;
            IsCaution = presentation.Severity == LauncherSeverity.Caution;
            IsCritical = presentation.Severity == LauncherSeverity.Critical;
        }
    }
}
