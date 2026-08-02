using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using SciFiEditor.App.Resources;
using SciFiEditor.Domain;

namespace SciFiEditor.App.ViewModels;

public sealed partial class BinderNodeViewModel : ObservableObject
{
    private readonly Action<BinderNodeViewModel, string>? _onRenameCommitted;
    private readonly Action<BinderNodeViewModel>? _onInspectorCommitted;

    public BinderNodeViewModel(
        ManuscriptNode node,
        Action<BinderNodeViewModel, string>? onRenameCommitted = null,
        Action<BinderNodeViewModel>? onInspectorCommitted = null)
    {
        Node = node;
        _onRenameCommitted = onRenameCommitted;
        _onInspectorCommitted = onInspectorCommitted;
        _title = ComputeDisplayTitle(node);
        _editTitle = _title;
        _synopsis = node.Synopsis;
        _notes = node.Notes;
        _label = node.Label;
        _status = node.Status;
        _targetWordCount = node.TargetWordCount;
        _wordCount = node.WordCount;
        _charCount = node.CharCount;
        _includeInCompile = node.IncludeInCompile;
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

    [ObservableProperty]
    private string _synopsis;

    [ObservableProperty]
    private string _notes;

    [ObservableProperty]
    private string _label;

    [ObservableProperty]
    private NodeStatus _status;

    [ObservableProperty]
    private int? _targetWordCount;

    [ObservableProperty]
    private int _wordCount;

    [ObservableProperty]
    private int _charCount;

    [ObservableProperty]
    private bool _includeInCompile;

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

    public void CommitInspector()
    {
        if (IsTrashNode)
        {
            return;
        }

        Node.Synopsis = Synopsis;
        Node.Notes = Notes;
        Node.Label = Label;
        Node.Status = Status;
        Node.TargetWordCount = TargetWordCount;
        _onInspectorCommitted?.Invoke(this);
    }

    public void UpdateWordCount(int wordCount, int charCount)
    {
        WordCount = wordCount;
        CharCount = charCount;
        Node.WordCount = wordCount;
        Node.CharCount = charCount;
    }

    private static string ComputeDisplayTitle(ManuscriptNode node) =>
        node.Id == WellKnownNodeIds.Trash ? Strings.BinderTrashTitle : node.Title;
}
