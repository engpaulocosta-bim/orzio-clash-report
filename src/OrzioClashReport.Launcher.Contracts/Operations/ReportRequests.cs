using System.Collections.Generic;

namespace OrzioClashReport.Launcher.Contracts.Operations
{
    /// <summary>
    /// Single-run grouped HTML report from one Clash Detective XML export.
    /// The engine takes the XML positionally and has no subcommand for this workflow.
    /// </summary>
    public sealed class QuickReportRequest : LauncherOperationRequest
    {
        public QuickReportRequest(string inputXmlPath, string outputHtmlPath)
        {
            InputXmlPath = RequestValues.RequiredPath(inputXmlPath, nameof(inputXmlPath));
            OutputHtmlPath = RequestValues.RequiredAbsoluteOutputPath(outputHtmlPath, nameof(outputHtmlPath));
        }

        public string InputXmlPath { get; }

        public string OutputHtmlPath { get; }

        public override LauncherOperationKind Kind => LauncherOperationKind.QuickReport;

        public override string? OutputPath => OutputHtmlPath;

        public override LauncherOperationRequest WithOutputPath(string outputPath) =>
            new QuickReportRequest(InputXmlPath, outputPath);
    }

    /// <summary>One immutable coordination-run snapshot from an XML export plus its declared manifest.</summary>
    public sealed class CreateSnapshotRequest : LauncherOperationRequest
    {
        public CreateSnapshotRequest(string xmlPath, string manifestPath, string outputSnapshotPath)
        {
            XmlPath = RequestValues.RequiredPath(xmlPath, nameof(xmlPath));
            ManifestPath = RequestValues.RequiredPath(manifestPath, nameof(manifestPath));
            OutputSnapshotPath = RequestValues.RequiredAbsoluteOutputPath(outputSnapshotPath, nameof(outputSnapshotPath));
        }

        public string XmlPath { get; }

        public string ManifestPath { get; }

        public string OutputSnapshotPath { get; }

        public override LauncherOperationKind Kind => LauncherOperationKind.CreateSnapshot;

        public override string? OutputPath => OutputSnapshotPath;

        public override LauncherOperationRequest WithOutputPath(string outputPath) =>
            new CreateSnapshotRequest(XmlPath, ManifestPath, outputPath);
    }

    /// <summary>
    /// Two-run comparison from explicit previous/current XML exports and manifests.
    /// Previous and current are explicit roles; the launcher never infers order.
    /// </summary>
    public sealed class CompareRunsRequest : LauncherOperationRequest
    {
        public CompareRunsRequest(
            string previousXmlPath,
            string previousManifestPath,
            string currentXmlPath,
            string currentManifestPath,
            string outputHtmlPath)
        {
            PreviousXmlPath = RequestValues.RequiredPath(previousXmlPath, nameof(previousXmlPath));
            PreviousManifestPath = RequestValues.RequiredPath(previousManifestPath, nameof(previousManifestPath));
            CurrentXmlPath = RequestValues.RequiredPath(currentXmlPath, nameof(currentXmlPath));
            CurrentManifestPath = RequestValues.RequiredPath(currentManifestPath, nameof(currentManifestPath));
            OutputHtmlPath = RequestValues.RequiredAbsoluteOutputPath(outputHtmlPath, nameof(outputHtmlPath));
        }

        public string PreviousXmlPath { get; }

        public string PreviousManifestPath { get; }

        public string CurrentXmlPath { get; }

        public string CurrentManifestPath { get; }

        public string OutputHtmlPath { get; }

        public override LauncherOperationKind Kind => LauncherOperationKind.CompareRuns;

        public override string? OutputPath => OutputHtmlPath;

        public override LauncherOperationRequest WithOutputPath(string outputPath) =>
            new CompareRunsRequest(
                PreviousXmlPath, PreviousManifestPath, CurrentXmlPath, CurrentManifestPath, outputPath);
    }

    /// <summary>Comparison of two already persisted snapshots in explicit previous/current roles.</summary>
    public sealed class CompareSnapshotsRequest : LauncherOperationRequest
    {
        public CompareSnapshotsRequest(
            string previousSnapshotPath, string currentSnapshotPath, string outputHtmlPath)
        {
            PreviousSnapshotPath = RequestValues.RequiredPath(previousSnapshotPath, nameof(previousSnapshotPath));
            CurrentSnapshotPath = RequestValues.RequiredPath(currentSnapshotPath, nameof(currentSnapshotPath));
            OutputHtmlPath = RequestValues.RequiredAbsoluteOutputPath(outputHtmlPath, nameof(outputHtmlPath));
        }

        public string PreviousSnapshotPath { get; }

        public string CurrentSnapshotPath { get; }

        public string OutputHtmlPath { get; }

        public override LauncherOperationKind Kind => LauncherOperationKind.CompareSnapshots;

        public override string? OutputPath => OutputHtmlPath;

        public override LauncherOperationRequest WithOutputPath(string outputPath) =>
            new CompareSnapshotsRequest(PreviousSnapshotPath, CurrentSnapshotPath, outputPath);
    }

    /// <summary>
    /// An explicitly ordered run index. The declared order is the only sequence authority:
    /// the launcher never sorts by date, name, or revision, and never silently drops duplicates.
    /// </summary>
    public sealed class IndexSnapshotsRequest : LauncherOperationRequest
    {
        public IndexSnapshotsRequest(IReadOnlyList<string> snapshotPaths, string outputIndexPath)
        {
            SnapshotPaths = RequestValues.RequiredOrderedPaths(snapshotPaths, 1, nameof(snapshotPaths));
            OutputIndexPath = RequestValues.RequiredAbsoluteOutputPath(outputIndexPath, nameof(outputIndexPath));
        }

        /// <summary>Snapshot paths in exactly the order the human declared them.</summary>
        public IReadOnlyList<string> SnapshotPaths { get; }

        public string OutputIndexPath { get; }

        public override LauncherOperationKind Kind => LauncherOperationKind.IndexSnapshots;

        public override string? OutputPath => OutputIndexPath;

        public override LauncherOperationRequest WithOutputPath(string outputPath) =>
            new IndexSnapshotsRequest(SnapshotPaths, outputPath);
    }

    /// <summary>Adjacent-pair longitudinal comparison driven by an explicit run index.</summary>
    public sealed class CompareIndexRequest : LauncherOperationRequest
    {
        public CompareIndexRequest(string indexPath, string outputHtmlPath)
        {
            IndexPath = RequestValues.RequiredPath(indexPath, nameof(indexPath));
            OutputHtmlPath = RequestValues.RequiredAbsoluteOutputPath(outputHtmlPath, nameof(outputHtmlPath));
        }

        public string IndexPath { get; }

        public string OutputHtmlPath { get; }

        public override LauncherOperationKind Kind => LauncherOperationKind.CompareIndex;

        public override string? OutputPath => OutputHtmlPath;

        public override LauncherOperationRequest WithOutputPath(string outputPath) =>
            new CompareIndexRequest(IndexPath, outputPath);
    }
}
