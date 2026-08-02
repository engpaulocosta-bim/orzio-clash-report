using System;
using System.IO;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Platform.Storage;
using OrzioClashReport.Launcher.Contracts.Ports;

namespace OrzioClashReport.Launcher.Desktop.Platform
{
    /// <summary>
    /// Opens produced files through Avalonia's own launcher. The user interface never starts a
    /// process, and no platform-specific shell command is needed for this.
    /// </summary>
    public sealed class TopLevelOutputRevealer : IOutputRevealer
    {
        private readonly Func<TopLevel?> _topLevel;

        public TopLevelOutputRevealer(Func<TopLevel?> topLevel)
        {
            _topLevel = topLevel ?? throw new ArgumentNullException(nameof(topLevel));
        }

        public async Task OpenAsync(string path)
        {
            TopLevel? topLevel = _topLevel();
            if (topLevel == null || string.IsNullOrWhiteSpace(path) || !File.Exists(path))
            {
                return;
            }

            await topLevel.Launcher.LaunchFileInfoAsync(new FileInfo(path)).ConfigureAwait(true);
        }

        public async Task RevealAsync(string path)
        {
            TopLevel? topLevel = _topLevel();
            if (topLevel == null || string.IsNullOrWhiteSpace(path))
            {
                return;
            }

            string? directory = Path.GetDirectoryName(path);
            if (directory == null || !Directory.Exists(directory))
            {
                return;
            }

            await topLevel.Launcher.LaunchDirectoryInfoAsync(new DirectoryInfo(directory)).ConfigureAwait(true);
        }
    }
}
