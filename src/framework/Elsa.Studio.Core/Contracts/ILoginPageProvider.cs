using Microsoft.AspNetCore.Components;

namespace Elsa.Studio.Contracts;

/// <summary>
/// Provides the <see cref="RenderFragment"/> to display the login page.
/// </summary>
public interface ILoginPageProvider
{
    RenderFragment GetLoginPage();
}