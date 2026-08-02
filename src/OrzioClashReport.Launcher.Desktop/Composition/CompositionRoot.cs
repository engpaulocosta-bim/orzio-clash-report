using System;
using System.Collections.Generic;
using System.Reflection;
using Avalonia.Controls;
using Avalonia.Styling;
using OrzioClashReport.Launcher.Application;
using OrzioClashReport.Launcher.Contracts.Ports;
using OrzioClashReport.Launcher.Contracts.Settings;
using OrzioClashReport.Launcher.Desktop.Dialogs;
using OrzioClashReport.Launcher.Desktop.Platform;
using OrzioClashReport.Launcher.Desktop.ViewModels;
using OrzioClashReport.Launcher.Contracts.Diagnostics;
using OrzioClashReport.Launcher.Contracts.Engine;
using OrzioClashReport.Launcher.Infrastructure;
using OrzioClashReport.Launcher.Infrastructure.Diagnostics;
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
        private readonly IFileDialogs _fileDialogs;
        private readonly IOutputCollisionPrompt _collisionPrompt;
        private readonly OperationJobService _jobs;
        private readonly IJobJournal _journal;
        private readonly IDiagnosticBundleBuilder _diagnostics;
        private EngineProbeResult? _lastEngineState;

        private CompositionRoot(
            LauncherLocalPaths localPaths,
            ISettingsStore settingsStore,
            IRecentItemsStore recentItemsStore,
            IEngineProbe engineProbe,
            IOutputRevealer outputRevealer,
            IFileDialogs fileDialogs,
            IOutputCollisionPrompt collisionPrompt,
            OperationJobService jobs,
            IJobJournal journal,
            Func<CompositionRoot, IDiagnosticBundleBuilder> diagnostics)
        {
            _localPaths = localPaths;
            _settingsStore = settingsStore;
            _recentItemsStore = recentItemsStore;
            _engineProbe = engineProbe;
            _outputRevealer = outputRevealer;
            _fileDialogs = fileDialogs;
            _collisionPrompt = collisionPrompt;
            _jobs = jobs;
            _journal = journal;
            _diagnostics = diagnostics(this);
        }

        public static CompositionRoot Create(Func<Window?> mainWindow)
        {
            if (mainWindow == null)
            {
                throw new ArgumentNullException(nameof(mainWindow));
            }

            TopLevel? TopLevel() => mainWindow();

            LauncherLocalPaths localPaths = LauncherLocalPaths.ForCurrentUser();
            localPaths.EnsureCreated();

            // The language is settled before anything visible is built, so nothing is shown twice.
            var settingsStore = new JsonSettingsStore(localPaths.SettingsFilePath);
            Localization.Localizer.Instance.Language = settingsStore.Load().Language;

            EngineLayout engineLayout = EngineLayout.ForInstalledLauncher();
            var integrityVerifier = new EngineManifestIntegrityVerifier(engineLayout);
            var processRunner = new ProcessJobRunner();
            var recentItemsStore = new JsonRecentItemsStore(localPaths.RecentItemsFilePath);
            var fileDialogs = new StorageProviderFileDialogs(TopLevel);

            var log = new JsonLinesLauncherLog(localPaths.LogsDirectory);
            log.ApplyRetention();

            var gateway = new CliEngineGateway(
                engineLayout.ExecutablePath, new EngineArgumentBuilder(), processRunner);

            var journal = new FileJobJournal(localPaths.JobsDirectory);

            var jobs = new OperationJobService(
                gateway,
                journal,
                recentItemsStore,
                new PhysicalFileSystemProbe(),
                log,
                new Sha256PathRedactor());

            return new CompositionRoot(
                localPaths,
                settingsStore,
                recentItemsStore,
                new CliEngineProbe(engineLayout, integrityVerifier, processRunner),
                new TopLevelOutputRevealer(TopLevel),
                fileDialogs,
                new DialogOutputCollisionPrompt(mainWindow, fileDialogs),
                jobs,
                journal,
                root => new DiagnosticBundleBuilder(
                    new DiagnosticContext(
                        LauncherVersion,
                        () => root._lastEngineState,
                        integrityVerifier.Verify,
                        () => jobs.History),
                    new JsonLinesLauncherLogReader(log.ReadAll)));
        }

        public ShellViewModel CreateShell()
        {
            var engine = new EngineStatusViewModel(_engineProbe);
            engine.PropertyChanged += (_, _) => _lastEngineState = engine.Status == EngineStatus.Checking
                ? null
                : Snapshot(engine);

            ShellViewModel? shell = null;

            void Navigate(ShellSection section) => shell?.Navigate(section);

            var pages = new Dictionary<ShellSection, ViewModelBase>
            {
                [ShellSection.Home] = new HomeViewModel(engine, _recentItemsStore, _outputRevealer, Navigate),
                [ShellSection.QuickReport] = new QuickReportViewModel(CreateRunner(), _fileDialogs, engine),
                [ShellSection.Snapshots] = new OperationSectionViewModel(
                    "Section.Snapshots.Title",
                    "Section.Snapshots.Description",
                    new OperationFormViewModel[]
                    {
                        new CreateSnapshotFormViewModel(CreateRunner(), engine, _fileDialogs),
                        new CompareSnapshotsFormViewModel(CreateRunner(), engine, _fileDialogs),
                    }),
                [ShellSection.Longitudinal] = new OperationSectionViewModel(
                    "Section.Longitudinal.Title",
                    "Section.Longitudinal.Description",
                    new OperationFormViewModel[]
                    {
                        new CompareRunsFormViewModel(CreateRunner(), engine, _fileDialogs),
                        new IndexSnapshotsFormViewModel(CreateRunner(), engine, _fileDialogs),
                        new CompareIndexFormViewModel(CreateRunner(), engine, _fileDialogs),
                    }),
                [ShellSection.Projects] = new OperationSectionViewModel(
                    "Section.Projects.Title",
                    "Section.Projects.Description",
                    new OperationFormViewModel[]
                    {
                        new CreateProjectFormViewModel(CreateRunner(), engine, _fileDialogs),
                        new AppendProjectSnapshotFormViewModel(CreateRunner(), engine, _fileDialogs),
                        new RenderProjectFormViewModel(CreateRunner(), engine, _fileDialogs),
                    }),
                [ShellSection.Governance] = new OperationSectionViewModel(
                    "Section.Governance.Title",
                    "Section.Governance.Description",
                    new OperationFormViewModel[]
                    {
                        new CreateIdentityGovernanceFormViewModel(CreateRunner(), engine, _fileDialogs),
                        new AppendIdentityDecisionFormViewModel(CreateRunner(), engine, _fileDialogs),
                        new ValidateIdentityGovernanceFormViewModel(CreateRunner(), engine, _fileDialogs),
                        new RenderIdentityGovernanceReportFormViewModel(CreateRunner(), engine, _fileDialogs),
                    }),
                [ShellSection.Settings] = new SettingsViewModel(
                    _settingsStore,
                    _localPaths.RootDirectory,
                    LauncherVersion,
                    ApplyTheme,
                    new DiagnosticsViewModel(_diagnostics, _fileDialogs, _outputRevealer)),
            };

            shell = new ShellViewModel(engine, pages, new InterruptedJobsViewModel(_journal));
            ApplyTheme(_settingsStore.Load().Theme);
            return shell;
        }

        /// <summary>The engine state as the diagnostic bundle should see it: status and versions only.</summary>
        private static EngineProbeResult Snapshot(EngineStatusViewModel engine)
        {
            switch (engine.Status)
            {
                case EngineStatus.Ready:
                    return EngineProbeResult.Ready(engine.Version ?? "unknown");
                case EngineStatus.VersionMismatch:
                    return EngineProbeResult.VersionMismatch(engine.Version ?? "unknown", "unknown");
                case EngineStatus.IntegrityFailure:
                    return EngineProbeResult.IntegrityFailure(engine.Description);
                case EngineStatus.Missing:
                    return EngineProbeResult.Missing(engine.Description);
                default:
                    return EngineProbeResult.Unsupported(engine.Description);
            }
        }

        private OperationRunnerViewModel CreateRunner() =>
            new OperationRunnerViewModel(_jobs, _collisionPrompt, _outputRevealer);

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
