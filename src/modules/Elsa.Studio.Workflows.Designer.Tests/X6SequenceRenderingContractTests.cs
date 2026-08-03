using Xunit;

namespace Elsa.Studio.Workflows.Designer.Tests;

public sealed class X6SequenceRenderingContractTests
{
    [Fact]
    public void SequenceLayout_MutationsNotifyX6SoNodesAndEdgesAreRedrawn()
    {
        var sequenceMode = ReadAsset("sequence-mode.ts");
        var graphCreation = ReadAsset("create-graph.ts");

        Assert.Contains("graph.batchUpdate('sequence-layout'", sequenceMode, StringComparison.Ordinal);
        Assert.Contains("sequenceLayout: true", sequenceMode, StringComparison.Ordinal);
        Assert.DoesNotContain("silent: true", sequenceMode, StringComparison.Ordinal);
        Assert.Contains("args.options?.sequenceLayout", graphCreation, StringComparison.Ordinal);
    }

    private static string ReadAsset(string name) =>
        File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "DesignerAssets", name));
}
