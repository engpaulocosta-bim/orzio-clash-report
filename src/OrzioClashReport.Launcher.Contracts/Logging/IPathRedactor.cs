namespace OrzioClashReport.Launcher.Contracts.Logging
{
    /// <summary>
    /// Turns an absolute path into the only representation the launcher is allowed to record. There is
    /// no inverse operation, and no setting that turns redaction off.
    /// </summary>
    public interface IPathRedactor
    {
        RedactedPath Redact(string path);
    }
}
