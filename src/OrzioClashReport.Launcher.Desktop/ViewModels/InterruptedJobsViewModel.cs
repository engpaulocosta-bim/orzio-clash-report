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

        public string StartedAtDisplay => Entry.StartedAtUtc.ToLocalTime().ToString("g");

        internal static string LabelFor(LauncherOperationKind kind)
        {
            switch (kind)
            {
                case LauncherOperationKind.QuickReport:
                    return "Relatório rápido";
                case LauncherOperationKind.CreateSnapshot:
                    return "Criar snapshot";
                case LauncherOperationKind.CompareRuns:
                    return "Comparar duas revisões";
                case LauncherOperationKind.CompareSnapshots:
                    return "Comparar snapshots";
                case LauncherOperationKind.IndexSnapshots:
                    return "Criar índice de runs";
                case LauncherOperationKind.CompareIndex:
                    return "Comparar índice de runs";
                case LauncherOperationKind.CreateProject:
                    return "Criar projeto";
                case LauncherOperationKind.AppendProjectSnapshot:
                    return "Acrescentar snapshot ao projeto";
                case LauncherOperationKind.RenderProject:
                    return "Regenerar relatório do projeto";
                case LauncherOperationKind.CreateIdentityGovernance:
                    return "Criar documento de governança";
                case LauncherOperationKind.AppendIdentityDecision:
                    return "Registar decisão humana";
                case LauncherOperationKind.ValidateIdentityGovernance:
                    return "Validar governança";
                case LauncherOperationKind.RenderIdentityGovernanceReport:
                    return "Gerar revisão de governança";
                default:
                    throw new ArgumentOutOfRangeException(nameof(kind), kind, "Unknown launcher operation.");
            }
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

        public string Title => "Operação interrompida";

        public string Explanation =>
            "A aplicação fechou enquanto isto estava a correr. Nada é retomado automaticamente: "
                + "verifique o ficheiro de destino antes de repetir a operação.";

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
