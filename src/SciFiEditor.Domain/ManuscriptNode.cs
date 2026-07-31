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

    public string Synopsis { get; set; } = string.Empty;
    public string Notes { get; set; } = string.Empty;
    public string Label { get; set; } = string.Empty;
    public NodeStatus Status { get; set; } = NodeStatus.None;
    public int? TargetWordCount { get; set; }
    public int WordCount { get; set; }
    public int CharCount { get; set; }
}
