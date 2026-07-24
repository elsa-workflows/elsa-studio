using Elsa.Studio.Authentication.Abstractions.Models;
using Elsa.Studio.Authentication.Abstractions.Options;
using Microsoft.Extensions.Options;

namespace Elsa.Studio.Authentication.Abstractions.Validation;

/// <summary>
/// Rejects ambiguous or incomplete direct-versus-broker authentication registrations.
/// </summary>
public sealed class StudioAuthenticationOptionsValidator(
    IEnumerable<StudioAuthenticationProviderRegistration> registrations) : IValidateOptions<StudioAuthenticationOptions>
{
    public ValidateOptionsResult Validate(string? name, StudioAuthenticationOptions options)
    {
        var activeRegistrations = registrations
            .Select(x => x.Provider)
            .Where(IsOpenIdConnectMode)
            .Distinct()
            .ToArray();

        if (activeRegistrations.Length > 1)
        {
            return ValidateOptionsResult.Fail(
                "Direct OpenID Connect and Brokered External Authentication handlers are both registered. " +
                "Register only the provider selected by Authentication:Provider.");
        }

        if (IsOpenIdConnectMode(options.Provider) &&
            (activeRegistrations.Length == 0 || activeRegistrations[0] != options.Provider))
        {
            return ValidateOptionsResult.Fail(
                $"Authentication:Provider selects '{options.Provider}', but its handler is not the sole active OpenID Connect authentication registration.");
        }

        if (!IsOpenIdConnectMode(options.Provider) && activeRegistrations.Length > 0)
        {
            return ValidateOptionsResult.Fail(
                $"Authentication:Provider selects '{options.Provider}', but '{activeRegistrations[0]}' handlers are also registered.");
        }

        return ValidateOptionsResult.Success;
    }

    private static bool IsOpenIdConnectMode(StudioAuthenticationProvider provider) =>
        provider is StudioAuthenticationProvider.OpenIdConnect or StudioAuthenticationProvider.ExternalAuthentication;
}
