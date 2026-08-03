using System;
using System.IO;
using System.Reflection;
using OrzioClashReport.Cli;

namespace OrzioClashReport.Tests
{
    /// <summary>
    /// The single-run report can be written in either language. The option changes the report's own
    /// wording and nothing else: what the export said is reproduced, never translated.
    /// </summary>
    [Collection("CliConsole")]
    public sealed class CliReportLanguageTests : IDisposable
    {
        private static readonly MethodInfo MainMethod = ResolveMainMethod();

        private readonly string _directory;
        private readonly string _inputPath;

        public CliReportLanguageTests()
        {
            _directory = Path.Combine(Path.GetTempPath(), "orzio-language-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(_directory);

            _inputPath = Path.Combine(_directory, "export.xml");
            File.WriteAllText(_inputPath, SampleExport);
        }

        public void Dispose()
        {
            if (Directory.Exists(_directory))
            {
                Directory.Delete(_directory, recursive: true);
            }
        }

        [Fact]
        public void WithoutTheOption_TheReportIsWrittenInEnglish()
        {
            string outputPath = Path.Combine(_directory, "report.html");

            var result = InvokeMain(_inputPath, "-o", outputPath);

            Assert.Equal(0, result.ExitCode);
            string html = File.ReadAllText(outputPath);
            Assert.Contains("<html lang=\"en\">", html, StringComparison.Ordinal);
            Assert.Contains("Coordination clash report", html, StringComparison.Ordinal);
        }

        [Fact]
        public void WithPortuguese_OnlyTheReportsOwnWordingChanges()
        {
            string outputPath = Path.Combine(_directory, "report-pt.html");

            var result = InvokeMain(_inputPath, "-o", outputPath, "--language", "pt");

            Assert.Equal(0, result.ExitCode);
            string html = File.ReadAllText(outputPath);

            Assert.Contains("<html lang=\"pt\">", html, StringComparison.Ordinal);
            Assert.Contains("Relatório de conflitos de coordenação", html, StringComparison.Ordinal);

            // The export's own vocabulary survives untouched in either language.
            Assert.Contains("Teste 01", html, StringComparison.Ordinal);
            Assert.Contains("New", html, StringComparison.Ordinal);
        }

        [Fact]
        public void TheHeaderNamesTheModelAndTheUnitItsDistancesAreIn()
        {
            string outputPath = Path.Combine(_directory, "report.html");

            Assert.Equal(0, InvokeMain(_inputPath, "-o", outputPath).ExitCode);
            string html = File.ReadAllText(outputPath);

            Assert.Contains("<h1>Federated</h1>", html, StringComparison.Ordinal);
            Assert.Contains("<dd>Federated.nwd</dd>", html, StringComparison.Ordinal);
            Assert.Contains("<dt>Units</dt><dd>m</dd>", html, StringComparison.Ordinal);
            Assert.Contains("<th>Distance (m)</th>", html, StringComparison.Ordinal);

            // The exporting machine's directory is in the export and must never reach the report.
            Assert.DoesNotContain("Acme Confidential", html, StringComparison.Ordinal);
        }

        [Fact]
        public void TheReportSaysWhenItWasProduced()
        {
            string outputPath = Path.Combine(_directory, "report.html");

            Assert.Equal(0, InvokeMain(_inputPath, "-o", outputPath).ExitCode);
            string html = File.ReadAllText(outputPath);

            Assert.Contains("<dt>Generated</dt>", html, StringComparison.Ordinal);
            Assert.Matches(@"<dd>\d{4}-\d{2}-\d{2} \d{2}:\d{2} UTC</dd>", html);
        }

        [Fact]
        public void AnUnknownLanguageIsRefusedRatherThanGuessed()
        {
            string outputPath = Path.Combine(_directory, "report.html");

            var result = InvokeMain(_inputPath, "-o", outputPath, "--language", "fr");

            Assert.Equal(1, result.ExitCode);
            Assert.Contains("Unrecognized language 'fr'", result.StdErr, StringComparison.Ordinal);
            Assert.Contains("Use 'en' or 'pt'", result.StdErr, StringComparison.Ordinal);
            Assert.False(File.Exists(outputPath));
        }

        [Fact]
        public void AMissingLanguageValueIsRefused()
        {
            var result = InvokeMain(_inputPath, "-o", Path.Combine(_directory, "r.html"), "--language");

            Assert.Equal(1, result.ExitCode);
            Assert.Contains("Missing value for '--language'", result.StdErr, StringComparison.Ordinal);
        }

        private const string SampleExport = @"<?xml version=""1.0"" encoding=""UTF-8"" ?>
<exchange units=""m"" filename=""Federated.nwd"" filepath=""C:\Clients\Acme Confidential\2026"">
  <batchtest name=""Report"" units=""m"">
    <clashtests>
      <clashtest name=""Teste 01"" tolerance=""0.001"">
        <clashresults>
          <clashresult name=""Clash1"" guid=""e37d5d4a-2bd1-2ea4-0694-6f2b9d7479ed"" status=""new"" distance=""-0.007"">
            <resultstatus>New</resultstatus>
            <clashpoint><pos3f x=""1.0"" y=""2.0"" z=""3.0""/></clashpoint>
            <clashobjects>
              <clashobject>
                <objectattribute><name>GUID</name><value>48b67dfa-ecf5-541d-fa9d-88e56fc0aa4e</value></objectattribute>
                <layer>Nivel +0</layer>
                <pathlink><node>Arquitetura</node><node>Parede</node></pathlink>
              </clashobject>
              <clashobject>
                <objectattribute><name>GUID</name><value>58b67dfa-ecf5-541d-fa9d-88e56fc0aa4e</value></objectattribute>
                <layer>Nivel +0</layer>
                <pathlink><node>AVAC</node><node>Conduta</node></pathlink>
              </clashobject>
            </clashobjects>
          </clashresult>
        </clashresults>
      </clashtest>
    </clashtests>
  </batchtest>
</exchange>
";

        private static MethodInfo ResolveMainMethod()
        {
            var programType = typeof(ConsoleAppLog).Assembly
                .GetType("OrzioClashReport.Cli.Program", throwOnError: true)!;
            MethodInfo? main = programType.GetMethod("Main", BindingFlags.Static | BindingFlags.NonPublic);

            return main ?? throw new InvalidOperationException("Program.Main was not found.");
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
                    result is int exitCode
                        ? exitCode
                        : throw new InvalidOperationException("Program.Main did not return an int."),
                    stdoutWriter.ToString(),
                    stderrWriter.ToString());
            }
            finally
            {
                Console.SetOut(originalOut);
                Console.SetError(originalError);
                stdoutWriter.Dispose();
                stderrWriter.Dispose();
            }
        }
    }
}
