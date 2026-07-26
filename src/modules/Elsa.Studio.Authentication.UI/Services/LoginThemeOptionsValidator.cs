using Elsa.Studio.Authentication.UI.Models;
using Elsa.Studio.Authentication.UI.Options;
using Microsoft.Extensions.Options;

namespace Elsa.Studio.Authentication.UI.Services;

/// <summary>
/// Validates login theme registrations and the configured selection during startup.
/// </summary>
public sealed class LoginThemeOptionsValidator(
    IEnumerable<LoginThemeRegistration> registrations) : IValidateOptions<LoginThemeOptions>
{
    public ValidateOptionsResult Validate(string? name, LoginThemeOptions options)
    {
        var result = LoginThemeRegistrationRules.ValidateAndSelect(registrations, options.Theme);

        return result.Errors.Count == 0
            ? ValidateOptionsResult.Success
            : ValidateOptionsResult.Fail(result.Errors);
    }
}
