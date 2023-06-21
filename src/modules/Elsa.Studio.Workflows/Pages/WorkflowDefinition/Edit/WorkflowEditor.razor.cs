using Elsa.Api.Client.Activities;
using Elsa.Api.Client.Contracts;
using Elsa.Api.Client.Extensions;
using Elsa.Api.Client.Models;
using Elsa.Api.Client.Resources.ActivityDescriptors.Models;
using Elsa.Api.Client.Resources.WorkflowDefinitions.Models;
using Elsa.Studio.Workflows.Core.Contracts;
using Elsa.Studio.Workflows.Designer.Components;
using Elsa.Studio.Workflows.Models;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;

namespace Elsa.Studio.Workflows.Pages.WorkflowDefinition.Edit;

using WorkflowDefinition = Api.Client.Resources.WorkflowDefinitions.Models.WorkflowDefinition;

public partial class WorkflowEditor
{
    private int _seed = 3;
    private FlowchartDesigner _designer = default!;

    [CascadingParameter] public DragDropManager DragDropManager { get; set; } = default!;
    [Parameter] public WorkflowDefinition? WorkflowDefinition { get; set; }
    [Inject] private IWorkflowDefinitionService WorkflowDefinitionService { get; set; } = default!;
    [Inject] private IActivityTypeService ActivityTypeService { get; set; } = default!;

    private Activity? SelectedActivity { get; set; }

    void OnDragOver(DragEventArgs e)
    {
        if (DragDropManager.Payload is not ActivityDescriptor)
        {
            e.DataTransfer.DropEffect = "none";
            return;
        }

        e.DataTransfer.DropEffect = "move";
    }

    async Task OnDrop(DragEventArgs e)
    {
        if (DragDropManager.Payload is not ActivityDescriptor activityDescriptor)
        {
            return;
        }

        var newActivityType = ActivityTypeService.ResolveType(activityDescriptor.TypeName);
        var newActivity = (Activity)Activator.CreateInstance(newActivityType)!;

        var x = e.PageX;
        var y = e.PageY;

        newActivity.Id = activityDescriptor.TypeName + " " + _seed++;
        newActivity.Type = activityDescriptor.TypeName;
        newActivity.Version = activityDescriptor.Version;

        newActivity.SetDesignerMetadata(new ActivityDesignerMetadata
        {
            Position = new Position(x, y)
        });

        await _designer.AddActivityAsync(newActivity);
    }

    private async Task OnZoomToFitClick() => await _designer.ZoomToFitAsync();
    private async Task OnCenterContentClick() => await _designer.CenterContentAsync();

    private async Task OnSaveClick()
    {
        var flowchart = await _designer.ReadFlowchartAsync();
        var workflowDefinition = WorkflowDefinition ?? new WorkflowDefinition();

        var saveRequest = new SaveWorkflowDefinitionRequest
        {
            Model = new WorkflowDefinitionModel
            {
                Id = workflowDefinition.Id,
                Description = workflowDefinition.Description,
                Name = workflowDefinition.Name,
                Inputs = workflowDefinition.Inputs,
                Options = workflowDefinition.Options,
                Outcomes = workflowDefinition.Outcomes,
                Outputs = workflowDefinition.Outputs,
                Variables = workflowDefinition.Variables.Select(x => new VariableDefinition
                {
                    Id = x.Id,
                    Name = x.Name,
                    TypeName = x.TypeName,
                    Value = x.Value?.ToString(),
                    IsArray = x.IsArray,
                    StorageDriverTypeName = x.StorageDriverTypeName
                }).ToList(),
                Version = workflowDefinition.Version,
                CreatedAt = workflowDefinition.CreatedAt,
                CustomProperties = workflowDefinition.CustomProperties,
                DefinitionId = workflowDefinition.DefinitionId,
                IsLatest = workflowDefinition.IsLatest,
                IsPublished = workflowDefinition.IsPublished,
                Root = flowchart
            },
            Publish = false,
        };

        WorkflowDefinition = await WorkflowDefinitionService.SaveAsync(saveRequest);
    }

    private void OnActivitySelected(Activity activity)
    {
        SelectedActivity = activity;
    }

    private async Task OnSelectedActivityUpdated(Activity activity)
    {
        await _designer.UpdateActivityAsync(activity);
    }
}