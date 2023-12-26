using Elsa.Studio.Contracts;
using Elsa.Studio.Models;
using Elsa.Studio.UIHints.Components;
using Microsoft.AspNetCore.Components;

namespace Elsa.Studio.UIHints.Handlers;

public class CheckListHandler : IUIHintHandler
{
    public bool GetSupportsUIHint(string uiHint) => uiHint == "checklist";
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