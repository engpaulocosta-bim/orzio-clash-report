using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using OrzioClashReport.Launcher.Contracts.Jobs;

namespace OrzioClashReport.Launcher.Infrastructure.Process
{
    /// <summary>
    /// Runs the engine as a child process. This is the only place in the launcher that starts a
    /// process, and it does so under fixed rules: no shell execution, no window, both streams
    /// redirected, UTF-8 without a BOM, and the argument vector passed element by element through
    /// <see cref="ProcessStartInfo.ArgumentList"/> so no quoting or escaping is ever invented.
    /// </summary>
    public sealed class ProcessJobRunner : IEngineProcessRunner
    {
        private static readonly TimeSpan TerminationGrace = TimeSpan.FromSeconds(10);

        public async Task<EngineProcessResult> RunAsync(
            EngineProcessRequest request,
            IProgress<EngineJobProgress>? progress,
            CancellationToken cancellationToken)
        {
            if (request == null)
            {
                throw new ArgumentNullException(nameof(request));
            }

            var startInfo = new ProcessStartInfo
            {
                FileName = request.ExecutablePath,
                WorkingDirectory = request.WorkingDirectory,
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardInput = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true,

                // UTF-8 without a byte order mark, so a BOM never contaminates the first captured line.
                StandardOutputEncoding = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false),
                StandardErrorEncoding = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false),
            };

            foreach (string argument in request.ArgumentList)
            {
                startInfo.ArgumentList.Add(argument);
            }

            var stopwatch = Stopwatch.StartNew();
            var standardOutput = new BoundedStreamCapture();
            var standardError = new BoundedStreamCapture();

            using (var process = new System.Diagnostics.Process { StartInfo = startInfo })
            {
                try
                {
                    if (!process.Start())
                    {
                        return StartFailure(
                            "The operating system did not start the engine process.", stopwatch.Elapsed);
                    }
                }
                catch (Exception exception) when (
                    exception is Win32Exception
                    || exception is InvalidOperationException
                    || exception is PlatformNotSupportedException
                    || exception is IOException
                    || exception is ObjectDisposedException)
                {
                    // A failure to start is a launcher-visible outcome, not an exception to propagate:
                    // the caller turns it into an actionable error with a next step.
                    return StartFailure(exception.Message, stopwatch.Elapsed);
                }

                // The engine never reads standard input. Closing it immediately guarantees it can never
                // block waiting for a console that does not exist.
                process.StandardInput.Close();

                Task readOutput = PumpAsync(
                    process.StandardOutput, standardOutput, EngineStreamKind.StandardOutput, progress);
                Task readError = PumpAsync(
                    process.StandardError, standardError, EngineStreamKind.StandardError, progress);

                bool timedOut = false;
                bool canceled = false;

                using (var timeoutSource = new CancellationTokenSource(request.Timeout))
                using (var linkedSource = CancellationTokenSource.CreateLinkedTokenSource(
                    timeoutSource.Token, cancellationToken))
                {
                    try
                    {
                        await process.WaitForExitAsync(linkedSource.Token).ConfigureAwait(false);
                    }
                    catch (OperationCanceledException)
                    {
                        canceled = cancellationToken.IsCancellationRequested;
                        timedOut = !canceled;

                        await TerminateAsync(process).ConfigureAwait(false);
                    }
                }

                await WaitForPumpsAsync(readOutput, readError).ConfigureAwait(false);
                stopwatch.Stop();

                int? exitCode = null;
                if (!timedOut && !canceled)
                {
                    exitCode = process.ExitCode;
                }

                return new EngineProcessResult(
                    exitCode,
                    standardOutput.ToText(),
                    standardError.ToText(),
                    standardOutput.Truncated,
                    standardError.Truncated,
                    timedOut,
                    canceled,
                    null,
                    stopwatch.Elapsed);
            }
        }

        private static async Task PumpAsync(
            StreamReader reader,
            BoundedStreamCapture capture,
            EngineStreamKind stream,
            IProgress<EngineJobProgress>? progress)
        {
            var splitter = new LineSplitter(line =>
                progress?.Report(EngineJobProgress.ForLine(EngineJobState.Running, stream, line)));

            var buffer = new char[4096];

            try
            {
                while (true)
                {
                    int read = await reader.ReadAsync(buffer, 0, buffer.Length).ConfigureAwait(false);
                    if (read <= 0)
                    {
                        break;
                    }

                    capture.Append(buffer, 0, read);
                    splitter.Append(buffer, 0, read);
                }
            }
            catch (Exception exception) when (exception is IOException || exception is ObjectDisposedException)
            {
                // The stream was closed underneath us because the process was killed. Whatever was
                // already captured stays captured; there is nothing further to read and nothing to hide.
            }

            splitter.Complete();
        }

        private static async Task TerminateAsync(System.Diagnostics.Process process)
        {
            try
            {
                if (!process.HasExited)
                {
                    process.Kill(entireProcessTree: true);
                }
            }
            catch (Exception exception) when (
                exception is InvalidOperationException
                || exception is NotSupportedException
                || exception is Win32Exception)
            {
                // The process already exited, or the platform refused the kill. Either way the wait
                // below is what decides whether it is really gone.
            }

            using (var grace = new CancellationTokenSource(TerminationGrace))
            {
                try
                {
                    await process.WaitForExitAsync(grace.Token).ConfigureAwait(false);
                }
                catch (OperationCanceledException)
                {
                    // The process outlived the grace period. Reporting the cancelled result is still
                    // correct and honest: the launcher stopped waiting, and says so.
                }
            }
        }

        private static async Task WaitForPumpsAsync(Task readOutput, Task readError)
        {
            using (var grace = new CancellationTokenSource(TerminationGrace))
            {
                Task pumps = Task.WhenAll(readOutput, readError);
                Task completed = await Task.WhenAny(pumps, Task.Delay(Timeout.Infinite, grace.Token))
                    .ConfigureAwait(false);

                if (ReferenceEquals(completed, pumps))
                {
                    await pumps.ConfigureAwait(false);
                }
            }
        }

        private static EngineProcessResult StartFailure(string message, TimeSpan duration) =>
            new EngineProcessResult(
                null, string.Empty, string.Empty, false, false, false, false, message, duration);
    }
}
