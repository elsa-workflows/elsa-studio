/**
 * Elsa Studio Initialization Module
 * Provides utilities for managing loading screens and Blazor initialization
 */

(function() {
    'use strict';

    let loadingHidden = false;
    let blazorStartAttempted = false;

    /**
     * Hides the loading screen by adding 'blazor-ready' class to body
     */
    function hideLoadingScreen() {
        if (!loadingHidden) {
            loadingHidden = true;
            document.body.classList.add('blazor-ready');
        }
    }

    /**
     * Updates the loading text
     * @param {string} text - The text to display
     */
    function updateLoadingText(text) {
        const loadingTextEl = document.getElementById('elsa-loading-text');
        if (loadingTextEl) {
            loadingTextEl.textContent = text;
        }
    }

    /**
     * Initializes Blazor WebAssembly with optional configuration
     * @param {object} config - Blazor configuration options
     */
    function initializeBlazorWasm(config) {
        if (typeof Blazor === 'undefined') {
            updateLoadingText('Blazor not loaded');
            setTimeout(hideLoadingScreen, 2000);
            return;
        }

        if (!blazorStartAttempted) {
            blazorStartAttempted = true;
            updateLoadingText('Starting...');

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
    }

    /**
     * Initializes Blazor Server
     * Sets up a fallback timeout to hide loading screen
     * @param {number} maxWaitMs - Maximum time to wait before hiding loading screen
     */
    function initializeBlazorServer(maxWaitMs) {
        maxWaitMs = maxWaitMs || 10000;
        setTimeout(function() {
            hideLoadingScreen();
        }, maxWaitMs);
    }

    // Expose functions to window for Blazor components to call
    window.ElsaStudio = window.ElsaStudio || {};
    window.ElsaStudio.hideLoading = hideLoadingScreen;
    window.ElsaStudio.updateLoadingText = updateLoadingText;
    window.ElsaStudio.initializeBlazorWasm = initializeBlazorWasm;
    window.ElsaStudio.initializeBlazorServer = initializeBlazorServer;

    // Legacy compatibility - keep existing function names
    window.hideAuthLoading = hideLoadingScreen;
    window.hideWasmLoading = hideLoadingScreen;
    window.updateLoadingStatus = updateLoadingText;

})();
