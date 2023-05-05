using GitBackup.Models;
using Microsoft.EntityFrameworkCore;

namespace GitBackup.Database
{
    public class ManifestContext : DbContext
    {
        public DbSet<ManifestEntry> ManifestEntries => Set<ManifestEntry>();
        public DbSet<CompressedFileEntry> CompressedFileEntries => Set<CompressedFileEntry>();
        public DbSet<RepositoryEntry> RepositoryEntries => Set<RepositoryEntry>();

        public string DbPath { get; set; }

        public ManifestContext(string dbPath)
        {
            DbPath = dbPath;
        }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseSqlite($"Data Source={DbPath};").EnableSensitiveDataLogging();
        }
    }
}
