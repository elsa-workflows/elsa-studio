using Elsa.Studio.Contracts;
using Elsa.Studio.Models;
using Elsa.Studio.UIHintHandlers.Components;
using Microsoft.AspNetCore.Components;

namespace Elsa.Studio.UIHintHandlers.Handlers;

public class CheckboxHandler : IUIHintHandler
{
    public bool GetSupportsUIHint(string uiHint) => uiHint == "checkbox";
    public string UISyntax => WellKnownSyntaxNames.Literal;

    public RenderFragment DisplayInputEditor(DisplayInputEditorContext context)
    {
        return builder =>
        {
            builder.OpenComponent(0, typeof(Checkbox));
            builder.AddAttribute(1, nameof(Checkbox.EditorContext), context);
            builder.CloseComponent();
        };
    }
}