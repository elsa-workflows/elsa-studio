using System.Net;
using Bunit;
using Elsa.Studio.Components;
using Elsa.Studio.Contracts;
using Elsa.Studio.Extensions;
using Elsa.Studio.Layouts;
using Elsa.Studio.Localization;
using Elsa.Studio.Testing;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using MudBlazor.Services;
using Xunit;
using DefaultBrandingProvider = Elsa.Studio.Branding.DefaultBrandingProvider;
using IBrandingProvider = Elsa.Studio.Branding.IBrandingProvider;

namespace Elsa.Studio.Core.Tests.Errors;

/// <summary>
/// A page failure the shell cannot recover from is shown by the error boundary. When the failure is the backend
/// refusing the user's sign-in, the boundary hands over to the sign-in component instead of only saying so.
/// </summary>
public sealed class MainLayoutErrorBoundaryTests : BunitContext, IAsyncLifetime
{
    public MainLayoutErrorBoundaryTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddMudServices();
        Services.AddCoreInternal();
        Services.AddSingleton<ILocalizer>(new DefaultLocalizer(new StubTranslations([])));
        Services.AddSingleton<IFeatureService, NoFeatures>();
        Services.AddSingleton<IBrandingProvider, DefaultBrandingProvider>();
        Services.AddSingleton<IUnauthorizedComponentProvider>(new MarkerUnauthorizedProvider());
        Services.AddSingleton<IErrorComponentProvider>(new MarkerErrorProvider());
        ComponentFactories.AddStub<NavMenu>();
        ComponentFactories.AddStub<PermissionPageGuard>(parameters => parameters.Get(x => x.ChildContent)!);
    }

    Task IAsyncLifetime.InitializeAsync() => Task.CompletedTask;
    async Task IAsyncLifetime.DisposeAsync() => await base.DisposeAsync();

    [Theory]
    [MemberData(nameof(SignInFailures))]
    public void ALostSignIn_HandsOverToTheSignInComponent(Exception failure)
    {
        var cut = RenderLayoutWith(failure);

        Assert.NotNull(cut.Find("#sign-in-redirect"));
        Assert.Empty(cut.FindAll("#error-display"));
    }

    [Theory]
    [MemberData(nameof(OtherFailures))]
    public void AnyOtherFailure_IsDisplayedInsteadOfRedirecting(Exception failure)
    {
        var cut = RenderLayoutWith(failure);

        Assert.NotNull(cut.Find("#error-display"));
        Assert.Empty(cut.FindAll("#sign-in-redirect"));
    }

    public static TheoryData<Exception> SignInFailures() => new()
    {
        new UnauthorizedAccessException(),
        ApiExceptions.Create(HttpStatusCode.Unauthorized)
    };

    public static TheoryData<Exception> OtherFailures() => new()
    {
        ApiExceptions.Create(HttpStatusCode.Forbidden),
        ApiExceptions.Create(HttpStatusCode.InternalServerError),
        new InvalidOperationException("boom")
    };

    private IRenderedComponent<MainLayout> RenderLayoutWith(Exception failure) =>
        Render<MainLayout>(parameters => parameters.Add(x => x.Body, builder =>
        {
            builder.OpenComponent<FailingPage>(0);
            builder.AddAttribute(1, nameof(FailingPage.Failure), failure);
            builder.CloseComponent();
        }));

    private sealed class FailingPage : ComponentBase
    {
        [Parameter] public Exception Failure { get; set; } = null!;

        protected override void OnInitialized() => throw Failure;
    }

    private sealed class NoFeatures : IFeatureService
    {
        public event Action? Initialized { add { } remove { } }
        public IEnumerable<IFeature> GetFeatures() => [];
        public Task InitializeFeaturesAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
    }

    private sealed class MarkerUnauthorizedProvider : IUnauthorizedComponentProvider
    {
        public RenderFragment GetUnauthorizedComponent() => builder => builder.AddMarkupContent(0, "<div id=\"sign-in-redirect\"></div>");
    }

    private sealed class MarkerErrorProvider : IErrorComponentProvider
    {
        public RenderFragment GetErrorComponent(Exception context) => builder => builder.AddMarkupContent(0, "<div id=\"error-display\"></div>");
    }
}
