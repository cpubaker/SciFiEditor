namespace SciFiEditor.Data;

public sealed record NodeSortUpdate(Guid Id, Guid? ParentId, int SortOrder);
