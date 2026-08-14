using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using OrzioClashReport.Launcher.Contracts.Operations;
using OrzioClashReport.Launcher.Contracts.Results;

namespace OrzioClashReport.Launcher.Contracts.Jobs
{
    /// <summary>
    /// The complete outcome of one engine job. Captured streams are bounded (see the ring-buffer policy
    /// in the infrastructure runner), so <see cref="StandardOutputTruncated"/> and
    /// <see cref="StandardErrorTruncated"/> state honestly when the middle of a stream was dropped.
    /// </summary>
    public sealed class EngineJobResult
    {
        public string JobId { get; }
        public LauncherOperationKind Operation { get; }
        public EngineJobState State { get; }
        public int? ExitCode { get; }
        public string StandardOutput { get; }
        public string StandardError { get; }
        public bool StandardOutputTruncated { get; }
        public bool StandardErrorTruncated { get; }
        public TimeSpan Duration { get; }
        public LauncherError? Error { get; }
        public IReadOnlyList<LauncherArtifact> Artifacts { get; }
        public IReadOnlyList<LauncherWarning> Warnings { get; }

        public EngineJobResult(
            string jobId,
            LauncherOperationKind operation,
            EngineJobState state,
            int? exitCode,
            string standardOutput,
            string standardError,
            bool standardOutputTruncated,
            bool standardErrorTruncated,
            TimeSpan duration,
            LauncherError? error,
            IReadOnlyList<LauncherArtifact> artifacts,
            IReadOnlyList<LauncherWarning> warnings)
        {
            if (string.IsNullOrWhiteSpace(jobId))
            {
                throw new ArgumentException("Job id cannot be empty.", nameof(jobId));
            }

            if (!Enum.IsDefined(typeof(LauncherOperationKind), operation))
            {
                throw new ArgumentOutOfRangeException(nameof(operation), operation, "Unknown launcher operation.");
            }

            if (!Enum.IsDefined(typeof(EngineJobState), state))
            {
                throw new ArgumentOutOfRangeException(nameof(state), state, "Unknown job state.");
            }

            if (state == EngineJobState.Pending || state == EngineJobState.Running)
            {
                throw new ArgumentException("A job result requires a terminal state.", nameof(state));
            }

            if (state == EngineJobState.Failed && error == null)
            {
                throw new ArgumentException("A failed job result requires an error.", nameof(error));
            }

            if (state == EngineJobState.Succeeded && error != null)
            {
                throw new ArgumentException("A succeeded job result cannot carry an error.", nameof(error));
            }

            if (duration < TimeSpan.Zero)
            {
                throw new ArgumentOutOfRangeException(nameof(duration), duration, "Duration cannot be negative.");
            }

            JobId = jobId;
            Operation = operation;
            State = state;
            ExitCode = exitCode;
            StandardOutput = standardOutput ?? throw new ArgumentNullException(nameof(standardOutput));
            StandardError = standardError ?? throw new ArgumentNullException(nameof(standardError));
            StandardOutputTruncated = standardOutputTruncated;
            StandardErrorTruncated = standardErrorTruncated;
            Duration = duration;
            Error = error;
            Artifacts = CopyOf(artifacts, nameof(artifacts));
            Warnings = CopyOf(warnings, nameof(warnings));
        }

        private static IReadOnlyList<T> CopyOf<T>(IReadOnlyList<T> source, string parameterName)
            where T : class
        {
            if (source == null)
            {
                throw new ArgumentNullException(parameterName);
            }

            var copy = new List<T>(source.Count);
            for (int i = 0; i < source.Count; i++)
            {
                if (source[i] == null)
                {
                    throw new ArgumentException($"Entry at index {i} is null.", parameterName);
                }

                copy.Add(source[i]);
            }

            return new ReadOnlyCollection<T>(copy);
        }
    }
}
