namespace Elsa.Studio.Workflows.UI.Models;

/// <summary>
/// A static class with default activity colors.
/// </summary>
public static class DefaultActivityColors
{
    public static string Default { get; set; } = "var(--mud-palette-primary)";
    public static string NotFound { get; set; } = "var(--mud-palette-error)";
    public static string Composition { get; set; } = "#f97316";
    public static string Console { get; set; } = "#0369a1";
    public static string Http { get; set; } = "#2dd4bf";
    public static string Scripting { get; set; } = "#22c55e";
    public static string Flowchart { get; set; } = "#06b6d4";
    public static string Looping { get; set; } = "#6366f1";
    public static string Primitives { get; set; } = Default;
    public static string Email { get; set; } = "#fcd34d";
    public static string Branching { get; set; } = "#06b6d4";
    public static string Timer { get; set; } = "#d946ef";
}