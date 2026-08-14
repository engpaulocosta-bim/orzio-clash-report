using System;
using OrzioClashReport.Launcher.Contracts.Engine;

namespace OrzioClashReport.Launcher.Infrastructure.Engine
{
    /// <summary>Reads the expected engine version from the packaged <c>engine-manifest.json</c>.</summary>
    public sealed class ManifestEngineExpectationSource : IEngineExpectationSource
    {
        private readonly EngineManifestReader _manifestReader;

        public ManifestEngineExpectationSource(EngineManifestReader manifestReader)
        {
            _manifestReader = manifestReader ?? throw new ArgumentNullException(nameof(manifestReader));
        }

        public string? ReadExpectedVersion(EngineLocation location)
        {
            if (location == null)
            {
                throw new ArgumentNullException(nameof(location));
            }

            return _manifestReader.TryRead(location.ManifestPath, out _)?.EngineVersion;
        }
    }
}
