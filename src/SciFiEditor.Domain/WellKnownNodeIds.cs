namespace SciFiEditor.Domain;

public static class WellKnownNodeIds
{
    // Fixed id so every project's Trash node is identifiable without a lookup.
    public static readonly Guid Trash = new("00000000-0000-0000-0000-000000000001");
}
