/**
 * Elsa Studio Loader for Blazor Server - Simplified for reliability
 * Focuses on loading remaining scripts and managing loading screen
 */

(function() {
    'use strict';

    if (!window.ElsaStudioCore) {
        console.error('ElsaStudioCore not found. Make sure elsa-studio-core.js is loaded first.');
        return;
    }

    let blazorReady = false;
    let initialRenderComplete = false;
    let mudBlazorReady = false;
    let authenticationReady = false;

    // Check if MudBlazor JavaScript is properly loaded
    function checkMudBlazorReady() {
        // Check for MudBlazor global objects
        if (typeof window.mudElementRef !== 'undefined' || 
            typeof window.MudBlazor !== 'undefined' ||
            document.querySelector('script[src*="MudBlazor.min.js"]')) {
            
            console.log('MudBlazor JavaScript detected');
            mudBlazorReady = true;
            return true;
        }
        
        // Give it some time to initialize
        setTimeout(() => {
            console.log('MudBlazor timeout - assuming ready');
            mudBlazorReady = true;
            checkReadiness();
        }, 2000);
        
        return false;
    }

    // Check authentication state before hiding loading screen
    function checkAuthenticationReady() {
        // First, check if we're in the middle of an authentication redirect
        const currentUrl = window.location.href;
        const isAuthRedirect = currentUrl.includes('/authentication/') || 
                              currentUrl.includes('code=') || 
                              currentUrl.includes('state=') ||
                              currentUrl.includes('returnUrl=');
        
        if (isAuthRedirect) {
            console.log('Authentication redirect in progress - waiting...');
            return false; // Don't hide loading screen during auth redirects
        }
        
        // Look for authentication indicators in the DOM
        const authIndicators = [
            '.mud-appbar', // Main app bar usually appears when authenticated
            '.elsa-main-layout',
            '.authenticated-content',
            '.mud-layout main', // Main content area
            '.workflows-page', // Specific to Elsa Studio authenticated pages
            '.dashboard-page'
        ];
        
        // Check if we're still on a login page
        const loginIndicators = [
            '.login-form',
            '.authentication-form',
            'form[action*="login"]',
            '.login-page',
            '.auth-container'
        ];
        
        const hasAuthContent = authIndicators.some(selector => {
            const element = document.querySelector(selector);
            return element && element.offsetHeight > 0;
        });
        
        const hasLoginContent = loginIndicators.some(selector => {
            const element = document.querySelector(selector);
            return element && element.offsetHeight > 0;
        });
        
        // If we have authenticated content and no login content
        if (hasAuthContent && !hasLoginContent) {
            console.log('Authentication state confirmed - user is logged in with app content');
            authenticationReady = true;
            return true;
        }
        
        // If we're on login page, that's also a valid state
        if (hasLoginContent && !hasAuthContent) {
            console.log('Login page detected - user needs to authenticate');
            authenticationReady = true;
            return true;
        }
        
        // If neither are clearly present, wait longer
        console.log('Authentication state unclear - waiting for content to appear...');
        return false;
    }

    // Check if we should hide loading screen
    function checkReadiness() {
        if (blazorReady && initialRenderComplete && mudBlazorReady && authenticationReady && ElsaStudioCore.monacoReady && !ElsaStudioCore.isLoadingHidden) {
            ElsaStudioCore.updateProgress(100);
            ElsaStudioCore.updateLoadingText('Ready!');
            setTimeout(ElsaStudioCore.hideLoadingScreen, 500); // Slightly longer delay for auth
        }
    }

    // Simplified Blazor detection - since blazor.server.js is already loaded
    function detectBlazorConnection() {
        // Check if Blazor is already available
        if (typeof window.Blazor !== 'undefined') {
            console.log('Blazor Server detected');
            blazorReady = true;
            checkReadiness();
            return;
        }

        // Monitor for Blazor availability
        let checkCount = 0;
        const checkInterval = setInterval(() => {
            checkCount++;
            
            if (typeof window.Blazor !== 'undefined') {
                console.log('Blazor Server became available');
                blazorReady = true;
                checkReadiness();
                clearInterval(checkInterval);
            } else if (checkCount > 50) { // 5 seconds max
                console.log('Blazor Server timeout - assuming ready');
                blazorReady = true;
                checkReadiness();
                clearInterval(checkInterval);
            }
        }, 100);
    }

    // Simplified render detection with authentication awareness
    function detectRenderCompletion() {
        // Check for main UI elements
        function checkForUI() {
            const indicators = [
                '.mud-main-content',
                '.mud-layout', 
                '.mud-appbar',
                'main',
                '[role="main"]'
            ];
            
            return indicators.some(selector => {
                const element = document.querySelector(selector);
                if (element && element.offsetHeight > 0) {
                    console.log(`UI detected: ${selector}`);
                    return true;
                }
                return false;
            });
        }

        // Check periodically
        let checkCount = 0;
        let authCheckAttempts = 0;
        const maxAuthCheckAttempts = 10;
        
        const checkInterval = setInterval(() => {
            checkCount++;
            
            if (checkForUI()) {
                initialRenderComplete = true;
                
                // Keep checking authentication state with retry logic
                const authCheckInterval = setInterval(() => {
                    authCheckAttempts++;
                    
                    if (checkAuthenticationReady()) {
                        console.log('Authentication check passed');
                        clearInterval(authCheckInterval);
                        checkReadiness();
                    } else if (authCheckAttempts >= maxAuthCheckAttempts) {
                        console.log('Authentication check timeout - proceeding anyway');
                        authenticationReady = true;
                        clearInterval(authCheckInterval);
                        checkReadiness();
                    }
                }, 500); // Check every 500ms
                
                clearInterval(checkInterval);
            } else if (checkCount > 100) { // 10 seconds max
                console.log('UI detection timeout - assuming ready');
                initialRenderComplete = true;
                authenticationReady = true;
                checkReadiness();
                clearInterval(checkInterval);
            }
        }, 100);
    }

    // Initialize with only Monaco and BlazorMonaco scripts (MudBlazor scripts now loaded in HTML)
    function init() {
        ElsaStudioCore.initialize({
            additionalScripts: [
                // Only Monaco-related scripts since MudBlazor is already loaded
                '_content/BlazorMonaco/jsInterop.js'
            ],
            onScriptsLoaded: () => {
                ElsaStudioCore.updateLoadingText('Initializing components...');
                
                // Check MudBlazor readiness first
                checkMudBlazorReady();
                
                // Give MudBlazor components time to initialize
                setTimeout(() => {
                    ElsaStudioCore.updateLoadingText('Starting components');
                    detectBlazorConnection();
                }, 200);
                
                setTimeout(() => {
                    ElsaStudioCore.updateLoadingText('Handling security');
                    detectRenderCompletion();
                }, 800);
            },
            fallbackTimeout: 8000
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
        console.log('Forcing readiness');
        blazorReady = true;
        initialRenderComplete = true;
        checkReadiness();
    };

})();