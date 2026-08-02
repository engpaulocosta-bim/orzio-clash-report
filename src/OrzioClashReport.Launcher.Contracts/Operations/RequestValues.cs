using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;

namespace OrzioClashReport.Launcher.Contracts.Operations
{
    /// <summary>
    /// Operational-format validation shared by the immutable request DTOs.
    /// This validates only what the launcher can honestly validate without the engine:
    /// presence, trimming, absolute output destinations, and non-negative indexes.
    /// Existence, schema validity, workspace containment, and every domain rule remain the engine's.
    /// </summary>
    internal static class RequestValues
    {
        internal static string RequiredText(string? value, string parameterName)
        {
            if (value == null)
            {
                throw new ArgumentNullException(parameterName);
            }

            string trimmed = value.Trim();
            if (trimmed.Length == 0)
            {
                throw new ArgumentException("Value must not be empty or whitespace.", parameterName);
            }

            return trimmed;
        }

        internal static string? OptionalText(string? value)
        {
            if (value == null)
            {
                return null;
            }

            string trimmed = value.Trim();
            return trimmed.Length == 0 ? null : trimmed;
        }

        internal static string RequiredPath(string? value, string parameterName)
        {
            return RequiredText(value, parameterName);
        }

        /// <summary>
        /// An operation that supports <c>-o</c> always receives an absolute destination, so the
        /// engine never resolves it against a working directory the launcher chose.
        /// </summary>
        internal static string RequiredAbsoluteOutputPath(string? value, string parameterName)
        {
            string trimmed = RequiredText(value, parameterName);
            if (!Path.IsPathRooted(trimmed))
            {
                throw new ArgumentException("Output path must be absolute.", parameterName);
            }

            return trimmed;
        }

        internal static int RequiredOccurrenceIndex(int value, string parameterName)
        {
            if (value < 0)
            {
                throw new ArgumentOutOfRangeException(
                    parameterName, value, "Occurrence index is zero-based and must be greater than or equal to 0.");
            }

            return value;
        }

        /// <summary>
        /// Copies an explicitly ordered path list without sorting, deduplicating, or normalizing it.
        /// Caller order is the only authority and duplicates are preserved exactly as declared.
        /// </summary>
        internal static IReadOnlyList<string> RequiredOrderedPaths(
            IReadOnlyList<string>? values, int minimumCount, string parameterName)
        {
            if (values == null)
            {
                throw new ArgumentNullException(parameterName);
            }

            var copy = new List<string>(values.Count);
            for (int i = 0; i < values.Count; i++)
            {
                copy.Add(RequiredText(values[i], parameterName));
            }

            if (copy.Count < minimumCount)
            {
                throw new ArgumentException(
                    $"At least {minimumCount} path(s) must be supplied.", parameterName);
            }

            return new ReadOnlyCollection<string>(copy);
        }
    }
}
