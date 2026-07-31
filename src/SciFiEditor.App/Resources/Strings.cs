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

    private static string Get(string name) =>
        ResourceManager.GetString(name, CultureInfo.CurrentUICulture) ?? name;
}
