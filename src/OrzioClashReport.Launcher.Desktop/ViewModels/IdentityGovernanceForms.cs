using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using OrzioClashReport.Launcher.Contracts.Operations;
using OrzioClashReport.Launcher.Desktop.Platform;

namespace OrzioClashReport.Launcher.Desktop.ViewModels
{
    /// <summary>
    /// One zero-based occurrence index inside a run snapshot, entered as text and validated for
    /// operational format only. Whether the occurrence actually exists is the engine's judgement.
    /// </summary>
    public sealed partial class OccurrenceIndexFieldViewModel : ViewModelBase, IFormField
    {
        [ObservableProperty]
        private string? _value;

        public OccurrenceIndexFieldViewModel(string label)
        {
            Label = label ?? throw new ArgumentNullException(nameof(label));
        }

        public string Label { get; }

        public string Hint =>
            "Índice da ocorrência dentro do snapshot, a começar em 0. A existência é verificada pelo motor.";

        public bool HasValue => TryParse(out _);

        public int? Index => TryParse(out int parsed) ? parsed : null;

        public string? FormatError => Value == null || Value.Trim().Length == 0 || HasValue
            ? null
            : "Indique um número inteiro maior ou igual a 0.";

        public bool HasFormatError => FormatError != null;

        private bool TryParse(out int parsed)
        {
            parsed = 0;
            string? text = Value?.Trim();

            return text != null
                && text.Length > 0
                && int.TryParse(text, NumberStyles.None, CultureInfo.InvariantCulture, out parsed)
                && parsed >= 0;
        }

        partial void OnValueChanged(string? value)
        {
            _ = value;
            OnPropertyChanged(nameof(HasValue));
            OnPropertyChanged(nameof(Index));
            OnPropertyChanged(nameof(FormatError));
            OnPropertyChanged(nameof(HasFormatError));
            Changed?.Invoke();
        }

        public event Action? Changed;
    }

    /// <summary>
    /// The two decisions a human may record, presented so they are never distinguishable by colour
    /// alone: each carries its own glyph and its own words.
    /// </summary>
    public sealed class DecisionKindOptionViewModel
    {
        private DecisionKindOptionViewModel(
            HumanIdentityDecisionKind kind, string label, string glyph, string explanation)
        {
            Kind = kind;
            Label = label;
            Glyph = glyph;
            Explanation = explanation;
        }

        public HumanIdentityDecisionKind Kind { get; }

        /// <summary>The visible label. The canonical value sent to the engine is never translated.</summary>
        public string Label { get; }

        public string Glyph { get; }

        public string Explanation { get; }

        public bool IsConfirmation => Kind == HumanIdentityDecisionKind.ConfirmSameIdentity;

        public static DecisionKindOptionViewModel Confirm { get; } = new(
            HumanIdentityDecisionKind.ConfirmSameIdentity,
            "Confirmar mesma identidade",
            "=",
            "Declara que as duas ocorrências são o mesmo clash persistente. Exige um identificador persistente.");

        public static DecisionKindOptionViewModel Reject { get; } = new(
            HumanIdentityDecisionKind.RejectSameIdentity,
            "Rejeitar mesma identidade",
            "≠",
            "Declara que as duas ocorrências não são o mesmo clash. Nunca leva identificador persistente.");

        public static IReadOnlyList<DecisionKindOptionViewModel> All { get; } =
            new ReadOnlyCollection<DecisionKindOptionViewModel>(new[] { Confirm, Reject });
    }

    /// <summary>
    /// The decision-kind choice, presented as an explicit pick between two clearly different things.
    /// Nothing preselects a confirmation on evidence: the human chooses.
    /// </summary>
    public sealed partial class DecisionKindFieldViewModel : ViewModelBase, IFormField
    {
        [ObservableProperty]
        private DecisionKindOptionViewModel _selected = DecisionKindOptionViewModel.Confirm;

        public string Label => "Decisão";

        public IReadOnlyList<DecisionKindOptionViewModel> Options => DecisionKindOptionViewModel.All;

        public bool IsConfirmation => Selected.IsConfirmation;

        partial void OnSelectedChanged(DecisionKindOptionViewModel value)
        {
            _ = value;
            OnPropertyChanged(nameof(IsConfirmation));
            Changed?.Invoke();
        }

        public event Action? Changed;
    }

    /// <summary>One empty project-scoped governance document.</summary>
    public sealed class CreateIdentityGovernanceFormViewModel : OperationFormViewModel
    {
        private readonly TextFieldViewModel _projectId;
        private readonly FileFieldViewModel _output;

        public CreateIdentityGovernanceFormViewModel(
            OperationRunnerViewModel runner, EngineStatusViewModel engine, IFileDialogs dialogs)
            : base(
                "Criar documento de governança",
                "Cria um documento vazio ligado a um projeto. As decisões são acrescentadas uma a uma, depois.",
                LauncherOperationKind.CreateIdentityGovernance,
                runner,
                engine)
        {
            _projectId = Add(new TextFieldViewModel(
                "Identificador do projeto", "Tem de ser exatamente o mesmo do catálogo do projeto."));
            _output = Add(FileFieldViewModel.Destination(
                "Documento de governança",
                "Onde guardar o documento.",
                dialogs,
                PickedFileKind.IdentityGovernanceJson,
                () => "identity-governance.json"));
        }

        public override bool CanBuild => _projectId.HasValue && _output.HasValue;

        protected override LauncherOperationRequest Build() =>
            new CreateIdentityGovernanceRequest(_projectId.Trimmed, _output.Path!);
    }

    /// <summary>
    /// One explicit human decision, appended to an existing governance document.
    /// An algorithmic suggestion is not a decision, and a High confidence match is not a
    /// confirmation: nothing on this form is filled in on the human's behalf.
    /// </summary>
    public sealed partial class AppendIdentityDecisionFormViewModel : OperationFormViewModel
    {
        private readonly FileFieldViewModel _governance;
        private readonly TextFieldViewModel _decisionId;
        private readonly TextFieldViewModel _leftRunId;
        private readonly OccurrenceIndexFieldViewModel _leftOccurrence;
        private readonly TextFieldViewModel _rightRunId;
        private readonly OccurrenceIndexFieldViewModel _rightOccurrence;
        private readonly TextFieldViewModel _reviewerAlias;
        private readonly TextFieldViewModel _persistentIdentityId;
        private readonly TextFieldViewModel _reason;
        private readonly DecisionKindFieldViewModel _decisionKind;

        public AppendIdentityDecisionFormViewModel(
            OperationRunnerViewModel runner, EngineStatusViewModel engine, IFileDialogs dialogs)
            : base(
                "Registar decisão humana",
                "Uma sugestão do algoritmo nunca é uma decisão. Confiança alta não significa confirmado. "
                    + "A identidade persistente só existe quando um humano a confirma aqui.",
                LauncherOperationKind.AppendIdentityDecision,
                runner,
                engine)
        {
            _governance = Add(FileFieldViewModel.Input(
                "Documento de governança",
                "O documento onde a decisão é acrescentada.",
                dialogs,
                PickedFileKind.IdentityGovernanceJson));
            _decisionId = Add(new TextFieldViewModel(
                "Identificador da decisão", "Um identificador estável desta decisão."));
            _decisionKind = Add(new DecisionKindFieldViewModel());
            _leftRunId = Add(new TextFieldViewModel("Run à esquerda", "O run id do lado esquerdo."));
            _leftOccurrence = Add(new OccurrenceIndexFieldViewModel("Ocorrência à esquerda"));
            _rightRunId = Add(new TextFieldViewModel("Run à direita", "O run id do lado direito."));
            _rightOccurrence = Add(new OccurrenceIndexFieldViewModel("Ocorrência à direita"));
            _reviewerAlias = Add(new TextFieldViewModel(
                "Alias do revisor", "Um alias. Não é preciso nome real, email ou login."));
            _persistentIdentityId = Add(new TextFieldViewModel(
                "Identificador persistente", "Obrigatório numa confirmação. Nunca usado numa rejeição."));
            _decisionKind.Changed += ApplyDecisionKind;
            _reason = Add(new TextFieldViewModel("Motivo", "Opcional."));

        }

        public DecisionKindFieldViewModel DecisionKind => _decisionKind;

        public bool IsConfirmation => _decisionKind.IsConfirmation;

        public override bool CanBuild =>
            _governance.HasValue
            && _decisionId.HasValue
            && _leftRunId.HasValue
            && _leftOccurrence.HasValue
            && _rightRunId.HasValue
            && _rightOccurrence.HasValue
            && _reviewerAlias.HasValue
            && (!IsConfirmation || _persistentIdentityId.HasValue);

        protected override LauncherOperationRequest Build()
        {
            var left = new IdentityEvidenceEndpoint(_leftRunId.Trimmed, _leftOccurrence.Index!.Value);
            var right = new IdentityEvidenceEndpoint(_rightRunId.Trimmed, _rightOccurrence.Index!.Value);
            string? reason = _reason.HasValue ? _reason.Trimmed : null;

            // A rejection never carries a persistent identity id, whatever was typed before the
            // human switched the decision kind.
            return IsConfirmation
                ? AppendIdentityDecisionRequest.Confirm(
                    _governance.Path!,
                    _decisionId.Trimmed,
                    left,
                    right,
                    _reviewerAlias.Trimmed,
                    _persistentIdentityId.Trimmed,
                    reason)
                : AppendIdentityDecisionRequest.Reject(
                    _governance.Path!,
                    _decisionId.Trimmed,
                    left,
                    right,
                    _reviewerAlias.Trimmed,
                    reason);
        }

        /// <summary>
        /// The persistent identity field exists only for a confirmation. Hiding it is not enough:
        /// a rejection is built without it whatever was typed before the human switched.
        /// </summary>
        private void ApplyDecisionKind()
        {
            _persistentIdentityId.IsShown = IsConfirmation;
            OnPropertyChanged(nameof(IsConfirmation));
        }
    }

    /// <summary>Read-only validation of a governance document against a project's indexed snapshots.</summary>
    public sealed class ValidateIdentityGovernanceFormViewModel : OperationFormViewModel
    {
        private readonly FileFieldViewModel _project;
        private readonly FileFieldViewModel _governance;

        public ValidateIdentityGovernanceFormViewModel(
            OperationRunnerViewModel runner, EngineStatusViewModel engine, IFileDialogs dialogs)
            : base(
                "Validar governança",
                "Verifica apenas a ligação ao projeto e a existência dos pontos de evidência. Não escreve nada.",
                LauncherOperationKind.ValidateIdentityGovernance,
                runner,
                engine)
        {
            _project = Add(FileFieldViewModel.Input(
                "Projeto", "O catálogo do projeto.", dialogs, PickedFileKind.ProjectCatalogJson));
            _governance = Add(FileFieldViewModel.Input(
                "Documento de governança",
                "O documento a validar.",
                dialogs,
                PickedFileKind.IdentityGovernanceJson));
        }

        public override bool CanBuild => _project.HasValue && _governance.HasValue;

        protected override LauncherOperationRequest Build() =>
            new ValidateIdentityGovernanceRequest(_project.Path!, _governance.Path!);
    }

    /// <summary>One standalone HTML review of the persisted human decisions.</summary>
    public sealed class RenderIdentityGovernanceReportFormViewModel : OperationFormViewModel
    {
        private readonly FileFieldViewModel _project;
        private readonly FileFieldViewModel _governance;
        private readonly FileFieldViewModel _output;

        public RenderIdentityGovernanceReportFormViewModel(
            OperationRunnerViewModel runner, EngineStatusViewModel engine, IFileDialogs dialogs)
            : base(
                "Gerar revisão de governança",
                "Apresenta as decisões persistidas pela ordem em que foram registadas. É um artefacto derivado, "
                    + "não evidência, e o motor só o gera depois de a validação passar.",
                LauncherOperationKind.RenderIdentityGovernanceReport,
                runner,
                engine)
        {
            _project = Add(FileFieldViewModel.Input(
                "Projeto", "O catálogo do projeto.", dialogs, PickedFileKind.ProjectCatalogJson));
            _governance = Add(FileFieldViewModel.Input(
                "Documento de governança",
                "O documento a apresentar.",
                dialogs,
                PickedFileKind.IdentityGovernanceJson));
            _output = Add(FileFieldViewModel.Destination(
                "Relatório de revisão",
                "Onde guardar a revisão.",
                dialogs,
                PickedFileKind.HtmlReport,
                () => "identity-governance.html"));
        }

        public override bool CanBuild => _project.HasValue && _governance.HasValue && _output.HasValue;

        protected override LauncherOperationRequest Build() =>
            new RenderIdentityGovernanceReportRequest(_project.Path!, _governance.Path!, _output.Path!);
    }
}
