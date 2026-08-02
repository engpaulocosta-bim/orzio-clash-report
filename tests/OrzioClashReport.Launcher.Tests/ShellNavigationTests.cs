using OrzioClashReport.Launcher.Contracts.Engine;
using OrzioClashReport.Launcher.Contracts.Settings;
using OrzioClashReport.Launcher.Desktop.ViewModels;

namespace OrzioClashReport.Launcher.Tests;

public sealed class ShellNavigationTests
{
    private static ShellViewModel CreateShell(out Dictionary<ShellSection, ViewModelBase> pages)
    {
        var engine = new EngineStatusViewModel(new StubEngineProbe(EngineProbeResult.Ready("0.1.0-preview.3")));

        pages = new Dictionary<ShellSection, ViewModelBase>();
        foreach (ShellSection section in Enum.GetValues<ShellSection>())
        {
            pages[section] = new SectionPlaceholderViewModel("Nav.Home", "App.Tagline");
        }

        return new ShellViewModel(engine, pages, new InterruptedJobsViewModel(new RecordingJournal()));
    }

    [Fact]
    public void TheShell_HasExactlySevenSectionsInTheDeclaredOrder()
    {
        ShellViewModel shell = CreateShell(out _);

        Assert.Equal(
            new[]
            {
                ShellSection.Home,
                ShellSection.QuickReport,
                ShellSection.Snapshots,
                ShellSection.Longitudinal,
                ShellSection.Projects,
                ShellSection.Governance,
                ShellSection.Settings,
            },
            shell.Sections.Select(section => section.Section).ToArray());

        using (LanguageScope.Use(InterfaceLanguage.Portuguese))
        {
            Assert.Equal(
                new[]
                {
                    "Início", "Relatório rápido", "Snapshots", "Longitudinal",
                    "Projetos", "Governança", "Definições",
                },
                shell.Sections.Select(section => section.Label).ToArray());
        }

        using (LanguageScope.Use(InterfaceLanguage.English))
        {
            Assert.Equal(
                new[]
                {
                    "Home", "Quick report", "Snapshots", "Longitudinal",
                    "Projects", "Governance", "Settings",
                },
                shell.Sections.Select(section => section.Label).ToArray());
        }
    }

    [Fact]
    public void EveryNavigationItem_HasALabelAndAGlyph()
    {
        ShellViewModel shell = CreateShell(out _);

        foreach (NavigationItemViewModel item in shell.Sections)
        {
            Assert.False(string.IsNullOrWhiteSpace(item.Label));
            Assert.False(string.IsNullOrWhiteSpace(item.Glyph));
        }
    }

    [Fact]
    public void TheShell_StartsOnHome()
    {
        ShellViewModel shell = CreateShell(out Dictionary<ShellSection, ViewModelBase> pages);

        Assert.Equal(ShellSection.Home, shell.SelectedSection.Section);
        Assert.Same(pages[ShellSection.Home], shell.CurrentPage);
    }

    [Fact]
    public void SelectingASection_SwapsThePage()
    {
        ShellViewModel shell = CreateShell(out Dictionary<ShellSection, ViewModelBase> pages);

        shell.Navigate(ShellSection.Governance);

        Assert.Equal(ShellSection.Governance, shell.SelectedSection.Section);
        Assert.Same(pages[ShellSection.Governance], shell.CurrentPage);
    }

    [Fact]
    public void AMissingPage_IsRejectedAtConstruction()
    {
        var engine = new EngineStatusViewModel(new StubEngineProbe(EngineProbeResult.Ready("0.1.0-preview.3")));
        var pages = new Dictionary<ShellSection, ViewModelBase>
        {
            [ShellSection.Home] = new SectionPlaceholderViewModel("Nav.Home", "App.Tagline"),
        };

        Assert.Throws<ArgumentException>(
            () => new ShellViewModel(engine, pages, new InterruptedJobsViewModel(new RecordingJournal())));
    }

    [Theory]
    [InlineData(1280, false, ShellViewModel.SidebarWidth)]
    [InlineData(901, false, ShellViewModel.SidebarWidth)]
    [InlineData(900, false, ShellViewModel.SidebarWidth)]
    [InlineData(899, true, ShellViewModel.RailWidth)]
    [InlineData(1024, false, ShellViewModel.SidebarWidth)]
    [InlineData(760, true, ShellViewModel.RailWidth)]
    public void TheSidebar_CollapsesToARailBelowNineHundred(
        double windowWidth, bool expectedCompact, double expectedWidth)
    {
        ShellViewModel shell = CreateShell(out _);

        shell.WindowWidth = windowWidth;

        Assert.Equal(expectedCompact, shell.IsCompactNavigation);
        Assert.Equal(expectedWidth, shell.NavigationWidth);
    }

    [Fact]
    public async Task Initialize_ProbesTheEngineAndReportsItInTheStatusBar()
    {
        var probe = new StubEngineProbe(EngineProbeResult.Ready("0.1.0-preview.3"));
        var engine = new EngineStatusViewModel(probe);
        var pages = new Dictionary<ShellSection, ViewModelBase>();
        foreach (ShellSection section in Enum.GetValues<ShellSection>())
        {
            pages[section] = new SectionPlaceholderViewModel("Nav.Home", "App.Tagline");
        }

        var shell = new ShellViewModel(engine, pages, new InterruptedJobsViewModel(new RecordingJournal()));
        await shell.InitializeAsync(CancellationToken.None);

        Assert.Equal(1, probe.ProbeCount);
        Assert.Equal(EngineStatus.Ready, engine.Status);
        Assert.Contains("0.1.0-preview.3", shell.StatusMessage, StringComparison.Ordinal);
    }

    [Fact]
    public void Home_ShowsAtMostFiveRecentOutputsNewestFirst()
    {
        var items = Enumerable.Range(0, 9)
            .Select(i => new RecentOutputItem(
                $"report-{i}.html",
                Path.Combine(Path.GetTempPath(), $"report-{i}.html"),
                DateTimeOffset.UnixEpoch.AddMinutes(i)))
            .ToArray();

        var home = new HomeViewModel(
            new EngineStatusViewModel(new StubEngineProbe(EngineProbeResult.Ready("0.1.0-preview.3"))),
            new InMemoryRecentItemsStore(items),
            new NoOpOutputRevealer(),
            _ => { });

        Assert.Equal(5, home.RecentOutputs.Count);
        Assert.True(home.HasRecentOutputs);
        Assert.Equal(
            new[] { "report-0.html", "report-1.html", "report-2.html", "report-3.html", "report-4.html" },
            home.RecentOutputs.Select(output => output.FileName).ToArray());
    }

    [Fact]
    public void Home_ShortcutsNavigateToTheMatchingSections()
    {
        var visited = new List<ShellSection>();
        var home = new HomeViewModel(
            new EngineStatusViewModel(new StubEngineProbe(EngineProbeResult.Ready("0.1.0-preview.3"))),
            new InMemoryRecentItemsStore(),
            new NoOpOutputRevealer(),
            visited.Add);

        home.OpenQuickReportCommand.Execute(null);
        home.OpenCreateSnapshotCommand.Execute(null);
        home.OpenCompareRevisionsCommand.Execute(null);

        Assert.Equal(
            new[] { ShellSection.QuickReport, ShellSection.Snapshots, ShellSection.Longitudinal },
            visited);
        Assert.False(home.HasRecentOutputs);
    }
}
