using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Elsa.Studio.PlatformIntegration.Contracts;
using Elsa.Studio.PlatformIntegration.Serialization;

namespace Elsa.Studio.PlatformIntegration.Services;

internal sealed class PlatformArtifactSubmitClient(HttpClient httpClient) : IPlatformArtifactSubmitClient
{
    private readonly PlatformArtifactEnvelopeValidator _validator = new();

    public async Task<PlatformSubmitResult> SubmitAsync(PlatformWorkflowSubmitPackage package, PlatformSubmitOptions options, CancellationToken cancellationToken = default)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, BuildSubmissionUri(options))
        {
            Content = JsonContent.Create(ToRegistrationRequest(package.Envelope), PlatformIntegrationJsonContext.Default.WorkspaceArtifactRegistrationRequest)
        };

        if (options.ConfigureRequestAsync is not null)
            await options.ConfigureRequestAsync(request, cancellationToken);

        using var response = await httpClient.SendAsync(request, cancellationToken);
        var message = await SafeResponseMessageAsync(response, cancellationToken);

        return response.StatusCode switch
        {
            HttpStatusCode.Created => await SubmittedAsync(response, PlatformSubmitStatus.Submitted, message, cancellationToken),
            HttpStatusCode.OK => await SubmittedAsync(response, PlatformSubmitStatus.Duplicate, message, cancellationToken),
            HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden => new PlatformSubmitResult(PlatformSubmitStatus.Unauthorized, message),
            HttpStatusCode.Conflict => new PlatformSubmitResult(PlatformSubmitStatus.Conflict, message),
            HttpStatusCode.BadRequest => new PlatformSubmitResult(PlatformSubmitStatus.ValidationFailed, message),
            _ when (int)response.StatusCode >= 500 => new PlatformSubmitResult(PlatformSubmitStatus.RetryableError, message),
            _ => new PlatformSubmitResult(PlatformSubmitStatus.ValidationFailed, message)
        };
    }

    private static Uri BuildSubmissionUri(PlatformSubmitOptions options)
    {
        if (options.PlatformEndpoint is null)
            throw new InvalidOperationException("Platform endpoint is required before submitting to Platform.");
        if (options.WorkspaceId is null || options.WorkspaceId == Guid.Empty)
            throw new InvalidOperationException("Platform workspace is required before submitting to Platform.");

        return new Uri($"{options.PlatformEndpoint.AbsoluteUri.TrimEnd('/')}/api/workspaces/{options.WorkspaceId:D}/artifacts");
    }

    private static WorkspaceArtifactRegistrationRequest ToRegistrationRequest(PlatformArtifactEnvelope envelope) =>
        new(
            envelope.ArtifactId,
            PlatformArtifactEnvelopeConstants.LayoutVersion,
            envelope.ContentDigest,
            "Unknown",
            envelope.PayloadReference.Provider,
            envelope.PayloadReference.Uri,
            new WorkspaceArtifactManifestSummary(
                envelope.DisplayMetadata.Name,
                envelope.DisplayMetadata.Version,
                null),
            [],
            envelope.Diagnostics
                .Select(x => new WorkspaceArtifactDiagnostic(x.Code, ToWorkspaceSeverity(x.Severity), x.Message))
                .ToList(),
            envelope.EnvelopeVersion,
            envelope.ArtifactTypeId,
            envelope.ArtifactSchemaVersion,
            envelope.ManifestDigest,
            envelope.PayloadReference,
            envelope.Producer,
            envelope.DisplayMetadata,
            envelope.CompatibilityHints);

    private async Task<PlatformSubmitResult> SubmittedAsync(HttpResponseMessage response, PlatformSubmitStatus status, string fallbackMessage, CancellationToken cancellationToken)
    {
        WorkspaceArtifactResponse? artifact;
        try
        {
            artifact = await response.Content.ReadFromJsonAsync(PlatformIntegrationJsonContext.Default.WorkspaceArtifactResponse, cancellationToken);
        }
        catch (Exception ex) when (ex is JsonException or NotSupportedException)
        {
            return new PlatformSubmitResult(PlatformSubmitStatus.RetryableError, "Platform submission response could not be read.");
        }

        if (artifact is null)
            return new PlatformSubmitResult(PlatformSubmitStatus.RetryableError, "Platform submission response could not be read.");

        return new PlatformSubmitResult(
            status,
            fallbackMessage,
            artifact.ArtifactId,
            $"{artifact.ContentDigest.Algorithm}:{artifact.ContentDigest.Value}",
            artifact.RegisteredAt);
    }

    private async Task<string> SafeResponseMessageAsync(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        if (response.IsSuccessStatusCode)
            return response.StatusCode == HttpStatusCode.Created ? "Submitted to Platform." : "Artifact already exists in Platform.";

        try
        {
            var problem = await response.Content.ReadFromJsonAsync(PlatformIntegrationJsonContext.Default.ProblemDetailsResponse, cancellationToken);
            return _validator.SafeMessage(problem?.Title ?? problem?.Detail ?? response.ReasonPhrase);
        }
        catch (Exception ex) when (ex is JsonException or NotSupportedException)
        {
            return _validator.SafeMessage(response.ReasonPhrase);
        }
    }

    private static string ToWorkspaceSeverity(PlatformArtifactEnvelopeDiagnosticSeverity severity) =>
        severity switch
        {
            PlatformArtifactEnvelopeDiagnosticSeverity.Error => "Error",
            PlatformArtifactEnvelopeDiagnosticSeverity.Warning => "Warning",
            _ => "Info"
        };
}
