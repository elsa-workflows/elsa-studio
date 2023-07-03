using Elsa.Studio.Contracts;
using Elsa.Studio.Models;
using Elsa.Studio.UIHintHandlers.Components;
using Microsoft.AspNetCore.Components;

namespace Elsa.Studio.UIHintHandlers.Handlers;

public class HttpStatusCodesHandler : IUIHintHandler
{
    public bool GetSupportsUIHint(string uiHint) => uiHint is "http-status-codes";
    public string UISyntax => WellKnownSyntaxNames.Object;

    public RenderFragment DisplayInputEditor(DisplayInputEditorContext context)
    {
        return builder =>
        {
            builder.OpenComponent(0, typeof(HttpStatusCodes));
            builder.AddAttribute(1, nameof(HttpStatusCodes.EditorContext), context);
            builder.CloseComponent();
        };
    }
}