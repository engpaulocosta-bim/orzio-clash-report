using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using OrzioClashReport.Launcher.Application.Presentation;
using OrzioClashReport.Launcher.Contracts.Platform;
using OrzioClashReport.Launcher.Contracts.Settings;

namespace OrzioClashReport.Launcher.Desktop.ViewModels
{
    /// <summary>
    /// The home screen: the three actions a coordinator reaches for, the engine state, and the five
    /// most recent outputs.
    /// </summary>
    public sealed partial class HomeViewModel : ObservableObject
    {
        /// <summary>The home list stays short on purpose; the full list lives in Definições.</summary>
        public const int VisibleRecentItems = 5;

        private readonly IRecentItemsStore _recentItemsStore;
        private readonly IOutputRevealer _outputRevealer;
        private readonly Action<LauncherSection> _navigate;

        [ObservableProperty]
        private bool _hasRecentOutputs;

        public HomeViewModel(
            EngineStatusViewModel engineStatus,
            IRecentItemsStore recentItemsStore,
            IOutputRevealer outputRevealer,
            Action<LauncherSection> navigate)
        {
            EngineStatus = engineStatus ?? throw new ArgumentNullException(nameof(engineStatus));
            _recentItemsStore = recentItemsStore ?? throw new ArgumentNullException(nameof(recentItemsStore));
            _outputRevealer = outputRevealer ?? throw new ArgumentNullException(nameof(outputRevealer));
            _navigate = navigate ?? throw new ArgumentNullException(nameof(navigate));
        }

        public EngineStatusViewModel EngineStatus { get; }

        public ObservableCollection<RecentOutputViewModel> RecentOutputs { get; } =
            new ObservableCollection<RecentOutputViewModel>();

        [RelayCommand]
        private void GoToQuickReport() => _navigate(LauncherSection.QuickReport);

        [RelayCommand]
        private void GoToSnapshots() => _navigate(LauncherSection.Snapshots);

        [RelayCommand]
        private void GoToLongitudinal() => _navigate(LauncherSection.Longitudinal);

        [RelayCommand]
        private async Task OpenRecentAsync(RecentOutputViewModel? recent)
        {
            if (recent == null)
            {
                return;
            }

            await _outputRevealer.OpenAsync(recent.Item.Path, CancellationToken.None).ConfigureAwait(true);
        }

        public async Task RefreshRecentOutputsAsync(CancellationToken cancellationToken)
        {
            IReadOnlyList<RecentOutputItem> items =
                await _recentItemsStore.LoadAsync(cancellationToken).ConfigureAwait(true);

            RecentOutputs.Clear();

            for (int i = 0; i < items.Count && i < VisibleRecentItems; i++)
            {
                RecentOutputs.Add(new RecentOutputViewModel(items[i]));
            }

            HasRecentOutputs = RecentOutputs.Count > 0;
        }
    }
}
