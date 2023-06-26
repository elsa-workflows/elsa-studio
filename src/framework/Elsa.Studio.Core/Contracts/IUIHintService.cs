namespace Elsa.Studio.Contracts;

public interface IUIHintService
{
    IUIHintHandler GetHandler(string uiHint);
    //RenderFragment DisplayInputEditor(DisplayInputEditorContext context);
}