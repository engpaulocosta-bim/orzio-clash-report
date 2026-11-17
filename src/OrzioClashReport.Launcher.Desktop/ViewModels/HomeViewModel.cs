using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using OrzioClashReport.Launcher.Application.Presentation;
using OrzioClashReport.Launcher.Contracts.Platform;
using OrzioClashReport.Launcher.Contracts.Settings;
using OrzioClashReport.Launcher.Application.Localization;
using OrzioClashReport.Launcher.Desktop.Localization;

namespace OrzioClashReport.Launcher.Desktop.ViewModels
{
    /// <summary>
    /// The home screen: the three actions a coordinator reaches for, the engine state, and the five
    /// most recent outputs.
    /// </summary>
    public sealed partial class HomeViewModel : LocalizedObservableObject
    {
        /// <summary>The home list stays short on purpose; the full list lives in Definições.</summary>
        public const int VisibleRecentItems = 5;

        private readonly IRecentItemsStore _recentItemsStore;
        private readonly IOutputRevealer _outputRevealer;
        private readonly Action<LauncherSection> _navigate;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(IsRecentEmpty))]
        private bool _hasRecentOutputs;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(IsRecentEmpty))]
        private bool _isLoadingRecentOutputs;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(RecentError), nameof(HasRecentError), nameof(IsRecentEmpty))]
        private string? _recentErrorKey;

        public string RecentError => RecentErrorKey == null ? string.Empty : Loc.T(RecentErrorKey);
        public bool HasRecentError => RecentErrorKey != null;
        public bool IsRecentEmpty => !HasRecentOutputs && !IsLoadingRecentOutputs && !HasRecentError;

        public HomeViewModel(
            EngineStatusViewModel engineStatus,
            IRecentItemsStore recentItemsStore,
            IOutputRevealer outputRevealer,
            Action<LauncherSection> navigate,
            UpdatesViewModel updates)
        {
            EngineStatus = engineStatus ?? throw new ArgumentNullException(nameof(engineStatus));
            _recentItemsStore = recentItemsStore ?? throw new ArgumentNullException(nameof(recentItemsStore));
            _outputRevealer = outputRevealer ?? throw new ArgumentNullException(nameof(outputRevealer));
            _navigate = navigate ?? throw new ArgumentNullException(nameof(navigate));
            Updates = updates ?? throw new ArgumentNullException(nameof(updates));
        }

        public UpdatesViewModel Updates { get; }

        public EngineStatusViewModel EngineStatus { get; }

        public ObservableCollection<RecentOutputViewModel> RecentOutputs { get; } =
            new ObservableCollection<RecentOutputViewModel>();

        [RelayCommand]
        private void GoToSettings() => _navigate(LauncherSection.Settings);

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

            RecentErrorKey = null;
            try
            {
                if (!await _outputRevealer.OpenAsync(recent.Item.Path, CancellationToken.None).ConfigureAwait(true))
                {
                    RecentErrorKey = "home.recent.openFailed";
                }
            }
            catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or InvalidOperationException)
            {
                RecentErrorKey = "home.recent.openFailed";
            }
        }

        public async Task RefreshRecentOutputsAsync(CancellationToken cancellationToken)
        {
            IsLoadingRecentOutputs = true;
            RecentErrorKey = null;
            try
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
            catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or JsonException)
            {
                RecentErrorKey = "home.recent.loadFailed";
            }
            finally
            {
                IsLoadingRecentOutputs = false;
            }
        }
    }
}
