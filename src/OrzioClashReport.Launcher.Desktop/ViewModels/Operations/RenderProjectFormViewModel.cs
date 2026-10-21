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
                "op.renderProject.title",
                "op.renderProject.description",
                job,
                engineStatus)
        {
            _project = new PathFieldViewModel(
                "op.renderProject.project.label", "op.renderProject.project.description",
                dialogs, FilePickerFileKind.ProjectCatalogJson, onChanged: NotifyFieldsChanged);

            RegisterField(_project);

            AddNote("op.renderProject.note.1");
            AddNote("op.renderProject.note.2");
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
