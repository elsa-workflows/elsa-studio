using Elsa.Studio.Contracts;
using Elsa.Studio.Models;
using Elsa.Studio.UIHints.Components;
using Microsoft.AspNetCore.Components;

namespace Elsa.Studio.UIHints.Handlers;

public class RadioListHandler : IUIHintHandler
{
    public bool GetSupportsUIHint(string uiHint) => uiHint == InputUIHints.RadioList;

    public string UISyntax => WellKnownSyntaxNames.Literal;

    public RenderFragment DisplayInputEditor(DisplayInputEditorContext context)
    {
        return builder =>
        {
            builder.OpenComponent(0, typeof(RadioList));
            builder.AddAttribute(1, nameof(RadioList.EditorContext), context);
            builder.CloseComponent();
        };
    }
}