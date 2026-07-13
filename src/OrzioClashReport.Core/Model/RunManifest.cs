using System;
using System.Collections.Generic;

namespace OrzioClashReport.Core.Model
{
    /// <summary>
    /// Immutable, validated declaration of a coordination run: an opaque <see cref="RunId"/>, when it was
    /// created, the exact <see cref="ModelRevision"/>s that compose it, and the exact clash tests declared as
    /// executed for it. This is a declared fact, not a computation: it does not calculate clashes, compare
    /// runs, or decide lifecycle status. An empty <see cref="ExecutedClashTests"/> list is valid and means no
    /// clash test was declared as executed for this run; it is never inferred from
    /// <see cref="ClashOccurrence"/>s observed elsewhere.
    /// </summary>
    public sealed class RunManifest
    {
        public string RunId { get; }
        public DateTimeOffset CreatedAt { get; }
        public IReadOnlyList<ModelRevision> Models { get; }
        public IReadOnlyList<ExecutedClashTest> ExecutedClashTests { get; }

        public RunManifest(
            string runId,
            DateTimeOffset createdAt,
            IReadOnlyList<ModelRevision> models,
            IReadOnlyList<ExecutedClashTest> executedClashTests)
        {
            RunId = RequireNonBlank(runId, nameof(runId));
            CreatedAt = createdAt;
            Models = ValidateModels(models);
            ExecutedClashTests = ValidateExecutedClashTests(executedClashTests, Models, RunId);
        }

        private static IReadOnlyList<ModelRevision> ValidateModels(IReadOnlyList<ModelRevision> models)
        {
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

            return copy.AsReadOnly();
        }

        private static IReadOnlyList<ExecutedClashTest> ValidateExecutedClashTests(
            IReadOnlyList<ExecutedClashTest> executedClashTests, IReadOnlyList<ModelRevision> models, string runId)
        {
            if (executedClashTests == null)
            {
                throw new ArgumentNullException(nameof(executedClashTests));
            }

            var copy = new List<ExecutedClashTest>(executedClashTests);

            for (int i = 0; i < copy.Count; i++)
            {
                var test = copy[i];
                if (test == null)
                {
                    throw new ArgumentException($"Executed clash test at index {i} cannot be null.", nameof(executedClashTests));
                }

                if (!ContainsIdentity(models, test.ModelA))
                {
                    throw new ArgumentException(
                        $"Executed clash test at index {i} ('{test.Name}') references ModelA '{test.ModelA}', "
                        + $"which is not declared in run manifest '{runId}'.",
                        nameof(executedClashTests));
                }

                if (!ContainsIdentity(models, test.ModelB))
                {
                    throw new ArgumentException(
                        $"Executed clash test at index {i} ('{test.Name}') references ModelB '{test.ModelB}', "
                        + $"which is not declared in run manifest '{runId}'.",
                        nameof(executedClashTests));
                }
            }

            for (int i = 0; i < copy.Count; i++)
            {
                for (int j = i + 1; j < copy.Count; j++)
                {
                    if (IsDuplicateTest(copy[i], copy[j]))
                    {
                        throw new ArgumentException(
                            $"Duplicate executed clash test at indexes {i} and {j}: '{copy[i].Name}' for "
                            + $"{copy[i].ModelA} x {copy[i].ModelB}. A run manifest allows at most one declaration "
                            + "per clash test name (case-insensitive) and model pair, regardless of A/B order.",
                            nameof(executedClashTests));
                    }
                }
            }

            return copy.AsReadOnly();
        }

        private static bool ContainsIdentity(IReadOnlyList<ModelRevision> models, ModelIdentity identity)
        {
            for (int i = 0; i < models.Count; i++)
            {
                if (models[i].Identity.Equals(identity))
                {
                    return true;
                }
            }

            return false;
        }

        private static bool IsDuplicateTest(ExecutedClashTest x, ExecutedClashTest y)
        {
            if (!string.Equals(x.Name, y.Name, StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            return (x.ModelA.Equals(y.ModelA) && x.ModelB.Equals(y.ModelB))
                || (x.ModelA.Equals(y.ModelB) && x.ModelB.Equals(y.ModelA));
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

        /// <summary>Human-readable representation for logs and test output, e.g. "run-1 @ 2026-07-10T09:00:00.0000000+01:00 (2 models, 3 executed clash tests)". Not a persistence key.</summary>
        public override string ToString() =>
            $"{RunId} @ {CreatedAt:O} ({Models.Count} models, {ExecutedClashTests.Count} executed clash tests)";
    }
}
