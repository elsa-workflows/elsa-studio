using Elsa.Api.Client.Activities;
using Elsa.Api.Client.Models;
using Elsa.Api.Client.Resources.ActivityDescriptors.Models;
using Elsa.Studio.Contracts;

namespace Elsa.Studio.Models;

public record DisplayInputEditorContext(
    Activity Activity,
    ActivityDescriptor ActivityDescriptor,
    InputDescriptor InputDescriptor,
    ActivityInput? Value,
    ISyntaxProvider? SyntaxProvider,
    Func<ActivityInput, Task> OnValueChanged);