using Elsa.Api.Client.Activities;
using Elsa.Api.Client.Contracts;
using Elsa.Api.Client.Expressions;
using Elsa.Api.Client.Models;
using Elsa.Api.Client.Resources.ActivityDescriptors.Models;
using Elsa.Studio.Contracts;

namespace Elsa.Studio.Models;

public class DisplayInputEditorContext
{
    public Activity Activity { get; set; } = default!;
    public ActivityDescriptor ActivityDescriptor { get; set; } = default!;
    public InputDescriptor InputDescriptor { get; set; } = default!;
    public object? Value { get; set; }
    public IUIHintHandler UIHintHandler { get; set; } = default!;
    public ISyntaxProvider? SelectedSyntaxProvider { get; set; }
    public Func<WrappedInput, Task> OnValueChanged { get; set; } = default!;

    public string GetLiteralValueOrDefault() => GetExpressionValueOrDefault<LiteralExpression, object?>(x => x.Value);
    public string GetObjectValueOrDefault() => GetExpressionValueOrDefault<ObjectExpression, object?>(x => x.Value);

    public string GetExpressionValueOrDefault<TExpression, TValue>(Func<TExpression, TValue> valueAccessor) where TExpression : class, IExpression
    {
        if (!InputDescriptor.IsWrapped)
            return Value?.ToString() ?? InputDescriptor.DefaultValue?.ToString() ?? string.Empty;
        
        var wrappedInput = Value as WrappedInput;
        
        if (wrappedInput?.Expression is not TExpression expression)
            return InputDescriptor.DefaultValue?.ToString() ?? string.Empty;

        var value = valueAccessor(expression);

        return value?.ToString() ?? InputDescriptor.DefaultValue?.ToString() ?? string.Empty;
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
}