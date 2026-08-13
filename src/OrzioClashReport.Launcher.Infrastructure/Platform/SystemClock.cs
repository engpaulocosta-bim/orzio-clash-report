using System;
using OrzioClashReport.Launcher.Contracts.Platform;

namespace OrzioClashReport.Launcher.Infrastructure.Platform
{
    /// <summary>The real clock. Tests substitute <see cref="IClock"/> instead of freezing time globally.</summary>
    public sealed class SystemClock : IClock
    {
        public DateTimeOffset UtcNow => DateTimeOffset.UtcNow;
    }
}
