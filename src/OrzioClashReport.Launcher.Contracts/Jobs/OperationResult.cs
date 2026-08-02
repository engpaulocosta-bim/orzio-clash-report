using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace OrzioClashReport.Launcher.Contracts.Jobs
{
    /// <summary>
    /// The failure classes the launcher can assert on its own. The engine's own taxonomy is not
    /// invented here: the engine works with exit code 0 for success and 1 for failure, so any
    /// unclassified non-zero exit becomes <see cref="EngineExecutionFailure"/>.
    /// </summary>
    public enum LauncherErrorCode
    {
        /// <summary>The engine exited non-zero without a launcher-side explanation.</summary>
        EngineExecutionFailure = 0,

        /// <summary>The engine exited 0 but the declared output is absent or empty.</summary>
        OutputMissing = 1,

        /// <summary>No engine executable was found.</summary>
        EngineMissing = 2,

        /// <summary>The engine's SHA-256 did not match the packaged manifest.</summary>
        EngineIntegrityFailure = 3,

        /// <summary>The engine reported a version this launcher build does not expect.</summary>
        EngineVersionMismatch = 4,

        /// <summary>The engine could not be probed in a usable way.</summary>
        EngineUnsupported = 5,

        /// <summary>A human cancelled the job.</summary>
        Canceled = 6,

        /// <summary>The engine did not answer within the allotted time.</summary>
        TimedOut = 7,

        /// <summary>The engine could not be started at all.</summary>
        EngineStartFailure = 8,

        /// <summary>The launcher refused the request before starting the engine.</summary>
        InvalidRequest = 9,

        /// <summary>The launcher could not read or write a local file it owns.</summary>
        LocalIoFailure = 10,
    }

    /// <summary>
    /// One immutable, actionable launcher error. Messages are written for a coordinator, not for a
    /// developer, and never carry a stack trace or an absolute path.
    /// </summary>
    public sealed class LauncherError
    {
        public LauncherError(LauncherErrorCode code, string message, string? detail = null)
        {
            if (string.IsNullOrWhiteSpace(message))
            {
                throw new ArgumentException("Message must not be empty or whitespace.", nameof(message));
            }

            Code = code;
            Message = message.Trim();
            Detail = string.IsNullOrWhiteSpace(detail) ? null : detail!.Trim();
        }

        public LauncherErrorCode Code { get; }

        public string Message { get; }

        /// <summary>Optional supporting text, for example the engine's own last stderr lines.</summary>
        public string? Detail { get; }
    }

    /// <summary>The non-fatal conditions the launcher surfaces before or after an operation runs.</summary>
    public enum LauncherWarningCode
    {
        /// <summary>The same snapshot path was declared more than once in an ordered list.</summary>
        DuplicateSnapshotReference = 0,

        /// <summary>The engine wrote to standard error while still exiting successfully.</summary>
        EngineWroteToStandardError = 1,

        /// <summary>Captured engine output exceeded the ring buffer and the middle was dropped.</summary>
        OutputTruncated = 2,

        /// <summary>This operation exercises a part of the engine that is still experimental.</summary>
        ExperimentalOperation = 3,
    }

    /// <summary>One immutable non-fatal warning.</summary>
    public sealed class LauncherWarning
    {
        public LauncherWarning(LauncherWarningCode code, string message)
        {
            if (string.IsNullOrWhiteSpace(message))
            {
                throw new ArgumentException("Message must not be empty or whitespace.", nameof(message));
            }

            Code = code;
            Message = message.Trim();
        }

        public LauncherWarningCode Code { get; }

        public string Message { get; }
    }

    /// <summary>What kind of file an operation produced.</summary>
    public enum ArtifactKind
    {
        HtmlReport = 0,
        SnapshotJson = 1,
        RunIndexJson = 2,
        ProjectCatalogJson = 3,
        IdentityGovernanceJson = 4,
    }

    /// <summary>One immutable artifact the engine wrote and the launcher confirmed exists.</summary>
    public sealed class LauncherArtifact
    {
        public LauncherArtifact(ArtifactKind kind, string path, long sizeInBytes)
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                throw new ArgumentException("Path must not be empty or whitespace.", nameof(path));
            }

            if (sizeInBytes < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(sizeInBytes), sizeInBytes, "Size must not be negative.");
            }

            Kind = kind;
            Path = path;
            SizeInBytes = sizeInBytes;
        }

        public ArtifactKind Kind { get; }

        /// <summary>Absolute path. Held in memory for the session; never written to a default log.</summary>
        public string Path { get; }

        public long SizeInBytes { get; }
    }

    /// <summary>
    /// The immutable outcome of one engine operation: what the engine reported, what the launcher
    /// verified afterwards, and the bounded tail of each captured stream.
    /// </summary>
    public sealed class OperationResult
    {
        private OperationResult(
            bool succeeded,
            int? exitCode,
            IReadOnlyList<LauncherArtifact> artifacts,
            IReadOnlyList<LauncherWarning> warnings,
            LauncherError? error,
            string standardOutput,
            string standardError,
            TimeSpan duration)
        {
            Succeeded = succeeded;
            ExitCode = exitCode;
            Artifacts = artifacts;
            Warnings = warnings;
            Error = error;
            StandardOutput = standardOutput;
            StandardError = standardError;
            Duration = duration;
        }

        public bool Succeeded { get; }

        /// <summary>The engine's exit code, or <c>null</c> when the engine never ran to completion.</summary>
        public int? ExitCode { get; }

        public IReadOnlyList<LauncherArtifact> Artifacts { get; }

        public IReadOnlyList<LauncherWarning> Warnings { get; }

        /// <summary>Set exactly when <see cref="Succeeded"/> is false.</summary>
        public LauncherError? Error { get; }

        /// <summary>Bounded capture of the engine's standard output.</summary>
        public string StandardOutput { get; }

        /// <summary>Bounded capture of the engine's standard error.</summary>
        public string StandardError { get; }

        public TimeSpan Duration { get; }

        public static OperationResult Success(
            int exitCode,
            IReadOnlyList<LauncherArtifact>? artifacts,
            IReadOnlyList<LauncherWarning>? warnings,
            string standardOutput,
            string standardError,
            TimeSpan duration)
        {
            return new OperationResult(
                succeeded: true,
                exitCode,
                Freeze(artifacts),
                Freeze(warnings),
                error: null,
                standardOutput ?? string.Empty,
                standardError ?? string.Empty,
                duration);
        }

        public static OperationResult Failure(
            LauncherError error,
            int? exitCode,
            IReadOnlyList<LauncherWarning>? warnings,
            string standardOutput,
            string standardError,
            TimeSpan duration)
        {
            if (error == null)
            {
                throw new ArgumentNullException(nameof(error));
            }

            return new OperationResult(
                succeeded: false,
                exitCode,
                Freeze<LauncherArtifact>(null),
                Freeze(warnings),
                error,
                standardOutput ?? string.Empty,
                standardError ?? string.Empty,
                duration);
        }

        private static IReadOnlyList<T> Freeze<T>(IReadOnlyList<T>? values)
        {
            if (values == null || values.Count == 0)
            {
                return Array.Empty<T>();
            }

            var copy = new List<T>(values.Count);
            for (int i = 0; i < values.Count; i++)
            {
                T value = values[i];
                if (value == null)
                {
                    throw new ArgumentException("Collection must not contain null entries.", nameof(values));
                }

                copy.Add(value);
            }

            return new ReadOnlyCollection<T>(copy);
        }
    }
}
