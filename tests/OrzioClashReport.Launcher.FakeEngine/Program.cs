using System;
using System.Globalization;
using System.IO;
using System.Text;
using System.Threading;

namespace OrzioClashReport.Launcher.FakeEngine
{
    /// <summary>
    /// A stand-in for the real engine, used to exercise the gateway against a real child process.
    /// It accepts the engine's published argument vectors and chooses its behaviour from the input
    /// file's name, so the tests drive it through exactly the arguments the launcher really builds.
    /// </summary>
    internal static class Program
    {
        private const string Version = "orzioclash 9.9.9-fake.1";

        private static int Main(string[] args)
        {
            if (args.Length == 1 && string.Equals(args[0], "--version", StringComparison.Ordinal))
            {
                Console.Out.WriteLine(Version);
                return 0;
            }

            string? input = args.Length > 0 ? args[0] : null;
            string? output = ReadOption(args, "-o");
            string behaviour = input == null
                ? "success"
                : Path.GetFileNameWithoutExtension(input).ToLowerInvariant();

            switch (behaviour)
            {
                case "no-output":
                    Console.Out.WriteLine("0 raw clashes -> 0 groups");
                    return 0;

                case "empty-output":
                    WriteOutput(output, string.Empty);
                    return 0;

                case "fail":
                    Console.Error.WriteLine("Failed to generate report: the export could not be parsed.");
                    return 1;

                case "unexpected-exit":
                    Console.Error.WriteLine("Something unusual happened.");
                    return 42;

                case "slow":
                    Console.Out.WriteLine("working");
                    Console.Out.Flush();
                    Thread.Sleep(TimeSpan.FromMinutes(5));
                    WriteOutput(output, "<html></html>");
                    return 0;

                case "huge-stdout":
                    WriteMany(Console.Out, "stdout");
                    WriteOutput(output, "<html>huge stdout</html>");
                    return 0;

                case "huge-stderr":
                    WriteMany(Console.Error, "stderr");
                    WriteOutput(output, "<html>huge stderr</html>");
                    return 0;

                case "bad-encoding":
                    WriteInvalidUtf8();
                    WriteOutput(output, "<html>bad encoding</html>");
                    return 0;

                default:
                    Console.Out.WriteLine("12 raw clashes -> 4 groups");
                    WriteOutput(output, "<html>fake report</html>");
                    Console.Out.WriteLine(
                        string.Format(CultureInfo.InvariantCulture, "Report written to {0}", output));
                    return 0;
            }
        }

        private static string? ReadOption(string[] args, string name)
        {
            for (int i = 0; i < args.Length - 1; i++)
            {
                if (string.Equals(args[i], name, StringComparison.Ordinal))
                {
                    return args[i + 1];
                }
            }

            return null;
        }

        private static void WriteOutput(string? path, string content)
        {
            if (path == null)
            {
                return;
            }

            File.WriteAllText(path, content, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
        }

        private static void WriteMany(TextWriter writer, string label)
        {
            // Comfortably more than the launcher's 64 KB capture budget.
            for (int i = 0; i < 4000; i++)
            {
                writer.WriteLine(
                    string.Format(CultureInfo.InvariantCulture, "{0} line {1} {2}", label, i, new string('x', 60)));
            }

            writer.Flush();
        }

        private static void WriteInvalidUtf8()
        {
            using Stream raw = Console.OpenStandardOutput();
            byte[] invalid = { 0x41, 0xC3, 0x28, 0xA0, 0xA1, 0xE2, 0x28, 0xA1, 0x0A };
            raw.Write(invalid, 0, invalid.Length);
            raw.Flush();
        }
    }
}
