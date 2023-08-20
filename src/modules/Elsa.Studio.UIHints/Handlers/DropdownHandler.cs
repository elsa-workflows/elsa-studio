using Elsa.Studio.Contracts;
using Elsa.Studio.Models;
using Elsa.Studio.UIHints.Components;
using Microsoft.AspNetCore.Components;

namespace Elsa.Studio.UIHints.Handlers;

public class DropdownHandler : IUIHintHandler
{
    public bool GetSupportsUIHint(string uiHint) => uiHint == "dropdown";
    public string UISyntax => WellKnownSyntaxNames.Literal;

    public RenderFragment DisplayInputEditor(DisplayInputEditorContext context)
    {
        return builder =>
        {
            builder.OpenComponent(0, typeof(Dropdown));
            builder.AddAttribute(1, nameof(Dropdown.EditorContext), context);
            builder.CloseComponent();
        };
    }
}