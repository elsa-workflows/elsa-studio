using System.Net;
using Bunit;
using Elsa.Studio.Components;
using Elsa.Studio.Extensions;
using Elsa.Studio.Localization;
using Elsa.Studio.Testing;
using Microsoft.Extensions.DependencyInjection;
using MudBlazor.Services;
using Xunit;

namespace Elsa.Studio.Core.Tests.Errors;

/// <summary>The shell's error boundary renders <see cref="Error"/> for any failure a page does not handle itself.</summary>
public sealed class ErrorTests : BunitContext, IAsyncLifetime
{
    public ErrorTests()
    {
        Services.AddMudServices();
        Services.AddSingleton<ILocalizer>(new DefaultLocalizer(new Translations([])));
    }

    Task IAsyncLifetime.InitializeAsync() => Task.CompletedTask;
    async Task IAsyncLifetime.DisposeAsync() => await base.DisposeAsync();

    [Fact]
    public void AForbiddenResponse_ShowsThePermissionGuidanceInsteadOfTheRawException()
    {
        var text = RenderError(ApiExceptions.Create(HttpStatusCode.Forbidden));

        Assert.Equal(AuthorizationFailureExtensions.ForbiddenMessage, text);
    }

    [Fact]
    public void AnyOtherFailure_StillShowsItsTypeAndMessage()
    {
        var text = RenderError(ApiExceptions.Create(HttpStatusCode.InternalServerError));

        Assert.Equal("ApiException: Response status code does not indicate success: 500 (Internal Server Error).", text);
    }

    private string RenderError(Exception exception) =>
        Render<Error>(parameters => parameters.Add(x => x.Context, exception)).Find(".mud-alert-message").TextContent.Trim();
}
