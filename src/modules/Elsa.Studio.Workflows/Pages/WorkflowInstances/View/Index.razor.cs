using Elsa.Api.Client.Resources.WorkflowInstances.Models;
using Elsa.Studio.Workflows.Domain.Contracts;
using Elsa.Studio.Workflows.Pages.WorkflowInstances.View.Components;
using Microsoft.AspNetCore.Components;

namespace Elsa.Studio.Workflows.Pages.WorkflowInstances.View;

public partial class Index
{
    private IList<WorkflowInstance> _workflowInstances = new List<WorkflowInstance>();

    [Parameter] public string Id { get; set; } = default!;

    [Inject] private IWorkflowInstanceService WorkflowInstanceService { get; set; } = default!;

    [Inject] private IWorkflowDefinitionService WorkflowDefinitionService { get; set; } = default!;

    private Journal Journal { get; set; } = default!;

    protected override async Task OnInitializedAsync()
    {
        var instance = await WorkflowInstanceService.GetAsync(Id) ?? throw new InvalidOperationException($"Workflow instance with ID {Id} not found.");
        _workflowInstances = new List<WorkflowInstance> { instance! };
        await SelectWorkflowInstanceAsync(instance);
    }

    private async Task SelectWorkflowInstanceAsync(WorkflowInstance workflowInstance)
    {
        await Journal.SetWorkflowInstanceAsync(workflowInstance);
    }

    private async Task OnSelectedWorkflowInstanceChanged(WorkflowInstance value)
    {
        await SelectWorkflowInstanceAsync(value);
    }
}