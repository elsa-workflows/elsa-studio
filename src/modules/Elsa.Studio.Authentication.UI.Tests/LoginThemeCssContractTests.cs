namespace Elsa.Studio.Authentication.UI.Tests;

public sealed class LoginThemeCssContractTests
{
    private static readonly string[] LoginMethodTokens =
    [
        "--elsa-login-method-surface",
        "--elsa-login-method-border",
        "--elsa-login-method-hover-surface",
        "--elsa-login-method-preferred-surface",
        "--elsa-login-method-preferred-border",
        "--elsa-login-method-preferred-color",
        "--elsa-login-method-icon-surface",
        "--elsa-login-method-icon-color",
        "--elsa-login-method-shadow"
    ];

    [Fact]
    public void LoginMethodTokens_ArePublicAndSupportedByBuiltInThemeFamilies()
    {
        var classicCss = ReadRepositoryFile(
            "src", "modules", "Elsa.Studio.Authentication.UI", "wwwroot", "css", "login.css");
        var readme = ReadRepositoryFile(
            "src", "modules", "Elsa.Studio.Authentication.UI", "README.md");
        var classicTheme = GetRuleText(classicCss, ".classic-login-theme");
        var modernTheme = GetRuleText(ReadModernThemeCss(), ".modern-login-theme");

        foreach (var token in LoginMethodTokens)
        {
            Assert.Contains(token, readme, StringComparison.Ordinal);
            Assert.Contains($"{token}:", classicTheme, StringComparison.Ordinal);
            Assert.Contains($"{token}:", modernTheme, StringComparison.Ordinal);
        }
    }

    [Fact]
    public void LightModernTheme_OverridesTheCompleteLoginMethodPalette()
    {
        var humanAutomationTheme = GetRuleText(
            ReadModernThemeCss(),
            ".modern-login-theme[data-theme=\"human-automation\"]");

        foreach (var token in LoginMethodTokens)
            Assert.Contains($"{token}:", humanAutomationTheme, StringComparison.Ordinal);
    }

    private static string ReadModernThemeCss() =>
        ReadRepositoryFile(
            "src", "modules", "Elsa.Studio.Authentication.Themes", "wwwroot", "css", "login-themes.css");

    private static string ReadRepositoryFile(params string[] pathSegments) =>
        File.ReadAllText(Path.Combine([FindRepositoryRoot(), .. pathSegments]));

    private static string GetRuleText(string css, string selector)
    {
        var start = css.IndexOf($"{selector} {{", StringComparison.Ordinal);
        Assert.True(start >= 0, $"CSS rule '{selector}' was not found.");
        var end = css.IndexOf('}', start);
        Assert.True(end >= 0, $"CSS rule '{selector}' is not closed.");
        return css[start..end];
    }

    private static string FindRepositoryRoot()
    {
        for (var current = new DirectoryInfo(AppContext.BaseDirectory); current is not null; current = current.Parent)
        {
            if (Directory.Exists(Path.Combine(current.FullName, "src", "modules", "Elsa.Studio.Authentication.UI")))
                return current.FullName;
        }

        throw new DirectoryNotFoundException("Could not locate the Elsa Studio repository root.");
    }
}
