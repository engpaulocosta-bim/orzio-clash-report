using OrzioClashReport.Core.Abstractions;

namespace OrzioClashReport.Cli
{
    /// <summary>Writes defensive-parsing warnings and errors to the console's error stream.</summary>
    public sealed class ConsoleAppLog : IAppLog
    {
        public void Warn(string message) => Console.Error.WriteLine($"WARN: {message}");

        public void Error(string message) => Console.Error.WriteLine($"ERROR: {message}");
    }
}
