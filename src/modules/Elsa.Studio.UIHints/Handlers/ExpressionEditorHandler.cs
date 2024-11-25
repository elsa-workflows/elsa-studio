using Elsa.Studio.Contracts;
using Elsa.Studio.Models;
using Elsa.Studio.UIHints.Components;
using Microsoft.AspNetCore.Components;

namespace Elsa.Studio.UIHints.Handlers;

public class ExpressionEditorHandler : IUIHintHandler
{
    /// <inheritdoc />
    public bool GetSupportsUIHint(string uiHint) => uiHint == "expression-editor";

    /// <inheritdoc />
    public string UISyntax => "JavaScript";

    /// <inheritdoc />
    public RenderFragment DisplayInputEditor(DisplayInputEditorContext context)
    {
        return builder =>
        {
            builder.OpenComponent(0, typeof(ExpressionEditor));
            builder.AddAttribute(1, nameof(ExpressionEditor.EditorContext), context);
            builder.CloseComponent();
        };
    }
}