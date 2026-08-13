using System;

namespace OrzioClashReport.Launcher.Contracts.Jobs
{
    /// <summary>
    /// Raw process outcome, before any launcher interpretation. Exactly one of
    /// <see cref="TimedOut"/>, <see cref="Canceled"/>, <see cref="StartFailure"/> may be true; when all
    /// three are false the process ran to completion and <see cref="ExitCode"/> has a value.
    /// </summary>
    public sealed class EngineProcessResult
    {
        public int? ExitCode { get; }
        public string StandardOutput { get; }
        public string StandardError { get; }
        public bool StandardOutputTruncated { get; }
        public bool StandardErrorTruncated { get; }
        public bool TimedOut { get; }
        public bool Canceled { get; }
        public string? StartFailure { get; }
        public TimeSpan Duration { get; }

        public EngineProcessResult(
            int? exitCode,
            string standardOutput,
            string standardError,
            bool standardOutputTruncated,
            bool standardErrorTruncated,
            bool timedOut,
            bool canceled,
            string? startFailure,
            TimeSpan duration)
        {
            if (duration < TimeSpan.Zero)
            {
                throw new ArgumentOutOfRangeException(nameof(duration), duration, "Duration cannot be negative.");
            }

            int abnormal = (timedOut ? 1 : 0) + (canceled ? 1 : 0) + (startFailure != null ? 1 : 0);
            if (abnormal > 1)
            {
                throw new ArgumentException(
                    "A process result cannot be timed out, canceled, and a start failure at the same time.",
                    nameof(timedOut));
            }

            if (abnormal == 0 && exitCode == null)
            {
                throw new ArgumentException(
                    "A process that completed normally must report an exit code.", nameof(exitCode));
            }

            ExitCode = exitCode;
            StandardOutput = standardOutput ?? throw new ArgumentNullException(nameof(standardOutput));
            StandardError = standardError ?? throw new ArgumentNullException(nameof(standardError));
            StandardOutputTruncated = standardOutputTruncated;
            StandardErrorTruncated = standardErrorTruncated;
            TimedOut = timedOut;
            Canceled = canceled;
            StartFailure = startFailure;
            Duration = duration;
        }

        public bool CompletedNormally => !TimedOut && !Canceled && StartFailure == null;
    }
}
