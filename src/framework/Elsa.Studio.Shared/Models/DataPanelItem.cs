namespace Elsa.Studio.Models;

public record DataPanelItem(string Label, string? Text = null, string? Link = null, Func<Task>? OnClick = null);