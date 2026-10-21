using System;
using System.Globalization;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using OrzioClashReport.Launcher.Application.Localization;
using OrzioClashReport.Launcher.Contracts.Platform;
using OrzioClashReport.Launcher.Desktop.Localization;
using OrzioClashReport.Launcher.Desktop.Platform;
using OrzioClashReport.Launcher.Infrastructure.Updates;

namespace OrzioClashReport.Launcher.Desktop.ViewModels
{
    public enum UpdateState
    {
        Idle,
        Checking,
        UpToDate,
        Available,
        Downloading,
        Installing,
        Failed,
    }

    /// <summary>
    /// The user-initiated update flow. Nothing here runs on its own: the network is contacted only when
    /// the user asks to check, and an installer is downloaded only when the user asks to install. A
    /// download is verified against the release's SHA-256 before anything is started.
    /// </summary>
    /// <remarks>
    /// Every sentence here is held as a key and its arguments, and looked up when read, so the card follows
    /// the interface language. The text of an unexpected exception is an argument, shown as it was written.
    /// </remarks>
    public sealed partial class UpdatesViewModel : LocalizedObservableObject
    {
        private readonly IDesktopUpdateClient _client;
        private readonly ActiveJobTracker _activeJobTracker;
        private readonly IClock _clock;
        private readonly Action<Uri> _openUri;
        private readonly Action _requestShutdown;
        private DesktopUpdateManifest? _available;
        private string _statusKey = "updates.status.idle";
        private object?[] _statusArguments = Array.Empty<object?>();
        private DateTimeOffset? _lastCheckedLocal;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(IsBusy))]
        [NotifyPropertyChangedFor(nameof(IsUpdateAvailable))]
        [NotifyPropertyChangedFor(nameof(IsDownloading))]
        [NotifyPropertyChangedFor(nameof(HasFailed))]
        [NotifyPropertyChangedFor(nameof(Glyph))]
        [NotifyPropertyChangedFor(nameof(IsChecking))]
        [NotifyPropertyChangedFor(nameof(IsUpToDate))]
        [NotifyPropertyChangedFor(nameof(SidebarStatus))]
        [NotifyCanExecuteChangedFor(nameof(CheckCommand))]
        [NotifyCanExecuteChangedFor(nameof(InstallCommand))]
        private UpdateState _state = UpdateState.Idle;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(InstallButtonText))]
        [NotifyPropertyChangedFor(nameof(AvailableHeadline))]
        [NotifyPropertyChangedFor(nameof(AvailableTooltip))]
        private string? _availableVersion;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(DownloadProgressText))]
        private double _downloadPercent;

        public UpdatesViewModel(
            IDesktopUpdateClient client,
            ActiveJobTracker activeJobTracker,
            IClock clock,
            string currentVersion,
            Action<Uri> openUri,
            Action requestShutdown)
        {
            _client = client ?? throw new ArgumentNullException(nameof(client));
            _activeJobTracker = activeJobTracker ?? throw new ArgumentNullException(nameof(activeJobTracker));
            _clock = clock ?? throw new ArgumentNullException(nameof(clock));
            CurrentVersion = currentVersion ?? throw new ArgumentNullException(nameof(currentVersion));
            _openUri = openUri ?? throw new ArgumentNullException(nameof(openUri));
            _requestShutdown = requestShutdown ?? throw new ArgumentNullException(nameof(requestShutdown));

            _activeJobTracker.PropertyChanged += (_, e) =>
            {
                if (e.PropertyName == nameof(ActiveJobTracker.IsBusy))
                {
                    InstallCommand.NotifyCanExecuteChanged();
                }
            };
        }

        public string CurrentVersion { get; }

        /// <summary>The sentence describing where the update flow is, in the active language.</summary>
        public string StatusText => Loc.T(_statusKey, _statusArguments);

        /// <summary>When the last explicit check finished, or null when there has been none.</summary>
        public string? LastCheckedText => _lastCheckedLocal == null
            ? null
            : Loc.T("updates.status.lastChecked", FormatTime(_lastCheckedLocal.Value));

        /// <summary>The text of the install button, which names the version it would install.</summary>
        public string InstallButtonText => Loc.T("updates.install", AvailableVersion);

        /// <summary>The headline shown on the home screen when a newer version was found.</summary>
        public string AvailableHeadline => Loc.T("home.update.title", AvailableVersion);

        /// <summary>The sidebar tooltip when a newer version was found.</summary>
        public string AvailableTooltip => Loc.T("shell.sidebar.updateTooltip", AvailableVersion);

        public string DownloadProgressText =>
            Loc.T("updates.downloaded", DownloadPercent.ToString("0", CultureInfo.InvariantCulture));

        private void SetStatus(string key, params object?[] arguments)
        {
            _statusKey = key;
            _statusArguments = arguments;
            OnPropertyChanged(nameof(StatusText));
        }

        private static string FormatTime(DateTimeOffset local) =>
            local.ToString(
                Loc.Language == UiLanguage.English ? "yyyy-MM-dd HH:mm" : "dd/MM/yyyy HH:mm",
                CultureInfo.InvariantCulture);

        public bool IsBusy => State is UpdateState.Checking or UpdateState.Downloading or UpdateState.Installing;

        public bool IsChecking => State == UpdateState.Checking;

        public bool IsUpToDate => State == UpdateState.UpToDate;

        public bool IsUpdateAvailable => State == UpdateState.Available;

        public bool IsDownloading => State == UpdateState.Downloading;

        public bool HasFailed => State == UpdateState.Failed;

        public bool HasReleaseNotes => _available?.ReleaseNotesUrl != null;

        /// <summary>A short line for the sidebar footer; the full explanation lives in Definições.</summary>
        public string SidebarStatus => State switch
        {
            UpdateState.Checking => Loc.T("updates.sidebar.checking"),
            UpdateState.UpToDate => Loc.T("updates.sidebar.upToDate", CurrentVersion),
            UpdateState.Downloading => Loc.T("updates.sidebar.downloading"),
            UpdateState.Installing => Loc.T("updates.sidebar.installing"),
            UpdateState.Failed => Loc.T("updates.sidebar.failed"),
            _ => Loc.T("updates.sidebar.version", CurrentVersion),
        };

        /// <summary>A glyph for the state, so it is never conveyed by colour alone.</summary>
        public string Glyph => State switch
        {
            UpdateState.Checking => "⟳",
            UpdateState.UpToDate => "✓",
            UpdateState.Available => "↑",
            UpdateState.Downloading => "↓",
            UpdateState.Installing => "↻",
            UpdateState.Failed => "⚠",
            _ => "•",
        };

        private bool CanCheck() => !IsBusy;

        [RelayCommand(CanExecute = nameof(CanCheck))]
        private async Task CheckAsync()
        {
            State = UpdateState.Checking;
            SetStatus("updates.status.checking");
            _available = null;
            AvailableVersion = null;
            OnPropertyChanged(nameof(HasReleaseNotes));

            try
            {
                DesktopUpdateCheckResult result =
                    await _client.CheckAsync(CurrentVersion, CancellationToken.None).ConfigureAwait(true);

                _lastCheckedLocal = _clock.UtcNow.ToLocalTime();
                OnPropertyChanged(nameof(LastCheckedText));

                if (!result.UpdateAvailable || result.Update == null)
                {
                    State = UpdateState.UpToDate;
                    SetStatus("updates.status.upToDate", CurrentVersion);
                    return;
                }

                _available = result.Update;
                AvailableVersion = result.Update.Version;
                OnPropertyChanged(nameof(HasReleaseNotes));
                State = UpdateState.Available;
                SetStatus("updates.status.available", result.Update.Version);
            }
            catch (Exception exception) when (IsExpectedFailure(exception))
            {
                Fail(DescribeKey(exception, downloading: false), DescribeArguments(exception));
            }
        }

        private bool CanInstall() => State == UpdateState.Available && _available != null && !_activeJobTracker.IsBusy;

        [RelayCommand(CanExecute = nameof(CanInstall))]
        private async Task InstallAsync()
        {
            DesktopUpdateManifest? update = _available;
            if (update == null)
            {
                return;
            }

            State = UpdateState.Downloading;
            DownloadPercent = 0;
            SetStatus("updates.status.downloading", update.Version);

            var progress = new Progress<double>(fraction => DownloadPercent = Math.Round(fraction * 100, 0));

            try
            {
                await _client.DownloadAndStartInstallerAsync(update, progress, CancellationToken.None)
                    .ConfigureAwait(true);

                DownloadPercent = 100;
                State = UpdateState.Installing;
                SetStatus("updates.status.installing");
                _requestShutdown();
            }
            catch (Exception exception) when (IsExpectedFailure(exception))
            {
                Fail(DescribeKey(exception, downloading: true), DescribeArguments(exception));
            }
        }

        [RelayCommand]
        private void OpenReleaseNotes()
        {
            if (_available?.ReleaseNotesUrl is string url && Uri.TryCreate(url, UriKind.Absolute, out Uri? uri)
                && uri.Scheme == Uri.UriSchemeHttps)
            {
                _openUri(uri);
            }
        }

        private void Fail(string key, params object?[] arguments)
        {
            State = UpdateState.Failed;
            SetStatus(key, arguments);
        }

        private static bool IsExpectedFailure(Exception exception) =>
            exception is HttpRequestException or IOException or JsonException or UnauthorizedAccessException
                or InvalidDataException or InvalidOperationException or ArgumentException
                or PlatformNotSupportedException or TaskCanceledException;

        private static string DescribeKey(Exception exception, bool downloading)
        {
            switch (exception)
            {
                case HttpRequestException http when http.StatusCode == HttpStatusCode.Forbidden:
                    return "updates.error.rateLimited";

                case HttpRequestException:
                case TaskCanceledException:
                    return "updates.error.unreachable";

                case InvalidDataException:
                    return downloading ? "updates.error.installerMismatch" : "updates.error.releaseRejected";

                case PlatformNotSupportedException:
                    return "updates.error.unsupportedPlatform";

                default:
                    return "updates.error.generic";
            }
        }

        // Only the unexpected case carries the exception's own text, which is shown as it was written.
        private static object?[] DescribeArguments(Exception exception) =>
            DescribeKey(exception, downloading: false) == "updates.error.generic"
                ? new object?[] { exception.Message }
                : Array.Empty<object?>();
    }
}
