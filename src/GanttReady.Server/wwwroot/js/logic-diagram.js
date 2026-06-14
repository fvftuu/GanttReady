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

        var oldWrap = container.querySelector('.logic-diagram-wrap');
        if (oldWrap) oldWrap.remove();

        var svgWrap = document.createElement('div');
        svgWrap.className = 'logic-diagram-wrap';
        svgWrap.style.transformOrigin = '0 0';
        svgWrap.style.display = 'inline-block';

        var svg = document.createElementNS('http://www.w3.org/2000/svg', 'svg');
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
        // 画连线（箭头旁标注来源任务编号）
        if (data.edges) {
            var inCount = {};
            data.edges.forEach(function (e) { inCount[e.toTaskId] = (inCount[e.toTaskId] || 0) + 1; });
            var inDrawn = {};

            data.edges.forEach(function (edge) {
                var from = data.nodes.find(function (n) { return n.taskId === edge.fromTaskId; });
                var to = data.nodes.find(function (n) { return n.taskId === edge.toTaskId; });
                if (!from || !to) return;

                var code = from.code || '';
                var x1 = from.x + from.width / 2;
                var y1 = from.y + from.height;
                var total = inCount[edge.toTaskId] || 1;
                var idx = inDrawn[edge.toTaskId] || 0;
                inDrawn[edge.toTaskId] = idx + 1;
                var spacing = to.width / (total + 1);
                var x2 = to.x + spacing * (idx + 1);
                var y2 = to.y;

                var d;
                var srcCenter = from.x + from.width / 2;
                var tgtCenter = to.x + to.width / 2;
                if (srcCenter === tgtCenter) {
                    // 同列：直线
                    d = 'M ' + srcCenter + ' ' + y1 + ' L ' + srcCenter + ' ' + (y2 - 6);
                } else {
                    // 判断是否跨行（y间距大于1层+间隙=130px）
                    var bendY = (y2 - y1 > 130) ? y2 - 20 : y1 + (y2 - y1) / 2;
                    d = 'M ' + x1 + ' ' + y1 + ' L ' + x1 + ' ' + bendY + ' L ' + x2 + ' ' + bendY + ' L ' + x2 + ' ' + (y2 - 6);
                }

                var line = document.createElementNS('http://www.w3.org/2000/svg', 'path');
                line.setAttribute('d', d);
                line.setAttribute('fill', 'none');
                line.setAttribute('stroke', '#666');
                line.setAttribute('stroke-width', '1.5');

                var arrowX = (srcCenter === tgtCenter) ? srcCenter : x2;
                var arrow = document.createElementNS('http://www.w3.org/2000/svg', 'polygon');
                arrow.setAttribute('points', (arrowX - 5) + ',' + (y2 - 7) + ' ' + arrowX + ',' + y2 + ' ' + (arrowX + 5) + ',' + (y2 - 7));
                arrow.setAttribute('fill', '#666');
                svg.appendChild(line);
                svg.appendChild(arrow);

                // 标注来源任务编号（箭头左侧）
                var labelX = (srcCenter === tgtCenter) ? srcCenter : x2;
                var label = document.createElementNS('http://www.w3.org/2000/svg', 'text');
                label.setAttribute('x', labelX - 8);
                label.setAttribute('y', y2 - 10);
                label.setAttribute('font-size', '9');
                label.setAttribute('fill', '#999');
                label.setAttribute('text-anchor', 'end');
                label.textContent = code;
                svg.appendChild(label);
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

        svgWrap.appendChild(svg);
        container.appendChild(svgWrap);

        // 保存引用用于缩放/平移
        container._svgWrap = svgWrap;
        container._scale = 1;
        container._panX = 0;
        container._panY = 0;
        UpdateTransform(container);
    };

    function UpdateTransform(container) {
        var wrap = container._svgWrap;
        if (!wrap) return;
        wrap.style.transform = 'translate(' + container._panX + 'px,' + container._panY + 'px) scale(' + container._scale + ')';
        var el = document.getElementById('logic-zoom-level');
        if (el) el.textContent = Math.round(container._scale * 100) + '%';
    }

    // 缩放
    window.logicDiagramZoomIn = function () {
        var c = document.getElementById('logic-diagram-container');
        if (!c) return;
        c._scale = Math.min(c._scale + 0.1, 3);
        UpdateTransform(c);
    };
    window.logicDiagramZoomOut = function () {
        var c = document.getElementById('logic-diagram-container');
        if (!c) return;
        c._scale = Math.max(c._scale - 0.1, 0.2);
        UpdateTransform(c);
    };
    // 鼠标滚轮缩放
    document.addEventListener('wheel', function (e) {
        var c = document.getElementById('logic-diagram-container');
        if (!c || !c.contains(e.target)) return;
        e.preventDefault();
        var delta = e.deltaY > 0 ? -0.05 : 0.05;
        c._scale = Math.max(0.2, Math.min(3, c._scale + delta));
        UpdateTransform(c);
    }, { passive: false });

    // 平移（鼠标拖拽）
    var _drag = null;
    document.addEventListener('mousedown', function (e) {
        var c = document.getElementById('logic-diagram-container');
        if (!c || !c.contains(e.target) || e.target.tagName === 'BUTTON') return;
        _drag = { x: e.clientX, y: e.clientY, px: c._panX, py: c._panY };
        c.style.cursor = 'grabbing';
    });
    document.addEventListener('mousemove', function (e) {
        if (!_drag) return;
        var c = document.getElementById('logic-diagram-container');
        if (!c) return;
        c._panX = _drag.px + (e.clientX - _drag.x);
        c._panY = _drag.py + (e.clientY - _drag.y);
        UpdateTransform(c);
    });
    document.addEventListener('mouseup', function () {
        if (!_drag) return;
        var c = document.getElementById('logic-diagram-container');
        if (c) c.style.cursor = 'grab';
        _drag = null;
    });

    // 导出PNG
    window.logicDiagramExport = function () {
        var c = document.getElementById('logic-diagram-container');
        if (!c) return;
        var wrap = c._svgWrap;
        if (!wrap) return;
        var svg = wrap.querySelector('svg');
        if (!svg) return;

        // 用 SVG 直接导出，避免 html2canvas 截不全
        var svgData = new XMLSerializer().serializeToString(svg);
        var canvas = document.createElement('canvas');
        canvas.width = svg.getAttribute('width') * 2;
        canvas.height = svg.getAttribute('height') * 2;
        var ctx = canvas.getContext('2d');
        ctx.scale(2, 2);
        ctx.fillStyle = '#fff';
        ctx.fillRect(0, 0, canvas.width, canvas.height);

        var img = new Image();
        var blob = new Blob([svgData], { type: 'image/svg+xml;charset=utf-8' });
        var url = URL.createObjectURL(blob);
        img.onload = function () {
            ctx.drawImage(img, 0, 0);
            URL.revokeObjectURL(url);
            var link = document.createElement('a');
            link.download = '逻辑图.png';
            link.href = canvas.toDataURL('image/png');
            link.click();
        };
        img.src = url;
    };
})();
