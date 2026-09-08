using Elsa.Studio.Security.Contracts;
using Elsa.Studio.Security.Models;
using Elsa.Studio.Security.Services;
using Xunit;

namespace Elsa.Studio.Security.Tests;

public sealed class IdentityPermissionSourceTests
{
    [Fact]
    public async Task GetAsync_WhenSnapshotIsReady_FlattensEffectiveGrants()
    {
        var context = new TestPermissionContext(new IdentityPermissionSnapshot(
            IdentityPermissionSnapshotState.Ready,
            new Dictionary<string, IReadOnlySet<string>>(StringComparer.Ordinal)
            {
                ["workflows/definitions"] = new HashSet<string>(["view", "execute"], StringComparer.Ordinal),
                ["dashboard"] = new HashSet<string>(["view"], StringComparer.Ordinal)
            }));
        var source = new IdentityPermissionSource(context);

        var permissions = await source.GetAsync();

        Assert.Equal(
            ["dashboard:view", "workflows/definitions:execute", "workflows/definitions:view"],
            permissions!.OrderBy(x => x, StringComparer.Ordinal));
    }

    [Theory]
    [InlineData(IdentityPermissionSnapshotState.Forbidden)]
    [InlineData(IdentityPermissionSnapshotState.Unavailable)]
    public async Task GetAsync_WhenSnapshotIsNotReady_FailsClosed(IdentityPermissionSnapshotState state)
    {
        var snapshot = new IdentityPermissionSnapshot(state, new Dictionary<string, IReadOnlySet<string>>());
        var source = new IdentityPermissionSource(new TestPermissionContext(snapshot));

        Assert.Null(await source.GetAsync());
    }

    private sealed class TestPermissionContext(IdentityPermissionSnapshot snapshot) : IIdentityPermissionContext
    {
        public Task<IdentityPermissionSnapshot> GetAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult(snapshot);

        public void Invalidate()
        {
        }
    }
}
