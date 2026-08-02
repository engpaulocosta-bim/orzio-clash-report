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

        /// <summary>
        /// The failure in the reader's language, resolved from the error code rather than from
        /// whatever text the layer below happened to produce.
        /// </summary>
        public string? ErrorMessage =>
            Result?.Error == null ? null : Text(MessageKeyFor(Result.Error.Code));

        public string? ErrorDetail => Result?.Error?.Detail;

        public string? ProducedFileName => Result != null && Result.Artifacts.Count > 0
            ? System.IO.Path.GetFileName(Result.Artifacts[0].Path)
            : null;

        public bool HasProducedFile => ProducedFileName != null;

        public string StatusText => State switch
        {
            JobState.Pending => Text("Runner.Idle"),
            JobState.Running => Text("Runner.Running"),
            JobState.Succeeded => Text("Runner.Succeeded"),
            JobState.Failed => ErrorMessage ?? Text("Runner.Failed"),
            JobState.Canceled => Text("Runner.Canceled"),
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
                    new LauncherError(LauncherErrorCode.InvalidRequest, ex.Message, ex.Message),
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
                    Warnings.Add(Text(MessageKeyFor(warning.Code)));
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

        /// <summary>
        /// The error code is the launcher's own classification, so the message shown comes from it.
        /// A collision the human declined is reported as such rather than as a generic cancellation.
        /// </summary>
        internal static string MessageKeyFor(LauncherErrorCode code)
        {
            switch (code)
            {
                case LauncherErrorCode.EngineExecutionFailure:
                    return "Error.EngineExecutionFailure";
                case LauncherErrorCode.OutputMissing:
                    return "Error.OutputMissing";
                case LauncherErrorCode.EngineMissing:
                    return "Error.EngineMissing";
                case LauncherErrorCode.EngineIntegrityFailure:
                    return "Error.EngineIntegrityFailure";
                case LauncherErrorCode.EngineVersionMismatch:
                    return "Error.EngineVersionMismatch";
                case LauncherErrorCode.EngineUnsupported:
                    return "Error.EngineUnsupported";
                case LauncherErrorCode.Canceled:
                    return "Error.Canceled";
                case LauncherErrorCode.TimedOut:
                    return "Error.TimedOut";
                case LauncherErrorCode.EngineStartFailure:
                    return "Error.EngineStartFailure";
                case LauncherErrorCode.InvalidRequest:
                    return "Error.InvalidRequest";
                case LauncherErrorCode.LocalIoFailure:
                    return "Error.LocalIoFailure";
                default:
                    throw new ArgumentOutOfRangeException(nameof(code), code, "Unknown launcher error code.");
            }
        }

        internal static string MessageKeyFor(LauncherWarningCode code)
        {
            switch (code)
            {
                case LauncherWarningCode.DuplicateSnapshotReference:
                    return "Warning.DuplicateSnapshotReference";
                case LauncherWarningCode.EngineWroteToStandardError:
                    return "Warning.EngineWroteToStandardError";
                case LauncherWarningCode.OutputTruncated:
                    return "Warning.OutputTruncated";
                case LauncherWarningCode.ExperimentalOperation:
                    return "Warning.ExperimentalOperation";
                default:
                    throw new ArgumentOutOfRangeException(nameof(code), code, "Unknown launcher warning code.");
            }
        }

        public string ProducedCaption => Text("Runner.ProducedCaption");

        public string EngineDetailCaption => Text("Runner.EngineDetailCaption");

        public string EngineOutputCaption => Text("Runner.EngineOutputCaption");

        public string OpenLabel => Text("Action.Open");

        public string RevealLabel => Text("Action.Reveal");

        public string CancelLabel => Text("Action.Cancel");

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
