using GitBackup.Models;
using System;
using System.Collections.Generic;
using System.IO.Abstractions;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GitBackup.Interfaces
{
    public interface IFileCompressionService
    {
        /// <summary>
        /// Compresses a file
        /// </summary>
        /// <param name="fileName">The name of the file to compress</param>
        /// <param name="outputSegementSize">Maximum size of each segment</param>
        /// <returns>The compressed file name and the number of segments the file has been compressed to</returns>
        (string FileName, int Segments) CompressFile(string fileName, int outputSegementSize);

        /// <summary>
        /// Restores a file based on a manifest entry
        /// </summary>
        /// <param name="manifestEntry">The manifest entry to restore</param>
        void RestoreFile(KeyValuePair<ManifestEntry, List<(CompressedFileEntry compressedFileEntry, RepositoryEntry repositoryEntry)>> manifestEntry, IFileSystem fileSystem);
    }
}
