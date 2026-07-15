using System;
using System.Text.RegularExpressions;

namespace OrzioClashReport.Persistence.RunIndexJson
{
    internal static class RunIndexPathReferenceValidator
    {
        private static readonly Regex WindowsDrivePattern = new Regex(
            @"^[A-Za-z]:",
            RegexOptions.Compiled | RegexOptions.CultureInvariant);

        public static void EnsureCanonicalReferenceForConstructor(string reference, string parameterName)
        {
            if (!TryValidateCanonicalReference(reference, out string message))
            {
                throw new ArgumentException(message, parameterName);
            }
        }

        public static void EnsureCanonicalReference(string? reference, string context)
        {
            if (reference == null)
            {
                throw new RunIndexFormatException($"Field '{context}' cannot be null.");
            }

            if (reference.Trim().Length == 0)
            {
                throw new RunIndexFormatException($"Field '{context}' cannot be empty or whitespace.");
            }

            if (!TryValidateCanonicalReference(reference, out string message))
            {
                throw new RunIndexFormatException($"Field '{context}' is invalid: {message}");
            }
        }

        private static bool TryValidateCanonicalReference(string reference, out string message)
        {
            if (reference.Length == 0 || reference.Trim().Length == 0)
            {
                message = "Run-index snapshot path references cannot be empty or whitespace.";
                return false;
            }

            if (reference.StartsWith("//", StringComparison.Ordinal))
            {
                message = "Run-index snapshot path references must not be UNC paths.";
                return false;
            }

            if (reference.StartsWith("/", StringComparison.Ordinal))
            {
                message = "Run-index snapshot path references must be relative, not absolute.";
                return false;
            }

            if (WindowsDrivePattern.IsMatch(reference))
            {
                message = "Run-index snapshot path references must not be Windows drive-qualified paths.";
                return false;
            }

            if (reference.Contains('\\'))
            {
                message = "Run-index snapshot path references must use '/' separators only.";
                return false;
            }

            if (reference.EndsWith("/", StringComparison.Ordinal))
            {
                message = "Run-index snapshot path references must not end with '/'.";
                return false;
            }

            if (reference == "." || reference == "..")
            {
                message = "Run-index snapshot path references must point to a file, not a bare directory segment.";
                return false;
            }

            string[] segments = reference.Split('/');
            for (int i = 0; i < segments.Length; i++)
            {
                if (segments[i].Length == 0)
                {
                    message = "Run-index snapshot path references must not contain empty path segments.";
                    return false;
                }
            }

            message = string.Empty;
            return true;
        }
    }
}
