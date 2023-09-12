using Elsa.Api.Client.Resources.VariableTypes.Models;

namespace Elsa.Studio.Workflows.Components.EditWorkflowDefinition.Components.WorkflowProperties.Tabs.InputOutput.Models;

public abstract class ArgumentDefinitionModel
{
    public VariableTypeDescriptor Type { get; set; } = default!;
    public bool IsArray { get; set; }
    public string Name { get; set; } = default!;
    public string DisplayName { get; set; } = default!;
    public string Description { get; set; } = default!;
    public string Category { get; set; } = default!;
}