(function() {
    'use strict';

    // Extend abp namespace if it exists
    var abp = window.abp || {};
    if (!window.abp) {
        window.abp = abp;
    }

    abp.embedding = abp.embedding || {};

    /**
     * ABP Embedding Web Component
     * Simple iframe with auto-height capability
     */
    class AbpEmbeddingElement extends HTMLElement {
        constructor() {
            super();
            this.iframe = null;
            this.isIframeLoaded = false;
            this.autoHeightConfig = null;
            
            // Bind methods
            this.handleIframeLoad = this.handleIframeLoad.bind(this);
            this.handleIframeMessage = this.handleIframeMessage.bind(this);
        }

        static get observedAttributes() {
            return ['src', 'width', 'height', 'allow', 'sandbox', 'loading', 'auto-height'];
        }

        connectedCallback() {
            this.render();
            this.setupEventListeners();
        }

        disconnectedCallback() {
            this.cleanup();
        }

        attributeChangedCallback(name, oldValue, newValue) {
            if (oldValue !== newValue) {
                this.updateIframeAttribute(name, newValue);
            }
        }

        render() {
            // Clear existing content
            this.innerHTML = '';

            // Create iframe element
            this.iframe = document.createElement('iframe');
            
            // Set default attributes
            this.iframe.setAttribute('frameborder', '0');
            this.iframe.setAttribute('scrolling', 'auto');
            this.iframe.style.border = 'none';
            this.iframe.style.outline = 'none';
            this.iframe.style.display = 'block';
            this.iframe.style.width = '100%';

            // Apply custom attributes
            this.updateAllAttributes();

            // Add load event listener
            this.iframe.addEventListener('load', this.handleIframeLoad);

            this.appendChild(this.iframe);
        }

        updateAllAttributes() {
            const attributes = ['src', 'width', 'height', 'allow', 'sandbox', 'loading'];
            attributes.forEach(attr => {
                const value = this.getAttribute(attr);
                if (value) {
                    this.updateIframeAttribute(attr, value);
                }
            });
        }

        updateIframeAttribute(name, value) {
            if (!this.iframe) return;

            switch (name) {
                case 'src':
                    this.iframe.src = value;
                    this.isIframeLoaded = false;
                    break;
                case 'width':
                    this.iframe.style.width = value.includes('%') || value.includes('px') ? value : value + 'px';
                    break;
                case 'height':
                    if (!this.hasAttribute('auto-height')) {
                        this.iframe.style.height = value.includes('%') || value.includes('px') ? value : value + 'px';
                    }
                    break;
                case 'auto-height':
                    if (value === 'true' || value === '') {
                        this.enableAutoHeight();
                    } else {
                        this.disableAutoHeight();
                    }
                    break;
                default:
                    this.iframe.setAttribute(name, value);
            }
        }

        setupEventListeners() {
            // Listen for messages from iframe
            window.addEventListener('message', this.handleIframeMessage);
        }

        cleanup() {
            window.removeEventListener('message', this.handleIframeMessage);
            if (this.iframe) {
                this.iframe.removeEventListener('load', this.handleIframeLoad);
            }
        }

        handleIframeLoad() {
            this.isIframeLoaded = true;

            // Dispatch custom event
            this.dispatchEvent(new CustomEvent('iframe-loaded', {
                detail: { iframe: this.iframe },
                bubbles: true
            }));
        }

        handleIframeMessage(event) {
            // Simple message filtering - only accept height updates from our iframe
            if (this.iframe && 
                event.source === this.iframe.contentWindow && 
                event.data && 
                event.data.type === 'height-update') {
                
                this.updateIframeHeight(event.data.height);
            }
        }

        /**
         * Update iframe height based on content
         */
        updateIframeHeight(height) {
            if (!this.iframe || !height || height <= 0) {
                return;
            }

            const config = this.autoHeightConfig || {};
            const minHeight = config.minHeight || 100;
            const maxHeight = config.maxHeight || window.innerHeight * 0.9;
            
            // Ensure height is within reasonable bounds
            const newHeight = Math.max(minHeight, Math.min(height, maxHeight));
            
            // Update iframe height
            this.iframe.style.height = newHeight + 'px';

            // Dispatch custom event
            this.dispatchEvent(new CustomEvent('height-updated', {
                detail: { 
                    height: newHeight,
                    originalHeight: height
                },
                bubbles: true
            }));
        }

        /**
         * Get the iframe element
         */
        getIframe() {
            return this.iframe;
        }

        /**
         * Check if iframe is loaded
         */
        isLoaded() {
            return this.isIframeLoaded;
        }

        /**
         * Enable auto-height functionality
         */
        enableAutoHeight(options = {}) {
            const config = {
                minHeight: options.minHeight || 100,
                maxHeight: options.maxHeight || window.innerHeight * 0.9,
                ...options
            };

            this.autoHeightConfig = config;
            this.setAttribute('auto-height', 'true');
        }

        /**
         * Disable auto-height functionality
         */
        disableAutoHeight() {
            this.removeAttribute('auto-height');
            this.autoHeightConfig = null;

            // Reset to original height if specified
            const originalHeight = this.getAttribute('height') || '400px';
            if (this.iframe) {
                this.iframe.style.height = originalHeight;
            }
        }
    }

    // Register the custom element
    if (!customElements.get('abp-embedding')) {
        customElements.define('abp-embedding', AbpEmbeddingElement);
    }

    // Add utility functions to abp.embedding namespace
    abp.embedding.create = function(options) {
        const element = document.createElement('abp-embedding');
        
        if (options.src) element.setAttribute('src', options.src);
        if (options.width) element.setAttribute('width', options.width);
        if (options.height) element.setAttribute('height', options.height);
        if (options.allow) element.setAttribute('allow', options.allow);
        if (options.sandbox) element.setAttribute('sandbox', options.sandbox);
        if (options.loading) element.setAttribute('loading', options.loading);
        if (options.autoHeight) element.setAttribute('auto-height', options.autoHeight);

        return element;
    };

    abp.embedding.enableAutoHeight = function(element, options) {
        if (element && typeof element.enableAutoHeight === 'function') {
            element.enableAutoHeight(options);
        }
    };

    abp.embedding.disableAutoHeight = function(element) {
        if (element && typeof element.disableAutoHeight === 'function') {
            element.disableAutoHeight();
        }
    };

})(); 