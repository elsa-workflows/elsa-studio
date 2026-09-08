using System.Text.Json.Nodes;
using Elsa.Api.Client.Resources.ActivityDescriptors.Enums;
using Elsa.Api.Client.Resources.ActivityDescriptors.Models;
using Elsa.Studio.ActivityPortProviders.Providers;
using Elsa.Studio.Workflows.Domain.Contexts;
using Elsa.Studio.Workflows.Domain.Models.Bpmn;
using Elsa.Studio.Workflows.Domain.Providers;
using Elsa.Studio.Workflows.Domain.Services;
using Xunit;

namespace Elsa.Studio.Workflows.Tests;

/// <summary>
/// Covers the outcome ports an <c>Elsa.BpmnProcess</c> node offers on a flowchart.
/// </summary>
/// <remarks>
/// elsa-core's <c>BpmnProcess</c> declares no <c>[FlowNode]</c> outcomes, so its descriptor carries no ports and the
/// flowchart mapper would synthesize the single default port it adds for any activity with none. That fallback is the
/// case that looks like success: its id is literally <c>Done</c>, so the happy path connects and runs, and the
/// missing piece -- a <c>Cancelled</c> port, which a scope completes with when a cancel end event cancelled a
/// transaction -- is invisible until a document needs it. These tests pin both names, and pin that the provider
/// really is the one the service picks, since a provider that never wins would leave the fallback in place while
/// looking installed.
/// </remarks>
public class BpmnProcessPortProviderTests
{
    private readonly DefaultActivityPortService _portService = new([new BpmnProcessPortProvider(), new DefaultActivityPortProvider()]);

    [Fact]
    public void GetProvider_PicksTheBpmnProvider_ForABpmnProcess()
    {
        var provider = _portService.GetProvider(CreateContext(BpmnProcessConstants.ActivityTypeName));

        Assert.IsType<BpmnProcessPortProvider>(provider);
    }

    [Fact]
    public void GetProvider_LeavesEveryOtherActivityToTheDefaultProvider()
    {
        var provider = _portService.GetProvider(CreateContext("Elsa.WriteLine"));

        Assert.IsType<DefaultActivityPortProvider>(provider);
    }

    [Fact]
    public void GetPorts_YieldsTheInterpretersOwnCompletionOutcomes()
    {
        var ports = _portService.GetPorts(CreateContext(BpmnProcessConstants.ActivityTypeName)).ToList();

        Assert.Equal(["Done", "Cancelled"], ports.Select(x => x.Name));
        Assert.Equal(["Done", "Cancelled"], ports.Select(x => x.DisplayName));
        Assert.All(ports, port => Assert.Equal(PortType.Flow, port.Type));
    }

    /// <summary>
    /// The mapper only synthesizes its default port when the activity offers no flow port of its own, so having flow
    /// ports here is what keeps a BPMN node off the fallback. A connection drawn from a fallback port would still
    /// carry the name <c>Done</c> and still fire, which is exactly why this has to be asserted rather than observed.
    /// </summary>
    [Fact]
    public void GetPorts_AreFlowPorts_SoTheFlowchartMapperDoesNotFallBackToItsDefaultPort()
    {
        var ports = _portService.GetPorts(CreateContext(BpmnProcessConstants.ActivityTypeName)).ToList();

        Assert.Contains(ports, port => port.Type == PortType.Flow);
    }

    /// <summary>
    /// A BPMN process never completes with <c>Outcomes.Default</c>, whose null name is what makes an ordinary
    /// activity's null-port connection fire. Every port here must therefore be named.
    /// </summary>
    [Fact]
    public void GetPorts_AreAllNamed_SoNoConnectionCanRelyOnTheNullPortShorthand()
    {
        var ports = _portService.GetPorts(CreateContext(BpmnProcessConstants.ActivityTypeName)).ToList();

        Assert.All(ports, port => Assert.False(string.IsNullOrEmpty(port.Name)));
    }

    /// <summary>
    /// A provider wins outright -- the default one is not consulted as well -- so anything the descriptor declares
    /// has to be carried through here or it disappears from the node with nothing to say why. An embedded port that
    /// stopped rendering is exactly the kind of loss that shows up as a missing feature rather than an error.
    /// </summary>
    [Fact]
    public void GetPorts_CarriesThroughWhateverTheDescriptorAlreadyDeclares()
    {
        var declared = new Port { Name = "Body", DisplayName = "Body", Type = PortType.Embedded, IsBrowsable = true };
        var context = CreateContext(BpmnProcessConstants.ActivityTypeName, declared);

        var ports = _portService.GetPorts(context).ToList();

        Assert.Equal(["Body", "Done", "Cancelled"], ports.Select(x => x.Name));
        Assert.Same(declared, ports[0]);
    }

    /// <summary>
    /// The day elsa-core declares the outcomes itself this provider becomes a no-op superset rather than a source of
    /// two identical ports on the node.
    /// </summary>
    [Fact]
    public void GetPorts_DoesNotRepeatAnOutcomeTheDescriptorAlreadyDeclares()
    {
        var declared = new Port { Name = "Done", DisplayName = "Done", Type = PortType.Flow };
        var context = CreateContext(BpmnProcessConstants.ActivityTypeName, declared);

        var ports = _portService.GetPorts(context).ToList();

        Assert.Equal(["Done", "Cancelled"], ports.Select(x => x.Name));
    }

    private static PortProviderContext CreateContext(string activityTypeName, params Port[] declaredPorts) =>
        new(new ActivityDescriptor { TypeName = activityTypeName, Name = activityTypeName, Version = 1, Ports = declaredPorts },
            new JsonObject { ["id"] = "activity-1", ["type"] = activityTypeName, ["version"] = 1 });
}
