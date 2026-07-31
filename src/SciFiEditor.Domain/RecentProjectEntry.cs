namespace SciFiEditor.Domain;

public class RecentProjectEntry
{
    public string Path { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public DateTime LastOpenedUtc { get; set; }
}
