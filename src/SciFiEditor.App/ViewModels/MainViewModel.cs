using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GongSolutions.Wpf.DragDrop;
using SciFiEditor.App.Resources;
using SciFiEditor.App.Theming;
using SciFiEditor.App.Views;
using SciFiEditor.Core.Compile;
using SciFiEditor.Core.Entities;
using SciFiEditor.Core.Manuscript;
using SciFiEditor.Core.Projects;
using SciFiEditor.Core.Search;
using SciFiEditor.Core.Settings;
using SciFiEditor.Core.Snapshots;
using SciFiEditor.Core.Stats;
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
    private readonly WordCountCoordinator _wordCountCoordinator;
    private readonly WritingStatsService _statsService;
    private readonly CompileService _compileService;
    private readonly ExportService _exportService;
    private readonly SnapshotService _snapshotService;
    private readonly EntityService _entityService;
    private readonly SearchService _searchService;
    private readonly AppSettingsService _appSettingsService;
    private readonly ILogger _logger;
    private readonly DispatcherTimer _previewTimer;
    private SearchWindow? _searchWindow;
    private bool _isLoadingContent;
    private int _sessionBaselineWordCount;

    public MainViewModel(
        ProjectService projectService,
        NodeService nodeService,
        RecentProjectsService recentProjectsService,
        ManuscriptFileService fileService,
        SceneAutosaveCoordinator autosaveCoordinator,
        WordCountCoordinator wordCountCoordinator,
        WritingStatsService statsService,
        CompileService compileService,
        ExportService exportService,
        SnapshotService snapshotService,
        EntityService entityService,
        SearchService searchService,
        AppSettingsService appSettingsService,
        ILogger logger)
    {
        _projectService = projectService;
        _nodeService = nodeService;
        _recentProjectsService = recentProjectsService;
        _fileService = fileService;
        _autosaveCoordinator = autosaveCoordinator;
        _wordCountCoordinator = wordCountCoordinator;
        _statsService = statsService;
        _compileService = compileService;
        _exportService = exportService;
        _snapshotService = snapshotService;
        _entityService = entityService;
        _searchService = searchService;
        _appSettingsService = appSettingsService;
        _logger = logger;

        _wordCountCoordinator.Counted += OnWordCountPersisted;

        _previewTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(300) };
        _previewTimer.Tick += (_, _) =>
        {
            _previewTimer.Stop();
            PreviewHtml = MarkdownPreviewService.ToHtml(EditorContent);
        };

        RefreshRecentProjects();
    }

    public ObservableCollection<BinderNodeViewModel> RootNodes { get; } = new();
    public ObservableCollection<RecentProjectEntry> RecentProjects { get; } = new();
    public ObservableCollection<EntityLinkOption> CharacterLinkOptions { get; } = new();
    public ObservableCollection<EntityLinkOption> LocationLinkOptions { get; } = new();

    public IReadOnlyList<NodeStatusOption> StatusOptions { get; } =
    [
        new(NodeStatus.None, Strings.StatusNone),
        new(NodeStatus.Draft, Strings.StatusDraft),
        new(NodeStatus.Revised, Strings.StatusRevised),
        new(NodeStatus.Final, Strings.StatusFinal)
    ];

    [ObservableProperty]
    private BinderNodeViewModel? _selectedNode;

    [ObservableProperty]
    private bool _isFocusMode;

    public ObservableCollection<BinderNodeViewModel> CorkboardNodes =>
        SelectedNode?.Children.Count > 0 ? SelectedNode.Children : RootNodes;

    partial void OnSelectedNodeChanged(BinderNodeViewModel? value) => OnPropertyChanged(nameof(CorkboardNodes));

    [ObservableProperty]
    private string _statusText = Strings.StatusReady;

    [ObservableProperty]
    private bool _isProjectOpen;

    [ObservableProperty]
    private bool _isSceneSelected;

    [ObservableProperty]
    private string _editorContent = string.Empty;

    [ObservableProperty]
    private int _sceneWordCount;

    [ObservableProperty]
    private int _sceneCharCount;

    [ObservableProperty]
    private int _projectWordCount;

    [ObservableProperty]
    private int _sessionWordDelta;

    [ObservableProperty]
    private string _previewHtml = string.Empty;

    partial void OnEditorContentChanged(string value)
    {
        if (_isLoadingContent || SelectedNode is not { NodeType: NodeType.Scene } scene)
        {
            return;
        }

        _autosaveCoordinator.NotifyChanged(scene.Id, value);
        _wordCountCoordinator.NotifyChanged(scene.Id, value);

        _previewTimer.Stop();
        _previewTimer.Start();
    }

    public async Task OnBinderSelectionChangedAsync(BinderNodeViewModel? newSelection)
    {
        if (SelectedNode is { NodeType: NodeType.Scene })
        {
            await _autosaveCoordinator.FlushAsync();
            await _wordCountCoordinator.FlushAsync();
        }

        SelectedNode = newSelection;
        IsSceneSelected = newSelection?.NodeType == NodeType.Scene;
        SceneWordCount = newSelection?.WordCount ?? 0;
        SceneCharCount = newSelection?.CharCount ?? 0;

        _isLoadingContent = true;
        EditorContent = IsSceneSelected && _projectService.Current is not null
            ? await _fileService.ReadSceneAsync(_projectService.Current.RootPath, newSelection!.Id)
            : string.Empty;
        _isLoadingContent = false;

        _previewTimer.Stop();
        PreviewHtml = MarkdownPreviewService.ToHtml(EditorContent);

        RefreshEntityLinkOptions();
    }

    public async Task FlushAutosaveAsync()
    {
        await _autosaveCoordinator.FlushAsync();
        await _wordCountCoordinator.FlushAsync();

        if (_projectService.Current is not null)
        {
            _statsService.RecordSnapshot();

            if (SelectedNode is { NodeType: NodeType.Scene } scene)
            {
                _snapshotService.RecordAutoSnapshotIfNeeded(scene.Id, EditorContent);
                _ = IndexNodeSafeAsync(scene.Id);
            }
        }
    }

    private async Task IndexNodeSafeAsync(Guid nodeId)
    {
        try
        {
            await _searchService.IndexNodeAsync(nodeId);
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Failed to index node {Id} for search", nodeId);
        }
    }

    private void OnWordCountPersisted(Guid nodeId, int words, int chars)
    {
        Application.Current.Dispatcher.BeginInvoke(() =>
        {
            if (SelectedNode?.Id == nodeId)
            {
                SelectedNode.UpdateWordCount(words, chars);
                SceneWordCount = words;
                SceneCharCount = chars;
            }

            RefreshProjectWordCount();
            _statsService.RecordSnapshot();
        });
    }

    private void RefreshProjectWordCount(bool resetSessionBaseline = false)
    {
        if (_projectService.Current is null)
        {
            return;
        }

        ProjectWordCount = _nodeService.GetProjectWordCount();
        if (resetSessionBaseline)
        {
            _sessionBaselineWordCount = ProjectWordCount;
        }

        SessionWordDelta = ProjectWordCount - _sessionBaselineWordCount;
    }

    [RelayCommand]
    private async Task SaveAsync() => await FlushAutosaveAsync();

    [RelayCommand]
    private void OpenStats()
    {
        var viewModel = new StatsViewModel(_statsService);
        var window = new StatsWindow(viewModel) { Owner = Application.Current.MainWindow };
        window.ShowDialog();
    }

    [RelayCommand]
    private void OpenCompile()
    {
        var viewModel = new CompileViewModel(_compileService, _exportService);
        var window = new CompileWindow(viewModel) { Owner = Application.Current.MainWindow };
        window.ShowDialog();
    }

    [RelayCommand]
    private void OpenSnapshots(BinderNodeViewModel? node)
    {
        if (node is null || node.NodeType != NodeType.Scene)
        {
            return;
        }

        var viewModel = new SnapshotViewModel(_snapshotService, node.Id);
        var window = new SnapshotsWindow(viewModel) { Owner = Application.Current.MainWindow };
        window.ShowDialog();

        if (viewModel.RestoredContent is not { } restoredContent)
        {
            return;
        }

        var (words, chars) = WordCountService.Count(restoredContent);
        _nodeService.UpdateWordCounts(node.Id, words, chars);
        node.UpdateWordCount(words, chars);

        if (SelectedNode?.Id == node.Id)
        {
            _isLoadingContent = true;
            EditorContent = restoredContent;
            _isLoadingContent = false;
            SceneWordCount = words;
            SceneCharCount = chars;
            _previewTimer.Stop();
            PreviewHtml = MarkdownPreviewService.ToHtml(EditorContent);
        }

        RefreshProjectWordCount();
    }

    [RelayCommand]
    private void OpenEntities()
    {
        var viewModel = new EntityViewModel(_entityService);
        var window = new EntityWindow(viewModel) { Owner = Application.Current.MainWindow };
        window.ShowDialog();
        RefreshEntityLinkOptions();
    }

    [RelayCommand]
    private void ToggleEntityLink(EntityLinkOption? option)
    {
        if (option is null || SelectedNode is null || SelectedNode.IsTrashNode)
        {
            return;
        }

        option.IsLinked = !option.IsLinked;
        PersistEntityLinks();
    }

    private void PersistEntityLinks()
    {
        if (SelectedNode is null)
        {
            return;
        }

        try
        {
            var linkedIds = CharacterLinkOptions.Concat(LocationLinkOptions)
                .Where(o => o.IsLinked)
                .Select(o => o.EntityId);
            _entityService.SetLinksForNode(SelectedNode.Id, linkedIds);
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Failed to update entity links for node {Id}", SelectedNode.Id);
            ShowError(ex.Message);
        }
    }

    private void RefreshEntityLinkOptions()
    {
        CharacterLinkOptions.Clear();
        LocationLinkOptions.Clear();

        if (SelectedNode is null || SelectedNode.IsTrashNode || _projectService.Current is null)
        {
            return;
        }

        var linkedIds = _entityService.GetForNode(SelectedNode.Id).Select(e => e.Id).ToHashSet();

        foreach (var character in _entityService.GetAll(EntityType.Character))
        {
            CharacterLinkOptions.Add(new EntityLinkOption(character.Id, character.Name, linkedIds.Contains(character.Id)));
        }

        foreach (var location in _entityService.GetAll(EntityType.Location))
        {
            LocationLinkOptions.Add(new EntityLinkOption(location.Id, location.Name, linkedIds.Contains(location.Id)));
        }
    }

    [RelayCommand]
    private void ToggleIncludeInCompile(BinderNodeViewModel? node)
    {
        if (node is null || node.IsTrashNode)
        {
            return;
        }

        try
        {
            var newValue = !node.IncludeInCompile;
            _nodeService.SetIncludeInCompile(node.Id, newValue);
            node.IncludeInCompile = newValue;
            node.Node.IncludeInCompile = newValue;
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Failed to toggle include-in-compile for node {Id}", node.Id);
            ShowError(ex.Message);
        }
    }

    [RelayCommand]
    private void OpenSearch()
    {
        if (_searchWindow is null || !_searchWindow.IsVisible)
        {
            var viewModel = new SearchViewModel(_searchService, NavigateToNode);
            _searchWindow = new SearchWindow(viewModel) { Owner = Application.Current.MainWindow };
            _searchWindow.Show();
        }
        else
        {
            _searchWindow.Activate();
        }
    }

    [RelayCommand]
    private void ToggleTheme()
    {
        var newTheme = _appSettingsService.GetTheme() == AppTheme.Light ? AppTheme.Dark : AppTheme.Light;
        _appSettingsService.SetTheme(newTheme);
        ThemeManager.Apply(newTheme);
    }

    private void NavigateToNode(Guid nodeId)
    {
        var path = FindPath(RootNodes, nodeId);
        if (path is null || path.Count == 0)
        {
            return;
        }

        foreach (var ancestor in path.SkipLast(1))
        {
            ancestor.IsExpanded = true;
        }

        path[^1].IsSelected = true;
    }

    [RelayCommand]
    private async Task NewProject()
    {
        var dialog = new NewProjectWindow { Owner = Application.Current.MainWindow };
        if (dialog.ShowDialog() != true)
        {
            return;
        }

        try
        {
            _projectService.CreateProject(dialog.ProjectLocation, dialog.ProjectName);
            LoadTree(resetSessionBaseline: true);

            var scene = _nodeService.AddNode(NodeType.Scene, Strings.BinderNewSceneTitle, null);
            await _fileService.WriteSceneAsync(_projectService.Current!.RootPath, scene.Id, Strings.SeedSceneContent);

            var vm = new BinderNodeViewModel(scene, OnRenameCommitted, OnInspectorCommitted);
            RootNodes.Add(vm);
            vm.IsSelected = true;
            await OnBinderSelectionChangedAsync(vm);
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
            LoadTree(resetSessionBaseline: true);
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
            var vm = new BinderNodeViewModel(created, OnRenameCommitted, OnInspectorCommitted);
            var collection = parent?.Children ?? RootNodes;
            collection.Add(vm);
            vm.BeginRename();
            _ = IndexNodeSafeAsync(created.Id);
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
            _ = IndexNodeSafeAsync(node.Id);
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Failed to rename node {Id}", node.Id);
            ShowError(ex.Message);
        }
    }

    private void OnInspectorCommitted(BinderNodeViewModel node)
    {
        try
        {
            _nodeService.UpdateInspector(node.Id, node.Synopsis, node.Notes, node.Label, node.Status, node.TargetWordCount);
            _ = IndexNodeSafeAsync(node.Id);
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Failed to update inspector fields for node {Id}", node.Id);
            ShowError(ex.Message);
        }
    }

    private void LoadTree(bool resetSessionBaseline = false)
    {
        var expandedIds = CollectExpandedIds(RootNodes);
        var selectedId = SelectedNode?.Id;

        RootNodes.Clear();
        var all = _nodeService.GetAll();
        var childrenByParent = all.ToLookup(n => n.ParentId);

        BinderNodeViewModel Build(ManuscriptNode node)
        {
            var vm = new BinderNodeViewModel(node, OnRenameCommitted, OnInspectorCommitted)
            {
                IsExpanded = expandedIds.Contains(node.Id)
            };
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
        RefreshProjectWordCount(resetSessionBaseline);
        _statsService.RecordSnapshot();
        _ = EnsureSearchIndexPopulatedAsync();

        if (selectedId is Guid id && FindNode(RootNodes, id) is { } restored)
        {
            restored.IsSelected = true;
            _ = OnBinderSelectionChangedAsync(restored);
        }
        else
        {
            _ = OnBinderSelectionChangedAsync(null);
        }
    }

    private async Task EnsureSearchIndexPopulatedAsync()
    {
        try
        {
            await _searchService.EnsureIndexPopulatedAsync();
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Failed to populate search index");
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

    private static List<BinderNodeViewModel>? FindPath(IEnumerable<BinderNodeViewModel> nodes, Guid id)
    {
        foreach (var node in nodes)
        {
            if (node.Id == id)
            {
                return [node];
            }

            var childPath = FindPath(node.Children, id);
            if (childPath is not null)
            {
                childPath.Insert(0, node);
                return childPath;
            }
        }

        return null;
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
