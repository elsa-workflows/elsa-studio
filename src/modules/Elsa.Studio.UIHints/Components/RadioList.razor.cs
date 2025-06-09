using Elsa.Api.Client.Shared.UIHints.RadioList;
using Elsa.Studio.Models;
using Elsa.Studio.UIHints.Extensions;
using Microsoft.AspNetCore.Components;

namespace Elsa.Studio.UIHints.Components;

/// <summary>
/// A component that renders a radio list.
/// </summary>
public partial class RadioList
{
    private ICollection<RadioListItem> _radioListItems = Array.Empty<RadioListItem>();
    private string? radioItem = string.Empty;

    public string _radioItem
    {
        get { return radioItem; }
        set
        {
            radioItem = value;
            OnValueChanged(value);
        }
    }

    /// <summary>
    /// The editor context.
    /// </summary>
    [Parameter]
    public DisplayInputEditorContext EditorContext { get; set; } = default!;

    /// <inheritdoc />
    protected override void OnInitialized()
    {
        var radioList = EditorContext.InputDescriptor.GetRadioList();
        _radioListItems = radioList.Items.ToList();

        var value = EditorContext.GetLiteralValueOrDefault();
        radioItem = _radioListItems.FirstOrDefault(x => x.Value == value)?.Value ?? "";
    }

    private async Task OnValueChanged(string? value)
    {
        await EditorContext.UpdateValueOrLiteralExpressionAsync(value ?? "");
    }
}