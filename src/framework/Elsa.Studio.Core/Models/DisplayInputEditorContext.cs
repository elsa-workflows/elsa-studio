using System.Text.Json;
using System.Text.Json.Nodes;
using Elsa.Api.Client.Contracts;
using Elsa.Api.Client.Converters;
using Elsa.Api.Client.Expressions;
using Elsa.Api.Client.Extensions;
using Elsa.Api.Client.Resources.ActivityDescriptors.Models;
using Elsa.Api.Client.Resources.WorkflowDefinitions.Models;
using Elsa.Api.Client.Shared.Models;
using Elsa.Studio.Contracts;

namespace Elsa.Studio.Models;

/// <summary>
/// Provides contextual information to the input editor.
/// </summary>
public class DisplayInputEditorContext
{
    /// <summary>
    /// The workflow definition.
    /// </summary>
    public WorkflowDefinition WorkflowDefinition { get; set; } = default!;
    
    /// <summary>
    /// The activity.
    /// </summary>
    public JsonObject Activity { get; set; } = default!;
    
    /// <summary>
    /// The activity descriptor.
    /// </summary>
    public ActivityDescriptor ActivityDescriptor { get; set; } = default!;
    
    /// <summary>
    /// The input descriptor.
    /// </summary>
    public InputDescriptor InputDescriptor { get; set; } = default!;
    
    /// <summary>
    /// The input value.
    /// </summary>
    public object? Value { get; set; }
    
    /// <summary>
    /// The UI hint handler.
    /// </summary>
    public IUIHintHandler UIHintHandler { get; set; } = default!;
    
    /// <summary>
    /// The syntax provider.
    /// </summary>
    public ISyntaxProvider? SelectedSyntaxProvider { get; set; }
    
    /// <summary>
    /// A delegate that is invoked when the input value changes.
    /// </summary>
    public Func<object?, Task> OnValueChanged { get; set; } = default!;
    
    /// <summary>
    /// A value indicating whether the input is read-only.
    /// </summary>
    public bool IsReadOnly { get; set; }

    /// <summary>
    /// Returns the input value or the default value if the input value is null.
    /// </summary>
    public T? GetValueOrDefault<T>()
    {
        return (Value ?? InputDescriptor.DefaultValue).ConvertTo<T>();
    }
    
    /// <summary>
    /// Returns the wrapped input literal value.
    /// </summary>
    /// <returns></returns>
    public string GetLiteralValueOrDefault()
    {
        if (!InputDescriptor.IsWrapped)
            return Value?.ToString() ?? InputDescriptor.DefaultValue?.ToString() ?? string.Empty;
        
        var wrappedInput = Value as WrappedInput;
        
        if (wrappedInput?.Expression is not LiteralExpression expression)
            return InputDescriptor.DefaultValue?.ToString() ?? string.Empty;

        var value = expression.Value;

        return value?.ToString() ?? InputDescriptor.DefaultValue?.ToString() ?? string.Empty;
    }

    public string GetObjectValueOrDefault()
    {
        if (!InputDescriptor.IsWrapped)
            return Serialize(Value ?? InputDescriptor.DefaultValue);

        var wrappedInput = Value as WrappedInput;
        
        if (wrappedInput?.Expression is not ObjectExpression expression)
            return Serialize(InputDescriptor.DefaultValue);

        var value = expression.Value;

        return value ?? InputDescriptor.DefaultValue?.ToString() ?? string.Empty;
    }
    
    public string GetExpressionValueOrDefault()
    {
        if (!InputDescriptor.IsWrapped)
            return Value?.ToString() ?? InputDescriptor.DefaultValue?.ToString() ?? string.Empty;
        
        var wrappedInput = Value as WrappedInput;
        var expression = wrappedInput?.Expression;
        var value = expression?.ToString();
        return value ?? InputDescriptor.DefaultValue?.ToString() ?? string.Empty;
    }

    public async Task UpdateValueAsync(object? value)
    {
        await OnValueChanged(value);
    }

    public async Task UpdateExpressionAsync(IExpression expression)
    {
        var wrappedInput = Value as WrappedInput;
        
        // Update input.
        wrappedInput ??= new WrappedInput();
        wrappedInput.Expression = expression;

        Value = wrappedInput;

        // Notify that the input has changed.
        await OnValueChanged(wrappedInput);
    }
    
    private static string Serialize(object? value)
    {
        var options = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };
        
        options.Converters.Add(new ExpressionJsonConverterFactory());

        return value != null ? JsonSerializer.Serialize(value, options) : string.Empty;
    }
}