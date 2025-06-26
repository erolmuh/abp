(function() {
    'use strict';

    // Check if we're in an iframe
    if (window.self === window.top) {
        return; // Not in iframe, exit
    }

    /**
     * ABP Embedding Iframe Content Script
     * Handles height measurement and communication with parent
     */
    class AbpEmbeddingIframeHandler {
        constructor() {
            this.config = {
                minHeight: 100,
                maxHeight: 10000,
                watchForChanges: true,
                debounceDelay: 250
            };
            this.isEnabled = false;
            this.observer = null;
            this.debounceTimer = null;
            this.lastReportedHeight = 0;

            // Bind methods
            this.handleMessage = this.handleMessage.bind(this);
            this.measureHeight = this.measureHeight.bind(this);
            this.reportHeight = this.reportHeight.bind(this);
            this.debouncedReportHeight = this.debouncedReportHeight.bind(this);
        }

        init() {
            // Listen for messages from parent
            window.addEventListener('message', this.handleMessage);

            // If we're in an iframe, proactively enable with defaults
            // This helps with race conditions where parent hasn't sent config yet
            if (window.self !== window.top) {
                setTimeout(() => {
                    if (!this.isEnabled) {
                        console.debug('ABP Iframe: Proactively enabling auto-height (iframe detected)');
                        this.configure({});
                        this.enable();
                    }
                }, 50);
            }

            // Wait for DOM to be ready
            if (document.readyState === 'loading') {
                document.addEventListener('DOMContentLoaded', () => {
                    this.onDomReady();
                });
            } else {
                this.onDomReady();
            }
        }

        onDomReady() {
            // Wait for all content to load (including images)
            if (document.readyState === 'complete') {
                this.onPageFullyLoaded();
            } else {
                window.addEventListener('load', () => {
                    this.onPageFullyLoaded();
                });
            }
        }

        onPageFullyLoaded() {
            // Wait for layout to complete with multiple strategies
            this.waitForLayoutCompletion().then(() => {
                // Send ready signal to parent first
                this.sendReadySignal();
                
                // Then report height
                this.reportHeight();
                
                // Also set up a retry mechanism in case the first measurement was off
                setTimeout(() => {
                    this.retryInitialHeight();
                }, 1000);
            });
        }

        sendReadySignal() {
            // Tell parent that iframe is ready and can handle auto-height
            if (window.parent && window.parent !== window) {
                window.parent.postMessage({
                    type: 'iframe-ready-for-auto-height',
                    timestamp: Date.now(),
                    initialHeight: this.measureHeight()
                }, '*');
            }
        }

        async waitForLayoutCompletion() {
            // Strategy 1: Wait for any pending images
            await this.waitForImages();
            
            // Strategy 2: Use requestAnimationFrame to ensure rendering is complete
            await new Promise(resolve => {
                requestAnimationFrame(() => {
                    requestAnimationFrame(() => {
                        resolve();
                    });
                });
            });
            
            // Strategy 3: Small delay to ensure CSS transitions/animations are done
            await new Promise(resolve => setTimeout(resolve, 250));
            
            // Strategy 4: Wait for fonts to load if supported
            if (document.fonts && document.fonts.ready) {
                try {
                    await document.fonts.ready;
                } catch (error) {
                    // Font loading API not supported, continue
                }
            }
        }

        async waitForImages() {
            const images = document.querySelectorAll('img');
            const imagePromises = Array.from(images).map(img => {
                if (img.complete) {
                    return Promise.resolve();
                }
                
                return new Promise(resolve => {
                    const timeout = setTimeout(() => {
                        resolve(); // Don't wait forever for broken images
                    }, 3000);
                    
                    img.addEventListener('load', () => {
                        clearTimeout(timeout);
                        resolve();
                    });
                    
                    img.addEventListener('error', () => {
                        clearTimeout(timeout);
                        resolve();
                    });
                });
            });
            
            await Promise.all(imagePromises);
        }

        retryInitialHeight() {
            if (!this.isEnabled) {
                return;
            }

            const currentHeight = this.measureHeight();
            
            // If the height seems too small, it might have been measured too early
            if (currentHeight < 200 && this.lastReportedHeight > 0) {
                const heightDifference = Math.abs(currentHeight - this.lastReportedHeight);
                
                // If there's a significant difference, report the new height
                if (heightDifference > 50) {
                    console.debug('ABP Iframe: Retrying height measurement due to significant change');
                    this.reportHeight();
                }
            }
        }

        handleMessage(event) {
            if (!event.data || typeof event.data !== 'object') {
                return;
            }

            switch (event.data.type) {
                case 'auto-height-config':
                    this.configure(event.data.config);
                    this.enable();
                    break;
                case 'auto-height-disable':
                    this.disable();
                    break;
                case 'request-height-update':
                    this.reportHeight();
                    break;
            }
        }

        configure(config) {
            this.config = { ...this.config, ...config };
        }

        enable() {
            if (this.isEnabled) {
                return;
            }

            this.isEnabled = true;

            // Set up change monitoring if enabled
            if (this.config.watchForChanges) {
                this.setupChangeMonitoring();
            }

            // Send ready signal and report initial height
            this.sendReadySignal();
            
            // Use a small delay to ensure parent receives ready signal first
            setTimeout(() => {
                this.reportHeight();
            }, 50);
        }

        disable() {
            this.isEnabled = false;

            // Clean up observers
            if (this.observer) {
                this.observer.disconnect();
                this.observer = null;
            }

            // Clear debounce timer
            if (this.debounceTimer) {
                clearTimeout(this.debounceTimer);
                this.debounceTimer = null;
            }
        }

        setupChangeMonitoring() {
            // Use ResizeObserver if available (modern browsers)
            if (window.ResizeObserver) {
                this.observer = new ResizeObserver(() => {
                    this.debouncedReportHeight();
                });
                this.observer.observe(document.body);
            } else {
                // Fallback: Use MutationObserver for older browsers
                this.observer = new MutationObserver(() => {
                    this.debouncedReportHeight();
                });
                
                this.observer.observe(document.body, {
                    childList: true,
                    subtree: true,
                    attributes: true,
                    attributeFilter: ['style', 'class']
                });
            }

            // Also monitor window resize
            window.addEventListener('resize', this.debouncedReportHeight);
        }

        measureHeight() {
            // Force layout recalculation
            document.body.offsetHeight;

            // Fix common CSS issues that interfere with height measurement
            this.fixCSSIssues();

            // Try multiple methods to get the most accurate height
            const methods = [
                () => {
                    // Method 1: Body scroll height (most reliable for content)
                    return Math.max(document.body.scrollHeight, document.body.offsetHeight);
                },
                () => {
                    // Method 2: Document element height
                    return Math.max(document.documentElement.scrollHeight, document.documentElement.offsetHeight);
                },
                () => {
                    // Method 3: Bounding rect of body
                    const bodyRect = document.body.getBoundingClientRect();
                    return bodyRect.height + window.pageYOffset;
                },
                () => {
                    // Method 4: Walk through all visible elements
                    const elements = document.querySelectorAll('body *');
                    let maxBottom = 0;
                    
                    for (const element of elements) {
                        const style = window.getComputedStyle(element);
                        if (style.display === 'none' || style.visibility === 'hidden') {
                            continue;
                        }
                        
                        const rect = element.getBoundingClientRect();
                        const bottom = rect.bottom + window.pageYOffset;
                        if (bottom > maxBottom) {
                            maxBottom = bottom;
                        }
                    }
                    
                    // Add body margins/padding
                    const bodyStyle = window.getComputedStyle(document.body);
                    const bodyMarginBottom = parseFloat(bodyStyle.marginBottom) || 0;
                    const bodyPaddingBottom = parseFloat(bodyStyle.paddingBottom) || 0;
                    
                    return maxBottom + bodyMarginBottom + bodyPaddingBottom;
                },
                () => {
                    // Method 5: Custom height calculation including margins
                    const body = document.body;
                    const html = document.documentElement;
                    
                    const height = Math.max(
                        body.scrollHeight,
                        body.offsetHeight,
                        html.clientHeight,
                        html.scrollHeight,
                        html.offsetHeight
                    );
                    
                    return height;
                }
            ];

            let maxHeight = 0;
            const heights = [];
            
            for (const method of methods) {
                try {
                    const height = method();
                    if (height && height > 0) {
                        heights.push(height);
                        if (height > maxHeight) {
                            maxHeight = height;
                        }
                    }
                } catch (error) {
                    console.warn('Height measurement method failed:', error);
                }
            }

            // If we got very different heights, log them for debugging
            if (heights.length > 1) {
                const min = Math.min(...heights);
                const max = Math.max(...heights);
                if (max - min > 100) {
                    console.debug('Height measurement variance:', heights);
                }
            }

            // Fallback if no height was measured
            if (maxHeight === 0) {
                maxHeight = window.innerHeight || 400;
                console.warn('Could not measure content height, using fallback:', maxHeight);
            }

            // Add padding to ensure no content is cut off
            maxHeight += 20;

            // Ensure height is within configured bounds
            return Math.max(
                this.config.minHeight,
                Math.min(maxHeight, this.config.maxHeight)
            );
        }

        fixCSSIssues() {
            // Common CSS fixes that interfere with height measurement
            const html = document.documentElement;
            const body = document.body;

            // Store original styles to avoid overriding intentional styles
            if (!this.originalStyles) {
                this.originalStyles = {
                    htmlHeight: html.style.height,
                    bodyHeight: body.style.height,
                    htmlOverflow: html.style.overflow,
                    bodyOverflow: body.style.overflow
                };
            }

            // Temporarily fix height issues for measurement
            const htmlStyle = window.getComputedStyle(html);
            const bodyStyle = window.getComputedStyle(body);

            // If html/body have 100% height, it can interfere with scrollHeight
            if (htmlStyle.height === '100%' || htmlStyle.height === '100vh') {
                html.style.height = 'auto';
            }
            if (bodyStyle.height === '100%' || bodyStyle.height === '100vh') {
                body.style.height = 'auto';
            }

            // Ensure overflow is visible for accurate measurement
            if (htmlStyle.overflow === 'hidden') {
                html.style.overflow = 'visible';
            }
            if (bodyStyle.overflow === 'hidden') {
                body.style.overflow = 'visible';
            }

            // Force layout recalculation
            body.offsetHeight;

            // Restore styles after a brief moment (after measurement)
            setTimeout(() => {
                if (this.originalStyles) {
                    if (this.originalStyles.htmlHeight) html.style.height = this.originalStyles.htmlHeight;
                    if (this.originalStyles.bodyHeight) body.style.height = this.originalStyles.bodyHeight;
                    if (this.originalStyles.htmlOverflow) html.style.overflow = this.originalStyles.htmlOverflow;
                    if (this.originalStyles.bodyOverflow) body.style.overflow = this.originalStyles.bodyOverflow;
                }
            }, 10);
        }

        reportHeight() {
            if (!this.isEnabled) {
                return;
            }

            const height = this.measureHeight();

            // Only report if height has changed significantly (avoid unnecessary updates)
            // But be more lenient for the first measurement
            const threshold = this.lastReportedHeight === 0 ? 1 : 5;
            if (Math.abs(height - this.lastReportedHeight) < threshold) {
                return;
            }

            console.debug('ABP Iframe: Reporting height:', height, '(previous:', this.lastReportedHeight, ')');
            
            this.lastReportedHeight = height;

            // Send height to parent
            window.parent.postMessage({
                type: 'height-update',
                height: height,
                timestamp: Date.now(),
                isInitial: this.lastReportedHeight === height // Flag if this is the first measurement
            }, '*');
        }

        debouncedReportHeight() {
            if (this.debounceTimer) {
                clearTimeout(this.debounceTimer);
            }

            this.debounceTimer = setTimeout(() => {
                this.reportHeight();
                this.debounceTimer = null;
            }, this.config.debounceDelay);
        }
    }

    // Create and initialize the handler
    const handler = new AbpEmbeddingIframeHandler();
    handler.init();

    // Expose handler globally for debugging
    window.abpEmbeddingIframeHandler = handler;

    // Auto-enable if embed parameter is present in URL
    const urlParams = new URLSearchParams(window.location.search);
    if (urlParams.get('embed') === '1') {
        // Enable with default config
        handler.configure({});
        handler.enable();
    }

    // Also try to auto-detect if we're in an iframe and enable automatically
    // This helps with cases where the embed parameter isn't set but auto-height is expected
    setTimeout(() => {
        if (window.self !== window.top && !handler.isEnabled) {
            console.debug('ABP Iframe: Auto-enabling iframe handler (detected iframe context)');
            handler.configure({});
            handler.enable();
        }
    }, 100);

})(); 