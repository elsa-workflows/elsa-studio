using Elsa.Studio.Models;
using Microsoft.AspNetCore.Components;

namespace Elsa.Studio.Contracts;

public interface IUIHintHandler
{
    bool GetSupportsUIHint(string uiHint);
    RenderFragment DisplayInputEditor(DisplayInputEditorContext context);
}