using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using OrzioClashReport.Launcher.Contracts.Jobs;

namespace OrzioClashReport.Launcher.Infrastructure.Processes
{
    /// <summary>What to run, with which arguments, from where, and for how long.</summary>
    public sealed class ProcessRunSpecification
    {
        public ProcessRunSpecification(
            string executablePath,
            IReadOnlyList<string> arguments,
            string workingDirectory,
            TimeSpan? timeout = null)
        {
            if (string.IsNullOrWhiteSpace(executablePath))
            {
                throw new ArgumentException("Executable path must not be empty or whitespace.", nameof(executablePath));
            }

            if (arguments == null)
            {
                throw new ArgumentNullException(nameof(arguments));
            }

            if (string.IsNullOrWhiteSpace(workingDirectory))
            {
                throw new ArgumentException(
                    "Working directory must not be empty or whitespace.", nameof(workingDirectory));
            }

            if (timeout.HasValue && timeout.Value <= TimeSpan.Zero)
            {
                throw new ArgumentOutOfRangeException(nameof(timeout), timeout, "Timeout must be positive.");
            }

            var copy = new List<string>(arguments.Count);
            for (int i = 0; i < arguments.Count; i++)
            {
                copy.Add(arguments[i] ?? throw new ArgumentException(
                    "Arguments must not contain null entries.", nameof(arguments)));
            }

            ExecutablePath = executablePath.Trim();
            Arguments = new ReadOnlyCollection<string>(copy);
            WorkingDirectory = workingDirectory.Trim();
            Timeout = timeout;
        }

        public string ExecutablePath { get; }

        /// <summary>Passed element by element; never joined and never handed to a shell.</summary>
        public IReadOnlyList<string> Arguments { get; }

        /// <summary>
        /// Where the process runs. This is a data directory chosen by the caller, never the
        /// launcher's own installation directory.
        /// </summary>
        public string WorkingDirectory { get; }

        public TimeSpan? Timeout { get; }
    }

    /// <summary>How a process run ended.</summary>
    public enum ProcessRunOutcome
    {
        /// <summary>The process exited on its own.</summary>
        Exited = 0,

        /// <summary>The process did not finish in time and was terminated.</summary>
        TimedOut = 1,

        /// <summary>A human cancelled and the process was terminated.</summary>
        Canceled = 2,

        /// <summary>The process could not be started at all.</summary>
        StartFailed = 3,
    }

    /// <summary>Immutable outcome of one process run.</summary>
    public sealed class ProcessRunResult
    {
        public ProcessRunResult(
            ProcessRunOutcome outcome,
            int? exitCode,
            string standardOutput,
            string standardError,
            bool outputTruncated,
            TimeSpan duration,
            string? startFailureMessage = null)
        {
            Outcome = outcome;
            ExitCode = exitCode;
            StandardOutput = standardOutput ?? string.Empty;
            StandardError = standardError ?? string.Empty;
            OutputTruncated = outputTruncated;
            Duration = duration;
            StartFailureMessage = startFailureMessage;
        }

        public ProcessRunOutcome Outcome { get; }

        public int? ExitCode { get; }

        public string StandardOutput { get; }

        public string StandardError { get; }

        public bool OutputTruncated { get; }

        public TimeSpan Duration { get; }

        public string? StartFailureMessage { get; }
    }

    /// <summary>
    /// Runs the engine as a child process. There is no shell, no command-line string, and no console
    /// window: arguments go through <see cref="ProcessStartInfo.ArgumentList"/>, both streams are
    /// redirected and bounded, and cancelling kills the whole process tree and waits for it to die.
    /// </summary>
    public sealed class ProcessJobRunner
    {
        private static readonly UTF8Encoding Utf8NoBom = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false);

        public async Task<ProcessRunResult> RunAsync(
            ProcessRunSpecification specification,
            IProgress<EngineOutputChunk>? progress,
            CancellationToken cancellationToken)
        {
            if (specification == null)
            {
                throw new ArgumentNullException(nameof(specification));
            }

            var startInfo = new ProcessStartInfo
            {
                FileName = specification.ExecutablePath,
                WorkingDirectory = specification.WorkingDirectory,
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                RedirectStandardInput = false,
                StandardOutputEncoding = Utf8NoBom,
                StandardErrorEncoding = Utf8NoBom,
            };

            foreach (string argument in specification.Arguments)
            {
                startInfo.ArgumentList.Add(argument);
            }

            var standardOutput = new BoundedStreamBuffer();
            var standardError = new BoundedStreamBuffer();
            var stopwatch = Stopwatch.StartNew();

            using var process = new Process { StartInfo = startInfo, EnableRaisingEvents = true };
            var exited = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
            process.Exited += (_, _) => exited.TrySetResult(true);

            try
            {
                if (!process.Start())
                {
                    stopwatch.Stop();
                    return new ProcessRunResult(
                        ProcessRunOutcome.StartFailed,
                        exitCode: null,
                        string.Empty,
                        string.Empty,
                        outputTruncated: false,
                        stopwatch.Elapsed,
                        "The engine process could not be started.");
                }
            }
            catch (Exception ex) when (ex is System.ComponentModel.Win32Exception
                                        or InvalidOperationException
                                        or PlatformNotSupportedException
                                        or FileNotFoundException)
            {
                stopwatch.Stop();
                return new ProcessRunResult(
                    ProcessRunOutcome.StartFailed,
                    exitCode: null,
                    string.Empty,
                    string.Empty,
                    outputTruncated: false,
                    stopwatch.Elapsed,
                    ex.Message);
            }

            Task readOutput = PumpAsync(
                process.StandardOutput, EngineOutputStream.StandardOutput, standardOutput, progress);
            Task readError = PumpAsync(
                process.StandardError, EngineOutputStream.StandardError, standardError, progress);

            ProcessRunOutcome outcome = await WaitAsync(
                process, exited.Task, specification.Timeout, cancellationToken).ConfigureAwait(false);

            // Both pumps end when the streams close, which happens once the process is gone.
            await Task.WhenAll(readOutput, readError).ConfigureAwait(false);
            stopwatch.Stop();

            int? exitCode = null;
            if (outcome == ProcessRunOutcome.Exited)
            {
                exitCode = process.ExitCode;
            }

            return new ProcessRunResult(
                outcome,
                exitCode,
                standardOutput.ToString(),
                standardError.ToString(),
                standardOutput.Truncated || standardError.Truncated,
                stopwatch.Elapsed);
        }

        private static async Task<ProcessRunOutcome> WaitAsync(
            Process process, Task exited, TimeSpan? timeout, CancellationToken cancellationToken)
        {
            using var timeoutSource = new CancellationTokenSource();
            if (timeout.HasValue)
            {
                timeoutSource.CancelAfter(timeout.Value);
            }

            using var linked = CancellationTokenSource.CreateLinkedTokenSource(
                cancellationToken, timeoutSource.Token);

            var completion = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
            using (linked.Token.Register(() => completion.TrySetResult(true)))
            {
                await Task.WhenAny(exited, completion.Task).ConfigureAwait(false);
            }

            if (exited.IsCompleted)
            {
                return ProcessRunOutcome.Exited;
            }

            KillTree(process);

            // The engine is not gone until it is actually gone; the caller may be about to inspect
            // the very file it was writing.
            await exited.ConfigureAwait(false);

            return timeoutSource.IsCancellationRequested && !cancellationToken.IsCancellationRequested
                ? ProcessRunOutcome.TimedOut
                : ProcessRunOutcome.Canceled;
        }

        private static void KillTree(Process process)
        {
            try
            {
                process.Kill(entireProcessTree: true);
            }
            catch (InvalidOperationException)
            {
                // The process exited between the wait and the kill. Nothing to terminate.
            }
            catch (NotSupportedException)
            {
                // A platform without process-tree termination still gets the direct child killed.
                TryKillDirect(process);
            }
            catch (System.ComponentModel.Win32Exception)
            {
                TryKillDirect(process);
            }
        }

        private static void TryKillDirect(Process process)
        {
            try
            {
                process.Kill();
            }
            catch (InvalidOperationException)
            {
                // Already gone.
            }
            catch (System.ComponentModel.Win32Exception)
            {
                // The operating system refused; the wait below still observes the real exit.
            }
        }

        private static async Task PumpAsync(
            StreamReader reader,
            EngineOutputStream stream,
            BoundedStreamBuffer buffer,
            IProgress<EngineOutputChunk>? progress)
        {
            while (true)
            {
                string? line;
                try
                {
                    line = await reader.ReadLineAsync().ConfigureAwait(false);
                }
                catch (Exception ex) when (ex is IOException or ObjectDisposedException)
                {
                    // The stream closed under us because the process was terminated. Everything read
                    // so far is still valid and is reported as-is.
                    return;
                }

                if (line == null)
                {
                    return;
                }

                buffer.AppendLine(line);
                progress?.Report(new EngineOutputChunk(stream, line));
            }
        }
    }
}
