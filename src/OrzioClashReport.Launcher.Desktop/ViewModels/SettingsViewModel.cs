using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using OrzioClashReport.Launcher.Contracts.Ports;
using OrzioClashReport.Launcher.Contracts.Settings;

namespace OrzioClashReport.Launcher.Desktop.ViewModels
{
    public sealed class ThemeOptionViewModel
    {
        public ThemeOptionViewModel(ThemePreference preference, string label)
        {
            Preference = preference;
            Label = label;
        }

        public ThemePreference Preference { get; }

        public string Label { get; }
    }

    /// <summary>
    /// Local preferences and the facts a coordinator needs about where their data lives. Nothing here
    /// changes a technical result.
    /// </summary>
    public sealed partial class SettingsViewModel : ViewModelBase
    {
        private readonly ISettingsStore _store;
        private readonly Action<ThemePreference> _applyTheme;
        private bool _loading;

        [ObservableProperty]
        private ThemeOptionViewModel _selectedTheme;

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
                new ThemeOptionViewModel(ThemePreference.System, "Seguir o sistema"),
                new ThemeOptionViewModel(ThemePreference.Light, "Claro"),
                new ThemeOptionViewModel(ThemePreference.Dark, "Escuro"),
            });

            LauncherSettings settings = _store.Load();

            _loading = true;
            _selectedTheme = FindTheme(settings.Theme);
            _showExperimentalWarnings = settings.ShowExperimentalWarnings;
            _loading = false;
        }

        public IReadOnlyList<ThemeOptionViewModel> Themes { get; }

        /// <summary>Where the launcher keeps its own files. Project data never lives here.</summary>
        public string LocalDataDirectory { get; }

        public string LauncherVersion { get; }

        public DiagnosticsViewModel Diagnostics { get; }

        partial void OnSelectedThemeChanged(ThemeOptionViewModel value)
        {
            if (_loading || value == null)
            {
                return;
            }

            _applyTheme(value.Preference);
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
    }
}
