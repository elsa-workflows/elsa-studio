using Xunit;

namespace Elsa.Studio.Core.Tests;

public sealed class ClassicAppBarCssContractTests
{
    private const string ClassicActionsSelector =
        ".elsa-studio-shell[data-elsa-theme=\"classic\"] .studio-appbar__actions";

    [Fact]
    public void UtilitiesUseSoftThemeScopedInteractionStates()
    {
        var layout = CssContractTestContext.ReadRepositoryFile(
            "src", "framework", "Elsa.Studio.Shared", "Layouts", "MainLayout.razor");
        var css = CssContractTestContext.ReadRepositoryFile(
            "src", "framework", "Elsa.Studio.Shell", "wwwroot", "css", "shell.css");

        Assert.Contains("studio-appbar__actions", layout, StringComparison.Ordinal);

        var neutralActions = CssContractTestContext.GetRuleBody(css, ".studio-appbar__actions");
        var actions = CssContractTestContext.GetRuleBody(css, ClassicActionsSelector);
        var languagePicker = CssContractTestContext.GetRuleBody(css, $"{ClassicActionsSelector} .mud-button-outlined");
        var hover = CssContractTestContext.GetRuleBody(
            css,
            $"{ClassicActionsSelector} :is(.mud-icon-button, .mud-button-root):not(.mud-disabled):not([disabled]):hover");
        var focus = CssContractTestContext.GetRuleBody(
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
}
