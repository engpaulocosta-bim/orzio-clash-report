using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace OrzioClashReport.Launcher.Tests
{
    /// <summary>
    /// Keeps the design system honest: every colour, font size, radius and spacing value lives in the
    /// token dictionary, and views only reference tokens or apply classes. A literal in a view is how
    /// a design system quietly stops being one.
    /// </summary>
    public sealed class LauncherStyleTests
    {
        private static readonly Regex HexColour = new Regex("#[0-9A-Fa-f]{3,8}\\b", RegexOptions.CultureInvariant);

        private static readonly (string Attribute, Regex Pattern)[] LiteralAttributes =
        {
            ("FontSize", NumericAttribute("FontSize")),
            ("CornerRadius", NumericAttribute("CornerRadius")),
            ("Padding", NumericAttribute("Padding")),
            ("Margin", NumericAttribute("Margin")),
            ("Spacing", NumericAttribute("Spacing")),
            ("BorderThickness", NumericAttribute("BorderThickness")),
        };

        [Fact]
        public void NoViewOrControlContainsAColourLiteral()
        {
            foreach (string file in MarkupOutsideThemes())
            {
                Match match = HexColour.Match(File.ReadAllText(file));

                Assert.False(
                    match.Success,
                    $"{Path.GetFileName(file)} contains the colour literal '{match.Value}'. "
                    + "Colours belong in Themes/Tokens.axaml.");
            }
        }

        [Fact]
        public void NoViewOrControlContainsASpacingTypeOrRadiusLiteral()
        {
            foreach (string file in MarkupOutsideThemes())
            {
                string content = File.ReadAllText(file);

                foreach ((string attribute, Regex pattern) in LiteralAttributes)
                {
                    Match match = pattern.Match(content);

                    Assert.False(
                        match.Success,
                        $"{Path.GetFileName(file)} sets {attribute} to the literal '{match.Value}'. "
                        + "Use a token or a style class from Themes/Controls.axaml.");
                }
            }
        }

        [Fact]
        public void EveryTokenReferencedByMarkupIsDefined()
        {
            string tokens = File.ReadAllText(Path.Combine(ThemesDirectory, "Tokens.axaml"));
            var defined = new HashSet<string>(StringComparer.Ordinal);

            foreach (Match match in Regex.Matches(tokens, "x:Key=\"(?<key>Orzio[A-Za-z0-9]+)\""))
            {
                defined.Add(match.Groups["key"].Value);
            }

            foreach (string file in AllMarkup())
            {
                foreach (Match match in Regex.Matches(
                    File.ReadAllText(file), "DynamicResource (?<key>Orzio[A-Za-z0-9]+)"))
                {
                    string key = match.Groups["key"].Value;

                    Assert.True(
                        defined.Contains(key),
                        $"{Path.GetFileName(file)} references the undefined token '{key}'.");
                }
            }
        }

        [Fact]
        public void TheDarkVariantOverridesExactlyTheColourTokens()
        {
            string tokens = File.ReadAllText(Path.Combine(ThemesDirectory, "Tokens.axaml"));

            int themeStart = tokens.IndexOf("<ResourceDictionary.ThemeDictionaries>", StringComparison.Ordinal);
            int darkStart = tokens.IndexOf("x:Key=\"Dark\"", StringComparison.Ordinal);
            int darkEnd = tokens.IndexOf("</ResourceDictionary>", darkStart, StringComparison.Ordinal);

            Assert.True(themeStart > 0, "The theme dictionaries block is missing.");
            Assert.True(darkStart > themeStart, "The dark theme dictionary is missing.");

            List<string> lightBrushes = BrushKeys(tokens.Substring(0, themeStart));
            List<string> darkBrushes = BrushKeys(tokens.Substring(darkStart, darkEnd - darkStart));

            // The light palette is the base, so every brush it defines must have a dark counterpart in
            // the same order: a token that exists in only one variant is a theme bug waiting to ship.
            Assert.Equal(lightBrushes, darkBrushes);
        }

        private static List<string> BrushKeys(string markup) =>
            Regex.Matches(markup, "<SolidColorBrush x:Key=\"(?<key>[A-Za-z0-9]+)\"")
                .Select(match => match.Groups["key"].Value)
                .ToList();

        private static Regex NumericAttribute(string attribute) =>
            new Regex(attribute + "=\"[0-9]", RegexOptions.CultureInvariant);

        private static string DesktopDirectory =>
            Path.Combine(RepositoryLayout.RootDirectory, "src", "OrzioClashReport.Launcher.Desktop");

        private static string ThemesDirectory => Path.Combine(DesktopDirectory, "Themes");

        private static IEnumerable<string> AllMarkup() =>
            Directory.EnumerateFiles(DesktopDirectory, "*.axaml", SearchOption.AllDirectories)
                .Where(path => !path.Contains(Path.DirectorySeparatorChar + "obj" + Path.DirectorySeparatorChar, StringComparison.Ordinal))
                .Where(path => !path.Contains(Path.DirectorySeparatorChar + "bin" + Path.DirectorySeparatorChar, StringComparison.Ordinal))
                .OrderBy(path => path, StringComparer.Ordinal);

        private static IEnumerable<string> MarkupOutsideThemes() =>
            AllMarkup().Where(path => !path.StartsWith(ThemesDirectory + Path.DirectorySeparatorChar, StringComparison.Ordinal));
    }
}
