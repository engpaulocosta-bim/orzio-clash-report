using System;
using OrzioClashReport.Launcher.Application.Engine;
using OrzioClashReport.Launcher.Application.Operations;
using OrzioClashReport.Launcher.Contracts.Jobs;
using OrzioClashReport.Launcher.Contracts.Operations;
using OrzioClashReport.Launcher.Desktop.Platform;
using OrzioClashReport.Launcher.Desktop.ViewModels.Fields;

namespace OrzioClashReport.Launcher.Desktop.ViewModels.Operations
{
    /// <summary>
    /// Appends one snapshot to the end of a project's run index. It mutates only the index: the
    /// report is not regenerated as a side effect, and the launcher offers that step explicitly
    /// afterwards rather than doing it silently.
    /// </summary>
    public sealed class AppendProjectSnapshotFormViewModel : OperationFormViewModel
    {
        private readonly PathFieldViewModel _project;
        private readonly PathFieldViewModel _snapshot;
        private readonly Action<string> _offerRender;

        public AppendProjectSnapshotFormViewModel(
            JobViewModel job,
            EngineStatusViewModel engineStatus,
            IFileDialogService dialogs,
            Action<string> offerRender)
            : base(
                LauncherOperationKind.AppendProjectSnapshot,
                "Acrescentar run ao projeto",
                "Acrescenta um snapshot no fim do índice do projeto. Nada é reordenado nem removido.",
                job,
                engineStatus)
        {
            _offerRender = offerRender ?? throw new ArgumentNullException(nameof(offerRender));

            _project = new PathFieldViewModel(
                "Catálogo do projeto", "O project.json do projeto a atualizar.",
                dialogs, FilePickerFileKind.ProjectCatalogJson, onChanged: NotifyFieldsChanged);

            _snapshot = new PathFieldViewModel(
                "Snapshot a acrescentar", "O run que passa a ser o último da sequência.",
                dialogs, FilePickerFileKind.RunSnapshotJson, onChanged: NotifyFieldsChanged);

            RegisterField(_project);
            RegisterField(_snapshot);

            Notes.Add("Esta operação altera apenas o índice. O relatório não é regenerado automaticamente.");
        }

        /// <summary>There is no destination: the engine owns the index it updates.</summary>
        protected override string? OutputPath => null;

        protected override LauncherOperationRequest BuildRequest(OutputCollisionDecision decision) =>
            new LauncherOperationRequest(
                LauncherOperationKind.AppendProjectSnapshot,
                EngineArgumentBuilder.AppendProjectSnapshot(_project.Value, _snapshot.Value),
                WorkingDirectoryFor(_project.Value),
                null,
                decision,
                System.IO.Path.GetFileName(_project.Value));

        protected override void OnSucceeded(EngineJobResult result)
        {
            FollowUpMessage =
                "O índice foi atualizado. O relatório longitudinal continua a mostrar a versão anterior "
                + "até o regenerar.";

            HasFollowUp = true;

            _offerRender(_project.Value);
        }
    }
}
