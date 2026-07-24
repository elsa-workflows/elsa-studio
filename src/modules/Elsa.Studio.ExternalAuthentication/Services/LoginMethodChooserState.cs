using Elsa.Studio.ExternalAuthentication.Models;

namespace Elsa.Studio.ExternalAuthentication.Services;

/// <summary>Pure chooser rules shared by both Studio hosts and deliberately easy to test.</summary>
public static class LoginMethodChooserState
{
    public static IReadOnlyList<LoginMethod> Order(IEnumerable<LoginMethod> methods) => methods
        .OrderBy(method => method.Order)
        .ThenBy(method => method.DisplayName, StringComparer.OrdinalIgnoreCase)
        .ThenBy(method => method.Key, StringComparer.Ordinal)
        .ToArray();

    public static LoginMethod? GetAutomaticMethod(LoginMethodsResponse response, bool chooserRequested, ISet<string> attemptedMethodKeys)
    {
        if (chooserRequested || string.IsNullOrWhiteSpace(response.AutomaticMethodKey))
            return null;

        return Order(response.Methods)
            .FirstOrDefault(method =>
                string.Equals(method.Kind, "external", StringComparison.OrdinalIgnoreCase) &&
                string.Equals(method.Key, response.AutomaticMethodKey, StringComparison.Ordinal) &&
                !attemptedMethodKeys.Contains(method.Key));
    }

    public static bool IsTrustedIcon(string? iconId) => iconId is not null && TrustedIcons.Contains(iconId);

    public static string GetAccessibleIconLabel(string? iconId) =>
        IsTrustedIcon(iconId) ? iconId! : "identity provider";

    private static readonly HashSet<string> TrustedIcons = new(StringComparer.OrdinalIgnoreCase)
    {
        "elsa", "building", "github", "microsoft", "google", "facebook", "x"
    };
}

/// <summary>Guards local post-authentication paths before a host constructs broker requests.</summary>
public static class LocalReturnPath
{
    public static string Normalize(string? candidate)
    {
        if (string.IsNullOrWhiteSpace(candidate) || !candidate.StartsWith("/", StringComparison.Ordinal) || candidate.StartsWith("//", StringComparison.Ordinal))
            return "/";

        return Uri.TryCreate(candidate, UriKind.Relative, out _) ? candidate : "/";
    }
}
