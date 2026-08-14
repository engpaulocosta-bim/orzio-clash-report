using System;
using System.Linq;
using OrzioClashReport.Launcher.Application.Engine;
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
                "Registar decisão",
                "Regista uma decisão humana explícita sobre duas ocorrências. Nenhuma sugestão do algoritmo se torna decisão sozinha.",
                job,
                engineStatus)
        {
            _governance = new PathFieldViewModel(
                "Documento de governança", "O identity-governance.json onde a decisão será acrescentada.",
                dialogs, FilePickerFileKind.IdentityGovernanceJson, onChanged: NotifyFieldsChanged);

            _decisionId = new TextFieldViewModel(
                "Identificador da decisão", "O seu identificador para esta decisão.", "d-001", NotifyFieldsChanged);

            _kind = new ChoiceFieldViewModel(
                "Decisão",
                "Confirmar regista que são o mesmo clash. Rejeitar regista que não são.",
                Enum.GetValues<IdentityDecisionKind>()
                    .Select(kind => IdentityDecisionPresentation.For(kind))
                    .Select(presentation => new ChoiceOptionViewModel(
                        presentation.CanonicalValue,
                        presentation.Label,
                        presentation.Glyph,
                        presentation.Description,
                        presentation.Severity))
                    .ToList(),
                OnKindChanged);

            _leftRunId = new TextFieldViewModel(
                "Run id (esquerda)", "O run da primeira ocorrência.", "run-001", NotifyFieldsChanged);

            _leftOccurrenceIndex = new TextFieldViewModel(
                "Índice da ocorrência (esquerda)",
                "Zero-based: a primeira ocorrência do run é 0.",
                "0",
                NotifyFieldsChanged,
                validate: ValidateOccurrenceIndex);

            _rightRunId = new TextFieldViewModel(
                "Run id (direita)", "O run da segunda ocorrência.", "run-002", NotifyFieldsChanged);

            _rightOccurrenceIndex = new TextFieldViewModel(
                "Índice da ocorrência (direita)",
                "Zero-based: a primeira ocorrência do run é 0.",
                "0",
                NotifyFieldsChanged,
                validate: ValidateOccurrenceIndex);

            _reviewerAlias = new TextFieldViewModel(
                "Alias do revisor",
                "Um alias basta. Não é pedido nome real, email nem login.",
                "coordenador-a",
                NotifyFieldsChanged);

            _persistentIdentityId = new TextFieldViewModel(
                "Identificador persistente",
                "Obrigatório apenas numa confirmação. Uma rejeição nunca o transporta.",
                "clash-042",
                NotifyFieldsChanged,
                isOptional: true);

            _reason = new TextFieldViewModel(
                "Motivo", "Opcional. Fica registado tal como o escrever.", string.Empty,
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

            Notes.Add("Uma confiança 'High' do algoritmo não é uma confirmação humana. A decisão é sempre sua.");
            Notes.Add("A existência dos runs e das ocorrências é verificada pelo motor, não por esta janela.");
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
                ?? throw new ArgumentException("Escolha confirmar ou rejeitar antes de registar a decisão.");

            if (!IdentityDecisionDraft.TryParseOccurrenceIndex(_leftOccurrenceIndex.Value, out int left))
            {
                throw new ArgumentException("O índice de ocorrência da esquerda tem de ser um inteiro maior ou igual a zero.");
            }

            if (!IdentityDecisionDraft.TryParseOccurrenceIndex(_rightOccurrenceIndex.Value, out int right))
            {
                throw new ArgumentException("O índice de ocorrência da direita tem de ser um inteiro maior ou igual a zero.");
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
                : "Tem de ser um inteiro maior ou igual a zero.";
    }
}
