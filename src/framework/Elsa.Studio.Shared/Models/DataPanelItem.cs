using Microsoft.AspNetCore.Components;

namespace Elsa.Studio.Models;

/// <summary>
/// Represents an item in a data panel with label, text, link, and optional click action.
/// </summary>
/// <param name="Label">The display label for the item.</param>
/// <param name="Text">The optional text content for the item.</param>
/// <param name="Link">The optional link URL for the item.</param>
/// <param name="OnClick">The optional click handler for the item.</param>
/// <param name="LabelToolTip">The optional tooltip for the label.</param>
/// <param name="LabelComponent">The optional custom component to render in the label cell.</param>
/// <param name="ValueComponent">The optional custom component to render in the value cell.</param>
public record DataPanelItem(
    string? Label = null,
    string? Text = null,
    string? Link = null,
    Func<Task>? OnClick = null,
    string? LabelToolTip = null,
    RenderFragment? LabelComponent = null,
    RenderFragment? ValueComponent = null);