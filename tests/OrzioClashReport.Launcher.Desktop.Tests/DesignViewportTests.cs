using System;
using System.IO;
using System.Linq;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Styling;
using Avalonia.Threading;
using Avalonia.VisualTree;

namespace OrzioClashReport.Launcher.Desktop.Tests;

public sealed class DesignViewportTests
{
    [AvaloniaTheory]
    [InlineData(1280, 720, false)]
    [InlineData(1600, 900, false)]
    [InlineData(1920, 1080, false)]
    [InlineData(1280, 720, true)]
    public void OverviewRendersAtRequiredViewportsWithReachableNavigation(int width, int height, bool dark)
    {
        var window = new MainWindow
        {
            DataContext = ShellSmokeTests.CreateShell(out _),
            Width = width,
            Height = height,
            RequestedThemeVariant = dark ? ThemeVariant.Dark : ThemeVariant.Light,
        };
        try
        {
            window.Show();
            Dispatcher.UIThread.RunJobs();
            window.UpdateLayout();
            AvaloniaHeadlessPlatform.ForceRenderTimerTick();
            Assert.NotNull(window.Icon);

            var navigation = window.GetVisualDescendants().OfType<ListBox>()
                .Single(list => list.Classes.Contains("navigation"));
            Assert.Equal(7, navigation.ItemCount);
            Assert.True(navigation.Bounds.Height > 7 * 30);
            foreach (TextBlock label in navigation.GetVisualDescendants().OfType<TextBlock>())
            {
                Assert.True(label.Bounds.Width + 1 >= label.DesiredSize.Width,
                    $"Navigation label is clipped: {label.Text}");
            }

            using var frame = window.CaptureRenderedFrame();
            Assert.NotNull(frame);
            Assert.True(frame.PixelSize.Width >= width);
            string? screenshots = Environment.GetEnvironmentVariable("ORZIO_QA_SCREENSHOTS");
            if (!string.IsNullOrEmpty(screenshots))
            {
                Directory.CreateDirectory(screenshots);
                frame.Save(Path.Combine(screenshots, $"overview-{width}x{height}-{(dark ? "dark" : "light")}.png"));
            }
        }
        finally { window.Close(); }
    }
}
