using Microsoft.AspNetCore.Components;

namespace Elsa.Studio.Workflows.Pages.WorkflowDefinition.Edit.ActivityProperties;

public class ActivityInputDisplayModel
{
    public ActivityInputDisplayModel(RenderFragment editor)
    {
        Editor = editor;
    }
    
    public RenderFragment Editor { get; }
}