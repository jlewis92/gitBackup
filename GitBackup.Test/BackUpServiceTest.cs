using AutoFixture;
using GitBackup.Interfaces;
using GitBackup.Models;
using GitBackup.Services;
using GitBackup.Settings;
using NSubstitute;
using NUnit.Framework;
using System.IO.Abstractions;
using System.IO.Abstractions.TestingHelpers;

namespace GitBackup.Test
{
    public class BackupServiceTest
    {
        IDatabaseService _databaseService; 
        IFileCompressionService _compressionService;
        IGitService _gitService;
        AppSettings _appSettings;
        IFileSystem _fileSystem;
        Fixture _fixture;

        public BackupServiceTest()
        {
            _databaseService = Substitute.For<IDatabaseService>();
            _compressionService = Substitute.For<IFileCompressionService>();
            _gitService = Substitute.For<IGitService>();
            _fileSystem = new MockFileSystem(new Dictionary<string, MockFileData>
            {
                { @"C:\temp\myfile.txt", new MockFileData("Testing is meh.") },
                { @"c:\temp\demo\jQuery.js", new MockFileData("some js") },
                { @"c:\temp\demo\image.gif", new MockFileData(new byte[] { 0x12, 0x34, 0x56, 0xd2 }) },
                { @"c:\temp\jQuery.js", new MockFileData("some js") },
            });

            _appSettings = new AppSettings()
            {
                BackupLocation = @"C:\temp\backup",
                FilesToBackupLocation = @"C:\temp",
                FilesToRestoreLocation = @"C:\temp\restore\backup",
                RestoreLocation = @"C:\temp\restore"
            };

            _fixture = new Fixture();
        }

        [Test]
        public void Constructor_CreatesBackupService_Successfully()
        {
            Assert.DoesNotThrow(() => {
                new BackupService(_gitService, _compressionService, _databaseService, _appSettings, _fileSystem);
            });
        }

        [Test]
        public void Constructor_ThrowsError_WhenNullGitService()
        {
            Assert.Throws<ArgumentNullException>(() => {
#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type.
                new BackupService(null, _compressionService, _databaseService, _appSettings, _fileSystem);
#pragma warning restore CS8625 // Cannot convert null literal to non-nullable reference type.
            });
        }

        [Test]
        public void Constructor_ThrowsError_WhenNullCompressionService()
        {
            Assert.Throws<ArgumentNullException>(() => {
#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type.
                new BackupService(_gitService, null, _databaseService, _appSettings, _fileSystem);
#pragma warning restore CS8625 // Cannot convert null literal to non-nullable reference type.
            });
        }

        [Test]
        public void Constructor_ThrowsError_WhenNullDatabaseService()
        {
            Assert.Throws<ArgumentNullException>(() => {
#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type.
                new BackupService(_gitService, _compressionService, null, _appSettings, _fileSystem);
#pragma warning restore CS8625 // Cannot convert null literal to non-nullable reference type.
            });
        }

        [Test]
        public void Constructor_ThrowsError_WhenNullAppSettings()
        {
            Assert.Throws<ArgumentNullException>(() => {
#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type.
                new BackupService(_gitService, _compressionService, _databaseService, null, _fileSystem);
#pragma warning restore CS8625 // Cannot convert null literal to non-nullable reference type.
            });
        }

        [Test]
        public void Constructor_ThrowsError_WhenNullFileSystem()
        {
            Assert.Throws<ArgumentNullException>(() => {
#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type.
                new BackupService(_gitService, _compressionService, _databaseService, _appSettings, null);
#pragma warning restore CS8625 // Cannot convert null literal to non-nullable reference type.
            });
        }

        [Test]
        public void BackupService_BackupFileNewFiles_WhenCalledWithNoPriorRepository()
        {
            // Arrange
            var databaseService = Substitute.For<IDatabaseService>();
            var compressionService = Substitute.For<IFileCompressionService>();

            var repositoryEntry = new RepositoryEntry()
            {
                RepositoryEntryId = 1,
                CompressedFileEntries = _fixture.Create<List<CompressedFileEntry>>(),
                RepositoryName = "RepoOne"
            };

            databaseService.CheckFilesInManifestForChanges(Arg.Any<List<string>>()).Returns((new List<string>(), new List<string>() { @"C:\temp\myfile.txt" }));
            databaseService.InsertRepositoryEntry(Arg.Any<RepositoryEntry>()).Returns(repositoryEntry);
            databaseService.GetRepositoryEntryById(Arg.Any<int>()).Returns(repositoryEntry);
            databaseService.BackupDatabaseLocation.Returns(_appSettings.BackupLocation);
            compressionService.CompressFile(Arg.Any<string>(), Arg.Any<int>()).Returns((@"C:\temp\myfile.txt.zip", 1));

            var backupService = new BackupService(_gitService, compressionService, databaseService, _appSettings, _fileSystem);

            // Act and Assert
            Assert.DoesNotThrow(() => backupService.BackupFiles());
        }

        [Test]
        public void BackupService_BackupFileNewFiles_WhenCalledWithPriorRepository()
        {
            // Arrange
            var databaseService = Substitute.For<IDatabaseService>();
            var compressionService = Substitute.For<IFileCompressionService>();

            var repositoryEntry = new RepositoryEntry()
            {
                RepositoryEntryId = 1,
                CompressedFileEntries = _fixture.Create<List<CompressedFileEntry>>(),
                RepositoryName = "RepoOne"
            };

            databaseService.CheckFilesInManifestForChanges(Arg.Any<List<string>>()).Returns((new List<string>(), new List<string>() { @"C:\temp\myfile.txt" }));
            databaseService.GetRepositoryEntry(Arg.Any<string>()).Returns(repositoryEntry);
            databaseService.GetRepositoryEntryById(Arg.Any<int>()).Returns(repositoryEntry);
            databaseService.BackupDatabaseLocation.Returns(_appSettings.BackupLocation);
            compressionService.CompressFile(Arg.Any<string>(), Arg.Any<int>()).Returns((@"C:\temp\myfile.txt.zip", 1));

            var backupService = new BackupService(_gitService, compressionService, databaseService, _appSettings, _fileSystem);

            // Act and Assert
            Assert.DoesNotThrow(() => backupService.BackupFiles());
        }

        [Test]
        public void BackupService_BackupFileUpdateFiles_WhenCalledWithNoPriorRepository()
        {
            // Arrange
            var databaseService = Substitute.For<IDatabaseService>();
            var compressionService = Substitute.For<IFileCompressionService>();

            var compressedFileEntries = _fixture.Create<List<CompressedFileEntry>>();

            var repositoryEntry = new RepositoryEntry()
            {
                RepositoryEntryId = 1,
                CompressedFileEntries = compressedFileEntries,
                RepositoryName = "RepoOne"
            };

            var manifestEntry = new ManifestEntry()
            {
                LastModified = DateTime.Now,
                FileName = @"C:\temp\myfile.txt",
                CompressedFileEntries = compressedFileEntries
            };

            databaseService.CheckFilesInManifestForChanges(Arg.Any<List<string>>()).Returns((new List<string>() { @"C:\temp\myfile.txt" }, new List<string>()));
            databaseService.GetManifestEntry(Arg.Any<string>()).Returns(manifestEntry);
            databaseService.InsertRepositoryEntry(Arg.Any<RepositoryEntry>()).Returns(repositoryEntry);
            databaseService.GetRepositoryEntryById(Arg.Any<int>()).Returns(repositoryEntry);
            databaseService.BackupDatabaseLocation.Returns(_appSettings.BackupLocation);
            compressionService.CompressFile(Arg.Any<string>(), Arg.Any<int>()).Returns((@"C:\temp\myfile.txt.zip", 1));

            var backupService = new BackupService(_gitService, compressionService, databaseService, _appSettings, _fileSystem);

            // Act and Assert
            Assert.DoesNotThrow(() => backupService.BackupFiles());
        }

        [Test]
        public void BackupService_BackupFileUpdateFiles_WhenCalledWithPriorRepository()
        {
            // Arrange
            var databaseService = Substitute.For<IDatabaseService>();
            var compressionService = Substitute.For<IFileCompressionService>();

            var compressedFileEntries = _fixture.Create<List<CompressedFileEntry>>();

            var repositoryEntry = new RepositoryEntry()
            {
                RepositoryEntryId = 1,
                CompressedFileEntries = compressedFileEntries,
                RepositoryName = "RepoOne"
            };

            var manifestEntry = new ManifestEntry()
            {
                LastModified = DateTime.Now,
                FileName = @"C:\temp\myfile.txt",
                CompressedFileEntries = compressedFileEntries
            };

            databaseService.CheckFilesInManifestForChanges(Arg.Any<List<string>>()).Returns((new List<string>() { @"C:\temp\myfile.txt" }, new List<string>()));
            databaseService.GetManifestEntry(Arg.Any<string>()).Returns(manifestEntry);
            databaseService.GetRepositoryEntry(Arg.Any<string>()).Returns(repositoryEntry);
            databaseService.GetRepositoryEntryById(Arg.Any<int>()).Returns(repositoryEntry);
            databaseService.BackupDatabaseLocation.Returns(_appSettings.BackupLocation);
            compressionService.CompressFile(Arg.Any<string>(), Arg.Any<int>()).Returns((@"C:\temp\myfile.txt.zip", 1));

            var backupService = new BackupService(_gitService, compressionService, databaseService, _appSettings, _fileSystem);

            // Act and Assert
            Assert.DoesNotThrow(() => backupService.BackupFiles());
        }

        [Test]
        public void BackupService_RestoreFiles_RestoresFiles()
        {
            // Arrange
            var databaseService = Substitute.For<IDatabaseService>();
            var compressionService = Substitute.For<IFileCompressionService>();

            var compressedFileEntries = _fixture.Create<List<CompressedFileEntry>>();
            var compressedFileEntry = _fixture.Create<CompressedFileEntry>();

            var repositoryEntry = new RepositoryEntry()
            {
                RepositoryEntryId = 1,
                CompressedFileEntries = compressedFileEntries,
                RepositoryName = "RepoOne"
            };

            var manifestEntry = new ManifestEntry()
            {
                LastModified = DateTime.Now,
                FileName = @"C:\temp\myfile.txt",
                CompressedFileEntries = compressedFileEntries
            };

            var manifestEntriesToRestore = new Dictionary<ManifestEntry, List<(CompressedFileEntry compressedFileEntry, RepositoryEntry repositoryEntry)>>();
            manifestEntriesToRestore.Add(manifestEntry, new List<(CompressedFileEntry compressedFileEntry, RepositoryEntry repositoryEntry)>()
            {
                (compressedFileEntry, repositoryEntry)
            });

            databaseService.CheckFilesInManifestForChanges(Arg.Any<List<string>>()).Returns((new List<string>(), new List<string>() { @"C:\temp\myfile.txt" }));
            databaseService.BackupDatabaseLocation.Returns(_appSettings.BackupLocation);
            databaseService.GetManifestEntriesToRestore().Returns(manifestEntriesToRestore);
            compressionService.CompressFile(Arg.Any<string>(), Arg.Any<int>()).Returns((@"C:\temp\myfile.txt.zip", 1));

            var backupService = new BackupService(_gitService, compressionService, databaseService, _appSettings, _fileSystem);

            // Act and Assert
            Assert.DoesNotThrow(() => backupService.RestoreFiles());
        }
    }
}