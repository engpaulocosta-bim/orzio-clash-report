using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace OrzioClashReport.Launcher.Desktop.ViewModels
{
    /// <summary>The seven places the application has. There is no basic or advanced mode.</summary>
    public enum ShellSection
    {
        Home = 0,
        QuickReport = 1,
        Snapshots = 2,
        Longitudinal = 3,
        Projects = 4,
        Governance = 5,
        Settings = 6,
    }

    /// <summary>One navigation entry. The glyph never stands alone: the label is always available.</summary>
    public sealed class NavigationItemViewModel : ViewModelBase
    {
        public NavigationItemViewModel(ShellSection section, string label, string glyph)
        {
            Section = section;
            Label = label;
            Glyph = glyph;
        }

        public ShellSection Section { get; }

        public string Label { get; }

        public string Glyph { get; }
    }

    /// <summary>
    /// The application shell: navigation, the current page, engine state, and the status bar.
    /// It owns no operation logic; every page brings its own.
    /// </summary>
    public sealed partial class ShellViewModel : ViewModelBase
    {
        internal const double RailBreakpoint = 900;
        internal const double SidebarWidth = 240;
        internal const double RailWidth = 56;

        private readonly IReadOnlyDictionary<ShellSection, ViewModelBase> _pages;

        [ObservableProperty]
        private NavigationItemViewModel _selectedSection;

        [ObservableProperty]
        private ViewModelBase _currentPage;

        [ObservableProperty]
        private double _windowWidth = SidebarWidth * 4;

        [ObservableProperty]
        private string _statusMessage = "Pronto.";

        public ShellViewModel(EngineStatusViewModel engine, IReadOnlyDictionary<ShellSection, ViewModelBase> pages)
        {
            Engine = engine ?? throw new ArgumentNullException(nameof(engine));
            _pages = pages ?? throw new ArgumentNullException(nameof(pages));

            Sections = new ReadOnlyCollection<NavigationItemViewModel>(new[]
            {
                new NavigationItemViewModel(ShellSection.Home, "Início", "⌂"),
                new NavigationItemViewModel(ShellSection.QuickReport, "Relatório rápido", "▤"),
                new NavigationItemViewModel(ShellSection.Snapshots, "Snapshots", "◧"),
                new NavigationItemViewModel(ShellSection.Longitudinal, "Longitudinal", "⇉"),
                new NavigationItemViewModel(ShellSection.Projects, "Projetos", "❏"),
                new NavigationItemViewModel(ShellSection.Governance, "Governança", "✎"),
                new NavigationItemViewModel(ShellSection.Settings, "Definições", "⚙"),
            });

            foreach (NavigationItemViewModel item in Sections)
            {
                if (!_pages.ContainsKey(item.Section))
                {
                    throw new ArgumentException($"No page was provided for {item.Section}.", nameof(pages));
                }
            }

            _selectedSection = Sections[0];
            _currentPage = _pages[ShellSection.Home];
        }

        public EngineStatusViewModel Engine { get; }

        public IReadOnlyList<NavigationItemViewModel> Sections { get; }

        /// <summary>Below the rail breakpoint the sidebar collapses to icons with tooltips.</summary>
        public bool IsCompactNavigation => WindowWidth < RailBreakpoint;

        public double NavigationWidth => IsCompactNavigation ? RailWidth : SidebarWidth;

        public void Navigate(ShellSection section)
        {
            foreach (NavigationItemViewModel item in Sections)
            {
                if (item.Section == section)
                {
                    SelectedSection = item;
                    return;
                }
            }

            throw new ArgumentOutOfRangeException(nameof(section), section, "Unknown shell section.");
        }

        [RelayCommand]
        private void NavigateTo(ShellSection section) => Navigate(section);

        public async Task InitializeAsync(CancellationToken cancellationToken)
        {
            await Engine.RefreshAsync(cancellationToken).ConfigureAwait(true);
            StatusMessage = Engine.Description;
        }

        partial void OnSelectedSectionChanged(NavigationItemViewModel value)
        {
            if (value != null)
            {
                CurrentPage = _pages[value.Section];
            }
        }

        partial void OnWindowWidthChanged(double value)
        {
            _ = value;
            OnPropertyChanged(nameof(IsCompactNavigation));
            OnPropertyChanged(nameof(NavigationWidth));
        }
    }
}
