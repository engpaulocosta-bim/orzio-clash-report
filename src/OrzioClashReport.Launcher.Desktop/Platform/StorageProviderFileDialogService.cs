using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Platform.Storage;

namespace OrzioClashReport.Launcher.Desktop.Platform
{
    /// <summary>
    /// File pickers backed by <c>TopLevel.StorageProvider</c>. Paths come back absolute, which is what
    /// every <c>-o</c> destination requires.
    /// </summary>
    public sealed class StorageProviderFileDialogService : IFileDialogService
    {
        private readonly Func<TopLevel?> _topLevelAccessor;

        public StorageProviderFileDialogService(Func<TopLevel?> topLevelAccessor)
        {
            _topLevelAccessor = topLevelAccessor ?? throw new ArgumentNullException(nameof(topLevelAccessor));
        }

        public async Task<string?> PickOpenFileAsync(
            string title, FilePickerFileKind kind, string? startDirectory)
        {
            IReadOnlyList<string> files = await PickOpenFilesCoreAsync(title, kind, startDirectory, false)
                .ConfigureAwait(true);

            return files.Count == 0 ? null : files[0];
        }

        public Task<IReadOnlyList<string>> PickOpenFilesAsync(
            string title, FilePickerFileKind kind, string? startDirectory) =>
            PickOpenFilesCoreAsync(title, kind, startDirectory, true);

        public async Task<string?> PickSaveFileAsync(
            string title, FilePickerFileKind kind, string suggestedFileName, string? startDirectory)
        {
            TopLevel? topLevel = _topLevelAccessor();
            if (topLevel == null)
            {
                return null;
            }

            IStorageFile? file = await topLevel.StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
            {
                Title = title,
                SuggestedFileName = suggestedFileName,
                DefaultExtension = DefaultExtension(kind),
                FileTypeChoices = FiltersFor(kind),
                SuggestedStartLocation = await ResolveStartLocationAsync(topLevel, startDirectory).ConfigureAwait(true),

                // The launcher decides about replacement itself, with an explicit question that names
                // the file. The platform prompt would not distinguish a regenerable report from
                // evidence that must never be overwritten.
                ShowOverwritePrompt = false,
            }).ConfigureAwait(true);

            return file?.TryGetLocalPath();
        }

        private async Task<IReadOnlyList<string>> PickOpenFilesCoreAsync(
            string title, FilePickerFileKind kind, string? startDirectory, bool allowMultiple)
        {
            TopLevel? topLevel = _topLevelAccessor();
            if (topLevel == null)
            {
                return Array.Empty<string>();
            }

            IReadOnlyList<IStorageFile> files = await topLevel.StorageProvider.OpenFilePickerAsync(
                new FilePickerOpenOptions
                {
                    Title = title,
                    AllowMultiple = allowMultiple,
                    FileTypeFilter = FiltersFor(kind),
                    SuggestedStartLocation = await ResolveStartLocationAsync(topLevel, startDirectory).ConfigureAwait(true),
                }).ConfigureAwait(true);

            return files
                .Select(file => file.TryGetLocalPath())
                .Where(path => !string.IsNullOrEmpty(path))
                .Select(path => path!)
                .ToList();
        }

        private static async Task<IStorageFolder?> ResolveStartLocationAsync(TopLevel topLevel, string? startDirectory)
        {
            if (string.IsNullOrWhiteSpace(startDirectory))
            {
                return null;
            }

            return await topLevel.StorageProvider.TryGetFolderFromPathAsync(startDirectory).ConfigureAwait(true);
        }

        private static string DefaultExtension(FilePickerFileKind kind) =>
            kind == FilePickerFileKind.HtmlReport ? "html"
            : kind == FilePickerFileKind.NavisworksClashXml ? "xml"
            : "json";

        private static IReadOnlyList<FilePickerFileType> FiltersFor(FilePickerFileKind kind)
        {
            var all = new FilePickerFileType("Todos os ficheiros") { Patterns = new[] { "*" } };

            switch (kind)
            {
                case FilePickerFileKind.NavisworksClashXml:
                    return new[]
                    {
                        new FilePickerFileType("Export XML do Clash Detective") { Patterns = new[] { "*.xml" } },
                        all,
                    };

                case FilePickerFileKind.HtmlReport:
                    return new[]
                    {
                        new FilePickerFileType("Relatório HTML") { Patterns = new[] { "*.html" } },
                        all,
                    };

                case FilePickerFileKind.RunManifestJson:
                    return new[]
                    {
                        new FilePickerFileType("Run manifest JSON") { Patterns = new[] { "*.json" } },
                        all,
                    };

                case FilePickerFileKind.RunSnapshotJson:
                    return new[]
                    {
                        new FilePickerFileType("Run snapshot JSON") { Patterns = new[] { "*.json" } },
                        all,
                    };

                case FilePickerFileKind.RunIndexJson:
                    return new[]
                    {
                        new FilePickerFileType("Run index JSON") { Patterns = new[] { "*.json" } },
                        all,
                    };

                case FilePickerFileKind.ProjectCatalogJson:
                    return new[]
                    {
                        new FilePickerFileType("Catálogo de projeto JSON") { Patterns = new[] { "*.json" } },
                        all,
                    };

                case FilePickerFileKind.IdentityGovernanceJson:
                    return new[]
                    {
                        new FilePickerFileType("Governança de identidade JSON") { Patterns = new[] { "*.json" } },
                        all,
                    };

                default:
                    return new[] { all };
            }
        }
    }
}
