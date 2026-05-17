using Elsa.Studio.Workflows.Designer.Models;
using Elsa.Studio.Workflows.DiagramDesigners.StateMachines;
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
}
