namespace OrzioClashReport.Core.Abstractions
{
    /// <summary>Minimal logging seam so adapters can report defensive-parsing decisions without a framework dependency.</summary>
    public interface IAppLog
    {
        void Warn(string message);

        void Error(string message);
    }
}
