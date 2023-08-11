using Elsa.Studio.Contracts;
using Elsa.Studio.Models;
using Elsa.Studio.UIHintHandlers.Components;
using Microsoft.AspNetCore.Components;

namespace Elsa.Studio.UIHintHandlers.Handlers;

/// <summary>
/// Provides a handler for the <c>workflow-definition-picker</c> UI hint.
/// </summary>
public class WorkflowDefinitionPickerHandler : IUIHintHandler
{
    /// <inheritdoc />
    public bool GetSupportsUIHint(string uiHint) => uiHint == "workflow-definition-picker";

    /// <inheritdoc />
    public string UISyntax => WellKnownSyntaxNames.Literal;

    /// <inheritdoc />
    public RenderFragment DisplayInputEditor(DisplayInputEditorContext context)
    {
        return builder =>
        {
            builder.OpenComponent(0, typeof(WorkflowDefinitionPicker));
            builder.AddAttribute(1, nameof(WorkflowDefinitionPicker.EditorContext), context);
            builder.CloseComponent();
        };
    }
}