/**
 * Elsa Studio Complete Loader for Blazor Server
 * This script handles all initialization including CSS, scripts, loading screen, and Blazor startup
 * Usage: <script src="_content/Elsa.Studio.Shared/js/elsa-studio-loader-server.js"></script>
 */

(function() {
    'use strict';

    // Load required stylesheets
    function loadStyles() {
        const styles = [
            '_content/MudBlazor/MudBlazor.min.css',
            '_content/CodeBeam.MudBlazor.Extensions/MudExtensions.min.css',
            '_content/Radzen.Blazor/css/material-base.css',
            '_content/Elsa.Studio.Shell/css/shell.css',
            '_content/Elsa.Studio.Workflows.Designer/designer.css'
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
    function loadScripts() {
        const scripts = [
            '_content/BlazorMonaco/jsInterop.js',
            '_content/BlazorMonaco/lib/monaco-editor/min/vs/loader.js',
            '_content/BlazorMonaco/lib/monaco-editor/min/vs/editor/editor.main.js',
            '_content/MudBlazor/MudBlazor.min.js',
            '_content/CodeBeam.MudBlazor.Extensions/MudExtensions.min.js',
            '_content/Radzen.Blazor/Radzen.Blazor.js',
            '_framework/blazor.server.js'
        ];

        scripts.forEach(src => {
            if (!document.querySelector(`script[src="${src}"]`)) {
                const script = document.createElement('script');
                script.src = src;
                document.body.appendChild(script);
            }
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

    // Initialize Blazor Server with timeout fallback
    function initializeBlazorServer(maxWaitMs) {
        maxWaitMs = maxWaitMs || 10000;
        setTimeout(function() {
            document.body.classList.add('blazor-ready');
        }, maxWaitMs);
    }

    // Initialize everything
    function initialize() {
        loadStyles();
        injectLoadingScreen();
        loadScripts();
        initializeBlazorServer(10000);
    }

    // Run initialization
    if (document.readyState === 'loading') {
        document.addEventListener('DOMContentLoaded', initialize);
    } else {
        initialize();
    }

    // Expose API for manual control if needed
    window.ElsaStudio = window.ElsaStudio || {};
    window.ElsaStudio.hideLoading = function() {
        document.body.classList.add('blazor-ready');
    };

})();
