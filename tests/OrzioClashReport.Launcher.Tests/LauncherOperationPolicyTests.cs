using OrzioClashReport.Launcher.Application;
using OrzioClashReport.Launcher.Contracts.Operations;

namespace OrzioClashReport.Launcher.Tests;

public sealed class LauncherOperationPolicyTests
{
    [Theory]
    [InlineData(LauncherOperationKind.AppendProjectSnapshot)]
    [InlineData(LauncherOperationKind.RenderProject)]
    [InlineData(LauncherOperationKind.ValidateIdentityGovernance)]
    public void OperationsWithoutOutputOption_AreExactlyTheEnginesThree(LauncherOperationKind kind)
    {
        Assert.False(LauncherOperationPolicy.SupportsOutputOption(kind));
    }

    [Fact]
    public void EveryOtherOperation_SupportsTheOutputOption()
    {
        LauncherOperationKind[] withoutOutput =
        {
            LauncherOperationKind.AppendProjectSnapshot,
            LauncherOperationKind.RenderProject,
            LauncherOperationKind.ValidateIdentityGovernance,
        };

        foreach (LauncherOperationKind kind in Enum.GetValues<LauncherOperationKind>())
        {
            if (withoutOutput.Contains(kind))
            {
                continue;
            }

            Assert.True(
                LauncherOperationPolicy.SupportsOutputOption(kind),
                $"{kind} should accept -o.");
        }
    }

    [Theory]
    [InlineData(LauncherOperationKind.CreateSnapshot)]
    [InlineData(LauncherOperationKind.IndexSnapshots)]
    [InlineData(LauncherOperationKind.CreateProject)]
    [InlineData(LauncherOperationKind.CreateIdentityGovernance)]
    public void EvidenceArtifacts_UseCreateNewAndAreNeverOfferedForReplacement(LauncherOperationKind kind)
    {
        Assert.Equal(OutputPersistenceKind.CreateNew, LauncherOperationPolicy.OutputPersistence(kind));
    }

    [Theory]
    [InlineData(LauncherOperationKind.QuickReport)]
    [InlineData(LauncherOperationKind.CompareRuns)]
    [InlineData(LauncherOperationKind.CompareSnapshots)]
    [InlineData(LauncherOperationKind.CompareIndex)]
    [InlineData(LauncherOperationKind.RenderIdentityGovernanceReport)]
    public void DerivedHtml_IsTheOnlyOutputWithACollisionQuestion(LauncherOperationKind kind)
    {
        Assert.Equal(OutputPersistenceKind.DerivedHtml, LauncherOperationPolicy.OutputPersistence(kind));
    }

    [Fact]
    public void EveryOperation_HasAPersistenceClassification()
    {
        foreach (LauncherOperationKind kind in Enum.GetValues<LauncherOperationKind>())
        {
            OutputPersistenceKind persistence = LauncherOperationPolicy.OutputPersistence(kind);
            Assert.True(Enum.IsDefined(persistence));
        }
    }

    [Fact]
    public void LongitudinalOperations_AreMarkedExperimental()
    {
        Assert.True(LauncherOperationPolicy.IsExperimental(LauncherOperationKind.CompareRuns));
        Assert.True(LauncherOperationPolicy.IsExperimental(LauncherOperationKind.CompareSnapshots));
        Assert.True(LauncherOperationPolicy.IsExperimental(LauncherOperationKind.CompareIndex));
        Assert.True(LauncherOperationPolicy.IsExperimental(LauncherOperationKind.RenderProject));

        Assert.False(LauncherOperationPolicy.IsExperimental(LauncherOperationKind.QuickReport));
        Assert.False(LauncherOperationPolicy.IsExperimental(LauncherOperationKind.CreateSnapshot));
    }

    [Fact]
    public void AnUnknownOperation_IsRejectedRatherThanGuessed()
    {
        const LauncherOperationKind unknown = (LauncherOperationKind)999;

        Assert.Throws<ArgumentOutOfRangeException>(() => LauncherOperationPolicy.SupportsOutputOption(unknown));
        Assert.Throws<ArgumentOutOfRangeException>(() => LauncherOperationPolicy.OutputPersistence(unknown));
        Assert.Throws<ArgumentOutOfRangeException>(() => LauncherOperationPolicy.IsExperimental(unknown));
    }
}
