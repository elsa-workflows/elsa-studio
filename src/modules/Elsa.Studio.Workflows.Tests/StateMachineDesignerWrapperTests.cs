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
}
