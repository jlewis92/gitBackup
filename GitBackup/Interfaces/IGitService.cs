using System.IO.Abstractions;

namespace GitBackup.Interfaces
{
    public interface IGitService
    {
        /// <summary>
        ///  Adds a file to a git repository
        /// </summary>
        /// <param name="fileName">the file to add</param>
        /// <returns>The directory of the repository that the file has been moved to</returns>
        IDirectoryInfo AddFile(IFileInfo fileName);

        /// <summary>
        /// Updates a file in a git repository
        /// </summary>
        /// <param name="fileInfo">The file to update</param>
        /// <returns>The directory location where the file has been updated</returns>
        IDirectoryInfo UpdateFile(IFileInfo fileInfo, IDirectoryInfo directoryToUpdate);

        /// <summary>
        /// Updates a remote repository with the latest changes
        /// </summary>
        /// <param name="directory">The directory of the current repository to update</param>
        void UpdateRemote(string directory);

        /// <summary>
        /// Updates the manifest file in the database
        /// </summary>
        /// <param name="fileInfo">The location of the database</param>
        /// <param name="directoryInfo">The directory where we want to update the manifest</param>
        void UpdateManifest(IFileInfo fileInfo, IDirectoryInfo directoryInfo);

        /// <summary>
        /// Downloads all the repositories from the remote folder
        /// </summary>
        /// <returns>A list of restored repositories</returns>
        List<IDirectoryInfo> DownloadToRestoreLocation(string manifestName);
    }
}
