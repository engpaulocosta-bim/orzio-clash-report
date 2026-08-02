using System.Text.RegularExpressions;

namespace OrzioClashReport.Launcher.Tests;

/// <summary>
/// The design system is only a design system if the views cannot bypass it. These assertions read
/// the shipped XAML and refuse literals that belong in Theme/Tokens.axaml.
/// </summary>
public sealed class DesignTokenTests
{
    private static string DesktopRoot =>
        Path.Combine(RepositoryPaths.SourceRoot, "OrzioClashReport.Launcher.Desktop");

    private static string ThemeDirectory => Path.Combine(DesktopRoot, "Theme");

    private static IEnumerable<string> ViewFiles =>
        Directory
            .GetFiles(DesktopRoot, "*.axaml", SearchOption.AllDirectories)
            .Where(path => !path.StartsWith(ThemeDirectory + Path.DirectorySeparatorChar, StringComparison.Ordinal))
            .OrderBy(path => path, StringComparer.Ordinal);

    public static TheoryData<string> Views
    {
        get
        {
            var data = new TheoryData<string>();
            foreach (string file in ViewFiles)
            {
                data.Add(Path.GetRelativePath(RepositoryPaths.RepositoryRoot, file));
            }

            return data;
        }
    }

    [Fact]
    public void ThereAreViewsToCheck()
    {
        Assert.NotEmpty(ViewFiles);
    }

    [Theory]
    [MemberData(nameof(Views))]
    public void NoView_ContainsALiteralColour(string relativePath)
    {
        string content = ReadView(relativePath);

        Assert.DoesNotContain("#", content, StringComparison.Ordinal);
        Assert.DoesNotMatch(new Regex(@"\bColor\s*=\s*""(?!\{)"), content);

        // "Transparent" is the absence of a fill rather than a palette choice, so it is the one
        // named brush a view may state directly. Every other named colour must come from a token.
        foreach (Match match in NamedBrushPattern.Matches(content))
        {
            string value = match.Groups["value"].Value;
            Assert.True(
                value == "Transparent",
                $"{relativePath} sets a literal brush '{value}'. Use a token from Theme/Tokens.axaml.");
        }
    }

    [Theory]
    [MemberData(nameof(Views))]
    public void NoView_ContainsALiteralSizeRadiusOrSpacing(string relativePath)
    {
        string content = ReadView(relativePath);

        foreach (string property in NumericProperties)
        {
            var pattern = new Regex($@"\b{property}\s*=\s*""(?<value>[^""]*)""");
            foreach (Match match in pattern.Matches(content))
            {
                string value = match.Groups["value"].Value.Trim();
                Assert.False(
                    LooksNumeric(value),
                    $"{relativePath} sets {property}=\"{value}\". Use a token from Theme/Tokens.axaml.");
            }
        }
    }

    [Fact]
    public void TokensDeclare_TheLayoutTheProductSpecifies()
    {
        string tokens = File.ReadAllText(Path.Combine(ThemeDirectory, "Tokens.axaml"));

        Assert.Contains("<x:Double x:Key=\"OrzioWindowMinWidth\">1024</x:Double>", tokens, StringComparison.Ordinal);
        Assert.Contains("<x:Double x:Key=\"OrzioWindowMinHeight\">680</x:Double>", tokens, StringComparison.Ordinal);
        Assert.Contains("<x:Double x:Key=\"OrzioSidebarWidth\">240</x:Double>", tokens, StringComparison.Ordinal);
        Assert.Contains("<x:Double x:Key=\"OrzioRailWidth\">56</x:Double>", tokens, StringComparison.Ordinal);
        Assert.Contains("<x:Double x:Key=\"OrzioRailBreakpoint\">900</x:Double>", tokens, StringComparison.Ordinal);
        Assert.Contains("<x:Double x:Key=\"OrzioStatusBarHeight\">40</x:Double>", tokens, StringComparison.Ordinal);
        Assert.Contains("<Thickness x:Key=\"OrzioContentPadding\">32</Thickness>", tokens, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("Light", "OrzioCanvasColor", "#FFFFFF")]
    [InlineData("Light", "OrzioRaisedColor", "#F7F8FA")]
    [InlineData("Light", "OrzioSunkenColor", "#F0F2F5")]
    [InlineData("Light", "OrzioBorderSubtleColor", "#E4E7EC")]
    [InlineData("Light", "OrzioBorderStrongColor", "#D0D5DD")]
    [InlineData("Light", "OrzioTextPrimaryColor", "#101828")]
    [InlineData("Light", "OrzioTextSecondaryColor", "#475467")]
    [InlineData("Light", "OrzioTextTertiaryColor", "#98A2B3")]
    [InlineData("Light", "OrzioAccentColor", "#2A5BD7")]
    [InlineData("Light", "OrzioAccentHoverColor", "#2450BE")]
    [InlineData("Light", "OrzioAccentPressedColor", "#1E45A8")]
    [InlineData("Light", "OrzioAccentTintColor", "#EEF3FE")]
    [InlineData("Dark", "OrzioCanvasColor", "#0F1216")]
    [InlineData("Dark", "OrzioRaisedColor", "#151A20")]
    [InlineData("Dark", "OrzioSunkenColor", "#1D242C")]
    [InlineData("Dark", "OrzioBorderSubtleColor", "#272F38")]
    [InlineData("Dark", "OrzioBorderStrongColor", "#364049")]
    [InlineData("Dark", "OrzioTextPrimaryColor", "#E6EAEF")]
    [InlineData("Dark", "OrzioTextSecondaryColor", "#A2AEBB")]
    [InlineData("Dark", "OrzioTextTertiaryColor", "#6C7A89")]
    [InlineData("Dark", "OrzioAccentColor", "#5B8DEF")]
    [InlineData("Dark", "OrzioAccentHoverColor", "#7BA3F3")]
    [InlineData("Dark", "OrzioAccentPressedColor", "#4A79D6")]
    [InlineData("Dark", "OrzioAccentTintColor", "#17243A")]
    public void TokensDeclare_TheSpecifiedPalette(string variant, string key, string expected)
    {
        string tokens = File.ReadAllText(Path.Combine(ThemeDirectory, "Tokens.axaml"));
        string section = ExtractThemeDictionary(tokens, variant);

        Assert.Contains($"<Color x:Key=\"{key}\">{expected}</Color>", section, StringComparison.Ordinal);
    }

    private static string ExtractThemeDictionary(string tokens, string variant)
    {
        string open = $"<ResourceDictionary x:Key=\"{variant}\">";
        int start = tokens.IndexOf(open, StringComparison.Ordinal);
        Assert.True(start >= 0, $"No '{variant}' theme dictionary was found.");

        int end = tokens.IndexOf("</ResourceDictionary>", start, StringComparison.Ordinal);
        Assert.True(end > start, $"The '{variant}' theme dictionary is not closed.");

        return tokens.Substring(start, end - start);
    }

    private static string ReadView(string relativePath) =>
        File.ReadAllText(Path.Combine(RepositoryPaths.RepositoryRoot, relativePath));

    private static bool LooksNumeric(string value)
    {
        if (value.Length == 0 || value.StartsWith("{", StringComparison.Ordinal))
        {
            return false;
        }

        foreach (char c in value)
        {
            bool allowed = char.IsDigit(c) || c == '.' || c == ',' || c == ' ' || c == '-';
            if (!allowed)
            {
                return false;
            }
        }

        return value.Any(char.IsDigit);
    }

    private static readonly Regex NamedBrushPattern = new Regex(
        @"\b(?:Background|Foreground|BorderBrush)\s*=\s*""(?<value>[^""{][^""]*)""");

    private static readonly string[] NumericProperties =
    {
        "FontSize",
        "CornerRadius",
        "Padding",
        "Margin",
        "Spacing",
        "BorderThickness",
        "Width",
        "Height",
        "MinWidth",
        "MinHeight",
        "MaxWidth",
        "MaxHeight",
    };
}
