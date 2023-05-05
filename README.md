# gitBackup

GitBackup contains the following commands:

```powershell
-r, --restore    Restore files.

-b, --backup     Backup files.

--help           Display this help screen.

--version        Display version information.
```

In addition to the command line arguments, there is an appSettings.json that can be set in the executable directory to further modify how the program works.  This is what this file looks like with all the defaults:

```json
{
  "AppSettings": {
    "FilesToBackupLocation": "<current folder>",
    "BackupLocation": "<current folder>\\backup",
    "RestoreLocation": "<current folder>\\backup",
    "FilesToRestoreLocation": "<current folder>\\restoreTestFolder",
    "RecursiveFileBackup": false,
    "GitSettings": {
      "RepositoryNamingConvention": "gitBackup-",
      "RepositorySizeInKilobytes": 800000,
      "GitRemoteLocation": "", // this value will need to be set at least
      "OverwriteOnAdd": true,
      "Username": null,
      "Email": null,
      "Password": null
    },
    "FileCompressionSettings": {
      "FileSizeInKilobytes": 80000
    }
  }
}
```