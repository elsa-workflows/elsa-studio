namespace Elsa.Studio.WorkflowContexts.Models;

/// <summary>
/// Represents a checkbox item.
/// </summary>
public class CheckBoxItem
{
    /// <summary>
    /// Gets or sets the label of the checkbox.
    /// </summary>
    public string Label { get; set; } = default!;
    
    /// <summary>
    /// Gets or sets whether the checkbox is checked.
    /// </summary>
    public bool IsChecked { get; set; }
    
    /// <summary>
    /// Gets or sets the value of the checkbox.
    /// </summary>
    public object? Value { get; set; }
}