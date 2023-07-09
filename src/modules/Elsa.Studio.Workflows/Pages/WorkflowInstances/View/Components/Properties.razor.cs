using Elsa.Api.Client.Resources.WorkflowInstances.Models;
using Microsoft.AspNetCore.Components;

namespace Elsa.Studio.Workflows.Pages.WorkflowInstances.View.Components;

public partial class Properties
{
    [Parameter] public WorkflowInstance WorkflowInstance { get; set; } = default!;
}