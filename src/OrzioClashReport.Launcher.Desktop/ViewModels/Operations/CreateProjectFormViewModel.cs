using OrzioClashReport.Launcher.Application.Engine;
using OrzioClashReport.Launcher.Application.Operations;
using OrzioClashReport.Launcher.Contracts.Operations;
using OrzioClashReport.Launcher.Desktop.Platform;
using OrzioClashReport.Launcher.Desktop.ViewModels.Fields;

namespace OrzioClashReport.Launcher.Desktop.ViewModels.Operations
{
    /// <summary>Creates one operational project catalog around an existing run index.</summary>
    public sealed class CreateProjectFormViewModel : OperationFormViewModel
    {
        private readonly TextFieldViewModel _projectId;
        private readonly TextFieldViewModel _displayName;
        private readonly PathFieldViewModel _index;
        private readonly PathFieldViewModel _report;
        private readonly PathFieldViewModel _output;

        public CreateProjectFormViewModel(
            JobViewModel job, EngineStatusViewModel engineStatus, IFileDialogService dialogs)
            : base(
                LauncherOperationKind.CreateProject,
                "op.createProject.title",
                "op.createProject.description",
                job,
                engineStatus)
        {
            _projectId = new TextFieldViewModel(
                "op.createProject.projectId.label",
                "op.createProject.projectId.description",
                "tower-a",
                NotifyFieldsChanged);

            _displayName = new TextFieldViewModel(
                "op.createProject.name.label", "op.createProject.name.description", "Tower A", NotifyFieldsChanged);

            _index = new PathFieldViewModel(
                "op.createProject.index.label", "op.createProject.index.description",
                dialogs, FilePickerFileKind.RunIndexJson, onChanged: NotifyFieldsChanged);

            _report = new PathFieldViewModel(
                "op.createProject.report.label",
                "op.createProject.report.description",
                dialogs, FilePickerFileKind.HtmlReport,
                isDestination: true, suggestedFileName: "longitudinal.html", onChanged: NotifyFieldsChanged);

            _output = new PathFieldViewModel(
                "op.createProject.output.label",
                "op.createProject.output.description",
                dialogs, FilePickerFileKind.ProjectCatalogJson,
                isDestination: true, suggestedFileName: "project.json", onChanged: NotifyFieldsChanged);

            RegisterField(_projectId);
            RegisterField(_displayName);
            RegisterField(_index);
            RegisterField(_report);
            RegisterField(_output);

            AddNote("op.createProject.note.1");
        }

        protected override string? OutputPath => _output.Value.Length == 0 ? null : _output.Value;

        protected override LauncherOperationRequest BuildRequest(OutputCollisionDecision decision) =>
            new LauncherOperationRequest(
                LauncherOperationKind.CreateProject,
                EngineArgumentBuilder.CreateProject(
                    _projectId.Value.Trim(),
                    _displayName.Value.Trim(),
                    _index.Value,
                    _report.Value,
                    _output.Value),
                WorkingDirectoryFor(_output.Value),
                _output.Value,
                decision,
                System.IO.Path.GetFileName(_output.Value));
    }
}
