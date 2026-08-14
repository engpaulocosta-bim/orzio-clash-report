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
                "Criar documento de governança",
                "Cria um documento vazio, ligado a um projeto, onde as suas decisões passam a ser registadas.",
                job,
                engineStatus)
        {
            _projectId = new TextFieldViewModel(
                "Identificador do projeto",
                "Tem de coincidir exatamente com o do catálogo do projeto. Não é normalizado.",
                "tower-a",
                NotifyFieldsChanged);

            _output = new PathFieldViewModel(
                "Destino do documento",
                "Onde o identity-governance JSON será criado. É sempre criado de novo.",
                dialogs, FilePickerFileKind.IdentityGovernanceJson,
                isDestination: true, suggestedFileName: "identity-governance.json", onChanged: NotifyFieldsChanged);

            RegisterField(_projectId);
            RegisterField(_output);

            Notes.Add("Um documento vazio significa apenas ausência de decisões. Não existe estado pendente.");
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
                "Validar governança",
                "Confirma que cada decisão aponta para um run indexado e para uma ocorrência que existe.",
                job,
                engineStatus)
        {
            _project = new PathFieldViewModel(
                "Catálogo do projeto", "O project.json a que a governança está ligada.",
                dialogs, FilePickerFileKind.ProjectCatalogJson, onChanged: NotifyFieldsChanged);

            _governance = new PathFieldViewModel(
                "Documento de governança", "O identity-governance.json a validar.",
                dialogs, FilePickerFileKind.IdentityGovernanceJson, onChanged: NotifyFieldsChanged);

            RegisterField(_project);
            RegisterField(_governance);

            Notes.Add("Esta operação não escreve nada. Valida apenas a ligação ao projeto e a existência das evidências.");
            Notes.Add("Não valida candidatura do matcher, adjacência, transitividade nem responsabilidade.");
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
                "Gerar revisão de governança",
                "Apresenta as decisões registadas, pela ordem em que foram registadas, com o lado esquerdo antes do direito.",
                job,
                engineStatus)
        {
            _project = new PathFieldViewModel(
                "Catálogo do projeto", "O project.json a que a governança está ligada.",
                dialogs, FilePickerFileKind.ProjectCatalogJson, onChanged: NotifyFieldsChanged);

            _governance = new PathFieldViewModel(
                "Documento de governança", "O identity-governance.json a apresentar.",
                dialogs, FilePickerFileKind.IdentityGovernanceJson, onChanged: NotifyFieldsChanged);

            _output = new PathFieldViewModel(
                "Destino da revisão", "O HTML da revisão. É derivado e pode ser gerado de novo.",
                dialogs, FilePickerFileKind.HtmlReport,
                isDestination: true, suggestedFileName: "identity-governance.html", onChanged: NotifyFieldsChanged);

            RegisterField(_project);
            RegisterField(_governance);
            RegisterField(_output);

            Notes.Add("A revisão só é gerada depois de a validação de evidências passar.");
            Notes.Add("É um artefacto derivado: não é evidência, não infere identidade e não altera o projeto.");
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
