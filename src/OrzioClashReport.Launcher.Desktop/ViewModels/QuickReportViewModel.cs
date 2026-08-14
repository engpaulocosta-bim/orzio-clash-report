using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using OrzioClashReport.Launcher.Application.Engine;
using OrzioClashReport.Launcher.Application.Operations;
using OrzioClashReport.Launcher.Contracts.Jobs;
using OrzioClashReport.Launcher.Contracts.Operations;
using OrzioClashReport.Launcher.Contracts.Results;
using OrzioClashReport.Launcher.Contracts.Settings;
using OrzioClashReport.Launcher.Desktop.Platform;

namespace OrzioClashReport.Launcher.Desktop.ViewModels
{
    /// <summary>
    /// One export XML to one grouped HTML report, without a terminal. The form collects two paths and
    /// nothing else; the argument vector is built by the application layer, never assembled here.
    /// </summary>
    public sealed partial class QuickReportViewModel : ObservableObject
    {
        private readonly IFileDialogService _fileDialogService;
        private readonly ISettingsStore _settingsStore;
        private readonly EngineStatusViewModel _engineStatus;

        [ObservableProperty]
        [NotifyCanExecuteChangedFor(nameof(GenerateCommand))]
        private string _inputXmlPath = string.Empty;

        [ObservableProperty]
        [NotifyCanExecuteChangedFor(nameof(GenerateCommand))]
        private string _outputHtmlPath = string.Empty;

        [ObservableProperty]
        private bool _isCollisionPending;

        [ObservableProperty]
        private string _collisionFileName = string.Empty;

        public QuickReportViewModel(
            JobViewModel job,
            IFileDialogService fileDialogService,
            ISettingsStore settingsStore,
            EngineStatusViewModel engineStatus)
        {
            Job = job ?? throw new ArgumentNullException(nameof(job));
            _fileDialogService = fileDialogService ?? throw new ArgumentNullException(nameof(fileDialogService));
            _settingsStore = settingsStore ?? throw new ArgumentNullException(nameof(settingsStore));
            _engineStatus = engineStatus ?? throw new ArgumentNullException(nameof(engineStatus));

            _engineStatus.PropertyChanged += (_, _) => GenerateCommand.NotifyCanExecuteChanged();
            Job.PropertyChanged += (_, _) => GenerateCommand.NotifyCanExecuteChanged();
        }

        public JobViewModel Job { get; }

        public EngineStatusViewModel EngineStatus => _engineStatus;

        [RelayCommand]
        private async Task BrowseInputAsync()
        {
            string? picked = await _fileDialogService.PickOpenFileAsync(
                "Escolher o export XML do Clash Detective",
                FilePickerFileKind.NavisworksClashXml,
                DirectoryOf(InputXmlPath)).ConfigureAwait(true);

            if (picked == null)
            {
                return;
            }

            InputXmlPath = picked;
            IsCollisionPending = false;

            if (OutputHtmlPath.Length == 0)
            {
                SuggestOutputFrom(picked);
            }
        }

        [RelayCommand]
        private async Task BrowseOutputAsync()
        {
            string? picked = await _fileDialogService.PickSaveFileAsync(
                "Escolher onde guardar o relatório",
                FilePickerFileKind.HtmlReport,
                SuggestedFileName(),
                DirectoryOf(OutputHtmlPath) ?? await LastOutputDirectoryAsync().ConfigureAwait(true))
                .ConfigureAwait(true);

            if (picked == null)
            {
                return;
            }

            OutputHtmlPath = picked;
            IsCollisionPending = false;

            await RememberOutputDirectoryAsync(picked).ConfigureAwait(true);
        }

        [RelayCommand(CanExecute = nameof(CanGenerate))]
        private Task GenerateAsync() => RunAsync(OutputCollisionDecision.None);

        private bool CanGenerate() =>
            !Job.IsRunning
            && _engineStatus.IsReady
            && InputXmlPath.Length > 0
            && OutputHtmlPath.Length > 0;

        /// <summary>Offered on a collision, and never the default: the user picks a new destination.</summary>
        [RelayCommand]
        private async Task ChooseAnotherNameAsync()
        {
            IsCollisionPending = false;
            await BrowseOutputAsync().ConfigureAwait(true);
        }

        /// <summary>
        /// Offered on a collision only after the user asks for it. A quick report is derived and
        /// regenerable, which is the only reason replacing it can be offered at all.
        /// </summary>
        [RelayCommand]
        private async Task ReplaceExistingAsync()
        {
            IsCollisionPending = false;
            await RunAsync(OutputCollisionDecision.ReplaceExisting).ConfigureAwait(true);
        }

        private async Task RunAsync(OutputCollisionDecision decision)
        {
            IsCollisionPending = false;

            var request = new LauncherOperationRequest(
                LauncherOperationKind.QuickReport,
                EngineArgumentBuilder.QuickReport(InputXmlPath, OutputHtmlPath),
                Path.GetDirectoryName(OutputHtmlPath) ?? string.Empty,
                OutputHtmlPath,
                decision,
                Path.GetFileName(OutputHtmlPath));

            EngineJobResult? result = await Job.RunAsync(request).ConfigureAwait(true);

            if (result?.Error?.Kind == LauncherErrorKind.OutputCollision)
            {
                CollisionFileName = Path.GetFileName(OutputHtmlPath);
                IsCollisionPending = true;
            }
        }

        private void SuggestOutputFrom(string inputPath)
        {
            string? directory = Path.GetDirectoryName(inputPath);
            if (string.IsNullOrEmpty(directory))
            {
                return;
            }

            OutputHtmlPath = Path.Combine(
                directory, Path.GetFileNameWithoutExtension(inputPath) + "-report.html");
        }

        private string SuggestedFileName() =>
            InputXmlPath.Length > 0
                ? Path.GetFileNameWithoutExtension(InputXmlPath) + "-report.html"
                : "clash-report.html";

        private static string? DirectoryOf(string path)
        {
            if (path.Length == 0)
            {
                return null;
            }

            string? directory = Path.GetDirectoryName(path);
            return string.IsNullOrEmpty(directory) ? null : directory;
        }

        private async Task<string?> LastOutputDirectoryAsync()
        {
            LauncherSettings settings = await _settingsStore.LoadAsync(CancellationToken.None).ConfigureAwait(true);
            return settings.LastOutputDirectory;
        }

        private async Task RememberOutputDirectoryAsync(string outputPath)
        {
            string? directory = DirectoryOf(outputPath);
            if (directory == null)
            {
                return;
            }

            LauncherSettings settings = await _settingsStore.LoadAsync(CancellationToken.None).ConfigureAwait(true);
            await _settingsStore.SaveAsync(
                settings.WithLastOutputDirectory(directory), CancellationToken.None).ConfigureAwait(true);
        }
    }
}
