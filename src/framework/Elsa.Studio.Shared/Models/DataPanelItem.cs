namespace Elsa.Studio.Models;

/// <summary>
/// Represents an item in a data panel with label, text, link, and optional click action.
/// </summary>
/// <param name="Label">The display label for the item.</param>
/// <param name="Text">The optional text content for the item.</param>
/// <param name="Link">The optional link URL for the item.</param>
/// <param name="OnClick">The optional click handler for the item.</param>
public record DataPanelItem(string Label, string? Text = null, string? Link = null, Func<Task>? OnClick = null);