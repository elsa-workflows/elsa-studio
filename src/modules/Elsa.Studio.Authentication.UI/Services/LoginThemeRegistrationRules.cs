using Elsa.Studio.Authentication.UI.Contracts;
using Elsa.Studio.Authentication.UI.Models;

namespace Elsa.Studio.Authentication.UI.Services;

internal static class LoginThemeRegistrationRules
{
    public static LoginThemeSelection ValidateAndSelect(
        IEnumerable<LoginThemeRegistration> registrations,
        string? configuredTheme)
    {
        var materialized = registrations.ToArray();
        var errors = new List<string>();

        foreach (var registration in materialized)
        {
            if (string.IsNullOrWhiteSpace(registration.Id))
                errors.Add("Login theme registrations require a non-blank ID.");

            if (!typeof(ILoginThemeProvider).IsAssignableFrom(registration.ProviderType))
                errors.Add($"Login theme '{registration.Id}' must use an {nameof(ILoginThemeProvider)} provider type.");
        }

        foreach (var duplicate in materialized
                     .Where(x => !string.IsNullOrWhiteSpace(x.Id))
                     .GroupBy(x => x.Id, StringComparer.OrdinalIgnoreCase)
                     .Where(x => x.Count() > 1))
        {
            errors.Add($"Login theme '{duplicate.First().Id}' is registered more than once.");
        }

        LoginThemeRegistration? selected = null;
        if (string.IsNullOrWhiteSpace(configuredTheme))
        {
            errors.Add("Authentication:Login:Theme must not be blank.");
        }
        else
        {
            selected = materialized.FirstOrDefault(
                x => string.Equals(x.Id, configuredTheme, StringComparison.OrdinalIgnoreCase));

            if (selected is null)
                errors.Add($"Authentication:Login:Theme selects '{configuredTheme}', but no matching login theme is registered.");
        }

        return new(selected, errors);
    }
}

internal sealed record LoginThemeSelection(
    LoginThemeRegistration? Selected,
    IReadOnlyList<string> Errors);
