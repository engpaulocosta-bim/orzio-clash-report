using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using OrzioClashReport.Launcher.Application;
using OrzioClashReport.Launcher.Contracts.Jobs;
using OrzioClashReport.Launcher.Contracts.Operations;
using OrzioClashReport.Launcher.Contracts.Ports;

namespace OrzioClashReport.Launcher.Desktop.ViewModels
{
    /// <summary>
    /// Runs one operation and presents it: live engine output while it works, and afterwards either
    /// the produced file or an error a coordinator can act on. Shared by every operation screen.
    /// </summary>
    public sealed partial class OperationRunnerViewModel : ViewModelBase
    {
        internal const int MaximumVisibleLines = 300;

        private readonly OperationJobService _jobs;
        private readonly IOutputCollisionPrompt _collisionPrompt;
        private readonly IOutputRevealer _revealer;
        private CancellationTokenSource? _cancellation;

        [ObservableProperty]
        private JobState _state = JobState.Pending;

        [ObservableProperty]
        private bool _hasRun;

        public OperationRunnerViewModel(
            OperationJobService jobs, IOutputCollisionPrompt collisionPrompt, IOutputRevealer revealer)
        {
            _jobs = jobs ?? throw new ArgumentNullException(nameof(jobs));
            _collisionPrompt = collisionPrompt ?? throw new ArgumentNullException(nameof(collisionPrompt));
            _revealer = revealer ?? throw new ArgumentNullException(nameof(revealer));

            OutputLines = new ObservableCollection<string>();
            Warnings = new ObservableCollection<string>();
        }

        /// <summary>The tail of what the engine printed, bounded so a loud engine cannot grow the view.</summary>
        public ObservableCollection<string> OutputLines { get; }

        public ObservableCollection<string> Warnings { get; }

        public OperationResult? Result { get; private set; }

        public bool IsRunning => State == JobState.Running;

        public bool Succeeded => State == JobState.Succeeded;

        public bool Failed => State == JobState.Failed;

        public bool WasCanceled => State == JobState.Canceled;

        public bool HasWarnings => Warnings.Count > 0;

        public bool HasOutputLines => OutputLines.Count > 0;

        public string? ErrorMessage => Result?.Error?.Message;

        public string? ErrorDetail => Result?.Error?.Detail;

        public string? ProducedFileName => Result != null && Result.Artifacts.Count > 0
            ? System.IO.Path.GetFileName(Result.Artifacts[0].Path)
            : null;

        public bool HasProducedFile => ProducedFileName != null;

        public string StatusText => State switch
        {
            JobState.Pending => "Pronto para gerar.",
            JobState.Running => "A executar o motor…",
            JobState.Succeeded => "Concluído.",
            JobState.Failed => ErrorMessage ?? "A operação falhou.",
            JobState.Canceled => "A operação foi cancelada.",
            _ => string.Empty,
        };

        public StatusSeverity Severity => State switch
        {
            JobState.Succeeded => StatusSeverity.Positive,
            JobState.Failed => StatusSeverity.Critical,
            JobState.Canceled => StatusSeverity.Caution,
            _ => StatusSeverity.Neutral,
        };

        public bool IsNeutral => Severity == StatusSeverity.Neutral;

        public bool IsPositive => Severity == StatusSeverity.Positive;

        public bool IsCaution => Severity == StatusSeverity.Caution;

        public bool IsCritical => Severity == StatusSeverity.Critical;

        public async Task RunAsync(LauncherOperationRequest request)
        {
            if (request == null)
            {
                throw new ArgumentNullException(nameof(request));
            }

            if (IsRunning)
            {
                return;
            }

            OutputLines.Clear();
            Warnings.Clear();
            Result = null;
            HasRun = true;
            State = JobState.Running;
            RaiseDerived();

            _cancellation = new CancellationTokenSource();
            var progress = new Progress<EngineOutputChunk>(AppendLine);

            JobSnapshot snapshot;
            try
            {
                snapshot = await _jobs
                    .RunAsync(request, _collisionPrompt, progress, _cancellation.Token)
                    .ConfigureAwait(true);
            }
            catch (InvalidOperationException ex)
            {
                // Only one job runs per window; a second request says so rather than queueing.
                Result = OperationResult.Failure(
                    new LauncherError(LauncherErrorCode.InvalidRequest, ex.Message),
                    exitCode: null,
                    warnings: null,
                    standardOutput: string.Empty,
                    standardError: string.Empty,
                    duration: TimeSpan.Zero);
                State = JobState.Failed;
                RaiseDerived();
                return;
            }
            finally
            {
                _cancellation.Dispose();
                _cancellation = null;
            }

            Result = snapshot.Result;
            if (snapshot.Result != null)
            {
                foreach (LauncherWarning warning in snapshot.Result.Warnings)
                {
                    Warnings.Add(warning.Message);
                }
            }

            State = snapshot.State;
            RaiseDerived();
        }

        [RelayCommand]
        private void Cancel()
        {
            _cancellation?.Cancel();
        }

        [RelayCommand]
        private Task OpenResult()
        {
            string? path = FirstArtifactPath();
            return path == null ? Task.CompletedTask : _revealer.OpenAsync(path);
        }

        [RelayCommand]
        private Task RevealResult()
        {
            string? path = FirstArtifactPath();
            return path == null ? Task.CompletedTask : _revealer.RevealAsync(path);
        }

        private string? FirstArtifactPath() =>
            Result != null && Result.Artifacts.Count > 0 ? Result.Artifacts[0].Path : null;

        private void AppendLine(EngineOutputChunk chunk)
        {
            string prefix = chunk.Stream == EngineOutputStream.StandardError ? "! " : string.Empty;
            OutputLines.Add(prefix + chunk.Text);

            while (OutputLines.Count > MaximumVisibleLines)
            {
                OutputLines.RemoveAt(0);
            }

            OnPropertyChanged(nameof(HasOutputLines));
        }

        partial void OnStateChanged(JobState value)
        {
            _ = value;
            RaiseDerived();
        }

        private void RaiseDerived()
        {
            foreach (string property in DerivedProperties)
            {
                OnPropertyChanged(property);
            }
        }

        private static readonly IReadOnlyList<string> DerivedProperties = new ReadOnlyCollection<string>(new[]
        {
            nameof(IsRunning),
            nameof(Succeeded),
            nameof(Failed),
            nameof(WasCanceled),
            nameof(HasWarnings),
            nameof(HasOutputLines),
            nameof(ErrorMessage),
            nameof(ErrorDetail),
            nameof(ProducedFileName),
            nameof(HasProducedFile),
            nameof(StatusText),
            nameof(Severity),
            nameof(IsNeutral),
            nameof(IsPositive),
            nameof(IsCaution),
            nameof(IsCritical),
        });
    }
}
