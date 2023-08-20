using Microsoft.AspNetCore.Components;

namespace Elsa.Studio.Login.Components;

/// <summary>
/// Redirects to the login page.
/// </summary>
public class RedirectToLogin : ComponentBase
{
    /// <summary>
    /// Gets or sets the <see cref="NavigationManager"/>.
    /// </summary>
    [Inject] protected NavigationManager NavigationManager { get; set; } = default!;

    /// <inheritdoc />
    protected override Task OnAfterRenderAsync(bool firstRender)
    {
        NavigationManager.NavigateTo("login", true);
        return Task.CompletedTask;
    }
}