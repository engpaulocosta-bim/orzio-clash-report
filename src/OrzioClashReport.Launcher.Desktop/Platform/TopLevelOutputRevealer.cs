using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Platform.Storage;
using OrzioClashReport.Launcher.Contracts.Platform;

namespace OrzioClashReport.Launcher.Desktop.Platform
{
    /// <summary>
    /// Opens and reveals produced files through the toolkit's own launcher service. There is no
    /// Windows-specific shell code here: <c>TopLevel.Launcher</c> already does exactly this, and using
    /// it keeps the launcher free of a second process-starting path.
    /// </summary>
    public sealed class TopLevelOutputRevealer : IOutputRevealer
    {
        private readonly Func<TopLevel?> _topLevelAccessor;

        public TopLevelOutputRevealer(Func<TopLevel?> topLevelAccessor)
        {
            _topLevelAccessor = topLevelAccessor ?? throw new ArgumentNullException(nameof(topLevelAccessor));
        }

        public async Task<bool> OpenAsync(string path, CancellationToken cancellationToken)
        {
            TopLevel? topLevel = _topLevelAccessor();
            if (topLevel == null || !File.Exists(path))
            {
                return false;
            }

            IStorageFile? file = await topLevel.StorageProvider.TryGetFileFromPathAsync(path).ConfigureAwait(true);
            if (file == null)
            {
                return false;
            }

            return await topLevel.Launcher.LaunchFileAsync(file).ConfigureAwait(true);
        }

        public async Task<bool> RevealInFolderAsync(string path, CancellationToken cancellationToken)
        {
            TopLevel? topLevel = _topLevelAccessor();
            if (topLevel == null)
            {
                return false;
            }

            string? directory = Directory.Exists(path) ? path : Path.GetDirectoryName(path);
            if (string.IsNullOrEmpty(directory) || !Directory.Exists(directory))
            {
                return false;
            }

            IStorageFolder? folder =
                await topLevel.StorageProvider.TryGetFolderFromPathAsync(directory).ConfigureAwait(true);

            if (folder == null)
            {
                return false;
            }

            return await topLevel.Launcher.LaunchFileAsync(folder).ConfigureAwait(true);
        }
    }
}
