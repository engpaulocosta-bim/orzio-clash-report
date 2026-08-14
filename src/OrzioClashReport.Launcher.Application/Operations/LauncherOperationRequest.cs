using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using OrzioClashReport.Launcher.Contracts.Operations;

namespace OrzioClashReport.Launcher.Application.Operations
{
    /// <summary>
    /// One operation a form has fully specified: the already-built argument vector, where the engine
    /// should run, which destination it will write, and what the human decided about a collision.
    /// </summary>
    public sealed class LauncherOperationRequest
    {
        public LauncherOperationKind Operation { get; }
        public IReadOnlyList<string> ArgumentList { get; }
        public string WorkingDirectory { get; }
        public string? OutputPath { get; }
        public OutputCollisionDecision CollisionDecision { get; }
        public string DisplayName { get; }

        public LauncherOperationRequest(
            LauncherOperationKind operation,
            IReadOnlyList<string> argumentList,
            string workingDirectory,
            string? outputPath,
            OutputCollisionDecision collisionDecision,
            string displayName)
        {
            if (!Enum.IsDefined(typeof(LauncherOperationKind), operation))
            {
                throw new ArgumentOutOfRangeException(nameof(operation), operation, "Unknown launcher operation.");
            }

            if (argumentList == null)
            {
                throw new ArgumentNullException(nameof(argumentList));
            }

            if (string.IsNullOrWhiteSpace(workingDirectory))
            {
                throw new ArgumentException("Working directory cannot be empty.", nameof(workingDirectory));
            }

            if (!Enum.IsDefined(typeof(OutputCollisionDecision), collisionDecision))
            {
                throw new ArgumentOutOfRangeException(
                    nameof(collisionDecision), collisionDecision, "Unknown collision decision.");
            }

            if (string.IsNullOrWhiteSpace(displayName))
            {
                throw new ArgumentException("Display name cannot be empty.", nameof(displayName));
            }

            if (outputPath != null && !LauncherOperationMetadata.SupportsOutputOption(operation))
            {
                throw new ArgumentException(
                    $"The '{operation}' command has no published output option.", nameof(outputPath));
            }

            Operation = operation;
            ArgumentList = new ReadOnlyCollection<string>(new List<string>(argumentList));
            WorkingDirectory = workingDirectory;
            OutputPath = outputPath;
            CollisionDecision = collisionDecision;
            DisplayName = displayName;
        }
    }
}
