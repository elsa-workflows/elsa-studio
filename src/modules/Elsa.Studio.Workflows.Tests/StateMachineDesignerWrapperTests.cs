using System.Reflection;
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

        var result = InvokeGetUniqueStateName(graph, "NewOrder", selectedState);

        Assert.Equal("NewOrder2", result);
    }

    private static string InvokeGetUniqueStateName(StateMachineGraph graph, string requestedName, StateMachineStateNode excludedState)
    {
        var method = typeof(StateMachineDesignerWrapper).GetMethod("GetUniqueStateName", BindingFlags.NonPublic | BindingFlags.Static);
        Assert.NotNull(method);
        return Assert.IsType<string>(method.Invoke(null, [graph, requestedName, excludedState]));
    }
}
