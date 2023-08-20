using Elsa.Studio.Contracts;
using Elsa.Studio.Models;
using Elsa.Studio.UIHints.Components;
using Microsoft.AspNetCore.Components;

namespace Elsa.Studio.UIHints.Handlers;

public class SwitchEditorHandler : IUIHintHandler
{
    public bool GetSupportsUIHint(string uiHint) => uiHint is "flow-switch-editor" or "switch-editor";
    public string UISyntax => WellKnownSyntaxNames.Object;

    public RenderFragment DisplayInputEditor(DisplayInputEditorContext context)
    {
        return builder =>
        {
            builder.OpenComponent(0, typeof(Cases));
            builder.AddAttribute(1, nameof(Cases.EditorContext), context);
            builder.CloseComponent();
        };
    }
}