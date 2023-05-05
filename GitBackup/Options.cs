using CommandLine;

namespace GitBackup
{
    public static partial class Program
    {
        public class Options
        {
            [Option('r', "restore", Required = false, HelpText = "Restore files.")]
            public bool Restore { get; set; }

            [Option('b', "backup", Required = false, HelpText = "Backup files.")]
            public bool Backup { get; set; }
        }
    }
}