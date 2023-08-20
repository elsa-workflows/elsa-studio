using Elsa.Studio.Contracts;
using Elsa.Studio.Models;
using Elsa.Studio.UIHints.Components;
using Microsoft.AspNetCore.Components;

namespace Elsa.Studio.UIHints.Handlers;

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