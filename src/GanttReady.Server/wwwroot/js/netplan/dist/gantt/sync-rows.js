// ============================================================
// gantt/sync-rows.ts — 左右行高同步
// 当左侧行高因内容换行变化时，同步右侧所有元素
// ============================================================
/**
 * 同步左右行高：读取左侧各行实际高度，调整右侧条形位置和高度
 * 应在 Blazor 渲染完成后调用
 */
export function syncGanttRowHeights() {
    const leftRows = document.querySelectorAll('#gantt-left-body .gantt-left-row');
    const bars = document.querySelectorAll('#gantt-right .gantt-bar-frame');
    const gridLines = document.querySelector('#gantt-right .gantt-hgrid');
    const relationSvg = document.querySelector('#gantt-right .gantt-relation-lines');
    const barsContainer = document.querySelector('#gantt-right .gantt-bars');
    const gridLinesContainer = document.querySelector('#gantt-right .gantt-grid-lines');
    if (!leftRows.length || !bars.length || !barsContainer)
        return;
    // 度量每行实际高度和累积 Y
    const rowTops = [];
    const rowHeights = [];
    let cumY = 0;
    leftRows.forEach((row) => {
        rowTops.push(cumY);
        const h = row.offsetHeight;
        rowHeights.push(h);
        cumY += h;
    });
    const totalHeight = cumY;
    // 更新容器高度
    barsContainer.style.height = totalHeight + 'px';
    // 更新网格竖线层高度
    if (gridLinesContainer) {
        gridLinesContainer.style.height = (totalHeight + 40) + 'px';
    }
    // 更新网格线
    if (gridLines) {
        gridLines.style.height = totalHeight + 'px';
        let lineIndex = 0;
        gridLines.querySelectorAll('line').forEach((line) => {
            const r = lineIndex + 1;
            if (r < rowTops.length) {
                line.setAttribute('y1', String(rowTops[r]));
                line.setAttribute('y2', String(rowTops[r]));
            }
            lineIndex++;
        });
    }
    // 更新关系线 SVG
    if (relationSvg) {
        relationSvg.style.height = totalHeight + 'px';
        // 更新今日线 y2
        const todayLine = relationSvg.querySelector('.gantt-today-line');
        if (todayLine) {
            todayLine.setAttribute('y2', String(totalHeight));
        }
        // 更新前锋线折线点
        const progressLine = relationSvg.querySelector('.gantt-progress-line');
        if (progressLine) {
            const pointsStr = progressLine.getAttribute('points') || '';
            const pts = pointsStr.trim().split(/\s+/).map(p => {
                const [x, y] = p.split(',').map(Number);
                return { x, y };
            });
            // 按顺序匹配任务行（进度点按任务顺序排列）
            const newPoints = pts.map((pt, i) => {
                if (i < rowHeights.length) {
                    return `${pt.x.toFixed(1)},${(rowTops[i] + rowHeights[i] / 2).toFixed(1)}`;
                }
                return `${pt.x.toFixed(1)},${pt.y.toFixed(1)}`;
            });
            progressLine.setAttribute('points', newPoints.join(' '));
        }
        // 更新前锋线圆点 cy
        const progressDots = relationSvg.querySelectorAll('.gantt-progress-point');
        progressDots.forEach((dot, i) => {
            if (i < rowHeights.length) {
                dot.setAttribute('cy', (rowTops[i] + rowHeights[i] / 2).toFixed(1));
            }
        });
        // 更新前置关系线 Y 坐标
        const relationGroups = relationSvg.querySelectorAll('[data-relation-pred][data-relation-succ]');
        relationGroups.forEach((group) => {
            const predId = group.getAttribute('data-relation-pred');
            const succId = group.getAttribute('data-relation-succ');
            if (!predId || !succId)
                return;
            const predRow = document.querySelector(`#gantt-left-body .gantt-left-row[data-task-id="${predId}"]`);
            const succRow = document.querySelector(`#gantt-left-body .gantt-left-row[data-task-id="${succId}"]`);
            if (!predRow || !succRow)
                return;
            const rowEls = Array.from(leftRows);
            const predIdx = rowEls.indexOf(predRow);
            const succIdx = rowEls.indexOf(succRow);
            if (predIdx < 0 || succIdx < 0)
                return;
            const predMid = rowTops[predIdx] + rowHeights[predIdx] / 2;
            const succMid = rowTops[succIdx] + rowHeights[succIdx] / 2;
            // 更新 <line> 元素
            const lineEl = group.querySelector('line');
            if (lineEl) {
                lineEl.setAttribute('y1', predMid.toFixed(1));
                lineEl.setAttribute('y2', succMid.toFixed(1));
            }
            // 更新 <path> 元素（d="M x1 y1 L midX y1 L midX y2 L x2 y2"）
            const pathEl = group.querySelector('path');
            if (pathEl) {
                const d = pathEl.getAttribute('d') || '';
                // 匹配 "M x0 Y0 L x1 Y1 L x2 Y2 L x3 Y3" 格式
                const parts = d.match(/M\s+([\d.]+)\s+[\d.]+\s+L\s+([\d.]+)\s+[\d.]+\s+L\s+([\d.]+)\s+[\d.]+\s+L\s+([\d.]+)\s+[\d.]+/);
                if (parts) {
                    const newD = `M ${parts[1]} ${predMid.toFixed(1)} L ${parts[2]} ${predMid.toFixed(1)} L ${parts[3]} ${succMid.toFixed(1)} L ${parts[4]} ${succMid.toFixed(1)}`;
                    pathEl.setAttribute('d', newD);
                }
            }
        });
    }
    // 更新每个任务条的 top 和 height
    bars.forEach((bar) => {
        const taskId = bar.getAttribute('data-task-id');
        if (taskId === null)
            return;
        // 通过 task-id 匹配左侧行（左侧行有 data-task-id 属性）
        const leftRow = document.querySelector(`#gantt-left-body .gantt-left-row[data-task-id="${taskId}"]`);
        if (!leftRow)
            return;
        const rowEls = Array.from(leftRows);
        const idx = rowEls.indexOf(leftRow);
        if (idx < 0 || idx >= rowTops.length)
            return;
        const top = rowTops[idx];
        const h = rowHeights[idx];
        bar.style.top = top + 'px';
        bar.style.height = h + 'px';
    });
}
