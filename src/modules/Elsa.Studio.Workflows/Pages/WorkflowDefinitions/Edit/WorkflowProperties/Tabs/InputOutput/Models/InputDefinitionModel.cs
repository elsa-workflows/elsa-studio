using Elsa.Api.Client.Resources.StorageDrivers.Models;
using Elsa.Api.Client.Resources.VariableTypes.Models;
using Elsa.Api.Client.Resources.WorkflowDefinitions.Models;

namespace Elsa.Studio.Workflows.Pages.WorkflowDefinitions.Edit.WorkflowProperties.Tabs.InputOutput.Models;

public class InputDefinitionModel : ArgumentDefinitionModel
{
    public UIHintDescriptor? UIHint { get; set; }

    public StorageDriverDescriptor StorageDriver { get; set; } = default!;
}