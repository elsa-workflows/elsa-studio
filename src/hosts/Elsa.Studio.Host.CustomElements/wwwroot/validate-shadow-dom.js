// Simple validation script for Shadow DOM TypeScript functions
// This can be run in a browser console to test the API

// Test 1: Basic Shadow DOM creation
console.group('Testing Shadow DOM API');

try {
    // Create a test element
    const testElement = document.createElement('div');
    document.body.appendChild(testElement);
    
    // Test createShadowRoot function
    if (typeof createShadowRoot === 'function') {
        const shadowRoot = createShadowRoot(testElement, 'open');
        console.log('✓ createShadowRoot works:', shadowRoot instanceof ShadowRoot);
    } else {
        console.log('✗ createShadowRoot function not found');
    }
    
    // Test getElsaStudioStylesheets function
    if (typeof getElsaStudioStylesheets === 'function') {
        const stylesheets = getElsaStudioStylesheets();
        console.log('✓ getElsaStudioStylesheets works:', Array.isArray(stylesheets) && stylesheets.length > 0);
        console.log('  Stylesheets:', stylesheets);
    } else {
        console.log('✗ getElsaStudioStylesheets function not found');
    }
    
    // Test injectStylesheets function
    if (typeof injectStylesheets === 'function' && testElement.shadowRoot) {
        const testStylesheets = ['test-style.css'];
        injectStylesheets(testElement.shadowRoot, testStylesheets);
        const linkElements = testElement.shadowRoot.querySelectorAll('link[rel="stylesheet"]');
        console.log('✓ injectStylesheets works:', linkElements.length === testStylesheets.length);
    } else {
        console.log('✗ injectStylesheets function not found or shadow root not available');
    }
    
    // Test setupElsaShadowRoot function
    if (typeof setupElsaShadowRoot === 'function') {
        const newTestElement = document.createElement('div');
        document.body.appendChild(newTestElement);
        const shadowRoot = setupElsaShadowRoot(newTestElement);
        
        const hasStylesheets = shadowRoot.querySelectorAll('link[rel="stylesheet"]').length > 0;
        console.log('✓ setupElsaShadowRoot works:', hasStylesheets);
        console.log('  Injected stylesheets count:', shadowRoot.querySelectorAll('link[rel="stylesheet"]').length);
    } else {
        console.log('✗ setupElsaShadowRoot function not found');
    }
    
    // Test registerBlazorCustomElementWithShadowDOM function
    if (typeof registerBlazorCustomElementWithShadowDOM === 'function') {
        const testTagName = 'test-shadow-element-' + Date.now();
        registerBlazorCustomElementWithShadowDOM(testTagName, 'TestComponent');
        
        const isRegistered = customElements.get(testTagName) !== undefined;
        console.log('✓ registerBlazorCustomElementWithShadowDOM works:', isRegistered);
        
        if (isRegistered) {
            // Test creating an instance
            const customElement = document.createElement(testTagName);
            document.body.appendChild(customElement);
            const hasShadowRoot = customElement.shadowRoot !== null;
            console.log('  Custom element has shadow root:', hasShadowRoot);
        }
    } else {
        console.log('✗ registerBlazorCustomElementWithShadowDOM function not found');
    }
    
    // Cleanup
    document.body.querySelectorAll('div').forEach(el => {
        if (el.shadowRoot || el.tagName.toLowerCase().startsWith('test-')) {
            el.remove();
        }
    });
    
} catch (error) {
    console.error('Error during testing:', error);
}

console.groupEnd();

// Test 2: Check browser compatibility
console.group('Browser Compatibility');

console.log('Shadow DOM support:', 'attachShadow' in Element.prototype);
console.log('Custom Elements support:', 'customElements' in window);
console.log('Constructable Stylesheets support:', 'adoptedStyleSheets' in Document.prototype);

console.groupEnd();

// Test 3: Mock Blazor environment
console.group('Blazor Integration Test');

// Mock Blazor object
if (!window.Blazor) {
    window.Blazor = {
        rootComponents: {
            add: function(element, componentName, parameters) {
                console.log('Mock Blazor.rootComponents.add called:', {
                    element: element.constructor.name,
                    componentName,
                    parameters
                });
                return true;
            }
        }
    };
    console.log('✓ Mock Blazor environment created');
}

// Test with mock Blazor
if (typeof registerBlazorCustomElementWithShadowDOM === 'function') {
    const mockTagName = 'mock-blazor-element-' + Date.now();
    registerBlazorCustomElementWithShadowDOM(mockTagName, 'MockComponent');
    
    const mockElement = document.createElement(mockTagName);
    document.body.appendChild(mockElement);
    
    // Trigger connectedCallback
    setTimeout(() => {
        console.log('✓ Mock integration test completed');
        mockElement.remove();
    }, 100);
} else {
    console.log('✗ Cannot test Blazor integration - function not available');
}

console.groupEnd();

console.log('Shadow DOM API validation completed. Check results above.');