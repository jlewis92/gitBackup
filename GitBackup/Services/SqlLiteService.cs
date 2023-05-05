using GitBackup.Database;
using GitBackup.Interfaces;
using GitBackup.Models;
using GitBackup.Settings;
using Microsoft.EntityFrameworkCore;
using Serilog;
using System.Data.Entity;

namespace GitBackup.Services
{
    public class SqlLiteService : IDatabaseService
    {
        public string BackupDatabaseLocation { get; }
        public string RestoreDatabaseLocation { get; }
        public string DatabaseName { get; } = "manifest.db";

        private readonly AppSettings _appSettings;

        public SqlLiteService(AppSettings settings)
        {
            BackupDatabaseLocation = Path.Combine(settings.BackupLocation, DatabaseName);
            RestoreDatabaseLocation = Path.Combine(settings.FilesToRestoreLocation, DatabaseName);
            _appSettings = settings;
        }

        public void Configure()
        {
            if (!Directory.Exists(_appSettings.BackupLocation))
            {
                Directory.CreateDirectory(_appSettings.BackupLocation);
            }

            using (var context = new ManifestContext(BackupDatabaseLocation))
            {
                context.Database.EnsureCreated();
            }

            var contextTwo = new ManifestContext(BackupDatabaseLocation);

            var connection = contextTwo.Database.GetDbConnection();
            connection.Open();
            using (var command = connection.CreateCommand())
            {
                command.CommandText = "PRAGMA journal_mode=DELETE;";
                command.ExecuteNonQuery();
            }

            contextTwo.Dispose();
        }

        public (List<string> FilesToUpdate, List<string> FilesToAdd) CheckFilesInManifestForChanges(List<string> filesBeingBackedUp)
        {
            (List<string> filesToUpdate, List<string> filesToAdd) filesToBackup = (new List<string>(), new List<string>());

            using (var context = new ManifestContext(BackupDatabaseLocation))
            {
                foreach (var file in filesBeingBackedUp)
                {
                    var manifestEntry = context.ManifestEntries.FirstOrDefault(f => f.FileName == file);

                    if (manifestEntry != null)
                    {
                        var fileLocation = Path.Combine(_appSettings.FilesToBackupLocation, file);

                        // if the file last modified date is later than the last time it was backed up, we need to back it up again
                        if (File.GetLastWriteTime(fileLocation) > manifestEntry.LastModified)
                        {
                            Log.Debug($"New file to backup - {file}");
                            filesToBackup.filesToUpdate.Add(file);
                        }
                    }
                    else
                    {
                        Log.Debug($"Updating file - {file}");
                        filesToBackup.filesToAdd.Add(file);
                    }
                }
            }

            return filesToBackup;
        }

        public void AddManifestEntry(ManifestEntry manifestEntry)
        {
            Log.Debug($"Adding manifest entry for {manifestEntry.FileName}");

            using (var context = new ManifestContext(BackupDatabaseLocation))
            {
                context.ManifestEntries.Add(manifestEntry);
                context.SaveChanges();
            }          
        }

        public void UpdateManifestEntry(ManifestEntry manifestEntry)
        {
            using (var context = new ManifestContext(BackupDatabaseLocation))
            {
                context.ManifestEntries.Update(manifestEntry);
                context.SaveChanges();
            }
        }

        public RepositoryEntry? GetRepositoryEntry(string repositoryName)
        {
            RepositoryEntry? repositoryEntry = new();

            using (var context = new ManifestContext(BackupDatabaseLocation))
            {
                repositoryEntry = context.RepositoryEntries.FirstOrDefault(r => r.RepositoryName == repositoryName);
            }

            return repositoryEntry;
        }

        public RepositoryEntry InsertRepositoryEntry(RepositoryEntry repositoryEntry)
        {
            var repositoryEntryFromDatabase = new RepositoryEntry();

            using (var context = new ManifestContext(BackupDatabaseLocation))
            {
                context.RepositoryEntries.Add(repositoryEntry);
                context.SaveChanges();

                repositoryEntryFromDatabase = context.RepositoryEntries.First(r => r.RepositoryName == repositoryEntry.RepositoryName);
            }

            return repositoryEntryFromDatabase;
        }

        public ManifestEntry GetManifestEntry(string fileName)
        {
            var manifestEntry = new ManifestEntry();

            using (var context = new ManifestContext(BackupDatabaseLocation))
            {
                manifestEntry = context.ManifestEntries
                    .First(m => m.FileName == fileName);
                manifestEntry.CompressedFileEntries = context.CompressedFileEntries.Where(m => m.ManifestEntryId == manifestEntry.ManifestEntryId).ToList();
            }

            return manifestEntry;
        }

        public RepositoryEntry GetRepositoryEntryById(int repositoryEntryId)
        {
            var repositoryEntry = new RepositoryEntry();

            using (var context = new ManifestContext(BackupDatabaseLocation))
            {
                repositoryEntry = context.RepositoryEntries.First(r => r.RepositoryEntryId == repositoryEntryId);
            }

            return repositoryEntry;
        }

        public Dictionary<ManifestEntry, List<(CompressedFileEntry compressedFileEntry, RepositoryEntry repositoryEntry)>> GetManifestEntriesToRestore()
        {
            Dictionary<ManifestEntry, List<(CompressedFileEntry compressedFileEntry, RepositoryEntry repositoryEntry)>> manifestEntries = new();

            using (var context = new ManifestContext(RestoreDatabaseLocation))
            {
                var manifest = context.ManifestEntries.ToList();

                foreach (var entry in manifest)
                {
                    var compressedFiles = context.CompressedFileEntries.Where(c => c.ManifestEntryId == entry.ManifestEntryId).ToList();
                    var compressedFileEntries = new List<(CompressedFileEntry compressedFileEntry, RepositoryEntry repositoryEntry)>();

                    foreach (var compressedFile in compressedFiles)
                    {
                        var repositryEntry = context.RepositoryEntries.First(r => r.RepositoryEntryId == compressedFile.RepositoryEntryId);

                        compressedFileEntries.Add((compressedFile, repositryEntry));
                    }

                    manifestEntries.Add(entry, compressedFileEntries);
                }
            }

            return manifestEntries;
        }
    }
}
