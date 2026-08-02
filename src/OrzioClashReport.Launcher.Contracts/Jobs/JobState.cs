using System;
using OrzioClashReport.Launcher.Contracts.Operations;

namespace OrzioClashReport.Launcher.Contracts.Jobs
{
    /// <summary>Lifecycle of one launcher job. Only one job is active per window.</summary>
    public enum JobState
    {
        /// <summary>Created but the engine has not been started.</summary>
        Pending = 0,

        /// <summary>The engine process is running.</summary>
        Running = 1,

        /// <summary>The engine exited successfully and every post-condition held.</summary>
        Succeeded = 2,

        /// <summary>The engine failed, or a post-condition did not hold.</summary>
        Failed = 3,

        /// <summary>A human cancelled the job.</summary>
        Canceled = 4,
    }

    /// <summary>The two engine streams the launcher captures separately.</summary>
    public enum EngineOutputStream
    {
        StandardOutput = 0,
        StandardError = 1,
    }

    /// <summary>One captured chunk of engine output. Immutable.</summary>
    public sealed class EngineOutputChunk
    {
        public EngineOutputChunk(EngineOutputStream stream, string text)
        {
            Stream = stream;
            Text = text ?? throw new ArgumentNullException(nameof(text));
        }

        public EngineOutputStream Stream { get; }

        public string Text { get; }
    }

    /// <summary>
    /// Immutable identity of one job. The value is opaque and safe to write to disk and logs:
    /// it carries no path, client name, or project data.
    /// </summary>
    public readonly struct JobId : IEquatable<JobId>
    {
        private readonly string? _value;

        private JobId(string value)
        {
            _value = value;
        }

        public string Value => _value ?? string.Empty;

        public bool IsEmpty => string.IsNullOrEmpty(_value);

        public static JobId New()
        {
            return new JobId(Guid.NewGuid().ToString("N"));
        }

        public static JobId Parse(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                throw new ArgumentException("Job id must not be empty or whitespace.", nameof(value));
            }

            string trimmed = value.Trim();
            for (int i = 0; i < trimmed.Length; i++)
            {
                char c = trimmed[i];
                bool allowed = (c >= '0' && c <= '9')
                    || (c >= 'a' && c <= 'z')
                    || (c >= 'A' && c <= 'Z')
                    || c == '-';
                if (!allowed)
                {
                    throw new ArgumentException(
                        "Job id may contain only ASCII letters, digits, and '-'.", nameof(value));
                }
            }

            return new JobId(trimmed);
        }

        public bool Equals(JobId other) => string.Equals(Value, other.Value, StringComparison.Ordinal);

        public override bool Equals(object? obj) => obj is JobId other && Equals(other);

        public override int GetHashCode() => StringComparer.Ordinal.GetHashCode(Value);

        public override string ToString() => Value;

        public static bool operator ==(JobId left, JobId right) => left.Equals(right);

        public static bool operator !=(JobId left, JobId right) => !left.Equals(right);
    }

    /// <summary>
    /// The immutable observable state of one job at a point in time. The view layer renders this
    /// and never reaches past it into the engine process.
    /// </summary>
    public sealed class JobSnapshot
    {
        public JobSnapshot(
            JobId jobId,
            LauncherOperationKind operationKind,
            JobState state,
            DateTimeOffset startedAtUtc,
            DateTimeOffset? completedAtUtc,
            OperationResult? result)
        {
            if (jobId.IsEmpty)
            {
                throw new ArgumentException("Job id must not be empty.", nameof(jobId));
            }

            JobId = jobId;
            OperationKind = operationKind;
            State = state;
            StartedAtUtc = startedAtUtc;
            CompletedAtUtc = completedAtUtc;
            Result = result;
        }

        public JobId JobId { get; }

        public LauncherOperationKind OperationKind { get; }

        public JobState State { get; }

        public DateTimeOffset StartedAtUtc { get; }

        public DateTimeOffset? CompletedAtUtc { get; }

        /// <summary>Set once the job reaches a terminal state.</summary>
        public OperationResult? Result { get; }

        public bool IsTerminal =>
            State == JobState.Succeeded || State == JobState.Failed || State == JobState.Canceled;
    }
}
