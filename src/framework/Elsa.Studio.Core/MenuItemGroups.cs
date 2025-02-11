using Elsa.Studio.Models;

namespace Elsa.Studio;

public static class MenuItemGroups
{
    public static readonly MenuItemGroup General = new("general", "General", 0f);
    public static readonly MenuItemGroup Settings = new("security", "Settings", 1000f);
}