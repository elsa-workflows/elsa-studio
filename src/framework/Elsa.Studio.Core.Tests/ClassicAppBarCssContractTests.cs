using Xunit;

namespace Elsa.Studio.Core.Tests;

public sealed class ClassicAppBarCssContractTests
{
    private const string ClassicActionsSelector =
        ".elsa-studio-shell[data-elsa-theme=\"classic\"] .studio-appbar__actions";

    [Fact]
    public void UtilitiesUseSoftThemeScopedInteractionStates()
    {
        var repositoryRoot = FindRepositoryRoot();
        var layout = File.ReadAllText(Path.Combine(
            repositoryRoot, "src", "framework", "Elsa.Studio.Shared", "Layouts", "MainLayout.razor"));
        var css = File.ReadAllText(Path.Combine(
            repositoryRoot, "src", "framework", "Elsa.Studio.Shell", "wwwroot", "css", "shell.css"));

        Assert.Contains("studio-appbar__actions", layout, StringComparison.Ordinal);

        var neutralActions = GetRuleBody(css, ".studio-appbar__actions");
        var actions = GetRuleBody(css, ClassicActionsSelector);
        var languagePicker = GetRuleBody(css, $"{ClassicActionsSelector} .mud-button-outlined");
        var hover = GetRuleBody(
            css,
            $"{ClassicActionsSelector} :is(.mud-icon-button, .mud-button-root):not(.mud-disabled):not([disabled]):hover");
        var focus = GetRuleBody(
            css,
            $"{ClassicActionsSelector} :is(.mud-icon-button, .mud-button-root):focus-visible");

        Assert.Contains("display: contents", neutralActions, StringComparison.Ordinal);
        Assert.DoesNotContain("gap:", neutralActions, StringComparison.Ordinal);
        Assert.Contains("display: flex", actions, StringComparison.Ordinal);
        Assert.Contains("gap: var(--elsa-space-1)", actions, StringComparison.Ordinal);
        Assert.Contains("color: var(--elsa-text-muted)", actions, StringComparison.Ordinal);
        Assert.Contains("border-color: color-mix(", languagePicker, StringComparison.Ordinal);
        Assert.Contains("background: color-mix(", hover, StringComparison.Ordinal);
        Assert.Contains("color: var(--elsa-text)", hover, StringComparison.Ordinal);
        Assert.Contains("outline: 2px solid color-mix(", focus, StringComparison.Ordinal);
        Assert.Contains("outline-offset: 2px", focus, StringComparison.Ordinal);
    }

    private static string GetRuleBody(string css, string selector)
    {
        var ruleStart = css.IndexOf($"{selector} {{", StringComparison.Ordinal);
        Assert.True(ruleStart >= 0, $"Expected CSS rule '{selector}'.");

        var bodyStart = ruleStart + selector.Length + 2;
        var bodyEnd = css.IndexOf('}', bodyStart);
        Assert.True(bodyEnd >= 0, $"Expected CSS rule '{selector}' to have a closing brace.");
        return css[bodyStart..bodyEnd];
    }

    private static string FindRepositoryRoot()
    {
        for (var current = new DirectoryInfo(AppContext.BaseDirectory); current is not null; current = current.Parent)
        {
            if (Directory.Exists(Path.Combine(current.FullName, "src", "framework", "Elsa.Studio.Shell")))
                return current.FullName;
        }

        throw new DirectoryNotFoundException("Could not locate the Elsa Studio repository root.");
    }
}
