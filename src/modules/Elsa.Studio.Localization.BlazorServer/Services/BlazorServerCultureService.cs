using Elsa.Studio.Localization.Services;
using Microsoft.AspNetCore.Components;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Elsa.Studio.Localization.BlazorServer.Services
{
    public class BlazorServerCultureService : ICultureService
    {
        private NavigationManager _navigationManager { get; set; }
        public BlazorServerCultureService(NavigationManager navigationManager)
        {
            _navigationManager = navigationManager;
        }
        public async Task ChangeCultureAsync(CultureInfo culture)
        {
            if (CultureInfo.CurrentUICulture.Name != culture.Name)
            {
                // module = await JS.InvokeAsync<IJSObjectReference>("import", "./_content/Elsa.Studio.Localization/blazorCulture.js");
                // await JS.InvokeVoidAsync("blazorCulture.set", culture!.Name);

                // Navigation.NavigateTo(Navigation.Uri, forceLoad: true);

                var cultureString = culture.Name;
                var uri = new Uri(_navigationManager.Uri)
                .GetComponents(UriComponents.PathAndQuery, UriFormat.Unescaped);
                var newUri = _navigationManager.ToAbsoluteUri($"/{cultureString}/{uri}");
                //set culture of the current thread
                CultureInfo.CurrentCulture = culture;
                CultureInfo.CurrentUICulture = culture;
                var cultureEscaped = Uri.EscapeDataString(cultureString);
                var uriEscaped = Uri.EscapeDataString(uri);

                _navigationManager.NavigateTo(
                    $"Culture/Set?culture={cultureEscaped}&redirectUri={uriEscaped}",
                    forceLoad: true);
            }
        }
    }
}
