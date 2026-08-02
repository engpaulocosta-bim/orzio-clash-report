using OrzioClashReport.Launcher.Contracts.Engine;
using OrzioClashReport.Launcher.Desktop.ViewModels;

namespace OrzioClashReport.Launcher.Tests;

public sealed class EngineStatusPresentationTests
{
    [Fact]
    public void EverySixStatus_HasAGlyphALabelAndADescription()
    {
        EngineStatus[] all = Enum.GetValues<EngineStatus>();

        Assert.Equal(6, all.Length);

        foreach (EngineStatus status in all)
        {
            Assert.False(string.IsNullOrWhiteSpace(EngineStatusViewModel.GlyphFor(status)));
            Assert.False(string.IsNullOrWhiteSpace(EngineStatusViewModel.LabelFor(status)));
            Assert.False(string.IsNullOrWhiteSpace(
                EngineStatusViewModel.DescriptionFor(status, version: null, expectedVersion: null)));
        }
    }

    [Fact]
    public void NoTwoStatuses_ShareAGlyphOrALabel()
    {
        EngineStatus[] all = Enum.GetValues<EngineStatus>();

        // Colour never distinguishes a state on its own: the glyph and the word must differ too.
        Assert.Equal(all.Length, all.Select(EngineStatusViewModel.GlyphFor).Distinct(StringComparer.Ordinal).Count());
        Assert.Equal(all.Length, all.Select(EngineStatusViewModel.LabelFor).Distinct(StringComparer.Ordinal).Count());
    }

    [Theory]
    [InlineData(EngineStatus.Checking, StatusSeverity.Neutral)]
    [InlineData(EngineStatus.Ready, StatusSeverity.Positive)]
    [InlineData(EngineStatus.VersionMismatch, StatusSeverity.Caution)]
    [InlineData(EngineStatus.Unsupported, StatusSeverity.Caution)]
    [InlineData(EngineStatus.IntegrityFailure, StatusSeverity.Critical)]
    [InlineData(EngineStatus.Missing, StatusSeverity.Critical)]
    public void SeverityMapping_IsExplicit(EngineStatus status, StatusSeverity expected)
    {
        Assert.Equal(expected, EngineStatusViewModel.SeverityFor(status));
    }

    [Fact]
    public async Task Probe_DrivesTheBadgeAndTheAbilityToRunOperations()
    {
        var probe = new StubEngineProbe(EngineProbeResult.Ready("0.1.0-preview.3"));
        var viewModel = new EngineStatusViewModel(probe);

        Assert.Equal(EngineStatus.Checking, viewModel.Status);
        Assert.False(viewModel.CanRunOperations);

        await viewModel.RefreshAsync(CancellationToken.None);

        Assert.Equal(EngineStatus.Ready, viewModel.Status);
        Assert.True(viewModel.CanRunOperations);
        Assert.True(viewModel.IsPositive);
        Assert.Contains("0.1.0-preview.3", viewModel.Description, StringComparison.Ordinal);
    }

    [Fact]
    public async Task VersionMismatch_NamesBothVersions()
    {
        var probe = new StubEngineProbe(
            EngineProbeResult.VersionMismatch("0.1.0-preview.2", "0.1.0-preview.3"));
        var viewModel = new EngineStatusViewModel(probe);

        await viewModel.RefreshAsync(CancellationToken.None);

        Assert.Equal(EngineStatus.VersionMismatch, viewModel.Status);
        Assert.False(viewModel.CanRunOperations);
        Assert.Contains("0.1.0-preview.2", viewModel.Description, StringComparison.Ordinal);
        Assert.Contains("0.1.0-preview.3", viewModel.Description, StringComparison.Ordinal);
    }

    [Fact]
    public async Task AMissingEngine_NeverReportsAsUsable()
    {
        var probe = new StubEngineProbe(EngineProbeResult.Missing("no engine"));
        var viewModel = new EngineStatusViewModel(probe);

        await viewModel.RefreshAsync(CancellationToken.None);

        Assert.Equal(EngineStatus.Missing, viewModel.Status);
        Assert.True(viewModel.IsCritical);
        Assert.False(viewModel.CanRunOperations);
    }
}
