using Elsa.Api.Client.Resources.WorkflowInstances.Models;
using Elsa.Studio.Workflows.Pages.WorkflowDefinitions.Edit;
using Elsa.Studio.Workflows.UI.Contracts;
using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace Elsa.Studio.Workflows.Pages.WorkflowInstances.View.Components;

public partial class Workspace : IWorkspace
{
    private MudDynamicTabs _dynamicTabs = default!;

    [Parameter] public IList<WorkflowInstance> WorkflowInstances { get; set; } = default!;
    [Parameter] public Func<WorkflowInstance, Task>? SelectedWorkflowInstanceChanged { get; set; }

    public bool IsReadOnly => true;
    private int ActiveTabIndex { get; } = 0;
    private IDictionary<string, WorkflowEditor> WorkflowEditors { get; } = new Dictionary<string, WorkflowEditor>();
    public WorkflowInstance? SelectedWorkflowInstance => ActiveTabIndex >= 0 && ActiveTabIndex < WorkflowInstances.Count ? WorkflowInstances.ElementAtOrDefault(ActiveTabIndex) : default;
    
    private Task AddTabCallback()
    {
        return Task.CompletedTask;
    }

    private Task CloseTabCallback(MudTabPanel arg)
    {
        return Task.CompletedTask;
    }

    private async Task OnActivePanelIndexChanged(int value)
    {
        if(SelectedWorkflowInstanceChanged != null)
            await SelectedWorkflowInstanceChanged(SelectedWorkflowInstance!);
    }
}