using Elsa.Studio.Localization.Options;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Elsa.Studio.Localization.BlazorServer.Extensions;

public static class WebApplicationExtensions
{
    public static WebApplication UseElsaLocalization(this WebApplication app)
    {
        var localisationOptions = app.Services.GetService<IOptions<LocalizationOptions>>()?.Value;
        var defaultCulture = localisationOptions?.DefaultCulture;
        var supportedCultures = localisationOptions?.SupportedCultures;

        if (supportedCultures != null)
        {
            var localizationOptions = new RequestLocalizationOptions()
                .SetDefaultCulture(defaultCulture ?? new LocalizationOptions().DefaultCulture)
                .AddSupportedCultures(supportedCultures)
                .AddSupportedUICultures(supportedCultures);

            app.UseRequestLocalization(localizationOptions);
        }

        return app;
    }
}