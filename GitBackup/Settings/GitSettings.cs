namespace GitBackup.Settings
{
    public class GitSettings
    {
        /// <summary>
        /// The naming convention of the repository
        /// </summary>
        public string RepositoryNamingConvention { get; set; } = "gitBackup-";

        /// <summary>
        /// The size of the repository in kilobytes
        /// </summary>
        public long RepositorySizeInKilobytes { get; set; } = 800000;

        /// <summary>
        /// The location of where the git remotes live
        /// </summary>
        public string GitRemoteLocation { get; set; } = string.Empty;

        /// <summary>
        /// Whether files should be overwritten when added
        /// </summary>
        public bool OverwriteOnAdd { get; set; } = true;

        /// <summary>
        /// An optional git username
        /// </summary>
        public string? Username { get; set; }

        /// <summary>
        /// An optional git email
        /// </summary>
        public string? Email { get; set; }

        /// <summary>
        /// An optional git password
        /// </summary>
        public string? Password { get; set; }
    }
}
