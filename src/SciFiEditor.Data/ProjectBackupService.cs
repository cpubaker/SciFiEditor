using System.Globalization;

namespace SciFiEditor.Data;

public sealed class ProjectBackupService
{
    private const int MaxBackups = 10;
    private const string BackupFolderName = ".backup";

    public void RotateBackup(string projectRootPath)
    {
        var dbPath = Path.Combine(projectRootPath, ProjectDatabase.DatabaseFileName);
        if (!File.Exists(dbPath))
        {
            return;
        }

        var backupDir = Path.Combine(projectRootPath, BackupFolderName);
        Directory.CreateDirectory(backupDir);

        var timestamp = DateTime.UtcNow.ToString("yyyyMMddTHHmmss", CultureInfo.InvariantCulture);
        var backupPath = Path.Combine(backupDir, $"project-{timestamp}.db");
        File.Copy(dbPath, backupPath, overwrite: true);

        var staleBackups = Directory.GetFiles(backupDir, "project-*.db")
            .OrderByDescending(f => f, StringComparer.Ordinal)
            .Skip(MaxBackups);

        foreach (var stale in staleBackups)
        {
            File.Delete(stale);
        }
    }
}
