using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.Threading;
using Avalonia.VisualTree;
using OrzioClashReport.Launcher.Application.Localization;
using OrzioClashReport.Launcher.Contracts.Operations;
using OrzioClashReport.Launcher.Contracts.Settings;
using OrzioClashReport.Launcher.Desktop.Localization;
using OrzioClashReport.Launcher.Desktop.ViewModels;
using OrzioClashReport.Launcher.Desktop.ViewModels.Fields;
using OrzioClashReport.Launcher.Desktop.ViewModels.Operations;
using OrzioClashReport.Launcher.Desktop.Views;
using OrzioClashReport.Launcher.Application.Presentation;

namespace OrzioClashReport.Launcher.Desktop.Tests
{
    /// <summary>Holds the interface language for one test and puts it back afterwards.</summary>
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
    /// Proves the interface really follows the language while it is on screen: the XAML bindings, the
    /// computed view-model text, and the choice the user makes in Settings. A binding that looked right
    /// in code but never refreshed would only show up here.
    /// </summary>
    public sealed class LocalizationSmokeTests
    {
        [AvaloniaFact]
        public void TheShellFollowsTheLanguageWithoutBeingRebuilt()
        {
            using (new LanguageScope(UiLanguage.Portuguese))
            {
                ShellViewModel shell = ShellSmokeTests.CreateShell(out _);
                var window = new MainWindow { DataContext = shell };
                window.Show();

                IReadOnlyList<string> portuguese = TextsIn(window);
                Assert.Contains("Início", portuguese);
                Assert.Contains("Definições", portuguese);
                Assert.Contains("Procurar atualizações", portuguese);
                Assert.Contains("A verificar", portuguese);

                Loc.Language = UiLanguage.English;
                Dispatcher.UIThread.RunJobs();

                IReadOnlyList<string> english = TextsIn(window);
                Assert.Contains("Home", english);
                Assert.Contains("Quick report", english);
                Assert.Contains("Settings", english);
                Assert.Contains("Check for updates", english);
                Assert.Contains("Checking", english);
                Assert.DoesNotContain("Início", english);
                Assert.DoesNotContain("Definições", english);

                // The status bar shows the selected section's description, which also follows.
                Assert.Contains("Quick actions, engine status and latest reports.", english);

                Loc.Language = UiLanguage.Portuguese;
                Dispatcher.UIThread.RunJobs();

                Assert.Contains("Início", TextsIn(window));
            }
        }

        [AvaloniaFact]
        public void EverySectionRendersInEnglish()
        {
            using (new LanguageScope(UiLanguage.English))
            {
                ShellViewModel shell = ShellSmokeTests.CreateShell(out _);
                var window = new MainWindow { DataContext = shell };
                window.Show();

                foreach (LauncherSection section in Enum.GetValues<LauncherSection>())
                {
                    shell.Navigate(section);
                    Dispatcher.UIThread.RunJobs();

                    Assert.NotNull(shell.SelectedSection!.Content);

                    // No raw key may reach the screen: an undefined key is shown as itself.
                    foreach (string text in TextsIn(window))
                    {
                        Assert.False(LooksLikeAKey(text), $"A key reached the screen in '{section}': {text}");
                    }
                }
            }
        }

        [AvaloniaFact]
        public void SettingsFollowTheLanguageIncludingItsOwnPickers()
        {
            using (new LanguageScope(UiLanguage.Portuguese))
            {
                ShellViewModel shell = ShellSmokeTests.CreateShell(out _);
                var window = new MainWindow { DataContext = shell };
                window.Show();
                shell.Navigate(LauncherSection.Settings);
                Dispatcher.UIThread.RunJobs();

                IReadOnlyList<string> portuguese = TextsIn(window);
                Assert.Contains("Aparência", portuguese);
                Assert.Contains("Idioma", portuguese);
                Assert.Contains("Tema", portuguese);
                Assert.Contains("Dados locais", portuguese);
                Assert.Contains("Sistema (automático)", portuguese);

                Loc.Language = UiLanguage.English;
                Dispatcher.UIThread.RunJobs();

                IReadOnlyList<string> english = TextsIn(window);
                Assert.Contains("Appearance", english);
                Assert.Contains("Language", english);
                Assert.Contains("Theme", english);
                Assert.Contains("Local data", english);
                Assert.Contains("System (automatic)", english);
                Assert.DoesNotContain("Aparência", english);
            }
        }

        [AvaloniaFact]
        public void ChoosingALanguageAppliesItAtOnceAndStoresIt()
        {
            using (new LanguageScope(UiLanguage.Portuguese))
            {
                SettingsViewModel settings = CreateSettings(out InMemorySettingsStore store);

                settings.Language = LauncherLanguagePreference.English;

                Assert.Equal(UiLanguage.English, Loc.Language);
                Assert.Equal(LauncherLanguagePreference.English, store.LoadAsync(CancellationToken.None).Result.Language);
                Assert.Equal("English", settings.SelectedLanguageOption.Label);

                settings.Language = LauncherLanguagePreference.Portuguese;

                Assert.Equal(UiLanguage.Portuguese, Loc.Language);
                Assert.Equal(
                    LauncherLanguagePreference.Portuguese, store.LoadAsync(CancellationToken.None).Result.Language);
            }
        }

        [AvaloniaFact]
        public async Task LoadingTheStoredLanguageAppliesItWithoutWritingTheFileBack()
        {
            using (new LanguageScope(UiLanguage.Portuguese))
            {
                SettingsViewModel settings = CreateSettings(out InMemorySettingsStore store);
                await store.SaveAsync(
                    LauncherSettings.Default.WithLanguage(LauncherLanguagePreference.English), CancellationToken.None);
                int savesBefore = store.SaveCount;

                await settings.LoadAsync(CancellationToken.None);

                Assert.Equal(UiLanguage.English, Loc.Language);
                Assert.Equal(LauncherLanguagePreference.English, settings.Language);
                Assert.Equal(savesBefore, store.SaveCount);
            }
        }

        [AvaloniaFact]
        public void ThePickersListEveryChoiceAndSelectTheCurrentOne()
        {
            using (new LanguageScope(UiLanguage.English))
            {
                SettingsViewModel settings = CreateSettings(out _);

                Assert.Equal(
                    new[] { "System (automatic)", "Português", "English" },
                    settings.LanguageOptions.Select(option => option.Label));
                Assert.Equal(
                    new[] { "System", "Light", "Dark" },
                    settings.ThemeOptions.Select(option => option.Label));

                Assert.Equal(LauncherLanguagePreference.System, settings.SelectedLanguageOption.Value);
                Assert.Equal(LauncherThemePreference.System, settings.SelectedThemeOption.Value);

                settings.SelectedThemeOption = settings.ThemeOptions[2];
                Assert.Equal(LauncherThemePreference.Dark, settings.Theme);
            }
        }

        [AvaloniaFact]
        public void AViewModelAnnouncesALanguageChangeSoItsTextIsReadAgain()
        {
            using (new LanguageScope(UiLanguage.Portuguese))
            {
                OperationFormViewModel form = OperationFormSmokeTests.AllForms()[0];
                var changed = new List<string?>();
                form.PropertyChanged += (_, e) => changed.Add(e.PropertyName);

                string before = form.Title;
                Loc.Language = UiLanguage.English;

                Assert.Contains(string.Empty, changed);
                Assert.NotEqual(before, form.Title);
                Assert.Equal("Create snapshot", form.Title);
            }
        }

        [AvaloniaFact]
        public void EveryFormResolvesAllOfItsTextInBothLanguages()
        {
            foreach (UiLanguage language in Enum.GetValues<UiLanguage>())
            {
                using (new LanguageScope(language))
                {
                    foreach (OperationFormViewModel form in AllTwelveForms())
                    {
                        AssertResolved(form.Title, form.Operation.ToString());
                        AssertResolved(form.Description, form.Operation.ToString());

                        foreach (string note in form.Notes)
                        {
                            AssertResolved(note, form.Operation.ToString());
                        }

                        foreach (OperationFieldViewModel field in form.Fields)
                        {
                            AssertResolved(field.Label, form.Operation.ToString());
                            AssertResolved(field.Description, form.Operation.ToString(), allowEmpty: true);
                        }
                    }
                }
            }
        }

        [AvaloniaFact]
        public void AFormRendersItsFieldsInEnglish()
        {
            using (new LanguageScope(UiLanguage.English))
            {
                OperationFormViewModel form = OperationFormSmokeTests.AllForms()[0];
                var window = new Window { Content = new OperationFormView { DataContext = form } };
                window.Show();

                IReadOnlyList<string> texts = TextsIn(window);

                Assert.Contains("Create snapshot", texts);
                Assert.Contains("XML export", texts);
                Assert.Contains("Run manifest", texts);
                Assert.Contains("Snapshot destination", texts);
                Assert.Contains("Run", texts);
                Assert.Contains("Choose…", texts);
                Assert.Contains("No file chosen", window.GetVisualDescendants().OfType<TextBox>().Select(box => box.Watermark));
            }
        }

        [AvaloniaFact]
        public void TheDecisionOptionsChangeLanguageButNeverTheValueTheEngineReceives()
        {
            AppendIdentityDecisionFormViewModel form = CreateDecisionForm();
            ChoiceFieldViewModel kind = form.Fields.OfType<ChoiceFieldViewModel>().Single();

            using (new LanguageScope(UiLanguage.Portuguese))
            {
                Assert.Equal(
                    new[] { "Confirmar mesma identidade", "Rejeitar mesma identidade" },
                    kind.Options.Select(option => option.Label));
            }

            using (new LanguageScope(UiLanguage.English))
            {
                Assert.Equal(
                    new[] { "Confirm same identity", "Reject same identity" },
                    kind.Options.Select(option => option.Label));

                // Whatever the screen says, the engine is given exactly these two words.
                Assert.Equal(
                    new[] { "ConfirmSameIdentity", "RejectSameIdentity" },
                    kind.Options.Select(option => option.Value));
            }
        }

        [AvaloniaFact]
        public void TheOccurrenceIndexMessageFollowsTheLanguageWhileTheValueIsInvalid()
        {
            using (new LanguageScope(UiLanguage.Portuguese))
            {
                AppendIdentityDecisionFormViewModel form = CreateDecisionForm();
                TextFieldViewModel left = form.Fields.OfType<TextFieldViewModel>()
                    .Single(field => field.Label == "Índice da ocorrência (esquerda)");

                left.Value = "-1";
                Assert.True(left.HasValidationMessage);
                Assert.Equal("Tem de ser um inteiro maior ou igual a zero.", left.ValidationMessage);

                Loc.Language = UiLanguage.English;
                Assert.Equal("Must be an integer greater than or equal to zero.", left.ValidationMessage);
            }
        }

        [AvaloniaFact]
        public void TheAliasExampleIsAWordInTheLanguageAndTheOtherExamplesAreIdentifiers()
        {
            AppendIdentityDecisionFormViewModel form = CreateDecisionForm();
            TextFieldViewModel alias = form.Fields.OfType<TextFieldViewModel>()
                .Single(field => field.Description.Contains("alias", StringComparison.OrdinalIgnoreCase));
            TextFieldViewModel decisionId = form.Fields.OfType<TextFieldViewModel>().First();

            using (new LanguageScope(UiLanguage.Portuguese))
            {
                Assert.Equal("coordenador-a", alias.Watermark);
                Assert.Equal("d-001", decisionId.Watermark);
            }

            using (new LanguageScope(UiLanguage.English))
            {
                Assert.Equal("coordinator-a", alias.Watermark);
                Assert.Equal("d-001", decisionId.Watermark);
            }
        }

        [AvaloniaFact]
        public void TheJobPanelStateFollowsTheLanguage()
        {
            using (new LanguageScope(UiLanguage.Portuguese))
            {
                JobViewModel job = OperationFormSmokeTests.CreateJob();
                job.ShowValidationFailure("x");

                Assert.Equal("Falhou", job.StateLabel);
                Assert.Equal("Reveja os campos do formulário e tente novamente.", job.ErrorNextStep);

                Loc.Language = UiLanguage.English;

                // The state is read again; the message that was produced stays as it was written.
                Assert.Equal("Failed", job.StateLabel);
                Assert.Equal("✕", job.StateGlyph);
                Assert.Equal("Reveja os campos do formulário e tente novamente.", job.ErrorNextStep);

                job.ShowValidationFailure("y");
                Assert.Equal("Review the form fields and try again.", job.ErrorNextStep);
            }
        }

        [AvaloniaFact]
        public void TheUpdateCardFollowsTheLanguage()
        {
            using (new LanguageScope(UiLanguage.Portuguese))
            {
                var updates = new UpdatesViewModel(
                    new FakeDesktopUpdateClient(), new ActiveJobTracker(), new FixedClock(),
                    "0.2.1-launcher-preview.1", _ => { }, () => { });

                Assert.Equal("Ainda não procurou atualizações nesta sessão.", updates.StatusText);
                Assert.Equal("Versão 0.2.1-launcher-preview.1", updates.SidebarStatus);

                Loc.Language = UiLanguage.English;

                Assert.Equal("You have not checked for updates in this session yet.", updates.StatusText);
                Assert.Equal("Version 0.2.1-launcher-preview.1", updates.SidebarStatus);
            }
        }

        [AvaloniaFact]
        public void TheEngineBadgeAndTheIntegrityLineFollowTheLanguage()
        {
            using (new LanguageScope(UiLanguage.Portuguese))
            {
                var badge = new EngineStatusViewModel();
                Assert.Equal("A verificar", badge.Label);

                Loc.Language = UiLanguage.English;
                Assert.Equal("Checking", badge.Label);
                Assert.Equal("Confirming the engine's version and integrity.", badge.Explanation);

                SettingsViewModel settings = CreateSettings(out _);
                Assert.Equal("Integrity not verified yet.", settings.EngineIntegrity);

                Loc.Language = UiLanguage.Portuguese;
                Assert.Equal("Integridade ainda não verificada.", settings.EngineIntegrity);
            }
        }

        [AvaloniaFact]
        public void TheDiagnosticFilesAreDescribedInTheActiveLanguage()
        {
            using (new LanguageScope(UiLanguage.English))
            {
                SettingsViewModel settings = CreateSettings(out _);
                settings.PrepareDiagnosticsCommand.Execute(null);

                Assert.Equal(6, settings.DiagnosticPlan.Count);
                Assert.Equal("This application's version.", settings.DiagnosticPlan[0].Description);
                Assert.Equal("launcher-version.json", settings.DiagnosticPlan[0].FileName);

                Loc.Language = UiLanguage.Portuguese;
                Assert.Equal("A versão desta aplicação.", settings.DiagnosticPlan[0].Description);
                Assert.Equal("launcher-version.json", settings.DiagnosticPlan[0].FileName);
            }
        }

        // ---------------------------------------------------------------- helpers

        private static IReadOnlyList<string> TextsIn(Visual root)
        {
            var texts = new List<string>();

            foreach (Visual visual in root.GetVisualDescendants().OfType<Visual>().Prepend(root))
            {
                switch (visual)
                {
                    case TextBlock block when !string.IsNullOrEmpty(block.Text):
                        texts.Add(block.Text);
                        break;
                }
            }

            return texts;
        }

        private static bool LooksLikeAKey(string text) =>
            text.IndexOf(' ') < 0
            && System.Text.RegularExpressions.Regex.IsMatch(
                text, @"^(nav|engine|decision|shell|home|quick|picker|settings|updates|common|job|form|placeholder|op|failure|probe|draft|orderedFiles|diagnostics)\.[A-Za-z0-9.\-]+$");

        private static void AssertResolved(string text, string where, bool allowEmpty = false)
        {
            if (allowEmpty && text.Length == 0)
            {
                return;
            }

            Assert.False(string.IsNullOrWhiteSpace(text), $"Empty text in {where}.");
            Assert.False(LooksLikeAKey(text), $"A key was shown instead of text in {where}: {text}");
        }

        private static SettingsViewModel CreateSettings(out InMemorySettingsStore store)
        {
            store = new InMemorySettingsStore();
            var engineStatus = new EngineStatusViewModel();
            var updates = new UpdatesViewModel(
                new FakeDesktopUpdateClient(), new ActiveJobTracker(), new FixedClock(),
                "0.2.1-launcher-preview.1", _ => { }, () => { });

            return new SettingsViewModel(
                store,
                new InMemoryRecentItemsStore(),
                new NullOutputRevealer(),
                new StubDiagnosticsBundleBuilder(),
                engineStatus,
                Path.GetTempPath(),
                "0.2.1-launcher-preview.1",
                _ => { },
                updates);
        }

        private static AppendIdentityDecisionFormViewModel CreateDecisionForm() =>
            new AppendIdentityDecisionFormViewModel(
                OperationFormSmokeTests.CreateJob(),
                new EngineStatusViewModel(),
                new OperationFormSmokeTests.NullFileDialogService());

        private static IReadOnlyList<OperationFormViewModel> AllTwelveForms()
        {
            var engineStatus = new EngineStatusViewModel();
            var dialogs = new OperationFormSmokeTests.NullFileDialogService();

            var forms = new List<OperationFormViewModel>(OperationFormSmokeTests.AllForms())
            {
                new CreateGovernanceFormViewModel(OperationFormSmokeTests.CreateJob(), engineStatus, dialogs),
                new ValidateGovernanceFormViewModel(OperationFormSmokeTests.CreateJob(), engineStatus, dialogs),
                new RenderGovernanceReportFormViewModel(OperationFormSmokeTests.CreateJob(), engineStatus, dialogs),
                CreateDecisionForm(),
            };

            Assert.Equal(12, forms.Count);
            return forms;
        }
    }
}
