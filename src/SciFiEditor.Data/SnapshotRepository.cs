using System.Globalization;
using System.IO.Compression;
using System.Text;
using Dapper;
using SciFiEditor.Domain;

namespace SciFiEditor.Data;

public sealed class SnapshotRepository
{
    private const string DateFormat = "yyyy-MM-dd";
    private readonly ProjectDatabase _database;

    public SnapshotRepository(ProjectDatabase database)
    {
        _database = database;
    }

    public void Insert(Guid nodeId, DateTime createdAtUtc, string label, string content)
    {
        const string sql = """
            INSERT INTO scene_snapshots (id, node_id, created_at_utc, label, content_gzip)
            VALUES (@Id, @NodeId, @CreatedAtUtc, @Label, @ContentGzip)
            """;
        _database.Connection.Execute(sql, new
        {
            Id = Guid.NewGuid().ToString(),
            NodeId = nodeId.ToString(),
            CreatedAtUtc = Format(createdAtUtc),
            Label = label,
            ContentGzip = Compress(content)
        });
    }

    public IReadOnlyList<SceneSnapshotInfo> GetByNode(Guid nodeId)
    {
        const string sql = """
            SELECT id AS Id, node_id AS NodeId, created_at_utc AS CreatedAtUtc, label AS Label
            FROM scene_snapshots
            WHERE node_id = @NodeId
            ORDER BY created_at_utc DESC
            """;
        return _database.Connection.Query<SnapshotRow>(sql, new { NodeId = nodeId.ToString() })
            .Select(MapRow)
            .ToList();
    }

    public string GetContent(Guid snapshotId)
    {
        const string sql = "SELECT content_gzip FROM scene_snapshots WHERE id = @Id";
        var bytes = _database.Connection.QuerySingle<byte[]>(sql, new { Id = snapshotId.ToString() });
        return Decompress(bytes);
    }

    public bool HasSnapshotToday(Guid nodeId, string label, DateOnly today)
    {
        const string sql = """
            SELECT COUNT(1) FROM scene_snapshots
            WHERE node_id = @NodeId AND label = @Label AND substr(created_at_utc, 1, 10) = @Today
            """;
        var count = _database.Connection.ExecuteScalar<int>(sql, new
        {
            NodeId = nodeId.ToString(),
            Label = label,
            Today = today.ToString(DateFormat, CultureInfo.InvariantCulture)
        });
        return count > 0;
    }

    private static string Format(DateTime value) => value.ToString("o", CultureInfo.InvariantCulture);

    private static byte[] Compress(string content)
    {
        using var output = new MemoryStream();
        using (var gzip = new GZipStream(output, CompressionMode.Compress, leaveOpen: true))
        {
            var bytes = Encoding.UTF8.GetBytes(content);
            gzip.Write(bytes, 0, bytes.Length);
        }

        return output.ToArray();
    }

    private static string Decompress(byte[] gzipBytes)
    {
        using var input = new MemoryStream(gzipBytes);
        using var gzip = new GZipStream(input, CompressionMode.Decompress);
        using var reader = new StreamReader(gzip, Encoding.UTF8);
        return reader.ReadToEnd();
    }

    private static SceneSnapshotInfo MapRow(SnapshotRow row) => new(
        Guid.Parse(row.Id),
        Guid.Parse(row.NodeId),
        DateTime.Parse(row.CreatedAtUtc, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind),
        row.Label);

    private sealed class SnapshotRow
    {
        public string Id { get; set; } = string.Empty;
        public string NodeId { get; set; } = string.Empty;
        public string CreatedAtUtc { get; set; } = string.Empty;
        public string Label { get; set; } = string.Empty;
    }
}
