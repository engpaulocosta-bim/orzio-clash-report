using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using OrzioClashReport.Launcher.Application;
using OrzioClashReport.Launcher.Contracts.Operations;

namespace OrzioClashReport.Launcher.Desktop.ViewModels
{
    /// <summary>
    /// A field a typed form is built from. The form listens for changes so its run command always
    /// reflects what has actually been supplied.
    /// </summary>
    public interface IFormField
    {
        event Action? Changed;
    }

    /// <summary>One free-text field in a typed form.</summary>
    public sealed partial class TextFieldViewModel : ViewModelBase, IFormField
    {
        [ObservableProperty]
        private string? _value;

        public TextFieldViewModel(string label, string hint)
        {
            Label = label ?? throw new ArgumentNullException(nameof(label));
            Hint = hint ?? throw new ArgumentNullException(nameof(hint));
        }

        public string Label { get; }

        public string Hint { get; }

        public bool HasValue => !string.IsNullOrWhiteSpace(Value);

        public string Trimmed => Value?.Trim() ?? string.Empty;

        /// <summary>Some fields exist only for one shape of a decision.</summary>
        [ObservableProperty]
        private bool _isShown = true;

        partial void OnValueChanged(string? value)
        {
            _ = value;
            OnPropertyChanged(nameof(HasValue));
            Changed?.Invoke();
        }

        public event Action? Changed;
    }

    /// <summary>
    /// A typed form for one engine operation. The form collects values and hands over a request;
    /// it never assembles a command string, and it never decides anything the engine decides.
    /// </summary>
    public abstract partial class OperationFormViewModel : ViewModelBase
    {
        private readonly EngineStatusViewModel _engine;

        protected OperationFormViewModel(
            string title,
            string description,
            LauncherOperationKind kind,
            OperationRunnerViewModel runner,
            EngineStatusViewModel engine)
        {
            Title = title ?? throw new ArgumentNullException(nameof(title));
            Description = description ?? throw new ArgumentNullException(nameof(description));
            Kind = kind;
            Runner = runner ?? throw new ArgumentNullException(nameof(runner));
            _engine = engine ?? throw new ArgumentNullException(nameof(engine));

            Fields = new ObservableCollection<ViewModelBase>();

            _engine.PropertyChanged += (_, _) =>
            {
                OnPropertyChanged(nameof(BlockedReason));
                OnPropertyChanged(nameof(IsBlocked));
                RunCommand.NotifyCanExecuteChanged();
            };

            Runner.PropertyChanged += (_, _) =>
            {
                RunCommand.NotifyCanExecuteChanged();
                OnPropertyChanged(nameof(ShowsFollowUp));
            };
        }

        public string Title { get; }

        public string Description { get; }

        public LauncherOperationKind Kind { get; }

        public OperationRunnerViewModel Runner { get; }

        /// <summary>The fields, in the order the form presents them.</summary>
        public ObservableCollection<ViewModelBase> Fields { get; }

        /// <summary>
        /// Whether this operation exercises engine behaviour that has not been validated against
        /// real sequential historical exports. The form says so rather than implying confidence.
        /// </summary>
        public bool IsExperimental => LauncherOperationPolicy.IsExperimental(Kind);

        public string RunLabel => "Executar";

        public string? BlockedReason => _engine.CanRunOperations ? null : _engine.Description;

        public bool IsBlocked => BlockedReason != null;

        /// <summary>An explicit next step this operation may offer once it has succeeded.</summary>
        public virtual string? FollowUpLabel => null;

        public bool ShowsFollowUp => FollowUpLabel != null && Runner.Succeeded;

        /// <summary>Whether every value the request needs is present, in operational terms only.</summary>
        public abstract bool CanBuild { get; }

        protected abstract LauncherOperationRequest Build();

        protected virtual Task RunFollowUpAsync() => Task.CompletedTask;

        [RelayCommand(CanExecute = nameof(CanRun))]
        private async Task RunAsync()
        {
            if (!CanBuild)
            {
                return;
            }

            await Runner.RunAsync(Build()).ConfigureAwait(true);
            OnPropertyChanged(nameof(ShowsFollowUp));
        }

        [RelayCommand]
        private async Task FollowUpAsync()
        {
            await RunFollowUpAsync().ConfigureAwait(true);
            OnPropertyChanged(nameof(ShowsFollowUp));
        }

        private bool CanRun() => CanBuild && !Runner.IsRunning && _engine.CanRunOperations;

        /// <summary>Adds a field and keeps the run command in step with it.</summary>
        protected TField Add<TField>(TField field)
            where TField : ViewModelBase
        {
            if (field is IFormField notifying)
            {
                notifying.Changed += NotifyChanged;
            }

            Fields.Add(field);
            return field;
        }

        protected void NotifyChanged()
        {
            OnPropertyChanged(nameof(CanBuild));
            RunCommand.NotifyCanExecuteChanged();
        }

        protected static IReadOnlyList<string> Snapshot(OrderedFileListViewModel list) => list.Paths;
    }
}
