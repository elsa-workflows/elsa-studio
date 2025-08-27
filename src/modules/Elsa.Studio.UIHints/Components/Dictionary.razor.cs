using System.Text.Json;
using System.Text.Json.Nodes;
using Elsa.Api.Client.Resources.Scripting.Models;
using Elsa.Studio.Models;
using Elsa.Studio.UIHints.Helpers;
using Elsa.Studio.Workflows.Domain.Models;
using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace Elsa.Studio.UIHints.Components;

/// <summary>
/// Renders an editor for a dictionary of key-value pairs.
/// </summary>
public partial class Dictionary
{
    private readonly string[] _uiSyntaxes = { "Literal", "Object" };

    private DictionaryEntryRecord? _entryBeingEdited;
    private DictionaryEntryRecord? _entryBeingAdded;
    private MudTable<DictionaryEntryRecord> _table = null!;

    /// <summary>
    /// The context for the editor.
    /// </summary>
    [Parameter]
    public DisplayInputEditorContext EditorContext { get; set; } = null!;

    [CascadingParameter] private ExpressionDescriptorProvider ExpressionDescriptorProvider { get; set; } = null!;

    private ICollection<DictionaryEntryRecord> Items { get; set; } = new List<DictionaryEntryRecord>();
    private bool DisableAddButton => _entryBeingEdited != null || _entryBeingAdded != null;

    /// <inheritdoc />
    protected override void OnParametersSet()
    {
        Items = GetItems();
    }

    private ICollection<DictionaryEntryRecord> GetItems()
    {
        var input = EditorContext.GetObjectValueOrDefault();
        var dictionary = ParseJson(input);
        var entryRecords = dictionary.Select(kvp => Map(kvp.Key, kvp.Value)).ToList();
        return entryRecords;
    }

    private IDictionary<string, object?> ParseJson(string? json)
    {
        var options = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        return JsonParser.ParseJson(json, () => new Dictionary<string, object?>(), options);
    }

    private IEnumerable<ExpressionDescriptor> GetSupportedExpressions()
    {
        return ExpressionDescriptorProvider.ListDescriptors().Where(x => !_uiSyntaxes.Contains(x.Type) && x.IsBrowsable).ToList();
    }

    private string GetDefaultExpressionType()
    {
        var defaultExpressionType = GetSupportedExpressions().FirstOrDefault()?.Type ?? "Literal";
        return defaultExpressionType;
    }

    private DictionaryEntryRecord Map(string key, object? value)
    {
        var defaultExpressionType = GetDefaultExpressionType();

        // Handle Expression objects directly
        if (value is Expression expression)
        {
            return new DictionaryEntryRecord
            {
                Key = key,
                Value = expression.Value?.ToString() ?? "",
                ExpressionType = string.IsNullOrWhiteSpace(expression.Type) ? defaultExpressionType : expression.Type
            };
        }

        // Try to extract expression information from the value if it's a JsonObject (legacy handling)
        if (value is JsonElement { ValueKind: JsonValueKind.Object } jsonElement) 
        {
            if (jsonElement.TryGetProperty("type", out var typeNode) && jsonElement.TryGetProperty("value", out var valueNode))
            {
                return new DictionaryEntryRecord
                {
                    Key = key,
                    Value = valueNode.GetString() ?? "",
                    ExpressionType = typeNode.GetString() ?? defaultExpressionType
                };
            }
        }

        return new DictionaryEntryRecord
        {
            Key = key,
            Value = value?.ToString() ?? "",
            ExpressionType = "Literal"
        };
    }

    private KeyValuePair<string, object> Map(DictionaryEntryRecord entry)
    {
        // For literal values, just store the raw string value
        if (entry.ExpressionType == "Literal")
        {
            return new KeyValuePair<string, object>(entry.Key, entry.Value);
        }
        
        // For other expression types, wrap in Expression object
        var expression = new Expression(entry.ExpressionType, entry.Value);
        return new KeyValuePair<string, object>(entry.Key, expression);
    }

    private Task SaveChangesAsync()
    {
        var dictionary = Items.ToDictionary(x => x.Key, x => Map(x).Value);
        return EditorContext.UpdateValueOrObjectExpressionAsync(dictionary);
    }

    private async void OnRowEditCommitted(object data)
    {
        _entryBeingAdded = null;
        _entryBeingEdited = null;
        await SaveChangesAsync();
        StateHasChanged();
    }

    private void OnRowEditPreview(object obj)
    {
        var entry = (DictionaryEntryRecord)obj;
        _entryBeingEdited = new()
        {
            Key = entry.Key,
            Value = entry.Value,
            ExpressionType = entry.ExpressionType
        };

        StateHasChanged();
    }

    private async void OnRowEditCancel(object obj)
    {
        if (_entryBeingAdded != null)
        {
            Items.Remove(_entryBeingAdded);
            await SaveChangesAsync();
            _entryBeingAdded = null;
            StateHasChanged();
            return;
        }

        var entry = (DictionaryEntryRecord)obj;
        entry.Key = _entryBeingEdited?.Key ?? "";
        entry.Value = _entryBeingEdited?.Value ?? "";
        entry.ExpressionType = _entryBeingEdited?.ExpressionType ?? "";
        _entryBeingEdited = null;
        StateHasChanged();
    }

    private async Task OnDeleteClicked(DictionaryEntryRecord entry)
    {
        Items.Remove(entry);
        await SaveChangesAsync();
    }

    private void OnAddClicked()
    {
        var entry = new DictionaryEntryRecord
        {
            Key = $"Key{Items.Count + 1}",
            Value = "",
            ExpressionType = GetDefaultExpressionType()
        };

        Items.Add(entry);
        _entryBeingAdded = entry;

        // Need to do it this way, otherwise MudTable doesn't show the item in edit mode.
        _ = Task.Delay(1).ContinueWith(_ =>
        {
            InvokeAsync(() =>
            {
                _table.SetEditingItem(entry);
                StateHasChanged();
            });
        });
    }

    private string GetExpressionTypeDisplayName(string expressionType)
    {
        var expressionDescriptor = ExpressionDescriptorProvider.GetByType(expressionType) ?? throw new($"Could not find expression descriptor for expression type '{expressionType}'.");
        return expressionDescriptor.DisplayName;
    }
}

/// <summary>
/// Represents a single key-value entry in a dictionary.
/// </summary>
public class DictionaryEntryRecord
{
    /// <summary>
    /// The key of the entry.
    /// </summary>
    public string Key { get; set; } = string.Empty;

    /// <summary>
    /// The value of the entry.
    /// </summary>
    public string Value { get; set; } = string.Empty;

    /// <summary>
    /// The expression type of the value.
    /// </summary>
    public string ExpressionType { get; set; } = "Literal";
}