using Microsoft.Data.Sqlite;

namespace SciFiEditor.Data;

public sealed class ProjectDatabase : IDisposable
{
    public const string DatabaseFileName = "project.db";

    public ProjectDatabase(string projectRootPath)
    {
        var dbPath = Path.Combine(projectRootPath, DatabaseFileName);
        Connection = new SqliteConnection($"Data Source={dbPath};Pooling=False");
        Connection.Open();

        using (var pragma = Connection.CreateCommand())
        {
            pragma.CommandText = "PRAGMA journal_mode=WAL;";
            pragma.ExecuteNonQuery();
        }

        EnsureSchema();
    }

    public SqliteConnection Connection { get; }

    private void EnsureSchema()
    {
        using var cmd = Connection.CreateCommand();
        cmd.CommandText = """
            CREATE TABLE IF NOT EXISTS nodes (
                id TEXT PRIMARY KEY,
                parent_id TEXT NULL,
                node_type TEXT NOT NULL,
                title TEXT NOT NULL,
                sort_order INTEGER NOT NULL,
                is_trashed INTEGER NOT NULL DEFAULT 0,
                original_parent_id TEXT NULL,
                created_at_utc TEXT NOT NULL,
                updated_at_utc TEXT NOT NULL
            );
            CREATE INDEX IF NOT EXISTS ix_nodes_parent_id ON nodes(parent_id);
            """;
        cmd.ExecuteNonQuery();
    }

    public void Dispose() => Connection.Dispose();
}
