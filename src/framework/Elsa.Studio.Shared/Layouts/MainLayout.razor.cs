using Blazored.LocalStorage;
using Elsa.Studio.Components;
using Elsa.Studio.Contracts;
using Elsa.Studio.Extensions;
using Elsa.Studio.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Web;
using MudBlazor;

namespace Elsa.Studio.Layouts;

/// <summary>
/// The main layout for the application.
/// </summary>
public partial class MainLayout : IDisposable
{
    private bool _drawerOpen = true;
    private ErrorBoundary? _errorBoundary;

    [Inject] private IThemeService ThemeService { get; set; } = default!;
    [Inject] private IAppBarService AppBarService { get; set; } = default!;
    [Inject] private IUnauthorizedComponentProvider UnauthorizedComponentProvider { get; set; } = default!;
    [Inject] private IFeatureService FeatureService { get; set; } = default!;
    [Inject] private IDialogService DialogService { get; set; } = default!;
    [Inject] private IServiceProvider ServiceProvider { get; set; } = default!;
    [Inject] private IBlazorServiceAccessor BlazorServiceAccessor { get; set; } = default!;
    [CascadingParameter] private Task<AuthenticationState>? AuthenticationState { get; set; }
    private MudTheme CurrentTheme => ThemeService.CurrentTheme;
    private bool IsDarkMode => ThemeService.IsDarkMode;
    private RenderFragment UnauthorizedComponent => UnauthorizedComponentProvider.GetUnauthorizedComponent();

    /// <inheritdoc />
    protected override void OnInitialized()
    {
        ThemeService.CurrentThemeChanged += OnThemeChanged;
        AppBarService.AppBarItemsChanged += OnAppBarItemsChanged;
    }

    /// <inheritdoc />
    protected override async Task OnInitializedAsync()
    {
        if (AuthenticationState != null)
        {
            var authState = await AuthenticationState;
            if (authState.User.Identity?.IsAuthenticated == true && !authState.User.Claims.IsExpired())
            {
                BlazorServiceAccessor.Services = ServiceProvider;
                await FeatureService.InitializeFeaturesAsync();
                StateHasChanged();
            }
        }
    }

    /// <inheritdoc />
    protected override void OnParametersSet()
    {
        _errorBoundary?.Recover();
    }

    private void OnThemeChanged() => StateHasChanged();
    private void OnAppBarItemsChanged() => InvokeAsync(StateHasChanged);

    private void DrawerToggle()
    {
        _drawerOpen = !_drawerOpen;
    }

    private void ToggleDarkMode()
    {
        ThemeService.IsDarkMode = !ThemeService.IsDarkMode;
    }

    private async Task ShowProductInfo()
    {
        await DialogService.ShowAsync<ProductInfoDialog>("Elsa Studio 3", new DialogOptions
        {
            FullWidth = true,
            MaxWidth = MaxWidth.ExtraSmall,
            CloseButton = true,
            CloseOnEscapeKey = true
        });
    }

    void IDisposable.Dispose()
    {
        ThemeService.CurrentThemeChanged -= OnThemeChanged;
    }
}