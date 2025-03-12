using Elsa.Studio.Localization.BlazorWasm.Services;
using Elsa.Studio.Localization.Extensions;
using Elsa.Studio.Localization.Models;
using Elsa.Studio.Localization.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Studio.Localization.BlazorWasm.Extensions
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddLocalizationModule(this IServiceCollection services, LocalizationConfig localizationConfig)
        {
            services.AddLocalizationModuleCore(localizationConfig);

            services.AddScoped<ICultureService, BlazorWasmCultureService>();

            return services;
        }
    }
}
