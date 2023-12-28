using Elsa.Studio.Contracts;
using Elsa.Studio.Models;
using Elsa.Studio.UIHints.Components;
using Microsoft.AspNetCore.Components;

namespace Elsa.Studio.UIHints.Handlers;

public class JsonEditorHandler : IUIHintHandler
{
    public bool GetSupportsUIHint(string uiHint) => uiHint == "json-editor";
    public string UISyntax => WellKnownSyntaxNames.Literal;

    public RenderFragment DisplayInputEditor(DisplayInputEditorContext context)
    {
        return builder =>
        {
            builder.OpenComponent(0, typeof(Code));
            builder.AddAttribute(1, nameof(Code.EditorContext), context);
            builder.CloseComponent();
        };
    }
}