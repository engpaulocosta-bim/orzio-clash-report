namespace OrzioClashReport.Launcher.Contracts.Platform
{
    /// <summary>
    /// The narrow filesystem questions the launcher needs answered: does this path already exist, and
    /// how large is it. Nothing here reads, writes, moves, or deletes a file — the engine owns every
    /// artifact it produces, and the launcher never stages output on its behalf.
    /// </summary>
    public interface IFileProbe
    {
        bool FileExists(string path);

        bool DirectoryExists(string path);

        /// <summary>Size in bytes, or <c>-1</c> when the file does not exist or cannot be measured.</summary>
        long GetFileSizeInBytes(string path);
    }
}
