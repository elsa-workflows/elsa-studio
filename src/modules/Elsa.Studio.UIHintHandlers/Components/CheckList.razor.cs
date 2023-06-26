using System.Text.Json;
using Elsa.Api.Client.Expressions;
using Elsa.Studio.Models;
using Elsa.Studio.UIHintHandlers.Extensions;
using Elsa.Studio.UIHintHandlers.Helpers;
using Microsoft.AspNetCore.Components;

namespace Elsa.Studio.UIHintHandlers.Components;

public partial class CheckList
{
    private ICollection<CheckListItem> _checkListItems = Array.Empty<CheckListItem>();

    [Parameter] public DisplayInputEditorContext EditorContext { get; set; } = default!;

    protected override async Task OnInitializedAsync()
    {
        var selectList = await EditorContext.InputDescriptor.GetSelectListAsync();
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
        var expression = new ObjectExpression(json);
        await EditorContext.UpdateExpressionAsync(expression);
    }
}

public class CheckListItem
{
    public CheckListItem(string value, string text, bool isChecked)
    {
        Value = value;
        Text = text;
        IsChecked = isChecked;
    }

    public string Value { get; set; }
    public string Text { get; set; }
    public bool IsChecked { get; set; }
}