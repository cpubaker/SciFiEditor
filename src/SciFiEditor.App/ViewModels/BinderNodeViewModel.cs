using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using SciFiEditor.App.Resources;
using SciFiEditor.Domain;

namespace SciFiEditor.App.ViewModels;

public sealed partial class BinderNodeViewModel : ObservableObject
{
    private readonly Action<BinderNodeViewModel, string>? _onRenameCommitted;

    public BinderNodeViewModel(ManuscriptNode node, Action<BinderNodeViewModel, string>? onRenameCommitted = null)
    {
        Node = node;
        _onRenameCommitted = onRenameCommitted;
        _title = ComputeDisplayTitle(node);
        _editTitle = _title;
    }

    public ManuscriptNode Node { get; private set; }

    public Guid Id => Node.Id;
    public NodeType NodeType => Node.NodeType;
    public bool IsTrashNode => Node.Id == WellKnownNodeIds.Trash;

    public ObservableCollection<BinderNodeViewModel> Children { get; } = new();

    [ObservableProperty]
    private string _title;

    [ObservableProperty]
    private string _editTitle;

    [ObservableProperty]
    private bool _isEditing;

    [ObservableProperty]
    private bool _isSelected;

    [ObservableProperty]
    private bool _isExpanded;

    public void UpdateNode(ManuscriptNode node)
    {
        Node = node;
        Title = ComputeDisplayTitle(node);
    }

    public void BeginRename()
    {
        if (IsTrashNode)
        {
            return;
        }

        EditTitle = Title;
        IsEditing = true;
    }

    public void CommitRename()
    {
        if (!IsEditing)
        {
            return;
        }

        IsEditing = false;
        var newTitle = EditTitle.Trim();
        if (string.IsNullOrEmpty(newTitle) || newTitle == Title)
        {
            return;
        }

        Title = newTitle;
        Node.Title = newTitle;
        _onRenameCommitted?.Invoke(this, newTitle);
    }

    public void CancelRename() => IsEditing = false;

    private static string ComputeDisplayTitle(ManuscriptNode node) =>
        node.Id == WellKnownNodeIds.Trash ? Strings.BinderTrashTitle : node.Title;
}
