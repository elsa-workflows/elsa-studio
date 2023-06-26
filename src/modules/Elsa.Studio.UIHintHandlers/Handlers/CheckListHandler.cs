using Elsa.Studio.Contracts;
using Elsa.Studio.Models;
using Elsa.Studio.UIHintHandlers.Components;
using Microsoft.AspNetCore.Components;

namespace Elsa.Studio.UIHintHandlers.Handlers;

public class CheckListHandler : IUIHintHandler
{
    public bool GetSupportsUIHint(string uiHint) => uiHint == "check-list";
    public string UISyntax => WellKnownSyntaxNames.Object;

    public RenderFragment DisplayInputEditor(DisplayInputEditorContext context)
    {
        return builder =>
        {
            builder.OpenComponent(0, typeof(CheckList));
            builder.AddAttribute(1, nameof(CheckList.EditorContext), context);
            builder.CloseComponent();
        };
    }
}