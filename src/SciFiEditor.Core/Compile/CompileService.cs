using System.Text;
using SciFiEditor.Core.Manuscript;
using SciFiEditor.Core.Projects;
using SciFiEditor.Domain;

namespace SciFiEditor.Core.Compile;

public sealed class CompileService
{
    private readonly ProjectService _projectService;
    private readonly NodeService _nodeService;
    private readonly ManuscriptFileService _fileService;

    public CompileService(ProjectService projectService, NodeService nodeService, ManuscriptFileService fileService)
    {
        _projectService = projectService;
        _nodeService = nodeService;
        _fileService = fileService;
    }

    public async Task<string> BuildManuscriptMarkdownAsync()
    {
        var project = _projectService.Current ?? throw new InvalidOperationException("No project is open.");
        var all = _nodeService.GetAll();
        var childrenByParent = all.ToLookup(n => n.ParentId);
        var builder = new StringBuilder();

        async Task WalkAsync(ManuscriptNode node, int depth)
        {
            if (node.Id == WellKnownNodeIds.Trash || !node.IncludeInCompile)
            {
                return;
            }

            switch (node.NodeType)
            {
                case NodeType.Folder:
                case NodeType.Chapter:
                    var level = Math.Min(depth + 1, 6);
                    builder.Append(new string('#', level)).Append(' ').AppendLine(node.Title);
                    builder.AppendLine();
                    break;
                case NodeType.Scene:
                    var content = await _fileService.ReadSceneAsync(project.RootPath, node.Id);
                    builder.AppendLine(content.TrimEnd());
                    builder.AppendLine();
                    break;
            }

            foreach (var child in childrenByParent[node.Id].OrderBy(n => n.SortOrder))
            {
                await WalkAsync(child, depth + 1);
            }
        }

        foreach (var root in childrenByParent[null].OrderBy(n => n.SortOrder))
        {
            await WalkAsync(root, 0);
        }

        return builder.ToString();
    }
}
