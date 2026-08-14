namespace OrzioClashReport.Launcher.Contracts.Jobs
{
    /// <summary>Lifecycle of one launcher job. <see cref="Succeeded"/>, <see cref="Failed"/> and <see cref="Canceled"/> are terminal.</summary>
    public enum EngineJobState
    {
        Pending = 0,
        Running = 1,
        Succeeded = 2,
        Failed = 3,
        Canceled = 4,
    }
}
