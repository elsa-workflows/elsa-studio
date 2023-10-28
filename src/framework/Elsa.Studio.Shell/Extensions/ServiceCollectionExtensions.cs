using Elsa.Studio.Contracts;
using Elsa.Studio.Shell.ComponentProviders;
using Elsa.Studio.Shell.Options;
using Microsoft.Extensions.DependencyInjection;
using MudBlazor;
using MudBlazor.Services;
using MudExtensions.Services;

namespace Elsa.Studio.Shell.Extensions;

/// <summary>
/// Provides extension methods for the <see cref="IServiceCollection"/> interface.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds the shell services.
    /// </summary>
    public static IServiceCollection AddShell(this IServiceCollection services, Action<ShellOptions>? configure = default)
    {
        configure ??= options => { options.DisableAuthorization = false; };
        services.Configure(configure);

        return services
                .AddMudServices(config =>
                {
                    config.SnackbarConfiguration.PositionClass = Defaults.Classes.Position.TopCenter;
                    config.SnackbarConfiguration.PreventDuplicates = false;
                    config.SnackbarConfiguration.NewestOnTop = false;
                    config.SnackbarConfiguration.ShowCloseIcon = true;
                    config.SnackbarConfiguration.VisibleStateDuration = 1000;
                    config.SnackbarConfiguration.HideTransitionDuration = 100;
                    config.SnackbarConfiguration.ShowTransitionDuration = 100;
                    config.SnackbarConfiguration.SnackbarVariant = Variant.Filled;
                    config.SnackbarConfiguration.MaxDisplayedSnackbars = 3;
                })
                .AddMudExtensions()
                .AddScoped<IUnauthorizedComponentProvider, DefaultUnauthorizedComponentProvider>()
                //.AddScoped<IStartupTask, InitializeModulesStartupTask>()
            ;
    }
}