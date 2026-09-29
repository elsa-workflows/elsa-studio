using Elsa.Studio.Authorization;

namespace Elsa.Studio.Core.Tests.Authorization;

/// <summary>Returns fixed permissions and counts how often they were requested.</summary>
internal sealed class StubPermissionService(UserPermissions permissions) : IPermissionService
{
    public StubPermissionService(params string[] grants) : this(UserPermissions.FromGrants(grants.Select(Parse)))
    {
    }

    public int Calls { get; private set; }

    public ValueTask<UserPermissions> GetPermissionsAsync(CancellationToken cancellationToken = default)
    {
        Calls++;
        return new(permissions);
    }

    private static Permission Parse(string value) => Permission.TryParse(value, out var permission) ? permission : throw new FormatException(value);
}
