using System;
using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.Media;
using Avalonia.Styling;

namespace OrzioClashReport.Launcher.Desktop.Tests;

public sealed class DesignContrastTests
{
    [AvaloniaFact]
    public void NormalTextAndActionStatesMeetAaContrastInBothThemes()
    {
        var window = new MainWindow();
        foreach (ThemeVariant theme in new[] { ThemeVariant.Light, ThemeVariant.Dark })
        {
            foreach ((string foreground, string background) in new[]
            {
                ("OrzioTextPrimaryBrush", "OrzioRaisedBrush"),
                ("OrzioTextSecondaryBrush", "OrzioRaisedBrush"),
                ("OrzioTextTertiaryBrush", "OrzioSunkenBrush"),
                ("OrzioAccentBrush", "OrzioAccentTintBrush"),
                ("OrzioOnAccentBrush", "OrzioAccentBrush"),
                ("OrzioSidebarMutedBrush", "OrzioSidebarBrush"),
                ("OrzioSidebarTextBrush", "OrzioSidebarActiveBrush"),
            })
            {
                Assert.True(window.TryFindResource(foreground, theme, out object? fg));
                Assert.True(window.TryFindResource(background, theme, out object? bg));
                double a = Luminance(((ISolidColorBrush)fg!).Color);
                double b = Luminance(((ISolidColorBrush)bg!).Color);
                double contrast = (Math.Max(a, b) + 0.05) / (Math.Min(a, b) + 0.05);
                Assert.True(contrast >= 4.5, $"{theme}: {foreground} on {background} is {contrast:F2}:1");
            }
        }
    }

    private static double Luminance(Color c) => 0.2126 * Linear(c.R) + 0.7152 * Linear(c.G) + 0.0722 * Linear(c.B);
    private static double Linear(byte channel)
    {
        double value = channel / 255d;
        return value <= 0.04045 ? value / 12.92 : Math.Pow((value + 0.055) / 1.055, 2.4);
    }
}
