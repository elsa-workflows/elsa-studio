using Elsa.Api.Client.Resources.StorageDrivers.Models;
using Elsa.Api.Client.Resources.VariableTypes.Models;

namespace Elsa.Studio.Workflows.Components.WorkflowDefinitionEditor.Components.WorkflowProperties.Tabs.Variables.Models;

public class VariableModel
{
    public string Id { get; set; } = null!;
    public string Name { get; set; } = null!;
    public VariableTypeDescriptor VariableType { get; set; } = null!;
    public bool IsArray { get; set; }
    public string? DefaultValue { get; set; }
    public StorageDriverDescriptor StorageDriver { get; set; } = null!;
}