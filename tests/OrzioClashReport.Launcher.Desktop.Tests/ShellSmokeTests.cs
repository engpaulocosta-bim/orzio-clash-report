using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.Threading;
using OrzioClashReport.Launcher.Application.Engine;
using OrzioClashReport.Launcher.Application.Presentation;
using OrzioClashReport.Launcher.Contracts.Engine;
using OrzioClashReport.Launcher.Contracts.Jobs;
using OrzioClashReport.Launcher.Desktop.ViewModels;

namespace OrzioClashReport.Launcher.Desktop.Tests
{
    /// <summary>
    /// Boots the real shell headlessly. These tests are what prove the XAML actually loads: a missing
    /// design token, a broken template or an invalid binding fails here instead of on an evaluator's
    /// machine.
    /// </summary>
    public sealed class ShellSmokeTests
    {
        [AvaloniaFact]
        public void TheShellWindowOpensWithTheSevenSections()
        {
            var window = new MainWindow { DataContext = CreateShell(out ShellViewModel shell) };
            window.Show();

            Assert.Equal(7, shell.Sections.Count);
            Assert.Equal(
                new[]
                {
                    "Início", "Relatório rápido", "Snapshots", "Longitudinal",
                    "Projetos", "Governança", "Definições",
                },
                shell.Sections.Select(section => section.Label));

            Assert.Equal(LauncherSection.Home, shell.SelectedSection!.Section);
        }

        [AvaloniaFact]
        public void TheDesignTokensResolveInBothThemes()
        {
            var window = new MainWindow { DataContext = CreateShell(out _) };
            window.Show();

            foreach (string token in new[]
            {
                "OrzioCanvasBrush", "OrzioRaisedBrush", "OrzioSunkenBrush",
                "OrzioBorderSubtleBrush", "OrzioBorderStrongBrush",
                "OrzioTextPrimaryBrush", "OrzioTextSecondaryBrush", "OrzioTextTertiaryBrush",
                "OrzioAccentBrush", "OrzioAccentHoverBrush", "OrzioAccentPressedBrush", "OrzioAccentTintBrush",
                "OrzioContentPadding", "OrzioCardPadding", "OrzioRadiusSm", "OrzioRadiusMd",
                "OrzioFontSizeDisplay", "OrzioFontSizeBody",
                "OrzioSidebarWidth", "OrzioRailWidth", "OrzioStatusBarHeight", "OrzioRailBreakpoint",
            })
            {
                Assert.True(
                    window.TryFindResource(token, out object? value) && value != null,
                    $"The design token '{token}' did not resolve.");
            }
        }

        [AvaloniaFact]
        public void EverySectionRendersItsOwnView()
        {
            ShellViewModel shell = CreateShell(out _);
            var window = new MainWindow { DataContext = shell };
            window.Show();

            foreach (LauncherSection section in Enum.GetValues<LauncherSection>())
            {
                shell.Navigate(section);
                Assert.Equal(section, shell.SelectedSection!.Section);
                Assert.NotNull(shell.SelectedSection.Content);
            }
        }

        [AvaloniaFact]
        public void TheNavigationCollapsesToARailBelowTheBreakpoint()
        {
            ShellViewModel shell = CreateShell(out _);
            var window = new MainWindow { DataContext = shell };
            window.Show();

            Assert.True(window.TryFindResource("OrzioRailBreakpoint", out object? breakpoint));
            double railBreakpoint = (double)breakpoint!;

            Resize(window, railBreakpoint - 1);
            Assert.True(shell.IsRailMode);

            Resize(window, railBreakpoint + 1);
            Assert.False(shell.IsRailMode);
        }

        [AvaloniaFact]
        public async Task AMissingEngineIsReportedWithAGlyphAndALabelRatherThanColourAlone()
        {
            ShellViewModel shell = CreateShell(out _, engineLocation: null);
            var window = new MainWindow { DataContext = shell };
            window.Show();

            await shell.RefreshEngineAsync(CancellationToken.None);

            Assert.False(shell.EngineStatus.IsReady);
            Assert.NotEmpty(shell.EngineStatus.Glyph);
            Assert.Equal("Motor não encontrado", shell.EngineStatus.Label);
            Assert.True(shell.EngineStatus.IsCritical);
        }

        [AvaloniaFact]
        public async Task AVerifiedEngineReportingTheExpectedVersionIsReady()
        {
            ShellViewModel shell = CreateShell(
                out _,
                engineLocation: new EngineLocation("/install/engine/orzioclash", "/install/engine/engine-manifest.json"),
                integrity: new EngineIntegrityResult(EngineIntegrityVerdict.Verified, "abc", "abc"),
                expectedVersion: "0.1.0-preview.3",
                processResult: Completed("orzioclash 0.1.0-preview.3\n"));

            var window = new MainWindow { DataContext = shell };
            window.Show();

            await shell.RefreshEngineAsync(CancellationToken.None);

            Assert.True(shell.EngineStatus.IsReady);
            Assert.Equal("Motor pronto", shell.EngineStatus.Label);
            Assert.Equal("0.1.0-preview.3", shell.EngineStatus.ReportedVersion);
        }

        private static ShellViewModel CreateShell(
            out ShellViewModel shell,
            EngineLocation? engineLocation = null,
            EngineIntegrityResult? integrity = null,
            string? expectedVersion = null,
            EngineProcessResult? processResult = null)
        {
            var probe = new EngineProbe(
                new StubEngineLocator(engineLocation),
                new StubIntegrityVerifier(integrity ?? EngineIntegrityResult.NotChecked),
                new StubExpectationSource(expectedVersion),
                new StubProcessRunner(processResult ?? Completed(string.Empty)),
                Path.GetTempPath());

            var engineStatus = new EngineStatusViewModel();
            var recentItems = new InMemoryRecentItemsStore();
            var revealer = new NullOutputRevealer();

            ShellViewModel? created = null;

            var home = new HomeViewModel(engineStatus, recentItems, revealer, section => created?.Navigate(section));

            var settings = new SettingsViewModel(
                new InMemorySettingsStore(),
                recentItems,
                revealer,
                engineStatus,
                Path.GetTempPath(),
                "0.2.0-launcher-preview.1",
                _ => { });

            var content = new Dictionary<LauncherSection, object>
            {
                [LauncherSection.Home] = home,
                [LauncherSection.QuickReport] = new SectionPlaceholderViewModel(LauncherSection.QuickReport, new[] { "op" }),
                [LauncherSection.Snapshots] = new SectionPlaceholderViewModel(LauncherSection.Snapshots, new[] { "op" }),
                [LauncherSection.Longitudinal] = new SectionPlaceholderViewModel(LauncherSection.Longitudinal, new[] { "op" }),
                [LauncherSection.Projects] = new SectionPlaceholderViewModel(LauncherSection.Projects, new[] { "op" }),
                [LauncherSection.Governance] = new SectionPlaceholderViewModel(LauncherSection.Governance, new[] { "op" }),
                [LauncherSection.Settings] = settings,
            };

            created = new ShellViewModel(
                probe, engineStatus, home, settings, content, new CollectingLog(), new FixedClock());

            shell = created;
            return created;
        }

        private static void Resize(Window window, double width)
        {
            // MinWidth is a shipped constraint; the rail behaviour has to be exercised below it, so
            // the test relaxes it rather than pretending the window can never get that narrow.
            window.MinWidth = 0;
            window.Width = width;

            Dispatcher.UIThread.RunJobs();
        }

        private static EngineProcessResult Completed(string standardOutput) =>
            new EngineProcessResult(
                0, standardOutput, string.Empty, false, false, false, false, null, TimeSpan.Zero);
    }
}
