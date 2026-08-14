using OrzioClashReport.Launcher.Application.Engine;
using OrzioClashReport.Launcher.Application.Operations;
using OrzioClashReport.Launcher.Contracts.Operations;
using OrzioClashReport.Launcher.Desktop.Platform;
using OrzioClashReport.Launcher.Desktop.ViewModels.Fields;

namespace OrzioClashReport.Launcher.Desktop.ViewModels.Operations
{
    /// <summary>
    /// Regenerates a project's longitudinal report. There is no destination field: the report path
    /// comes from the catalog the engine already owns, and the launcher never proposes one.
    /// </summary>
    public sealed class RenderProjectFormViewModel : OperationFormViewModel
    {
        private readonly PathFieldViewModel _project;

        public RenderProjectFormViewModel(
            JobViewModel job, EngineStatusViewModel engineStatus, IFileDialogService dialogs)
            : base(
                LauncherOperationKind.RenderProject,
                "Renderizar projeto",
                "Recalcula tudo a partir dos snapshots e reescreve o relatório longitudinal do projeto.",
                job,
                engineStatus)
        {
            _project = new PathFieldViewModel(
                "Catálogo do projeto", "O project.json cujo relatório será regenerado.",
                dialogs, FilePickerFileKind.ProjectCatalogJson, onChanged: NotifyFieldsChanged);

            RegisterField(_project);

            Notes.Add("O destino do relatório vem do catálogo do projeto, em leitura apenas.");
            Notes.Add("Os snapshots e o índice não são alterados.");
        }

        public void UseProject(string projectPath) => _project.Value = projectPath;

        protected override string? OutputPath => null;

        protected override LauncherOperationRequest BuildRequest(OutputCollisionDecision decision) =>
            new LauncherOperationRequest(
                LauncherOperationKind.RenderProject,
                EngineArgumentBuilder.RenderProject(_project.Value),
                WorkingDirectoryFor(_project.Value),
                null,
                decision,
                System.IO.Path.GetFileName(_project.Value));
    }
}
