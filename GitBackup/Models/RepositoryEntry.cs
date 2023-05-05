using System.ComponentModel.DataAnnotations;

namespace GitBackup.Models
{
    public class RepositoryEntry
    {
        [Key]
        public int RepositoryEntryId { get; set; }

        public string RepositoryName { get; set; } = string.Empty;

        public ICollection<CompressedFileEntry>? CompressedFileEntries { get; set; }
    }
}
