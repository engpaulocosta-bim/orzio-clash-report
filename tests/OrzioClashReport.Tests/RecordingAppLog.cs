using System.Collections.Generic;
using OrzioClashReport.Core.Abstractions;

namespace OrzioClashReport.Tests
{
    /// <summary>Test double for <see cref="IAppLog"/> that records messages instead of writing anywhere.</summary>
    public sealed class RecordingAppLog : IAppLog
    {
        public List<string> Warnings { get; } = new List<string>();
        public List<string> Errors { get; } = new List<string>();

        public void Warn(string message) => Warnings.Add(message);

        public void Error(string message) => Errors.Add(message);
    }
}
