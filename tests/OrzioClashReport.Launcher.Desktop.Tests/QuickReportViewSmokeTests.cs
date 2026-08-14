using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using OrzioClashReport.Launcher.Application.Engine;
using OrzioClashReport.Launcher.Application.Operations;
using OrzioClashReport.Launcher.Contracts.Engine;
using OrzioClashReport.Launcher.Contracts.Jobs;
using OrzioClashReport.Launcher.Contracts.Logging;
using OrzioClashReport.Launcher.Contracts.Platform;
using OrzioClashReport.Launcher.Desktop.Platform;
using OrzioClashReport.Launcher.Desktop.ViewModels;
using OrzioClashReport.Launcher.Desktop.Views;

namespace OrzioClashReport.Launcher.Desktop.Tests
{
    /// <summary>
    /// Renders the quick-report screen headlessly. It is the product's main path, so a broken template
    /// or an unresolvable token there has to fail here rather than in front of an evaluator.
    /// </summary>
    public sealed class QuickReportViewSmokeTests
    {
        [AvaloniaFact]
        public void TheQuickReportScreenRenders()
        {
            var view = new QuickReportView { DataContext = CreateViewModel() };
            var window = new Window { Content = view };

            window.Show();

            Assert.NotNull(view.DataContext);
        }

        [AvaloniaFact]
        public void GeneratingIsBlockedUntilTheEngineIsReadyAndBothPathsAreChosen()
        {
            QuickReportViewModel viewModel = CreateViewModel();

            var window = new Window { Content = new QuickReportView { DataContext = viewModel } };
            window.Show();

            Assert.False(viewModel.GenerateCommand.CanExecute(null));

            viewModel.InputXmlPath = "/inputs/model.xml";
            viewModel.OutputHtmlPath = "/reports/report.html";
            Assert.False(viewModel.GenerateCommand.CanExecute(null));

            viewModel.EngineStatus.Update(ReadyEngine());
            Assert.True(viewModel.GenerateCommand.CanExecute(null));
        }

        [AvaloniaFact]
        public void TheJobPanelRendersARunningJobAndAResult()
        {
            var view = new JobPanelView { DataContext = CreateJob() };
            var window = new Window { Content = view };

            window.Show();

            Assert.NotNull(view.DataContext);
        }

        private static EngineInfo ReadyEngine() =>
            new EngineInfo(
                EngineStatusKind.Ready,
                "0.1.0-preview.3",
                "0.1.0-preview.3",
                new EngineLocation("/install/engine/orzioclash", "/install/engine/engine-manifest.json"),
                new EngineIntegrityResult(EngineIntegrityVerdict.Verified, "abc", "abc"),
                "Motor verificado.");

        private static QuickReportViewModel CreateViewModel() =>
            new QuickReportViewModel(
                CreateJob(),
                new NullFileDialogService(),
                new InMemorySettingsStore(),
                new EngineStatusViewModel());

        private static JobViewModel CreateJob()
        {
            var executor = new LauncherOperationExecutor(
                new NullEngineGateway(),
                new NullFileProbe(),
                new InMemoryRecentItemsStore(),
                new NullJobJournal(),
                new CollectingLog(),
                new NullPathRedactor(),
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

        private sealed class NullEngineGateway : IEngineGateway
        {
            public Task<EngineInfo> DescribeAsync(CancellationToken cancellationToken) =>
                Task.FromResult(ReadyEngine());

            public Task<EngineJobResult> ExecuteAsync(
                EngineJobRequest request,
                IProgress<EngineJobProgress>? progress,
                CancellationToken cancellationToken) =>
                throw new NotSupportedException("The view smoke tests never execute the engine.");
        }

        private sealed class NullFileProbe : IFileProbe
        {
            public bool FileExists(string path) => false;

            public bool DirectoryExists(string path) => true;

            public long GetFileSizeInBytes(string path) => -1;
        }

        private sealed class NullJobJournal : IJobJournal
        {
            public Task BeginAsync(JobJournalEntry entry, CancellationToken cancellationToken) => Task.CompletedTask;

            public Task CompleteAsync(string jobId, CancellationToken cancellationToken) => Task.CompletedTask;

            public Task<IReadOnlyList<JobJournalEntry>> ReadInterruptedAsync(CancellationToken cancellationToken) =>
                Task.FromResult<IReadOnlyList<JobJournalEntry>>(Array.Empty<JobJournalEntry>());

            public Task ClearAsync(CancellationToken cancellationToken) => Task.CompletedTask;
        }

        private sealed class NullPathRedactor : IPathRedactor
        {
            public RedactedPath Redact(string path) =>
                new RedactedPath(Path.GetFileName(path), Path.GetExtension(path), new string('0', 64), PathRootKind.Unknown);
        }
    }
}
