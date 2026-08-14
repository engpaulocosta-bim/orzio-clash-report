using System;
using System.Globalization;
using System.IO;
using System.Text;
using System.Threading;

namespace OrzioClashReport.Launcher.FakeEngine
{
    /// <summary>
    /// A deliberately dumb engine stand-in. The first argument selects a behaviour; everything else is
    /// behaviour-specific. It is never shipped and never referenced by the launcher itself.
    /// </summary>
    internal static class Program
    {
        private static int Main(string[] args)
        {
            if (args.Length == 0)
            {
                Console.Error.WriteLine("fake-engine: no behaviour requested");
                return 1;
            }

            // A real single-run invocation: the XML is positional and there is no subcommand. The
            // behaviour is then chosen from the input file's own name, so the tests can drive every
            // outcome while still passing the exact argument vector the launcher really builds.
            if (args[0].EndsWith(".xml", StringComparison.OrdinalIgnoreCase))
            {
                return RunAsSingleRun(args);
            }

            switch (args[0])
            {
                case "--version":
                    Console.WriteLine(Environment.GetEnvironmentVariable("FAKE_ENGINE_VERSION")
                        ?? "orzioclash 0.1.0-preview.3");
                    return 0;

                case "succeed-with-output":
                    return SucceedWithOutput(args);

                case "succeed-without-output":
                    Console.WriteLine("Report written to nowhere");
                    return 0;

                case "fail":
                    Console.Error.WriteLine("Failed to read input: the file is not a Clash Detective export.");
                    return 1;

                case "hang":
                    Thread.Sleep(Timeout.Infinite);
                    return 0;

                case "huge-stdout":
                    return WriteHuge(Console.Out, args);

                case "huge-stderr":
                    return WriteHuge(Console.Error, args);

                case "bad-encoding":
                    return WriteInvalidUtf8();

                case "unexpected-exit":
                    return args.Length > 1 && int.TryParse(args[1], NumberStyles.Integer, CultureInfo.InvariantCulture, out int code)
                        ? code
                        : 42;

                case "echo-arguments":
                    foreach (string argument in args)
                    {
                        Console.WriteLine(argument);
                    }

                    return 0;

                case "echo-working-directory":
                    Console.WriteLine(Directory.GetCurrentDirectory());
                    return 0;

                default:
                    return RunAsSubcommand(args);
            }
        }

        /// <summary>
        /// Stands in for a real engine subcommand. It writes whatever <c>-o</c> names, if anything, and
        /// otherwise just succeeds — enough for the launcher's own contract (argument vector, working
        /// directory, output verification, collision policy) to be exercised end to end.
        /// </summary>
        private static int RunAsSubcommand(string[] args)
        {
            string[] known =
            {
                "snapshot", "compare", "compare-snapshots", "index-snapshots", "compare-index",
                "create-project", "append-project-snapshot", "render-project",
                "create-identity-governance", "append-identity-decision",
                "validate-identity-governance", "render-identity-governance-report",
            };

            if (Array.IndexOf(known, args[0]) < 0)
            {
                Console.Error.WriteLine("fake-engine: unknown behaviour " + args[0]);
                return 1;
            }

            string? destination = OutputOf(args);
            if (destination != null)
            {
                File.WriteAllText(destination, "{ \"fake\": \"" + args[0] + "\" }");
                Console.WriteLine(args[0] + " wrote " + Path.GetFileName(destination));
            }
            else
            {
                Console.WriteLine(args[0] + " completed");
            }

            return 0;
        }

        private static int RunAsSingleRun(string[] args)
        {
            string behaviour = Path.GetFileNameWithoutExtension(args[0]);

            if (behaviour.StartsWith("fail", StringComparison.Ordinal))
            {
                Console.Error.WriteLine("Failed to read input: the file is not a Clash Detective export.");
                return 1;
            }

            if (behaviour.StartsWith("hang", StringComparison.Ordinal))
            {
                Thread.Sleep(Timeout.Infinite);
                return 0;
            }

            if (behaviour.StartsWith("no-output", StringComparison.Ordinal))
            {
                Console.WriteLine("Report written to " + OutputOf(args));
                return 0;
            }

            if (behaviour.StartsWith("empty-output", StringComparison.Ordinal))
            {
                string? destination = OutputOf(args);
                if (destination != null)
                {
                    File.WriteAllText(destination, string.Empty);
                }

                Console.WriteLine("Report written to " + destination);
                return 0;
            }

            return SucceedWithOutput(args);
        }

        private static string? OutputOf(string[] args)
        {
            for (int i = 0; i < args.Length - 1; i++)
            {
                if (args[i] == "-o")
                {
                    return args[i + 1];
                }
            }

            return null;
        }

        private static int SucceedWithOutput(string[] args)
        {
            // Mirrors the real single-run contract: the destination follows -o.
            for (int i = 0; i < args.Length - 1; i++)
            {
                if (args[i] == "-o")
                {
                    File.WriteAllText(args[i + 1], "<!doctype html><html><body>fake report</body></html>");
                    Console.WriteLine("Report written to " + Path.GetFileName(args[i + 1]));
                    return 0;
                }
            }

            Console.Error.WriteLine("fake-engine: succeed-with-output requires -o");
            return 1;
        }

        private static int WriteHuge(TextWriter writer, string[] args)
        {
            int lines = args.Length > 1 && int.TryParse(args[1], NumberStyles.Integer, CultureInfo.InvariantCulture, out int requested)
                ? requested
                : 20000;

            var line = new string('x', 200);

            writer.WriteLine("BEGIN-MARKER");
            for (int i = 0; i < lines; i++)
            {
                writer.WriteLine(line);
            }

            writer.WriteLine("END-MARKER");
            return 0;
        }

        private static int WriteInvalidUtf8()
        {
            // Lone continuation bytes: valid on the wire, not decodable as UTF-8. The launcher must
            // capture replacement characters rather than throwing.
            using (Stream standardOutput = Console.OpenStandardOutput())
            {
                byte[] valid = Encoding.UTF8.GetBytes("before ");
                byte[] invalid = { 0xC3, 0x28, 0xA0, 0xA1, 0xE2, 0x28, 0xA1 };
                byte[] after = Encoding.UTF8.GetBytes(" after\n");

                standardOutput.Write(valid, 0, valid.Length);
                standardOutput.Write(invalid, 0, invalid.Length);
                standardOutput.Write(after, 0, after.Length);
                standardOutput.Flush();
            }

            return 0;
        }
    }
}
