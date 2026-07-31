using System.Text.Json;
using SciFiEditor.Domain;

namespace SciFiEditor.Core.Projects;

public sealed class RecentProjectsService
{
    private const int MaxEntries = 10;
    private readonly string _filePath;

    public RecentProjectsService()
        : this(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "SciFiEditor", "recent-projects.json"))
    {
    }

    public RecentProjectsService(string filePath)
    {
        _filePath = filePath;
    }

    public IReadOnlyList<RecentProjectEntry> GetAll()
    {
        if (!File.Exists(_filePath))
        {
            return Array.Empty<RecentProjectEntry>();
        }

        var json = File.ReadAllText(_filePath);
        return JsonSerializer.Deserialize<List<RecentProjectEntry>>(json) ?? [];
    }

    public void AddOrUpdate(RecentProjectEntry entry)
    {
        var entries = GetAll()
            .Where(e => !string.Equals(e.Path, entry.Path, StringComparison.OrdinalIgnoreCase))
            .ToList();
        entries.Insert(0, entry);
        Save(entries.Take(MaxEntries).ToList());
    }

    public void Remove(string path)
    {
        var entries = GetAll()
            .Where(e => !string.Equals(e.Path, path, StringComparison.OrdinalIgnoreCase))
            .ToList();
        Save(entries);
    }

    private void Save(IReadOnlyList<RecentProjectEntry> entries)
    {
        var directory = Path.GetDirectoryName(_filePath);
        if (!string.IsNullOrEmpty(directory))
        {
            Directory.CreateDirectory(directory);
        }

        var json = JsonSerializer.Serialize(entries, new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(_filePath, json);
    }
}
