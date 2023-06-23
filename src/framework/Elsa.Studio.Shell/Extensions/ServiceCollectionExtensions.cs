using Elsa.Studio.Contracts;
using Elsa.Studio.Extensions;
using Elsa.Studio.Shell.Tasks;
using Microsoft.Extensions.DependencyInjection;
using MudBlazor;
using MudBlazor.Services;

namespace Elsa.Studio.Shell.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddShell(this IServiceCollection services)
    {
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
            .AddCore()
            .AddSingleton<IStartupTask, InitializeModulesStartupTask>();
    }
}