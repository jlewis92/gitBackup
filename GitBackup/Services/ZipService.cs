using GitBackup.Interfaces;
using GitBackup.Models;
using GitBackup.Settings;
using Ionic.Zip;
using System.IO.Abstractions;

namespace GitBackup.Services
{
    public class ZipService : IFileCompressionService
    {
        AppSettings _appSettings;

        public ZipService(AppSettings appSettings)
        {
            _appSettings = appSettings;
        }

        public (string FileName, int Segments) CompressFile(string fileName, int outputSegementSize)
        {

            var fileLocation = Path.Combine(_appSettings.FilesToBackupLocation, fileName);

            int segmentsCreated;
            using (ZipFile zip = new ZipFile())
            {
                zip.AddFile(fileLocation);
                zip.Comment = "This zip was created at " + DateTime.Now.ToString("G");
                zip.MaxOutputSegmentSize = outputSegementSize * 1024;
                zip.Save($"{fileName}.zip");

                segmentsCreated = zip.NumberOfSegmentsForMostRecentSave;
            }

            return (Path.Combine(_appSettings.FilesToBackupLocation, $"{fileName}.zip"), segmentsCreated);
        }

        public void RestoreFile(KeyValuePair<ManifestEntry, List<(CompressedFileEntry compressedFileEntry, RepositoryEntry repositoryEntry)>> manifestEntry, IFileSystem fileSystem)
        {
            var tempLocation = fileSystem.DirectoryInfo.New(Path.Combine(_appSettings.FilesToRestoreLocation, "temp"));
            fileSystem.Directory.CreateDirectory(tempLocation.FullName);

            // restore all the files to a central folder for restore
            foreach (var compressedFileEntry in manifestEntry.Value)
            {
                var fileToCopy = fileSystem.FileInfo.New(Path.Combine(_appSettings.FilesToRestoreLocation, compressedFileEntry.repositoryEntry.RepositoryName, compressedFileEntry.compressedFileEntry.ZipFileName));
                fileToCopy.CopyTo(Path.Combine(tempLocation.FullName, compressedFileEntry.compressedFileEntry.ZipFileName));
            }

            var fileLocationToRestore = Path.Combine(tempLocation.FullName, manifestEntry.Value[0].compressedFileEntry.ZipFileName);

            using (ZipFile zip = ZipFile.Read(fileLocationToRestore))
            {
                zip.ExtractAll(tempLocation.FullName);
            }

            // The file gets extracted far down the tree due to abusing zip files, so we need to find it
            var fileInfo = fileSystem.FileInfo.New(fileSystem.Directory.GetFiles(tempLocation.FullName, manifestEntry.Key.FileName, SearchOption.AllDirectories).First());

            fileInfo.CopyTo(Path.Combine(_appSettings.RestoreLocation, manifestEntry.Key.FileName), true);
            fileSystem.Directory.Delete(tempLocation.FullName, true);
        }
    }
}
