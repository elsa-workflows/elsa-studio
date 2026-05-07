/**
 * Elsa Studio Loader for Blazor Hosted WebAssembly
 * Uses ElsaStudioCore for shared functionality with custom configuration support
 */

(function() {
    'use strict';

    if (!window.ElsaStudioCore) {
        console.error('ElsaStudioCore not found. Make sure elsa-studio-core.js is loaded first.');
        return;
    }

    let blazorStartAttempted = false;

    // Initialize Blazor with client config for hosted scenarios
    function initializeBlazorWasm() {
        if (typeof Blazor === 'undefined') {
            setTimeout(initializeBlazorWasm, 100);
            return;
        }

        if (!blazorStartAttempted && ElsaStudioCore.monacoReady && ElsaStudioCore.scriptsReady) {
            blazorStartAttempted = true;
            ElsaStudioCore.updateProgress(98);
            ElsaStudioCore.updateLoadingText('Configuring application...');
            
            // Get client configuration if available
            const config = window.getClientConfig ? window.getClientConfig() : {};
            
            const blazorConfig = {};
            if (config.apiUrl) {
                blazorConfig.configureServices = function(services) {
                    services.set('apiUrl', config.apiUrl);
                };
            }
            
            Blazor.start(blazorConfig).then(() => {
                console.log('Blazor Hosted WASM started successfully with config:', config);
                ElsaStudioCore.updateProgress(100);
                ElsaStudioCore.updateLoadingText('Ready');
                setTimeout(ElsaStudioCore.hideLoadingScreen, 300);
            }).catch((error) => {
                console.error('Blazor startup failed:', error);
                if (!error.message?.includes('already started')) {
                    ElsaStudioCore.updateLoadingText('Startup failed - reloading...');
                    setTimeout(() => location.reload(), 3000);
                } else {
                    ElsaStudioCore.hideLoadingScreen();
                }
            });
        } else if (!ElsaStudioCore.monacoReady || !ElsaStudioCore.scriptsReady) {
            setTimeout(initializeBlazorWasm, 100);
        }
    }

    // Initialize with hosted WASM optimizations
    function init() {
        ElsaStudioCore.initialize({
            additionalScripts: [], // Hosted WASM has minimal additional scripts
            onProgress: (percentage, status) => {
                // Hosted WASM specific messages
                if (percentage >= 80) {
                    ElsaStudioCore.updateLoadingText('Loading hosted application...');
                }
            },
            onScriptsLoaded: () => {
                ElsaStudioCore.updateLoadingText('Initializing configuration...');
                setTimeout(initializeBlazorWasm, 100);
            },
            fallbackTimeout: 18000 // Hosted WASM is usually faster than standalone
        });
    }

    // Run initialization
    if (document.readyState === 'loading') {
        document.addEventListener('DOMContentLoaded', init);
    } else {
        init();
    }

    // Expose API
    window.ElsaStudio = window.ElsaStudio || {};
    window.ElsaStudio.hideLoading = ElsaStudioCore.hideLoadingScreen;
    window.ElsaStudio.forceReady = function() {
        console.log('Forcing Blazor Hosted WASM readiness');
        ElsaStudioCore.hideLoadingScreen();
    };

})();