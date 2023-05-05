using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GitBackup.Settings
{
    public class AppSettings
    {
        /// <summary>
        /// The location of the git backup folder
        /// </summary>
        public string BackupLocation { get; set; } = Path.Combine(Directory.GetCurrentDirectory(), "backup");

        /// <summary>
        /// The location of the files to backup
        /// </summary>
        public string FilesToBackupLocation { get; set; } = Directory.GetCurrentDirectory();

        /// <summary>
        /// The git restore location for file backup
        /// </summary>
        public string RestoreLocation { get; set; } = Path.Combine(Directory.GetCurrentDirectory(), "backup");

        /// <summary>
        /// The location that files are to be restored to
        /// </summary>
        public string FilesToRestoreLocation { get; set; } = Directory.GetCurrentDirectory();

        /// <summary>
        /// Whether to recursively backup the back up folder
        /// </summary>
        public bool RecursiveFileBackup { get; set; } = false;

        /// <summary>
        /// File compression settings
        /// </summary>
        public FileCompressionSettings FileCompressionSettings { get; set; } = new FileCompressionSettings();

        /// <summary>
        /// Git settings
        /// </summary>
        public GitSettings GitSettings { get; set; } = new GitSettings();
    }
}
