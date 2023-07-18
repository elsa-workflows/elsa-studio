using Elsa.Studio.Contracts;
using Elsa.Studio.Models;
using Elsa.Studio.UIHintHandlers.Components;
using Microsoft.AspNetCore.Components;

namespace Elsa.Studio.UIHintHandlers.Handlers;

public class OutputPickerHandler : IUIHintHandler
{
    public bool GetSupportsUIHint(string uiHint) => uiHint == "output-picker";
    public string UISyntax => WellKnownSyntaxNames.Literal;

    public RenderFragment DisplayInputEditor(DisplayInputEditorContext context)
    {
        return builder =>
        {
            builder.OpenComponent(0, typeof(OutputPicker));
            builder.AddAttribute(1, nameof(OutputPicker.EditorContext), context);
            builder.CloseComponent();
        };
    }
}