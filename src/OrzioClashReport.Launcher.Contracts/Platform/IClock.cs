using System;

namespace OrzioClashReport.Launcher.Contracts.Platform
{
    /// <summary>Supplies the current time, so journals, logs and recent items stay testable.</summary>
    public interface IClock
    {
        DateTimeOffset UtcNow { get; }
    }
}
