using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GitBackup.Models
{
    public class CompressedFileEntry
    {
        [Key]
        public int CompressedFileEntryId { get; set; }

        public string ZipFileName { get; set; } = string.Empty;

        [ForeignKey("RepositoryEntry")]
        public int RepositoryEntryId { get; set; }

        [ForeignKey("ManifestEntry")]
        public int ManifestEntryId { get; set; }
    }
}
