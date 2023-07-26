using Elsa.Api.Client.Resources.StorageDrivers.Models;
using Elsa.Api.Client.Resources.VariableTypes.Models;

namespace Elsa.Studio.Workflows.Screens.EditWorkflowDefinition.Components.WorkflowProperties.Tabs.Variables.Models;

public class VariableModel
{
    public string Id { get; set; } = default!;
    public string Name { get; set; } = default!;
    public VariableTypeDescriptor VariableType { get; set; } = default!;
    public bool IsArray { get; set; }
    public string? DefaultValue { get; set; }
    public StorageDriverDescriptor StorageDriver { get; set; } = default!;
}