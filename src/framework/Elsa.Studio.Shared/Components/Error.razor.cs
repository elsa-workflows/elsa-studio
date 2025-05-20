using Microsoft.AspNetCore.Components;

namespace Elsa.Studio.Components;

public partial class Error : ComponentBase
{
    [Parameter] public Exception Context { get; set; } = null!;
}