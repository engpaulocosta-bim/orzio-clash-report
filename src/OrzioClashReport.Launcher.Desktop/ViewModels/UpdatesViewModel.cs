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
using OrzioClashReport.Launcher.Contracts.Platform;
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
    public sealed partial class UpdatesViewModel : ObservableObject
    {
        private readonly IDesktopUpdateClient _client;
        private readonly ActiveJobTracker _activeJobTracker;
        private readonly IClock _clock;
        private readonly Action<Uri> _openUri;
        private readonly Action _requestShutdown;
        private DesktopUpdateManifest? _available;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(IsBusy))]
        [NotifyPropertyChangedFor(nameof(IsUpdateAvailable))]
        [NotifyPropertyChangedFor(nameof(IsDownloading))]
        [NotifyPropertyChangedFor(nameof(HasFailed))]
        [NotifyPropertyChangedFor(nameof(Glyph))]
        [NotifyCanExecuteChangedFor(nameof(CheckCommand))]
        [NotifyCanExecuteChangedFor(nameof(InstallCommand))]
        private UpdateState _state = UpdateState.Idle;

        [ObservableProperty]
        private string _statusText = "Ainda não procurou atualizações nesta sessão.";

        [ObservableProperty]
        private string? _availableVersion;

        [ObservableProperty]
        private double _downloadPercent;

        [ObservableProperty]
        private string? _lastCheckedText;

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

        public bool IsBusy => State is UpdateState.Checking or UpdateState.Downloading or UpdateState.Installing;

        public bool IsUpdateAvailable => State == UpdateState.Available;

        public bool IsDownloading => State == UpdateState.Downloading;

        public bool HasFailed => State == UpdateState.Failed;

        public bool HasReleaseNotes => _available?.ReleaseNotesUrl != null;

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
            StatusText = "A procurar atualizações oficiais no GitHub…";
            _available = null;
            AvailableVersion = null;
            OnPropertyChanged(nameof(HasReleaseNotes));

            try
            {
                DesktopUpdateCheckResult result =
                    await _client.CheckAsync(CurrentVersion, CancellationToken.None).ConfigureAwait(true);

                LastCheckedText = "Última verificação: "
                    + _clock.UtcNow.ToLocalTime().ToString("dd/MM/yyyy HH:mm", CultureInfo.InvariantCulture);

                if (!result.UpdateAvailable || result.Update == null)
                {
                    State = UpdateState.UpToDate;
                    StatusText = $"Tem a versão mais recente ({CurrentVersion}).";
                    return;
                }

                _available = result.Update;
                AvailableVersion = result.Update.Version;
                OnPropertyChanged(nameof(HasReleaseNotes));
                State = UpdateState.Available;
                StatusText = $"Nova versão disponível: {result.Update.Version}. A instalação só começa quando a pedir.";
            }
            catch (Exception exception) when (IsExpectedFailure(exception))
            {
                Fail(Describe(exception, downloading: false));
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
            StatusText = $"A descarregar a versão {update.Version}…";

            var progress = new Progress<double>(fraction => DownloadPercent = Math.Round(fraction * 100, 0));

            try
            {
                await _client.DownloadAndStartInstallerAsync(update, progress, CancellationToken.None)
                    .ConfigureAwait(true);

                DownloadPercent = 100;
                State = UpdateState.Installing;
                StatusText = "Instalador verificado (SHA-256) e iniciado. A aplicação vai fechar para a atualização poder substituir os ficheiros.";
                _requestShutdown();
            }
            catch (Exception exception) when (IsExpectedFailure(exception))
            {
                Fail(Describe(exception, downloading: true));
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

        private void Fail(string message)
        {
            State = UpdateState.Failed;
            StatusText = message;
        }

        private static bool IsExpectedFailure(Exception exception) =>
            exception is HttpRequestException or IOException or JsonException or UnauthorizedAccessException
                or InvalidDataException or InvalidOperationException or ArgumentException
                or PlatformNotSupportedException or TaskCanceledException;

        private static string Describe(Exception exception, bool downloading)
        {
            switch (exception)
            {
                case HttpRequestException http when http.StatusCode == HttpStatusCode.Forbidden:
                    return "O GitHub recusou temporariamente o pedido (limite de pedidos). Tente novamente dentro de alguns minutos.";

                case HttpRequestException:
                case TaskCanceledException:
                    return "Não foi possível contactar o GitHub. Verifique a ligação à internet e tente novamente.";

                case InvalidDataException:
                    return downloading
                        ? "O instalador descarregado não corresponde ao SHA-256 publicado e não foi executado."
                        : "A release publicada não passou a validação de segurança e foi ignorada.";

                case PlatformNotSupportedException:
                    return "A instalação automática só está disponível no Windows.";

                default:
                    return "Não foi possível concluir a atualização: " + exception.Message;
            }
        }
    }
}
