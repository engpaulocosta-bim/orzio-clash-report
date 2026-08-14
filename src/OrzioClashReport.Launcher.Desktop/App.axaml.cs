using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Avalonia.Styling;
using OrzioClashReport.Launcher.Application;
using OrzioClashReport.Launcher.Application.Engine;
using OrzioClashReport.Launcher.Application.Operations;
using OrzioClashReport.Launcher.Application.Presentation;
using OrzioClashReport.Launcher.Contracts.Engine;
using OrzioClashReport.Launcher.Contracts.Jobs;
using OrzioClashReport.Launcher.Contracts.Logging;
using OrzioClashReport.Launcher.Contracts.Platform;
using OrzioClashReport.Launcher.Contracts.Settings;
using OrzioClashReport.Launcher.Desktop.Platform;
using OrzioClashReport.Launcher.Desktop.ViewModels;
using OrzioClashReport.Launcher.Desktop.ViewModels.Operations;
using OrzioClashReport.Launcher.Infrastructure.Engine;
using OrzioClashReport.Launcher.Infrastructure.Logging;
using OrzioClashReport.Launcher.Infrastructure.Platform;
using OrzioClashReport.Launcher.Infrastructure.Process;
using OrzioClashReport.Launcher.Infrastructure.Storage;

namespace OrzioClashReport.Launcher.Desktop
{
    /// <summary>
    /// The application object and the manual composition root. Every adapter is constructed here,
    /// once, and handed to the view models explicitly. There is no dependency-injection container:
    /// the object graph is small enough to read in one screen, and keeping it explicit is what makes
    /// the boundaries checkable.
    /// </summary>
    public partial class App : Avalonia.Application
    {
        private MainWindow? _mainWindow;

        public override void Initialize()
        {
            AvaloniaXamlLoader.Load(this);
        }

        public override void OnFrameworkInitializationCompleted()
        {
            if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            {
                LauncherStorageLocations locations = LauncherStorageLocations.CreateDefault();
                locations.EnsureCreated();

                IClock clock = new SystemClock();
                ILauncherLog log = new JsonLinesLauncherLog(locations.LogsDirectory, clock);
                ISettingsStore settingsStore = new JsonSettingsStore(locations.SettingsFilePath);
                IRecentItemsStore recentItemsStore = new JsonRecentItemsStore(locations.RecentItemsFilePath);
                IOutputRevealer outputRevealer = new TopLevelOutputRevealer(() => _mainWindow);

                var manifestReader = new EngineManifestReader();
                var locator = new InstalledEngineLocator();
                IEngineProcessRunner processRunner = new ProcessJobRunner();
                IFileProbe fileProbe = new FileSystemProbe();

                var engineProbe = new EngineProbe(
                    locator,
                    new Sha256EngineIntegrityVerifier(manifestReader),
                    new ManifestEngineExpectationSource(manifestReader),
                    processRunner,

                    // Never the installation directory: probing must not be able to write there.
                    Path.GetTempPath());

                IEngineGateway gateway = new CliEngineGateway(engineProbe, processRunner, fileProbe);
                IJobJournal journal = new FileSystemJobJournal(locations.JobsDirectory);
                IPathRedactor redactor = new Sha256PathRedactor();

                var executor = new LauncherOperationExecutor(
                    gateway,
                    fileProbe,
                    recentItemsStore,
                    journal,
                    log,
                    redactor,
                    clock,
                    AppContext.BaseDirectory);

                var activeJobTracker = new ActiveJobTracker();
                var fileDialogService = new StorageProviderFileDialogService(() => _mainWindow);
                var engineStatus = new EngineStatusViewModel();

                ShellViewModel? shell = null;

                var quickReport = new QuickReportViewModel(
                    NewJob(executor, outputRevealer, activeJobTracker),
                    fileDialogService,
                    settingsStore,
                    engineStatus);

                OperationsSectionViewModel snapshots = BuildSnapshotsSection(
                    executor, outputRevealer, activeJobTracker, engineStatus, fileDialogService);

                OperationsSectionViewModel longitudinal = BuildLongitudinalSection(
                    executor, outputRevealer, activeJobTracker, engineStatus, fileDialogService);

                OperationsSectionViewModel projects = BuildProjectsSection(
                    executor, outputRevealer, activeJobTracker, engineStatus, fileDialogService);

                var home = new HomeViewModel(
                    engineStatus,
                    recentItemsStore,
                    outputRevealer,
                    section => shell?.Navigate(section));

                var settings = new SettingsViewModel(
                    settingsStore,
                    recentItemsStore,
                    outputRevealer,
                    engineStatus,
                    locations.RootDirectory,
                    LauncherBuildInfo.LauncherVersion,
                    ApplyTheme);

                shell = new ShellViewModel(
                    engineProbe,
                    engineStatus,
                    home,
                    settings,
                    BuildSectionContent(home, quickReport, snapshots, longitudinal, projects, settings),
                    log,
                    clock);

                _mainWindow = new MainWindow { DataContext = shell };
                desktop.MainWindow = _mainWindow;

                _ = shell.InitializeAsync(CancellationToken.None);
            }

            base.OnFrameworkInitializationCompleted();
        }

        private static JobViewModel NewJob(
            LauncherOperationExecutor executor, IOutputRevealer revealer, ActiveJobTracker tracker) =>
            new JobViewModel(executor, revealer, tracker);

        private static OperationsSectionViewModel BuildSnapshotsSection(
            LauncherOperationExecutor executor,
            IOutputRevealer revealer,
            ActiveJobTracker tracker,
            EngineStatusViewModel engineStatus,
            IFileDialogService dialogs) =>
            new OperationsSectionViewModel(
                LauncherSection.Snapshots,
                new OperationFormViewModel[]
                {
                    new SnapshotFormViewModel(NewJob(executor, revealer, tracker), engineStatus, dialogs),
                    new CompareSnapshotsFormViewModel(NewJob(executor, revealer, tracker), engineStatus, dialogs),
                });

        private static OperationsSectionViewModel BuildLongitudinalSection(
            LauncherOperationExecutor executor,
            IOutputRevealer revealer,
            ActiveJobTracker tracker,
            EngineStatusViewModel engineStatus,
            IFileDialogService dialogs) =>
            new OperationsSectionViewModel(
                LauncherSection.Longitudinal,
                new OperationFormViewModel[]
                {
                    new IndexSnapshotsFormViewModel(NewJob(executor, revealer, tracker), engineStatus, dialogs),
                    new CompareIndexFormViewModel(NewJob(executor, revealer, tracker), engineStatus, dialogs),
                    new CompareFormViewModel(NewJob(executor, revealer, tracker), engineStatus, dialogs),
                });

        private static OperationsSectionViewModel BuildProjectsSection(
            LauncherOperationExecutor executor,
            IOutputRevealer revealer,
            ActiveJobTracker tracker,
            EngineStatusViewModel engineStatus,
            IFileDialogService dialogs)
        {
            var render = new RenderProjectFormViewModel(
                NewJob(executor, revealer, tracker), engineStatus, dialogs);

            // Appending never regenerates the report on its own. Offering the next step explicitly,
            // with the project already filled in, is as far as the launcher goes.
            var append = new AppendProjectSnapshotFormViewModel(
                NewJob(executor, revealer, tracker), engineStatus, dialogs, render.UseProject);

            return new OperationsSectionViewModel(
                LauncherSection.Projects,
                new OperationFormViewModel[]
                {
                    new CreateProjectFormViewModel(NewJob(executor, revealer, tracker), engineStatus, dialogs),
                    append,
                    render,
                });
        }

        private static IReadOnlyDictionary<LauncherSection, object> BuildSectionContent(
            HomeViewModel home,
            QuickReportViewModel quickReport,
            OperationsSectionViewModel snapshots,
            OperationsSectionViewModel longitudinal,
            OperationsSectionViewModel projects,
            SettingsViewModel settings)
        {
            return new Dictionary<LauncherSection, object>
            {
                [LauncherSection.Home] = home,
                [LauncherSection.QuickReport] = quickReport,
                [LauncherSection.Snapshots] = snapshots,
                [LauncherSection.Longitudinal] = longitudinal,
                [LauncherSection.Projects] = projects,
                [LauncherSection.Governance] = new SectionPlaceholderViewModel(
                    LauncherSection.Governance,
                    new[]
                    {
                        "orzioclash create-identity-governance --project-id <id> -o <identity-governance.json>",
                        "orzioclash append-identity-decision --governance <identity-governance.json> ...",
                        "orzioclash validate-identity-governance --project <project.json> --governance <identity-governance.json>",
                        "orzioclash render-identity-governance-report --project <project.json> --governance <identity-governance.json> -o <report.html>",
                    }),
                [LauncherSection.Settings] = settings,
            };
        }

        private void ApplyTheme(LauncherThemePreference preference)
        {
            RequestedThemeVariant = preference switch
            {
                LauncherThemePreference.Light => ThemeVariant.Light,
                LauncherThemePreference.Dark => ThemeVariant.Dark,
                _ => ThemeVariant.Default,
            };
        }
    }
}
