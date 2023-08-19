using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Nodes;
using System.Threading.Tasks;
using Elsa.Api.Client.Resources.WorkflowDefinitions.Models;
using Elsa.Api.Client.Resources.WorkflowInstances.Models;
using Elsa.Studio.Workflows.Pages.WorkflowDefinitions.Edit;
using Elsa.Studio.Workflows.Pages.WorkflowInstances.View.Models;
using Elsa.Studio.Workflows.Screens.EditWorkflowDefinition;
using Elsa.Studio.Workflows.Screens.EditWorkflowDefinition.Components;
using Elsa.Studio.Workflows.Shared.Args;
using Elsa.Studio.Workflows.UI.Contracts;
using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace Elsa.Studio.Workflows.Pages.WorkflowInstances.View.Components;

public partial class Workspace : IWorkspace
{
    private MudDynamicTabs _dynamicTabs = default!;

    [Parameter] public IList<WorkflowInstance> WorkflowInstances { get; set; } = default!;
    [Parameter] public IList<WorkflowDefinition> WorkflowDefinitions { get; set; } = default!;
    [Parameter] public JournalEntry? SelectedWorkflowExecutionLogRecord { get; set; }
    [Parameter] public Func<WorkflowInstance, Task>? SelectedWorkflowInstanceChanged { get; set; }
    [Parameter] public Func<DesignerPathChangedArgs, Task>? PathChanged { get; set; }
    [Parameter] public Func<JsonObject, Task>? ActivitySelected { get; set; }

    public bool IsReadOnly => true;
    private int ActiveTabIndex { get; } = 0;
    private IDictionary<string, WorkflowEditor> WorkflowEditors { get; } = new Dictionary<string, WorkflowEditor>();
    private WorkflowInstance? SelectedWorkflowInstance => ActiveTabIndex >= 0 && ActiveTabIndex < WorkflowInstances.Count ? WorkflowInstances.ElementAtOrDefault(ActiveTabIndex) : default;

    private WorkflowDefinition? SelectedWorkflowDefinition
    {
        get
        {
            var instance = SelectedWorkflowInstance;
            return instance == null ? default : WorkflowDefinitions.FirstOrDefault(x => x.Id == instance.DefinitionVersionId);
        }
    }

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