using System.Text.Json.Nodes;
using Elsa.Api.Client.Extensions;
using Elsa.Api.Client.Resources.WorkflowDefinitions.Models;
using Elsa.Api.Client.Resources.WorkflowInstances.Models;
using Elsa.Api.Client.Resources.WorkflowInstances.Requests;
using Elsa.Studio.Workflows.Domain.Contracts;
using Elsa.Studio.Workflows.Pages.WorkflowInstances.View.Components;
using Elsa.Studio.Workflows.Pages.WorkflowInstances.View.Models;
using Elsa.Studio.Workflows.Shared.Args;
using Microsoft.AspNetCore.Components;

namespace Elsa.Studio.Workflows.Pages.WorkflowInstances.View;

public partial class Index
{
    private IList<WorkflowInstance> _workflowInstances = new List<WorkflowInstance>();
    private IList<WorkflowDefinition> _workflowDefinitions = new List<WorkflowDefinition>();
    private Workspace _workspace = default!;

    [Parameter] public string Id { get; set; } = default!;

    [Inject] private IWorkflowInstanceService WorkflowInstanceService { get; set; } = default!;

    [Inject] private IWorkflowDefinitionService WorkflowDefinitionService { get; set; } = default!;

    private Journal Journal { get; set; } = default!;
    private JournalEntry? SelectedJournalEntry { get; set; }

    protected override async Task OnInitializedAsync()
    {
        var instance = await WorkflowInstanceService.GetAsync(Id) ?? throw new InvalidOperationException($"Workflow instance with ID {Id} not found.");
        var definitionVersionIds = new[] { instance.DefinitionVersionId };
        var definitions = await WorkflowDefinitionService.FindManyByIdAsync(definitionVersionIds);
        _workflowInstances = new List<WorkflowInstance> { instance };
        _workflowDefinitions = definitions.Items.ToList();
        await SelectWorkflowInstanceAsync(instance);
    }

    private async Task SelectWorkflowInstanceAsync(WorkflowInstance instance)
    {
        // Select activity IDs that are direct children of the root.
        var definition = _workflowDefinitions.First(x => x.Id == instance.DefinitionVersionId);
        var activityIds = definition.Root.GetActivities().Select(x => x.GetId()).ToList();
        var filter = new JournalFilter
        {
            ActivityIds = activityIds
        };
        await Journal.SetWorkflowInstanceAsync(instance, filter);
    }

    private async Task OnSelectedWorkflowInstanceChanged(WorkflowInstance value)
    {
        await SelectWorkflowInstanceAsync(value);
    }

    private async Task OnDesignerPathChanged(DesignerPathChangedArgs args)
    {
        var activityIds = args.ContainerActivity.GetActivities().Select(x => x.GetId()).ToList();
        var filter = new JournalFilter
        {
            ActivityIds = activityIds
        };
        var instance = _workflowInstances.First();
        await Journal.SetWorkflowInstanceAsync(instance, filter);
    }

    private Task OnWorkflowExecutionLogRecordSelected(JournalEntry entry)
    {
        SelectedJournalEntry = entry;
        StateHasChanged();
        return Task.CompletedTask;
    }

    private Task OnActivitySelected(JsonObject arg)
    {
        Journal.ClearSelection();
        return Task.CompletedTask;
    }
}