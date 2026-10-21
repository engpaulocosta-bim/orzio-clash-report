using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using OrzioClashReport.Launcher.Application.Localization;
using OrzioClashReport.Launcher.Contracts.Diagnostics;
using OrzioClashReport.Launcher.Contracts.Engine;
using OrzioClashReport.Launcher.Contracts.Platform;
using OrzioClashReport.Launcher.Contracts.Settings;
using OrzioClashReport.Launcher.Desktop.Localization;

namespace OrzioClashReport.Launcher.Desktop.ViewModels
{
    /// <summary>
    /// Definições: language and appearance, where local data lives, what the engine is, and what the
    /// application deliberately does not do.
    /// </summary>
    /// <remarks>
    /// Sentences are held as keys and read in the active language. Choosing a language applies it at once
    /// and stores the preference; loading the stored preference at startup applies it without writing the
    /// settings file back.
    /// </remarks>
    public sealed partial class SettingsViewModel : LocalizedObservableObject
    {
        private readonly ISettingsStore _settingsStore;
        private readonly IRecentItemsStore _recentItemsStore;
        private readonly IOutputRevealer _outputRevealer;
        private readonly IDiagnosticsBundleBuilder _diagnosticsBundleBuilder;
        private readonly Action<LauncherThemePreference> _applyTheme;
        private readonly string _dataDirectory;

        private const int PreviewLineCount = 200;

        private bool _isLoading;
        private EngineInfo? _engine;
        private string? _lastBundlePath;
        private EngineIntegrityVerdict? _integrityVerdict;
        private string _statusKey = string.Empty;
        private object?[] _statusArguments = Array.Empty<object?>();

        [ObservableProperty]
        private LauncherThemePreference _theme = LauncherThemePreference.System;

        [ObservableProperty]
        private LauncherLanguagePreference _language = LauncherLanguagePreference.System;

        [ObservableProperty]
        private bool _showExperimentalWarnings = true;

        [ObservableProperty]
        private string _engineVersion = string.Empty;

        [ObservableProperty]
        private string _redactedLogPreview = string.Empty;

        [ObservableProperty]
        private bool _hasDiagnosticsPreview;

        [ObservableProperty]
        private bool _canRevealDiagnosticsBundle;

        public SettingsViewModel(
            ISettingsStore settingsStore,
            IRecentItemsStore recentItemsStore,
            IOutputRevealer outputRevealer,
            IDiagnosticsBundleBuilder diagnosticsBundleBuilder,
            EngineStatusViewModel engineStatus,
            string dataDirectory,
            string launcherVersion,
            Action<LauncherThemePreference> applyTheme,
            UpdatesViewModel updates)
        {
            _settingsStore = settingsStore ?? throw new ArgumentNullException(nameof(settingsStore));
            _diagnosticsBundleBuilder = diagnosticsBundleBuilder
                ?? throw new ArgumentNullException(nameof(diagnosticsBundleBuilder));
            _recentItemsStore = recentItemsStore ?? throw new ArgumentNullException(nameof(recentItemsStore));
            _outputRevealer = outputRevealer ?? throw new ArgumentNullException(nameof(outputRevealer));
            EngineStatus = engineStatus ?? throw new ArgumentNullException(nameof(engineStatus));
            _dataDirectory = dataDirectory ?? throw new ArgumentNullException(nameof(dataDirectory));
            LauncherVersion = launcherVersion ?? throw new ArgumentNullException(nameof(launcherVersion));
            _applyTheme = applyTheme ?? throw new ArgumentNullException(nameof(applyTheme));
            Updates = updates ?? throw new ArgumentNullException(nameof(updates));
        }

        /// <summary>The user-initiated update flow, shared with the sidebar and the home screen.</summary>
        public UpdatesViewModel Updates { get; }

        public EngineStatusViewModel EngineStatus { get; }

        public string LauncherVersion { get; }

        /// <summary>Shown so a user can find their own data; it is inside their profile, never in the installation.</summary>
        public string DataDirectory => _dataDirectory;

        /// <summary>What the engine's integrity check concluded, in the active language.</summary>
        public string EngineIntegrity => _integrityVerdict switch
        {
            EngineIntegrityVerdict.Verified => Loc.T("settings.integrity.verified"),
            EngineIntegrityVerdict.Mismatch => Loc.T("settings.integrity.mismatch"),
            EngineIntegrityVerdict.ManifestUnavailable => Loc.T("settings.integrity.manifestUnavailable"),
            _ => Loc.T("settings.integrity.notChecked"),
        };

        /// <summary>A short message about the last thing the user did here, in the active language.</summary>
        public string StatusMessage => _statusKey.Length == 0 ? string.Empty : Loc.T(_statusKey, _statusArguments);

        public IReadOnlyList<LocalizedOption<LauncherThemePreference>> ThemeOptions { get; } = new[]
        {
            new LocalizedOption<LauncherThemePreference>(LauncherThemePreference.System, "settings.theme.system"),
            new LocalizedOption<LauncherThemePreference>(LauncherThemePreference.Light, "settings.theme.light"),
            new LocalizedOption<LauncherThemePreference>(LauncherThemePreference.Dark, "settings.theme.dark"),
        };

        public IReadOnlyList<LocalizedOption<LauncherLanguagePreference>> LanguageOptions { get; } = new[]
        {
            new LocalizedOption<LauncherLanguagePreference>(LauncherLanguagePreference.System, "settings.language.system"),
            new LocalizedOption<LauncherLanguagePreference>(LauncherLanguagePreference.Portuguese, "settings.language.portuguese"),
            new LocalizedOption<LauncherLanguagePreference>(LauncherLanguagePreference.English, "settings.language.english"),
        };

        public LocalizedOption<LauncherThemePreference> SelectedThemeOption
        {
            get => ThemeOptions.First(option => option.Value == Theme);
            set
            {
                if (value != null)
                {
                    Theme = value.Value;
                }
            }
        }

        public LocalizedOption<LauncherLanguagePreference> SelectedLanguageOption
        {
            get => LanguageOptions.First(option => option.Value == Language);
            set
            {
                if (value != null)
                {
                    Language = value.Value;
                }
            }
        }

        private void SetStatus(string key, params object?[] arguments)
        {
            _statusKey = key;
            _statusArguments = arguments;
            OnPropertyChanged(nameof(StatusMessage));
        }

        public async Task LoadAsync(CancellationToken cancellationToken)
        {
            LauncherSettings settings = await _settingsStore.LoadAsync(cancellationToken).ConfigureAwait(true);

            _isLoading = true;
            try
            {
                Theme = settings.Theme;
                Language = settings.Language;
                ShowExperimentalWarnings = settings.ShowExperimentalWarnings;
            }
            finally
            {
                _isLoading = false;
            }

            // Applied explicitly rather than through the change handler: loading must restore the
            // stored theme without writing the settings file back on every start.
            _applyTheme(settings.Theme);
            ApplyLanguage(settings.Language);
        }

        /// <summary>What a diagnostic bundle would contain, shown before anything is written.</summary>
        public ObservableCollection<DiagnosticItemViewModel> DiagnosticPlan { get; } =
            new ObservableCollection<DiagnosticItemViewModel>();

        public void UpdateEngine(EngineInfo info)
        {
            _engine = info;
            EngineVersion = info.ReportedVersion ?? "—";

            _integrityVerdict = info.Integrity.Verdict;
            OnPropertyChanged(nameof(EngineIntegrity));
        }

        partial void OnThemeChanged(LauncherThemePreference value)
        {
            OnPropertyChanged(nameof(SelectedThemeOption));

            if (_isLoading)
            {
                return;
            }

            _applyTheme(value);
            _ = PersistAsync();
        }

        partial void OnLanguageChanged(LauncherLanguagePreference value)
        {
            OnPropertyChanged(nameof(SelectedLanguageOption));

            if (_isLoading)
            {
                return;
            }

            ApplyLanguage(value);
            _ = PersistAsync();
        }

        /// <summary>
        /// Turns the stored preference into a concrete language and makes it the active one. "System"
        /// follows the operating system's display language, read now so a changed system language is
        /// picked up the next time the application starts.
        /// </summary>
        private static void ApplyLanguage(LauncherLanguagePreference preference) =>
            Loc.Language = Loc.Resolve(preference, CultureInfo.CurrentUICulture);

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
            SetStatus("settings.data.recentCleared");
        }

        /// <summary>
        /// Shows exactly what a bundle would contain, including the redacted log itself, before
        /// anything is written. Nothing is collected until the user asks a second time.
        /// </summary>
        [RelayCommand]
        private async Task PrepareDiagnosticsAsync()
        {
            DiagnosticPlan.Clear();
            foreach (DiagnosticBundleItem item in _diagnosticsBundleBuilder.Plan())
            {
                DiagnosticPlan.Add(new DiagnosticItemViewModel(item));
            }

            RedactedLogPreview = await _diagnosticsBundleBuilder
                .PreviewRedactedLogAsync(PreviewLineCount, CancellationToken.None)
                .ConfigureAwait(true);

            HasDiagnosticsPreview = true;
            CanRevealDiagnosticsBundle = false;
            SetStatus(string.Empty);
        }

        [RelayCommand]
        private async Task CreateDiagnosticsBundleAsync()
        {
            if (_engine == null)
            {
                SetStatus("settings.diagnostics.waitForEngine");
                return;
            }

            try
            {
                _lastBundlePath = await _diagnosticsBundleBuilder
                    .BuildAsync(_engine, CancellationToken.None)
                    .ConfigureAwait(true);

                CanRevealDiagnosticsBundle = true;
                SetStatus("settings.diagnostics.created", Path.GetFileName(_lastBundlePath));
            }
            catch (IOException exception)
            {
                SetStatus("settings.diagnostics.failed", exception.Message);
            }
            catch (UnauthorizedAccessException)
            {
                SetStatus("settings.diagnostics.noPermission");
            }
        }

        [RelayCommand]
        private async Task RevealDiagnosticsBundleAsync()
        {
            if (_lastBundlePath != null)
            {
                await _outputRevealer.RevealInFolderAsync(_lastBundlePath, CancellationToken.None)
                    .ConfigureAwait(true);
            }
        }

        private async Task PersistAsync()
        {
            LauncherSettings current = await _settingsStore.LoadAsync(CancellationToken.None).ConfigureAwait(true);

            LauncherSettings updated = current
                .WithTheme(Theme)
                .WithLanguage(Language)
                .WithShowExperimentalWarnings(ShowExperimentalWarnings);

            await _settingsStore.SaveAsync(updated, CancellationToken.None).ConfigureAwait(true);
        }
    }

    /// <summary>
    /// One file a diagnostic bundle may contain. The name is fixed; the description is looked up by that
    /// name, because the allow-list is closed and every entry has a translation.
    /// </summary>
    public sealed class DiagnosticItemViewModel : LocalizedObservableObject
    {
        private readonly DiagnosticBundleItem _item;

        public DiagnosticItemViewModel(DiagnosticBundleItem item)
        {
            _item = item ?? throw new ArgumentNullException(nameof(item));
        }

        public string FileName => _item.FileName;

        /// <summary>
        /// The description in the active language. Falls back to the contract's own text if an entry were
        /// ever added to the allow-list without a translation; a test makes that impossible to merge.
        /// </summary>
        public string Description
        {
            get
            {
                string key = "diagnostics.item." + _item.FileName;
                return Loc.IsDefined(key) ? Loc.T(key) : _item.Description;
            }
        }
    }
}
