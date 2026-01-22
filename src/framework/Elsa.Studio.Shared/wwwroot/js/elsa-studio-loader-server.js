/**
 * Elsa Studio Complete Loader for Blazor Server
 * This script handles all initialization including CSS, scripts, loading screen, and Blazor startup
 * Usage: <script src="_content/Elsa.Studio.Shared/js/elsa-studio-loader-server.js"></script>
 */

(function() {
    'use strict';

    let loadingHidden = false;
    let blazorReady = false;
    let initialRenderComplete = false;

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

    // Update loading text (minimal usage)
    function updateLoadingText(text) {
        const loadingTextEl = document.getElementById('elsa-loading-text');
        if (loadingTextEl) {
            loadingTextEl.textContent = text;
        }
    }

    // Hide loading screen when actually ready
    function hideLoadingScreen() {
        if (!loadingHidden) {
            loadingHidden = true;
            console.log('Blazor Server ready - hiding loading screen');
            document.body.classList.add('blazor-ready');
        }
    }

    // Check if we should hide loading screen
    function checkReadiness() {
        if (blazorReady && initialRenderComplete && !loadingHidden) {
            hideLoadingScreen();
        }
    }

    // Detect when Blazor Server connection is established
    function detectBlazorConnection() {
        // Check for Blazor global object and connection
        if (typeof window.Blazor !== 'undefined') {
            // Monitor for SignalR connection
            const originalLog = console.log;
            console.log = function(...args) {
                const message = args.join(' ');
                if (message.includes('SignalR') || message.includes('connected') || message.includes('circuit')) {
                    blazorReady = true;
                    checkReadiness();
                }
                originalLog.apply(console, args);
            };
        }

        // Check for Blazor Server specific DOM changes
        const observer = new MutationObserver(function(mutations) {
            mutations.forEach(function(mutation) {
                if (mutation.type === 'childList' && mutation.addedNodes.length > 0) {
                    for (let node of mutation.addedNodes) {
                        if (node.nodeType === Node.ELEMENT_NODE) {
                            // Check for Blazor component markers
                            if (node.hasAttribute && (
                                node.hasAttribute('_bl_') || 
                                node.querySelector && node.querySelector('[_bl_]') ||
                                node.classList && node.classList.contains('mud-main-content') ||
                                node.tagName === 'APP'
                            )) {
                                if (!blazorReady) {
                                    blazorReady = true;
                                }
                            }
                        }
                    }
                }
            });
        });

        observer.observe(document.body, { 
            childList: true, 
            subtree: true,
            attributes: true 
        });

        // Stop observing after readiness is detected
        setTimeout(() => {
            if (blazorReady || loadingHidden) {
                observer.disconnect();
            }
        }, 10000);
    }

    // Detect when initial render is complete
    function detectRenderCompletion() {
        // Check for common Blazor/MudBlazor elements
        function checkForMainContent() {
            const indicators = [
                '.mud-main-content',
                '.mud-layout',
                '[role="main"]',
                '.elsa-main-layout',
                '.mud-drawer',
                '.mud-appbar'
            ];
            
            for (let selector of indicators) {
                const element = document.querySelector(selector);
                if (element && element.offsetHeight > 0) {
                    initialRenderComplete = true;
                    checkReadiness();
                    return true;
                }
            }
            return false;
        }

        // Use requestAnimationFrame to detect when rendering settles
        let frameCount = 0;
        let lastBodyHeight = 0;
        let stableFrames = 0;
        
        function checkRenderStability() {
            frameCount++;
            const currentHeight = document.body.offsetHeight;
            
            if (currentHeight === lastBodyHeight && currentHeight > 100) {
                stableFrames++;
                if (stableFrames >= 5) { // 5 stable frames
                    if (!initialRenderComplete && checkForMainContent()) {
                        return;
                    } else if (!initialRenderComplete && frameCount > 30) {
                        initialRenderComplete = true;
                        checkReadiness();
                        return;
                    }
                }
            } else {
                stableFrames = 0;
                lastBodyHeight = currentHeight;
            }
            
            if (frameCount < 100 && !initialRenderComplete) {
                requestAnimationFrame(checkRenderStability);
            } else if (!initialRenderComplete) {
                // Fallback after 100 frames
                initialRenderComplete = true;
                checkReadiness();
            }
        }
        
        // Start checking on next frame
        requestAnimationFrame(checkRenderStability);
        
        // Also check periodically
        const intervalCheck = setInterval(() => {
            if (checkForMainContent()) {
                clearInterval(intervalCheck);
            } else if (frameCount > 100) {
                clearInterval(intervalCheck);
            }
        }, 200);
    }

    // Enhanced Blazor Server initialization
    function initializeBlazorServer(maxWaitMs) {
        maxWaitMs = maxWaitMs || 8000; // Fallback timeout
        
        // Start detection methods
        setTimeout(() => detectBlazorConnection(), 100);
        setTimeout(() => detectRenderCompletion(), 500);
        
        // Ultimate fallback timeout
        setTimeout(function() {
            if (!loadingHidden) {
                console.warn('Fallback timeout reached - forcing loading screen to hide');
                blazorReady = true;
                initialRenderComplete = true;
                hideLoadingScreen();
            }
        }, maxWaitMs);
    }

    // Initialize everything
    function initialize() {
        loadStyles();
        injectLoadingScreen();
        loadScripts();
        initializeBlazorServer(8000);
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
    window.ElsaStudio.forceReady = function() {
        blazorReady = true;
        initialRenderComplete = true;
        checkReadiness();
    };

})();
