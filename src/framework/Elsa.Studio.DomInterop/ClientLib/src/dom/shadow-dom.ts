/**
 * Creates a shadow DOM root for the given element
 */
export function createShadowRoot(element: HTMLElement, mode: 'open' | 'closed' = 'open'): ShadowRoot {
    if (element.shadowRoot) {
        return element.shadowRoot;
    }
    
    return element.attachShadow({ mode });
}

/**
 * Injects stylesheets into a shadow DOM root
 */
export function injectStylesheets(shadowRoot: ShadowRoot, stylesheets: string[]): void {
    stylesheets.forEach(href => {
        const link = document.createElement('link');
        link.rel = 'stylesheet';
        link.href = href;
        shadowRoot.appendChild(link);
    });
}

/**
 * Gets the default Elsa Studio stylesheets
 */
export function getElsaStudioStylesheets(): string[] {
    return [
        '_content/MudBlazor/MudBlazor.min.css',
        '_content/CodeBeam.MudBlazor.Extensions/MudExtensions.min.css',
        '_content/Radzen.Blazor/css/material-base.css',
        '_content/Elsa.Studio.Shell/css/shell.css',
        '_content/Elsa.Studio.Workflows.Designer/designer.css'
    ];
}

/**
 * Sets up a shadow DOM root with Elsa Studio styles
 */
export function setupElsaShadowRoot(element: HTMLElement, customStylesheets?: string[]): ShadowRoot {
    const shadowRoot = createShadowRoot(element);
    const stylesheets = customStylesheets || getElsaStudioStylesheets();
    injectStylesheets(shadowRoot, stylesheets);
    return shadowRoot;
}

/**
 * Registers a Blazor custom element with Shadow DOM support
 */
export function registerBlazorCustomElementWithShadowDOM(tagName: string, componentName: string): void {
    if (customElements.get(tagName)) {
        return; // Already defined
    }

    class ElsaShadowCustomElement extends HTMLElement {
        private _elsaShadowRoot: ShadowRoot;

        constructor() {
            super();
            this._elsaShadowRoot = this.attachShadow({ mode: 'open' });
            injectStylesheets(this._elsaShadowRoot, getElsaStudioStylesheets());
        }

        connectedCallback() {
            // Wait for Blazor to be ready
            if (window.Blazor && window.Blazor.rootComponents) {
                window.Blazor.rootComponents.add(this._elsaShadowRoot, componentName, this);
            } else {
                // Blazor not ready yet, wait for it
                document.addEventListener('DOMContentLoaded', () => {
                    if (window.Blazor && window.Blazor.rootComponents) {
                        window.Blazor.rootComponents.add(this._elsaShadowRoot, componentName, this);
                    }
                });
            }
        }
    }

    customElements.define(tagName, ElsaShadowCustomElement);
}

// Extend the global Window interface to include Blazor
declare global {
    interface Window {
        Blazor: {
            rootComponents: {
                add(element: Element | ShadowRoot, componentName: string, parameters?: any): void;
            };
        };
    }
}