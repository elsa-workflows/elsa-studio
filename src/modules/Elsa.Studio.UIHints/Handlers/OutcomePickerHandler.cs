using Elsa.Studio.Contracts;
using Elsa.Studio.Models;
using Elsa.Studio.UIHints.Components;
using Microsoft.AspNetCore.Components;

namespace Elsa.Studio.UIHints.Handlers;

public class OutcomePickerHandler : IUIHintHandler
{
    public bool GetSupportsUIHint(string uiHint) => uiHint == "outcome-picker";
    public string UISyntax => WellKnownSyntaxNames.Literal;

    public RenderFragment DisplayInputEditor(DisplayInputEditorContext context)
    {
        return builder =>
        {
            builder.OpenComponent(0, typeof(OutcomePicker));
            builder.AddAttribute(1, nameof(OutcomePicker.EditorContext), context);
            builder.CloseComponent();
        };
    }
}