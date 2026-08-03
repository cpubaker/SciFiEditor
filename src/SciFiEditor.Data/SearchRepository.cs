using Dapper;
using SciFiEditor.Domain;

namespace SciFiEditor.Data;

public sealed class SearchRepository
{
    private readonly ProjectDatabase _database;

    public SearchRepository(ProjectDatabase database)
    {
        _database = database;
    }

    public void IndexNode(Guid nodeId, string title, string synopsis, string notes, string label, string body)
    {
        var nodeIdText = nodeId.ToString();
        _database.Connection.Execute("DELETE FROM node_fts WHERE node_id = @NodeId", new { NodeId = nodeIdText });

        const string sql = """
            INSERT INTO node_fts (node_id, title, synopsis, notes, label, body)
            VALUES (@NodeId, @Title, @Synopsis, @Notes, @Label, @Body)
            """;
        _database.Connection.Execute(sql, new
        {
            NodeId = nodeIdText,
            Title = title,
            Synopsis = synopsis,
            Notes = notes,
            Label = label,
            Body = body
        });
    }

    public IReadOnlyList<SearchResult> Search(string query)
    {
        // Treat the whole input as a literal phrase so arbitrary user text (hyphens, quotes, etc.)
        // can't be misread as FTS5 query-language operators.
        var ftsQuery = "\"" + query.Replace("\"", "\"\"") + "\"";

        const string sql = """
            SELECT f.node_id AS NodeId, f.title AS Title,
                   snippet(node_fts, 5, '[', ']', '...', 10) AS Snippet
            FROM node_fts f
            INNER JOIN nodes n ON n.id = f.node_id
            WHERE node_fts MATCH @Query AND n.is_trashed = 0
            ORDER BY rank
            """;
        return _database.Connection.Query<SearchRow>(sql, new { Query = ftsQuery })
            .Select(row => new SearchResult(Guid.Parse(row.NodeId), row.Title, row.Snippet))
            .ToList();
    }

    public int GetIndexedCount() => _database.Connection.ExecuteScalar<int>("SELECT COUNT(*) FROM node_fts");

    private sealed class SearchRow
    {
        public string NodeId { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string Snippet { get; set; } = string.Empty;
    }
}
