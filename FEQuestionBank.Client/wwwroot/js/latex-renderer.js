// LaTeX Renderer using KaTeX
// Provides functions for Blazor JS Interop to render LaTeX formulas

window.LatexRenderer = {
    // Render LaTeX in a specific element by ID
    renderElement: function (elementId) {
        const element = document.getElementById(elementId);
        if (element && typeof renderMathInElement === 'function') {
            renderMathInElement(element, {
                delimiters: [
                    { left: '$$', right: '$$', display: true },
                    { left: '$', right: '$', display: false },
                    { left: '\\[', right: '\\]', display: true },
                    { left: '\\(', right: '\\)', display: false },
                    { left: '\\begin{equation}', right: '\\end{equation}', display: true },
                    { left: '\\begin{align}', right: '\\end{align}', display: true },
                    { left: '\\begin{align*}', right: '\\end{align*}', display: true }
                ],
                throwOnError: false,
                errorColor: '#cc0000',
                strict: false,
                trust: true,
                macros: {
                    "\\RR": "\\mathbb{R}",
                    "\\NN": "\\mathbb{N}",
                    "\\ZZ": "\\mathbb{Z}",
                    "\\QQ": "\\mathbb{Q}",
                    "\\CC": "\\mathbb{C}"
                }
            });
        }
        return true;
    },

    // Render LaTeX in all elements with a specific class
    renderByClass: function (className) {
        const elements = document.getElementsByClassName(className);
        if (typeof renderMathInElement === 'function') {
            for (let element of elements) {
                renderMathInElement(element, {
                    delimiters: [
                        { left: '$$', right: '$$', display: true },
                        { left: '$', right: '$', display: false },
                        { left: '\\[', right: '\\]', display: true },
                        { left: '\\(', right: '\\)', display: false },
                        { left: '\\begin{equation}', right: '\\end{equation}', display: true },
                        { left: '\\begin{align}', right: '\\end{align}', display: true },
                        { left: '\\begin{align*}', right: '\\end{align*}', display: true }
                    ],
                    throwOnError: false,
                    errorColor: '#cc0000',
                    strict: false,
                    trust: true
                });
            }
        }
        return true;
    },

    // Render LaTeX string to HTML string
    renderToString: function (latexString, displayMode) {
        if (typeof katex === 'undefined') {
            console.warn('KaTeX not loaded yet');
            return latexString;
        }
        
        try {
            return katex.renderToString(latexString, {
                displayMode: displayMode || false,
                throwOnError: false,
                errorColor: '#cc0000',
                strict: false,
                trust: true
            });
        } catch (e) {
            console.error('KaTeX render error:', e);
            return latexString;
        }
    },

    // Auto-render all LaTeX on page
    renderPage: function () {
        if (typeof renderMathInElement === 'function') {
            renderMathInElement(document.body, {
                delimiters: [
                    { left: '$$', right: '$$', display: true },
                    { left: '$', right: '$', display: false },
                    { left: '\\[', right: '\\]', display: true },
                    { left: '\\(', right: '\\)', display: false }
                ],
                throwOnError: false,
                errorColor: '#cc0000',
                strict: false,
                trust: true
            });
        }
        return true;
    },

    // Check if KaTeX is loaded
    isLoaded: function () {
        return typeof katex !== 'undefined' && typeof renderMathInElement !== 'undefined';
    },

    // Wait for KaTeX to load then render element
    renderWhenReady: function (elementId, maxRetries = 10, retryInterval = 100) {
        return new Promise((resolve, reject) => {
            let retries = 0;
            
            const tryRender = () => {
                if (this.isLoaded()) {
                    this.renderElement(elementId);
                    resolve(true);
                } else if (retries < maxRetries) {
                    retries++;
                    setTimeout(tryRender, retryInterval);
                } else {
                    console.warn('KaTeX failed to load after retries');
                    resolve(false);
                }
            };
            
            tryRender();
        });
    }
};

