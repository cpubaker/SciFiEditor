namespace SciFiEditor.Domain;

public sealed record SceneSnapshotInfo(Guid Id, Guid NodeId, DateTime CreatedAtUtc, string Label);
