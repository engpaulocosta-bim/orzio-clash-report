using System;
using System.Globalization;
using System.IO;
using OrzioClashReport.Launcher.Contracts.Settings;

namespace OrzioClashReport.Launcher.Desktop.ViewModels
{
    /// <summary>
    /// One previously produced output on the home screen. The full path is kept so the file can be
    /// opened, but only the file name and its folder name are shown.
    /// </summary>
    public sealed class RecentOutputViewModel
    {
        public RecentOutputViewModel(RecentOutputItem item)
        {
            if (item == null)
            {
                throw new ArgumentNullException(nameof(item));
            }

            Item = item;
            FileName = Path.GetFileName(item.Path);

            string? directory = Path.GetDirectoryName(item.Path);
            FolderName = string.IsNullOrEmpty(directory) ? string.Empty : Path.GetFileName(directory);

            CompletedAt = item.CompletedAtUtc.ToLocalTime()
                .ToString("yyyy-MM-dd HH:mm", CultureInfo.InvariantCulture);
        }

        public RecentOutputItem Item { get; }

        public string FileName { get; }

        public string FolderName { get; }

        public string CompletedAt { get; }
    }
}
