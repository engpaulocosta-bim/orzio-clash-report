using System;
using System.Collections.Generic;
using System.IO;

namespace OrzioClashReport.Launcher.Application.Engine
{
    /// <summary>
    /// Builds the exact argument vector for each published engine command. This is the only place in
    /// the launcher that knows what the engine's flags are called, and it produces a list of strings —
    /// never a command line. Every vector here mirrors a contract already published by the CLI; no
    /// flag, subcommand, or ordering is invented.
    /// </summary>
    public static class EngineArgumentBuilder
    {
        /// <summary>
        /// Single-run report. The XML input is positional and there is no subcommand, exactly as the
        /// engine's legacy contract defines it.
        /// </summary>
        public static IReadOnlyList<string> QuickReport(string inputXmlPath, string outputHtmlPath)
        {
            RequireInput(inputXmlPath, nameof(inputXmlPath));
            RequireAbsoluteOutput(outputHtmlPath, nameof(outputHtmlPath));

            return new[] { inputXmlPath, "-o", outputHtmlPath };
        }

        internal static void RequireInput(string value, string parameterName)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                throw new ArgumentException("An input path is required.", parameterName);
            }
        }

        /// <summary>
        /// Every <c>-o</c> destination is absolute. A relative destination would be resolved against
        /// the process working directory, which is not something a user can see or predict.
        /// </summary>
        internal static void RequireAbsoluteOutput(string value, string parameterName)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                throw new ArgumentException("An output path is required.", parameterName);
            }

            if (!Path.IsPathFullyQualified(value))
            {
                throw new ArgumentException("The output path must be absolute.", parameterName);
            }
        }
    }
}
