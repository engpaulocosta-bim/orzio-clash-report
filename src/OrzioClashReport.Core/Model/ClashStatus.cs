namespace OrzioClashReport.Core.Model
{
    /// <summary>Review state of a clash result, as tracked in Clash Detective.</summary>
    public enum ClashStatus
    {
        New,
        Active,
        Reviewed,
        Approved,
        Resolved,
        Unknown
    }
}
