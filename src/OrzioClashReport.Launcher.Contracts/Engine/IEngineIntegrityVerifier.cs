using System.Threading;
using System.Threading.Tasks;

namespace OrzioClashReport.Launcher.Contracts.Engine
{
    /// <summary>
    /// Compares the engine executable against the SHA-256 recorded in its packaged manifest. A
    /// verification failure is reported, never repaired: the launcher does not re-download, patch, or
    /// rewrite the manifest.
    /// </summary>
    public interface IEngineIntegrityVerifier
    {
        Task<EngineIntegrityResult> VerifyAsync(EngineLocation location, CancellationToken cancellationToken);
    }
}
