using SciFiEditor.Core.Projects;
using SciFiEditor.Domain;

namespace SciFiEditor.Core.Entities;

public sealed class EntityService
{
    private readonly ProjectService _projectService;

    public EntityService(ProjectService projectService)
    {
        _projectService = projectService;
    }

    private OpenProject Project =>
        _projectService.Current ?? throw new InvalidOperationException("No project is open.");

    public StoryEntity AddEntity(EntityType entityType, string name, string description) =>
        Project.Entities.Insert(entityType, name, description);

    public void UpdateEntity(Guid id, string name, string description) =>
        Project.Entities.Update(id, name, description, DateTime.UtcNow);

    public void DeleteEntity(Guid id) => Project.Entities.Delete(id);

    public IReadOnlyList<StoryEntity> GetAll(EntityType? filter = null) => Project.Entities.GetAll(filter);

    public IReadOnlyList<StoryEntity> GetForNode(Guid nodeId) => Project.Entities.GetForNode(nodeId);

    public void SetLinksForNode(Guid nodeId, IEnumerable<Guid> entityIds) => Project.Entities.SetLinksForNode(nodeId, entityIds);
}
