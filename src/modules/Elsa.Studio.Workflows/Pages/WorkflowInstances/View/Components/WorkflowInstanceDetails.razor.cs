using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Elsa.Api.Client.Resources.StorageDrivers.Models;
using Elsa.Api.Client.Resources.WorkflowDefinitions.Models;
using Elsa.Api.Client.Resources.WorkflowInstances.Enums;
using Elsa.Api.Client.Resources.WorkflowInstances.Models;
using Elsa.Studio.Workflows.Contracts;
using Elsa.Studio.Workflows.Domain.Contracts;
using Microsoft.AspNetCore.Components;

namespace Elsa.Studio.Workflows.Pages.WorkflowInstances.View.Components;

public partial class WorkflowInstanceDetails
{
    private WorkflowInstance? _workflowInstance;
    [Parameter] public WorkflowInstance? WorkflowInstance { get; set; }
    [Parameter] public WorkflowDefinition? WorkflowDefinition { get; set; }
    [Inject] private IStorageDriverService StorageDriverService { get; set; } = default!;
    [Inject] private IWorkflowInstanceObserverFactory WorkflowInstanceObserverFactory { get; set; } = default!;
    [Inject] private IWorkflowInstanceService WorkflowInstanceService { get; set; } = default!;
    private IDictionary<string, StorageDriverDescriptor> StorageDriverLookup { get; set; } = new Dictionary<string, StorageDriverDescriptor>();
    private IWorkflowInstanceObserver WorkflowInstanceObserver { get; set; } = default!;

    private IDictionary<string, string?> WorkflowInstanceData
    {
        get
        {
            if (_workflowInstance == null)
                return new Dictionary<string, string?>();

            return new Dictionary<string, string?>
            {
                ["ID"] = _workflowInstance.Id,
                ["Definition ID"] = _workflowInstance.DefinitionId,
                ["Definition version"] = _workflowInstance.Version.ToString(),
                ["Definition version ID"] = _workflowInstance.DefinitionVersionId,
                ["Correlation ID"] = _workflowInstance.CorrelationId,
                ["Status"] = _workflowInstance.Status.ToString(),
                ["Sub status"] = _workflowInstance.SubStatus.ToString(),
                ["Created"] = _workflowInstance.CreatedAt.ToString("G"),
                ["Updated"] = _workflowInstance.UpdatedAt.ToString("G"),
                ["Finished"] = _workflowInstance.FinishedAt?.ToString("G"),
            };
        }
    }

    protected override async Task OnInitializedAsync()
    {
        var drivers = await StorageDriverService.GetStorageDriversAsync();
        StorageDriverLookup = drivers.ToDictionary(x => x.TypeName);
    }

    protected override async Task OnParametersSetAsync()
    {
        if (_workflowInstance != WorkflowInstance)
        {
            _workflowInstance = WorkflowInstance;

            // If the workflow instance is still running, observe it.
            if (_workflowInstance?.Status == WorkflowStatus.Running)
                await ObserveWorkflowInstanceAsync();
        }
    }

    private async Task ObserveWorkflowInstanceAsync()
    {
        WorkflowInstanceObserver = await WorkflowInstanceObserverFactory.CreateAsync(_workflowInstance!.Id);
        WorkflowInstanceObserver.WorkflowInstanceUpdated += async _ => await InvokeAsync(async () =>
        {
            _workflowInstance = await WorkflowInstanceService.GetAsync(_workflowInstance!.Id);
            StateHasChanged();
        });
    }

    private string GetStorageDriverDisplayName(string? storageDriverTypeName)
    {
        if (storageDriverTypeName == null)
            return "None";

        return !StorageDriverLookup.TryGetValue(storageDriverTypeName, out var descriptor) ? storageDriverTypeName : descriptor.DisplayName;
    }
}