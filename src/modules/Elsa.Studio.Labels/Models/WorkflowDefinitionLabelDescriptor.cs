using MudBlazor;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Elsa.Studio.Labels.Models;

public class WorkflowDefinitionLabelDescriptor
{
    public string Id { get; set; } = default!;

    public string Name { get; set; } = default!;
    public string WorkflowDefinitionId { get; internal set; }
    public string? Color { get; internal set; }
}
