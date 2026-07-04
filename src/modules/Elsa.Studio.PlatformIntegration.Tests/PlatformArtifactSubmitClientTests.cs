using System.Net;
using System.Text.Json;
using Elsa.Studio.PlatformIntegration.Services;
using Xunit;

namespace Elsa.Studio.PlatformIntegration.Tests;

public sealed class PlatformArtifactSubmitClientTests
{
    private readonly PlatformSubmitOptions _options = new()
    {
        PlatformEndpoint = new Uri("https://platform.example.test"),
        WorkspaceId = Guid.Parse("10000000-0000-0000-0000-000000000001")
    };

    [Fact]
    public async Task Posts_artifact_registration_to_workspace_endpoint()
    {
        var handler = new RecordingHandler(HttpStatusCode.Created, """
            {
              "id": "20000000-0000-0000-0000-000000000001",
              "artifactId": "elsa.workflow-definition:payment-retry:abc",
              "contentDigest": { "algorithm": "sha256", "value": "abc" },
              "registeredAt": "2026-05-29T08:00:00Z"
            }
            """);
        var client = new PlatformArtifactSubmitClient(new HttpClient(handler));

        var result = await client.SubmitAsync(Package(), _options);

        Assert.Equal(PlatformSubmitStatus.Submitted, result.Status);
        Assert.Equal("elsa.workflow-definition:payment-retry:abc", result.ArtifactId);
        Assert.Equal("sha256:abc", result.ArtifactDigest);
        Assert.Equal(new Uri("https://platform.example.test/api/workspaces/10000000-0000-0000-0000-000000000001/artifacts"), handler.RequestUri);
        Assert.NotNull(handler.RequestBody);

        using var document = JsonDocument.Parse(handler.RequestBody);
        Assert.Equal("elsa.workflow-definition", document.RootElement.GetProperty("artifactTypeId").GetString());
        Assert.Equal("Unknown", document.RootElement.GetProperty("format").GetString());
        Assert.Equal(JsonValueKind.Null, document.RootElement.GetProperty("manifest").GetProperty("environment").ValueKind);
        Assert.Equal("producer-managed", document.RootElement.GetProperty("payloadReference").GetProperty("provider").GetString());
        Assert.DoesNotContain("WorkflowDefinitionJson", handler.RequestBody);
    }

    [Theory]
    [InlineData(HttpStatusCode.Conflict, PlatformSubmitStatus.Conflict)]
    [InlineData(HttpStatusCode.BadRequest, PlatformSubmitStatus.ValidationFailed)]
    [InlineData(HttpStatusCode.Unauthorized, PlatformSubmitStatus.Unauthorized)]
    [InlineData(HttpStatusCode.InternalServerError, PlatformSubmitStatus.RetryableError)]
    public async Task Maps_platform_responses_to_safe_submit_states(HttpStatusCode statusCode, PlatformSubmitStatus expectedStatus)
    {
        var handler = new RecordingHandler(statusCode, """{"title":"Bearer token rejected"}""");
        var client = new PlatformArtifactSubmitClient(new HttpClient(handler));

        var result = await client.SubmitAsync(Package(), _options);

        Assert.Equal(expectedStatus, result.Status);
        Assert.DoesNotContain("Bearer", result.Message);
        Assert.Contains("[redacted]", result.Message);
    }

    [Fact]
    public async Task Treats_duplicate_platform_response_as_success()
    {
        var handler = new RecordingHandler(HttpStatusCode.OK, """
            {
              "id": "20000000-0000-0000-0000-000000000001",
              "artifactId": "elsa.workflow-definition:payment-retry:abc",
              "contentDigest": { "algorithm": "sha256", "value": "abc" },
              "registeredAt": "2026-05-29T08:00:00Z"
            }
            """);
        var client = new PlatformArtifactSubmitClient(new HttpClient(handler));

        var result = await client.SubmitAsync(Package(), _options);

        Assert.Equal(PlatformSubmitStatus.Duplicate, result.Status);
        Assert.True(result.Succeeded);
        Assert.Equal("Artifact already exists in Platform.", result.Message);
    }

    private static PlatformWorkflowSubmitPackage Package() =>
        new(
            new PlatformArtifactEnvelope(
                "elsa.workflow-definition:payment-retry:abc",
                PlatformArtifactEnvelopeConstants.EnvelopeVersion,
                PlatformArtifactEnvelopeConstants.ElsaWorkflowDefinitionArtifactType,
                PlatformArtifactEnvelopeConstants.DefaultArtifactSchemaVersion,
                new PlatformArtifactDigest("sha256", new string('a', 64)),
                null,
                new PlatformArtifactPayloadReference("producer-managed", "studio://workflows/payment-retry/snapshots/abc"),
                new PlatformArtifactProducer("studio", "Elsa Studio"),
                new PlatformArtifactDisplayMetadata("Payment Retry", "42", null, new Dictionary<string, string>(), new Dictionary<string, string>(), "studio://workflows/payment-retry"),
                [],
                []),
            "{}",
            DateTimeOffset.UtcNow);

    private sealed class RecordingHandler(HttpStatusCode statusCode, string content, string contentType = "application/json") : HttpMessageHandler
    {
        public Uri? RequestUri { get; private set; }

        public string? RequestBody { get; private set; }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            RequestUri = request.RequestUri;
            RequestBody = request.Content is null ? null : await request.Content.ReadAsStringAsync(cancellationToken);
            return new HttpResponseMessage(statusCode)
            {
                Content = new StringContent(content, System.Text.Encoding.UTF8, contentType)
            };
        }
    }
}
