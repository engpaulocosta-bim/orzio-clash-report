namespace OrzioClashReport.Launcher.Contracts.Logging
{
    /// <summary>
    /// Writes redacted structured entries to the local log. Logging is best-effort: a failure to write a
    /// log line must never fail the operation the user asked for, and it is never sent anywhere.
    /// </summary>
    public interface ILauncherLog
    {
        void Write(LauncherLogEntry entry);
    }
}
