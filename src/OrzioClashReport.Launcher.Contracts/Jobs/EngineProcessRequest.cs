using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace OrzioClashReport.Launcher.Contracts.Jobs
{
    /// <summary>
    /// The lowest-level engine invocation: an executable, an ordered argument vector, a working
    /// directory, and a timeout. There is no command string anywhere in this contract, by design.
    /// </summary>
    public sealed class EngineProcessRequest
    {
        public string ExecutablePath { get; }
        public IReadOnlyList<string> ArgumentList { get; }
        public string WorkingDirectory { get; }
        public TimeSpan Timeout { get; }

        public EngineProcessRequest(
            string executablePath,
            IReadOnlyList<string> argumentList,
            string workingDirectory,
            TimeSpan timeout)
        {
            if (string.IsNullOrWhiteSpace(executablePath))
            {
                throw new ArgumentException("Executable path cannot be empty.", nameof(executablePath));
            }

            if (argumentList == null)
            {
                throw new ArgumentNullException(nameof(argumentList));
            }

            if (string.IsNullOrWhiteSpace(workingDirectory))
            {
                throw new ArgumentException("Working directory cannot be empty.", nameof(workingDirectory));
            }

            if (timeout <= TimeSpan.Zero)
            {
                throw new ArgumentOutOfRangeException(nameof(timeout), timeout, "Timeout must be positive.");
            }

            var arguments = new List<string>(argumentList.Count);
            for (int i = 0; i < argumentList.Count; i++)
            {
                if (argumentList[i] == null)
                {
                    throw new ArgumentException($"Argument at index {i} is null.", nameof(argumentList));
                }

                arguments.Add(argumentList[i]);
            }

            ExecutablePath = executablePath;
            ArgumentList = new ReadOnlyCollection<string>(arguments);
            WorkingDirectory = workingDirectory;
            Timeout = timeout;
        }
    }
}
