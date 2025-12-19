namespace Elsa.Studio.Login.Pages.Login.Models;

internal class LoginModel
{
    /// <summary>
    /// Gets or sets the username.
    /// </summary>
    public string Username { get; set; } = default!;
    /// <summary>
    /// Gets or sets the password.
    /// </summary>
    public string Password { get; set; } = default!;
    /// <summary>
    /// Indicates whether remember me.
    /// </summary>
    public bool RememberMe { get; set; }
}