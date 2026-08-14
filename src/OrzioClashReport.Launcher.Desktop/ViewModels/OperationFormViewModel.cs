using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using OrzioClashReport.Launcher.Application.Operations;
using OrzioClashReport.Launcher.Contracts.Jobs;
using OrzioClashReport.Launcher.Contracts.Operations;
using OrzioClashReport.Launcher.Contracts.Results;
using OrzioClashReport.Launcher.Desktop.ViewModels.Fields;

namespace OrzioClashReport.Launcher.Desktop.ViewModels
{
    /// <summary>
    /// One typed operation form. The fields are presentation; the request is built in code from named
    /// values, so no screen ever assembles a command line or guesses a flag.
    /// </summary>
    public abstract partial class OperationFormViewModel : ObservableObject
    {
        private readonly EngineStatusViewModel _engineStatus;

        [ObservableProperty]
        private bool _isCollisionPending;

        [ObservableProperty]
        private string _collisionFileName = string.Empty;

        [ObservableProperty]
        private bool _isCollisionReplaceable;

        [ObservableProperty]
        private string _followUpMessage = string.Empty;

        [ObservableProperty]
        private bool _hasFollowUp;

        protected OperationFormViewModel(
            LauncherOperationKind operation,
            string title,
            string description,
            JobViewModel job,
            EngineStatusViewModel engineStatus)
        {
            Operation = operation;
            Title = title ?? throw new ArgumentNullException(nameof(title));
            Description = description ?? throw new ArgumentNullException(nameof(description));
            Job = job ?? throw new ArgumentNullException(nameof(job));
            _engineStatus = engineStatus ?? throw new ArgumentNullException(nameof(engineStatus));

            _engineStatus.PropertyChanged += (_, _) => RunCommand.NotifyCanExecuteChanged();
            Job.PropertyChanged += (_, _) => RunCommand.NotifyCanExecuteChanged();
        }

        public LauncherOperationKind Operation { get; }

        public string Title { get; }

        public string Description { get; }

        public JobViewModel Job { get; }

        public EngineStatusViewModel EngineStatus => _engineStatus;

        public ObservableCollection<OperationFieldViewModel> Fields { get; } =
            new ObservableCollection<OperationFieldViewModel>();

        /// <summary>Notes shown above the form: experimental status, ordering rules, and similar.</summary>
        public ObservableCollection<string> Notes { get; } = new ObservableCollection<string>();

        /// <summary>Builds the request from the form's own typed fields.</summary>
        protected abstract LauncherOperationRequest BuildRequest(OutputCollisionDecision decision);

        /// <summary>The destination this form writes, or <c>null</c> when the engine owns it.</summary>
        protected abstract string? OutputPath { get; }

        /// <summary>Called after a successful run so a form can offer the natural next step.</summary>
        protected virtual void OnSucceeded(EngineJobResult result)
        {
        }

        protected void RegisterField(OperationFieldViewModel field)
        {
            Fields.Add(field);
            RunCommand.NotifyCanExecuteChanged();
        }

        protected void NotifyFieldsChanged() => RunCommand.NotifyCanExecuteChanged();

        [RelayCommand(CanExecute = nameof(CanRun))]
        private Task RunAsync() => ExecuteAsync(OutputCollisionDecision.None);

        protected virtual bool CanRun()
        {
            if (Job.IsRunning || !_engineStatus.IsReady)
            {
                return false;
            }

            foreach (OperationFieldViewModel field in Fields)
            {
                if (!field.IsComplete)
                {
                    return false;
                }
            }

            return true;
        }

        [RelayCommand]
        private void DismissCollision() => IsCollisionPending = false;

        [RelayCommand]
        private Task ReplaceExistingAsync() => ExecuteAsync(OutputCollisionDecision.ReplaceExisting);

        private async Task ExecuteAsync(OutputCollisionDecision decision)
        {
            IsCollisionPending = false;
            HasFollowUp = false;
            FollowUpMessage = string.Empty;

            LauncherOperationRequest request;
            try
            {
                request = BuildRequest(decision);
            }
            catch (ArgumentException exception)
            {
                // A malformed form is reported through the same result panel as any other failure,
                // so the user never sees a raw exception.
                Job.ShowValidationFailure(exception.Message);
                return;
            }

            EngineJobResult? result = await Job.RunAsync(request).ConfigureAwait(true);
            if (result == null)
            {
                return;
            }

            if (result.Error?.Kind == LauncherErrorKind.OutputCollision && OutputPath != null)
            {
                CollisionFileName = Path.GetFileName(OutputPath);
                IsCollisionReplaceable = LauncherOperationMetadata.ProducesReplaceableHtmlOutput(Operation);
                IsCollisionPending = true;
                return;
            }

            if (result.State == EngineJobState.Succeeded)
            {
                OnSucceeded(result);
            }
        }

        /// <summary>
        /// The directory the engine runs in. It is the destination's own folder when there is one, and
        /// otherwise the folder of the file the operation acts on — never the installation directory.
        /// </summary>
        protected static string WorkingDirectoryFor(string anchorPath)
        {
            string? directory = Path.GetDirectoryName(anchorPath);
            return string.IsNullOrEmpty(directory) ? anchorPath : directory;
        }
    }
}
