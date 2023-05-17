using Microsoft.AspNetCore.Components;

namespace Elsa.Studio.Login.Components;

public class RedirectToLogin : ComponentBase
{
    [Inject] protected NavigationManager NavigationManager { get; set; } = default!;

    // USe Async version to prevent exception from happening, see: https://stackoverflow.com/a/70608500/690374
    protected override Task OnInitializedAsync()
    {
        NavigationManager.NavigateTo("login", true);
        return Task.CompletedTask;
    }
}