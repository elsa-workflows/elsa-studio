namespace Elsa.Studio.Models;

/// <summary>
/// Represents the data panel item record.
/// </summary>
public record DataPanelItem(string Label, string? Text = null, string? Link = null, Func<Task>? OnClick = null);