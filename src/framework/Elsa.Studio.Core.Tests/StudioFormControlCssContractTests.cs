using Xunit;

namespace Elsa.Studio.Core.Tests;

public sealed class StudioFormControlCssContractTests
{
    private static readonly string[] ControlTokens =
    [
        "--elsa-control-surface",
        "--elsa-control-surface-hover",
        "--elsa-control-surface-disabled",
        "--elsa-control-border",
        "--elsa-control-border-hover",
        "--elsa-control-focus",
        "--elsa-control-error",
        "--elsa-control-radius"
    ];

    [Fact]
    public void ShellDefinesThemeAwareControlTokensAndAllMudInputVariants()
    {
        var css = ReadRepositoryFile(
            "src", "framework", "Elsa.Studio.Shell", "wwwroot", "css", "shell.css");

        foreach (var token in ControlTokens)
            Assert.Contains($"{token}:", css, StringComparison.Ordinal);

        Assert.Contains(".mud-input.mud-input-text", css, StringComparison.Ordinal);
        Assert.Contains(".mud-input.mud-input-filled", css, StringComparison.Ordinal);
        Assert.Contains(".mud-input.mud-input-outlined", css, StringComparison.Ordinal);
        Assert.Contains(".mud-input.mud-input-underline::before", css, StringComparison.Ordinal);
        Assert.Contains("border-block-end: 0 !important", css, StringComparison.Ordinal);
        Assert.Contains("var(--mud-palette-primary)", css, StringComparison.Ordinal);
        Assert.Contains("var(--mud-palette-error)", css, StringComparison.Ordinal);
    }

    private static string ReadRepositoryFile(params string[] pathSegments) =>
        File.ReadAllText(Path.Combine([FindRepositoryRoot(), .. pathSegments]));

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
