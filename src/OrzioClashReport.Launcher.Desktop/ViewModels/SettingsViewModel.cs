using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using OrzioClashReport.Launcher.Contracts.Engine;
using OrzioClashReport.Launcher.Contracts.Platform;
using OrzioClashReport.Launcher.Contracts.Settings;

namespace OrzioClashReport.Launcher.Desktop.ViewModels
{
    /// <summary>
    /// Definições: appearance, where local data lives, what the engine is, and what the application
    /// deliberately does not do.
    /// </summary>
    public sealed partial class SettingsViewModel : ObservableObject
    {
        private readonly ISettingsStore _settingsStore;
        private readonly IRecentItemsStore _recentItemsStore;
        private readonly IOutputRevealer _outputRevealer;
        private readonly Action<LauncherThemePreference> _applyTheme;
        private readonly string _dataDirectory;

        private bool _isLoading;

        [ObservableProperty]
        private LauncherThemePreference _theme = LauncherThemePreference.System;

        [ObservableProperty]
        private bool _showExperimentalWarnings = true;

        [ObservableProperty]
        private string _engineVersion = string.Empty;

        [ObservableProperty]
        private string _engineIntegrity = string.Empty;

        [ObservableProperty]
        private string _statusMessage = string.Empty;

        public SettingsViewModel(
            ISettingsStore settingsStore,
            IRecentItemsStore recentItemsStore,
            IOutputRevealer outputRevealer,
            EngineStatusViewModel engineStatus,
            string dataDirectory,
            string launcherVersion,
            Action<LauncherThemePreference> applyTheme)
        {
            _settingsStore = settingsStore ?? throw new ArgumentNullException(nameof(settingsStore));
            _recentItemsStore = recentItemsStore ?? throw new ArgumentNullException(nameof(recentItemsStore));
            _outputRevealer = outputRevealer ?? throw new ArgumentNullException(nameof(outputRevealer));
            EngineStatus = engineStatus ?? throw new ArgumentNullException(nameof(engineStatus));
            _dataDirectory = dataDirectory ?? throw new ArgumentNullException(nameof(dataDirectory));
            LauncherVersion = launcherVersion ?? throw new ArgumentNullException(nameof(launcherVersion));
            _applyTheme = applyTheme ?? throw new ArgumentNullException(nameof(applyTheme));
        }

        public EngineStatusViewModel EngineStatus { get; }

        public string LauncherVersion { get; }

        /// <summary>Shown so a user can find their own data; it is inside their profile, never in the installation.</summary>
        public string DataDirectory => _dataDirectory;

        public IReadOnlyList<LauncherThemePreference> Themes { get; } = new[]
        {
            LauncherThemePreference.System,
            LauncherThemePreference.Light,
            LauncherThemePreference.Dark,
        };

        public async Task LoadAsync(CancellationToken cancellationToken)
        {
            LauncherSettings settings = await _settingsStore.LoadAsync(cancellationToken).ConfigureAwait(true);

            _isLoading = true;
            try
            {
                Theme = settings.Theme;
                ShowExperimentalWarnings = settings.ShowExperimentalWarnings;
            }
            finally
            {
                _isLoading = false;
            }

            // Applied explicitly rather than through the change handler: loading must restore the
            // stored theme without writing the settings file back on every start.
            _applyTheme(settings.Theme);
        }

        public void UpdateEngine(EngineInfo info)
        {
            EngineVersion = info.ReportedVersion ?? "—";

            switch (info.Integrity.Verdict)
            {
                case EngineIntegrityVerdict.Verified:
                    EngineIntegrity = "SHA-256 verificado contra o manifesto instalado.";
                    break;
                case EngineIntegrityVerdict.Mismatch:
                    EngineIntegrity = "SHA-256 diferente do manifesto instalado.";
                    break;
                case EngineIntegrityVerdict.ManifestUnavailable:
                    EngineIntegrity = "Manifesto do motor indisponível: integridade não verificada.";
                    break;
                default:
                    EngineIntegrity = "Integridade ainda não verificada.";
                    break;
            }
        }

        partial void OnThemeChanged(LauncherThemePreference value)
        {
            if (_isLoading)
            {
                return;
            }

            _applyTheme(value);
            _ = PersistAsync();
        }

        partial void OnShowExperimentalWarningsChanged(bool value)
        {
            if (_isLoading)
            {
                return;
            }

            _ = PersistAsync();
        }

        [RelayCommand]
        private async Task OpenDataFolderAsync()
        {
            await _outputRevealer.RevealInFolderAsync(_dataDirectory, CancellationToken.None).ConfigureAwait(true);
        }

        [RelayCommand]
        private async Task ClearRecentOutputsAsync()
        {
            await _recentItemsStore.ClearAsync(CancellationToken.None).ConfigureAwait(true);
            StatusMessage = "Lista de relatórios recentes limpa. Nenhum ficheiro foi apagado.";
        }

        private async Task PersistAsync()
        {
            LauncherSettings current = await _settingsStore.LoadAsync(CancellationToken.None).ConfigureAwait(true);

            LauncherSettings updated = current
                .WithTheme(Theme)
                .WithShowExperimentalWarnings(ShowExperimentalWarnings);

            await _settingsStore.SaveAsync(updated, CancellationToken.None).ConfigureAwait(true);
        }
    }
}
