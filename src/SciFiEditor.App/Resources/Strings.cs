using System.Globalization;
using System.Reflection;
using System.Resources;

namespace SciFiEditor.App.Resources;

public static class Strings
{
    private static readonly ResourceManager ResourceManager =
        new("SciFiEditor.App.Resources.Strings", Assembly.GetExecutingAssembly());

    public static string MenuFile => Get("MenuFile");
    public static string MenuEdit => Get("MenuEdit");
    public static string MenuView => Get("MenuView");
    public static string MenuProject => Get("MenuProject");
    public static string MenuHelp => Get("MenuHelp");
    public static string TabText => Get("TabText");
    public static string TabBoard => Get("TabBoard");
    public static string TabPreview => Get("TabPreview");
    public static string InspectorSynopsis => Get("InspectorSynopsis");
    public static string InspectorNotes => Get("InspectorNotes");
    public static string StatusReady => Get("StatusReady");
    public static string MenuNewProject => Get("MenuNewProject");
    public static string MenuOpenProject => Get("MenuOpenProject");
    public static string MenuRecentProjects => Get("MenuRecentProjects");
    public static string MenuSave => Get("MenuSave");
    public static string BinderAddFolder => Get("BinderAddFolder");
    public static string BinderAddChapter => Get("BinderAddChapter");
    public static string BinderAddScene => Get("BinderAddScene");
    public static string BinderRename => Get("BinderRename");
    public static string BinderDuplicate => Get("BinderDuplicate");
    public static string BinderMoveToTrash => Get("BinderMoveToTrash");
    public static string BinderTrashTitle => Get("BinderTrashTitle");
    public static string DialogNewProjectTitle => Get("DialogNewProjectTitle");
    public static string DialogNewProjectName => Get("DialogNewProjectName");
    public static string DialogNewProjectLocation => Get("DialogNewProjectLocation");
    public static string DialogBrowse => Get("DialogBrowse");
    public static string DialogOk => Get("DialogOk");
    public static string DialogCancel => Get("DialogCancel");
    public static string ErrorTitle => Get("ErrorTitle");
    public static string BinderNewFolderTitle => Get("BinderNewFolderTitle");
    public static string BinderNewChapterTitle => Get("BinderNewChapterTitle");
    public static string BinderNewSceneTitle => Get("BinderNewSceneTitle");
    public static string InspectorLabel => Get("InspectorLabel");
    public static string InspectorStatus => Get("InspectorStatus");
    public static string InspectorTargetWordCount => Get("InspectorTargetWordCount");
    public static string StatusNone => Get("StatusNone");
    public static string StatusDraft => Get("StatusDraft");
    public static string StatusRevised => Get("StatusRevised");
    public static string StatusFinal => Get("StatusFinal");
    public static string StatusBarSceneWords => Get("StatusBarSceneWords");
    public static string StatusBarProjectWords => Get("StatusBarProjectWords");
    public static string StatusBarSessionDelta => Get("StatusBarSessionDelta");
    public static string MenuStatistics => Get("MenuStatistics");
    public static string StatsWindowTitle => Get("StatsWindowTitle");
    public static string StatsHeatmapLabel => Get("StatsHeatmapLabel");
    public static string StatsChartLabel => Get("StatsChartLabel");
    public static string StatsStreakLabel => Get("StatsStreakLabel");
    public static string StatsDailyGoalLabel => Get("StatsDailyGoalLabel");
    public static string StatsSessionGoalLabel => Get("StatsSessionGoalLabel");
    public static string StatsSaveGoals => Get("StatsSaveGoals");
    public static string MenuCompile => Get("MenuCompile");
    public static string BinderIncludeInCompile => Get("BinderIncludeInCompile");
    public static string CompileWindowTitle => Get("CompileWindowTitle");
    public static string CompileFormatLabel => Get("CompileFormatLabel");
    public static string CompileExportButton => Get("CompileExportButton");
    public static string CompileSuccess => Get("CompileSuccess");

    private static string Get(string name) =>
        ResourceManager.GetString(name, CultureInfo.CurrentUICulture) ?? name;
}
