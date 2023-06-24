using Elsa.Studio.Contracts;
using Elsa.Studio.Models;
using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace Elsa.Studio.Services;

public class DefaultUIHintService : IUIHintService
{
    private readonly IEnumerable<IUIHintHandler> _handlers;

    public DefaultUIHintService(IEnumerable<IUIHintHandler> handlers)
    {
        _handlers = handlers;
    }

    public RenderFragment DisplayInputEditor(DisplayInputEditorContext context)
    {
        var handler = _handlers.FirstOrDefault(x => x.GetSupportsUIHint(context.InputDescriptor.UIHint));
        return handler == null ? DisplayUnsupportedUIHint(context) : handler.DisplayInputEditor(context);
    }

    private RenderFragment DisplayUnsupportedUIHint(DisplayInputEditorContext context)
    {
        return builder =>
        {
            builder.OpenComponent<MudAlert>(0);
            builder.AddAttribute(1, nameof(MudAlert.Severity), Severity.Warning);
            builder.AddAttribute(2, nameof(MudAlert.Class), "my-2");
            builder.AddAttribute(3, nameof(MudText.ChildContent), (RenderFragment)(textBuilder =>
            {
                textBuilder.AddContent(0, $"Unsupported UI hint: {context.InputDescriptor.UIHint}");
            }));
            builder.CloseElement();
        };
    }
}