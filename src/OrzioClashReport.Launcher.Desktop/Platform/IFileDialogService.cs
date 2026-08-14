using System.Collections.Generic;
using System.Threading.Tasks;

namespace OrzioClashReport.Launcher.Desktop.Platform
{
    /// <summary>
    /// The file pickers, expressed so view models never touch a toolkit type. The implementation uses
    /// <c>TopLevel.StorageProvider</c>; there is no bespoke Windows dialog code anywhere.
    /// </summary>
    public interface IFileDialogService
    {
        Task<string?> PickOpenFileAsync(string title, FilePickerFileKind kind, string? startDirectory);

        Task<IReadOnlyList<string>> PickOpenFilesAsync(string title, FilePickerFileKind kind, string? startDirectory);

        Task<string?> PickSaveFileAsync(
            string title, FilePickerFileKind kind, string suggestedFileName, string? startDirectory);
    }

    /// <summary>The file kinds the launcher works with, so filters stay consistent across every form.</summary>
    public enum FilePickerFileKind
    {
        NavisworksClashXml = 0,
        HtmlReport = 1,
        RunManifestJson = 2,
        RunSnapshotJson = 3,
        RunIndexJson = 4,
        ProjectCatalogJson = 5,
        IdentityGovernanceJson = 6,
    }
}
