using System.Reflection;
using System.Text.Json;
using Elsa.Api.Client.Expressions;
using Elsa.Studio.Models;
using Elsa.Studio.UIHintHandlers.Helpers;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using MudExtensions;

namespace Elsa.Studio.UIHintHandlers.Components;

public partial class MultiText
{
    private List<string> _items = new();
    private MudChipField<string> _chipField = default!;

    [Parameter] public DisplayInputEditorContext EditorContext { get; set; } = default!;

    protected override void OnInitialized()
    {
        _items = GetCurrentItems();
    }

    private List<string> GetCurrentItems()
    {
        var inputDescriptor = EditorContext.InputDescriptor;
        var input = (EditorContext.Value?.Expression as ObjectExpression)?.Value ?? string.Empty;
        var defaultValue = inputDescriptor.DefaultValue;
        var json = !string.IsNullOrWhiteSpace(input) ? input : defaultValue as string;
            
        var items = ParseJson(json);

        if (!items.Any())
            items = ParseJson(defaultValue as string);

        return items;
    }
    
    private List<string> ParseJson(string? json)
    {
        return JsonParser.ParseJson(json, () => new List<string>());
    }

    private async Task OnValuesChanges(List<string> arg)
    { var json = JsonSerializer.Serialize(_items);
        var expression = new ObjectExpression(json);
        await EditorContext.UpdateExpressionAsync(expression);
    }

    private async Task OnKeyDown(KeyboardEventArgs arg)
    {
        // TODO: This is a hack to get the chips to update when the user presses enter.
        // Ideally, we can configure this on MudChipField, but this is not currently supported. 
        if (arg.Key is "Enter" or "Tab")
        {
            var setChipsMethod = _chipField.GetType().GetMethod("SetChips", BindingFlags.Instance | BindingFlags.NonPublic);
            setChipsMethod!.Invoke(_chipField, null);
        }
    }
}