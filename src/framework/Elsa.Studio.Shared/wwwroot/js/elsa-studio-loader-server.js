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
    let monacoReady = false;

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

    // Load Monaco Editor first with proper sequencing
    function loadMonacoEditor() {
        return new Promise((resolve, reject) => {
            // First load the Monaco loader
            const loaderScript = document.createElement('script');
            loaderScript.src = '_content/BlazorMonaco/lib/monaco-editor/min/vs/loader.js';
            loaderScript.onload = () => {
                // Configure RequireJS paths for Monaco
                if (typeof require !== 'undefined') {
                    require.config({ 
                        paths: { 
                            'vs': '_content/BlazorMonaco/lib/monaco-editor/min/vs' 
                        } 
                    });
                    
                    // Load Monaco editor main
                    require(['vs/editor/editor.main'], () => {
                        console.log('Monaco editor loaded successfully');
                        monacoReady = true;
                        resolve();
                    }, (error) => {
                        console.error('Failed to load Monaco editor:', error);
                        reject(error);
                    });
                } else {
                    // Fallback: load editor.main.js directly
                    const editorScript = document.createElement('script');
                    editorScript.src = '_content/BlazorMonaco/lib/monaco-editor/min/vs/editor/editor.main.js';
                    editorScript.onload = () => {
                        console.log('Monaco editor loaded (fallback method)');
                        monacoReady = true;
                        resolve();
                    };
                    editorScript.onerror = reject;
                    document.body.appendChild(editorScript);
                }
            };
            loaderScript.onerror = reject;
            document.body.appendChild(loaderScript);
        });
    }

    // Load remaining scripts after Monaco is ready
    function loadOtherScripts() {
        const scripts = [
            '_content/BlazorMonaco/jsInterop.js',
            '_content/MudBlazor/MudBlazor.min.js',
            '_content/CodeBeam.MudBlazor.Extensions/MudExtensions.min.js',
            '_content/Radzen.Blazor/Radzen.Blazor.js',
            '_framework/blazor.server.js'
        ];

        const loadPromises = scripts.map(src => {
            return new Promise((resolve, reject) => {
                if (document.querySelector(`script[src="${src}"]`)) {
                    resolve(); // Already loaded
                    return;
                }
                
                const script = document.createElement('script');
                script.src = src;
                script.onload = resolve;
                script.onerror = reject;
                document.body.appendChild(script);
            });
        });

        return Promise.all(loadPromises);
    }

    // Load all scripts in proper sequence
    async function loadScripts() {
        try {
            console.log('Loading Monaco editor...');
            await loadMonacoEditor();
            console.log('Loading other scripts...');
            await loadOtherScripts();
            console.log('All scripts loaded successfully');
        } catch (error) {
            console.error('Script loading failed:', error);
            // Continue anyway - some features might still work
        }
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
        if (blazorReady && initialRenderComplete && monacoReady && !loadingHidden) {
            hideLoadingScreen();
        }
    }

    // Detect when Blazor Server connection is established
    function detectBlazorConnection() {
        if (typeof window.Blazor !== 'undefined') {
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

        const observer = new MutationObserver(function(mutations) {
            mutations.forEach(function(mutation) {
                if (mutation.type === 'childList' && mutation.addedNodes.length > 0) {
                    for (let node of mutation.addedNodes) {
                        if (node.nodeType === Node.ELEMENT_NODE) {
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

        setTimeout(() => {
            if (blazorReady || loadingHidden) {
                observer.disconnect();
            }
        }, 10000);
    }

    // Detect when initial render is complete
    function detectRenderCompletion() {
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

        let frameCount = 0;
        let lastBodyHeight = 0;
        let stableFrames = 0;
        
        function checkRenderStability() {
            frameCount++;
            const currentHeight = document.body.offsetHeight;
            
            if (currentHeight === lastBodyHeight && currentHeight > 100) {
                stableFrames++;
                if (stableFrames >= 5) {
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
                initialRenderComplete = true;
                checkReadiness();
            }
        }
        
        requestAnimationFrame(checkRenderStability);
        
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
        maxWaitMs = maxWaitMs || 8000;
        
        setTimeout(() => detectBlazorConnection(), 100);
        setTimeout(() => detectRenderCompletion(), 500);
        
        setTimeout(function() {
            if (!loadingHidden) {
                console.warn('Fallback timeout reached - forcing loading screen to hide');
                blazorReady = true;
                initialRenderComplete = true;
                monacoReady = true; // Force ready on timeout
                hideLoadingScreen();
            }
        }, maxWaitMs);
    }

    // Initialize everything with proper sequencing
    async function initialize() {
        console.log('Starting Elsa Studio initialization...');
        loadStyles();
        injectLoadingScreen();
        
        // Load scripts asynchronously but track completion
        loadScripts(); // Don't await - let it load in background
        
        initializeBlazorServer(10000); // Give more time for Monaco loading
    }

    // Run initialization
    if (document.readyState === 'loading') {
        document.addEventListener('DOMContentLoaded', initialize);
    } else {
        initialize();
    }

    // Expose API
    window.ElsaStudio = window.ElsaStudio || {};
    window.ElsaStudio.hideLoading = hideLoadingScreen;
    window.ElsaStudio.forceReady = function() {
        blazorReady = true;
        initialRenderComplete = true;
        monacoReady = true;
        checkReadiness();
    };

})();
