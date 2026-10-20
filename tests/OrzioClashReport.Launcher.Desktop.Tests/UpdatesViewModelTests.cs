using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Avalonia.Headless.XUnit;
using OrzioClashReport.Launcher.Desktop.ViewModels;
using OrzioClashReport.Launcher.Infrastructure.Updates;

namespace OrzioClashReport.Launcher.Desktop.Tests
{
    /// <summary>
    /// The update flow is user-initiated end to end: nothing is checked, downloaded or started unless
    /// the user asks, and a running job always takes precedence over an installation.
    /// </summary>
    public sealed class UpdatesViewModelTests
    {
        private const string Current = "0.2.1-launcher-preview.1";

        private static readonly DesktopUpdateManifest Newer = new DesktopUpdateManifest(
            "0.2.1-launcher-preview.2",
            "https://github.com/engpaulocosta-bim/orzio-clash-report/releases/download/desktop-v0.2.1-launcher-preview.2/orzio-clash-report-desktop-v0.2.1-launcher-preview.2-win-x64-setup.exe",
            new string('a', 64),
            "https://github.com/engpaulocosta-bim/orzio-clash-report/releases/tag/desktop-v0.2.1-launcher-preview.2");

        [AvaloniaFact]
        public void NothingHappensUntilTheUserAsks()
        {
            var client = new FakeDesktopUpdateClient { Available = Newer };
            UpdatesViewModel updates = Create(client, out _, out _);

            Assert.Equal(UpdateState.Idle, updates.State);
            Assert.False(updates.InstallCommand.CanExecute(null));
            Assert.Equal(0, client.Downloads);
        }

        [AvaloniaFact]
        public async Task ACheckWithNoNewerReleaseReportsUpToDate()
        {
            UpdatesViewModel updates = Create(new FakeDesktopUpdateClient(), out _, out _);

            await updates.CheckCommand.ExecuteAsync(null);

            Assert.Equal(UpdateState.UpToDate, updates.State);
            Assert.Contains(Current, updates.StatusText);
            Assert.False(updates.IsUpdateAvailable);
            Assert.NotNull(updates.LastCheckedText);
        }

        [AvaloniaFact]
        public async Task ANewerReleaseIsOfferedButNotDownloaded()
        {
            var client = new FakeDesktopUpdateClient { Available = Newer };
            UpdatesViewModel updates = Create(client, out _, out bool[] shutdown);

            await updates.CheckCommand.ExecuteAsync(null);

            Assert.Equal(UpdateState.Available, updates.State);
            Assert.Equal("0.2.1-launcher-preview.2", updates.AvailableVersion);
            Assert.True(updates.HasReleaseNotes);
            Assert.True(updates.InstallCommand.CanExecute(null));
            Assert.Equal(0, client.Downloads);
            Assert.False(shutdown[0]);
        }

        [AvaloniaFact]
        public async Task InstallingStartsTheVerifiedInstallerAndClosesTheApplication()
        {
            var client = new FakeDesktopUpdateClient { Available = Newer };
            UpdatesViewModel updates = Create(client, out _, out bool[] shutdown);

            await updates.CheckCommand.ExecuteAsync(null);
            await updates.InstallCommand.ExecuteAsync(null);

            Assert.Equal(1, client.Downloads);
            Assert.Equal(UpdateState.Installing, updates.State);
            Assert.Equal(100, updates.DownloadPercent);
            Assert.True(shutdown[0]);
        }

        [AvaloniaFact]
        public async Task ARunningJobBlocksInstallation()
        {
            var client = new FakeDesktopUpdateClient { Available = Newer };
            UpdatesViewModel updates = Create(client, out ActiveJobTracker tracker, out _);

            await updates.CheckCommand.ExecuteAsync(null);
            Assert.True(tracker.TryAcquire());

            Assert.False(updates.InstallCommand.CanExecute(null));

            tracker.Release();
            Assert.True(updates.InstallCommand.CanExecute(null));
        }

        [AvaloniaFact]
        public async Task AHashMismatchIsReportedAndNothingIsStarted()
        {
            var client = new FakeDesktopUpdateClient
            {
                Available = Newer,
                DownloadFailure = new InvalidDataException("Downloaded installer SHA-256 does not match the release."),
            };
            UpdatesViewModel updates = Create(client, out _, out bool[] shutdown);

            await updates.CheckCommand.ExecuteAsync(null);
            await updates.InstallCommand.ExecuteAsync(null);

            Assert.Equal(UpdateState.Failed, updates.State);
            Assert.Contains("SHA-256", updates.StatusText);
            Assert.False(shutdown[0]);
        }

        [AvaloniaFact]
        public async Task AnUnreachableGitHubIsReportedInPlainLanguage()
        {
            var client = new FakeDesktopUpdateClient { CheckFailure = new HttpRequestException("offline") };
            UpdatesViewModel updates = Create(client, out _, out _);

            await updates.CheckCommand.ExecuteAsync(null);

            Assert.Equal(UpdateState.Failed, updates.State);
            Assert.Contains("ligação", updates.StatusText);
        }

        [AvaloniaFact]
        public async Task ARateLimitIsDistinguishedFromAnOutage()
        {
            var client = new FakeDesktopUpdateClient
            {
                CheckFailure = new HttpRequestException("forbidden", null, HttpStatusCode.Forbidden),
            };
            UpdatesViewModel updates = Create(client, out _, out _);

            await updates.CheckCommand.ExecuteAsync(null);

            Assert.Contains("limite", updates.StatusText);
        }

        [AvaloniaFact]
        public async Task ReleaseNotesOpenOnlyAsHttps()
        {
            Uri? opened = null;
            var client = new FakeDesktopUpdateClient { Available = Newer };
            var updates = new UpdatesViewModel(
                client, new ActiveJobTracker(), new FixedClock(), Current, uri => opened = uri, () => { });

            await updates.CheckCommand.ExecuteAsync(null);
            updates.OpenReleaseNotesCommand.Execute(null);

            Assert.NotNull(opened);
            Assert.Equal(Uri.UriSchemeHttps, opened!.Scheme);
        }

        private static UpdatesViewModel Create(
            FakeDesktopUpdateClient client, out ActiveJobTracker tracker, out bool[] shutdown)
        {
            tracker = new ActiveJobTracker();
            bool[] requested = { false };
            shutdown = requested;
            return new UpdatesViewModel(client, tracker, new FixedClock(), Current, _ => { }, () => requested[0] = true);
        }
    }
}
