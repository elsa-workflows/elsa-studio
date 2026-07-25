namespace Elsa.Studio.ExternalAuthentication.Models;

public sealed record ExternalIdentityLink(
    string Id,
    string UserId,
    string ConnectionId,
    string Issuer,
    string? SubjectHint,
    DateTimeOffset CreatedAt,
    DateTimeOffset? LastSignedInAt);

public sealed record IdentityLinkUser(string Id, string DisplayName);

public sealed record ListExternalIdentityLinksResponse(IReadOnlyCollection<ExternalIdentityLink> Items, string? NextCursor);
public sealed record FindIdentityLinkUsersResponse(IReadOnlyCollection<IdentityLinkUser> Items, string? NextCursor);

public sealed class PrelinkExternalIdentityRequest
{
    public string UserId { get; set; } = string.Empty;
    public string ConnectionId { get; set; } = string.Empty;
    public string Issuer { get; set; } = string.Empty;
    public string Subject { get; set; } = string.Empty;
}
