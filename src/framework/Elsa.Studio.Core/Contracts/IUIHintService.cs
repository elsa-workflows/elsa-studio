using Elsa.Studio.Models;
using Microsoft.AspNetCore.Components;

namespace Elsa.Studio.Contracts;

public interface IUIHintService
{
    IUIHintHandler GetHandler(string uiHint);
    //RenderFragment DisplayInputEditor(DisplayInputEditorContext context);
}