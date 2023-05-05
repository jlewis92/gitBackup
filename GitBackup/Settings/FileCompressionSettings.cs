using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GitBackup.Settings
{
    public class FileCompressionSettings
    {
        /// <summary>
        /// The maximum file size of each chunk of a compressed file
        /// </summary>
        public int FileSizeInKilobytes { get; set; } = 80000; // defaults to 80MB
    }
}
