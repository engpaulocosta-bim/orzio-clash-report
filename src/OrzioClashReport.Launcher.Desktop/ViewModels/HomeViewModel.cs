using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.Input;
using OrzioClashReport.Launcher.Contracts.Ports;
using OrzioClashReport.Launcher.Contracts.Settings;

namespace OrzioClashReport.Launcher.Desktop.ViewModels
{
    /// <summary>One produced output, shown as a shortcut back to a result.</summary>
    public sealed partial class RecentOutputViewModel : ViewModelBase
    {
        private readonly IOutputRevealer _revealer;

        public RecentOutputViewModel(RecentOutputItem item, IOutputRevealer revealer)
        {
            Item = item ?? throw new ArgumentNullException(nameof(item));
            _revealer = revealer ?? throw new ArgumentNullException(nameof(revealer));
        }

        public RecentOutputItem Item { get; }

        public string FileName => Item.FileName;

        public string CreatedAtDisplay => Item.CreatedAtUtc.ToLocalTime().ToString("g");

        public string OpenLabel => Text("Action.Open");

        public string RevealLabel => Text("Action.Reveal");

        [RelayCommand]
        private Task OpenAsync() => _revealer.OpenAsync(Item.FullPath);

        [RelayCommand]
        private Task RevealAsync() => _revealer.RevealAsync(Item.FullPath);
    }

    /// <summary>
    /// The first screen: the three things a coordinator does most, the engine's state, and the last
    /// results. Exactly one filled primary action, so the main path is unambiguous.
    /// </summary>
    public sealed partial class HomeViewModel : ViewModelBase
    {
        internal const int RecentOutputLimit = 5;

        private readonly IRecentItemsStore _recentItems;
        private readonly IOutputRevealer _revealer;
        private readonly Action<ShellSection> _navigate;

        public HomeViewModel(
            EngineStatusViewModel engine,
            IRecentItemsStore recentItems,
            IOutputRevealer revealer,
            Action<ShellSection> navigate)
        {
            Engine = engine ?? throw new ArgumentNullException(nameof(engine));
            _recentItems = recentItems ?? throw new ArgumentNullException(nameof(recentItems));
            _revealer = revealer ?? throw new ArgumentNullException(nameof(revealer));
            _navigate = navigate ?? throw new ArgumentNullException(nameof(navigate));

            RecentOutputs = new ObservableCollection<RecentOutputViewModel>();
            RefreshRecentOutputs();
        }

        public EngineStatusViewModel Engine { get; }

        /// <summary>The five most recent outputs, newest first.</summary>
        public ObservableCollection<RecentOutputViewModel> RecentOutputs { get; }

        public bool HasRecentOutputs => RecentOutputs.Count > 0;

        public string Title => Text("App.Title");

        public string Tagline => Text("App.Tagline");

        public string EngineStateCaption => Text("Home.EngineStateCaption");

        public string StartCaption => Text("Home.StartCaption");

        public string RecentCaption => Text("Home.RecentCaption");

        public string NoRecentNote => Text("Home.NoRecent");

        public string QuickReportLabel => Text("Home.QuickReport");

        public string CreateSnapshotLabel => Text("Home.CreateSnapshot");

        public string CompareRevisionsLabel => Text("Home.CompareRevisions");

        public void RefreshRecentOutputs()
        {
            RecentOutputs.Clear();

            IReadOnlyList<RecentOutputItem> items = _recentItems.Load();
            int count = Math.Min(RecentOutputLimit, items.Count);
            for (int i = 0; i < count; i++)
            {
                RecentOutputs.Add(new RecentOutputViewModel(items[i], _revealer));
            }

            OnPropertyChanged(nameof(HasRecentOutputs));
        }

        [RelayCommand]
        private void OpenQuickReport() => _navigate(ShellSection.QuickReport);

        [RelayCommand]
        private void OpenCreateSnapshot() => _navigate(ShellSection.Snapshots);

        [RelayCommand]
        private void OpenCompareRevisions() => _navigate(ShellSection.Longitudinal);
    }
}
