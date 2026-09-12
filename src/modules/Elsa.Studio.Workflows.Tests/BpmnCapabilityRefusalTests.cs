using Elsa.Studio.Workflows.Domain.Extensions;
using Elsa.Studio.Workflows.Domain.Models.Bpmn;
using Xunit;

namespace Elsa.Studio.Workflows.Tests;

/// <summary>
/// Covers how a <see cref="BpmnCapabilityRefusal"/> is built: from the coded error envelope's <c>data.capabilities</c>
/// and <c>data.elementIds</c>, through <see cref="ValidationApiExceptionExtensions.GetValidationErrorsFromContent"/>,
/// rather than by parsing the human-readable message (pinned by
/// <see cref="RemoteBpmnInterchangeServiceTests.ImportAsync_ReturnsTheCapabilityRefusalMessage_On422"/>, which a
/// server may reword without notice).
/// </summary>
public class BpmnCapabilityRefusalTests
{
    [Fact]
    public void GetValidationErrorsFromContent_ExtractsCapabilityNamesAndElementIds_FromTheCodedEnvelope()
    {
        const string content = $$"""
        {
          "errors": { "generalErrors": ["This deployment does not declare a required capability."] },
          "code": "{{BpmnErrorCodes.ImportCapabilityUnsupported}}",
          "data": { "capabilities": ["ScopeSignalling"], "elementIds": ["Gateway_1"] }
        }
        """;

        var errors = ValidationApiExceptionExtensions.GetValidationErrorsFromContent(content);

        Assert.NotNull(errors!.Data);
        Assert.Equal(["ScopeSignalling"], errors.Data!.CapabilityNames);
        Assert.Equal(["Gateway_1"], errors.Data.ElementIds);
    }

    [Fact]
    public void GetValidationErrorsFromContent_ExtractsMultipleCapabilityNamesAndElementIds()
    {
        const string content = $$"""
        {
          "errors": { "generalErrors": ["This deployment does not declare two required capabilities."] },
          "code": "{{BpmnErrorCodes.ImportCapabilityUnsupported}}",
          "data": { "capabilities": ["ScopeSignalling", "CompensationHandling"], "elementIds": ["Gateway_1", "Task_2"] }
        }
        """;

        var errors = ValidationApiExceptionExtensions.GetValidationErrorsFromContent(content);

        Assert.NotNull(errors!.Data);
        Assert.Equal(["ScopeSignalling", "CompensationHandling"], errors.Data!.CapabilityNames);
        Assert.Equal(["Gateway_1", "Task_2"], errors.Data.ElementIds);
    }

    [Theory]
    [InlineData(BpmnErrorCodes.ExportNotImported)]
    [InlineData(BpmnErrorCodes.ImportBindingInvalid)]
    public void GetValidationErrorsFromContent_LeavesDataNull_ForCodesThatCarryNone(string code)
    {
        var content = $$"""
        {
          "errors": { "generalErrors": ["Some refusal."] },
          "code": "{{code}}"
        }
        """;

        var errors = ValidationApiExceptionExtensions.GetValidationErrorsFromContent(content);

        Assert.Null(errors!.Data);
    }

    [Fact]
    public void GetValidationErrorsFromContent_LeavesCodeAndDataNull_ForABodyWithNoCode()
    {
        const string content = """
        {
          "errors": { "generalErrors": ["This deployment does not declare the following BPMN host capabilities the document requires: ScopeSignalling."] }
        }
        """;

        var errors = ValidationApiExceptionExtensions.GetValidationErrorsFromContent(content);

        Assert.Null(errors!.Code);
        Assert.Null(errors.Data);
    }
}
