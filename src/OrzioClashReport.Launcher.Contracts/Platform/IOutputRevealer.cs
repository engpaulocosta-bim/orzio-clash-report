using System.Threading;
using System.Threading.Tasks;

namespace OrzioClashReport.Launcher.Contracts.Platform
{
    /// <summary>
    /// Hands a produced file to the operating system: open it with its default handler, or show it in
    /// the file manager. Returns false when the platform declined; it never throws for a missing file.
    /// </summary>
    public interface IOutputRevealer
    {
        Task<bool> OpenAsync(string path, CancellationToken cancellationToken);

        Task<bool> RevealInFolderAsync(string path, CancellationToken cancellationToken);
    }
}
