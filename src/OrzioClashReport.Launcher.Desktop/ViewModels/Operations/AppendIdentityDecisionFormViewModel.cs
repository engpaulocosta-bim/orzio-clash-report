using System;
using System.Linq;
using OrzioClashReport.Launcher.Application.Engine;
using OrzioClashReport.Launcher.Application.Localization;
using OrzioClashReport.Launcher.Application.Operations;
using OrzioClashReport.Launcher.Application.Presentation;
using OrzioClashReport.Launcher.Contracts.Operations;
using OrzioClashReport.Launcher.Desktop.Platform;
using OrzioClashReport.Launcher.Desktop.ViewModels.Fields;

namespace OrzioClashReport.Launcher.Desktop.ViewModels.Operations
{
    /// <summary>
    /// Records one explicit human decision. The launcher collects it and passes it through; it never
    /// proposes a pairing, never turns an algorithmic suggestion into a decision, and never infers a
    /// persistent identity.
    /// </summary>
    public sealed class AppendIdentityDecisionFormViewModel : OperationFormViewModel
    {
        private readonly PathFieldViewModel _governance;
        private readonly TextFieldViewModel _decisionId;
        private readonly ChoiceFieldViewModel _kind;
        private readonly TextFieldViewModel _leftRunId;
        private readonly TextFieldViewModel _leftOccurrenceIndex;
        private readonly TextFieldViewModel _rightRunId;
        private readonly TextFieldViewModel _rightOccurrenceIndex;
        private readonly TextFieldViewModel _reviewerAlias;
        private readonly TextFieldViewModel _persistentIdentityId;
        private readonly TextFieldViewModel _reason;

        public AppendIdentityDecisionFormViewModel(
            JobViewModel job, EngineStatusViewModel engineStatus, IFileDialogService dialogs)
            : base(
                LauncherOperationKind.AppendIdentityDecision,
                "op.appendDecision.title",
                "op.appendDecision.description",
                job,
                engineStatus)
        {
            _governance = new PathFieldViewModel(
                "op.appendDecision.governance.label", "op.appendDecision.governance.description",
                dialogs, FilePickerFileKind.IdentityGovernanceJson, onChanged: NotifyFieldsChanged);

            _decisionId = new TextFieldViewModel(
                "op.appendDecision.decisionId.label", "op.appendDecision.decisionId.description", "d-001", NotifyFieldsChanged);

            _kind = new ChoiceFieldViewModel(
                "op.appendDecision.kind.label",
                "op.appendDecision.kind.description",
                Enum.GetValues<IdentityDecisionKind>()
                    .Select(kind => IdentityDecisionPresentation.For(kind))
                    .Select(presentation => new ChoiceOptionViewModel(
                        presentation.CanonicalValue,
                        () => presentation.Label,
                        presentation.Glyph,
                        () => presentation.Description,
                        presentation.Severity))
                    .ToList(),
                OnKindChanged);

            _leftRunId = new TextFieldViewModel(
                "op.appendDecision.leftRun.label", "op.appendDecision.leftRun.description", "run-001", NotifyFieldsChanged);

            _leftOccurrenceIndex = new TextFieldViewModel(
                "op.appendDecision.leftIndex.label",
                "op.appendDecision.occurrenceIndex.description",
                "0",
                NotifyFieldsChanged,
                validate: ValidateOccurrenceIndex);

            _rightRunId = new TextFieldViewModel(
                "op.appendDecision.rightRun.label", "op.appendDecision.rightRun.description", "run-002", NotifyFieldsChanged);

            _rightOccurrenceIndex = new TextFieldViewModel(
                "op.appendDecision.rightIndex.label",
                "op.appendDecision.occurrenceIndex.description",
                "0",
                NotifyFieldsChanged,
                validate: ValidateOccurrenceIndex);

            _reviewerAlias = new TextFieldViewModel(
                "op.appendDecision.alias.label",
                "op.appendDecision.alias.description",
                watermarkKey: "op.appendDecision.alias.example",
                onChanged: NotifyFieldsChanged);

            _persistentIdentityId = new TextFieldViewModel(
                "op.appendDecision.persistentId.label",
                "op.appendDecision.persistentId.description",
                "clash-042",
                NotifyFieldsChanged,
                isOptional: true);

            _reason = new TextFieldViewModel(
                "op.appendDecision.reason.label", "op.appendDecision.reason.description", string.Empty,
                NotifyFieldsChanged, isOptional: true);

            RegisterField(_governance);
            RegisterField(_decisionId);
            RegisterField(_kind);
            RegisterField(_leftRunId);
            RegisterField(_leftOccurrenceIndex);
            RegisterField(_rightRunId);
            RegisterField(_rightOccurrenceIndex);
            RegisterField(_reviewerAlias);
            RegisterField(_persistentIdentityId);
            RegisterField(_reason);

            AddNote("op.appendDecision.note.1");
            AddNote("op.appendDecision.note.2");
        }

        /// <summary>There is no destination: the engine replaces the governance file it already owns.</summary>
        protected override string? OutputPath => null;

        protected override bool CanRun()
        {
            if (!base.CanRun())
            {
                return false;
            }

            IdentityDecisionKind? kind = SelectedKind();
            if (kind == null)
            {
                return false;
            }

            bool hasPersistentId = _persistentIdentityId.Value.Trim().Length > 0;

            // A confirmation needs an identity id; a rejection must not have one. Both are enforced
            // again when the draft is constructed, so neither can slip through.
            return kind == IdentityDecisionKind.ConfirmSameIdentity ? hasPersistentId : !hasPersistentId;
        }

        protected override LauncherOperationRequest BuildRequest(OutputCollisionDecision decision)
        {
            IdentityDecisionKind kind = SelectedKind()
                ?? throw new ArgumentException("op.appendDecision.error.chooseKind");

            if (!IdentityDecisionDraft.TryParseOccurrenceIndex(_leftOccurrenceIndex.Value, out int left))
            {
                throw new ArgumentException(Loc.T("op.appendDecision.error.leftIndex"));
            }

            if (!IdentityDecisionDraft.TryParseOccurrenceIndex(_rightOccurrenceIndex.Value, out int right))
            {
                throw new ArgumentException(Loc.T("op.appendDecision.error.rightIndex"));
            }

            var draft = new IdentityDecisionDraft(
                kind,
                _decisionId.Value,
                _leftRunId.Value,
                left,
                _rightRunId.Value,
                right,
                _reviewerAlias.Value,
                _persistentIdentityId.Value,
                _reason.Value);

            return new LauncherOperationRequest(
                LauncherOperationKind.AppendIdentityDecision,
                EngineArgumentBuilder.AppendIdentityDecision(_governance.Value, draft),
                WorkingDirectoryFor(_governance.Value),
                null,
                decision,
                System.IO.Path.GetFileName(_governance.Value));
        }

        private IdentityDecisionKind? SelectedKind()
        {
            ChoiceOptionViewModel? selected = _kind.Selected;
            if (selected == null)
            {
                return null;
            }

            return Enum.TryParse(selected.Value, ignoreCase: false, out IdentityDecisionKind kind) ? kind : null;
        }

        private void OnKindChanged()
        {
            if (SelectedKind() == IdentityDecisionKind.RejectSameIdentity)
            {
                // Clearing it is what makes "a rejection never carries an identity id" true in the UI
                // as well as in the contract, instead of leaving a stale value the user has to notice.
                _persistentIdentityId.Value = string.Empty;
            }

            NotifyFieldsChanged();
        }

        private static string? ValidateOccurrenceIndex(string value) =>
            IdentityDecisionDraft.TryParseOccurrenceIndex(value, out _)
                ? null
                : Loc.T("op.appendDecision.validation.occurrenceIndex");
    }
}
