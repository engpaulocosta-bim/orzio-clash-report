using OrzioClashReport.Launcher.Application.Engine;
using OrzioClashReport.Launcher.Application.Operations;
using OrzioClashReport.Launcher.Contracts.Operations;
using OrzioClashReport.Launcher.Desktop.Platform;
using OrzioClashReport.Launcher.Desktop.ViewModels.Fields;

namespace OrzioClashReport.Launcher.Desktop.ViewModels.Operations
{
    /// <summary>Creates one empty governance document bound to a project id.</summary>
    public sealed class CreateGovernanceFormViewModel : OperationFormViewModel
    {
        private readonly TextFieldViewModel _projectId;
        private readonly PathFieldViewModel _output;

        public CreateGovernanceFormViewModel(
            JobViewModel job, EngineStatusViewModel engineStatus, IFileDialogService dialogs)
            : base(
                LauncherOperationKind.CreateIdentityGovernance,
                "op.createGovernance.title",
                "op.createGovernance.description",
                job,
                engineStatus)
        {
            _projectId = new TextFieldViewModel(
                "op.createGovernance.projectId.label",
                "op.createGovernance.projectId.description",
                "tower-a",
                NotifyFieldsChanged);

            _output = new PathFieldViewModel(
                "op.createGovernance.output.label",
                "op.createGovernance.output.description",
                dialogs, FilePickerFileKind.IdentityGovernanceJson,
                isDestination: true, suggestedFileName: "identity-governance.json", onChanged: NotifyFieldsChanged);

            RegisterField(_projectId);
            RegisterField(_output);

            AddNote("op.createGovernance.note.1");
        }

        protected override string? OutputPath => _output.Value.Length == 0 ? null : _output.Value;

        protected override LauncherOperationRequest BuildRequest(OutputCollisionDecision decision) =>
            new LauncherOperationRequest(
                LauncherOperationKind.CreateIdentityGovernance,
                EngineArgumentBuilder.CreateIdentityGovernance(_projectId.Value.Trim(), _output.Value),
                WorkingDirectoryFor(_output.Value),
                _output.Value,
                decision,
                System.IO.Path.GetFileName(_output.Value));
    }

    /// <summary>Read-only validation of the recorded decisions against the project's indexed snapshots.</summary>
    public sealed class ValidateGovernanceFormViewModel : OperationFormViewModel
    {
        private readonly PathFieldViewModel _project;
        private readonly PathFieldViewModel _governance;

        public ValidateGovernanceFormViewModel(
            JobViewModel job, EngineStatusViewModel engineStatus, IFileDialogService dialogs)
            : base(
                LauncherOperationKind.ValidateIdentityGovernance,
                "op.validateGovernance.title",
                "op.validateGovernance.description",
                job,
                engineStatus)
        {
            _project = new PathFieldViewModel(
                "op.validateGovernance.project.label", "op.validateGovernance.project.description",
                dialogs, FilePickerFileKind.ProjectCatalogJson, onChanged: NotifyFieldsChanged);

            _governance = new PathFieldViewModel(
                "op.validateGovernance.governance.label", "op.validateGovernance.governance.description",
                dialogs, FilePickerFileKind.IdentityGovernanceJson, onChanged: NotifyFieldsChanged);

            RegisterField(_project);
            RegisterField(_governance);

            AddNote("op.validateGovernance.note.1");
            AddNote("op.validateGovernance.note.2");
        }

        protected override string? OutputPath => null;

        protected override LauncherOperationRequest BuildRequest(OutputCollisionDecision decision) =>
            new LauncherOperationRequest(
                LauncherOperationKind.ValidateIdentityGovernance,
                EngineArgumentBuilder.ValidateIdentityGovernance(_project.Value, _governance.Value),
                WorkingDirectoryFor(_project.Value),
                null,
                decision,
                System.IO.Path.GetFileName(_governance.Value));
    }

    /// <summary>Renders the standalone review of the decisions that were actually recorded.</summary>
    public sealed class RenderGovernanceReportFormViewModel : OperationFormViewModel
    {
        private readonly PathFieldViewModel _project;
        private readonly PathFieldViewModel _governance;
        private readonly PathFieldViewModel _output;

        public RenderGovernanceReportFormViewModel(
            JobViewModel job, EngineStatusViewModel engineStatus, IFileDialogService dialogs)
            : base(
                LauncherOperationKind.RenderIdentityGovernanceReport,
                "op.renderGovernance.title",
                "op.renderGovernance.description",
                job,
                engineStatus)
        {
            _project = new PathFieldViewModel(
                "op.renderGovernance.project.label", "op.renderGovernance.project.description",
                dialogs, FilePickerFileKind.ProjectCatalogJson, onChanged: NotifyFieldsChanged);

            _governance = new PathFieldViewModel(
                "op.renderGovernance.governance.label", "op.renderGovernance.governance.description",
                dialogs, FilePickerFileKind.IdentityGovernanceJson, onChanged: NotifyFieldsChanged);

            _output = new PathFieldViewModel(
                "op.renderGovernance.output.label", "op.renderGovernance.output.description",
                dialogs, FilePickerFileKind.HtmlReport,
                isDestination: true, suggestedFileName: "identity-governance.html", onChanged: NotifyFieldsChanged);

            RegisterField(_project);
            RegisterField(_governance);
            RegisterField(_output);

            AddNote("op.renderGovernance.note.1");
            AddNote("op.renderGovernance.note.2");
        }

        protected override string? OutputPath => _output.Value.Length == 0 ? null : _output.Value;

        protected override LauncherOperationRequest BuildRequest(OutputCollisionDecision decision) =>
            new LauncherOperationRequest(
                LauncherOperationKind.RenderIdentityGovernanceReport,
                EngineArgumentBuilder.RenderIdentityGovernanceReport(
                    _project.Value, _governance.Value, _output.Value),
                WorkingDirectoryFor(_output.Value),
                _output.Value,
                decision,
                System.IO.Path.GetFileName(_output.Value));
    }
}
