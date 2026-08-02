using OrzioClashReport.Launcher.Application;
using OrzioClashReport.Launcher.Contracts.Engine;
using OrzioClashReport.Launcher.Contracts.Jobs;
using OrzioClashReport.Launcher.Contracts.Operations;
using OrzioClashReport.Launcher.Contracts.Ports;
using OrzioClashReport.Launcher.Contracts.Settings;
using OrzioClashReport.Launcher.Desktop.ViewModels;

namespace OrzioClashReport.Launcher.Tests;

public sealed class IdentityGovernanceFormTests
{
    private static string Absolute(string name) => Path.Combine(Path.GetTempPath(), name);

    private sealed class Harness
    {
        internal Harness()
        {
            Gateway = new StubEngineGateway(OperationResult.Success(
                exitCode: 0,
                artifacts: null,
                warnings: null,
                standardOutput: "Identity governance validation passed.",
                standardError: string.Empty,
                duration: TimeSpan.FromSeconds(1)));

            Dialogs = new StubFileDialogs();

            var jobs = new OperationJobService(
                Gateway, new RecordingJournal(), new InMemoryRecentItemsStore(), new StubFileSystem());

            Runner = new OperationRunnerViewModel(
                jobs, new StubCollisionPrompt(OutputCollisionResolution.Cancel), new NoOpOutputRevealer());

            Engine = new EngineStatusViewModel(new StubEngineProbe(EngineProbeResult.Ready("0.1.0-preview.3")));
            Engine.RefreshAsync(CancellationToken.None).GetAwaiter().GetResult();
        }

        internal StubEngineGateway Gateway { get; }

        internal StubFileDialogs Dialogs { get; }

        internal OperationRunnerViewModel Runner { get; }

        internal EngineStatusViewModel Engine { get; }

        internal AppendIdentityDecisionFormViewModel Decision() => new(Runner, Engine, Dialogs);
    }

    private static void FillDecision(
        AppendIdentityDecisionFormViewModel form,
        string leftRunId = "run-001",
        string leftIndex = "0",
        string rightRunId = "run-002",
        string rightIndex = "3",
        string? persistentIdentityId = "identity-1")
    {
        var files = form.Fields.OfType<FileFieldViewModel>().ToList();
        files[0].Path = Absolute("identity-governance.json");

        var texts = form.Fields.OfType<TextFieldViewModel>().ToList();
        texts[0].Value = "decision-1";  // decision id
        texts[1].Value = leftRunId;
        texts[2].Value = rightRunId;
        texts[3].Value = "reviewer-a";  // reviewer alias
        texts[4].Value = persistentIdentityId;  // persistent identity id

        var occurrences = form.Fields.OfType<OccurrenceIndexFieldViewModel>().ToList();
        occurrences[0].Value = leftIndex;
        occurrences[1].Value = rightIndex;
    }

    [Fact]
    public void TheTwoDecisions_AreDistinguishableWithoutColour()
    {
        Assert.Equal(2, DecisionKindOptionViewModel.All.Count);

        DecisionKindOptionViewModel confirm = DecisionKindOptionViewModel.Confirm;
        DecisionKindOptionViewModel reject = DecisionKindOptionViewModel.Reject;

        Assert.NotEqual(confirm.Glyph, reject.Glyph);
        Assert.NotEqual(confirm.Label, reject.Label);
        Assert.NotEqual(confirm.Explanation, reject.Explanation);
        Assert.All(
            new[] { confirm, reject },
            option =>
            {
                Assert.False(string.IsNullOrWhiteSpace(option.Glyph));
                Assert.False(string.IsNullOrWhiteSpace(option.Label));
                Assert.False(string.IsNullOrWhiteSpace(option.Explanation));
            });
    }

    [Fact]
    public void TheCanonicalDecisionValues_AreNotTheVisibleLabels()
    {
        // Only the visible label is localized; the value the engine receives is fixed.
        Assert.Equal(HumanIdentityDecisionKind.ConfirmSameIdentity, DecisionKindOptionViewModel.Confirm.Kind);
        Assert.Equal(HumanIdentityDecisionKind.RejectSameIdentity, DecisionKindOptionViewModel.Reject.Kind);
        Assert.NotEqual("ConfirmSameIdentity", DecisionKindOptionViewModel.Confirm.Label);
    }

    [Fact]
    public async Task Confirming_SendsTheCanonicalKindAndThePersistentIdentityId()
    {
        var harness = new Harness();
        AppendIdentityDecisionFormViewModel form = harness.Decision();

        form.DecisionKind.Selected = DecisionKindOptionViewModel.Confirm;
        FillDecision(form);

        await form.RunCommand.ExecuteAsync(null);

        var request = Assert.IsType<AppendIdentityDecisionRequest>(harness.Gateway.LastRequest);
        Assert.Equal(HumanIdentityDecisionKind.ConfirmSameIdentity, request.DecisionKind);
        Assert.Equal("identity-1", request.PersistentIdentityId);
        Assert.Equal("run-001", request.Left.RunId);
        Assert.Equal(0, request.Left.OccurrenceIndex);
        Assert.Equal("run-002", request.Right.RunId);
        Assert.Equal(3, request.Right.OccurrenceIndex);
        Assert.Equal("reviewer-a", request.ReviewerAlias);
    }

    [Fact]
    public async Task Rejecting_NeverSendsAPersistentIdentityIdEvenIfOneWasTypedFirst()
    {
        var harness = new Harness();
        AppendIdentityDecisionFormViewModel form = harness.Decision();

        // The human filled in a persistent identity, then changed their mind about the decision.
        FillDecision(form, persistentIdentityId: "identity-1");
        form.DecisionKind.Selected = DecisionKindOptionViewModel.Reject;

        await form.RunCommand.ExecuteAsync(null);

        var request = Assert.IsType<AppendIdentityDecisionRequest>(harness.Gateway.LastRequest);
        Assert.Equal(HumanIdentityDecisionKind.RejectSameIdentity, request.DecisionKind);
        Assert.Null(request.PersistentIdentityId);
    }

    [Fact]
    public void AConfirmation_CannotRunWithoutAPersistentIdentityId()
    {
        var harness = new Harness();
        AppendIdentityDecisionFormViewModel form = harness.Decision();

        FillDecision(form, persistentIdentityId: null);
        form.DecisionKind.Selected = DecisionKindOptionViewModel.Confirm;

        Assert.False(form.CanBuild);

        form.DecisionKind.Selected = DecisionKindOptionViewModel.Reject;

        Assert.True(form.CanBuild);
    }

    [Fact]
    public void ThePersistentIdentityField_IsShownOnlyForAConfirmation()
    {
        var harness = new Harness();
        AppendIdentityDecisionFormViewModel form = harness.Decision();
        TextFieldViewModel persistent = form.Fields.OfType<TextFieldViewModel>().ToList()[4];

        form.DecisionKind.Selected = DecisionKindOptionViewModel.Reject;
        Assert.False(persistent.IsShown);

        form.DecisionKind.Selected = DecisionKindOptionViewModel.Confirm;
        Assert.True(persistent.IsShown);
    }

    [Theory]
    [InlineData("0", true, 0)]
    [InlineData("7", true, 7)]
    [InlineData("00", true, 0)]
    [InlineData("-1", false, null)]
    [InlineData("1.5", false, null)]
    [InlineData("um", false, null)]
    [InlineData(" ", false, null)]
    [InlineData("+2", false, null)]
    public void TheOccurrenceIndex_IsZeroBasedAndValidatedForFormatOnly(
        string typed, bool valid, int? expected)
    {
        var field = new OccurrenceIndexFieldViewModel("Field.LeftOccurrence.Label");

        field.Value = typed;

        Assert.Equal(valid, field.HasValue);
        Assert.Equal(expected, field.Index);
    }

    [Fact]
    public void AMalformedOccurrenceIndex_ExplainsItselfWithoutClaimingItDoesNotExist()
    {
        var field = new OccurrenceIndexFieldViewModel("Field.LeftOccurrence.Label");

        field.Value = "-1";

        Assert.True(field.HasFormatError);
        Assert.NotNull(field.FormatError);

        // Existence is the engine's judgement, so the message never claims the occurrence is absent.
        Assert.DoesNotContain("não existe", field.FormatError!, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void AnEmptyOccurrenceIndex_IsIncompleteRatherThanWrong()
    {
        var field = new OccurrenceIndexFieldViewModel("Field.LeftOccurrence.Label");

        Assert.False(field.HasValue);
        Assert.False(field.HasFormatError);
    }

    [Fact]
    public async Task CreateGovernance_SendsTheProjectIdAndDestination()
    {
        var harness = new Harness();
        var form = new CreateIdentityGovernanceFormViewModel(harness.Runner, harness.Engine, harness.Dialogs);

        form.Fields.OfType<TextFieldViewModel>().First().Value = "tower-a";
        form.Fields.OfType<FileFieldViewModel>().First().Path = Absolute("identity-governance.json");

        await form.RunCommand.ExecuteAsync(null);

        var request = Assert.IsType<CreateIdentityGovernanceRequest>(harness.Gateway.LastRequest);
        Assert.Equal("tower-a", request.ProjectId);
        Assert.Equal(Absolute("identity-governance.json"), request.OutputGovernancePath);
    }

    [Fact]
    public async Task Validation_IsReadOnlyAndHasNoDestination()
    {
        var harness = new Harness();
        var form = new ValidateIdentityGovernanceFormViewModel(harness.Runner, harness.Engine, harness.Dialogs);

        List<FileFieldViewModel> files = form.Fields.OfType<FileFieldViewModel>().ToList();
        files[0].Path = Absolute("project.json");
        files[1].Path = Absolute("identity-governance.json");

        await form.RunCommand.ExecuteAsync(null);

        var request = Assert.IsType<ValidateIdentityGovernanceRequest>(harness.Gateway.LastRequest);
        Assert.Null(request.OutputPath);
    }

    [Fact]
    public async Task TheReviewReport_SendsProjectGovernanceAndDestination()
    {
        var harness = new Harness();
        var form = new RenderIdentityGovernanceReportFormViewModel(
            harness.Runner, harness.Engine, harness.Dialogs);

        List<FileFieldViewModel> files = form.Fields.OfType<FileFieldViewModel>().ToList();
        files[0].Path = Absolute("project.json");
        files[1].Path = Absolute("identity-governance.json");
        files[2].Path = Absolute("identity-governance.html");

        await form.RunCommand.ExecuteAsync(null);

        var request = Assert.IsType<RenderIdentityGovernanceReportRequest>(harness.Gateway.LastRequest);
        Assert.Equal(Absolute("project.json"), request.ProjectPath);
        Assert.Equal(Absolute("identity-governance.json"), request.GovernancePath);
        Assert.Equal(Absolute("identity-governance.html"), request.OutputHtmlPath);
    }

    [Fact]
    public void TheDecisionForm_SaysThatASuggestionIsNotADecision()
    {
        var harness = new Harness();
        AppendIdentityDecisionFormViewModel form = harness.Decision();

        using (LanguageScope.Use(InterfaceLanguage.Portuguese))
        {
            Assert.Contains("nunca é uma decisão", form.Description, StringComparison.Ordinal);
            Assert.Contains("Confiança alta não significa", form.Description, StringComparison.Ordinal);
        }

        using (LanguageScope.Use(InterfaceLanguage.English))
        {
            Assert.Contains("never a decision", form.Description, StringComparison.Ordinal);
            Assert.Contains("High confidence does not mean", form.Description, StringComparison.Ordinal);
        }
    }
}
