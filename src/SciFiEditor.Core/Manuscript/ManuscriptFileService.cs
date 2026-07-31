namespace SciFiEditor.Core.Manuscript;

public sealed class ManuscriptFileService
{
    private const string ManuscriptFolderName = "manuscript";

    public string GetScenePath(string projectRootPath, Guid nodeId) =>
        Path.Combine(projectRootPath, ManuscriptFolderName, $"{nodeId}.md");

    public void CreateSceneFile(string projectRootPath, Guid nodeId)
    {
        var path = GetScenePath(projectRootPath, nodeId);
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        if (!File.Exists(path))
        {
            File.WriteAllText(path, string.Empty);
        }
    }

    public void CopySceneFile(string projectRootPath, Guid sourceNodeId, Guid targetNodeId)
    {
        var sourcePath = GetScenePath(projectRootPath, sourceNodeId);
        var targetPath = GetScenePath(projectRootPath, targetNodeId);
        Directory.CreateDirectory(Path.GetDirectoryName(targetPath)!);

        if (File.Exists(sourcePath))
        {
            File.Copy(sourcePath, targetPath, overwrite: true);
        }
        else
        {
            File.WriteAllText(targetPath, string.Empty);
        }
    }

    public async Task<string> ReadSceneAsync(string projectRootPath, Guid nodeId)
    {
        var path = GetScenePath(projectRootPath, nodeId);
        return File.Exists(path) ? await File.ReadAllTextAsync(path) : string.Empty;
    }

    public async Task WriteSceneAsync(string projectRootPath, Guid nodeId, string content)
    {
        var path = GetScenePath(projectRootPath, nodeId);
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        await File.WriteAllTextAsync(path, content);
    }
}
