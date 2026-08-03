using System.Globalization;
using Dapper;
using SciFiEditor.Domain;

namespace SciFiEditor.Data;

public sealed class EntityRepository
{
    private readonly ProjectDatabase _database;

    public EntityRepository(ProjectDatabase database)
    {
        _database = database;
    }

    public StoryEntity Insert(EntityType entityType, string name, string description)
    {
        var now = DateTime.UtcNow;
        var id = Guid.NewGuid();
        const string sql = """
            INSERT INTO entities (id, entity_type, name, description, created_at_utc, updated_at_utc)
            VALUES (@Id, @EntityType, @Name, @Description, @CreatedAtUtc, @UpdatedAtUtc)
            """;
        _database.Connection.Execute(sql, new
        {
            Id = id.ToString(),
            EntityType = entityType.ToString(),
            Name = name,
            Description = description,
            CreatedAtUtc = Format(now),
            UpdatedAtUtc = Format(now)
        });

        return new StoryEntity(id, entityType, name, description, now, now);
    }

    public void Update(Guid id, string name, string description, DateTime updatedAtUtc)
    {
        const string sql = "UPDATE entities SET name = @Name, description = @Description, updated_at_utc = @UpdatedAtUtc WHERE id = @Id";
        _database.Connection.Execute(sql, new
        {
            Id = id.ToString(),
            Name = name,
            Description = description,
            UpdatedAtUtc = Format(updatedAtUtc)
        });
    }

    public void Delete(Guid id)
    {
        _database.Connection.Execute("DELETE FROM node_entities WHERE entity_id = @Id", new { Id = id.ToString() });
        _database.Connection.Execute("DELETE FROM entities WHERE id = @Id", new { Id = id.ToString() });
    }

    public IReadOnlyList<StoryEntity> GetAll(EntityType? filter = null)
    {
        var sql = """
            SELECT id AS Id, entity_type AS EntityType, name AS Name, description AS Description,
                   created_at_utc AS CreatedAtUtc, updated_at_utc AS UpdatedAtUtc
            FROM entities
            """;
        if (filter is not null)
        {
            sql += " WHERE entity_type = @EntityType";
        }

        sql += " ORDER BY name";

        return _database.Connection
            .Query<EntityRow>(sql, new { EntityType = filter?.ToString() })
            .Select(MapRow)
            .ToList();
    }

    public IReadOnlyList<StoryEntity> GetForNode(Guid nodeId)
    {
        const string sql = """
            SELECT e.id AS Id, e.entity_type AS EntityType, e.name AS Name, e.description AS Description,
                   e.created_at_utc AS CreatedAtUtc, e.updated_at_utc AS UpdatedAtUtc
            FROM entities e
            INNER JOIN node_entities ne ON ne.entity_id = e.id
            WHERE ne.node_id = @NodeId
            ORDER BY e.name
            """;
        return _database.Connection.Query<EntityRow>(sql, new { NodeId = nodeId.ToString() })
            .Select(MapRow)
            .ToList();
    }

    public void SetLinksForNode(Guid nodeId, IEnumerable<Guid> entityIds)
    {
        using var transaction = _database.Connection.BeginTransaction();
        _database.Connection.Execute(
            "DELETE FROM node_entities WHERE node_id = @NodeId", new { NodeId = nodeId.ToString() }, transaction);

        const string insertSql = "INSERT INTO node_entities (node_id, entity_id) VALUES (@NodeId, @EntityId)";
        foreach (var entityId in entityIds)
        {
            _database.Connection.Execute(insertSql, new { NodeId = nodeId.ToString(), EntityId = entityId.ToString() }, transaction);
        }

        transaction.Commit();
    }

    private static string Format(DateTime value) => value.ToString("o", CultureInfo.InvariantCulture);

    private static StoryEntity MapRow(EntityRow row) => new(
        Guid.Parse(row.Id),
        Enum.Parse<EntityType>(row.EntityType),
        row.Name,
        row.Description,
        DateTime.Parse(row.CreatedAtUtc, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind),
        DateTime.Parse(row.UpdatedAtUtc, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind));

    private sealed class EntityRow
    {
        public string Id { get; set; } = string.Empty;
        public string EntityType { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string CreatedAtUtc { get; set; } = string.Empty;
        public string UpdatedAtUtc { get; set; } = string.Empty;
    }
}
