namespace Elsa.Studio.Login.Pages.Login.Models;

internal class LoginModel
{
    public string Username { get; set; } = default!;
    public string Password { get; set; } = default!;
    public bool RememberMe { get; set; }
}