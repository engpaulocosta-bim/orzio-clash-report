using System;

namespace OrzioClashReport.Launcher.Contracts.Results
{
    /// <summary>
    /// One actionable failure. <see cref="Message"/> states what happened, <see cref="NextStep"/> states
    /// what the user can do about it, and <see cref="ExitCode"/> carries the engine's raw exit code when
    /// one exists. The launcher never parses <see cref="Message"/> to take a decision.
    /// </summary>
    public sealed class LauncherError
    {
        public LauncherErrorKind Kind { get; }
        public string Message { get; }
        public string NextStep { get; }
        public int? ExitCode { get; }

        public LauncherError(LauncherErrorKind kind, string message, string nextStep, int? exitCode = null)
        {
            if (!Enum.IsDefined(typeof(LauncherErrorKind), kind))
            {
                throw new ArgumentOutOfRangeException(nameof(kind), kind, "Unknown launcher error kind.");
            }

            if (string.IsNullOrWhiteSpace(message))
            {
                throw new ArgumentException("Error message cannot be empty.", nameof(message));
            }

            if (string.IsNullOrWhiteSpace(nextStep))
            {
                throw new ArgumentException("Error next step cannot be empty.", nameof(nextStep));
            }

            Kind = kind;
            Message = message;
            NextStep = nextStep;
            ExitCode = exitCode;
        }
    }
}
