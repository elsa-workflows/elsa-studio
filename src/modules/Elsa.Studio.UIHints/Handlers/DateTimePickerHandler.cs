using Elsa.Studio.Contracts;
using Elsa.Studio.Models;
using Elsa.Studio.UIHints.Components;
using Microsoft.AspNetCore.Components;

namespace Elsa.Studio.UIHints.Handlers;

public class DateTimePickerHandler : IUIHintHandler
{
    public bool GetSupportsUIHint(string uiHint) => uiHint == InputUIHints.DateTimePicker;

    public string UISyntax => WellKnownSyntaxNames.Literal;

    public RenderFragment DisplayInputEditor(DisplayInputEditorContext context)
    {
        return builder =>
        {
            builder.OpenComponent(0, typeof(DateTimePicker));
            builder.AddAttribute(1, nameof(DateTimePicker.EditorContext), context);
            builder.CloseComponent();
        };
    }
}