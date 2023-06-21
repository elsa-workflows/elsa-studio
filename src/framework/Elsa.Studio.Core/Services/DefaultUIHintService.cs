using Elsa.Studio.Contracts;
using Elsa.Studio.Models;
using Microsoft.AspNetCore.Components;

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
        return handler == null ? builder => builder.AddContent(0, context.Value) : handler.DisplayInputEditor(context);
    }
}