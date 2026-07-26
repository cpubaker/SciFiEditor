using System.Globalization;
using System.Reflection;
using System.Resources;

namespace Scriptorium.App.Resources;

public static class Strings
{
    private static readonly ResourceManager ResourceManager =
        new("Scriptorium.App.Resources.Strings", Assembly.GetExecutingAssembly());

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

    private static string Get(string name) =>
        ResourceManager.GetString(name, CultureInfo.CurrentUICulture) ?? name;
}
