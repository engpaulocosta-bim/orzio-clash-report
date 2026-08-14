using System;

namespace OrzioClashReport.Launcher.Contracts.Results
{
    /// <summary>One immutable warning shown to the user. Warnings never block and never mutate the request.</summary>
    public sealed class LauncherWarning
    {
        public LauncherWarningKind Kind { get; }
        public string Message { get; }

        public LauncherWarning(LauncherWarningKind kind, string message)
        {
            if (!Enum.IsDefined(typeof(LauncherWarningKind), kind))
            {
                throw new ArgumentOutOfRangeException(nameof(kind), kind, "Unknown launcher warning kind.");
            }

            if (string.IsNullOrWhiteSpace(message))
            {
                throw new ArgumentException("Warning message cannot be empty.", nameof(message));
            }

            Kind = kind;
            Message = message;
        }
    }
}
