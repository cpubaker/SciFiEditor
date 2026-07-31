using FluentAssertions;

namespace SciFiEditor.Data.Tests;

public class ProjectBackupServiceTests
{
    [Fact]
    public void RotateBackup_DoesNothing_WhenProjectDbDoesNotExist()
    {
        using var temp = new TempDirectory();
        var service = new ProjectBackupService();

        service.RotateBackup(temp.Path);

        Directory.Exists(Path.Combine(temp.Path, ".backup")).Should().BeFalse();
    }

    [Fact]
    public void RotateBackup_CreatesBackupCopy_WhenProjectDbExists()
    {
        using var temp = new TempDirectory();
        File.WriteAllText(Path.Combine(temp.Path, ProjectDatabase.DatabaseFileName), "fake-db");
        var service = new ProjectBackupService();

        service.RotateBackup(temp.Path);

        var backupDir = Path.Combine(temp.Path, ".backup");
        Directory.GetFiles(backupDir, "project-*.db").Should().ContainSingle();
    }

    [Fact]
    public void RotateBackup_KeepsOnlyNewest10Backups()
    {
        using var temp = new TempDirectory();
        File.WriteAllText(Path.Combine(temp.Path, ProjectDatabase.DatabaseFileName), "fake-db");
        var backupDir = Path.Combine(temp.Path, ".backup");
        Directory.CreateDirectory(backupDir);

        // Seed 11 pre-existing backups with distinct, ordered timestamps.
        var baseTime = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        for (var i = 0; i < 11; i++)
        {
            var timestamp = baseTime.AddMinutes(i).ToString("yyyyMMddTHHmmss");
            File.WriteAllText(Path.Combine(backupDir, $"project-{timestamp}.db"), "old-backup");
        }

        var service = new ProjectBackupService();
        service.RotateBackup(temp.Path);

        var remaining = Directory.GetFiles(backupDir, "project-*.db");
        remaining.Should().HaveCount(10);

        // The two oldest seeded backups (minute 0 and minute 1) must have been pruned.
        remaining.Should().NotContain(f => f.Contains(baseTime.ToString("yyyyMMddTHHmmss")));
        remaining.Should().NotContain(f => f.Contains(baseTime.AddMinutes(1).ToString("yyyyMMddTHHmmss")));
    }
}
