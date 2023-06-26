using Elsa.Studio.Contracts;
using Elsa.Studio.Models;
using Elsa.Studio.UIHintHandlers.Components;
using Microsoft.AspNetCore.Components;

namespace Elsa.Studio.UIHintHandlers.Handlers;

public class MultiTextHandler : IUIHintHandler
{
    public bool GetSupportsUIHint(string uiHint) => uiHint == "multi-text";
    public string UISyntax => WellKnownSyntaxNames.Object;

    public RenderFragment DisplayInputEditor(DisplayInputEditorContext context)
    {
        return builder =>
        {
            builder.OpenComponent(0, typeof(MultiText));
            builder.AddAttribute(1, nameof(MultiText.EditorContext), context);
            builder.CloseComponent();
        };
    }
}