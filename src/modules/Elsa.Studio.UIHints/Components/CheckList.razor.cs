using System.Text.Json;
using Elsa.Api.Client.Resources.Scripting.Models;
using Elsa.Studio.Models;
using Elsa.Studio.UIHints.Extensions;
using Elsa.Studio.UIHints.Helpers;
using Elsa.Studio.UIHints.Models;
using Microsoft.AspNetCore.Components;

namespace Elsa.Studio.UIHints.Components;

/// <summary>
/// A component that renders a check list.
/// </summary>
public partial class CheckList
{
    private ICollection<CheckListItem> _checkListItems = Array.Empty<CheckListItem>();

    /// <summary>
    /// The editor context.
    /// </summary>
    [Parameter] public DisplayInputEditorContext EditorContext { get; set; } = default!;

    /// <inheritdoc />
    protected override void OnInitialized()
    {
        var selectList = EditorContext.InputDescriptor.GetSelectList();
        var selectedValues = GetSelectedValues(selectList.IsFlagsEnum);

        _checkListItems = selectList.Items
            .Select(i => new CheckListItem(i.Value, i.Text, selectedValues.Contains(i.Value)))
            .ToList();
    }

    private ICollection<string> GetSelectedValues(bool isFlagsEnum)
    {
        var inputDescriptor = EditorContext.InputDescriptor;
        var input = EditorContext.GetObjectValueOrDefault();
        var defaultValue = inputDescriptor.DefaultValue;
        var json = !string.IsNullOrWhiteSpace(input) ? input : defaultValue as string;

        if (isFlagsEnum)
        {
            return !string.IsNullOrWhiteSpace(json)
                ? int.TryParse(json, out var n) ? new[] { n.ToString() } : Array.Empty<string>()
                : new[] { defaultValue?.ToString() ?? string.Empty };
        }

        var selectListItems = ParseJson(json);

        if (!selectListItems.Any())
            selectListItems = ParseJson(defaultValue as string);

        return selectListItems;
    }

    private ICollection<string> ParseJson(string? json)
    {
        return JsonParser.ParseJson<ICollection<string>>(json, () => new List<string>());
    }

    private async Task OnCheckedChanged(CheckListItem item, bool? state)
    {
        // Toggle state.
        item.IsChecked = state == true;
        
        // Get selected values.
        var selectedValues = _checkListItems.Where(x => x.IsChecked).Select(x => x.Value).ToList();
        
        // Serialize to JSON.
        var json = JsonSerializer.Serialize(selectedValues);
        
        // Update expression.
        var expression = Expression.CreateObject(json);
        await EditorContext.UpdateExpressionAsync(expression);
    }
}