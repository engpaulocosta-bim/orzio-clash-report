using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using OrzioClashReport.Launcher.Application.Operations;
using OrzioClashReport.Launcher.Application.Presentation;
using OrzioClashReport.Launcher.Contracts.Engine;
using OrzioClashReport.Launcher.Contracts.Jobs;
using OrzioClashReport.Launcher.Contracts.Logging;
using OrzioClashReport.Launcher.Contracts.Platform;
using OrzioClashReport.Launcher.Desktop.Platform;
using OrzioClashReport.Launcher.Desktop.ViewModels;
using OrzioClashReport.Launcher.Desktop.ViewModels.Fields;
using OrzioClashReport.Launcher.Desktop.ViewModels.Operations;
using OrzioClashReport.Launcher.Desktop.Views;

namespace OrzioClashReport.Launcher.Desktop.Tests
{
    /// <summary>
    /// Renders the advanced operation forms headlessly and exercises the ordered-list behaviour that
    /// the longitudinal workflow depends on.
    /// </summary>
    public sealed class OperationFormSmokeTests
    {
        [AvaloniaFact]
        public void EveryAdvancedFormRenders()
        {
            foreach (OperationFormViewModel form in AllForms())
            {
                var view = new OperationFormView { DataContext = form };
                var window = new Window { Content = view };

                window.Show();

                Assert.NotEmpty(form.Title);
                Assert.NotEmpty(form.Description);
                Assert.NotEmpty(form.Fields);

                window.Close();
            }
        }

        [AvaloniaFact]
        public void ASectionRendersItsFormsAndSelectsTheFirst()
        {
            var section = new OperationsSectionViewModel(LauncherSection.Snapshots, AllForms().Take(2).ToList());
            var window = new Window { Content = new OperationsSectionView { DataContext = section } };

            window.Show();

            Assert.Equal(2, section.Forms.Count);
            Assert.Same(section.Forms[0], section.SelectedForm);
        }

        [AvaloniaFact]
        public void AFormCannotRunUntilTheEngineIsReadyAndEveryFieldIsComplete()
        {
            var engineStatus = new EngineStatusViewModel();
            var form = new SnapshotFormViewModel(CreateJob(), engineStatus, new NullFileDialogService());

            var window = new Window { Content = new OperationFormView { DataContext = form } };
            window.Show();

            Assert.False(form.RunCommand.CanExecute(null));

            foreach (PathFieldViewModel field in form.Fields.Cast<PathFieldViewModel>())
            {
                field.Value = Path.Combine(Path.GetTempPath(), field.Label + ".json");
            }

            Assert.False(form.RunCommand.CanExecute(null));

            engineStatus.Update(ReadyEngine());
            Assert.True(form.RunCommand.CanExecute(null));
        }

        [AvaloniaFact]
        public void TheOrderedListKeepsTheDeclaredOrderAndReportsRepeats()
        {
            var field = new OrderedFilesFieldViewModel(
                "Snapshots", "ordered", new StubFileDialogService(new[] { "/z.json", "/a.json", "/z.json" }),
                FilePickerFileKind.RunSnapshotJson);

            field.AddCommand.Execute(null);

            Assert.Equal(new[] { "/z.json", "/a.json", "/z.json" }, field.Paths);
            Assert.Single(field.Warnings);

            field.MoveDownCommand.Execute(field.Entries[0]);
            Assert.Equal(new[] { "/a.json", "/z.json", "/z.json" }, field.Paths);

            field.RemoveCommand.Execute(field.Entries[2]);
            Assert.Equal(new[] { "/a.json", "/z.json" }, field.Paths);
        }

        [AvaloniaFact]
        public void OnlyOneJobRunsAtATimePerWindow()
        {
            var tracker = new ActiveJobTracker();

            Assert.True(tracker.TryAcquire());
            Assert.False(tracker.TryAcquire());

            tracker.Release();
            Assert.True(tracker.TryAcquire());
        }

        private static IReadOnlyList<OperationFormViewModel> AllForms()
        {
            var engineStatus = new EngineStatusViewModel();
            var dialogs = new NullFileDialogService();

            var render = new RenderProjectFormViewModel(CreateJob(), engineStatus, dialogs);

            return new OperationFormViewModel[]
            {
                new SnapshotFormViewModel(CreateJob(), engineStatus, dialogs),
                new CompareSnapshotsFormViewModel(CreateJob(), engineStatus, dialogs),
                new IndexSnapshotsFormViewModel(CreateJob(), engineStatus, dialogs),
                new CompareIndexFormViewModel(CreateJob(), engineStatus, dialogs),
                new CompareFormViewModel(CreateJob(), engineStatus, dialogs),
                new CreateProjectFormViewModel(CreateJob(), engineStatus, dialogs),
                new AppendProjectSnapshotFormViewModel(CreateJob(), engineStatus, dialogs, render.UseProject),
                render,
            };
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
                new UnusedEngineGateway(),
                new PermissiveFileProbe(),
                new InMemoryRecentItemsStore(),
                new NoOpJobJournal(),
                new CollectingLog(),
                new PassThroughPathRedactor(),
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

        private sealed class StubFileDialogService : IFileDialogService
        {
            private readonly IReadOnlyList<string> _files;

            public StubFileDialogService(IReadOnlyList<string> files) => _files = files;

            public Task<string?> PickOpenFileAsync(string title, FilePickerFileKind kind, string? startDirectory) =>
                Task.FromResult<string?>(_files.Count == 0 ? null : _files[0]);

            public Task<IReadOnlyList<string>> PickOpenFilesAsync(
                string title, FilePickerFileKind kind, string? startDirectory) => Task.FromResult(_files);

            public Task<string?> PickSaveFileAsync(
                string title, FilePickerFileKind kind, string suggestedFileName, string? startDirectory) =>
                Task.FromResult<string?>(null);
        }

        private sealed class UnusedEngineGateway : IEngineGateway
        {
            public Task<EngineInfo> DescribeAsync(CancellationToken cancellationToken) =>
                Task.FromResult(ReadyEngine());

            public Task<EngineJobResult> ExecuteAsync(
                EngineJobRequest request,
                IProgress<EngineJobProgress>? progress,
                CancellationToken cancellationToken) =>
                throw new NotSupportedException("The form smoke tests never execute the engine.");
        }

        private sealed class PermissiveFileProbe : IFileProbe
        {
            public bool FileExists(string path) => false;

            public bool DirectoryExists(string path) => true;

            public long GetFileSizeInBytes(string path) => -1;
        }

        private sealed class NoOpJobJournal : IJobJournal
        {
            public Task BeginAsync(JobJournalEntry entry, CancellationToken cancellationToken) => Task.CompletedTask;

            public Task CompleteAsync(string jobId, CancellationToken cancellationToken) => Task.CompletedTask;

            public Task<IReadOnlyList<JobJournalEntry>> ReadInterruptedAsync(CancellationToken cancellationToken) =>
                Task.FromResult<IReadOnlyList<JobJournalEntry>>(Array.Empty<JobJournalEntry>());

            public Task ClearAsync(CancellationToken cancellationToken) => Task.CompletedTask;
        }

        private sealed class PassThroughPathRedactor : IPathRedactor
        {
            public RedactedPath Redact(string path) =>
                new RedactedPath(Path.GetFileName(path), Path.GetExtension(path), new string('0', 64), PathRootKind.Unknown);
        }
    }
}
