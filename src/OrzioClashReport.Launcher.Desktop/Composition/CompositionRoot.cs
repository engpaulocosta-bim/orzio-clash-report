using System;
using System.Collections.Generic;
using System.Reflection;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Styling;
using OrzioClashReport.Launcher.Contracts.Ports;
using OrzioClashReport.Launcher.Contracts.Settings;
using OrzioClashReport.Launcher.Desktop.Platform;
using OrzioClashReport.Launcher.Desktop.ViewModels;
using OrzioClashReport.Launcher.Infrastructure;
using OrzioClashReport.Launcher.Infrastructure.Engine;
using OrzioClashReport.Launcher.Infrastructure.Processes;
using OrzioClashReport.Launcher.Infrastructure.Storage;

namespace OrzioClashReport.Launcher.Desktop.Composition
{
    /// <summary>
    /// The one place where concrete types are chosen and wired together, by hand. There is no
    /// container: the graph is small, and reading this file is the whole story.
    /// </summary>
    public sealed class CompositionRoot
    {
        private readonly LauncherLocalPaths _localPaths;
        private readonly ISettingsStore _settingsStore;
        private readonly IRecentItemsStore _recentItemsStore;
        private readonly IEngineProbe _engineProbe;
        private readonly IOutputRevealer _outputRevealer;

        private CompositionRoot(
            LauncherLocalPaths localPaths,
            ISettingsStore settingsStore,
            IRecentItemsStore recentItemsStore,
            IEngineProbe engineProbe,
            IOutputRevealer outputRevealer)
        {
            _localPaths = localPaths;
            _settingsStore = settingsStore;
            _recentItemsStore = recentItemsStore;
            _engineProbe = engineProbe;
            _outputRevealer = outputRevealer;
        }

        public static CompositionRoot Create(Func<TopLevel?> topLevel)
        {
            if (topLevel == null)
            {
                throw new ArgumentNullException(nameof(topLevel));
            }

            LauncherLocalPaths localPaths = LauncherLocalPaths.ForCurrentUser();
            localPaths.EnsureCreated();

            EngineLayout engineLayout = EngineLayout.ForInstalledLauncher();
            var integrityVerifier = new EngineManifestIntegrityVerifier(engineLayout);
            var processRunner = new ProcessJobRunner();

            return new CompositionRoot(
                localPaths,
                new JsonSettingsStore(localPaths.SettingsFilePath),
                new JsonRecentItemsStore(localPaths.RecentItemsFilePath),
                new CliEngineProbe(engineLayout, integrityVerifier, processRunner),
                new TopLevelOutputRevealer(topLevel));
        }

        public ShellViewModel CreateShell()
        {
            var engine = new EngineStatusViewModel(_engineProbe);
            ShellViewModel? shell = null;

            void Navigate(ShellSection section) => shell?.Navigate(section);

            var pages = new Dictionary<ShellSection, ViewModelBase>
            {
                [ShellSection.Home] = new HomeViewModel(engine, _recentItemsStore, _outputRevealer, Navigate),
                [ShellSection.QuickReport] = new SectionPlaceholderViewModel(
                    "Relatório rápido",
                    "Gere um relatório HTML agrupado a partir de um export XML do Clash Detective."),
                [ShellSection.Snapshots] = new SectionPlaceholderViewModel(
                    "Snapshots",
                    "Crie snapshots imutáveis de coordenação e compare dois snapshots persistidos."),
                [ShellSection.Longitudinal] = new SectionPlaceholderViewModel(
                    "Longitudinal",
                    "Declare a ordem explícita dos runs e compare transições adjacentes."),
                [ShellSection.Projects] = new SectionPlaceholderViewModel(
                    "Projetos",
                    "Crie o catálogo operacional do projeto, acrescente snapshots e regenere o relatório."),
                [ShellSection.Governance] = new SectionPlaceholderViewModel(
                    "Governança",
                    "Registe confirmações e rejeições humanas de identidade, valide-as e gere a revisão."),
                [ShellSection.Settings] = new SettingsViewModel(
                    _settingsStore, _localPaths.RootDirectory, LauncherVersion, ApplyTheme),
            };

            shell = new ShellViewModel(engine, pages);
            ApplyTheme(_settingsStore.Load().Theme);
            return shell;
        }

        internal static string LauncherVersion
        {
            get
            {
                string version = typeof(CompositionRoot).Assembly
                    .GetCustomAttribute<AssemblyInformationalVersionAttribute>()?
                    .InformationalVersion
                    ?? typeof(CompositionRoot).Assembly.GetName().Version?.ToString()
                    ?? "unknown";

                int metadataIndex = version.IndexOf('+');
                return metadataIndex >= 0 ? version.Substring(0, metadataIndex) : version;
            }
        }

        private static void ApplyTheme(ThemePreference preference)
        {
            Avalonia.Application? application = Avalonia.Application.Current;
            if (application == null)
            {
                return;
            }

            application.RequestedThemeVariant = preference switch
            {
                ThemePreference.Light => ThemeVariant.Light,
                ThemePreference.Dark => ThemeVariant.Dark,
                _ => ThemeVariant.Default,
            };
        }
    }
}
