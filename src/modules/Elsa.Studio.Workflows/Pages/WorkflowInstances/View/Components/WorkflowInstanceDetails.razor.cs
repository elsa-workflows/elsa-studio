using Elsa.Api.Client.Resources.StorageDrivers.Models;
using Elsa.Api.Client.Resources.WorkflowDefinitions.Models;
using Elsa.Api.Client.Resources.WorkflowInstances.Models;
using Elsa.Studio.Workflows.Domain.Contracts;
using Microsoft.AspNetCore.Components;

namespace Elsa.Studio.Workflows.Pages.WorkflowInstances.View.Components;

public partial class WorkflowInstanceDetails
{
    [Parameter] public WorkflowInstance? WorkflowInstance { get; set; }
    [Parameter] public WorkflowDefinition? WorkflowDefinition { get; set; }
    [Inject] private IStorageDriverService StorageDriverService { get; set; } = default!;
    private IDictionary<string, StorageDriverDescriptor> StorageDriverLookup { get; set; } = new Dictionary<string, StorageDriverDescriptor>();

    private IDictionary<string, string?> WorkflowInstanceData
    {
        get
        {
            if(WorkflowInstance == null)
                return new Dictionary<string, string?>();
            
            return new Dictionary<string, string?>
            {
                ["ID"] = WorkflowInstance.Id,
                ["Definition ID"] = WorkflowInstance.DefinitionId,
                ["Definition version"] = WorkflowInstance.Version.ToString(),
                ["Definition version ID"] = WorkflowInstance.DefinitionVersionId,
                ["Correlation ID"] = WorkflowInstance.CorrelationId,
                ["Status"] = WorkflowInstance.Status.ToString(),
                ["Sub status"] = WorkflowInstance.SubStatus.ToString(),
                ["Created"] = WorkflowInstance.CreatedAt.ToString("G"),
                ["Updated"] = WorkflowInstance.UpdatedAt.ToString("G"),
                ["Finished"] = WorkflowInstance.FinishedAt?.ToString("G"),
            };
        }
    }

    protected override async Task OnInitializedAsync()
    {
        var drivers = await StorageDriverService.GetStorageDriversAsync();
        StorageDriverLookup = drivers.ToDictionary(x => x.TypeName);
    }
    
    private string GetStorageDriverDisplayName(string? storageDriverTypeName)
    {
        if (storageDriverTypeName == null)
            return "None";
        
        return !StorageDriverLookup.TryGetValue(storageDriverTypeName, out var descriptor) ? storageDriverTypeName : descriptor.DisplayName;
    }
}