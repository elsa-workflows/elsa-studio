using Elsa.Studio.Branding;
using Elsa.Studio.Components.AppBar;
using Elsa.Studio.Contracts;
using Elsa.Studio.Extensions;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;
using MudBlazor;

namespace Elsa.Studio.Layouts;

/// <summary>
/// The main layout for the application.
/// </summary>
public partial class MainLayout : IDisposable
{
    private bool _drawerOpen = true;
    private ErrorBoundary? _errorBoundary;
    private AuthenticationState? _currentAuthState;

    [Inject] private IThemeService ThemeService { get; set; } = null!;
    [Inject] private IAppBarService AppBarService { get; set; } = null!;
    [Inject] private IUnauthorizedComponentProvider UnauthorizedComponentProvider { get; set; } = null!;
    [Inject] private IErrorComponentProvider ErrorComponentProvider { get; set; } = null!;
    [Inject] private IFeatureService FeatureService { get; set; } = null!;
    [Inject] private IDialogService DialogService { get; set; } = null!;
    [Inject] private IBrandingProvider BrandingProvider { get; set; } = null!;
    [Inject] private IServiceProvider ServiceProvider { get; set; } = null!;
    [Inject] private AuthenticationStateProvider AuthenticationStateProvider { get; set; } = null!;
    [CascadingParameter] private Task<AuthenticationState>? AuthenticationState { get; set; }
    
    private MudTheme CurrentTheme => ThemeService.CurrentTheme;
    private bool IsDarkMode => ThemeService.IsDarkMode;
    
    // Smart unauthorized component that checks authentication state
    private RenderFragment UnauthorizedComponent => GetSmartUnauthorizedComponent();
    private RenderFragment DisplayError(Exception context) => ErrorComponentProvider.GetErrorComponent(context);

    /// <summary>
    /// Returns unauthorized component only if user is not authenticated.
    /// Prevents login modal from appearing when user is already logged in.
    /// </summary>
    private RenderFragment GetSmartUnauthorizedComponent()
    {
        return builder =>
        {
            // Only show unauthorized component if user is genuinely not authenticated
            if (_currentAuthState?.User?.Identity?.IsAuthenticated != true)
            {
                // User is not authenticated - show login
                var unauthorizedFragment = UnauthorizedComponentProvider.GetUnauthorizedComponent();
                unauthorizedFragment(builder);
            }
            else
            {
                // User is authenticated but got unauthorized exception - show error message instead
                builder.OpenElement(0, "div");
                builder.AddAttribute(1, "class", "d-flex flex-column align-center justify-center pa-8");
                builder.AddAttribute(2, "style", "min-height: 300px;");
                
                builder.OpenElement(3, "div");
                builder.AddAttribute(4, "class", "mb-4");
                builder.OpenComponent<MudIcon>(5);
                builder.AddAttribute(6, "Icon", Icons.Material.Filled.Warning);
                builder.AddAttribute(7, "Color", Color.Warning);
                builder.AddAttribute(8, "Size", Size.Large);
                builder.CloseComponent();
                builder.CloseElement();
                
                builder.OpenElement(9, "h3");
                builder.AddAttribute(10, "class", "mb-2");
                builder.AddContent(11, "Access Temporarily Unavailable");
                builder.CloseElement();
                
                builder.OpenElement(12, "p");
                builder.AddAttribute(13, "class", "text-center mb-4");
                builder.AddContent(14, "You don't have permission to access this resource right now. This might be temporary - please try refreshing the page.");
                builder.CloseElement();
                
                builder.OpenComponent<MudButton>(15);
                builder.AddAttribute(16, "Variant", Variant.Filled);
                builder.AddAttribute(17, "Color", Color.Primary);
                builder.AddAttribute(18, "OnClick", EventCallback.Factory.Create(this, RefreshPage));
                builder.AddContent(19, "Refresh Page");
                builder.CloseComponent();
                
                builder.CloseElement();
            }
        };
    }

    /// <summary>
    /// Refreshes the current page
    /// </summary>
    private void RefreshPage()
    {
        var navigationManager = ServiceProvider.GetRequiredService<NavigationManager>();
        navigationManager.NavigateTo(navigationManager.Uri, forceLoad: true);
    }

    /// <inheritdoc />
    protected override void OnInitialized()
    {
        if (BrandingProvider.AppBarIcons.ShowDocumentationLink) AppBarService.AddComponent<Documentation>(10);
        if (BrandingProvider.AppBarIcons.ShowGitHubLink) AppBarService.AddComponent<GitHub>(15);
        AppBarService.AddComponent<DarkModeToggle>(20);
        AppBarService.AddComponent<ProductInfo>(25);
        AppBarService.AddComponent<LogoutButton>(99);
        
        ThemeService.CurrentThemeChanged += OnThemeChanged;
        ThemeService.IsDarkModeChanged += OnDarkModeChanged;
        AppBarService.AppBarItemsChanged += OnAppBarItemsChanged;
    }

    /// <inheritdoc />
    protected override async Task OnInitializedAsync()
    {
        // Track current authentication state
        if (AuthenticationState != null)
        {
            _currentAuthState = await AuthenticationState;
            if (_currentAuthState.User.Identity?.IsAuthenticated == true && !_currentAuthState.User.Claims.IsExpired())
            {
                await FeatureService.InitializeFeaturesAsync();
                StateHasChanged();
            }
        }
        
        // Subscribe to authentication state changes
        AuthenticationStateProvider.AuthenticationStateChanged += OnAuthenticationStateChanged;
    }

    /// <summary>
    /// Handles authentication state changes to update the current state
    /// </summary>
    private async void OnAuthenticationStateChanged(Task<AuthenticationState> authStateTask)
    {
        try
        {
            _currentAuthState = await authStateTask;
            await InvokeAsync(StateHasChanged);
        }
        catch (Exception ex)
        {
            // Log error but don't crash the app
            Console.WriteLine($"Error updating authentication state: {ex.Message}");
        }
    }

    /// <inheritdoc />
    protected override void OnParametersSet()
    {
        _errorBoundary?.Recover();
    }

    private void OnThemeChanged() => InvokeAsync(StateHasChanged);
    private void OnDarkModeChanged() => InvokeAsync(StateHasChanged);
    private void OnAppBarItemsChanged() => InvokeAsync(StateHasChanged);

    private void DrawerToggle()
    {
        _drawerOpen = !_drawerOpen;
    }

    void IDisposable.Dispose()
    {
        ThemeService.CurrentThemeChanged -= OnThemeChanged;
        ThemeService.IsDarkModeChanged -= OnDarkModeChanged;
        AppBarService.AppBarItemsChanged -= OnAppBarItemsChanged;
        AuthenticationStateProvider.AuthenticationStateChanged -= OnAuthenticationStateChanged;
    }
}