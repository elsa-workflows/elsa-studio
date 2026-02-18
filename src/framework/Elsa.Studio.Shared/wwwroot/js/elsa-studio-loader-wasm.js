/**
 * Elsa Studio Loader for Blazor WebAssembly
 * Uses ElsaStudioCore for shared functionality with optimized WASM startup
 */

(function() {
    'use strict';

    if (!window.ElsaStudioCore) {
        console.error('ElsaStudioCore not found. Make sure elsa-studio-core.js is loaded first.');
        return;
    }

    let blazorStartAttempted = false;

    // Initialize Blazor WASM with proper sequencing
    function initializeBlazorWasm(config) {
        if (typeof Blazor === 'undefined') {
            setTimeout(() => initializeBlazorWasm(config), 100);
            return;
        }

        if (!blazorStartAttempted && ElsaStudioCore.monacoReady && ElsaStudioCore.scriptsReady) {
            blazorStartAttempted = true;
            ElsaStudioCore.updateProgress(98);
            ElsaStudioCore.updateLoadingText('Starting application...');
            
            Blazor.start(config || {}).then(() => {
                console.log('Blazor WASM started successfully');
                ElsaStudioCore.updateProgress(100);
                ElsaStudioCore.updateLoadingText('Ready');
                setTimeout(ElsaStudioCore.hideLoadingScreen, 300);
            }).catch((error) => {
                console.error('Blazor WASM startup failed:', error);
                if (!error.message?.includes('already started')) {
                    ElsaStudioCore.updateLoadingText('Startup failed - reloading...');
                    setTimeout(() => location.reload(), 3000);
                } else {
                    ElsaStudioCore.hideLoadingScreen();
                }
            });
        } else if (!ElsaStudioCore.monacoReady || !ElsaStudioCore.scriptsReady) {
            // Wait for dependencies
            setTimeout(() => initializeBlazorWasm(config), 100);
        }
    }

    // Initialize with WASM-specific optimizations
    function init() {
        ElsaStudioCore.initialize({
            additionalScripts: [
                '_content/Microsoft.AspNetCore.Components.WebAssembly.Authentication/AuthenticationService.js',
                '_framework/blazor.webassembly.js'
            ],
            onProgress: (percentage, status) => {
                // WASM-specific progress messages
                if (percentage >= 80) {
                    ElsaStudioCore.updateLoadingText('Loading WebAssembly...');
                }
            },
            onScriptsLoaded: () => {
                ElsaStudioCore.updateLoadingText('Preparing WebAssembly...');
                // Give WASM a moment to initialize before starting Blazor
                setTimeout(() => initializeBlazorWasm(), 200);
            },
            fallbackTimeout: 20000 // WASM needs more time than Server
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
    window.ElsaStudio.initializeBlazorWasm = initializeBlazorWasm;
    window.ElsaStudio.forceReady = function() {
        console.log('Forcing Blazor WASM readiness');
        ElsaStudioCore.hideLoadingScreen();
    };

})();