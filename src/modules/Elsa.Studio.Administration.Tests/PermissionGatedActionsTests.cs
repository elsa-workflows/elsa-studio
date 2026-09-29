using System.Diagnostics.CodeAnalysis;
using Bunit;
using Elsa.Studio.Authorization;
using Elsa.Studio.Contracts;
using Elsa.Studio.Secrets.Client;
using Elsa.Studio.Secrets.Models;
using Elsa.Studio.Testing;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using MudBlazor;
using MudBlazor.Services;
using Xunit;
using SecretPage = Elsa.Studio.Secrets.Pages.Secret;
using SecretsPage = Elsa.Studio.Secrets.Pages.Secrets;

namespace Elsa.Studio.Administration.Tests;

/// <summary>A user who can only view a module's records does not get its write actions.</summary>
public sealed class PermissionGatedActionsTests : BunitContext, IAsyncLifetime
{
    private static readonly SecretModel ApiKey = new() { Id = "1", Name = "api-key", DisplayName = "API key", TypeName = "Text", StoreName = "Database" };

    public PermissionGatedActionsTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddMudServices();
        Services.AddSingleton<IBackendApiClientProvider>(new ApiProvider());
        Render<MudPopoverProvider>();
    }

    Task IAsyncLifetime.InitializeAsync() => Task.CompletedTask;
    async Task IAsyncLifetime.DisposeAsync() => await base.DisposeAsync();

    [Fact]
    public void SecretsList_ViewOnly_HidesCreateAndRowActions()
    {
        var cut = RenderPage<SecretsPage>(["secrets:view"]);

        cut.WaitForAssertion(() => Assert.Contains(ApiKey.DisplayName, cut.Markup));
        Assert.DoesNotContain("Create Secret", cut.Markup);
        Assert.Empty(cut.FindAll("tbody .mud-menu"));
    }

    [Fact]
    public void SecretsList_WithWriteAccess_ShowsCreateAndRowActions()
    {
        var cut = RenderPage<SecretsPage>(["secrets:*"]);

        cut.WaitForAssertion(() => Assert.Contains(ApiKey.DisplayName, cut.Markup));
        Assert.Contains("Create Secret", cut.Markup);
        Assert.Single(cut.FindAll("tbody .mud-menu"));
    }

    [Fact]
    public void Secret_ViewOnly_HidesEditingRevocationAndRotation()
    {
        var cut = RenderPage<SecretPage>(["secrets:view"], parameters => parameters.Add(x => x.Name, ApiKey.Name));

        cut.WaitForAssertion(() => Assert.Contains("Secret details", cut.Markup));
        Assert.DoesNotContain("Edit details", cut.Markup);
        Assert.DoesNotContain("Revoke", cut.Markup);
        Assert.DoesNotContain("Rotation", cut.Markup);
    }

    [Fact]
    public void Secret_WithWriteAccess_ShowsEditingRevocationAndRotation()
    {
        var cut = RenderPage<SecretPage>(["secrets:view", "secrets:write"], parameters => parameters.Add(x => x.Name, ApiKey.Name));

        cut.WaitForAssertion(() => Assert.Contains("Edit details", cut.Markup));
        Assert.Contains("Revoke", cut.Markup);
        Assert.Contains("Rotation", cut.Markup);
    }

    // The shell's page guard cascades the user's permissions to the page.
    private IRenderedComponent<TPage> RenderPage<TPage>(string[] grants, Action<ComponentParameterCollectionBuilder<TPage>>? parameters = null) where TPage : IComponent =>
        Render<TPage>(builder =>
        {
            builder.AddCascadingValue(StubPermissionService.Grants(grants));
            parameters?.Invoke(builder);
        });

    private sealed class ApiProvider : IBackendApiClientProvider
    {
        private readonly object[] _apis = [new SecretsApi()];

        public Uri Url => new("https://elsa.example.test");

        public ValueTask<T> GetApiAsync<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] T>(CancellationToken cancellationToken = default) where T : class =>
            new(_apis.OfType<T>().Single());
    }

    private sealed class SecretsApi : ISecretsApi
    {
        public Task<ListSecretsResponse> ListAsync(string? search = null, string? typeName = null, string? storeName = null, string? scope = null, SecretStatus? status = null, int? page = null, int? pageSize = null, CancellationToken cancellationToken = default) =>
            Task.FromResult(new ListSecretsResponse { Items = [ApiKey], TotalCount = 1 });

        public Task<SecretModel> GetAsync(string name, CancellationToken cancellationToken = default) => Task.FromResult(ApiKey);
        public Task<SecretDescriptorsResponse> GetDescriptorsAsync(CancellationToken cancellationToken = default) => Task.FromResult(new SecretDescriptorsResponse());
        public Task<SecretModel> CreateAsync(CreateSecretRequest request, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<SecretModel> UpdateAsync(string name, UpdateSecretRequest request, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<SecretModel> RotateAsync(string name, RotateSecretRequest request, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<SecretModel> RevokeAsync(string name, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task DeleteAsync(string name, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<SecretTestResponse> TestAsync(string name, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<SecretPickerResponse> PickAsync(SecretPickerRequest request, CancellationToken cancellationToken = default) => throw new NotSupportedException();
    }
}
