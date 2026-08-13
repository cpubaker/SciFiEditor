using System.Globalization;
using Dapper;
using SciFiEditor.Domain;

namespace SciFiEditor.Data;

public sealed class NodeRepository
{
    private const string SortOrderStep = "1000";
    private const string SelectColumns = """
        id AS Id, parent_id AS ParentId, node_type AS NodeType, title AS Title,
        sort_order AS SortOrder, is_trashed AS IsTrashed, original_parent_id AS OriginalParentId,
        created_at_utc AS CreatedAtUtc, updated_at_utc AS UpdatedAtUtc,
        synopsis AS Synopsis, notes AS Notes, label AS Label, status AS Status,
        target_word_count AS TargetWordCount, word_count AS WordCount, char_count AS CharCount,
        include_in_compile AS IncludeInCompile, is_expanded AS IsExpanded
        """;

    private readonly ProjectDatabase _database;

    public NodeRepository(ProjectDatabase database)
    {
        _database = database;
    }

    public IReadOnlyList<ManuscriptNode> GetAll()
    {
        var sql = $"SELECT {SelectColumns} FROM nodes ORDER BY sort_order";
        return _database.Connection.Query<NodeRow>(sql).Select(MapRow).ToList();
    }

    public ManuscriptNode? GetById(Guid id)
    {
        var sql = $"SELECT {SelectColumns} FROM nodes WHERE id = @Id";
        var row = _database.Connection.QuerySingleOrDefault<NodeRow>(sql, new { Id = id.ToString() });
        return row is null ? null : MapRow(row);
    }

    public void Insert(ManuscriptNode node)
    {
        const string sql = """
            INSERT INTO nodes (
                id, parent_id, node_type, title, sort_order, is_trashed, original_parent_id, created_at_utc, updated_at_utc,
                synopsis, notes, label, status, target_word_count, word_count, char_count, include_in_compile, is_expanded)
            VALUES (
                @Id, @ParentId, @NodeType, @Title, @SortOrder, @IsTrashed, @OriginalParentId, @CreatedAtUtc, @UpdatedAtUtc,
                @Synopsis, @Notes, @Label, @Status, @TargetWordCount, @WordCount, @CharCount, @IncludeInCompile, @IsExpanded)
            """;
        _database.Connection.Execute(sql, ToParameters(node));
    }

    public void UpdateTitle(Guid id, string title, DateTime updatedAtUtc)
    {
        const string sql = "UPDATE nodes SET title = @Title, updated_at_utc = @UpdatedAtUtc WHERE id = @Id";
        _database.Connection.Execute(sql, new { Id = id.ToString(), Title = title, UpdatedAtUtc = Format(updatedAtUtc) });
    }

    public void UpdateInspector(
        Guid id, string synopsis, string notes, string label, NodeStatus status, int? targetWordCount, DateTime updatedAtUtc)
    {
        const string sql = """
            UPDATE nodes
            SET synopsis = @Synopsis, notes = @Notes, label = @Label, status = @Status,
                target_word_count = @TargetWordCount, updated_at_utc = @UpdatedAtUtc
            WHERE id = @Id
            """;
        _database.Connection.Execute(sql, new
        {
            Id = id.ToString(),
            Synopsis = synopsis,
            Notes = notes,
            Label = label,
            Status = status.ToString(),
            TargetWordCount = targetWordCount,
            UpdatedAtUtc = Format(updatedAtUtc)
        });
    }

    public void UpdateWordCounts(Guid id, int wordCount, int charCount)
    {
        const string sql = "UPDATE nodes SET word_count = @WordCount, char_count = @CharCount WHERE id = @Id";
        _database.Connection.Execute(sql, new { Id = id.ToString(), WordCount = wordCount, CharCount = charCount });
    }

    public void UpdateIncludeInCompile(Guid id, bool include, DateTime updatedAtUtc)
    {
        const string sql = "UPDATE nodes SET include_in_compile = @Include, updated_at_utc = @UpdatedAtUtc WHERE id = @Id";
        _database.Connection.Execute(sql, new
        {
            Id = id.ToString(),
            Include = include ? 1 : 0,
            UpdatedAtUtc = Format(updatedAtUtc)
        });
    }

    public void UpdateExpanded(Guid id, bool isExpanded)
    {
        const string sql = "UPDATE nodes SET is_expanded = @IsExpanded WHERE id = @Id";
        _database.Connection.Execute(sql, new { Id = id.ToString(), IsExpanded = isExpanded ? 1 : 0 });
    }

    public int GetProjectWordCount()
    {
        const string sql = "SELECT COALESCE(SUM(word_count), 0) FROM nodes WHERE node_type = 'Scene' AND is_trashed = 0";
        return _database.Connection.ExecuteScalar<int>(sql);
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
        UpdatedAtUtc = Format(node.UpdatedAtUtc),
        node.Synopsis,
        node.Notes,
        node.Label,
        Status = node.Status.ToString(),
        node.TargetWordCount,
        node.WordCount,
        node.CharCount,
        IncludeInCompile = node.IncludeInCompile ? 1 : 0,
        IsExpanded = node.IsExpanded ? 1 : 0
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
        UpdatedAtUtc = DateTime.Parse(row.UpdatedAtUtc, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind),
        Synopsis = row.Synopsis,
        Notes = row.Notes,
        Label = row.Label,
        Status = Enum.Parse<NodeStatus>(row.Status),
        TargetWordCount = row.TargetWordCount is null ? null : (int)row.TargetWordCount.Value,
        WordCount = (int)row.WordCount,
        CharCount = (int)row.CharCount,
        IncludeInCompile = row.IncludeInCompile != 0,
        IsExpanded = row.IsExpanded != 0
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
        public string Synopsis { get; set; } = string.Empty;
        public string Notes { get; set; } = string.Empty;
        public string Label { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public long? TargetWordCount { get; set; }
        public long WordCount { get; set; }
        public long CharCount { get; set; }
        public long IncludeInCompile { get; set; }
        public long IsExpanded { get; set; }
    }
}
