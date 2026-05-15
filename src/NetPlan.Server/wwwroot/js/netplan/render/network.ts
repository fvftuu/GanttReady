// ============================================================
// render/network.ts — 网络图主渲染流程
// ============================================================

import { svgEl, clearChildren } from '../utils/dom.js';
import { renderTimeline } from './timeline.js';
import { renderEventNodes } from './nodes.js';
import { renderArrows, renderDummys } from './arrows.js';
import { renderCrossArcs } from './crossarc.js';
import { renderProgressLine } from './progress.js';
import { renderLegend } from './legend.js';
import type { EventNode, ActivityEdge, NetworkOpts } from '../types.js';

const MARGIN_LEFT = 12;
const ROW_LABEL_WIDTH = 80;

export function calcSvgDimensions(
  opts: NetworkOpts,
  maxLayer: number
) {
  const totalPx = opts.dayWidth * opts.totalDays;
  const marginTop = opts.mode === 'time' ? 55 : 20;
  const netLayerHeight = opts.layerHeight || 60;
  const totalHeight = marginTop + (maxLayer + 1) * netLayerHeight;

  return {
    svgWidth: MARGIN_LEFT + totalPx + ROW_LABEL_WIDTH,
    svgHeight: totalHeight,
    netTop: marginTop,
    netLayerHeight
  };
}

export function buildNetworkSvg(
  parent: SVGSVGElement,
  events: EventNode[],
  activities: ActivityEdge[],
  opts: NetworkOpts,
  projectStartDate: Date
): void {
  clearChildren(parent);

  const maxLayer = Math.max(...events.map(e => e.layer ?? 0));
  const dims = calcSvgDimensions(opts, maxLayer);

  parent.setAttribute('width', String(dims.svgWidth));
  parent.setAttribute('height', String(dims.svgHeight));
  parent.classList.add('network-svg');

  // 背景
  parent.appendChild(svgEl('rect', {
    x: 0, y: 0, width: dims.svgWidth, height: dims.svgHeight,
    fill: '#fafafa', stroke: '#e8e8e8'
  }));

  // 时标尺
  if (opts.mode === 'time') {
    renderTimeline(parent, opts, projectStartDate);
  }

  // 行背景
  renderRowBackgrounds(parent, events, opts, dims);

  // 箭线
  renderArrows(parent, events, activities, opts);

  // 虚工作
  renderDummys(parent, events, activities, opts);

  // 过桥弧
  renderCrossArcs(parent, activities, events, opts);

  // 事件节点
  renderEventNodes(parent, events, opts);

  // 前锋线
  renderProgressLine(parent, events, opts, projectStartDate);

  // 图例
  renderLegend(parent, dims);

  // 总工期
  renderTotalDuration(parent, dims);

  // 底部标尺
  renderBottomRuler(parent, opts, projectStartDate, dims);
}

function renderRowBackgrounds(
  parent: SVGElement,
  events: EventNode[],
  opts: NetworkOpts,
  dims: ReturnType<typeof calcSvgDimensions>
): void {
  const bgGroup = svgEl('g', { class: 'row-bg' });
  const layers = [...new Set(events.map(e => e.layer ?? 0))].sort((a, b) => a - b);

  for (const layer of layers) {
    const y = dims.netTop + layer * dims.netLayerHeight;
    const isEven = layer % 2 === 0;

    bgGroup.appendChild(svgEl('rect', {
      x: 0, y, width: dims.svgWidth, height: dims.netLayerHeight,
      fill: isEven ? '#ffffff' : '#f5f5f5', opacity: 0.5
    }));

    if (opts.mode === 'logic') {
      const t = svgEl('text', {
        x: 4, y: y + dims.netLayerHeight / 2 + 4,
        'font-size': 10, fill: '#999'
      });
      t.textContent = `L${layer}`;
      bgGroup.appendChild(t);
    }
  }

  parent.insertBefore(bgGroup, parent.firstChild?.nextSibling || null);
}

function renderTotalDuration(
  parent: SVGElement,
  dims: ReturnType<typeof calcSvgDimensions>
): void {
  const group = svgEl('g', { class: 'total-duration' });
  const y = dims.svgHeight - 25;

  const t = svgEl('text', {
    x: dims.svgWidth / 2, y, 'text-anchor': 'middle',
    'font-size': 12, fill: '#333', 'font-weight': 'bold'
  });
  t.textContent = `总工期: -- 天`; // 需传入 real total
  group.appendChild(t);
  parent.appendChild(group);
}

function renderBottomRuler(
  parent: SVGElement,
  opts: NetworkOpts,
  projectStartDate: Date,
  dims: ReturnType<typeof calcSvgDimensions>
): void {
  if (opts.mode !== 'time') return;

  const group = svgEl('g', { class: 'bottom-ruler' });
  const y = dims.svgHeight - 14;
  const dw = opts.dayWidth;

  let lastMonth = -1;
  for (let d = 0; d <= opts.totalDays; d += 7) {
    const dt = new Date(projectStartDate.getTime() + d * 86400000);
    const x = MARGIN_LEFT + d * dw;
    const month = dt.getMonth();

    if (month !== lastMonth) {
      lastMonth = month;
      const t = svgEl('text', { x, y, 'font-size': 10, fill: '#999' });
      t.textContent = ` ${dt.getFullYear()}-${String(month + 1).padStart(2, '0')}`;
      group.appendChild(t);
    }
  }

  parent.appendChild(group);
}
