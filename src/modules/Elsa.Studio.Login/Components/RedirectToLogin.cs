using Microsoft.AspNetCore.Components;

namespace Elsa.Studio.Login.Components;

public class RedirectToLogin : ComponentBase
{
    [Inject] protected NavigationManager NavigationManager { get; set; } = default!;

    protected override Task OnAfterRenderAsync(bool firstRender)
    {
        NavigationManager.NavigateTo("login", true);
        return Task.CompletedTask;
    }
}