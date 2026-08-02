using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Platform.Storage;
using OrzioClashReport.Launcher.Desktop.Localization;

namespace OrzioClashReport.Launcher.Desktop.Platform
{
    /// <summary>The file kinds the launcher asks a human to choose.</summary>
    public enum PickedFileKind
    {
        ClashXml = 0,
        RunManifestJson = 1,
        SnapshotJson = 2,
        RunIndexJson = 3,
        ProjectCatalogJson = 4,
        IdentityGovernanceJson = 5,
        HtmlReport = 6,
    }

    /// <summary>Choosing inputs and destinations. Backed by the platform's own picker.</summary>
    public interface IFileDialogs
    {
        Task<string?> PickOpenFileAsync(string title, PickedFileKind kind);

        Task<IReadOnlyList<string>> PickOpenFilesAsync(string title, PickedFileKind kind);

        Task<string?> PickSaveFileAsync(string title, PickedFileKind kind, string suggestedFileName);
    }

    /// <summary>
    /// Uses <see cref="TopLevel.StorageProvider"/> rather than a Windows-specific dialog service:
    /// Avalonia already provides the native picker on every target.
    /// </summary>
    public sealed class StorageProviderFileDialogs : IFileDialogs
    {
        private readonly Func<TopLevel?> _topLevel;

        public StorageProviderFileDialogs(Func<TopLevel?> topLevel)
        {
            _topLevel = topLevel ?? throw new ArgumentNullException(nameof(topLevel));
        }

        public async Task<string?> PickOpenFileAsync(string title, PickedFileKind kind)
        {
            IReadOnlyList<string> picked = await PickOpenFilesAsync(title, kind, allowMultiple: false)
                .ConfigureAwait(true);

            return picked.Count > 0 ? picked[0] : null;
        }

        public Task<IReadOnlyList<string>> PickOpenFilesAsync(string title, PickedFileKind kind) =>
            PickOpenFilesAsync(title, kind, allowMultiple: true);

        public async Task<string?> PickSaveFileAsync(string title, PickedFileKind kind, string suggestedFileName)
        {
            IStorageProvider? storage = _topLevel()?.StorageProvider;
            if (storage == null)
            {
                return null;
            }

            IStorageFile? file = await storage.SaveFilePickerAsync(new FilePickerSaveOptions
            {
                Title = title,
                SuggestedFileName = suggestedFileName,
                DefaultExtension = DefaultExtensionFor(kind),
                ShowOverwritePrompt = false,
                FileTypeChoices = new[] { FileTypeFor(kind) },
            }).ConfigureAwait(true);

            return LocalPathOf(file);
        }

        private async Task<IReadOnlyList<string>> PickOpenFilesAsync(
            string title, PickedFileKind kind, bool allowMultiple)
        {
            IStorageProvider? storage = _topLevel()?.StorageProvider;
            if (storage == null)
            {
                return Array.Empty<string>();
            }

            IReadOnlyList<IStorageFile> files = await storage.OpenFilePickerAsync(new FilePickerOpenOptions
            {
                Title = title,
                AllowMultiple = allowMultiple,
                FileTypeFilter = new[] { FileTypeFor(kind) },
            }).ConfigureAwait(true);

            var paths = new List<string>(files.Count);
            foreach (IStorageFile file in files)
            {
                string? path = LocalPathOf(file);
                if (path != null)
                {
                    paths.Add(path);
                }
            }

            return paths;
        }

        private static string? LocalPathOf(IStorageFile? file)
        {
            // The engine works with real paths; a location the platform cannot express as one is
            // refused here rather than half-handled later.
            return file?.TryGetLocalPath();
        }

        private static FilePickerFileType Json(string key) =>
            new FilePickerFileType(Localizer.Instance.Get(key)) { Patterns = new[] { "*.json" } };

        private static string DefaultExtensionFor(PickedFileKind kind) =>
            kind == PickedFileKind.ClashXml ? "xml" : kind == PickedFileKind.HtmlReport ? "html" : "json";

        private static FilePickerFileType FileTypeFor(PickedFileKind kind)
        {
            switch (kind)
            {
                case PickedFileKind.ClashXml:
                    return new FilePickerFileType(Localizer.Instance.Get("Picker.ClashXml"))
                    {
                        Patterns = new[] { "*.xml" },
                    };
                case PickedFileKind.HtmlReport:
                    return new FilePickerFileType(Localizer.Instance.Get("Picker.HtmlReport"))
                    {
                        Patterns = new[] { "*.html" },
                    };
                case PickedFileKind.RunManifestJson:
                    return Json("Picker.RunManifestJson");
                case PickedFileKind.SnapshotJson:
                    return Json("Picker.SnapshotJson");
                case PickedFileKind.RunIndexJson:
                    return Json("Picker.RunIndexJson");
                case PickedFileKind.ProjectCatalogJson:
                    return Json("Picker.ProjectCatalogJson");
                case PickedFileKind.IdentityGovernanceJson:
                    return Json("Picker.IdentityGovernanceJson");
                default:
                    throw new ArgumentOutOfRangeException(nameof(kind), kind, "Unknown file kind.");
            }
        }
    }
}
