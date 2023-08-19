using Elsa.Studio.Contracts;
using Elsa.Studio.Models;
using Elsa.Studio.UIHints.Components;
using Microsoft.AspNetCore.Components;

namespace Elsa.Studio.UIHints.Handlers;

/// <summary>
/// Provides a handler for the <c>variable-picker</c> UI hint.
/// </summary>
public class VariablePickerHandler : IUIHintHandler
{
    /// <inheritdoc />
    public bool GetSupportsUIHint(string uiHint) => uiHint == "variable-picker";

    /// <inheritdoc />
    public string UISyntax => WellKnownSyntaxNames.Literal;

    /// <inheritdoc />
    public RenderFragment DisplayInputEditor(DisplayInputEditorContext context)
    {
        return builder =>
        {
            builder.OpenComponent(0, typeof(VariablePicker));
            builder.AddAttribute(1, nameof(VariablePicker.EditorContext), context);
            builder.CloseComponent();
        };
    }
}