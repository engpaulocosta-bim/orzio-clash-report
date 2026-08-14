using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using OrzioClashReport.Launcher.Application.Operations;
using OrzioClashReport.Launcher.Contracts.Engine;
using OrzioClashReport.Launcher.Contracts.Jobs;
using OrzioClashReport.Launcher.Contracts.Logging;
using OrzioClashReport.Launcher.Contracts.Operations;
using OrzioClashReport.Launcher.Contracts.Platform;
using OrzioClashReport.Launcher.Desktop.Platform;
using OrzioClashReport.Launcher.Desktop.ViewModels;
using OrzioClashReport.Launcher.Desktop.ViewModels.Fields;
using OrzioClashReport.Launcher.Desktop.ViewModels.Operations;
using OrzioClashReport.Launcher.Desktop.Views;

namespace OrzioClashReport.Launcher.Desktop.Tests
{
    /// <summary>
    /// Renders the governance forms and checks the rules that matter most in the whole product:
    /// confirm and reject look different, a confirmation cannot be recorded without an identity id,
    /// and a rejection can never carry one.
    /// </summary>
    public sealed class GovernanceFormSmokeTests
    {
        [AvaloniaFact]
        public void TheDecisionFormRenders()
        {
            AppendIdentityDecisionFormViewModel form = CreateDecisionForm();
            var window = new Window { Content = new OperationFormView { DataContext = form } };

            window.Show();

            Assert.Equal(10, form.Fields.Count);
        }

        [AvaloniaFact]
        public void NoDecisionIsPreSelected()
        {
            AppendIdentityDecisionFormViewModel form = CreateDecisionForm();
            var window = new Window { Content = new OperationFormView { DataContext = form } };
            window.Show();

            ChoiceFieldViewModel kind = form.Fields.OfType<ChoiceFieldViewModel>().Single();

            Assert.Null(kind.Selected);
            Assert.False(kind.IsComplete);
            Assert.False(form.RunCommand.CanExecute(null));
        }

        [AvaloniaFact]
        public void ConfirmAndRejectAreDistinguishableWithoutColour()
        {
            AppendIdentityDecisionFormViewModel form = CreateDecisionForm();
            ChoiceFieldViewModel kind = form.Fields.OfType<ChoiceFieldViewModel>().Single();

            Assert.Equal(2, kind.Options.Count);

            ChoiceOptionViewModel confirm = kind.Options.Single(
                option => option.Value == nameof(IdentityDecisionKind.ConfirmSameIdentity));
            ChoiceOptionViewModel reject = kind.Options.Single(
                option => option.Value == nameof(IdentityDecisionKind.RejectSameIdentity));

            Assert.NotEqual(confirm.Glyph, reject.Glyph);
            Assert.NotEqual(confirm.Label, reject.Label);
            Assert.NotEqual(confirm.Description, reject.Description);
        }

        [AvaloniaFact]
        public void SelectingOneDecisionDeselectsTheOther()
        {
            AppendIdentityDecisionFormViewModel form = CreateDecisionForm();
            ChoiceFieldViewModel kind = form.Fields.OfType<ChoiceFieldViewModel>().Single();

            kind.Options[0].IsSelected = true;
            Assert.Same(kind.Options[0], kind.Selected);
            Assert.False(kind.Options[1].IsSelected);

            kind.Options[1].IsSelected = true;
            Assert.Same(kind.Options[1], kind.Selected);
            Assert.False(kind.Options[0].IsSelected);
        }

        [AvaloniaFact]
        public void AConfirmationCannotBeRecordedWithoutAPersistentIdentityId()
        {
            AppendIdentityDecisionFormViewModel form = CreateDecisionForm();
            FillCommonFields(form);
            SelectKind(form, IdentityDecisionKind.ConfirmSameIdentity);

            Assert.False(form.RunCommand.CanExecute(null));

            Field(form, "Identificador persistente").Value = "clash-042";
            Assert.True(form.RunCommand.CanExecute(null));
        }

        [AvaloniaFact]
        public void ChoosingRejectClearsAnyPersistentIdentityIdThatWasTyped()
        {
            AppendIdentityDecisionFormViewModel form = CreateDecisionForm();
            FillCommonFields(form);
            SelectKind(form, IdentityDecisionKind.ConfirmSameIdentity);

            TextFieldViewModel persistentId = Field(form, "Identificador persistente");
            persistentId.Value = "clash-042";

            SelectKind(form, IdentityDecisionKind.RejectSameIdentity);

            Assert.Equal(string.Empty, persistentId.Value);
            Assert.True(form.RunCommand.CanExecute(null));
        }

        [AvaloniaFact]
        public void AnInvalidOccurrenceIndexBlocksTheFormAndSaysWhy()
        {
            AppendIdentityDecisionFormViewModel form = CreateDecisionForm();
            FillCommonFields(form);
            SelectKind(form, IdentityDecisionKind.RejectSameIdentity);

            TextFieldViewModel left = Field(form, "Índice da ocorrência (esquerda)");

            left.Value = "-1";
            Assert.True(left.HasValidationMessage);
            Assert.False(form.RunCommand.CanExecute(null));

            left.Value = "abc";
            Assert.False(form.RunCommand.CanExecute(null));

            left.Value = "0";
            Assert.False(left.HasValidationMessage);
            Assert.True(form.RunCommand.CanExecute(null));
        }

        [AvaloniaFact]
        public void TheReasonIsOptional()
        {
            AppendIdentityDecisionFormViewModel form = CreateDecisionForm();
            FillCommonFields(form);
            SelectKind(form, IdentityDecisionKind.RejectSameIdentity);

            Assert.Equal(string.Empty, Field(form, "Motivo").Value);
            Assert.True(form.RunCommand.CanExecute(null));
        }

        [AvaloniaFact]
        public void EveryGovernanceFormRenders()
        {
            var engineStatus = new EngineStatusViewModel();
            var dialogs = new NullFileDialogService();

            var forms = new OperationFormViewModel[]
            {
                new CreateGovernanceFormViewModel(CreateJob(), engineStatus, dialogs),
                new AppendIdentityDecisionFormViewModel(CreateJob(), engineStatus, dialogs),
                new ValidateGovernanceFormViewModel(CreateJob(), engineStatus, dialogs),
                new RenderGovernanceReportFormViewModel(CreateJob(), engineStatus, dialogs),
            };

            foreach (OperationFormViewModel form in forms)
            {
                var window = new Window { Content = new OperationFormView { DataContext = form } };
                window.Show();

                Assert.NotEmpty(form.Notes);

                window.Close();
            }
        }

        private static void FillCommonFields(AppendIdentityDecisionFormViewModel form)
        {
            form.Fields.OfType<PathFieldViewModel>().Single().Value =
                Path.Combine(Path.GetTempPath(), "identity-governance.json");

            Field(form, "Identificador da decisão").Value = "d-001";
            Field(form, "Run id (esquerda)").Value = "run-001";
            Field(form, "Índice da ocorrência (esquerda)").Value = "0";
            Field(form, "Run id (direita)").Value = "run-002";
            Field(form, "Índice da ocorrência (direita)").Value = "3";
            Field(form, "Alias do revisor").Value = "coordenador-a";
        }

        private static void SelectKind(AppendIdentityDecisionFormViewModel form, IdentityDecisionKind kind)
        {
            ChoiceFieldViewModel choice = form.Fields.OfType<ChoiceFieldViewModel>().Single();
            choice.Options.Single(option => option.Value == kind.ToString()).IsSelected = true;
        }

        private static TextFieldViewModel Field(OperationFormViewModel form, string label) =>
            form.Fields.OfType<TextFieldViewModel>().Single(field => field.Label == label);

        private static AppendIdentityDecisionFormViewModel CreateDecisionForm()
        {
            var engineStatus = new EngineStatusViewModel();
            engineStatus.Update(ReadyEngine());

            return new AppendIdentityDecisionFormViewModel(CreateJob(), engineStatus, new NullFileDialogService());
        }

        private static EngineInfo ReadyEngine() =>
            new EngineInfo(
                EngineStatusKind.Ready,
                "0.1.0-preview.3",
                "0.1.0-preview.3",
                new EngineLocation("/install/engine/orzioclash", "/install/engine/engine-manifest.json"),
                new EngineIntegrityResult(EngineIntegrityVerdict.Verified, "abc", "abc"),
                "Motor verificado.");

        private static JobViewModel CreateJob()
        {
            var executor = new LauncherOperationExecutor(
                new UnusedGateway(),
                new PermissiveProbe(),
                new InMemoryRecentItemsStore(),
                new NoOpJournal(),
                new CollectingLog(),
                new PassThroughRedactor(),
                new FixedClock(),
                Path.GetTempPath());

            return new JobViewModel(executor, new NullOutputRevealer(), new ActiveJobTracker());
        }

        private sealed class NullFileDialogService : IFileDialogService
        {
            public Task<string?> PickOpenFileAsync(string title, FilePickerFileKind kind, string? startDirectory) =>
                Task.FromResult<string?>(null);

            public Task<IReadOnlyList<string>> PickOpenFilesAsync(
                string title, FilePickerFileKind kind, string? startDirectory) =>
                Task.FromResult<IReadOnlyList<string>>(Array.Empty<string>());

            public Task<string?> PickSaveFileAsync(
                string title, FilePickerFileKind kind, string suggestedFileName, string? startDirectory) =>
                Task.FromResult<string?>(null);
        }

        private sealed class UnusedGateway : IEngineGateway
        {
            public Task<EngineInfo> DescribeAsync(CancellationToken cancellationToken) =>
                Task.FromResult(ReadyEngine());

            public Task<EngineJobResult> ExecuteAsync(
                EngineJobRequest request,
                IProgress<EngineJobProgress>? progress,
                CancellationToken cancellationToken) =>
                throw new NotSupportedException("The governance smoke tests never execute the engine.");
        }

        private sealed class PermissiveProbe : IFileProbe
        {
            public bool FileExists(string path) => false;

            public bool DirectoryExists(string path) => true;

            public long GetFileSizeInBytes(string path) => -1;
        }

        private sealed class NoOpJournal : IJobJournal
        {
            public Task BeginAsync(JobJournalEntry entry, CancellationToken cancellationToken) => Task.CompletedTask;

            public Task CompleteAsync(string jobId, CancellationToken cancellationToken) => Task.CompletedTask;

            public Task<IReadOnlyList<JobJournalEntry>> ReadInterruptedAsync(CancellationToken cancellationToken) =>
                Task.FromResult<IReadOnlyList<JobJournalEntry>>(Array.Empty<JobJournalEntry>());

            public Task ClearAsync(CancellationToken cancellationToken) => Task.CompletedTask;
        }

        private sealed class PassThroughRedactor : IPathRedactor
        {
            public RedactedPath Redact(string path) =>
                new RedactedPath(Path.GetFileName(path), Path.GetExtension(path), new string('0', 64), PathRootKind.Unknown);
        }
    }
}
