using Elsa.Studio.Models;
using Microsoft.AspNetCore.Components;

namespace Elsa.Studio.Contracts;

public interface IUIHintHandler
{
    bool GetSupportsUIHint(string uiHint);
    string UISyntax { get; }
    RenderFragment DisplayInputEditor(DisplayInputEditorContext context);
}