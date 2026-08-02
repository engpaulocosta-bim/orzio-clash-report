using OrzioClashReport.Launcher.Contracts.Operations;

namespace OrzioClashReport.Launcher.Tests;

public sealed class IdentityDecisionContractTests
{
    private static IdentityEvidenceEndpoint Left => new("run-previous", 0);

    private static IdentityEvidenceEndpoint Right => new("run-current", 3);

    [Fact]
    public void Confirm_CarriesThePersistentIdentityId()
    {
        AppendIdentityDecisionRequest request = AppendIdentityDecisionRequest.Confirm(
            "governance.json", "decision-1", Left, Right, "alias", "identity-1", "same duct");

        Assert.Equal(HumanIdentityDecisionKind.ConfirmSameIdentity, request.DecisionKind);
        Assert.Equal("identity-1", request.PersistentIdentityId);
        Assert.Equal("same duct", request.Reason);
    }

    [Fact]
    public void Confirm_RequiresThePersistentIdentityId()
    {
        Assert.Throws<ArgumentException>(() => AppendIdentityDecisionRequest.Confirm(
            "governance.json", "decision-1", Left, Right, "alias", "   "));
    }

    [Fact]
    public void Reject_NeverCarriesAPersistentIdentityId()
    {
        AppendIdentityDecisionRequest request = AppendIdentityDecisionRequest.Reject(
            "governance.json", "decision-1", Left, Right, "alias");

        Assert.Equal(HumanIdentityDecisionKind.RejectSameIdentity, request.DecisionKind);
        Assert.Null(request.PersistentIdentityId);
    }

    [Fact]
    public void Reason_IsOptionalAndNormalizesEmptyToNull()
    {
        AppendIdentityDecisionRequest request = AppendIdentityDecisionRequest.Reject(
            "governance.json", "decision-1", Left, Right, "alias", "   ");

        Assert.Null(request.Reason);
    }

    [Fact]
    public void OccurrenceIndex_IsZeroBasedAndNeverNegative()
    {
        Assert.Equal(0, new IdentityEvidenceEndpoint("run", 0).OccurrenceIndex);
        Assert.Throws<ArgumentOutOfRangeException>(() => new IdentityEvidenceEndpoint("run", -1));
    }

    [Fact]
    public void DecisionKindNames_AreTheCanonicalEngineValues()
    {
        // These are passed verbatim to --decision-kind. Only a visible label may ever be localized.
        Assert.Equal("ConfirmSameIdentity", HumanIdentityDecisionKind.ConfirmSameIdentity.ToString());
        Assert.Equal("RejectSameIdentity", HumanIdentityDecisionKind.RejectSameIdentity.ToString());
        Assert.Equal(2, Enum.GetValues<HumanIdentityDecisionKind>().Length);
    }
}
