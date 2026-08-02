using SciFiEditor.Data;

namespace SciFiEditor.Core.Projects;

public sealed class OpenProject : IDisposable
{
    public OpenProject(string rootPath)
    {
        RootPath = rootPath;
        Database = new ProjectDatabase(rootPath);
        Nodes = new NodeRepository(Database);
        Stats = new StatsRepository(Database);
    }

    public string RootPath { get; }
    public ProjectDatabase Database { get; }
    public NodeRepository Nodes { get; }
    public StatsRepository Stats { get; }

    public void Dispose() => Database.Dispose();
}
