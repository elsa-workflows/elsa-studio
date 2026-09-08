using Elsa.Studio.Workflows.Domain.Models.Bpmn;
using Xunit;

namespace Elsa.Studio.Workflows.Tests;

/// <summary>
/// Covers <see cref="BpmnCapabilityRefusal.TryParse"/>: it must recognize the exact sentence
/// <c>BpmnInterchangeDocumentService.Import</c> raises for a missing-host-capability refusal (pinned by
/// <see cref="RemoteBpmnInterchangeServiceTests.ImportAsync_ReturnsTheCapabilityRefusalMessage_On422"/>), and must
/// not misclassify the two differently-worded export refusals or unrelated messages as the same thing.
/// </summary>
public class BpmnCapabilityRefusalTests
{
    [Fact]
    public void TryParse_ExtractsCapabilityNamesAndElementIds_FromTheKnownRefusalMessage()
    {
        const string message =
            "This deployment does not declare the following BPMN host capabilities the document requires: ScopeSignalling. "
            + "Offending elements (combined across all missing capabilities above, not attributable to any one of them): Gateway_1.";

        var refusal = BpmnCapabilityRefusal.TryParse(message);

        Assert.NotNull(refusal);
        Assert.Equal(["ScopeSignalling"], refusal!.CapabilityNames);
        Assert.Equal(["Gateway_1"], refusal.ElementIds);
    }

    [Fact]
    public void TryParse_ExtractsMultipleCapabilityNamesAndElementIds()
    {
        const string message =
            "This deployment does not declare the following BPMN host capabilities the document requires: ScopeSignalling, CompensationHandling. "
            + "Offending elements (combined across all missing capabilities above, not attributable to any one of them): Gateway_1, Task_2.";

        var refusal = BpmnCapabilityRefusal.TryParse(message);

        Assert.NotNull(refusal);
        Assert.Equal(["ScopeSignalling", "CompensationHandling"], refusal!.CapabilityNames);
        Assert.Equal(["Gateway_1", "Task_2"], refusal.ElementIds);
    }

    [Theory]
    [InlineData("Workflow definition 'wf-1' does not currently carry BPMN source, so it cannot be exported as BPMN 2.0 XML.")]
    [InlineData("Workflow definition 'wf-1' has changed since it was imported from BPMN (imported at version 1, currently at version 2).")]
    [InlineData("Upload exactly one .bpmn file.")]
    public void TryParse_ReturnsNull_ForMessagesThatAreNotTheCapabilityRefusal(string message)
    {
        Assert.Null(BpmnCapabilityRefusal.TryParse(message));
    }
}
