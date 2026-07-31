using FluentAssertions;

namespace SciFiEditor.Data.Tests;

public class ProjectDatabaseTests
{
    [Fact]
    public void Constructor_CreatesDatabaseFile_WhenNoneExists()
    {
        using var temp = new TempDirectory();

        using var database = new ProjectDatabase(temp.Path);

        File.Exists(Path.Combine(temp.Path, ProjectDatabase.DatabaseFileName)).Should().BeTrue();
    }

    [Fact]
    public void Constructor_IsIdempotent_WhenSchemaAlreadyExists()
    {
        using var temp = new TempDirectory();

        using (var first = new ProjectDatabase(temp.Path))
        {
            var node = new NodeRepository(first);
            node.Insert(new Domain.ManuscriptNode
            {
                Id = Guid.NewGuid(),
                NodeType = Domain.NodeType.Folder,
                Title = "Existing",
                SortOrder = 1000,
                CreatedAtUtc = DateTime.UtcNow,
                UpdatedAtUtc = DateTime.UtcNow
            });
        }

        using var second = new ProjectDatabase(temp.Path);
        var repository = new NodeRepository(second);

        repository.GetAll().Should().ContainSingle(n => n.Title == "Existing");
    }
}
