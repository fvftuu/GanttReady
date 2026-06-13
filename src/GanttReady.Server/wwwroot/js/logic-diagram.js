// logic-diagram.js — 竖排逻辑图 SVG 渲染
// 不依赖任何第三方库，纯原生 JS

(function () {
    'use strict';

    window.renderLogicDiagram = function (containerId, data) {
        if (typeof data === 'string') { try { data = JSON.parse(data); } catch (e) { return; } }
        const container = document.getElementById(containerId);
        if (!container || !data || !data.nodes) return;

        // 移除占位文字
        var ph = document.getElementById('logic-diagram-placeholder');
        if (ph) ph.style.display = 'none';

        var svg = container.querySelector('.logic-diagram-svg');
        if (svg) svg.remove();
        svg = document.createElementNS('http://www.w3.org/2000/svg', 'svg');
        svg.setAttribute('class', 'logic-diagram-svg');
        svg.setAttribute('width', data.totalWidth || 800);
        svg.setAttribute('height', data.totalHeight || 600);
        svg.style.display = 'block';
        svg.style.margin = '0 auto';

        // 箭头定义
        const defs = document.createElementNS('http://www.w3.org/2000/svg', 'defs');
        const marker = document.createElementNS('http://www.w3.org/2000/svg', 'marker');
        marker.setAttribute('id', 'arrowhead');
        marker.setAttribute('markerWidth', '10');
        marker.setAttribute('markerHeight', '7');
        marker.setAttribute('refX', '10');
        marker.setAttribute('refY', '3.5');
        marker.setAttribute('orient', 'auto');
        const arrowPath = document.createElementNS('http://www.w3.org/2000/svg', 'polygon');
        arrowPath.setAttribute('points', '0 0, 10 3.5, 0 7');
        arrowPath.setAttribute('fill', '#666');
        marker.appendChild(arrowPath);
        defs.appendChild(marker);
        svg.appendChild(defs);

        // 画连线
        if (data.edges) {
            data.edges.forEach(function (edge) {
                var from = data.nodes.find(function (n) { return n.taskId === edge.fromTaskId; });
                var to = data.nodes.find(function (n) { return n.taskId === edge.toTaskId; });
                if (!from || !to) return;

                var x1 = from.x + from.width / 2;
                var y1 = from.y + from.height;
                var x2 = to.x + to.width / 2;
                var y2 = to.y;

                var line = document.createElementNS('http://www.w3.org/2000/svg', 'path');
                var d = 'M ' + x1 + ' ' + y1 +
                        ' C ' + x1 + ' ' + (y1 + (y2 - y1) / 2) +
                        ', ' + x2 + ' ' + (y1 + (y2 - y1) / 2) +
                        ', ' + x2 + ' ' + y2;
                line.setAttribute('d', d);
                line.setAttribute('fill', 'none');
                line.setAttribute('stroke', '#666');
                line.setAttribute('stroke-width', '1.5');
                line.setAttribute('marker-end', 'url(#arrowhead)');
                svg.appendChild(line);
            });
        }

        // 画节点
        data.nodes.forEach(function (node) {
            var g = document.createElementNS('http://www.w3.org/2000/svg', 'g');

            // 背景矩形
            var rect = document.createElementNS('http://www.w3.org/2000/svg', 'rect');
            rect.setAttribute('x', node.x);
            rect.setAttribute('y', node.y);
            rect.setAttribute('width', node.width);
            rect.setAttribute('height', node.height);
            rect.setAttribute('rx', '6');
            rect.setAttribute('ry', '6');
            rect.setAttribute('fill', '#f0f5ff');
            rect.setAttribute('stroke', '#4f6ef7');
            rect.setAttribute('stroke-width', '1.5');
            g.appendChild(rect);

            // 代号（左上角）
            var codeText = document.createElementNS('http://www.w3.org/2000/svg', 'text');
            codeText.setAttribute('x', node.x + 10);
            codeText.setAttribute('y', node.y + 18);
            codeText.setAttribute('font-size', '11');
            codeText.setAttribute('fill', '#4f6ef7');
            codeText.setAttribute('font-weight', 'bold');
            codeText.textContent = node.code;
            g.appendChild(codeText);

            // 任务名称
            var nameText = document.createElementNS('http://www.w3.org/2000/svg', 'text');
            nameText.setAttribute('x', node.x + node.width / 2);
            nameText.setAttribute('y', node.y + node.height / 2);
            nameText.setAttribute('font-size', '13');
            nameText.setAttribute('fill', '#333');
            nameText.setAttribute('text-anchor', 'middle');
            nameText.setAttribute('dominant-baseline', 'middle');
            nameText.textContent = node.name;
            g.appendChild(nameText);

            // 日期（底部）
            var dateText = document.createElementNS('http://www.w3.org/2000/svg', 'text');
            dateText.setAttribute('x', node.x + node.width / 2);
            dateText.setAttribute('y', node.y + node.height - 12);
            dateText.setAttribute('font-size', '10');
            dateText.setAttribute('fill', '#999');
            dateText.setAttribute('text-anchor', 'middle');
            var sd = node.planStartDate ? node.planStartDate.substring(0, 10) : '';
            var ed = node.planEndDate ? node.planEndDate.substring(0, 10) : '';
            dateText.textContent = sd + ' → ' + ed;
            g.appendChild(dateText);

            svg.appendChild(g);
        });

        container.appendChild(svg);
    };
})();
