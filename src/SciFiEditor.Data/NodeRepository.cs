using System.Globalization;
using Dapper;
using SciFiEditor.Domain;

namespace SciFiEditor.Data;

public sealed class NodeRepository
{
    private const string SortOrderStep = "1000";
    private readonly ProjectDatabase _database;

    public NodeRepository(ProjectDatabase database)
    {
        _database = database;
    }

    public IReadOnlyList<ManuscriptNode> GetAll()
    {
        const string sql = """
            SELECT id AS Id, parent_id AS ParentId, node_type AS NodeType, title AS Title,
                   sort_order AS SortOrder, is_trashed AS IsTrashed, original_parent_id AS OriginalParentId,
                   created_at_utc AS CreatedAtUtc, updated_at_utc AS UpdatedAtUtc
            FROM nodes
            ORDER BY sort_order
            """;
        return _database.Connection.Query<NodeRow>(sql).Select(MapRow).ToList();
    }

    public ManuscriptNode? GetById(Guid id)
    {
        const string sql = """
            SELECT id AS Id, parent_id AS ParentId, node_type AS NodeType, title AS Title,
                   sort_order AS SortOrder, is_trashed AS IsTrashed, original_parent_id AS OriginalParentId,
                   created_at_utc AS CreatedAtUtc, updated_at_utc AS UpdatedAtUtc
            FROM nodes
            WHERE id = @Id
            """;
        var row = _database.Connection.QuerySingleOrDefault<NodeRow>(sql, new { Id = id.ToString() });
        return row is null ? null : MapRow(row);
    }

    public void Insert(ManuscriptNode node)
    {
        const string sql = """
            INSERT INTO nodes (id, parent_id, node_type, title, sort_order, is_trashed, original_parent_id, created_at_utc, updated_at_utc)
            VALUES (@Id, @ParentId, @NodeType, @Title, @SortOrder, @IsTrashed, @OriginalParentId, @CreatedAtUtc, @UpdatedAtUtc)
            """;
        _database.Connection.Execute(sql, ToParameters(node));
    }

    public void UpdateTitle(Guid id, string title, DateTime updatedAtUtc)
    {
        const string sql = "UPDATE nodes SET title = @Title, updated_at_utc = @UpdatedAtUtc WHERE id = @Id";
        _database.Connection.Execute(sql, new { Id = id.ToString(), Title = title, UpdatedAtUtc = Format(updatedAtUtc) });
    }

    public void MoveToTrash(Guid id, DateTime updatedAtUtc)
    {
        var node = GetById(id) ?? throw new InvalidOperationException($"Node {id} not found.");
        const string sql = """
            UPDATE nodes
            SET parent_id = @TrashId, is_trashed = 1, original_parent_id = @OriginalParentId, updated_at_utc = @UpdatedAtUtc
            WHERE id = @Id
            """;
        _database.Connection.Execute(sql, new
        {
            Id = id.ToString(),
            TrashId = WellKnownNodeIds.Trash.ToString(),
            OriginalParentId = node.ParentId?.ToString(),
            UpdatedAtUtc = Format(updatedAtUtc)
        });
    }

    public void UpdateSortOrders(IReadOnlyList<NodeSortUpdate> updates)
    {
        using var transaction = _database.Connection.BeginTransaction();
        const string sql = "UPDATE nodes SET parent_id = @ParentId, sort_order = @SortOrder WHERE id = @Id";
        foreach (var update in updates)
        {
            _database.Connection.Execute(sql, new
            {
                Id = update.Id.ToString(),
                ParentId = update.ParentId?.ToString(),
                update.SortOrder
            }, transaction);
        }
        transaction.Commit();
    }

    public void Delete(Guid id)
    {
        const string sql = "DELETE FROM nodes WHERE id = @Id";
        _database.Connection.Execute(sql, new { Id = id.ToString() });
    }

    public int GetNextSortOrder(Guid? parentId)
    {
        const string sql = "SELECT MAX(sort_order) FROM nodes WHERE parent_id IS @ParentId";
        var max = _database.Connection.ExecuteScalar<int?>(sql, new { ParentId = parentId?.ToString() });
        return (max ?? 0) + int.Parse(SortOrderStep, CultureInfo.InvariantCulture);
    }

    private static object ToParameters(ManuscriptNode node) => new
    {
        Id = node.Id.ToString(),
        ParentId = node.ParentId?.ToString(),
        NodeType = node.NodeType.ToString(),
        node.Title,
        node.SortOrder,
        IsTrashed = node.IsTrashed ? 1 : 0,
        OriginalParentId = node.OriginalParentId?.ToString(),
        CreatedAtUtc = Format(node.CreatedAtUtc),
        UpdatedAtUtc = Format(node.UpdatedAtUtc)
    };

    private static string Format(DateTime value) => value.ToString("o", CultureInfo.InvariantCulture);

    private static ManuscriptNode MapRow(NodeRow row) => new()
    {
        Id = Guid.Parse(row.Id),
        ParentId = row.ParentId is null ? null : Guid.Parse(row.ParentId),
        NodeType = Enum.Parse<NodeType>(row.NodeType),
        Title = row.Title,
        SortOrder = row.SortOrder,
        IsTrashed = row.IsTrashed != 0,
        OriginalParentId = row.OriginalParentId is null ? null : Guid.Parse(row.OriginalParentId),
        CreatedAtUtc = DateTime.Parse(row.CreatedAtUtc, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind),
        UpdatedAtUtc = DateTime.Parse(row.UpdatedAtUtc, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind)
    };

    private sealed class NodeRow
    {
        public string Id { get; set; } = string.Empty;
        public string? ParentId { get; set; }
        public string NodeType { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public int SortOrder { get; set; }
        public long IsTrashed { get; set; }
        public string? OriginalParentId { get; set; }
        public string CreatedAtUtc { get; set; } = string.Empty;
        public string UpdatedAtUtc { get; set; } = string.Empty;
    }
}
