using Elsa.Studio.Models;
using Microsoft.AspNetCore.Components;

namespace Elsa.Studio.Contracts;

public interface IUIHintService
{
    RenderFragment DisplayInputEditor(DisplayInputEditorContext context);
}