namespace SciFiEditor.Domain;

public sealed record StoryEntity(
    Guid Id,
    EntityType EntityType,
    string Name,
    string Description,
    DateTime CreatedAtUtc,
    DateTime UpdatedAtUtc);
