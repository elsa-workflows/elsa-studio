using Elsa.Studio.Contracts;
using Elsa.Studio.Extensions;
using Elsa.Studio.Login.Components;
using Microsoft.AspNetCore.Components;

namespace Elsa.Studio.Login.Services;

public class LoginPageProvider : ILoginPageProvider
{
    public RenderFragment GetLoginPage()
    {
        return builder => builder.CreateComponent<RedirectToLogin>();
    }
}