using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using OrzioClashReport.Launcher.Application.Localization;
using OrzioClashReport.Launcher.Application.Operations;
using OrzioClashReport.Launcher.Application.Presentation;
using OrzioClashReport.Launcher.Contracts.Diagnostics;
using OrzioClashReport.Launcher.Contracts.Operations;
using OrzioClashReport.Launcher.Contracts.Results;
using OrzioClashReport.Launcher.Contracts.Settings;
using OrzioClashReport.Launcher.Infrastructure.Storage;

namespace OrzioClashReport.Launcher.Tests
{
    /// <summary>
    /// Holds the interface language for one test and puts it back afterwards. The language is a
    /// process-wide fact and every other test in this suite is written against Portuguese.
    /// </summary>
    internal sealed class LanguageScope : IDisposable
    {
        private readonly UiLanguage _previous;

        public LanguageScope(UiLanguage language)
        {
            _previous = Loc.Language;
            Loc.Language = language;
        }

        public void Dispose() => Loc.Language = _previous;
    }

    /// <summary>
    /// The interface is bilingual: every sentence has a key and exactly two translations. These tests make
    /// a missing, mistyped, untranslated or stray string a failing build instead of a blank label on an
    /// evaluator's machine.
    /// </summary>
    public sealed class LocalizationTests
    {
        private static readonly Regex Placeholder = new Regex(@"\{(\d+)\}", RegexOptions.Compiled);

        // Keys written as a literal at a call that looks one up.
        private static readonly Regex CodeKeyCall = new Regex(
            @"(?:Loc\.T|SetStatus|ShowFollowUp|AddNote|Fail)\(\s*""([^""]+)""", RegexOptions.Compiled);

        private static readonly Regex WatermarkKey = new Regex(
            @"watermarkKey:\s*""([^""]+)""", RegexOptions.Compiled);

        // The operation forms pass their keys as constructor arguments.
        private static readonly Regex FormKey = new Regex(@"""(op\.[A-Za-z0-9.]+)""", RegexOptions.Compiled);

        // The update flow returns its error keys.
        private static readonly Regex UpdateKey = new Regex(
            @"""(updates\.(?:error|status|sidebar)\.[A-Za-z]+)""", RegexOptions.Compiled);

        private static readonly Regex XamlKey = new Regex(@"\{loc:Loc\s+([^}\s]+)\}", RegexOptions.Compiled);

        private static readonly Regex XamlLiteral = new Regex(
            @"\b(Text|Content|Header|Watermark|ToolTip\.Tip|Title)=""([^""{][^""]*)""",
            RegexOptions.Compiled | RegexOptions.Singleline);

        [Fact]
        public void EveryKeyHasBothTranslations()
        {
            Assert.NotEmpty(Loc.AllKeys);

            foreach (string key in Loc.AllKeys)
            {
                string portuguese = Loc.T(key, UiLanguage.Portuguese);
                string english = Loc.T(key, UiLanguage.English);

                Assert.False(string.IsNullOrWhiteSpace(portuguese), $"'{key}' has no Portuguese text.");
                Assert.False(string.IsNullOrWhiteSpace(english), $"'{key}' has no English text.");
            }
        }

        [Fact]
        public void BothTranslationsCarryTheSamePlaceholders()
        {
            foreach (string key in Loc.AllKeys)
            {
                string[] portuguese = PlaceholdersIn(Loc.T(key, UiLanguage.Portuguese));
                string[] english = PlaceholdersIn(Loc.T(key, UiLanguage.English));

                Assert.True(
                    portuguese.SequenceEqual(english),
                    $"'{key}' uses different placeholders in Portuguese ({string.Join(",", portuguese)}) "
                    + $"and English ({string.Join(",", english)}).");
            }
        }

        [Fact]
        public void ALongSentenceIsNeverLeftUntranslated()
        {
            // A copy-and-paste of the Portuguese into the English column is the most likely mistake in a
            // table like this. Short labels may legitimately be identical ("Snapshots", "Longitudinal").
            foreach (string key in Loc.AllKeys)
            {
                string portuguese = Loc.T(key, UiLanguage.Portuguese);
                string english = Loc.T(key, UiLanguage.English);

                int words = portuguese.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries).Length;
                if (words >= 4)
                {
                    Assert.NotEqual(portuguese, english);
                }
            }
        }

        [Fact]
        public void AMissingKeyShowsTheKeyRatherThanABlankLabel()
        {
            Assert.Equal("no.such.key", Loc.T("no.such.key"));
            Assert.False(Loc.IsDefined("no.such.key"));
        }

        [Fact]
        public void EveryKeyUsedInCodeIsDefined()
        {
            var missing = new List<string>();

            foreach (string file in SourceFiles("*.cs"))
            {
                string text = File.ReadAllText(file);

                foreach (Match match in CodeKeyCall.Matches(text))
                {
                    CollectIfLooksLikeAKey(match.Groups[1].Value, file, missing);
                }

                foreach (Match match in WatermarkKey.Matches(text))
                {
                    CollectIfLooksLikeAKey(match.Groups[1].Value, file, missing);
                }

                foreach (Match match in UpdateKey.Matches(text))
                {
                    CollectIfLooksLikeAKey(match.Groups[1].Value, file, missing);
                }

                if (file.Replace('\\', '/').Contains("/ViewModels/Operations/"))
                {
                    foreach (Match match in FormKey.Matches(text))
                    {
                        CollectIfLooksLikeAKey(match.Groups[1].Value, file, missing);
                    }
                }
            }

            Assert.True(missing.Count == 0, "Undefined keys: " + string.Join("; ", missing));
        }

        [Fact]
        public void EveryKeyUsedInAViewIsDefined()
        {
            var missing = new List<string>();

            foreach (string file in SourceFiles("*.axaml"))
            {
                foreach (Match match in XamlKey.Matches(File.ReadAllText(file)))
                {
                    string key = match.Groups[1].Value;
                    if (!Loc.IsDefined(key))
                    {
                        missing.Add(Path.GetFileName(file) + ": " + key);
                    }
                }
            }

            Assert.True(missing.Count == 0, "Undefined keys in views: " + string.Join("; ", missing));
        }

        [Fact]
        public void NoViewCarriesASentenceOfItsOwn()
        {
            var offenders = new List<string>();

            foreach (string file in SourceFiles("*.axaml"))
            {
                foreach (Match match in XamlLiteral.Matches(File.ReadAllText(file)))
                {
                    string value = match.Groups[2].Value.Trim();

                    // Glyphs, separators and the product name are not sentences.
                    bool hasLetters = value.Any(char.IsLetter);
                    bool isBrand = value == "Orzio Clash Report" || value == "OC";
                    bool isGlyph = value.Length <= 2;

                    if (hasLetters && !isBrand && !isGlyph)
                    {
                        offenders.Add(Path.GetFileName(file) + ": " + match.Groups[1].Value + "=\"" + value + "\"");
                    }
                }
            }

            Assert.True(
                offenders.Count == 0,
                "A view must use {loc:Loc key}, not literal text: " + string.Join("; ", offenders));
        }

        [Fact]
        public void EveryKeyIsReferencedSomewhere()
        {
            // A dead entry is a translation nobody will ever read, and it hides the day a real one is
            // renamed. Keys built from a prefix are matched by the prefix.
            string everything = string.Join(
                "\n",
                SourceFiles("*.cs").Where(file => !IsTranslationTable(file)).Select(File.ReadAllText)
                    .Concat(SourceFiles("*.axaml").Select(File.ReadAllText)));

            var unused = new List<string>();
            foreach (string key in Loc.AllKeys)
            {
                if (everything.Contains(key, StringComparison.Ordinal))
                {
                    continue;
                }

                // Section, engine-status, decision and diagnostic keys are composed from a prefix.
                int lastDot = key.LastIndexOf('.');
                string prefix = key.Substring(0, lastDot);

                bool composed =
                    (prefix.StartsWith("nav.", StringComparison.Ordinal) && everything.Contains("\"" + prefix + "\""))
                    || (prefix.StartsWith("diagnostics.item", StringComparison.Ordinal)
                        && everything.Contains("\"diagnostics.item.\""))
                    || (prefix.StartsWith("engine.", StringComparison.Ordinal)
                        && everything.Contains("engine." + prefix.Substring("engine.".Length)))
                    || (prefix.StartsWith("decision.", StringComparison.Ordinal)
                        && everything.Contains("\"" + key + "\""));

                if (!composed)
                {
                    unused.Add(key);
                }
            }

            Assert.True(unused.Count == 0, "Keys defined but never used: " + string.Join(", ", unused));
        }

        [Fact]
        public void NoCanonicalEngineValueIsEverTranslated()
        {
            // The language changes visible text only. What the engine receives is identical in both.
            foreach (IdentityDecisionKind kind in Enum.GetValues<IdentityDecisionKind>())
            {
                foreach (string key in Loc.AllKeys)
                {
                    Assert.DoesNotContain(kind.ToString(), Loc.T(key, UiLanguage.Portuguese));
                    Assert.DoesNotContain(kind.ToString(), Loc.T(key, UiLanguage.English));
                }
            }
        }

        [Fact]
        public void TheCanonicalDecisionValuesAreTheSameInBothLanguages()
        {
            foreach (UiLanguage language in Enum.GetValues<UiLanguage>())
            {
                using (new LanguageScope(language))
                {
                    Assert.Equal(
                        "ConfirmSameIdentity",
                        IdentityDecisionPresentation.For(IdentityDecisionKind.ConfirmSameIdentity).CanonicalValue);
                    Assert.Equal(
                        "RejectSameIdentity",
                        IdentityDecisionPresentation.For(IdentityDecisionKind.RejectSameIdentity).CanonicalValue);
                }
            }
        }

        [Fact]
        public void EveryDiagnosticBundleFileHasADescriptionInBothLanguages()
        {
            foreach (DiagnosticBundleItem item in DiagnosticBundleItem.All)
            {
                Assert.True(
                    Loc.IsDefined("diagnostics.item." + item.FileName),
                    $"The diagnostic file '{item.FileName}' has no translation.");
            }
        }

        [Fact]
        public void EverySectionEngineStateAndDecisionIsTranslated()
        {
            foreach (UiLanguage language in Enum.GetValues<UiLanguage>())
            {
                using (new LanguageScope(language))
                {
                    foreach (LauncherSectionPresentation section in LauncherSectionPresentation.All)
                    {
                        Assert.True(Loc.IsDefined(KeyOf(section.Section) + ".label"), section.Section.ToString());
                        Assert.NotEqual(KeyOf(section.Section) + ".label", section.Label);
                        Assert.NotEqual(KeyOf(section.Section) + ".description", section.Description);
                    }

                    foreach (OrzioClashReport.Launcher.Contracts.Engine.EngineStatusKind status
                        in Enum.GetValues<OrzioClashReport.Launcher.Contracts.Engine.EngineStatusKind>())
                    {
                        EngineStatusPresentation presentation = EngineStatusPresentation.For(status);
                        Assert.False(presentation.Label.StartsWith("engine.", StringComparison.Ordinal), status.ToString());
                        Assert.False(presentation.Explanation.StartsWith("engine.", StringComparison.Ordinal), status.ToString());
                    }

                    foreach (IdentityDecisionKind kind in Enum.GetValues<IdentityDecisionKind>())
                    {
                        IdentityDecisionPresentation presentation = IdentityDecisionPresentation.For(kind);
                        Assert.False(presentation.Label.StartsWith("decision.", StringComparison.Ordinal), kind.ToString());
                        Assert.False(presentation.Description.StartsWith("decision.", StringComparison.Ordinal), kind.ToString());
                    }
                }
            }
        }

        [Fact]
        public void TheSectionLabelsFollowTheLanguage()
        {
            using (new LanguageScope(UiLanguage.Portuguese))
            {
                Assert.Equal(
                    new[] { "Início", "Relatório rápido", "Snapshots", "Longitudinal", "Projetos", "Governança", "Definições" },
                    LauncherSectionPresentation.All.Select(section => section.Label));
            }

            using (new LanguageScope(UiLanguage.English))
            {
                Assert.Equal(
                    new[] { "Home", "Quick report", "Snapshots", "Longitudinal", "Projects", "Governance", "Settings" },
                    LauncherSectionPresentation.All.Select(section => section.Label));
            }
        }

        [Fact]
        public void TheEngineBadgeFollowsTheLanguage()
        {
            var ready = OrzioClashReport.Launcher.Contracts.Engine.EngineStatusKind.Ready;

            using (new LanguageScope(UiLanguage.Portuguese))
            {
                Assert.Equal("Motor pronto", EngineStatusPresentation.For(ready).Label);
            }

            using (new LanguageScope(UiLanguage.English))
            {
                Assert.Equal("Engine ready", EngineStatusPresentation.For(ready).Label);
            }
        }

        [Fact]
        public void ChangingTheLanguageRaisesOneNotificationAndSettingItAgainRaisesNone()
        {
            using (new LanguageScope(UiLanguage.Portuguese))
            {
                int raised = 0;
                Action handler = () => raised++;
                Loc.Changed += handler;

                try
                {
                    Loc.Language = UiLanguage.English;
                    Loc.Language = UiLanguage.English;
                    Assert.Equal(1, raised);

                    Loc.Language = UiLanguage.Portuguese;
                    Assert.Equal(2, raised);
                }
                finally
                {
                    Loc.Changed -= handler;
                }
            }
        }

        [Theory]
        [InlineData(LauncherLanguagePreference.System, "pt-PT", UiLanguage.Portuguese)]
        [InlineData(LauncherLanguagePreference.System, "pt-BR", UiLanguage.Portuguese)]
        [InlineData(LauncherLanguagePreference.System, "pt", UiLanguage.Portuguese)]
        [InlineData(LauncherLanguagePreference.System, "en-US", UiLanguage.English)]
        [InlineData(LauncherLanguagePreference.System, "en-GB", UiLanguage.English)]
        [InlineData(LauncherLanguagePreference.System, "de-DE", UiLanguage.English)]
        [InlineData(LauncherLanguagePreference.English, "pt-PT", UiLanguage.English)]
        [InlineData(LauncherLanguagePreference.Portuguese, "en-US", UiLanguage.Portuguese)]
        public void ThePreferenceResolvesToAConcreteLanguage(
            LauncherLanguagePreference preference, string culture, UiLanguage expected)
        {
            Assert.Equal(expected, Loc.Resolve(preference, new CultureInfo(culture)));
        }

        [Fact]
        public void ARecordedDecisionIsRefusedInTheActiveLanguage()
        {
            using (new LanguageScope(UiLanguage.English))
            {
                ArgumentException exception = Assert.Throws<ArgumentException>(() => new IdentityDecisionDraft(
                    IdentityDecisionKind.RejectSameIdentity,
                    "d-1", "run-1", 0, "run-2", 0, "alias", "clash-1", null));

                Assert.Contains("rejection can never carry a persistent identifier", exception.Message);
            }

            using (new LanguageScope(UiLanguage.Portuguese))
            {
                ArgumentException exception = Assert.Throws<ArgumentException>(() => new IdentityDecisionDraft(
                    IdentityDecisionKind.RejectSameIdentity,
                    "d-1", "run-1", 0, "run-2", 0, "alias", "clash-1", null));

                Assert.Contains("rejeição nunca pode transportar", exception.Message);
            }
        }

        [Fact]
        public void ARepeatedFileIsReportedInTheActiveLanguageAndStillKept()
        {
            string[] paths = { "/work/a.json", "/work/b.json", "/work/a.json" };

            using (new LanguageScope(UiLanguage.English))
            {
                var list = new OrderedFileList();
                list.AddRange(paths);

                Assert.Equal(3, list.Count);
                LauncherWarning warning = Assert.Single(list.Warnings());
                Assert.Contains("Position 3 repeats position 1", warning.Message);
            }
        }

        [Fact]
        public async Task TheLanguageChoiceIsStoredAndReadBack()
        {
            string path = TempSettingsPath();
            var store = new JsonSettingsStore(path);

            await store.SaveAsync(
                LauncherSettings.Default.WithLanguage(LauncherLanguagePreference.English), CancellationToken.None);

            LauncherSettings loaded = await new JsonSettingsStore(path).LoadAsync(CancellationToken.None);

            Assert.Equal(LauncherLanguagePreference.English, loaded.Language);
            Assert.Contains("\"language\": \"English\"", File.ReadAllText(path));
        }

        [Fact]
        public async Task ASettingsFileWrittenBeforeTheLanguageExistedKeepsFollowingTheSystem()
        {
            string path = TempSettingsPath();
            File.WriteAllText(
                path,
                "{ \"schemaVersion\": 1, \"theme\": \"Dark\", \"lastOutputDirectory\": null, \"showExperimentalWarnings\": false }");

            LauncherSettings loaded = await new JsonSettingsStore(path).LoadAsync(CancellationToken.None);

            Assert.Equal(LauncherLanguagePreference.System, loaded.Language);
            Assert.Equal(LauncherThemePreference.Dark, loaded.Theme);
            Assert.False(loaded.ShowExperimentalWarnings);
        }

        [Theory]
        [InlineData("\"Klingon\"")]
        [InlineData("\"english\"")]
        [InlineData("42")]
        [InlineData("null")]
        public async Task AnUnknownLanguageValueFallsBackToTheSystemAndNeverStopsTheApplication(string value)
        {
            string path = TempSettingsPath();
            File.WriteAllText(path, "{ \"schemaVersion\": 1, \"language\": " + value + " }");

            LauncherSettings loaded = await new JsonSettingsStore(path).LoadAsync(CancellationToken.None);

            Assert.Equal(LauncherLanguagePreference.System, loaded.Language);
        }

        [Fact]
        public void ChangingAnotherSettingKeepsTheChosenLanguage()
        {
            LauncherSettings settings = LauncherSettings.Default
                .WithLanguage(LauncherLanguagePreference.English)
                .WithTheme(LauncherThemePreference.Dark)
                .WithLastOutputDirectory("/reports")
                .WithShowExperimentalWarnings(false);

            Assert.Equal(LauncherLanguagePreference.English, settings.Language);
        }

        private static string[] PlaceholdersIn(string text) =>
            Placeholder.Matches(text).Select(match => match.Groups[1].Value).Distinct().OrderBy(x => x).ToArray();

        private static string KeyOf(LauncherSection section) => "nav." + section.ToString().ToLowerInvariant();

        private static bool IsTranslationTable(string file) =>
            file.Replace('\\', '/').Contains("/Localization/UiStrings");

        private static void CollectIfLooksLikeAKey(string candidate, string file, List<string> missing)
        {
            // A literal passed to one of these calls that is not shaped like a key (no dot) is some
            // other value, such as a file name.
            if (candidate.IndexOf('.') < 0)
            {
                return;
            }

            // A prefix that is completed at run time ("nav.home" + ".label") is checked where it is composed.
            bool isPrefix = Loc.AllKeys.Any(key => key.StartsWith(candidate + ".", StringComparison.Ordinal));

            if (!Loc.IsDefined(candidate) && !isPrefix)
            {
                missing.Add(Path.GetFileName(file) + ": " + candidate);
            }
        }

        private static IEnumerable<string> SourceFiles(string pattern) =>
            Directory.GetFiles(
                    Path.Combine(RepositoryLayout.RootDirectory, "src"), pattern, SearchOption.AllDirectories)
                .Where(file =>
                {
                    string normalized = file.Replace('\\', '/');
                    return !normalized.Contains("/obj/") && !normalized.Contains("/bin/")
                        && normalized.Contains("/OrzioClashReport.Launcher.");
                });

        private static string TempSettingsPath()
        {
            string directory = Path.Combine(Path.GetTempPath(), "orzio-loc-tests-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(directory);
            return Path.Combine(directory, "settings.json");
        }
    }
}
