using System;
using System.IO;
using System.Reflection;
using OrzioClashReport.Cli;

namespace OrzioClashReport.Tests
{
    [Collection("CliConsole")]
    public sealed class CliMetadataTests
    {
        private static readonly MethodInfo MainMethod = ResolveMainMethod();

        [Fact]
        public void Main_Help_ExitsZeroAndListsEverySupportedCommand()
        {
            var result = InvokeMain("--help");

            Assert.Equal(0, result.ExitCode);
            Assert.Equal(string.Empty, result.StdErr);
            Assert.Contains("Default workflow:", result.StdOut, StringComparison.Ordinal);
            Assert.Contains("Generate the grouped single-run HTML report", result.StdOut, StringComparison.Ordinal);
            Assert.Contains("compare", result.StdOut, StringComparison.Ordinal);
            Assert.Contains("snapshot", result.StdOut, StringComparison.Ordinal);
            Assert.Contains("compare-snapshots", result.StdOut, StringComparison.Ordinal);
            Assert.Contains("index-snapshots", result.StdOut, StringComparison.Ordinal);
            Assert.Contains("compare-index", result.StdOut, StringComparison.Ordinal);
            Assert.Contains("create-project", result.StdOut, StringComparison.Ordinal);
            Assert.Contains("append-project-snapshot", result.StdOut, StringComparison.Ordinal);
            Assert.Contains("render-project", result.StdOut, StringComparison.Ordinal);
            Assert.Contains("create-identity-governance", result.StdOut, StringComparison.Ordinal);
            Assert.Contains("append-identity-decision", result.StdOut, StringComparison.Ordinal);
            Assert.Contains("validate-identity-governance", result.StdOut, StringComparison.Ordinal);
            Assert.Contains("render-identity-governance-report", result.StdOut, StringComparison.Ordinal);
            Assert.Contains("Run-index order is authoritative", result.StdOut, StringComparison.Ordinal);
        }

        [Fact]
        public void Main_Help_DoesNotAdvertiseSingleRunAsInvokableCommand()
        {
            var result = InvokeMain("--help");

            Assert.Equal(0, result.ExitCode);
            Assert.DoesNotContain("  single-run report", result.StdOut, StringComparison.Ordinal);
            Assert.DoesNotContain("orzioclash single-run", result.StdOut, StringComparison.Ordinal);
        }

        [Fact]
        public void Main_Version_ExitsZeroAndWritesExactlyOneStdoutLine()
        {
            var result = InvokeMain("--version");

            Assert.Equal(0, result.ExitCode);
            Assert.Equal(string.Empty, result.StdErr);
            Assert.Equal("orzioclash 0.1.0-preview.3\n", result.StdOut);
            Assert.Single(result.StdOut.Split('\n', StringSplitOptions.RemoveEmptyEntries));
        }

        [Fact]
        public void Main_ExistingCommandDispatch_IsNotInterceptedByMetadataCommands()
        {
            var result = InvokeMain("snapshot");

            Assert.Equal(1, result.ExitCode);
            Assert.Equal(string.Empty, result.StdOut);
            Assert.Contains("Missing required option '--xml'.", result.StdErr, StringComparison.Ordinal);
            Assert.DoesNotContain("orzioclash - Navisworks Clash Detective report tooling", result.StdErr, StringComparison.Ordinal);
            Assert.DoesNotContain("0.1.0-preview.3", result.StdErr, StringComparison.Ordinal);
        }

        private static MethodInfo ResolveMainMethod()
        {
            var programType = typeof(ConsoleAppLog).Assembly.GetType("OrzioClashReport.Cli.Program", throwOnError: true)!;
            var method = programType.GetMethod("Main", BindingFlags.Static | BindingFlags.NonPublic);
            Assert.NotNull(method);
            return method!;
        }

        private static (int ExitCode, string StdOut, string StdErr) InvokeMain(params string[] args)
        {
            TextWriter originalOut = Console.Out;
            TextWriter originalError = Console.Error;
            var stdoutWriter = new StringWriter();
            var stderrWriter = new StringWriter();

            try
            {
                Console.SetOut(stdoutWriter);
                Console.SetError(stderrWriter);

                object? result;
                try
                {
                    result = MainMethod.Invoke(null, new object[] { args });
                }
                catch (TargetInvocationException ex) when (ex.InnerException != null)
                {
                    throw ex.InnerException;
                }

                return (
                    result is int exitCode ? exitCode : throw new InvalidOperationException("Program.Main did not return an int."),
                    NormalizeLineEndings(stdoutWriter.ToString()),
                    NormalizeLineEndings(stderrWriter.ToString()));
            }
            finally
            {
                Console.SetOut(originalOut);
                Console.SetError(originalError);
                stdoutWriter.Dispose();
                stderrWriter.Dispose();
            }
        }

        private static string NormalizeLineEndings(string value) =>
            value.ReplaceLineEndings("\n");
    }
}
