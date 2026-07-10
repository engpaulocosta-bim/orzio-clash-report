using System;
using System.Collections.Generic;

namespace OrzioClashReport.Core.Model
{
    /// <summary>
    /// Immutable, validated declaration of a coordination run: an opaque <see cref="RunId"/>, when it was
    /// created, and the exact <see cref="ModelRevision"/>s that compose it. This is a declared fact, not a
    /// computation: it does not calculate clashes, compare runs, or decide lifecycle status.
    /// </summary>
    public sealed class RunManifest
    {
        public string RunId { get; }
        public DateTimeOffset CreatedAt { get; }
        public IReadOnlyList<ModelRevision> Models { get; }

        public RunManifest(string runId, DateTimeOffset createdAt, IReadOnlyList<ModelRevision> models)
        {
            RunId = RequireNonBlank(runId, nameof(runId));
            CreatedAt = createdAt;

            if (models == null)
            {
                throw new ArgumentNullException(nameof(models));
            }

            var copy = new List<ModelRevision>(models);

            if (copy.Count == 0)
            {
                throw new ArgumentException("A run manifest must declare at least one model revision.", nameof(models));
            }

            for (int i = 0; i < copy.Count; i++)
            {
                if (copy[i] == null)
                {
                    throw new ArgumentException($"Model revision at index {i} cannot be null.", nameof(models));
                }
            }

            for (int i = 0; i < copy.Count; i++)
            {
                for (int j = i + 1; j < copy.Count; j++)
                {
                    if (copy[i].Identity.Equals(copy[j].Identity))
                    {
                        throw new ArgumentException(
                            $"Duplicate model identity at indexes {i} and {j}: {copy[i].Identity}. "
                            + "A run manifest allows at most one revision per model identity.",
                            nameof(models));
                    }
                }
            }

            Models = copy.AsReadOnly();
        }

        private static string RequireNonBlank(string value, string paramName)
        {
            if (value == null)
            {
                throw new ArgumentNullException(paramName);
            }

            string trimmed = value.Trim();
            if (trimmed.Length == 0)
            {
                throw new ArgumentException("Value cannot be empty or whitespace.", paramName);
            }

            return trimmed;
        }

        /// <summary>Human-readable representation for logs and test output, e.g. "run-1 @ 2026-07-10T09:00:00.0000000+01:00 (2 models)". Not a persistence key.</summary>
        public override string ToString() => $"{RunId} @ {CreatedAt:O} ({Models.Count} models)";
    }
}
