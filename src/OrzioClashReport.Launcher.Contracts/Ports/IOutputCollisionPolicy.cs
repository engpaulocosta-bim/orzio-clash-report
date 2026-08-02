using System.Threading;
using System.Threading.Tasks;

namespace OrzioClashReport.Launcher.Contracts.Ports
{
    /// <summary>What a human chose to do about an HTML destination that already exists.</summary>
    public enum OutputCollisionDecision
    {
        /// <summary>Do not run. This is the default whenever nobody answered.</summary>
        Cancel = 0,

        /// <summary>Run against a different destination the human picked.</summary>
        UseDifferentPath = 1,

        /// <summary>Replace the existing report. Never a default.</summary>
        Replace = 2,
    }

    /// <summary>The decision plus, when a different destination was chosen, that destination.</summary>
    public sealed class OutputCollisionResolution
    {
        private OutputCollisionResolution(OutputCollisionDecision decision, string? replacementPath)
        {
            Decision = decision;
            ReplacementPath = replacementPath;
        }

        public OutputCollisionDecision Decision { get; }

        /// <summary>Set exactly when <see cref="Decision"/> is <see cref="OutputCollisionDecision.UseDifferentPath"/>.</summary>
        public string? ReplacementPath { get; }

        public static OutputCollisionResolution Cancel { get; } =
            new OutputCollisionResolution(OutputCollisionDecision.Cancel, replacementPath: null);

        public static OutputCollisionResolution Replace { get; } =
            new OutputCollisionResolution(OutputCollisionDecision.Replace, replacementPath: null);

        public static OutputCollisionResolution UseDifferentPath(string replacementPath)
        {
            return new OutputCollisionResolution(
                OutputCollisionDecision.UseDifferentPath,
                Operations.RequestValues.RequiredAbsoluteOutputPath(replacementPath, nameof(replacementPath)));
        }
    }

    /// <summary>
    /// Asks a human what to do when an HTML destination already exists. The engine may overwrite
    /// HTML, so the launcher detects the collision first and never decides on the human's behalf.
    /// Only HTML has a collision question: snapshots, run indexes, and project catalogs use
    /// create-new semantics in the engine and the launcher never offers to overwrite them.
    /// </summary>
    public interface IOutputCollisionPrompt
    {
        Task<OutputCollisionResolution> ResolveAsync(string existingPath, CancellationToken cancellationToken);
    }
}
