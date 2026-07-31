using System.Collections.ObjectModel;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GongSolutions.Wpf.DragDrop;
using SciFiEditor.App.Resources;
using SciFiEditor.App.Views;
using SciFiEditor.Core.Manuscript;
using SciFiEditor.Core.Projects;
using SciFiEditor.Data;
using SciFiEditor.Domain;
using ILogger = Serilog.ILogger;

namespace SciFiEditor.App.ViewModels;

public sealed partial class MainViewModel : ObservableObject, IDropTarget
{
    private readonly ProjectService _projectService;
    private readonly NodeService _nodeService;
    private readonly RecentProjectsService _recentProjectsService;
    private readonly ManuscriptFileService _fileService;
    private readonly SceneAutosaveCoordinator _autosaveCoordinator;
    private readonly ILogger _logger;
    private bool _isLoadingContent;

    public MainViewModel(
        ProjectService projectService,
        NodeService nodeService,
        RecentProjectsService recentProjectsService,
        ManuscriptFileService fileService,
        SceneAutosaveCoordinator autosaveCoordinator,
        ILogger logger)
    {
        _projectService = projectService;
        _nodeService = nodeService;
        _recentProjectsService = recentProjectsService;
        _fileService = fileService;
        _autosaveCoordinator = autosaveCoordinator;
        _logger = logger;

        RefreshRecentProjects();
    }

    public ObservableCollection<BinderNodeViewModel> RootNodes { get; } = new();
    public ObservableCollection<RecentProjectEntry> RecentProjects { get; } = new();

    [ObservableProperty]
    private BinderNodeViewModel? _selectedNode;

    [ObservableProperty]
    private string _statusText = Strings.StatusReady;

    [ObservableProperty]
    private bool _isProjectOpen;

    [ObservableProperty]
    private bool _isSceneSelected;

    [ObservableProperty]
    private string _editorContent = string.Empty;

    partial void OnEditorContentChanged(string value)
    {
        if (_isLoadingContent || SelectedNode is not { NodeType: NodeType.Scene } scene)
        {
            return;
        }

        _autosaveCoordinator.NotifyChanged(scene.Id, value);
    }

    public async Task OnBinderSelectionChangedAsync(BinderNodeViewModel? newSelection)
    {
        if (SelectedNode is { NodeType: NodeType.Scene })
        {
            await _autosaveCoordinator.FlushAsync();
        }

        SelectedNode = newSelection;
        IsSceneSelected = newSelection?.NodeType == NodeType.Scene;

        _isLoadingContent = true;
        EditorContent = IsSceneSelected && _projectService.Current is not null
            ? await _fileService.ReadSceneAsync(_projectService.Current.RootPath, newSelection!.Id)
            : string.Empty;
        _isLoadingContent = false;
    }

    public Task FlushAutosaveAsync() => _autosaveCoordinator.FlushAsync();

    [RelayCommand]
    private async Task SaveAsync() => await FlushAutosaveAsync();

    [RelayCommand]
    private void NewProject()
    {
        var dialog = new NewProjectWindow { Owner = Application.Current.MainWindow };
        if (dialog.ShowDialog() != true)
        {
            return;
        }

        try
        {
            _projectService.CreateProject(dialog.ProjectLocation, dialog.ProjectName);
            LoadTree();
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Failed to create project {Name} at {Location}", dialog.ProjectName, dialog.ProjectLocation);
            ShowError(ex.Message);
        }
    }

    [RelayCommand]
    private void OpenProject()
    {
        var dialog = new Microsoft.Win32.OpenFolderDialog { Title = Strings.MenuOpenProject };
        if (dialog.ShowDialog() != true)
        {
            return;
        }

        OpenProjectAt(dialog.FolderName);
    }

    [RelayCommand]
    private void OpenRecentProject(RecentProjectEntry? entry)
    {
        if (entry is not null)
        {
            OpenProjectAt(entry.Path);
        }
    }

    [RelayCommand]
    private void AddFolder(BinderNodeViewModel? parent) => AddNode(NodeType.Folder, parent);

    [RelayCommand]
    private void AddChapter(BinderNodeViewModel? parent) => AddNode(NodeType.Chapter, parent);

    [RelayCommand]
    private void AddScene(BinderNodeViewModel? parent) => AddNode(NodeType.Scene, parent);

    [RelayCommand]
    private void Rename(BinderNodeViewModel? node) => node?.BeginRename();

    [RelayCommand]
    private void Duplicate(BinderNodeViewModel? node)
    {
        if (node is null || node.IsTrashNode)
        {
            return;
        }

        try
        {
            _nodeService.Duplicate(node.Id);
            LoadTree();
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Failed to duplicate node {Id}", node.Id);
            ShowError(ex.Message);
        }
    }

    [RelayCommand]
    private void MoveToTrash(BinderNodeViewModel? node)
    {
        if (node is null || node.IsTrashNode)
        {
            return;
        }

        try
        {
            _nodeService.MoveToTrash(node.Id);
            LoadTree();
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Failed to move node {Id} to trash", node.Id);
            ShowError(ex.Message);
        }
    }

    private void OpenProjectAt(string path)
    {
        try
        {
            _projectService.OpenProject(path);
            LoadTree();
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Failed to open project at {Path}", path);
            ShowError(ex.Message);
        }
    }

    private void AddNode(NodeType nodeType, BinderNodeViewModel? parent)
    {
        try
        {
            var defaultTitle = nodeType switch
            {
                NodeType.Folder => Strings.BinderNewFolderTitle,
                NodeType.Chapter => Strings.BinderNewChapterTitle,
                _ => Strings.BinderNewSceneTitle
            };

            var created = _nodeService.AddNode(nodeType, defaultTitle, parent?.Id);
            var vm = new BinderNodeViewModel(created, OnRenameCommitted);
            var collection = parent?.Children ?? RootNodes;
            collection.Add(vm);
            vm.BeginRename();
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Failed to add {NodeType} node", nodeType);
            ShowError(ex.Message);
        }
    }

    private void OnRenameCommitted(BinderNodeViewModel node, string newTitle)
    {
        try
        {
            _nodeService.Rename(node.Id, newTitle);
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Failed to rename node {Id}", node.Id);
            ShowError(ex.Message);
        }
    }

    private void LoadTree()
    {
        var expandedIds = CollectExpandedIds(RootNodes);
        var selectedId = SelectedNode?.Id;

        RootNodes.Clear();
        var all = _nodeService.GetAll();
        var childrenByParent = all.ToLookup(n => n.ParentId);

        BinderNodeViewModel Build(ManuscriptNode node)
        {
            var vm = new BinderNodeViewModel(node, OnRenameCommitted) { IsExpanded = expandedIds.Contains(node.Id) };
            foreach (var child in childrenByParent[node.Id].OrderBy(n => n.SortOrder))
            {
                vm.Children.Add(Build(child));
            }

            return vm;
        }

        foreach (var root in childrenByParent[null].OrderBy(n => n.SortOrder))
        {
            RootNodes.Add(Build(root));
        }

        IsProjectOpen = true;
        RefreshRecentProjects();

        if (selectedId is Guid id)
        {
            SelectedNode = FindNode(RootNodes, id);
        }
    }

    private void RefreshRecentProjects()
    {
        RecentProjects.Clear();
        foreach (var entry in _recentProjectsService.GetAll())
        {
            RecentProjects.Add(entry);
        }
    }

    private static HashSet<Guid> CollectExpandedIds(IEnumerable<BinderNodeViewModel> nodes)
    {
        var result = new HashSet<Guid>();

        void Walk(IEnumerable<BinderNodeViewModel> items)
        {
            foreach (var item in items)
            {
                if (item.IsExpanded)
                {
                    result.Add(item.Id);
                }

                Walk(item.Children);
            }
        }

        Walk(nodes);
        return result;
    }

    private static BinderNodeViewModel? FindNode(IEnumerable<BinderNodeViewModel> nodes, Guid id)
    {
        foreach (var node in nodes)
        {
            if (node.Id == id)
            {
                return node;
            }

            var found = FindNode(node.Children, id);
            if (found is not null)
            {
                return found;
            }
        }

        return null;
    }

    private ObservableCollection<BinderNodeViewModel> GetCurrentCollection(BinderNodeViewModel node)
    {
        if (RootNodes.Contains(node))
        {
            return RootNodes;
        }

        return FindParentCollection(RootNodes, node) ?? RootNodes;
    }

    private static ObservableCollection<BinderNodeViewModel>? FindParentCollection(
        ObservableCollection<BinderNodeViewModel> nodes, BinderNodeViewModel target)
    {
        foreach (var candidate in nodes)
        {
            if (candidate.Children.Contains(target))
            {
                return candidate.Children;
            }

            var found = FindParentCollection(candidate.Children, target);
            if (found is not null)
            {
                return found;
            }
        }

        return null;
    }

    private static bool IsSameOrDescendant(BinderNodeViewModel node, BinderNodeViewModel possibleAncestor)
    {
        if (node == possibleAncestor)
        {
            return true;
        }

        foreach (var child in possibleAncestor.Children)
        {
            if (IsSameOrDescendant(node, child))
            {
                return true;
            }
        }

        return false;
    }

    void IDropTarget.DragOver(IDropInfo dropInfo)
    {
        if (dropInfo.Data is not BinderNodeViewModel source)
        {
            return;
        }

        if (source.IsTrashNode)
        {
            dropInfo.Effects = DragDropEffects.None;
            return;
        }

        if (dropInfo.TargetItem is BinderNodeViewModel target)
        {
            if (target.IsTrashNode || IsSameOrDescendant(target, source))
            {
                dropInfo.Effects = DragDropEffects.None;
                return;
            }
        }

        dropInfo.DropTargetAdorner = DropTargetAdorners.Insert;
        dropInfo.Effects = DragDropEffects.Move;
    }

    void IDropTarget.Drop(IDropInfo dropInfo)
    {
        if (dropInfo.Data is not BinderNodeViewModel source || source.IsTrashNode)
        {
            return;
        }

        var targetItem = dropInfo.TargetItem as BinderNodeViewModel;
        var dropInto = dropInfo.InsertPosition.HasFlag(RelativeInsertPosition.TargetItemCenter)
            && targetItem is not null
            && targetItem.NodeType != NodeType.Scene
            && !targetItem.IsTrashNode;

        ObservableCollection<BinderNodeViewModel> targetCollection;
        Guid? newParentId;
        int insertIndex;

        if (dropInto)
        {
            if (targetItem is null || IsSameOrDescendant(targetItem, source))
            {
                return;
            }

            targetCollection = targetItem.Children;
            newParentId = targetItem.Id;
            insertIndex = targetCollection.Count;
        }
        else
        {
            if (targetItem is not null && targetItem.IsTrashNode)
            {
                return;
            }

            newParentId = targetItem?.Node.ParentId;
            targetCollection = newParentId is null ? RootNodes : FindNode(RootNodes, newParentId.Value)?.Children ?? RootNodes;
            insertIndex = targetItem is null ? targetCollection.Count : targetCollection.IndexOf(targetItem);
            if (targetItem is not null && dropInfo.InsertPosition.HasFlag(RelativeInsertPosition.AfterTargetItem))
            {
                insertIndex++;
            }
        }

        var oldCollection = GetCurrentCollection(source);
        var oldIndex = oldCollection.IndexOf(source);
        oldCollection.RemoveAt(oldIndex);

        if (ReferenceEquals(oldCollection, targetCollection) && oldIndex < insertIndex)
        {
            insertIndex--;
        }

        insertIndex = Math.Clamp(insertIndex, 0, targetCollection.Count);
        targetCollection.Insert(insertIndex, source);
        source.Node.ParentId = newParentId;

        PersistSortOrder(targetCollection, newParentId);
    }

    private void PersistSortOrder(ObservableCollection<BinderNodeViewModel> siblings, Guid? parentId)
    {
        var updates = new List<NodeSortUpdate>();
        var order = 1000;
        foreach (var sibling in siblings)
        {
            updates.Add(new NodeSortUpdate(sibling.Id, parentId, order));
            order += 1000;
        }

        try
        {
            _nodeService.Reorder(updates);
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Failed to persist binder reorder");
            ShowError(ex.Message);
        }
    }

    private void ShowError(string message) =>
        MessageBox.Show(Application.Current.MainWindow, message, Strings.ErrorTitle, MessageBoxButton.OK, MessageBoxImage.Error);
}
