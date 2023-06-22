using Elsa.Api.Client.Activities;
using Elsa.Api.Client.Contracts;
using Elsa.Api.Client.Resources.WorkflowDefinitions.Models;
using Elsa.Studio.Workflows.Core.Contracts;
using Elsa.Studio.Workflows.Models;
using Elsa.Studio.Workflows.Pages.WorkflowDefinition.Edit.ActivityProperties;
using Elsa.Studio.Workflows.Pages.WorkflowDefinition.Edit.DiagramEditors;
using Microsoft.AspNetCore.Components;

namespace Elsa.Studio.Workflows.Pages.WorkflowDefinition.Edit;

using WorkflowDefinition = Api.Client.Resources.WorkflowDefinitions.Models.WorkflowDefinition;

public partial class WorkflowEditor
{
    private FlowchartEditor _flowchartEditor = default!;
    private IDiagramEditor _diagramEditor = default!;

    [CascadingParameter] public DragDropManager DragDropManager { get; set; } = default!;
    [Parameter] public WorkflowDefinition? WorkflowDefinition { get; set; }
    [Inject] private IWorkflowDefinitionService WorkflowDefinitionService { get; set; } = default!;
    [Inject] private IActivityTypeService ActivityTypeService { get; set; } = default!;

    private FlowchartEditor FlowchartEditor
    {
        get => _flowchartEditor;
        set
        {
            _flowchartEditor = value;
            _diagramEditor = value;
        }
    }

    private Activity? SelectedActivity { get; set; }
    private ActivityPropertiesTabs? ActivityPropertiesTab { get; set; }

    private async Task SaveAsync(Activity root)
    {
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
                Root = root
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
        await _diagramEditor.UpdateActivityAsync(activity);
    }

    private async Task OnSaveRequested()
    {
        var root = await _diagramEditor.ReadRootActivityAsync();
        await SaveAsync(root);
    }
}