using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using OrzioClashReport.Launcher.Contracts.Operations;

namespace OrzioClashReport.Launcher.Contracts.Jobs
{
    /// <summary>
    /// One fully-formed engine invocation. The argument vector is already final and ordered: it is
    /// passed element by element to the process API, never joined into a string and never handed to a
    /// shell. The working directory is the output's directory, never the installation directory.
    /// </summary>
    public sealed class EngineJobRequest
    {
        public string JobId { get; }
        public LauncherOperationKind Operation { get; }
        public IReadOnlyList<string> ArgumentList { get; }
        public string WorkingDirectory { get; }

        /// <summary>
        /// The file the engine is expected to have produced when it exits successfully, or <c>null</c>
        /// for operations that own their destination internally. When present, the launcher verifies
        /// after exit code 0 that the file exists and is not empty.
        /// </summary>
        public string? ExpectedOutputPath { get; }

        public EngineJobRequest(
            string jobId,
            LauncherOperationKind operation,
            IReadOnlyList<string> argumentList,
            string workingDirectory,
            string? expectedOutputPath)
        {
            if (string.IsNullOrWhiteSpace(jobId))
            {
                throw new ArgumentException("Job id cannot be empty.", nameof(jobId));
            }

            if (!Enum.IsDefined(typeof(LauncherOperationKind), operation))
            {
                throw new ArgumentOutOfRangeException(nameof(operation), operation, "Unknown launcher operation.");
            }

            if (argumentList == null)
            {
                throw new ArgumentNullException(nameof(argumentList));
            }

            if (argumentList.Count == 0)
            {
                throw new ArgumentException("Argument list cannot be empty.", nameof(argumentList));
            }

            var arguments = new List<string>(argumentList.Count);
            for (int i = 0; i < argumentList.Count; i++)
            {
                string argument = argumentList[i];
                if (argument == null)
                {
                    throw new ArgumentException($"Argument at index {i} is null.", nameof(argumentList));
                }

                if (argument.Length == 0)
                {
                    throw new ArgumentException($"Argument at index {i} is empty.", nameof(argumentList));
                }

                arguments.Add(argument);
            }

            if (string.IsNullOrWhiteSpace(workingDirectory))
            {
                throw new ArgumentException("Working directory cannot be empty.", nameof(workingDirectory));
            }

            if (expectedOutputPath != null && expectedOutputPath.Length == 0)
            {
                throw new ArgumentException("Expected output path cannot be empty when supplied.", nameof(expectedOutputPath));
            }

            JobId = jobId;
            Operation = operation;
            ArgumentList = new ReadOnlyCollection<string>(arguments);
            WorkingDirectory = workingDirectory;
            ExpectedOutputPath = expectedOutputPath;
        }
    }
}
