using System.ComponentModel.DataAnnotations;

namespace GitBackup.Models
{
    public class ManifestEntry
    {
        [Key]
        public int ManifestEntryId { get; set; }

        public string FileName { get; set; } = string.Empty;

        public DateTime Created { get; set; }

        public DateTime LastModified { get; set; }

        public List<CompressedFileEntry> CompressedFileEntries { get; set; } = new List<CompressedFileEntry>();
    }
}
