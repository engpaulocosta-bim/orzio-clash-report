namespace OrzioClashReport.Launcher.Contracts.Operations
{
    /// <summary>One operational project catalog created from an existing run index.</summary>
    public sealed class CreateProjectRequest : LauncherOperationRequest
    {
        public CreateProjectRequest(
            string projectId, string displayName, string indexPath, string reportPath, string outputProjectPath)
        {
            ProjectId = RequestValues.RequiredText(projectId, nameof(projectId));
            DisplayName = RequestValues.RequiredText(displayName, nameof(displayName));
            IndexPath = RequestValues.RequiredPath(indexPath, nameof(indexPath));
            ReportPath = RequestValues.RequiredPath(reportPath, nameof(reportPath));
            OutputProjectPath = RequestValues.RequiredAbsoluteOutputPath(outputProjectPath, nameof(outputProjectPath));
        }

        public string ProjectId { get; }

        public string DisplayName { get; }

        public string IndexPath { get; }

        /// <summary>The longitudinal report destination recorded in the catalog. Not an <c>-o</c> value.</summary>
        public string ReportPath { get; }

        public string OutputProjectPath { get; }

        public override LauncherOperationKind Kind => LauncherOperationKind.CreateProject;

        public override string? OutputPath => OutputProjectPath;

        public override LauncherOperationRequest WithOutputPath(string outputPath) =>
            new CreateProjectRequest(ProjectId, DisplayName, IndexPath, ReportPath, outputPath);
    }

    /// <summary>
    /// Appends exactly one snapshot reference to the end of a project's run index.
    /// The engine has no <c>-o</c> for this operation and does not regenerate HTML.
    /// </summary>
    public sealed class AppendProjectSnapshotRequest : LauncherOperationRequest
    {
        public AppendProjectSnapshotRequest(string projectPath, string snapshotPath)
        {
            ProjectPath = RequestValues.RequiredPath(projectPath, nameof(projectPath));
            SnapshotPath = RequestValues.RequiredPath(snapshotPath, nameof(snapshotPath));
        }

        public string ProjectPath { get; }

        public string SnapshotPath { get; }

        public override LauncherOperationKind Kind => LauncherOperationKind.AppendProjectSnapshot;
    }

    /// <summary>
    /// Regenerates a project's longitudinal report. The engine has no <c>-o</c> for this operation:
    /// the destination comes from the existing catalog, which the launcher reads but never invents.
    /// </summary>
    public sealed class RenderProjectRequest : LauncherOperationRequest
    {
        public RenderProjectRequest(string projectPath)
        {
            ProjectPath = RequestValues.RequiredPath(projectPath, nameof(projectPath));
        }

        public string ProjectPath { get; }

        public override LauncherOperationKind Kind => LauncherOperationKind.RenderProject;
    }
}
