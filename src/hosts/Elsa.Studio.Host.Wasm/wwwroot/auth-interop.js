// Custom JavaScript to patch oidc-client settings for Azure AD compatibility
// Azure AD requires the 'scope' parameter in token exchange requests

(function () {
    // Azure AD v2.0 only allows scopes for ONE resource per token request
    // Use only the primary API resource - OIDC scopes (openid, profile, offline_access) are always allowed
    // To access multiple resources, you need separate token requests for each resource
    const requestedScopes = 'openid profile offline_access api://dda3270c-997e-413a-9175-36b70134547c/elsa-server-api';
    console.log('[auth-interop] Will use scopes (single resource):', requestedScopes);

    // Store the ID token to extract claims for userinfo
    let lastIdToken = null;

    // Intercept XMLHttpRequest (used by oidc-client-js for token requests)
    const originalOpen = XMLHttpRequest.prototype.open;
    const originalSend = XMLHttpRequest.prototype.send;

    XMLHttpRequest.prototype.open = function (method, url) {
        this._method = method;
        this._url = url;
        this._isUserInfoRequest = url && url.includes('graph.microsoft.com/oidc/userinfo');
        return originalOpen.apply(this, arguments);
    };

    XMLHttpRequest.prototype.send = function (body) {
        let modifiedBody = body;

        // Intercept userinfo requests and return synthetic response from ID token
        if (this._isUserInfoRequest) {
            console.log('[auth-interop] Intercepting userinfo request - will return empty response');
            const xhr = this;

            // Prevent the actual request from being sent
            setTimeout(() => {
                Object.defineProperty(xhr, 'status', { value: 200, writable: false, configurable: true });
                Object.defineProperty(xhr, 'statusText', { value: 'OK', writable: false, configurable: true });
                Object.defineProperty(xhr, 'responseText', { value: '{}', writable: false, configurable: true });
                Object.defineProperty(xhr, 'response', { value: '{}', writable: false, configurable: true });
                Object.defineProperty(xhr, 'readyState', { value: 4, writable: false, configurable: true });

                if (xhr.onreadystatechange) xhr.onreadystatechange();
                if (xhr.onload) xhr.onload();
            }, 0);

            return; // Don't call original send
        }

        // Check if this is a token endpoint request
        if (this._method === 'POST' && this._url && this._url.includes('/oauth2/') && this._url.includes('/token')) {
            console.log('[auth-interop] Intercepted XHR token request to:', this._url);

            if (body && typeof body === 'string') {
                console.log('[auth-interop] Original body length:', body.length);

                // Check if this is an authorization code grant and scope is missing
                if (body.includes('grant_type=authorization_code') && !body.includes('scope=')) {
                    modifiedBody = body + '&scope=' + encodeURIComponent(requestedScopes);
                    console.log('[auth-interop] Added scope to token request');
                    console.log('[auth-interop] Updated body length:', modifiedBody.length);
                }
            }

            // Capture the ID token from the response
            const originalOnLoad = this.onload;
            this.onload = function() {
                try {
                    const tokenResponse = JSON.parse(this.responseText);
                    if (tokenResponse.id_token) {
                        lastIdToken = tokenResponse.id_token;
                        console.log('[auth-interop] Captured ID token from response');
                    }
                } catch (e) {
                    // Ignore
                }
                if (originalOnLoad) originalOnLoad.apply(this, arguments);
            };
        }

        return originalSend.call(this, modifiedBody);
    };

    // Also intercept fetch as a fallback
    const originalFetch = window.fetch;
    window.fetch = function (url, options) {
        // Intercept userinfo requests
        if (url && typeof url === 'string' && url.includes('graph.microsoft.com/oidc/userinfo')) {
            console.log('[auth-interop] Intercepting userinfo fetch - will return empty response');
            return Promise.resolve(new Response('{}', {
                status: 200,
                statusText: 'OK',
                headers: { 'Content-Type': 'application/json' }
            }));
        }

        // Patch token requests
        if (url && typeof url === 'string' && url.includes('/oauth2/') && url.includes('/token') && options && options.method === 'POST') {
            console.log('[auth-interop] Intercepted fetch token request to:', url);

            if (options.body && typeof options.body === 'string') {
                if (options.body.includes('grant_type=authorization_code') && !options.body.includes('scope=')) {
                    options.body = options.body + '&scope=' + encodeURIComponent(requestedScopes);
                    console.log('[auth-interop] Added scope to token request (fetch)');
                }
            }

            // Capture ID token
            return originalFetch.apply(this, arguments).then(response => {
                return response.clone().json().then(tokenResponse => {
                    if (tokenResponse.id_token) {
                        lastIdToken = tokenResponse.id_token;
                        console.log('[auth-interop] Captured ID token from fetch response');
                    }
                    return response;
                }).catch(() => response);
            });
        }

        return originalFetch.apply(this, arguments);
    };

    console.log('[auth-interop] Azure AD compatibility patches initialized (scope + userinfo intercept)');
})();
