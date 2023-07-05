using Elsa.Api.Client.Resources.WorkflowDefinitions.Models;
using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace Elsa.Studio.Workflows.Pages.WorkflowDefinitions.Edit;

public partial class Workspace
{
    private readonly IDictionary<string, WorkflowEditor> _workflowEditors = new Dictionary<string, WorkflowEditor>();
    private MudDynamicTabs _dynamicTabs = default!;

    [Parameter] public IList<WorkflowDefinition> WorkflowDefinitions { get; set; } = default!;
    
    private int ActiveTabIndex { get; } = 0;
    private WorkflowDefinition? SelectedWorkflowDefinition => ActiveTabIndex >= 0 && ActiveTabIndex < WorkflowDefinitions.Count ? WorkflowDefinitions.ElementAtOrDefault(ActiveTabIndex) : default;

    private Task AddTabCallback()
    {
        return Task.CompletedTask;
    }

    private Task CloseTabCallback(MudTabPanel arg)
    {
        return Task.CompletedTask;
    }

    private async Task OnWorkflowDefinitionPropsUpdated()
    {
        var workflowEditor = _workflowEditors[SelectedWorkflowDefinition!.DefinitionId];
        await workflowEditor.NotifyWorkflowChangedAsync();
    }
    
    private Task OnWorkflowDefinitionUpdated()
    {
        var definitionId = SelectedWorkflowDefinition!.DefinitionId;
        var workflowEditor = _workflowEditors[definitionId];
        WorkflowDefinitions[ActiveTabIndex] = workflowEditor.WorkflowDefinition!;
        StateHasChanged();
        return Task.CompletedTask;
    }
}