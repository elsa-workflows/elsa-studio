using Elsa.Studio.Contracts;
using Elsa.Studio.Models;
using Elsa.Studio.UIHints.Components;
using Microsoft.AspNetCore.Components;

namespace Elsa.Studio.UIHints.Handlers;

public class SingleLineHandler : IUIHintHandler
{
    public bool GetSupportsUIHint(string uiHint) => uiHint == "singleline";
    public string UISyntax => WellKnownSyntaxNames.Literal;

    public RenderFragment DisplayInputEditor(DisplayInputEditorContext context)
    {
        return builder =>
        {
            builder.OpenComponent(0, typeof(SingleLine));
            builder.AddAttribute(1, nameof(SingleLine.EditorContext), context);
            builder.CloseComponent();
        };
    }
}