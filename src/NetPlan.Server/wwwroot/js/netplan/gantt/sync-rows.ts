// ============================================================
// gantt/sync-rows.ts — 左右行高同步
// 当左侧行高因内容换行变化时，同步右侧所有元素
// ============================================================

/**
 * 同步左右行高：读取左侧各行实际高度，调整右侧条形位置和高度
 * 应在 Blazor 渲染完成后调用
 */
export function syncGanttRowHeights(): void {
  const leftRows = document.querySelectorAll('#gantt-left-body .gantt-left-row');
  const bars = document.querySelectorAll('#gantt-right .gantt-bar-frame');
  const gridLines = document.querySelector('#gantt-right .gantt-hgrid') as SVGSVGElement;
  const relationSvg = document.querySelector('#gantt-right .gantt-relation-lines') as SVGSVGElement;
  const barsContainer = document.querySelector('#gantt-right .gantt-bars') as HTMLElement;
  const gridLinesContainer = document.querySelector('#gantt-right .gantt-grid-lines') as HTMLElement;

  if (!leftRows.length || !bars.length || !barsContainer) return;

  // 度量每行实际高度和累积 Y
  const rowTops: number[] = [];
  const rowHeights: number[] = [];
  let cumY = 0;
  leftRows.forEach((row) => {
    rowTops.push(cumY);
    const h = (row as HTMLElement).offsetHeight;
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

  // 绘制行背景（隔行变色）和水平分隔线
  let hlines = barsContainer.querySelector('.gantt-hlines');
  if (!hlines) {
    hlines = document.createElement('div');
    hlines.className = 'gantt-hlines';
    barsContainer.appendChild(hlines);
  }
  hlines.innerHTML = '';
  for (let ri = 0; ri < rowTops.length; ri++) {
    const top = rowTops[ri];
    const h = rowHeights[ri];

    // zebra 背景
    if (ri % 2 === 1) {
      const bg = document.createElement('div');
      bg.className = 'gantt-hline-bg';
      bg.style.top = top + 'px';
      bg.style.height = h + 'px';
      hlines.appendChild(bg);
    }

    // 水平分隔线（每行底部）
    if (ri > 0) {
      const line = document.createElement('div');
      line.className = 'gantt-hline';
      line.style.top = (top - 0.5) + 'px';
      hlines.appendChild(line);
    }
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
    const progressLine = relationSvg.querySelector('.gantt-progress-line') as SVGPolylineElement;
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

    // 先更新条形位置（确保关系线能读到正确的坐标）
    bars.forEach((bar) => {
      const taskId = bar.getAttribute('data-task-id');
      if (taskId === null) return;
      const leftRow = document.querySelector(`#gantt-left-body .gantt-left-row[data-task-id="${taskId}"]`);
      if (!leftRow) return;
      const rowEls = Array.from(leftRows);
      const idx = rowEls.indexOf(leftRow as Element);
      if (idx < 0 || idx >= rowTops.length) return;
      (bar as HTMLElement).style.top = rowTops[idx] + 'px';
      (bar as HTMLElement).style.height = rowHeights[idx] + 'px';
    });

    // 再更新前置关系线的 X/Y 坐标（此时条形已在正确位置）
    const relationGroups = relationSvg.querySelectorAll('[data-relation-pred][data-relation-succ]');
    relationGroups.forEach((group) => {
      const predId = group.getAttribute('data-relation-pred');
      const succId = group.getAttribute('data-relation-succ');
      if (!predId || !succId) return;

      const predBar = document.querySelector(`#gantt-right .gantt-bar-frame[data-task-id="${predId}"]`) as HTMLElement;
      const succBar = document.querySelector(`#gantt-right .gantt-bar-frame[data-task-id="${succId}"]`) as HTMLElement;

      const predRow = document.querySelector(`#gantt-left-body .gantt-left-row[data-task-id="${predId}"]`);
      const succRow = document.querySelector(`#gantt-left-body .gantt-left-row[data-task-id="${succId}"]`);
      if (!predRow || !succRow) return;

      const rowEls = Array.from(leftRows);
      const predIdx = rowEls.indexOf(predRow as Element);
      const succIdx = rowEls.indexOf(succRow as Element);
      if (predIdx < 0 || succIdx < 0) return;

      const predMid = rowTops[predIdx] + rowHeights[predIdx] / 2;
      const succMid = rowTops[succIdx] + rowHeights[succIdx] / 2;

      // 从条形的 left/width 计算关系线 X 端点
      let barEdgeX = 0, endX = 0;
      if (predBar && succBar) {
        const predLeft = parseFloat(predBar.style.left) || 0;
        const predW = parseFloat(predBar.style.width) || 0;
        const succLeft = parseFloat(succBar.style.left) || 0;
        barEdgeX = predLeft + predW;
        endX = succLeft;
      }

      // 更新 <line> 元素（同行）
      const lineEl = group.querySelector('line');
      if (lineEl) {
        lineEl.setAttribute('x1', barEdgeX.toFixed(1));
        lineEl.setAttribute('y1', predMid.toFixed(1));
        lineEl.setAttribute('x2', endX.toFixed(1));
        lineEl.setAttribute('y2', succMid.toFixed(1));
        lineEl.setAttribute('stroke-width', '0.5');
        lineEl.setAttribute('stroke-dasharray', '3,3');
      }

      // 更新 <path> 元素（竖直线→水平线）
      const pathEl = group.querySelector('path');
      if (pathEl) {
        const newD = `M ${barEdgeX.toFixed(1)} ${predMid.toFixed(1)} L ${barEdgeX.toFixed(1)} ${succMid.toFixed(1)} L ${endX.toFixed(1)} ${succMid.toFixed(1)}`;
        pathEl.setAttribute('d', newD);
        pathEl.setAttribute('stroke-width', '0.5');
        pathEl.setAttribute('stroke-dasharray', '3,3');
      }
    });
  }
}

/**
 * 延迟版同步：等待浏览器 layout 完成后再执行
 * 双 rAF + 防抖：确保 Blazor Server SignalR 批量 DOM 更新 + layout 全部完成后执行
 */
let _syncPending = false;
export function syncGanttRowHeightsDeferred(): void {
  if (_syncPending) return;
  _syncPending = true;
  requestAnimationFrame(() => {
    requestAnimationFrame(() => {
      _syncPending = false;
      syncGanttRowHeights();
    });
  });
}
