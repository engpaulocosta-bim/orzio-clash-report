using System.Reflection;
using System.Text.RegularExpressions;
using OrzioClashReport.Launcher.Contracts.Engine;
using OrzioClashReport.Launcher.Contracts.Jobs;
using OrzioClashReport.Launcher.Contracts.Operations;
using OrzioClashReport.Launcher.Contracts.Settings;
using OrzioClashReport.Launcher.Desktop.Localization;
using OrzioClashReport.Launcher.Desktop.ViewModels;

namespace OrzioClashReport.Launcher.Tests;

/// <summary>
/// The interface is available in Portuguese and English. Every visible string comes from one table
/// with both translations; nothing the engine receives is ever translated.
/// </summary>
public sealed class LocalizationTests
{
    [Fact]
    public void EveryKeyHasBothTranslations()
    {
        Assert.NotEmpty(UiStrings.All);

        foreach (KeyValuePair<string, UiText> entry in UiStrings.All)
        {
            Assert.False(
                string.IsNullOrWhiteSpace(entry.Value.Portuguese),
                $"'{entry.Key}' has no Portuguese text.");
            Assert.False(
                string.IsNullOrWhiteSpace(entry.Value.English),
                $"'{entry.Key}' has no English text.");
        }
    }

    [Fact]
    public void NoTranslationIsAccidentallyTheOtherLanguagesPlaceholder()
    {
        // A handful of terms are the same word in both languages; anything else being identical is
        // almost always an untranslated string that slipped through.
        string[] intentionallyIdentical =
        {
            "App.Title", "Shell.Brand", "Nav.Snapshots", "Nav.Longitudinal",
            "Section.Snapshots.Title", "Section.Longitudinal.Title",
            "Field.Manifest.Label", "Field.Snapshot.Label",
            "Picker.RunManifestJson", "Picker.SnapshotJson", "Picker.RunIndexJson",
        };

        var identical = UiStrings.All
            .Where(entry => string.Equals(entry.Value.Portuguese, entry.Value.English, StringComparison.Ordinal))
            .Select(entry => entry.Key)
            .Where(key => !intentionallyIdentical.Contains(key))
            .OrderBy(key => key, StringComparer.Ordinal)
            .ToArray();

        Assert.Empty(identical);
    }

    [Fact]
    public void FormatStringsUseTheSamePlaceholdersInBothLanguages()
    {
        var placeholder = new Regex(@"\{\d+\}");

        foreach (KeyValuePair<string, UiText> entry in UiStrings.All)
        {
            string[] portuguese = placeholder.Matches(entry.Value.Portuguese)
                .Select(match => match.Value).OrderBy(value => value, StringComparer.Ordinal).ToArray();
            string[] english = placeholder.Matches(entry.Value.English)
                .Select(match => match.Value).OrderBy(value => value, StringComparer.Ordinal).ToArray();

            Assert.True(
                portuguese.SequenceEqual(english),
                $"'{entry.Key}' uses different placeholders in each language.");
        }
    }

    [Theory]
    [InlineData(InterfaceLanguage.Portuguese, "Início")]
    [InlineData(InterfaceLanguage.English, "Home")]
    public void TheLanguageChoiceDecidesWhichTextIsReturned(InterfaceLanguage language, string expected)
    {
        using LanguageScope scope = LanguageScope.Use(language);

        Assert.Equal(expected, Localizer.Instance.Get("Nav.Home"));
        Assert.Equal(expected, Localizer.Instance["Nav.Home"]);
    }

    [Fact]
    public void FollowingTheSystemNeverResolvesToItself()
    {
        using LanguageScope scope = LanguageScope.Use(InterfaceLanguage.System);

        Assert.Equal(InterfaceLanguage.System, Localizer.Instance.Language);
        Assert.NotEqual(InterfaceLanguage.System, Localizer.Instance.Resolved);
    }

    [Fact]
    public void AnUnknownKeyIsADefectRatherThanAnEmptyLabel()
    {
        Assert.Throws<KeyNotFoundException>(() => Localizer.Instance.Get("No.Such.Key"));
    }

    [Fact]
    public async Task ChangingTheLanguageRefreshesWhatIsAlreadyOnScreen()
    {
        var engine = new EngineStatusViewModel(new StubEngineProbe(EngineProbeResult.Ready("0.1.0-preview.3")));
        await engine.RefreshAsync(CancellationToken.None);

        var changed = new List<string?>();
        engine.PropertyChanged += (_, e) => changed.Add(e.PropertyName);

        using (LanguageScope.Use(InterfaceLanguage.Portuguese))
        {
            Assert.Equal("Motor pronto", engine.Label);

            using (LanguageScope.Use(InterfaceLanguage.English))
            {
                Assert.Equal("Engine ready", engine.Label);
            }
        }

        // An empty name is the "re-read everything" signal, which is what the bindings need.
        Assert.Contains(changed, name => string.IsNullOrEmpty(name));
    }

    [Fact]
    public void EveryEngineStateReadsInBothLanguages()
    {
        foreach (EngineStatus status in Enum.GetValues<EngineStatus>())
        {
            using (LanguageScope.Use(InterfaceLanguage.Portuguese))
            {
                Assert.False(string.IsNullOrWhiteSpace(EngineStatusViewModel.LabelFor(status)));
            }

            using (LanguageScope.Use(InterfaceLanguage.English))
            {
                Assert.False(string.IsNullOrWhiteSpace(EngineStatusViewModel.LabelFor(status)));
            }
        }
    }

    [Fact]
    public void EveryOperationAndErrorAndWarningHasTextInBothLanguages()
    {
        foreach (LauncherOperationKind kind in Enum.GetValues<LauncherOperationKind>())
        {
            AssertKeyExists(InterruptedJobViewModel.LabelKeyFor(kind));
        }

        foreach (LauncherErrorCode code in Enum.GetValues<LauncherErrorCode>())
        {
            AssertKeyExists(OperationRunnerViewModel.MessageKeyFor(code));
        }

        foreach (LauncherWarningCode code in Enum.GetValues<LauncherWarningCode>())
        {
            AssertKeyExists(OperationRunnerViewModel.MessageKeyFor(code));
        }
    }

    [Fact]
    public void TheCanonicalEngineValuesAreNotInTheTranslationTable()
    {
        // Whatever the interface language, these reach the engine unchanged.
        string[] canonical =
        {
            "ConfirmSameIdentity",
            "RejectSameIdentity",
            "--decision-kind",
            "--persistent-identity-id",
            "orzioclash",
        };

        foreach (KeyValuePair<string, UiText> entry in UiStrings.All)
        {
            foreach (string value in canonical)
            {
                Assert.DoesNotContain(value, entry.Value.Portuguese, StringComparison.Ordinal);
                Assert.DoesNotContain(value, entry.Value.English, StringComparison.Ordinal);
            }
        }
    }

    [Fact]
    public void TheDecisionLabelsDifferInBothLanguagesWithoutChangingTheCanonicalValue()
    {
        foreach (InterfaceLanguage language in new[] { InterfaceLanguage.Portuguese, InterfaceLanguage.English })
        {
            using LanguageScope scope = LanguageScope.Use(language);

            Assert.NotEqual(
                DecisionKindOptionViewModel.Confirm.Label, DecisionKindOptionViewModel.Reject.Label);
            Assert.NotEqual(
                DecisionKindOptionViewModel.Confirm.Explanation,
                DecisionKindOptionViewModel.Reject.Explanation);
        }

        Assert.Equal(HumanIdentityDecisionKind.ConfirmSameIdentity, DecisionKindOptionViewModel.Confirm.Kind);
        Assert.Equal(HumanIdentityDecisionKind.RejectSameIdentity, DecisionKindOptionViewModel.Reject.Kind);
    }

    [Fact]
    public void TheLanguageIsAStoredSettingThatDefaultsToFollowingTheSystem()
    {
        Assert.Equal(InterfaceLanguage.System, LauncherSettings.Default.Language);
        Assert.Equal(
            InterfaceLanguage.English,
            LauncherSettings.Default.WithLanguage(InterfaceLanguage.English).Language);

        // Changing the language leaves every other setting alone.
        LauncherSettings settings = LauncherSettings.Default
            .WithTheme(ThemePreference.Dark)
            .WithShowExperimentalWarnings(false)
            .WithLanguage(InterfaceLanguage.Portuguese);

        Assert.Equal(ThemePreference.Dark, settings.Theme);
        Assert.False(settings.ShowExperimentalWarnings);
        Assert.Equal(InterfaceLanguage.Portuguese, settings.Language);
    }

    [Fact]
    public void EveryKeyUsedByAViewExists()
    {
        string desktopRoot = Path.Combine(RepositoryPaths.SourceRoot, "OrzioClashReport.Launcher.Desktop");
        var pattern = new Regex(@"\{l:Loc\s+(?<key>[A-Za-z0-9.]+)\s*\}");
        var used = new List<string>();

        foreach (string file in Directory.GetFiles(desktopRoot, "*.axaml", SearchOption.AllDirectories))
        {
            foreach (Match match in pattern.Matches(File.ReadAllText(file)))
            {
                used.Add(match.Groups["key"].Value);
            }
        }

        Assert.NotEmpty(used);
        foreach (string key in used.Distinct())
        {
            AssertKeyExists(key);
        }
    }

    [Fact]
    public void EveryKeyUsedByAViewModelExists()
    {
        string viewModels = Path.Combine(
            RepositoryPaths.SourceRoot, "OrzioClashReport.Launcher.Desktop", "ViewModels");
        var pattern = new Regex(@"Text\(""(?<key>[A-Za-z0-9.]+)""");
        var used = new List<string>();

        foreach (string file in Directory.GetFiles(viewModels, "*.cs", SearchOption.AllDirectories))
        {
            foreach (Match match in pattern.Matches(File.ReadAllText(file)))
            {
                used.Add(match.Groups["key"].Value);
            }
        }

        Assert.NotEmpty(used);
        foreach (string key in used.Distinct())
        {
            AssertKeyExists(key);
        }
    }

    [Fact]
    public void NoInterfaceTextIsWrittenOutsideTheTranslationTable()
    {
        // A view model may hold a key; it may not hold a sentence.
        string viewModels = Path.Combine(
            RepositoryPaths.SourceRoot, "OrzioClashReport.Launcher.Desktop", "ViewModels");
        var accented = new Regex(@"""[^""]*[çãõáéíóúâêôàÇÃÕÁÉÍÓÚÂÊÔÀ][^""]*""");

        foreach (string file in Directory.GetFiles(viewModels, "*.cs", SearchOption.AllDirectories))
        {
            string content = File.ReadAllText(file);
            foreach (Match match in accented.Matches(content))
            {
                Assert.Fail(
                    $"{Path.GetFileName(file)} contains the literal {match.Value}. "
                        + "Interface text belongs in Localization/UiStrings.cs.");
            }
        }
    }

    [Fact]
    public void TheLocalizerDoesNotKeepViewModelsAlive()
    {
        int before = SubscriberCount();

        CreateAndDrop();
        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();

        // Notifying purges the references that died, so the list cannot grow without bound.
        Localizer.Instance.Language = InterfaceLanguage.Portuguese;
        Localizer.Instance.Language = InterfaceLanguage.System;

        Assert.True(
            SubscriberCount() <= before + 8,
            $"Subscribers grew from {before} to {SubscriberCount()}; dead view models are being retained.");
    }

    private static void CreateAndDrop()
    {
        for (int i = 0; i < 200; i++)
        {
            _ = new SectionPlaceholderViewModel("Nav.Home", "App.Tagline");
        }
    }

    private static int SubscriberCount()
    {
        FieldInfo field = typeof(Localizer).GetField(
            "_subscribers", BindingFlags.Instance | BindingFlags.NonPublic)!;
        var subscribers = (System.Collections.ICollection)field.GetValue(Localizer.Instance)!;
        return subscribers.Count;
    }

    private static void AssertKeyExists(string key)
    {
        Assert.True(UiStrings.All.ContainsKey(key), $"No interface text is defined for '{key}'.");
    }
}
