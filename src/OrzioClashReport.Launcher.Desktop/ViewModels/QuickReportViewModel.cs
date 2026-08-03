using System;
using System.IO;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using OrzioClashReport.Launcher.Contracts.Operations;
using OrzioClashReport.Launcher.Desktop.Platform;

namespace OrzioClashReport.Launcher.Desktop.ViewModels
{
    /// <summary>
    /// The shortest path to a result: choose an export, choose where the report goes, generate,
    /// watch it run, and open it. No terminal, and no command line assembled anywhere in this class.
    /// </summary>
    public sealed partial class QuickReportViewModel : ViewModelBase
    {
        private readonly IFileDialogs _fileDialogs;
        private readonly EngineStatusViewModel _engine;

        [ObservableProperty]
        private string? _inputXmlPath;

        [ObservableProperty]
        private string? _outputHtmlPath;

        public QuickReportViewModel(
            OperationRunnerViewModel runner, IFileDialogs fileDialogs, EngineStatusViewModel engine)
        {
            Runner = runner ?? throw new ArgumentNullException(nameof(runner));
            _fileDialogs = fileDialogs ?? throw new ArgumentNullException(nameof(fileDialogs));
            _engine = engine ?? throw new ArgumentNullException(nameof(engine));

            _engine.PropertyChanged += (_, _) =>
            {
                OnPropertyChanged(nameof(BlockedReason));
                OnPropertyChanged(nameof(IsBlocked));
                GenerateCommand.NotifyCanExecuteChanged();
                OnPropertyChanged(nameof(ActionHint));
            };

            Runner.PropertyChanged += (_, _) =>
            {
                GenerateCommand.NotifyCanExecuteChanged();
                OnPropertyChanged(nameof(ActionHint));
            };
        }

        /// <summary>
        /// What the primary action is waiting for. The runner can only report what it has done, so
        /// until something has run this says whether the operation can start, and readiness is
        /// claimed only when it is true.
        /// </summary>
        public string ActionHint =>
            Runner.HasRun ? Runner.StatusText
            : CanGenerate() ? Text("Action.ReadyToRun")
            : Text("Action.NotReadyToRun");

        public OperationRunnerViewModel Runner { get; }

        public string? InputFileName => InputXmlPath == null ? null : Path.GetFileName(InputXmlPath);

        public string? OutputFileName => OutputHtmlPath == null ? null : Path.GetFileName(OutputHtmlPath);

        public bool HasInput => InputXmlPath != null;

        public bool HasOutput => OutputHtmlPath != null;

        /// <summary>Shown when the engine is not usable, so the reason is visible before trying.</summary>
        public string? BlockedReason => _engine.CanRunOperations ? null : _engine.Description;

        public bool IsBlocked => BlockedReason != null;

        public string Title => Text("QuickReport.Title");

        public string Description => Text("QuickReport.Description");

        public string Step1Caption => Text("QuickReport.Step1");

        public string Step2Caption => Text("QuickReport.Step2");

        public string DropHint => Text("QuickReport.DropHint");

        public string NotChosen => Text("Field.NotChosen");

        public string GenerateLabel => Text("QuickReport.Generate");

        public string PickFileLabel => Text("Action.PickFile");

        public string PickDestinationLabel => Text("Action.PickDestination");

        public string CancelLabel => Text("Action.Cancel");

        [RelayCommand]
        private async Task PickInputAsync()
        {
            string? picked = await _fileDialogs
                .PickOpenFileAsync(Text("QuickReport.PickInput"), PickedFileKind.ClashXml)
                .ConfigureAwait(true);

            if (picked != null)
            {
                InputXmlPath = Path.GetFullPath(picked);
                SuggestOutput();
            }
        }

        [RelayCommand]
        private async Task PickOutputAsync()
        {
            string? picked = await _fileDialogs
                .PickSaveFileAsync(Text("QuickReport.PickOutput"), PickedFileKind.HtmlReport, SuggestedFileName())
                .ConfigureAwait(true);

            if (picked != null)
            {
                OutputHtmlPath = Path.GetFullPath(picked);
            }
        }

        /// <summary>Accepts a file dropped onto the screen, which is the same as choosing it.</summary>
        public void AcceptDroppedInput(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                return;
            }

            if (!string.Equals(Path.GetExtension(path), ".xml", StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            InputXmlPath = Path.GetFullPath(path);
            SuggestOutput();
        }

        [RelayCommand(CanExecute = nameof(CanGenerate))]
        private async Task GenerateAsync()
        {
            if (InputXmlPath == null || OutputHtmlPath == null)
            {
                return;
            }

            await Runner
                .RunAsync(new QuickReportRequest(InputXmlPath, OutputHtmlPath))
                .ConfigureAwait(true);
        }

        private bool CanGenerate() =>
            HasInput && HasOutput && !Runner.IsRunning && _engine.CanRunOperations;

        /// <summary>
        /// Proposes a destination next to the export the first time, so the common case needs one
        /// click. It never silently changes a destination the human already chose.
        /// </summary>
        private void SuggestOutput()
        {
            if (OutputHtmlPath != null || InputXmlPath == null)
            {
                return;
            }

            string? directory = Path.GetDirectoryName(InputXmlPath);
            if (directory != null)
            {
                OutputHtmlPath = Path.Combine(directory, SuggestedFileName());
            }
        }

        private string SuggestedFileName()
        {
            string stem = InputXmlPath == null
                ? "clash-report"
                : Path.GetFileNameWithoutExtension(InputXmlPath);

            return stem + ".report.html";
        }

        partial void OnInputXmlPathChanged(string? value)
        {
            _ = value;
            OnPropertyChanged(nameof(InputFileName));
            OnPropertyChanged(nameof(HasInput));
            GenerateCommand.NotifyCanExecuteChanged();
            OnPropertyChanged(nameof(ActionHint));
        }

        partial void OnOutputHtmlPathChanged(string? value)
        {
            _ = value;
            OnPropertyChanged(nameof(OutputFileName));
            OnPropertyChanged(nameof(HasOutput));
            GenerateCommand.NotifyCanExecuteChanged();
            OnPropertyChanged(nameof(ActionHint));
        }
    }
}
