using System.Text.Json.Nodes;
using Elsa.Studio.Localization;
using Elsa.Studio.Workflows.DiagramDesigners.StateMachines;
using Microsoft.Extensions.Localization;
using Xunit;

namespace Elsa.Studio.Workflows.Tests;

public class StateMachineDiagramDesignerProviderTests
{
    private readonly StateMachineDiagramDesignerProvider _provider = new(new TestLocalizer());

    [Fact]
    public void GetSupportsActivity_ReturnsTrueForStateMachineActivity()
    {
        var activity = CreateActivity("Elsa.StateMachine");

        var result = _provider.GetSupportsActivity(activity);

        Assert.True(result);
    }

    [Theory]
    [InlineData("Elsa.Flowchart")]
    [InlineData("Elsa.WriteLine")]
    public void GetSupportsActivity_ReturnsFalseForNonStateMachineActivities(string type)
    {
        var activity = CreateActivity(type);

        var result = _provider.GetSupportsActivity(activity);

        Assert.False(result);
    }

    [Fact]
    public void GetEditor_ReturnsStateMachineDesigner()
    {
        var editor = _provider.GetEditor();

        Assert.IsType<StateMachineDiagramDesigner>(editor);
    }

    private static JsonObject CreateActivity(string type) => new()
    {
        ["type"] = type
    };

    private class TestLocalizer : ILocalizer
    {
        public LocalizedString this[string key] => new(key, key);
        public LocalizedString this[string key, params object[] arguments] => new(key, string.Format(key, arguments));
    }
}
