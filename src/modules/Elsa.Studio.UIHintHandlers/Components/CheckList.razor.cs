using System.Text.Json;
using Elsa.Api.Client.Expressions;
using Elsa.Api.Client.Models;
using Elsa.Studio.Models;
using Elsa.Studio.UIHintHandlers.Models;
using Microsoft.AspNetCore.Components;

namespace Elsa.Studio.UIHintHandlers.Components;

public partial class CheckList
{
    private ICollection<CheckListItem> _checkListItems = Array.Empty<CheckListItem>();

    [Parameter] public DisplayInputEditorContext EditorContext { get; set; } = default!;

    protected override async Task OnInitializedAsync()
    {
        var selectList = await GetSelectListAsync();
        var selectedValues = GetSelectedValues(selectList.IsFlagsEnum);

        _checkListItems = selectList.Items
            .Select(i => new CheckListItem(i.Value, i.Text, selectedValues.Contains(i.Value)))
            .ToList();
    }

    private async Task<SelectList> GetSelectListAsync()
    {
        var descriptor = EditorContext.InputDescriptor;
        var options = (JsonElement?)descriptor.Options;

        if (options == null || options.Value.ValueKind == JsonValueKind.Null)
            return new SelectList(new List<SelectListItem>(), false);

        // if (options is IRuntimeListProviderSettings runtimeListProviderSettings)
        // {
        //     return new SelectList(new List<SelectListItem>(), false);
        // }

        if (options.Value.ValueKind == JsonValueKind.Array)
        {
            var items = options.Value.Deserialize<string[]>()!;
            var selectListItems = items.Select(s => new SelectListItem(s, s)).ToList();
            return new SelectList(selectListItems, false);
        }

        // if (options is SelectList selectList)
        // {
        //     return new SelectList(new List<SelectListItem>(), false);
        // }

        return new SelectList(new List<SelectListItem>(), false);
    }

    private ICollection<string> GetSelectedValues(bool isFlagsEnum)
    {
        var inputDescriptor = EditorContext.InputDescriptor;
        var input = (EditorContext.Value?.Expression as ObjectExpression)?.Value ?? string.Empty;
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
        if (string.IsNullOrWhiteSpace(json))
            return new List<string>();

        try
        {
            return JsonSerializer.Deserialize<ICollection<string>>(json) ?? new List<string>();
        }
        catch (JsonException)
        {
            return new List<string>();
        }
    }

    private async Task OnCheckedChanged(CheckListItem item, bool? state)
    {
        // Toggle state.
        item.IsChecked = state == true;
        
        // Get selected values.
        var selectedValues = _checkListItems.Where(x => x.IsChecked).Select(x => x.Value).ToList();
        
        // Update input.
        var input = EditorContext.Value ?? new ActivityInput();
        var json = JsonSerializer.Serialize(selectedValues);
        input.Expression = new ObjectExpression(json);
        
        // Notify that the input has changed.
        await EditorContext.OnValueChanged(input);
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