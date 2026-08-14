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
                "Criar projeto",
                "Guarda as referências operacionais do projeto: o índice de runs e o destino do relatório longitudinal.",
                job,
                engineStatus)
        {
            _projectId = new TextFieldViewModel(
                "Identificador do projeto",
                "Usado para ligar decisões de governança a este projeto. Comparado exatamente, sem normalização.",
                "tower-a",
                NotifyFieldsChanged);

            _displayName = new TextFieldViewModel(
                "Nome do projeto", "O nome legível mostrado nos relatórios.", "Tower A", NotifyFieldsChanged);

            _index = new PathFieldViewModel(
                "Índice de runs", "O run-index JSON já criado.",
                dialogs, FilePickerFileKind.RunIndexJson, onChanged: NotifyFieldsChanged);

            _report = new PathFieldViewModel(
                "Destino do relatório longitudinal",
                "Onde o HTML do projeto será escrito. O ficheiro ainda não precisa de existir, mas a pasta sim.",
                dialogs, FilePickerFileKind.HtmlReport,
                isDestination: true, suggestedFileName: "longitudinal.html", onChanged: NotifyFieldsChanged);

            _output = new PathFieldViewModel(
                "Destino do catálogo",
                "Onde o project.json será criado. O catálogo é sempre criado de novo.",
                dialogs, FilePickerFileKind.ProjectCatalogJson,
                isDestination: true, suggestedFileName: "project.json", onChanged: NotifyFieldsChanged);

            RegisterField(_projectId);
            RegisterField(_displayName);
            RegisterField(_index);
            RegisterField(_report);
            RegisterField(_output);

            Notes.Add("O índice, os snapshots e o relatório têm de ficar dentro da árvore de pastas do catálogo.");
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
