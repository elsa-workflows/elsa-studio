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
        var returnUrl = NavigationManager.ToBaseRelativePath(NavigationManager.Uri);
        if (string.IsNullOrWhiteSpace(returnUrl))
        {
            NavigationManager.NavigateTo("login", true);
        }
        else
        {
            NavigationManager.NavigateTo($"login?returnUrl={returnUrl}", true);
        }
        
        return Task.CompletedTask;
    }
}