// Cytoscape Dagre Layout Integration
// This file enables Dagre layout for Cytoscape.js

(function() {
    if (typeof cytoscape === 'undefined') {
        console.warn('Cytoscape not loaded yet');
        return;
    }

    if (typeof dagre === 'undefined') {
        console.warn('Dagre not loaded yet');
        return;
    }

    // Register Dagre layout
    cytoscape('layout', 'dagre', function() {
        // Dagre layout implementation
        console.log('Dagre layout registered');
    });
})();

// Global function to initialize Cytoscape
function initCytoscape(elements) {
    if (typeof cytoscape === 'undefined') {
        console.error('Cytoscape library not loaded');
        return;
    }

    var cy = cytoscape({
        container: document.getElementById('cy'),
        elements: elements,
        style: [
            {
                selector: 'node',
                style: {
                    'label': 'data(label)',
                    'text-valign': 'center',
                    'text-halign': 'center',
                    'background-color': 'data(isCritical) ? "#ff4d4f" : "#1890ff"',
                    'color': '#333',
                    'font-size': '10px',
                    'width': 80,
                    'height': 50,
                    'border-width': 2,
                    'border-color': 'data(isCritical) ? "#cf1322" : "#096dd9"'
                }
            },
            {
                selector: 'edge',
                style: {
                    'width': 2,
                    'line-color': '#ccc',
                    'target-arrow-color': '#ccc',
                    'target-arrow-shape': 'triangle',
                    'curve-style': 'bezier',
                    'label': 'data(label)',
                    'font-size': '9px',
                    'text-background-color': '#fff',
                    'text-background-opacity': 1,
                    'text-background-padding': '3px'
                }
            },
            {
                selector: ':selected',
                style: {
                    'background-color': '#1890ff',
                    'line-color': '#1890ff',
                    'target-arrow-color': '#1890ff'
                }
            }
        ],
        layout: {
            name: 'dagre',
            rankDir: 'TB',
            spacingFactor: 1.2,
            nodeSep: 50,
            rankSep: 80
        },
        wheelSensitivity: 0.2
    });

    // Enable zoom
    cy.zoom({
        level: 1.0,
        renderedPosition: { x: 225, y: 225 }
    });

    return cy;
}
