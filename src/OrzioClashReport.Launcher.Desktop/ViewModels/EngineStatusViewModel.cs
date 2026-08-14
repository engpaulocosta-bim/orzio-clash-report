using CommunityToolkit.Mvvm.ComponentModel;
using OrzioClashReport.Launcher.Application.Presentation;
using OrzioClashReport.Launcher.Contracts.Engine;

namespace OrzioClashReport.Launcher.Desktop.ViewModels
{
    /// <summary>
    /// Presents the engine state. Glyph and label always come from
    /// <see cref="EngineStatusPresentation"/>, so the badge is readable without perceiving colour.
    /// </summary>
    public sealed partial class EngineStatusViewModel : ObservableObject
    {
        [ObservableProperty]
        private string _glyph = string.Empty;

        [ObservableProperty]
        private string _label = string.Empty;

        [ObservableProperty]
        private string _explanation = string.Empty;

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

        public void Update(EngineInfo info)
        {
            Apply(info.Status);

            ReportedVersion = info.ReportedVersion ?? string.Empty;
            ExpectedVersion = info.ExpectedVersion;
            IsReady = info.IsReady;

            if (info.Detail.Length > 0)
            {
                Explanation = info.Detail;
            }
        }

        private void Apply(EngineStatusKind status)
        {
            EngineStatusPresentation presentation = EngineStatusPresentation.For(status);

            Glyph = presentation.Glyph;
            Label = presentation.Label;
            Explanation = presentation.Explanation;

            IsNeutral = presentation.Severity == LauncherSeverity.Neutral;
            IsPositive = presentation.Severity == LauncherSeverity.Positive;
            IsCaution = presentation.Severity == LauncherSeverity.Caution;
            IsCritical = presentation.Severity == LauncherSeverity.Critical;
        }
    }
}
