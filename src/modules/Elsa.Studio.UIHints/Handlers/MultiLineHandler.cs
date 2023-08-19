using Elsa.Studio.Contracts;
using Elsa.Studio.Models;
using Elsa.Studio.UIHints.Components;
using Microsoft.AspNetCore.Components;

namespace Elsa.Studio.UIHints.Handlers;

public class MultiLineHandler : IUIHintHandler
{
    public bool GetSupportsUIHint(string uiHint) => uiHint == "multi-line";
    public string UISyntax => WellKnownSyntaxNames.Literal;

    public RenderFragment DisplayInputEditor(DisplayInputEditorContext context)
    {
        return builder =>
        {
            builder.OpenComponent(0, typeof(MultiLine));
            builder.AddAttribute(1, nameof(MultiLine.EditorContext), context);
            builder.CloseComponent();
        };
    }
}