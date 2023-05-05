using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GitBackup.Interfaces
{
    public interface IBackupService
    {
        /// <summary>
        /// Backs up files
        /// </summary>
        void BackupFiles();

        /// <summary>
        /// Restores files
        /// </summary>
        void RestoreFiles();
    }
}
