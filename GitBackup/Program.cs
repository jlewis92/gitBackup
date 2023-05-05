using CommandLine;
using GitBackup.Interfaces;
using GitBackup.Services;
using GitBackup.Settings;
using Microsoft.Extensions.Configuration;
using Serilog;

namespace GitBackup
{
    public static partial class Program
    {
        static void Main(string[] args)
        {
            var environment = Environment.GetEnvironmentVariable("NETCORE_ENVIRONMENT");
            Log.Logger = new LoggerConfiguration().WriteTo.Console().CreateLogger();

            var builder = new ConfigurationBuilder();
            builder.SetBasePath(Directory.GetCurrentDirectory())
                   .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
                   .AddJsonFile($"appsettings.{environment}.json", optional: true);

            IConfiguration config = builder.Build();

            var settings = config.GetSection("AppSettings").Get<AppSettings>();

            var databaseService = new SqlLiteService(settings);
            var gitService = new GitLocalService(settings);
            var zipService = new ZipService(settings);

            IBackupService backupService = new BackupService(gitService, zipService, databaseService, settings);

            Parser.Default.ParseArguments<Options>(args)
                .WithParsed<Options>(o =>
                {
                    if (o.Backup)
                    {
                        Log.Information("Backing up files.");
                        backupService.BackupFiles();
                    }
                    else if (o.Restore)
                    {
                        Console.WriteLine("Restoring files.");
                        backupService.RestoreFiles();
                    }
                    else
                    {
                        Log.Information("No option set.");
                    }
                });
        }
    }
}