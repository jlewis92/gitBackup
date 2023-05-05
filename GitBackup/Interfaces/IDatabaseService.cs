using GitBackup.Models;

namespace GitBackup.Interfaces
{
    public interface IDatabaseService
    {
        /// <summary>
        /// Retrieves the database location
        /// </summary>
        string BackupDatabaseLocation { get; }

        /// <summary>
        /// Retrieves the database location
        /// </summary>
        string RestoreDatabaseLocation { get; }

        /// <summary>
        /// Retrieves the name of the database
        /// </summary>
        string DatabaseName { get; }

        /// <summary>
        /// Configures a database
        /// </summary>
        void Configure();

        /// <summary>
        /// Checks the manifest database for if there's been changes or additions since the last time a backup was run
        /// </summary>
        /// <param name="filesBeingBackedUp">The files that need to be backed up</param>
        /// <returns>A list of files that have been added or modified since the last time an update was run</returns>
        (List<string> FilesToUpdate, List<string> FilesToAdd) CheckFilesInManifestForChanges(List<string> filesBeingBackedUp);

        /// <summary>
        /// Adds an entry to the database
        /// </summary>
        /// <param name="manifestContext">The entry to add to the manifest</param>
        void AddManifestEntry(ManifestEntry manifestEntry);

        /// <summary>
        /// Updates an entry in the database
        /// </summary>
        /// <param name="manifestContext">The entry to update in  the manifest</param>
        void UpdateManifestEntry(ManifestEntry manifestEntry);

        /// <summary>
        /// Retrieves a manifest entry from the database
        /// </summary>
        /// <param name="fileName">The name of the file to retrieve a manifest for</param>
        /// <returns>A manifest entry</returns>
        ManifestEntry GetManifestEntry(string fileName);

        /// <summary>
        /// Retrieves a repository entry from the database
        /// </summary>
        /// <param name="repositoryName">The name of the repository</param>
        /// <returns>A Repository entry</returns>
        RepositoryEntry? GetRepositoryEntry(string repositoryName);

        /// <summary>
        /// Retrieves a repository entry from the database
        /// </summary>
        /// <param name="repositoryName">The name of the repository</param>
        /// <returns>A Repository entry</returns>
        RepositoryEntry GetRepositoryEntryById(int repositoryEntryId);

        /// <summary>
        /// Inserts a repository entry into the database
        /// </summary>
        /// <param name="repositoryEntry">The repository entry to add</param>
        RepositoryEntry InsertRepositoryEntry (RepositoryEntry repositoryEntry);

        /// <summary>
        /// Restores files using entr
        /// </summary>
        Dictionary<ManifestEntry, List<(CompressedFileEntry compressedFileEntry, RepositoryEntry repositoryEntry)>> GetManifestEntriesToRestore();
    }
}
