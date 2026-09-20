using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using OrzioClashReport.Launcher.Application.Operations;
using OrzioClashReport.Launcher.Contracts.Engine;
using OrzioClashReport.Launcher.Contracts.Jobs;
using OrzioClashReport.Launcher.Contracts.Logging;
using OrzioClashReport.Launcher.Contracts.Operations;
using OrzioClashReport.Launcher.Contracts.Platform;
using OrzioClashReport.Launcher.Desktop.ViewModels;

namespace OrzioClashReport.Launcher.Desktop.Tests
{
    public sealed class JobViewModelTests
    {
        [Fact]
        public async Task UserCancellationDuringEngineVerificationIsPresentedAsCanceled()
        {
            var gateway = new CancelableGateway();
            var tracker = new ActiveJobTracker();
            JobViewModel viewModel = CreateViewModel(gateway, tracker);

            Task<EngineJobResult?> run = viewModel.RunAsync(CreateRequest());
            await gateway.Entered;

            viewModel.CancelCommand.Execute(null);
            EngineJobResult? result = await run;

            Assert.Null(result);
            Assert.True(viewModel.HasResult);
            Assert.True(viewModel.IsCanceled);
            Assert.False(viewModel.IsFailed);
            Assert.False(viewModel.IsSucceeded);
            Assert.False(viewModel.IsRunning);
            Assert.Equal("Cancelado", viewModel.StateLabel);
            Assert.True(tracker.TryAcquire());
        }

        [Fact]
        public async Task UnexpectedOperationCanceledExceptionStillPropagates()
        {
            var tracker = new ActiveJobTracker();
            JobViewModel viewModel = CreateViewModel(new UnexpectedCancellationGateway(), tracker);

            await Assert.ThrowsAsync<OperationCanceledException>(() => viewModel.RunAsync(CreateRequest()));

            Assert.False(viewModel.IsCanceled);
            Assert.False(viewModel.IsRunning);
            Assert.True(tracker.TryAcquire());
        }

        private static JobViewModel CreateViewModel(IEngineGateway gateway, ActiveJobTracker tracker)
        {
            var executor = new LauncherOperationExecutor(
                gateway,
                new ExistingDirectoryFileProbe(),
                new InMemoryRecentItemsStore(),
                new NoOpJobJournal(),
                new CollectingLog(),
                new ConstantPathRedactor(),
                new FixedClock(),
                Path.Combine(Path.GetTempPath(), "orzio-launcher-installation"));

            return new JobViewModel(executor, new NullOutputRevealer(), tracker);
        }

        private static LauncherOperationRequest CreateRequest()
        {
            string workingDirectory = Path.Combine(Path.GetTempPath(), "orzio-cancel-tests");
            string outputPath = Path.Combine(workingDirectory, "report.html");

            return new LauncherOperationRequest(
                LauncherOperationKind.QuickReport,
                new[] { "input.xml", "-o", outputPath },
                workingDirectory,
                outputPath,
                OutputCollisionDecision.None,
                "report.html");
        }

        private sealed class CancelableGateway : IEngineGateway
        {
            private readonly TaskCompletionSource<bool> _entered =
                new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);

            public Task Entered => _entered.Task;

            public Task<EngineInfo> DescribeAsync(CancellationToken cancellationToken) =>
                throw new NotSupportedException();

            public async Task<EngineJobResult> ExecuteAsync(
                EngineJobRequest request,
                IProgress<EngineJobProgress>? progress,
                CancellationToken cancellationToken)
            {
                _entered.TrySetResult(true);
                await Task.Delay(Timeout.InfiniteTimeSpan, cancellationToken);
                throw new InvalidOperationException("The cancellation wait unexpectedly completed.");
            }
        }

        private sealed class UnexpectedCancellationGateway : IEngineGateway
        {
            public Task<EngineInfo> DescribeAsync(CancellationToken cancellationToken) =>
                throw new NotSupportedException();

            public Task<EngineJobResult> ExecuteAsync(
                EngineJobRequest request,
                IProgress<EngineJobProgress>? progress,
                CancellationToken cancellationToken) =>
                Task.FromException<EngineJobResult>(new OperationCanceledException("Unexpected cancellation."));
        }

        private sealed class ExistingDirectoryFileProbe : IFileProbe
        {
            public bool FileExists(string path) => false;

            public bool DirectoryExists(string path) => true;

            public long GetFileSizeInBytes(string path) => -1;
        }

        private sealed class ConstantPathRedactor : IPathRedactor
        {
            public RedactedPath Redact(string path) =>
                new RedactedPath("report.html", ".html", new string('0', 64), PathRootKind.OtherLocalVolume);
        }
    }
}
