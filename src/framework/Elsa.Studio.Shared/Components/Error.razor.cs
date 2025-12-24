using Microsoft.AspNetCore.Components;

namespace Elsa.Studio.Components;

/// <summary>
/// Represents the error.
/// </summary>
public partial class Error : ComponentBase
{
    [Parameter] public Exception Context { get; set; } = null!;
}