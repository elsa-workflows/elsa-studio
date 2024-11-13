using Elsa.Studio.Localization.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Elsa.Studio.Localization.BlazorWasm.Services
{
    public class BlazorWasmCultureService : ICultureService
    {
        private readonly NavigationManager _navigationManager;
        private readonly IJSRuntime _jSRuntime;

        private IJSObjectReference? module;

        public BlazorWasmCultureService(NavigationManager navigationManager, IJSRuntime jSRuntime)
        {
            _navigationManager = navigationManager;
            _jSRuntime = jSRuntime;
        }
        public async Task ChangeCultureAsync(CultureInfo culture)
        {
            if (CultureInfo.CurrentUICulture.Name != culture.Name)
            {
                module = await _jSRuntime.InvokeAsync<IJSObjectReference>("import", "./_content/Elsa.Studio.Localization.BlazorWasm/blazorCulture.js");
                await _jSRuntime.InvokeVoidAsync("blazorCulture.set", culture!.Name);

                _navigationManager.NavigateTo(_navigationManager.Uri, forceLoad: true);
            }
        }
    }
}
