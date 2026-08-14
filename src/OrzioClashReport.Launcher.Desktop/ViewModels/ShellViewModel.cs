using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using OrzioClashReport.Launcher.Application.Engine;
using OrzioClashReport.Launcher.Application.Presentation;
using OrzioClashReport.Launcher.Contracts.Engine;
using OrzioClashReport.Launcher.Contracts.Jobs;
using OrzioClashReport.Launcher.Contracts.Logging;
using OrzioClashReport.Launcher.Contracts.Platform;

namespace OrzioClashReport.Launcher.Desktop.ViewModels
{
    /// <summary>
    /// The application shell: exactly seven sections, the engine badge, and the status bar. There is
    /// no basic/advanced mode and no hidden section.
    /// </summary>
    public sealed partial class ShellViewModel : ObservableObject
    {
        private readonly EngineProbe _engineProbe;
        private readonly IJobJournal _journal;
        private readonly ILauncherLog _log;
        private readonly IClock _clock;
        private readonly Dictionary<LauncherSection, NavigationItemViewModel> _sectionsBySection =
            new Dictionary<LauncherSection, NavigationItemViewModel>();

        [ObservableProperty]
        private NavigationItemViewModel? _selectedSection;

        [ObservableProperty]
        private bool _isRailMode;

        [ObservableProperty]
        private string _statusMessage = "A verificar o motor.";

        [ObservableProperty]
        private bool _hasInterruptedOperations;

        public ShellViewModel(
            EngineProbe engineProbe,
            EngineStatusViewModel engineStatus,
            HomeViewModel home,
            SettingsViewModel settings,
            IReadOnlyDictionary<LauncherSection, object> sectionContent,
            IJobJournal journal,
            ILauncherLog log,
            IClock clock)
        {
            _engineProbe = engineProbe ?? throw new ArgumentNullException(nameof(engineProbe));
            _journal = journal ?? throw new ArgumentNullException(nameof(journal));
            EngineStatus = engineStatus ?? throw new ArgumentNullException(nameof(engineStatus));
            Home = home ?? throw new ArgumentNullException(nameof(home));
            Settings = settings ?? throw new ArgumentNullException(nameof(settings));
            _log = log ?? throw new ArgumentNullException(nameof(log));
            _clock = clock ?? throw new ArgumentNullException(nameof(clock));

            if (sectionContent == null)
            {
                throw new ArgumentNullException(nameof(sectionContent));
            }

            foreach (LauncherSectionPresentation presentation in LauncherSectionPresentation.All)
            {
                if (!sectionContent.TryGetValue(presentation.Section, out object? content))
                {
                    throw new ArgumentException(
                        $"No content was supplied for the '{presentation.Section}' section.", nameof(sectionContent));
                }

                var item = new NavigationItemViewModel(presentation, content);
                Sections.Add(item);
                _sectionsBySection.Add(presentation.Section, item);
            }

            SelectedSection = _sectionsBySection[LauncherSection.Home];
        }

        public ObservableCollection<NavigationItemViewModel> Sections { get; } =
            new ObservableCollection<NavigationItemViewModel>();

        /// <summary>Operations that were still running when the previous session ended.</summary>
        public ObservableCollection<string> InterruptedOperations { get; } = new ObservableCollection<string>();

        public EngineStatusViewModel EngineStatus { get; }

        public HomeViewModel Home { get; }

        public SettingsViewModel Settings { get; }

        public void Navigate(LauncherSection section)
        {
            if (_sectionsBySection.TryGetValue(section, out NavigationItemViewModel? item))
            {
                SelectedSection = item;
            }
        }

        /// <summary>
        /// Switches between the full sidebar and the icon rail. The threshold lives in the view, which
        /// is the only place that knows the window's width.
        /// </summary>
        public void SetRailMode(bool isRail) => IsRailMode = isRail;

        public async Task InitializeAsync(CancellationToken cancellationToken)
        {
            await Home.RefreshRecentOutputsAsync(cancellationToken).ConfigureAwait(true);
            await Settings.LoadAsync(cancellationToken).ConfigureAwait(true);
            await LoadInterruptedOperationsAsync(cancellationToken).ConfigureAwait(true);
            await RefreshEngineAsync(cancellationToken).ConfigureAwait(true);
        }

        /// <summary>
        /// Reports operations that were running when the application last stopped. Nothing is ever
        /// resumed automatically: what to do about an interrupted run is a human decision, and the
        /// engine's own create-new semantics make a blind retry unsafe.
        /// </summary>
        public async Task LoadInterruptedOperationsAsync(CancellationToken cancellationToken)
        {
            IReadOnlyList<JobJournalEntry> interrupted =
                await _journal.ReadInterruptedAsync(cancellationToken).ConfigureAwait(true);

            InterruptedOperations.Clear();

            foreach (JobJournalEntry entry in interrupted)
            {
                string when = entry.StartedAtUtc.ToLocalTime().ToString(
                    "yyyy-MM-dd HH:mm", System.Globalization.CultureInfo.InvariantCulture);

                InterruptedOperations.Add(
                    entry.OutputFileName == null
                        ? $"{entry.Operation} — iniciada em {when}"
                        : $"{entry.Operation} — {entry.OutputFileName}, iniciada em {when}");
            }

            HasInterruptedOperations = InterruptedOperations.Count > 0;

            if (HasInterruptedOperations)
            {
                _log.Write(new LauncherLogEntry(
                    _clock.UtcNow,
                    LauncherLogLevel.Warning,
                    "startup.interrupted",
                    "Interrupted operations were found at startup.",
                    new Dictionary<string, string>
                    {
                        ["count"] = InterruptedOperations.Count.ToString(
                            System.Globalization.CultureInfo.InvariantCulture),
                    }));
            }
        }

        [RelayCommand]
        private async Task DismissInterruptedAsync()
        {
            await _journal.ClearAsync(CancellationToken.None).ConfigureAwait(true);

            InterruptedOperations.Clear();
            HasInterruptedOperations = false;
        }

        public async Task RefreshEngineAsync(CancellationToken cancellationToken)
        {
            EngineInfo info = await _engineProbe.DescribeAsync(cancellationToken).ConfigureAwait(true);

            EngineStatus.Update(info);
            Settings.UpdateEngine(info);
            StatusMessage = info.Detail;

            _log.Write(new LauncherLogEntry(
                _clock.UtcNow,
                info.IsReady ? LauncherLogLevel.Information : LauncherLogLevel.Warning,
                "engine.probed",
                "Engine probe completed.",
                new Dictionary<string, string>
                {
                    ["status"] = info.Status.ToString(),
                    ["reportedVersion"] = info.ReportedVersion ?? string.Empty,
                    ["expectedVersion"] = info.ExpectedVersion,
                    ["integrity"] = info.Integrity.Verdict.ToString(),
                }));
        }

        partial void OnSelectedSectionChanged(NavigationItemViewModel? value)
        {
            if (value == null)
            {
                return;
            }

            StatusMessage = value.Description;
        }
    }
}
