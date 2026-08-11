using Microsoft.Data.Sqlite;

namespace SciFiEditor.Data;

public sealed class GlobalActivityDatabase : IDisposable
{
    public GlobalActivityDatabase()
        : this(Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "SciFiEditor",
            "global-activity.db"))
    {
    }

    public GlobalActivityDatabase(string databasePath)
    {
        var directory = Path.GetDirectoryName(databasePath);
        if (!string.IsNullOrEmpty(directory))
        {
            Directory.CreateDirectory(directory);
        }

        Connection = new SqliteConnection($"Data Source={databasePath};Pooling=False");
        Connection.Open();

        using (var pragma = Connection.CreateCommand())
        {
            pragma.CommandText = "PRAGMA journal_mode=WAL;";
            pragma.ExecuteNonQuery();
        }

        using (var cmd = Connection.CreateCommand())
        {
            cmd.CommandText = """
                CREATE TABLE IF NOT EXISTS project_daily_activity (
                    project_path TEXT NOT NULL,
                    date TEXT NOT NULL,
                    words_written INTEGER NOT NULL,
                    PRIMARY KEY (project_path, date)
                );
                """;
            cmd.ExecuteNonQuery();
        }
    }

    public SqliteConnection Connection { get; }

    public void Dispose() => Connection.Dispose();
}
