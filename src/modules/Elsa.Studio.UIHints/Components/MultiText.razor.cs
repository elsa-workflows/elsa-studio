using System.Reflection;
using System.Text.Json;
using Elsa.Api.Client.Expressions;
using Elsa.Studio.Converters;
using Elsa.Studio.Models;
using Elsa.Studio.UIHints.Helpers;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using MudExtensions;

namespace Elsa.Studio.UIHints.Components;

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
        var input = EditorContext.GetObjectValueOrDefault();
        return ParseJson(input);
    }
    
    private List<string> ParseJson(string? json)
    {
        var options = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };
        
        options.Converters.Add(new JsonPrimitiveToStringConverter());
        return JsonParser.ParseJson(json, () => new List<string>(), options);
    }

    private async Task OnValuesChanges(List<string> arg)
    { 
        var json = JsonSerializer.Serialize(_items);
        var expression = new ObjectExpression(json);
        await EditorContext.UpdateExpressionAsync(expression);
    }

    private void OnKeyDown(KeyboardEventArgs arg)
    {
        // TODO: This is a hack to get the chips to update when the user presses enter.
        // Ideally, we can configure this on MudChipField, but this is not currently supported. 
        if (arg.Key is not ("Enter" or "Tab")) return;
        
        var setChipsMethod = _chipField.GetType().GetMethod("SetChips", BindingFlags.Instance | BindingFlags.NonPublic);
        setChipsMethod!.Invoke(_chipField, null);
    }
}