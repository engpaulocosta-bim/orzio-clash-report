using OrzioClashReport.Launcher.Application.Engine;
using OrzioClashReport.Launcher.Application.Operations;
using OrzioClashReport.Launcher.Contracts.Operations;
using OrzioClashReport.Launcher.Desktop.Platform;
using OrzioClashReport.Launcher.Desktop.ViewModels.Fields;

namespace OrzioClashReport.Launcher.Desktop.ViewModels.Operations
{
    /// <summary>Traverses the adjacent pairs of an already-declared run index.</summary>
    public sealed class CompareIndexFormViewModel : OperationFormViewModel
    {
        private readonly PathFieldViewModel _index;
        private readonly PathFieldViewModel _output;

        public CompareIndexFormViewModel(
            JobViewModel job, EngineStatusViewModel engineStatus, IFileDialogService dialogs)
            : base(
                LauncherOperationKind.CompareIndex,
                "op.compareIndex.title",
                "op.compareIndex.description",
                job,
                engineStatus)
        {
            _index = new PathFieldViewModel(
                "op.compareIndex.index.label", "op.compareIndex.index.description",
                dialogs, FilePickerFileKind.RunIndexJson, onChanged: NotifyFieldsChanged);

            _output = new PathFieldViewModel(
                "op.compareIndex.output.label", "op.compareIndex.output.description",
                dialogs, FilePickerFileKind.HtmlReport,
                isDestination: true, suggestedFileName: "longitudinal.html", onChanged: NotifyFieldsChanged);

            RegisterField(_index);
            RegisterField(_output);

            AddNote("op.compareIndex.note.1");
            AddNote("op.compareIndex.note.2");
        }

        protected override string? OutputPath => _output.Value.Length == 0 ? null : _output.Value;

        protected override LauncherOperationRequest BuildRequest(OutputCollisionDecision decision) =>
            new LauncherOperationRequest(
                LauncherOperationKind.CompareIndex,
                EngineArgumentBuilder.CompareIndex(_index.Value, _output.Value),
                WorkingDirectoryFor(_output.Value),
                _output.Value,
                decision,
                System.IO.Path.GetFileName(_output.Value));
    }
}
