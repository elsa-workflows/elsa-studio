using Xunit;

namespace Elsa.Studio.Workflows.Tests.StateMachines;

public sealed class StateMachineCanvasLifecycleContractTests
{
    [Fact]
    public void Wrapper_RemountsForReadOnlyChangesAndCarriesSelectionAcrossViews()
    {
        var wrapper = ReadAsset("StateMachineDesignerWrapper.razor");
        var canvas = ReadAsset("StateMachineCanvas.razor.cs");

        Assert.Contains("@key=\"IsReadOnly\"", wrapper, StringComparison.Ordinal);
        Assert.Contains("SelectedVisualId=\"@(_selectedStateId ?? _selectedTransitionId)\"", wrapper, StringComparison.Ordinal);
        Assert.Contains("[Parameter] public string? SelectedVisualId", canvas, StringComparison.Ordinal);
        Assert.Contains("await _graphApi.SelectCellAsync(SelectedVisualId)", canvas, StringComparison.Ordinal);
    }

    [Fact]
    public void ProgrammaticSelection_DoesNotMasqueradeAsUserSelection()
    {
        var bindings = ReadAsset("graph-bindings.ts");
        var selection = ReadAsset("select-cell.ts");
        var graph = ReadAsset("create-graph.ts");

        Assert.Contains("suppressSelectionCallbacks?: number", bindings, StringComparison.Ordinal);
        Assert.Contains("binding.suppressSelectionCallbacks =", selection, StringComparison.Ordinal);
        Assert.Contains("graph.resetSelection(cell)", selection, StringComparison.Ordinal);
        Assert.Contains("binding.suppressSelectionCallbacks--", selection, StringComparison.Ordinal);
        Assert.Contains("graphBindings[graphId]?.suppressSelectionCallbacks", graph, StringComparison.Ordinal);
    }

    [Fact]
    public void ReadOnlyMode_DoesNotExposeOrExecuteAutoLayout()
    {
        var wrapper = ReadAsset("StateMachineDesignerWrapper.razor");
        var canvas = ReadAsset("StateMachineCanvas.razor.cs");

        Assert.Contains("@if (!_showOutline && !IsReadOnly)", wrapper, StringComparison.Ordinal);
        Assert.Contains("public Task AutoLayoutAsync() => IsReadOnly", canvas, StringComparison.Ordinal);
    }

    [Fact]
    public void TransitionInspector_UsesSemanticPickerCallbacksAndExcludesConditionFromActivityDrops()
    {
        var wrapper = ReadAsset("StateMachineDesignerWrapper.razor");

        Assert.Contains("ActivityAddRequested=\"AddTransitionActivityAsync\"", wrapper, StringComparison.Ordinal);
        Assert.Contains("ActivityOpenRequested=\"OpenTransitionActivityAsync\"", wrapper, StringComparison.Ordinal);
        Assert.Contains("ActivityReplaceRequested=\"ReplaceTransitionActivityAsync\"", wrapper, StringComparison.Ordinal);
        Assert.Contains("ActivityClearRequested=\"ClearTransitionSlotAsync\"", wrapper, StringComparison.Ordinal);
        Assert.Contains("ActivityDropRequested=\"OnTransitionSlotDropAsync\"", wrapper, StringComparison.Ordinal);
    }

    private static string ReadAsset(string name) =>
        File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "DesignerAssets", name));
}
