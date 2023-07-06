using System.Text.Json;
using Elsa.Api.Client.Activities;
using Elsa.Api.Client.Contracts;
using Elsa.Api.Client.Converters;
using Elsa.Api.Client.Expressions;
using Elsa.Api.Client.Extensions;
using Elsa.Api.Client.Models;
using Elsa.Api.Client.Resources.ActivityDescriptors.Models;
using Elsa.Api.Client.Resources.WorkflowDefinitions.Models;
using Elsa.Studio.Contracts;

namespace Elsa.Studio.Models;

public class DisplayInputEditorContext
{
    public WorkflowDefinition WorkflowDefinition { get; set; } = default!;
    public Activity Activity { get; set; } = default!;
    public ActivityDescriptor ActivityDescriptor { get; set; } = default!;
    public InputDescriptor InputDescriptor { get; set; } = default!;
    public object? Value { get; set; }
    public IUIHintHandler UIHintHandler { get; set; } = default!;
    public ISyntaxProvider? SelectedSyntaxProvider { get; set; }
    public Func<object?, Task> OnValueChanged { get; set; } = default!;
    public bool IsReadOnly { get; set; }

    public T? GetValueOrDefault<T>()
    {
        return (Value ?? InputDescriptor.DefaultValue).ConvertTo<T>();
    }
    
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