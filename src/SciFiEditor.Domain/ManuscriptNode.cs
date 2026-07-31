namespace SciFiEditor.Domain;

public class ManuscriptNode
{
    public Guid Id { get; set; }
    public Guid? ParentId { get; set; }
    public NodeType NodeType { get; set; }
    public string Title { get; set; } = string.Empty;
    public int SortOrder { get; set; }
    public bool IsTrashed { get; set; }
    public Guid? OriginalParentId { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public DateTime UpdatedAtUtc { get; set; }
}
