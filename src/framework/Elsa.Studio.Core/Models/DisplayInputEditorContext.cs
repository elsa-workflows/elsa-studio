using Elsa.Api.Client.Activities;
using Elsa.Api.Client.Models;
using Elsa.Api.Client.Resources.ActivityDescriptors.Models;
using Elsa.Studio.Contracts;

namespace Elsa.Studio.Models;

public class DisplayInputEditorContext
{
    public Activity Activity { get; set; } = default!;
    public ActivityDescriptor ActivityDescriptor { get; set; } = default!;
    public InputDescriptor InputDescriptor { get; set; } = default!;
    public ActivityInput? Value { get; set; }
    public ISyntaxProvider? SyntaxProvider { get; set; }
    public Func<ActivityInput, Task> OnValueChanged { get; set; } = default!;
}