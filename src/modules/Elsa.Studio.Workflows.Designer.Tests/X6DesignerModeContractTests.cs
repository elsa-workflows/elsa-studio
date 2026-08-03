using Xunit;

namespace Elsa.Studio.Workflows.Designer.Tests;

public sealed class X6DesignerModeContractTests
{
    [Fact]
    public void GraphKernel_ResolvesStateMachineWithoutFallingBackToFlowchart()
    {
        var modes = ReadAsset("designer-mode.ts");
        var graphCreation = ReadAsset("create-graph.ts");
        var bindings = ReadAsset("graph-bindings.ts");

        Assert.Contains("'stateMachine'", modes, StringComparison.Ordinal);
        Assert.Contains("resolveDesignerMode(settings?.mode)", graphCreation, StringComparison.Ordinal);
        Assert.Contains("getDesignerModePolicy(mode)", graphCreation, StringComparison.Ordinal);
        Assert.Contains("DesignerMode", bindings, StringComparison.Ordinal);
    }

    [Fact]
    public void GraphReads_UseModeSpecificPersistentCellShapes()
    {
        var graphReader = ReadAsset("read-graph.ts");
        var modes = ReadAsset("designer-mode.ts");

        Assert.Contains("isPersistentDesignerCell(cell, mode)", graphReader, StringComparison.Ordinal);
        Assert.Contains("StateMachineStateShape", modes, StringComparison.Ordinal);
        Assert.Contains("StateMachineTransitionShape", modes, StringComparison.Ordinal);
        Assert.Contains("hasConnectedEndpoints", modes, StringComparison.Ordinal);
    }

    [Fact]
    public void StateMachineShapes_AreRegisteredIndependentlyFromActivityShapes()
    {
        var initialization = ReadAsset("init.ts");
        var shapes = ReadAsset("state-machine-shapes.ts");

        Assert.Contains("registerStateMachineShapes()", initialization, StringComparison.Ordinal);
        Assert.Contains("Graph.registerNode", shapes, StringComparison.Ordinal);
        Assert.Contains("StateMachineStateShape", shapes, StringComparison.Ordinal);
        Assert.Contains("Graph.registerEdge", shapes, StringComparison.Ordinal);
        Assert.Contains("StateMachineTransitionShape", shapes, StringComparison.Ordinal);
    }

    [Fact]
    public void StateMachineMode_DoesNotInvokeActivityOnlyInteractionsOrSizing()
    {
        var graphCreation = ReadAsset("create-graph.ts");

        Assert.Contains("modePolicy.usesActivityInteractions", graphCreation, StringComparison.Ordinal);
        Assert.Contains("modePolicy.enforcesActivityMinimumSize", graphCreation, StringComparison.Ordinal);
    }

    [Fact]
    public void StateMachineNodes_ExposeKeyboardAccessibleSelection()
    {
        var graphCreation = ReadAsset("create-graph.ts");
        var accessibility = ReadAsset("state-machine-accessibility.ts");

        Assert.Contains("requestAnimationFrame(() => applyStateMachineNodeAccessibility(graph, node))", graphCreation, StringComparison.Ordinal);
        Assert.Contains("container.setAttribute('role', 'button')", accessibility, StringComparison.Ordinal);
        Assert.Contains("container.setAttribute('aria-label', accessibleName)", accessibility, StringComparison.Ordinal);
        Assert.Contains("container.setAttribute('tabindex', '0')", accessibility, StringComparison.Ordinal);
        Assert.Contains("graph.select(node)", accessibility, StringComparison.Ordinal);
    }

    [Fact]
    public void GraphLifecycle_DisposesX6AndCancelsDeferredCanvasWork()
    {
        var disposal = ReadAsset("dispose-graph.ts");
        var loading = ReadAsset("load-graph.ts");

        Assert.Contains("binding.graph.dispose()", disposal, StringComparison.Ordinal);
        Assert.True(
            disposal.IndexOf("binding.graph.dispose()", StringComparison.Ordinal) <
            disposal.IndexOf("delete graphBindings[graphId]", StringComparison.Ordinal));
        Assert.Contains("!container.isConnected", loading, StringComparison.Ordinal);
        Assert.Contains("graphBindings[graphId]?.graph !== graph", loading, StringComparison.Ordinal);
        Assert.Contains("graphBindings[graphId] !== binding", loading, StringComparison.Ordinal);
    }

    private static string ReadAsset(string name) =>
        File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "DesignerAssets", name));
}
