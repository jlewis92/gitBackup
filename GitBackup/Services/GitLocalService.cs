using GitBackup.Interfaces;
using GitBackup.Settings;
using LibGit2Sharp;
using LibGit2Sharp.Handlers;
using Serilog;
using System;
using System.Collections.Generic;
using System.IO.Abstractions;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GitBackup.Services
{
    public class GitLocalService : IGitService
    {
        private AppSettings _appSettings;
        private IFileSystem _fileSystem;

        public GitLocalService(AppSettings appSettings, IFileSystem fileSystem)
        {
            ArgumentNullException.ThrowIfNull(appSettings);

            _appSettings = appSettings;
            _fileSystem = fileSystem;
        }

        public GitLocalService(AppSettings appSettings) : this(appSettings, new FileSystem())
        {
        }

        public IDirectoryInfo AddFile(IFileInfo fileInfo)
        {
            var directoryInfo = GetAndSetupRepositoryToSaveTo(fileInfo);

            fileInfo.CopyTo(Path.Combine(directoryInfo.FullName, fileInfo.Name), _appSettings.GitSettings.OverwriteOnAdd);
            fileInfo.Delete();

            return directoryInfo;
        }

        public IDirectoryInfo UpdateFile(IFileInfo fileInfo, IDirectoryInfo directoryToUpdate)
        {
            fileInfo.CopyTo(Path.Combine(directoryToUpdate.FullName, fileInfo.Name), true);
            fileInfo.Delete();

            return directoryToUpdate;
        }

        private IDirectoryInfo GetAndSetupRepositoryToSaveTo(IFileInfo fileInfo)
        {
            var directories = _fileSystem.Directory.GetDirectories(_appSettings.BackupLocation, $"{_appSettings.GitSettings.RepositoryNamingConvention}*", SearchOption.TopDirectoryOnly).ToList();

            IDirectoryInfo directoryInfo;

            if (directories.Count == 0)
            {
                directoryInfo = _fileSystem.Directory.CreateDirectory($"{_appSettings.BackupLocation}{Path.DirectorySeparatorChar}{_appSettings.GitSettings.RepositoryNamingConvention}0");
                SetupGitRepository(directoryInfo);
            }
            else
            {
                // get the last created
                var directory = directories.Last();

                directoryInfo = _fileSystem.DirectoryInfo.New(directory);

                var directorySize = GetDirectorySize(directoryInfo);
                var maxRepositorySize = _appSettings.GitSettings.RepositorySizeInKilobytes * 1024;

                // if adding the file will make this repository larger than the max allowed, create a new repository
                if (directorySize + fileInfo.Length > maxRepositorySize)
                {
                    // get the last directory and then increase the number by 1
                    var newDirectoryNumber = int.Parse(directory.Remove(0, _appSettings.GitSettings.RepositoryNamingConvention.Length)) + 1;

                    directoryInfo = _fileSystem.Directory.CreateDirectory($"{_appSettings.GitSettings.RepositoryNamingConvention}{newDirectoryNumber}");
                    SetupGitRepository(directoryInfo);
                }
            }

            return directoryInfo;
        }

        private void SetupGitRepository(IDirectoryInfo directory)
        {
            var remoteLocation = Path.Combine(_appSettings.GitSettings.GitRemoteLocation, directory.Name);

            // Create the remote (bare) repository
            Repository.Init(remoteLocation, true);

            Repository.Clone(remoteLocation, directory.FullName);
        }

        private long GetDirectorySize(IDirectoryInfo directoryInfo)
        {
            var startDirectorySize = default(long);
            if (directoryInfo == null || !directoryInfo.Exists)
            {
                return startDirectorySize; //Return 0 while Directory does not exist.
            }

            //Add size of files in the Current Directory to main size.
            foreach (var fileInfo in directoryInfo.GetFiles())
            {
                Interlocked.Add(ref startDirectorySize, fileInfo.Length);
            }

            return startDirectorySize;  //Return full Size of this Directory.
        }

        public void UpdateRemote(string directory)
        {
            using (var repo = new Repository(directory))
            {               
                var remote = repo.Network.Remotes["origin"];
                var pushRefSpec = "refs/heads/master";

                Commands.Stage(repo, "*");

                PushOptions options = new PushOptions();
                var config = repo.Config;
                var author = config.BuildSignature(DateTimeOffset.Now);
                repo.Commit("updating files..", author, author);

                if (_appSettings.GitSettings.Username != null)
                {
                    options.CredentialsProvider = new CredentialsHandler(
                    (url, usernameFromUrl, types) =>
                    new UsernamePasswordCredentials()
                    {
                        Username = _appSettings.GitSettings.Username,
                        Password = _appSettings.GitSettings.Password
                    });
                }

                Log.Debug($"pushing repo {repo}");
                repo.Network.Push(remote, pushRefSpec, options);
            }
        }

        public void UpdateManifest(IFileInfo fileInfo, IDirectoryInfo directoryInfo)
        {
            fileInfo.CopyTo($"{directoryInfo.FullName}{Path.DirectorySeparatorChar}{fileInfo.Name}", true);
        }

        public List<IDirectoryInfo> DownloadToRestoreLocation(string manifestName)
        {
            var directoriesToClone = _fileSystem.Directory.GetDirectories(_appSettings.GitSettings.GitRemoteLocation).ToList();
            var directoryInfoList = new List<IDirectoryInfo>();
            
            foreach (var directory in directoriesToClone)
            {
                var directoryInfo = _fileSystem.DirectoryInfo.New(directory);
                var cloneLocation = Path.Combine(_appSettings.FilesToRestoreLocation, directoryInfo.Name);

                if (_fileSystem.Directory.Exists(cloneLocation))
                {
                    using (var repo = new Repository(cloneLocation))
                    {
                        var config = repo.Config;
                        var author = config.BuildSignature(DateTimeOffset.Now);

                        Commands.Pull(repo, author, new PullOptions());
                    }
                }
                else
                {
                    Repository.Clone(directory, cloneLocation);
                }

                var possibleManifestLocation = Path.Combine(cloneLocation, manifestName);

                if (File.Exists(possibleManifestLocation))
                {
                    Log.Information("Restoring manifest from git repository..");
                    RestoreManifestFile(possibleManifestLocation);
                }

                directoryInfoList.Add(directoryInfo);
            }

            return directoryInfoList;
        }

        private void RestoreManifestFile(string manifestLocation)
        {
            var fileInfo = new FileInfo(manifestLocation);

            fileInfo.CopyTo(Path.Combine(_appSettings.FilesToRestoreLocation, fileInfo.Name), true);
        }
    }
}
