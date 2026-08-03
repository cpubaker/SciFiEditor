using Microsoft.Data.Sqlite;

namespace SciFiEditor.Data;

public sealed class ProjectDatabase : IDisposable
{
    public const string DatabaseFileName = "project.db";

    private static readonly (string Name, string Definition)[] MigratedColumns =
    [
        ("synopsis", "TEXT NOT NULL DEFAULT ''"),
        ("notes", "TEXT NOT NULL DEFAULT ''"),
        ("label", "TEXT NOT NULL DEFAULT ''"),
        ("status", "TEXT NOT NULL DEFAULT 'None'"),
        ("target_word_count", "INTEGER NULL"),
        ("word_count", "INTEGER NOT NULL DEFAULT 0"),
        ("char_count", "INTEGER NOT NULL DEFAULT 0"),
        ("include_in_compile", "INTEGER NOT NULL DEFAULT 1")
    ];

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
        using (var cmd = Connection.CreateCommand())
        {
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
                    updated_at_utc TEXT NOT NULL,
                    synopsis TEXT NOT NULL DEFAULT '',
                    notes TEXT NOT NULL DEFAULT '',
                    label TEXT NOT NULL DEFAULT '',
                    status TEXT NOT NULL DEFAULT 'None',
                    target_word_count INTEGER NULL,
                    word_count INTEGER NOT NULL DEFAULT 0,
                    char_count INTEGER NOT NULL DEFAULT 0,
                    include_in_compile INTEGER NOT NULL DEFAULT 1
                );
                CREATE INDEX IF NOT EXISTS ix_nodes_parent_id ON nodes(parent_id);

                CREATE TABLE IF NOT EXISTS daily_stats (
                    date TEXT PRIMARY KEY,
                    word_count_total INTEGER NOT NULL,
                    words_written INTEGER NOT NULL
                );

                CREATE TABLE IF NOT EXISTS project_settings (
                    id INTEGER PRIMARY KEY CHECK (id = 1),
                    daily_goal INTEGER NULL,
                    session_goal INTEGER NULL
                );

                CREATE TABLE IF NOT EXISTS scene_snapshots (
                    id TEXT PRIMARY KEY,
                    node_id TEXT NOT NULL,
                    created_at_utc TEXT NOT NULL,
                    label TEXT NOT NULL,
                    content_gzip BLOB NOT NULL
                );
                CREATE INDEX IF NOT EXISTS ix_scene_snapshots_node_id ON scene_snapshots(node_id);

                CREATE TABLE IF NOT EXISTS entities (
                    id TEXT PRIMARY KEY,
                    entity_type TEXT NOT NULL,
                    name TEXT NOT NULL,
                    description TEXT NOT NULL DEFAULT '',
                    created_at_utc TEXT NOT NULL,
                    updated_at_utc TEXT NOT NULL
                );

                CREATE TABLE IF NOT EXISTS node_entities (
                    node_id TEXT NOT NULL,
                    entity_id TEXT NOT NULL,
                    PRIMARY KEY (node_id, entity_id)
                );
                """;
            cmd.ExecuteNonQuery();
        }

        MigrateMissingColumns();
    }

    private void MigrateMissingColumns()
    {
        var existingColumns = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        using (var pragma = Connection.CreateCommand())
        {
            pragma.CommandText = "PRAGMA table_info(nodes);";
            using var reader = pragma.ExecuteReader();
            while (reader.Read())
            {
                existingColumns.Add(reader.GetString(reader.GetOrdinal("name")));
            }
        }

        foreach (var (name, definition) in MigratedColumns)
        {
            if (existingColumns.Contains(name))
            {
                continue;
            }

            using var alter = Connection.CreateCommand();
            alter.CommandText = $"ALTER TABLE nodes ADD COLUMN {name} {definition};";
            alter.ExecuteNonQuery();
        }
    }

    public void Dispose() => Connection.Dispose();
}
