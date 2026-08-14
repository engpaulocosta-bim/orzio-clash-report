using System;
using System.Collections.ObjectModel;
using System.Globalization;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using OrzioClashReport.Launcher.Application.Operations;
using OrzioClashReport.Launcher.Contracts.Jobs;
using OrzioClashReport.Launcher.Contracts.Platform;
using OrzioClashReport.Launcher.Contracts.Results;

namespace OrzioClashReport.Launcher.Desktop.ViewModels
{
    /// <summary>
    /// Runs one operation and presents it: live engine output while it runs, and afterwards either the
    /// produced file or an error that says what failed and what to do next. Shared by every form, so
    /// running an operation looks and behaves the same everywhere.
    /// </summary>
    public sealed partial class JobViewModel : ObservableObject
    {
        /// <summary>The panel shows a rolling window; the complete streams stay in the result.</summary>
        public const int MaximumVisibleLines = 500;

        private readonly LauncherOperationExecutor _executor;
        private readonly IOutputRevealer _outputRevealer;
        private readonly ActiveJobTracker _activeJobTracker;

        private CancellationTokenSource? _cancellation;

        [ObservableProperty]
        private bool _isRunning;

        [ObservableProperty]
        private bool _hasResult;

        [ObservableProperty]
        private bool _isSucceeded;

        [ObservableProperty]
        private bool _isFailed;

        [ObservableProperty]
        private bool _isCanceled;

        [ObservableProperty]
        private string _stateGlyph = string.Empty;

        [ObservableProperty]
        private string _stateLabel = string.Empty;

        [ObservableProperty]
        private string _errorMessage = string.Empty;

        [ObservableProperty]
        private string _errorNextStep = string.Empty;

        [ObservableProperty]
        private string _engineOutput = string.Empty;

        [ObservableProperty]
        private bool _hasEngineOutput;

        [ObservableProperty]
        private string _durationText = string.Empty;

        [ObservableProperty]
        private string _artifactFileName = string.Empty;

        [ObservableProperty]
        private bool _hasArtifact;

        private string? _artifactPath;

        public JobViewModel(
            LauncherOperationExecutor executor,
            IOutputRevealer outputRevealer,
            ActiveJobTracker activeJobTracker)
        {
            _executor = executor ?? throw new ArgumentNullException(nameof(executor));
            _outputRevealer = outputRevealer ?? throw new ArgumentNullException(nameof(outputRevealer));
            _activeJobTracker = activeJobTracker ?? throw new ArgumentNullException(nameof(activeJobTracker));
        }

        public ObservableCollection<string> OutputLines { get; } = new ObservableCollection<string>();

        public ObservableCollection<string> Warnings { get; } = new ObservableCollection<string>();

        /// <summary>
        /// Runs the request. Returns the result so a form can react to it, for example by offering the
        /// natural follow-up step after a successful append.
        /// </summary>
        public async Task<EngineJobResult?> RunAsync(LauncherOperationRequest request)
        {
            if (!_activeJobTracker.TryAcquire())
            {
                ShowRefusal(
                    "Já existe uma operação em curso nesta janela.",
                    "Espere que termine, ou cancele-a, antes de iniciar outra.");

                return null;
            }

            Reset();
            IsRunning = true;
            StateGlyph = "⋯";
            StateLabel = "Em execução";

            _cancellation = new CancellationTokenSource();

            var progress = new Progress<EngineJobProgress>(AppendProgress);

            try
            {
                EngineJobResult result =
                    await _executor.ExecuteAsync(request, progress, _cancellation.Token).ConfigureAwait(true);

                Present(result);
                return result;
            }
            finally
            {
                IsRunning = false;
                _cancellation.Dispose();
                _cancellation = null;
                _activeJobTracker.Release();
            }
        }

        [RelayCommand]
        private void Cancel() => _cancellation?.Cancel();

        [RelayCommand]
        private async Task OpenArtifactAsync()
        {
            if (_artifactPath != null)
            {
                await _outputRevealer.OpenAsync(_artifactPath, CancellationToken.None).ConfigureAwait(true);
            }
        }

        [RelayCommand]
        private async Task RevealArtifactAsync()
        {
            if (_artifactPath != null)
            {
                await _outputRevealer.RevealInFolderAsync(_artifactPath, CancellationToken.None).ConfigureAwait(true);
            }
        }

        /// <summary>
        /// Reports a form that could not produce a valid request. It is shown through the same result
        /// panel as an engine failure, so the user never meets a raw exception.
        /// </summary>
        public void ShowValidationFailure(string message) =>
            ShowRefusal(message, "Reveja os campos do formulário e tente novamente.");

        public void Reset()
        {
            OutputLines.Clear();
            Warnings.Clear();

            HasResult = false;
            IsSucceeded = false;
            IsFailed = false;
            IsCanceled = false;
            HasArtifact = false;
            HasEngineOutput = false;

            ErrorMessage = string.Empty;
            ErrorNextStep = string.Empty;
            EngineOutput = string.Empty;
            DurationText = string.Empty;
            ArtifactFileName = string.Empty;
            _artifactPath = null;
        }

        private void AppendProgress(EngineJobProgress progress)
        {
            if (progress.Line == null)
            {
                return;
            }

            OutputLines.Add(progress.Line);

            while (OutputLines.Count > MaximumVisibleLines)
            {
                OutputLines.RemoveAt(0);
            }
        }

        private void Present(EngineJobResult result)
        {
            HasResult = true;
            IsSucceeded = result.State == EngineJobState.Succeeded;
            IsFailed = result.State == EngineJobState.Failed;
            IsCanceled = result.State == EngineJobState.Canceled;

            DurationText = result.Duration.TotalSeconds.ToString("0.0", CultureInfo.InvariantCulture) + " s";

            foreach (LauncherWarning warning in result.Warnings)
            {
                Warnings.Add(warning.Message);
            }

            if (IsSucceeded)
            {
                StateGlyph = "✓";
                StateLabel = "Concluído";
            }
            else if (IsCanceled)
            {
                StateGlyph = "⊘";
                StateLabel = "Cancelado";
            }
            else
            {
                StateGlyph = "✕";
                StateLabel = "Falhou";
            }

            if (result.Error != null)
            {
                ErrorMessage = result.Error.Message;
                ErrorNextStep = result.Error.NextStep;
            }

            string engineOutput = BuildEngineOutput(result);
            EngineOutput = engineOutput;
            HasEngineOutput = engineOutput.Length > 0;

            if (result.Artifacts.Count > 0)
            {
                LauncherArtifact artifact = result.Artifacts[0];
                _artifactPath = artifact.Path;
                ArtifactFileName = Path.GetFileName(artifact.Path);
                HasArtifact = true;
            }
        }

        private void ShowRefusal(string message, string nextStep)
        {
            Reset();

            HasResult = true;
            IsFailed = true;
            StateGlyph = "✕";
            StateLabel = "Falhou";
            ErrorMessage = message;
            ErrorNextStep = nextStep;
        }

        private static string BuildEngineOutput(EngineJobResult result)
        {
            string standardOutput = result.StandardOutput.TrimEnd();
            string standardError = result.StandardError.TrimEnd();

            if (standardOutput.Length == 0)
            {
                return standardError;
            }

            if (standardError.Length == 0)
            {
                return standardOutput;
            }

            return standardOutput + "\n" + standardError;
        }
    }
}
