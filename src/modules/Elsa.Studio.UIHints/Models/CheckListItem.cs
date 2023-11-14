namespace Elsa.Studio.UIHints.Models;

/// <summary>
/// Represents a check list item.
/// </summary>
public class CheckListItem
{
    /// <summary>
    /// Initializes a new instance of the <see cref="CheckListItem"/> class.
    /// </summary>
    public CheckListItem(string value, string text, bool isChecked)
    {
        Value = value;
        Text = text;
        IsChecked = isChecked;
    }

    /// <summary>
    /// Gets or sets the value.
    /// </summary>
    public string Value { get; set; }
    
    /// <summary>
    /// Gets or sets the text.
    /// </summary>
    public string Text { get; set; }
    
    /// <summary>
    /// Gets or sets a value indicating whether the item is checked.
    /// </summary>
    public bool IsChecked { get; set; }
}