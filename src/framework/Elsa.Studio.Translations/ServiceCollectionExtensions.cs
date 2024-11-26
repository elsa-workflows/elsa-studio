using Elsa.Studio.Localization;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Studio.Translations;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddTranslations(this IServiceCollection services)
    {
        services.AddSingleton<ILocalizationProvider, ResxLocalizationProvider>();
        return services;
    }
}