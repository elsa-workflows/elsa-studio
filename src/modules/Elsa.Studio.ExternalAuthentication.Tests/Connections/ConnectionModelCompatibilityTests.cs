using System.Text.Json;
using Elsa.Studio.ExternalAuthentication.Models;
using Xunit;

namespace Elsa.Studio.ExternalAuthentication.Tests.Connections;

public class ConnectionModelCompatibilityTests
{
    [Fact]
    public void Validation_warnings_preserve_the_release_contract()
    {
        const string payload = """
            {
              "valid": true,
              "errors": [],
              "warnings": [
                {
                  "field": "issuer",
                  "code": "legacy_issuer",
                  "message": "Review the configured issuer."
                }
              ]
            }
            """;

        var result = JsonSerializer.Deserialize<ConnectionValidationResult>(
            payload,
            new JsonSerializerOptions(JsonSerializerDefaults.Web));

        var warning = Assert.Single(result!.Warnings);
        Assert.Equal("issuer", warning.Field);
        Assert.Equal("legacy_issuer", warning.Code);
        Assert.Equal("Review the configured issuer.", warning.Message);
    }
}
