using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.Input;
using OrzioClashReport.Launcher.Contracts.Jobs;
using OrzioClashReport.Launcher.Contracts.Operations;
using OrzioClashReport.Launcher.Contracts.Ports;

namespace OrzioClashReport.Launcher.Desktop.ViewModels
{
    /// <summary>One operation that was running when the application last stopped.</summary>
    public sealed class InterruptedJobViewModel : ViewModelBase
    {
        public InterruptedJobViewModel(JobJournalEntry entry)
        {
            Entry = entry ?? throw new ArgumentNullException(nameof(entry));
        }

        public JobJournalEntry Entry { get; }

        public string OperationLabel => LabelFor(Entry.OperationKind);

        internal static string LabelFor(LauncherOperationKind kind) => Text(LabelKeyFor(kind));

        public string StartedAtDisplay => Entry.StartedAtUtc.ToLocalTime().ToString("g");

        internal static string LabelKeyFor(LauncherOperationKind kind)
        {
            if (!Enum.IsDefined(typeof(LauncherOperationKind), kind))
            {
                throw new ArgumentOutOfRangeException(nameof(kind), kind, "Unknown launcher operation.");
            }

            return "Operation." + kind;
        }
    }

    /// <summary>
    /// Reports operations that were running when the application last stopped. It never resumes one:
    /// the launcher cannot know what the engine managed to write, so the human decides what to do.
    /// </summary>
    public sealed partial class InterruptedJobsViewModel : ViewModelBase
    {
        private readonly IJobJournal _journal;

        public InterruptedJobsViewModel(IJobJournal journal)
        {
            _journal = journal ?? throw new ArgumentNullException(nameof(journal));
            Jobs = new ObservableCollection<InterruptedJobViewModel>();
        }

        public ObservableCollection<InterruptedJobViewModel> Jobs { get; }

        public bool HasJobs => Jobs.Count > 0;

        public string Title => Text("Interrupted.Title");

        public string Explanation => Text("Interrupted.Explanation");

        public string DismissLabel => Text("Action.Understood");

        /// <summary>Loads what the previous session left behind. Called once, at start.</summary>
        public void Load()
        {
            Jobs.Clear();

            IReadOnlyList<JobJournalEntry> entries = _journal.LoadInterrupted();
            foreach (JobJournalEntry entry in entries)
            {
                Jobs.Add(new InterruptedJobViewModel(entry));
            }

            OnPropertyChanged(nameof(HasJobs));
        }

        [RelayCommand]
        private void Dismiss()
        {
            foreach (InterruptedJobViewModel job in Jobs)
            {
                _journal.Clear(job.Entry.JobId);
            }

            Jobs.Clear();
            OnPropertyChanged(nameof(HasJobs));
        }
    }
}
