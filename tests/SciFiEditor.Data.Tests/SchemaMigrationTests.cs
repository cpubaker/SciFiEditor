using FluentAssertions;
using Microsoft.Data.Sqlite;
using SciFiEditor.Domain;

namespace SciFiEditor.Data.Tests;

public class SchemaMigrationTests
{
    [Fact]
    public void EnsureSchema_AddsMissingColumns_ToAnM1ShapedTable_WithoutDataLoss()
    {
        using var temp = new TempDirectory();
        var dbPath = Path.Combine(temp.Path, ProjectDatabase.DatabaseFileName);
        var nodeId = Guid.NewGuid();

        // Simulate a project.db created by the M1 schema, before the M2 inspector/word-count columns existed.
        using (var connection = new SqliteConnection($"Data Source={dbPath};Pooling=False"))
        {
            connection.Open();
            using var create = connection.CreateCommand();
            create.CommandText = """
                CREATE TABLE nodes (
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
                """;
            create.ExecuteNonQuery();

            using var insert = connection.CreateCommand();
            insert.CommandText = """
                INSERT INTO nodes (id, parent_id, node_type, title, sort_order, is_trashed, original_parent_id, created_at_utc, updated_at_utc)
                VALUES (@Id, NULL, 'Scene', 'Pre-existing Scene', 1000, 0, NULL, '2026-01-01T00:00:00.0000000Z', '2026-01-01T00:00:00.0000000Z')
                """;
            insert.Parameters.AddWithValue("@Id", nodeId.ToString());
            insert.ExecuteNonQuery();
        }

        using var database = new ProjectDatabase(temp.Path);
        var repository = new NodeRepository(database);

        var node = repository.GetById(nodeId);

        node.Should().NotBeNull();
        node!.Title.Should().Be("Pre-existing Scene");
        node.Synopsis.Should().BeEmpty();
        node.Notes.Should().BeEmpty();
        node.Label.Should().BeEmpty();
        node.Status.Should().Be(NodeStatus.None);
        node.TargetWordCount.Should().BeNull();
        node.WordCount.Should().Be(0);
        node.CharCount.Should().Be(0);
    }

    [Fact]
    public void EnsureSchema_IsIdempotent_WhenColumnsAlreadyMigrated()
    {
        using var temp = new TempDirectory();

        using (new ProjectDatabase(temp.Path))
        {
        }

        var act = () =>
        {
            using var second = new ProjectDatabase(temp.Path);
        };

        act.Should().NotThrow();
    }
}
