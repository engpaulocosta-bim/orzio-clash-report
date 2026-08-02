using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using OrzioClashReport.Launcher.Contracts.Operations;
using OrzioClashReport.Launcher.Desktop.Localization;
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

        private readonly string _labelKey;

        public OccurrenceIndexFieldViewModel(string labelKey)
        {
            _labelKey = labelKey ?? throw new ArgumentNullException(nameof(labelKey));
        }

        public string Label => Text(_labelKey);

        public string Hint => Text("Field.Occurrence.Hint");

        public bool HasValue => TryParse(out _);

        public int? Index => TryParse(out int parsed) ? parsed : null;

        public string? FormatError => Value == null || Value.Trim().Length == 0 || HasValue
            ? null
            : Text("Field.Occurrence.FormatError");

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
    public sealed class DecisionKindOptionViewModel : ViewModelBase
    {
        private readonly string _labelKey;
        private readonly string _explanationKey;

        private DecisionKindOptionViewModel(
            HumanIdentityDecisionKind kind, string labelKey, string glyph, string explanationKey)
        {
            Kind = kind;
            _labelKey = labelKey;
            Glyph = glyph;
            _explanationKey = explanationKey;
        }

        public HumanIdentityDecisionKind Kind { get; }

        /// <summary>The visible label. The canonical value sent to the engine is never translated.</summary>
        public string Label => Text(_labelKey);

        public string Glyph { get; }

        public string Explanation => Text(_explanationKey);

        public bool IsConfirmation => Kind == HumanIdentityDecisionKind.ConfirmSameIdentity;

        public static DecisionKindOptionViewModel Confirm { get; } = new(
            HumanIdentityDecisionKind.ConfirmSameIdentity,
            "Decision.Confirm.Label",
            "=",
            "Decision.Confirm.Explanation");

        public static DecisionKindOptionViewModel Reject { get; } = new(
            HumanIdentityDecisionKind.RejectSameIdentity,
            "Decision.Reject.Label",
            "≠",
            "Decision.Reject.Explanation");

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

        public string Label => Text("Decision.Label");

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
                "Form.CreateGovernance.Title",
                "Form.CreateGovernance.Description",
                LauncherOperationKind.CreateIdentityGovernance,
                runner,
                engine)
        {
            _projectId = Add(new TextFieldViewModel(
                "Field.ProjectId.Label", "Field.ProjectId.GovernanceHint"));
            _output = Add(FileFieldViewModel.Destination(
                "Field.Governance.Label",
                "Field.Governance.DestinationHint",
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
                "Form.AppendDecision.Title",
                "Form.AppendDecision.Description",
                LauncherOperationKind.AppendIdentityDecision,
                runner,
                engine)
        {
            _governance = Add(FileFieldViewModel.Input(
                "Field.Governance.Label",
                "Field.Governance.AppendHint",
                dialogs,
                PickedFileKind.IdentityGovernanceJson));
            _decisionId = Add(new TextFieldViewModel("Field.DecisionId.Label", "Field.DecisionId.Hint"));
            _decisionKind = Add(new DecisionKindFieldViewModel());
            _leftRunId = Add(new TextFieldViewModel("Field.LeftRunId.Label", "Field.LeftRunId.Hint"));
            _leftOccurrence = Add(new OccurrenceIndexFieldViewModel("Field.LeftOccurrence.Label"));
            _rightRunId = Add(new TextFieldViewModel("Field.RightRunId.Label", "Field.RightRunId.Hint"));
            _rightOccurrence = Add(new OccurrenceIndexFieldViewModel("Field.RightOccurrence.Label"));
            _reviewerAlias = Add(new TextFieldViewModel(
                "Field.ReviewerAlias.Label", "Field.ReviewerAlias.Hint"));
            _persistentIdentityId = Add(new TextFieldViewModel(
                "Field.PersistentIdentityId.Label", "Field.PersistentIdentityId.Hint"));
            _decisionKind.Changed += ApplyDecisionKind;
            _reason = Add(new TextFieldViewModel("Field.Reason.Label", "Field.Reason.Hint"));

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
                "Form.ValidateGovernance.Title",
                "Form.ValidateGovernance.Description",
                LauncherOperationKind.ValidateIdentityGovernance,
                runner,
                engine)
        {
            _project = Add(FileFieldViewModel.Input(
                "Field.Project.Label", "Field.Project.Hint", dialogs, PickedFileKind.ProjectCatalogJson));
            _governance = Add(FileFieldViewModel.Input(
                "Field.Governance.Label",
                "Field.Governance.ValidateHint",
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
                "Form.RenderGovernanceReport.Title",
                "Form.RenderGovernanceReport.Description",
                LauncherOperationKind.RenderIdentityGovernanceReport,
                runner,
                engine)
        {
            _project = Add(FileFieldViewModel.Input(
                "Field.Project.Label", "Field.Project.Hint", dialogs, PickedFileKind.ProjectCatalogJson));
            _governance = Add(FileFieldViewModel.Input(
                "Field.Governance.Label",
                "Field.Governance.RenderHint",
                dialogs,
                PickedFileKind.IdentityGovernanceJson));
            _output = Add(FileFieldViewModel.Destination(
                "Field.ReviewReport.Label",
                "Field.ReviewReport.Hint",
                dialogs,
                PickedFileKind.HtmlReport,
                () => "identity-governance.html"));
        }

        public override bool CanBuild => _project.HasValue && _governance.HasValue && _output.HasValue;

        protected override LauncherOperationRequest Build() =>
            new RenderIdentityGovernanceReportRequest(_project.Path!, _governance.Path!, _output.Path!);
    }
}
