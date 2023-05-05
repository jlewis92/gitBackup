using GitBackup.Interfaces;
using GitBackup.Models;
using GitBackup.Settings;
using Serilog;
using System.IO.Abstractions;

namespace GitBackup.Services
{
    public class BackupService : IBackupService
    {
        readonly IFileCompressionService _fileCompressionService;
        readonly IGitService _gitService;
        readonly IDatabaseService _databaseService;
        readonly AppSettings _appSettings;
        readonly IFileSystem _fileSystem;

        /// <summary>
        /// Constructor that provides access to the file system for testing
        /// </summary>
        /// <param name="gitService"></param>
        /// <param name="fileCompressionService"></param>
        /// <param name="databaseService"></param>
        /// <param name="appSettings"></param>
        /// <param name="fileSystem"></param>
        public BackupService(IGitService gitService, IFileCompressionService fileCompressionService, IDatabaseService databaseService, AppSettings appSettings, IFileSystem fileSystem)
        {
            ArgumentNullException.ThrowIfNull(gitService);
            ArgumentNullException.ThrowIfNull(fileCompressionService);
            ArgumentNullException.ThrowIfNull(databaseService);
            ArgumentNullException.ThrowIfNull(appSettings);
            ArgumentNullException.ThrowIfNull(fileSystem);

            _fileCompressionService = fileCompressionService;
            _gitService = gitService;
            _databaseService = databaseService;
            _appSettings = appSettings;
            _fileSystem = fileSystem;

            _databaseService.Configure();
        }

        public BackupService(IGitService gitService, IFileCompressionService fileCompressionService, IDatabaseService databaseService, AppSettings appSettings) : this (gitService, fileCompressionService, databaseService, appSettings, new FileSystem())
        {
        }

        public void BackupFiles()
        {
            // Create the directories if they don't exist already
            _fileSystem.Directory.CreateDirectory(_appSettings.BackupLocation);
            _fileSystem.Directory.CreateDirectory(_appSettings.FilesToBackupLocation);

            Log.Information("Indexing files..");
            var files = IndexFiles(_appSettings.FilesToBackupLocation, _appSettings.RecursiveFileBackup);

            Log.Information("Checking files for changes..");
            var (FilesToUpdate, FilesToAdd) = _databaseService.CheckFilesInManifestForChanges(files);

            // add both the new and updated files to the git repositories and update the manifest
            var updatedDirectories = AddNewFiles(FilesToAdd);
            updatedDirectories.UnionWith(UpdateFiles(FilesToUpdate));

            if (updatedDirectories.Count > 0)
            {
                Log.Information("Backing up manifest..");
                BackupManifest();
            }

            CommitGitRepositories(updatedDirectories);
        }

        public void RestoreFiles()
        {
            // Create the directories if they don't exist already
            _fileSystem.Directory.CreateDirectory(_appSettings.FilesToRestoreLocation);
            _fileSystem.Directory.CreateDirectory(_appSettings.RestoreLocation);

            Log.Information("Restoring Git Repositories to manifest location..");
            _gitService.DownloadToRestoreLocation(_databaseService.DatabaseName);

            Log.Information("Finding files to restore..");
            var manifestEntries = _databaseService.GetManifestEntriesToRestore();

            Log.Information("Restoring files..");
            foreach (var manifestEntry in manifestEntries)
            {
                _fileCompressionService.RestoreFile(manifestEntry, _fileSystem);
            }
        }

        private void BackupManifest()
        {
            GC.Collect();
            // just get the first repo to backup the manifest
            var repositoryEntry = _databaseService.GetRepositoryEntryById(1);

            var directoryToUpdate = Path.Combine(_appSettings.BackupLocation, repositoryEntry.RepositoryName);

            var directoryInfo = _fileSystem.DirectoryInfo.New(directoryToUpdate);
            var fileInfo = _fileSystem.FileInfo.New(_databaseService.BackupDatabaseLocation);

            _gitService.UpdateManifest(fileInfo, directoryInfo);
        }

        private void CommitGitRepositories(HashSet<string> updatedDirectoriesForAdd)
        {
            Log.Information("Committing and updating git remotes");
            foreach (var directory in updatedDirectoriesForAdd)
            {
                _gitService.UpdateRemote(directory);
            }
            Log.Information("Git remotes updated");
        }

        private HashSet<string> UpdateFiles(List<string> filesToUpdate)
        {
            Log.Information("Beginning compression process for updated files..");

            var updatedDirectories = new HashSet<string>();

            foreach (var file in filesToUpdate)
            {
                var fileInfo = _fileSystem.FileInfo.New(file);

                var manifestEntry = _databaseService.GetManifestEntry(fileInfo.Name);

                var zippedFile = _fileCompressionService.CompressFile(Path.Combine(_appSettings.FilesToBackupLocation, file), _appSettings.FileCompressionSettings.FileSizeInKilobytes);

                manifestEntry.LastModified = DateTime.Now;

                // add .zip to service
                for (int i = 0; i < zippedFile.Segments; i++)
                {
                    // if it's a new segment, make sure to update the file
                    if (i > manifestEntry.CompressedFileEntries.Count)
                    {
                        var segmentFileInfo = _fileSystem.FileInfo.New($"{fileInfo.DirectoryName}{Path.DirectorySeparatorChar}{file}.z{i:00}");
                        var directorySegment = _gitService.AddFile(segmentFileInfo);
                        updatedDirectories.Add(directorySegment.FullName);

                        var repositorySegment = _databaseService.GetRepositoryEntry(directorySegment.Name);

                        if (repositorySegment == null)
                        {
                            repositorySegment = _databaseService.InsertRepositoryEntry(new RepositoryEntry()
                            {
                                RepositoryName = directorySegment.Name
                            });
                        }

                        manifestEntry.CompressedFileEntries.Add(new CompressedFileEntry()
                        {
                            RepositoryEntryId = repositorySegment.RepositoryEntryId,
                            ZipFileName = segmentFileInfo.Name
                        });
                    }
                    else
                    {
                        var repositoryEntry = _databaseService.GetRepositoryEntryById(manifestEntry.CompressedFileEntries[i].RepositoryEntryId);

                        var directoryToUpdate = Path.Combine(_appSettings.BackupLocation, repositoryEntry.RepositoryName);
                        var fileToUpdate = Path.Combine(_appSettings.FilesToBackupLocation, manifestEntry.CompressedFileEntries[i].ZipFileName);

                        var directoryInfo = _fileSystem.DirectoryInfo.New(directoryToUpdate);
                        var fileToUpdateInfo = _fileSystem.FileInfo.New(fileToUpdate);

                        _gitService.UpdateFile(fileToUpdateInfo, directoryInfo);
                        updatedDirectories.Add(directoryInfo.FullName);
                    }
                }

                _databaseService.UpdateManifestEntry(manifestEntry);
            }

            Log.Information("Compression process complete..");

            return updatedDirectories;
        }

        private HashSet<string> AddNewFiles(List<string> filesToAdd)
        {
            var updatedDirectories = new HashSet<string>();

            Log.Information("Beginning compression process for new files..");

            foreach (var file in filesToAdd)
            {
                Log.Debug($"Compressing file - {file}");

                var zippedFile = _fileCompressionService.CompressFile(Path.Combine(_appSettings.FilesToBackupLocation, file), _appSettings.FileCompressionSettings.FileSizeInKilobytes);

                Log.Debug($"{file} compressed into {zippedFile.Segments} segments");

                var newManifestEntry = new ManifestEntry()
                {
                    FileName = file,
                    Created = DateTime.Now,
                    LastModified = DateTime.Now
                };

                var fileInfo = _fileSystem.FileInfo.New(zippedFile.FileName);

                var directory = _gitService.AddFile(fileInfo);
                updatedDirectories.Add(directory.FullName);

                var repository = _databaseService.GetRepositoryEntry(directory.Name);

                if (repository == null)
                {
                    repository = _databaseService.InsertRepositoryEntry(new RepositoryEntry()
                    {
                        RepositoryName = directory.Name
                    });
                }

                newManifestEntry.CompressedFileEntries.Add(new CompressedFileEntry()
                {
                    RepositoryEntryId = repository.RepositoryEntryId,
                    ZipFileName = fileInfo.Name
                });

                for (int i = 1; i < zippedFile.Segments; i++)
                {
                    var segmentFileInfo = _fileSystem.FileInfo.New($"{fileInfo.DirectoryName}{Path.DirectorySeparatorChar}{file}.z{i:00}");

                    var directorySegment = _gitService.AddFile(segmentFileInfo);
                    updatedDirectories.Add(directorySegment.FullName);

                    var repositorySegment = _databaseService.GetRepositoryEntry(directorySegment.Name);

                    if (repositorySegment == null)
                    {
                        repositorySegment = _databaseService.InsertRepositoryEntry(new RepositoryEntry()
                        {
                            RepositoryName = directorySegment.Name
                        });
                    }

                    newManifestEntry.CompressedFileEntries.Add(new CompressedFileEntry()
                    {
                        RepositoryEntryId = repositorySegment.RepositoryEntryId,
                        ZipFileName = segmentFileInfo.Name
                    });
                }

                _databaseService.AddManifestEntry(newManifestEntry);
            }

            Log.Information("Compression process complete..");

            return updatedDirectories;
        }

        private List<string> IndexFiles(string fileBackupLocation, bool recursiveFileBackup)
        {
            var fileList = new List<string>();

            if (recursiveFileBackup)
            {
#pragma warning disable CS8619 // Nullability of reference types in value doesn't match target type.
                fileList = _fileSystem.Directory.EnumerateFiles(fileBackupLocation, "*", SearchOption.AllDirectories).Select(Path.GetFileName).ToList();
#pragma warning restore CS8619 // Nullability of reference types in value doesn't match target type.
            }
            else
            {
#pragma warning disable CS8619 // Nullability of reference types in value doesn't match target type.
                fileList = _fileSystem.Directory.EnumerateFiles(fileBackupLocation, "*", SearchOption.TopDirectoryOnly).Select(Path.GetFileName).ToList();
#pragma warning restore CS8619 // Nullability of reference types in value doesn't match target type.
            }

            return fileList;
        }
    }
}
