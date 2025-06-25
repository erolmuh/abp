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
     * Renders an iframe with data passing capabilities
     */
    class AbpEmbeddingElement extends HTMLElement {
        constructor() {
            super();
            this.iframe = null;
            this.dataQueue = [];
            this.isIframeLoaded = false;
            this.urlSyncEnabled = false;
            this.baseIframeUrl = '';
            this.isNavigatingFromParent = false;
            
            // Bind methods
            this.handleIframeLoad = this.handleIframeLoad.bind(this);
            this.handleIframeMessage = this.handleIframeMessage.bind(this);
            this.handlePopState = this.handlePopState.bind(this);
        }

        static get observedAttributes() {
            return ['src', 'width', 'height', 'allow', 'sandbox', 'loading', 'url-sync'];
        }

        connectedCallback() {
            this.render();
            this.setupEventListeners();
            this.initializeUrlSync();
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
            this.iframe.style.height = '100%';

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
                    this.baseIframeUrl = this.extractBaseUrl(value);
                    this.iframe.src = value;
                    this.isIframeLoaded = false;
                    break;
                case 'width':
                    this.iframe.style.width = value.includes('%') || value.includes('px') ? value : value + 'px';
                    break;
                case 'height':
                    this.iframe.style.height = value.includes('%') || value.includes('px') ? value : value + 'px';
                    break;
                case 'url-sync':
                    this.urlSyncEnabled = value === 'true' || value === '';
                    if (this.urlSyncEnabled) {
                        this.initializeUrlSync();
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
            window.removeEventListener('popstate', this.handlePopState);
            if (this.iframe) {
                this.iframe.removeEventListener('load', this.handleIframeLoad);
            }
        }

        handleIframeLoad() {
            this.isIframeLoaded = true;
            
            // Send any queued data
            if (this.dataQueue.length > 0) {
                this.dataQueue.forEach(data => this.sendDataToIframe(data));
                this.dataQueue = [];
            }

            // Initialize URL sync if enabled
            if (this.urlSyncEnabled) {
                this.syncInitialUrl();
            }

            // Dispatch custom event
            this.dispatchEvent(new CustomEvent('iframe-loaded', {
                detail: { iframe: this.iframe },
                bubbles: true
            }));
        }

        handleIframeMessage(event) {
            // Verify the message is from our iframe
            if (this.iframe && event.source === this.iframe.contentWindow) {
                // Handle URL sync messages
                if (this.urlSyncEnabled && event.data && event.data.type === 'url-change') {
                    this.handleIframeUrlChange(event.data.path);
                    return;
                }

                // Dispatch custom event with the received data
                this.dispatchEvent(new CustomEvent('iframe-message', {
                    detail: {
                        data: event.data,
                        origin: event.origin,
                        source: event.source
                    },
                    bubbles: true
                }));
            }
        }

        /**
         * Send data to the iframe content
         * @param {*} data - Data to send
         * @param {string} targetOrigin - Target origin (default: '*')
         */
        sendData(data, targetOrigin = '*') {
            if (this.isIframeLoaded && this.iframe) {
                this.sendDataToIframe({ data, targetOrigin });
            } else {
                // Queue the data until iframe is loaded
                this.dataQueue.push({ data, targetOrigin });
            }
        }

        sendDataToIframe({ data, targetOrigin }) {
            try {
                this.iframe.contentWindow.postMessage(data, targetOrigin);
            } catch (error) {
                console.error('Failed to send data to iframe:', error);
            }
        }

        /**
         * Get the iframe element
         * @returns {HTMLIFrameElement}
         */
        getIframe() {
            return this.iframe;
        }

        /**
         * Check if iframe is loaded
         * @returns {boolean}
         */
        isLoaded() {
            return this.isIframeLoaded;
        }

        /**
         * Initialize URL synchronization
         */
        initializeUrlSync() {
            if (!this.urlSyncEnabled) {
                this.urlSyncEnabled = this.hasAttribute('url-sync');
            }

            if (this.urlSyncEnabled) {
                window.addEventListener('popstate', this.handlePopState);
                
                // Sync initial URL if needed
                if (this.isIframeLoaded) {
                    this.syncInitialUrl();
                }
            }
        }

        /**
         * Extract base URL from iframe src
         * @param {string} url 
         * @returns {string}
         */
        extractBaseUrl(url) {
            try {
                const urlObj = new URL(url);
                return `${urlObj.protocol}//${urlObj.host}`;
            } catch (error) {
                console.warn('Invalid iframe URL:', url);
                return '';
            }
        }

        /**
         * Handle URL change from iframe
         * @param {string} path 
         */
        handleIframeUrlChange(path) {
            if (this.isNavigatingFromParent) {
                this.isNavigatingFromParent = false;
                return;
            }

            // Update parent URL to match iframe path
            const currentPath = window.location.pathname;
            const newPath = path.startsWith('/') ? path : '/' + path;

            if (currentPath !== newPath) {
                window.history.pushState({ iframePath: newPath }, '', newPath);
                
                // Dispatch custom event
                this.dispatchEvent(new CustomEvent('url-synced', {
                    detail: { 
                        type: 'iframe-to-parent',
                        oldPath: currentPath, 
                        newPath: newPath 
                    },
                    bubbles: true
                }));
            }
        }

        /**
         * Handle browser back/forward button
         * @param {PopStateEvent} event 
         */
        handlePopState(event) {
            if (!this.urlSyncEnabled || !this.isIframeLoaded) {
                return;
            }

            const currentPath = window.location.pathname;
            this.navigateIframeToPath(currentPath);
        }

        /**
         * Navigate iframe to specific path
         * @param {string} path 
         */
        navigateIframeToPath(path) {
            if (!this.baseIframeUrl || !this.iframe) {
                return;
            }

            this.isNavigatingFromParent = true;

            // Construct iframe URL with embed parameter
            const iframePath = path.startsWith('/') ? path : '/' + path;
            const iframeUrl = `${this.baseIframeUrl}${iframePath}?embed=1`;

            // Send navigation command to iframe
            this.iframe.contentWindow.postMessage({
                type: 'navigate',
                path: iframePath,
                url: iframeUrl
            }, '*');

            // Dispatch custom event
            this.dispatchEvent(new CustomEvent('url-synced', {
                detail: { 
                    type: 'parent-to-iframe',
                    path: iframePath, 
                    url: iframeUrl 
                },
                bubbles: true
            }));
        }

        /**
         * Sync initial URL when iframe loads
         */
        syncInitialUrl() {
            const currentPath = window.location.pathname;
            
            // If we have a specific path in the URL, navigate iframe to it
            if (currentPath && currentPath !== '/') {
                setTimeout(() => {
                    this.navigateIframeToPath(currentPath);
                }, 100); // Small delay to ensure iframe is ready
            }
        }

        /**
         * Manually sync URL (useful for programmatic navigation)
         * @param {string} path 
         */
        syncUrl(path) {
            if (this.urlSyncEnabled) {
                this.navigateIframeToPath(path);
                
                // Update parent URL
                const newPath = path.startsWith('/') ? path : '/' + path;
                window.history.pushState({ iframePath: newPath }, '', newPath);
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
        if (options.urlSync) element.setAttribute('url-sync', options.urlSync);

        return element;
    };

    abp.embedding.sendData = function(element, data, targetOrigin) {
        if (element && typeof element.sendData === 'function') {
            element.sendData(data, targetOrigin);
        }
    };

    // Log that the component is loaded
    if (abp.log && abp.log.debug) {
        abp.log.debug('ABP Embedding Web Component loaded');
    }

})(); 