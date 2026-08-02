using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using OrzioClashReport.Launcher.Contracts.Ports;
using OrzioClashReport.Launcher.Contracts.Settings;
using OrzioClashReport.Launcher.Desktop.Localization;

namespace OrzioClashReport.Launcher.Desktop.ViewModels
{
    public sealed class ThemeOptionViewModel : ViewModelBase
    {
        private readonly string _labelKey;

        public ThemeOptionViewModel(ThemePreference preference, string labelKey)
        {
            Preference = preference;
            _labelKey = labelKey;
        }

        public ThemePreference Preference { get; }

        public string Label => Text(_labelKey);
    }

    public sealed class LanguageOptionViewModel : ViewModelBase
    {
        private readonly string _labelKey;

        public LanguageOptionViewModel(InterfaceLanguage language, string labelKey)
        {
            Language = language;
            _labelKey = labelKey;
        }

        public InterfaceLanguage Language { get; }

        public string Label => Text(_labelKey);
    }

    /// <summary>
    /// Local preferences and the facts a coordinator needs about where their data lives. Nothing here
    /// changes a technical result: the language changes visible text only, and never a value the
    /// engine receives.
    /// </summary>
    public sealed partial class SettingsViewModel : ViewModelBase
    {
        private readonly ISettingsStore _store;
        private readonly Action<ThemePreference> _applyTheme;
        private bool _loading;

        [ObservableProperty]
        private ThemeOptionViewModel _selectedTheme;

        [ObservableProperty]
        private LanguageOptionViewModel _selectedLanguage;

        [ObservableProperty]
        private bool _showExperimentalWarnings;

        public SettingsViewModel(
            ISettingsStore store,
            string localDataDirectory,
            string launcherVersion,
            Action<ThemePreference> applyTheme,
            DiagnosticsViewModel diagnostics)
        {
            _store = store ?? throw new ArgumentNullException(nameof(store));
            _applyTheme = applyTheme ?? throw new ArgumentNullException(nameof(applyTheme));
            Diagnostics = diagnostics ?? throw new ArgumentNullException(nameof(diagnostics));

            LocalDataDirectory = localDataDirectory ?? throw new ArgumentNullException(nameof(localDataDirectory));
            LauncherVersion = launcherVersion ?? throw new ArgumentNullException(nameof(launcherVersion));

            Themes = new ReadOnlyCollection<ThemeOptionViewModel>(new[]
            {
                new ThemeOptionViewModel(ThemePreference.System, "Settings.Theme.System"),
                new ThemeOptionViewModel(ThemePreference.Light, "Settings.Theme.Light"),
                new ThemeOptionViewModel(ThemePreference.Dark, "Settings.Theme.Dark"),
            });

            Languages = new ReadOnlyCollection<LanguageOptionViewModel>(new[]
            {
                new LanguageOptionViewModel(InterfaceLanguage.System, "Settings.Language.System"),
                new LanguageOptionViewModel(InterfaceLanguage.Portuguese, "Settings.Language.Portuguese"),
                new LanguageOptionViewModel(InterfaceLanguage.English, "Settings.Language.English"),
            });

            LauncherSettings settings = _store.Load();

            _loading = true;
            _selectedTheme = FindTheme(settings.Theme);
            _selectedLanguage = FindLanguage(settings.Language);
            _showExperimentalWarnings = settings.ShowExperimentalWarnings;
            _loading = false;
        }

        public IReadOnlyList<ThemeOptionViewModel> Themes { get; }

        public IReadOnlyList<LanguageOptionViewModel> Languages { get; }

        /// <summary>Where the launcher keeps its own files. Project data never lives here.</summary>
        public string LocalDataDirectory { get; }

        public string LauncherVersion { get; }

        public DiagnosticsViewModel Diagnostics { get; }

        public string Title => Text("Settings.Title");

        public string AppearanceCaption => Text("Settings.AppearanceCaption");

        public string ThemeLabel => Text("Settings.Theme");

        public string LanguageLabel => Text("Settings.Language");

        public string WarningsCaption => Text("Settings.WarningsCaption");

        public string ShowExperimentalLabel => Text("Settings.ShowExperimental");

        public string ExperimentalNote => Text("Settings.ExperimentalNote");

        public string LocalDataCaption => Text("Settings.LocalDataCaption");

        public string LocalDataIntro => Text("Settings.LocalDataIntro");

        public string LocalDataNote => Text("Settings.LocalDataNote");

        public string VersionLabel => Text("Settings.Version");

        public string PrivacyNote => Text("Settings.PrivacyNote");

        partial void OnSelectedThemeChanged(ThemeOptionViewModel value)
        {
            if (_loading || value == null)
            {
                return;
            }

            _applyTheme(value.Preference);
            Persist();
        }

        partial void OnSelectedLanguageChanged(LanguageOptionViewModel value)
        {
            if (_loading || value == null)
            {
                return;
            }

            // Changing this re-reads every visible string; nothing reopens and nothing restarts.
            Localizer.Instance.Language = value.Language;
            Persist();
        }

        partial void OnShowExperimentalWarningsChanged(bool value)
        {
            _ = value;
            if (!_loading)
            {
                Persist();
            }
        }

        private void Persist()
        {
            LauncherSettings settings = _store.Load()
                .WithTheme(SelectedTheme.Preference)
                .WithLanguage(SelectedLanguage.Language)
                .WithShowExperimentalWarnings(ShowExperimentalWarnings);

            _store.Save(settings);
        }

        private ThemeOptionViewModel FindTheme(ThemePreference preference)
        {
            foreach (ThemeOptionViewModel option in Themes)
            {
                if (option.Preference == preference)
                {
                    return option;
                }
            }

            return Themes[0];
        }

        private LanguageOptionViewModel FindLanguage(InterfaceLanguage language)
        {
            foreach (LanguageOptionViewModel option in Languages)
            {
                if (option.Language == language)
                {
                    return option;
                }
            }

            return Languages[0];
        }
    }
}
