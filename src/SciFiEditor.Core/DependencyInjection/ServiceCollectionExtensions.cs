using Microsoft.Extensions.DependencyInjection;
using SciFiEditor.Core.Compile;
using SciFiEditor.Core.Entities;
using SciFiEditor.Core.Manuscript;
using SciFiEditor.Core.Projects;
using SciFiEditor.Core.Snapshots;
using SciFiEditor.Core.Stats;
using SciFiEditor.Data;

namespace SciFiEditor.Core.DependencyInjection;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddSciFiEditorCore(this IServiceCollection services)
    {
        services.AddSingleton<RecentProjectsService>();
        services.AddSingleton<ProjectBackupService>();
        services.AddSingleton<ProjectService>();
        services.AddSingleton<ManuscriptFileService>();
        services.AddSingleton<NodeService>();
        services.AddSingleton<WritingStatsService>();
        services.AddSingleton<CompileService>();
        services.AddSingleton<ExportService>();
        services.AddSingleton<SnapshotService>();
        services.AddSingleton<EntityService>();
        services.AddSingleton(sp =>
        {
            var projectService = sp.GetRequiredService<ProjectService>();
            var fileService = sp.GetRequiredService<ManuscriptFileService>();
            return new SceneAutosaveCoordinator(
                (nodeId, content) => fileService.WriteSceneAsync(projectService.Current!.RootPath, nodeId, content),
                TimeSpan.FromSeconds(2));
        });
        services.AddSingleton(sp =>
        {
            var nodeService = sp.GetRequiredService<NodeService>();
            return new WordCountCoordinator(
                (nodeId, words, chars) =>
                {
                    nodeService.UpdateWordCounts(nodeId, words, chars);
                    return Task.CompletedTask;
                },
                TimeSpan.FromMilliseconds(400));
        });

        return services;
    }
}
