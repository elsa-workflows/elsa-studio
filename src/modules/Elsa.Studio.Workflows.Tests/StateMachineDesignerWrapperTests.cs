using System.Text.Json.Nodes;
using Elsa.Studio.Workflows.DiagramDesigners;
using Elsa.Studio.Workflows.Designer.Models;
using Elsa.Studio.Workflows.DiagramDesigners.StateMachines;
using Elsa.Studio.Workflows.Domain.Models;
using Elsa.Studio.Workflows.Shared.Components;
using Elsa.Studio.Workflows.UI.Contexts;
using Elsa.Studio.Workflows.UI.Contracts;
using Microsoft.AspNetCore.Components;
using Xunit;

namespace Elsa.Studio.Workflows.Tests;

public class StateMachineDesignerWrapperTests
{
    [Fact]
    public void GetUniqueStateName_WhenRenamingToExistingStateName_ReturnsAvailableVariant()
    {
        var selectedState = new StateMachineStateNode { Name = "Pending" };
        var graph = new StateMachineGraph
        {
            States =
            {
                new StateMachineStateNode { Name = "NewOrder" },
                selectedState
            }
        };

        var result = StateMachineDesignerNames.GetUniqueStateName(graph, "NewOrder", selectedState);

        Assert.Equal("NewOrder2", result);
    }

    [Fact]
    public void GetUniqueTransitionName_IgnoresUnnamedTransitions()
    {
        var graph = new StateMachineGraph
        {
            Transitions =
            {
                new StateMachineTransitionEdge(),
                new StateMachineTransitionEdge { Name = "Approve" }
            }
        };

        var result = StateMachineDesignerNames.GetUniqueTransitionName(graph, "Approve");

        Assert.Equal("Approve2", result);
    }

    [Fact]
    public void IsTransitionSelected_WhenTransitionIdentitiesAreDuplicated_SelectsOnlyMatchingOccurrence()
    {
        var firstTransition = new StateMachineTransitionEdge { Name = "Review", From = "Pending", To = "Approved" };
        var selectedTransition = new StateMachineTransitionEdge { Name = "Review", From = "Pending", To = "Approved" };
        var graph = new StateMachineGraph
        {
            Transitions =
            {
                firstTransition,
                selectedTransition
            }
        };
        var wrapper = new StateMachineDesignerWrapper();
        var wrapperType = typeof(StateMachineDesignerWrapper);
        wrapperType.GetField("_graph", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!.SetValue(wrapper, graph);
        wrapperType.GetField("_selectedTransition", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!.SetValue(wrapper, selectedTransition);
        var method = wrapperType.GetMethod("IsTransitionSelected", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!;

        var firstSelected = (bool)method.Invoke(wrapper, [firstTransition])!;
        var secondSelected = (bool)method.Invoke(wrapper, [selectedTransition])!;

        Assert.False(firstSelected);
        Assert.True(secondSelected);
    }

    [Fact]
    public async Task OnGraphUpdated_WhenDesignerActivityCannotBeRead_DoesNotNotifyParent()
    {
        var graphUpdated = false;
        var wrapper = new DiagramDesignerWrapper();
        var wrapperType = typeof(DiagramDesignerWrapper);
        wrapperType.GetProperty(nameof(DiagramDesignerWrapper.GraphUpdated))!
            .SetValue(wrapper, EventCallback.Factory.Create(this, () => graphUpdated = true));
        wrapperType.GetField("_diagramDesigner", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!
            .SetValue(wrapper, new ThrowingDiagramDesigner());
        var method = wrapperType.GetMethod("OnGraphUpdated", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!;

        await (Task)method.Invoke(wrapper, [])!;

        Assert.False(graphUpdated);
    }

    private class ThrowingDiagramDesigner : IDiagramDesigner
    {
        public Task LoadRootActivityAsync(JsonObject activity, IDictionary<string, ActivityStats>? activityStatsMap) => Task.CompletedTask;

        public Task UpdateActivityAsync(string id, JsonObject activity) => Task.CompletedTask;

        public Task UpdateActivityStatsAsync(string id, ActivityStats stats) => Task.CompletedTask;

        public Task SelectActivityAsync(string id) => Task.CompletedTask;

        public Task<JsonObject> ReadRootActivityAsync() =>
            throw new DiagramDesignerValidationException("Cannot read the current designer activity.");

        public RenderFragment DisplayDesigner(DisplayContext context) => _ => { };
    }
}
