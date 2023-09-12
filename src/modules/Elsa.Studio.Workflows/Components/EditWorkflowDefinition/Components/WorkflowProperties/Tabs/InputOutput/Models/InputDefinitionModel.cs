using Elsa.Api.Client.Resources.StorageDrivers.Models;
using Elsa.Api.Client.Resources.WorkflowDefinitions.Models;

namespace Elsa.Studio.Workflows.Components.EditWorkflowDefinition.Components.WorkflowProperties.Tabs.InputOutput.Models;

public class InputDefinitionModel : ArgumentDefinitionModel
{
    public UIHintDescriptor? UIHint { get; set; }

    public StorageDriverDescriptor StorageDriver { get; set; } = default!;
}