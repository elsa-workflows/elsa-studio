using Elsa.Studio.Authorization;
using Elsa.Studio.Testing;
using Xunit;

namespace Elsa.Studio.Core.Tests.Authorization;

public class UserPermissionsTests
{
    private static readonly Permission[] DashboardOrInstances = [new("dashboard", PermissionVerbs.View), new("workflows/instances", PermissionVerbs.View)];

    [Theory]
    [InlineData("workflows/instances:view", true)]
    [InlineData("workflows/*:view", true)]
    [InlineData("dashboard:view", true)]
    [InlineData("secrets:view", false)]
    [InlineData(null, true)]
    public void HasAny_PassesWhenOneOfTheRequiredPermissionsIsHeld(string? grant, bool expected)
    {
        var permissions = grant == null ? UserPermissions.Unknown : StubPermissionService.Grants(grant);

        Assert.Equal(expected, permissions.HasAny(DashboardOrInstances));
    }
}
