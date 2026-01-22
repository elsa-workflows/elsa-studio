/**
 * Elsa Studio Complete Loader for Blazor WebAssembly
 * This script handles all initialization including CSS, scripts, loading screen, and Blazor startup
 * Usage: <script src="_content/Elsa.Studio.Shared/js/elsa-studio-loader-wasm.js"></script>
 */

(function() {
    'use strict';

    // Load required stylesheets
    function loadStyles() {
        const styles = [
            '_content/MudBlazor/MudBlazor.min.css',
            '_content/CodeBeam.MudBlazor.Extensions/MudExtensions.min.css',
            '_content/Radzen.Blazor/css/material-base.css',
            '_content/Elsa.Studio.Shell/css/shell.css'
        ];

        styles.forEach(href => {
            if (!document.querySelector(`link[href="${href}"]`)) {
                const link = document.createElement('link');
                link.rel = 'stylesheet';
                link.href = href;
                document.head.appendChild(link);
            }
        });
    }

    // Load required scripts
    function loadScripts(callback) {
        const scripts = [
            '_content/BlazorMonaco/jsInterop.js',
            '_content/BlazorMonaco/lib/monaco-editor/min/vs/loader.js',
            '_content/BlazorMonaco/lib/monaco-editor/min/vs/editor/editor.main.js',
            '_content/MudBlazor/MudBlazor.min.js',
            '_content/CodeBeam.MudBlazor.Extensions/MudExtensions.min.js',
            '_content/Radzen.Blazor/Radzen.Blazor.js',
            '_content/Microsoft.AspNetCore.Components.WebAssembly.Authentication/AuthenticationService.js',
            '_framework/blazor.webassembly.js'
        ];

        let loaded = 0;
        scripts.forEach(src => {
            if (document.querySelector(`script[src="${src}"]`)) {
                loaded++;
                if (loaded === scripts.length && callback) callback();
                return;
            }

            const script = document.createElement('script');
            script.src = src;
            script.onload = () => {
                loaded++;
                if (loaded === scripts.length && callback) callback();
            };
            document.body.appendChild(script);
        });
    }

    // Inject loading screen HTML
    function injectLoadingScreen() {
        if (document.getElementById('elsa-loading')) {
            return; // Already exists
        }

        const loadingHtml = `
            <div id="elsa-loading" style="position: fixed; top: 0; left: 0; width: 100%; height: 100%; background: #f5f5f5; display: flex; justify-content: center; align-items: center; z-index: 9999;">
                <div style="text-align: center;">
                    <div id="elsa-loading-spinner" style="width: 40px; height: 40px; border: 4px solid #e0e0e0; border-top: 4px solid #1976d2; border-radius: 50%; animation: elsa-loading-spin 1s linear infinite; margin: 0 auto 20px;"></div>
                    <div id="elsa-loading-text" style="color: #666; font-family: 'Roboto', sans-serif;">Initializing...</div>
                </div>
            </div>
        `;
        
        const loadingStyle = `
            <style id="elsa-loading-styles">
                @keyframes elsa-loading-spin {
                    0% { transform: rotate(0deg); }
                    100% { transform: rotate(360deg); }
                }
                .blazor-ready #elsa-loading {
                    display: none !important;
                }
            </style>
        `;
        
        document.body.insertAdjacentHTML('afterbegin', loadingHtml);
        document.head.insertAdjacentHTML('beforeend', loadingStyle);
    }

    // Initialize Blazor WASM
    function initializeBlazorWasm(config) {
        if (typeof Blazor === 'undefined') {
            updateLoadingText('Blazor not loaded');
            setTimeout(hideLoadingScreen, 2000);
            return;
        }

        updateLoadingText('Starting Blazor WASM...');

        Blazor.start(config || {}).then(() => {
            updateLoadingText('Loading application...');
        }).catch((error) => {
            if (error.message && error.message.includes('already started')) {
                setTimeout(hideLoadingScreen, 100);
            } else {
                console.error('Blazor startup failed:', error);
                updateLoadingText('Startup failed');
                setTimeout(hideLoadingScreen, 2000);
            }
        });
    }

    function hideLoadingScreen() {
        document.body.classList.add('blazor-ready');
    }

    function updateLoadingText(text) {
        const loadingTextEl = document.getElementById('elsa-loading-text');
        if (loadingTextEl) {
            loadingTextEl.textContent = text;
        }
    }

    // Initialize everything
    function initialize() {
        loadStyles();
        injectLoadingScreen();
        loadScripts(function() {
            // Wait a bit for Blazor to be available
            setTimeout(function() {
                initializeBlazorWasm();
            }, 100);
        });

        // Safety timeout
        setTimeout(hideLoadingScreen, 5000);
    }

    // Run initialization
    if (document.readyState === 'loading') {
        document.addEventListener('DOMContentLoaded', initialize);
    } else {
        initialize();
    }

    // Expose API for manual control if needed
    window.ElsaStudio = window.ElsaStudio || {};
    window.ElsaStudio.hideLoading = hideLoadingScreen;
    window.ElsaStudio.updateLoadingText = updateLoadingText;

})();
