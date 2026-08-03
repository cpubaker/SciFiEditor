using CommunityToolkit.Mvvm.ComponentModel;

namespace SciFiEditor.App.ViewModels;

public sealed partial class EntityLinkOption : ObservableObject
{
    public EntityLinkOption(Guid entityId, string name, bool isLinked)
    {
        EntityId = entityId;
        Name = name;
        _isLinked = isLinked;
    }

    public Guid EntityId { get; }
    public string Name { get; }

    [ObservableProperty]
    private bool _isLinked;
}
