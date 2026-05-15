// ============================================================
// render/arrows.ts — 箭线和虚工作绘制
// ============================================================

import { svgEl, svgArrowMarker } from '../utils/dom.js';
import type { EventNode, ActivityEdge, NetworkOpts } from '../types.js';

const MARGIN_LEFT = 12;

/**
 * 绘制所有实箭线
 */
export function renderArrows(
  parent: SVGElement,
  events: EventNode[],
  activities: ActivityEdge[],
  opts: NetworkOpts
): void {
  const group = svgEl('g', { class: 'arrows' });
  const dw = opts.dayWidth;
  const critColor = opts.colors?.critical || '#ff4d4f';
  const normColor = opts.colors?.normal || '#1890ff';
  const critWidth = 2.5;
  const normWidth = 1.5;

  const eMap: Record<number, EventNode> = {};
  for (const e of events) eMap[e.id] = e;

  for (const act of activities) {
    if (act.isDummy) continue;

    const src = eMap[act.srcId];
    const tgt = eMap[act.tgtId];
    if (!src || !tgt) continue;

    const isCrit = act.isCritical;
    const color = isCrit ? critColor : normColor;
    const lineWidth = isCrit ? critWidth : normWidth;

    const sx = MARGIN_LEFT + src.es * dw + (src.offsetX || 0);
    const sy = (src.y ?? 0) + (src.offsetY || 0);
    const ex = MARGIN_LEFT + tgt.es * dw + (tgt.offsetX || 0);
    const ey = (tgt.y ?? 0) + (tgt.offsetY || 0);

    const markerId = svgArrowMarker(isCrit, critColor, normColor);

    let pathD: string;
    if (Math.abs(sy - ey) < 5) {
      pathD = `M ${sx} ${sy} L ${ex} ${ey}`;
    } else if (ex > sx) {
      pathD = `M ${sx} ${sy} L ${ex} ${sy} L ${ex} ${ey}`;
    } else {
      pathD = `M ${sx} ${sy} L ${sx + 12} ${sy} L ${sx + 12} ${ey} L ${ex} ${ey}`;
    }

    const path = svgEl('path', {
      d: pathD, fill: 'none', stroke: color,
      'stroke-width': lineWidth, 'marker-end': markerId
    });
    group.appendChild(path);

    // 标注工期/时差
    const labelX = (sx + ex) / 2;
    const labelY = Math.min(sy, ey) - 8;
    const labelParts: string[] = [];
    if (act.dur >= 0) labelParts.push(`D=${act.dur}`);
    if (act.tf > 0) labelParts.push(`TF=${act.tf}`);

    if (labelParts.length > 0) {
      const label = svgEl('text', {
        x: labelX, y: labelY, 'text-anchor': 'middle',
        'font-size': 9, fill: '#666'
      });
      label.textContent = labelParts.join(' ');
      group.appendChild(label);
    }
  }

  parent.appendChild(group);
}

/**
 * 绘制虚工作
 */
export function renderDummys(
  parent: SVGElement,
  events: EventNode[],
  activities: ActivityEdge[],
  opts: NetworkOpts
): void {
  const group = svgEl('g', { class: 'dummys' });
  const dw = opts.dayWidth;
  const dummyColor = opts.colors?.dummy || '#1890ff';

  const eMap: Record<number, EventNode> = {};
  for (const e of events) eMap[e.id] = e;

  for (const act of activities) {
    if (!act.isDummy) continue;

    const src = eMap[act.srcId];
    const tgt = eMap[act.tgtId];
    if (!src || !tgt) continue;

    const sx = MARGIN_LEFT + src.es * dw + (src.offsetX || 0);
    const sy = (src.y ?? 0) + (src.offsetY || 0);
    const ex = MARGIN_LEFT + tgt.es * dw + (tgt.offsetX || 0);
    const ey = (tgt.y ?? 0) + (tgt.offsetY || 0);

    const markerId = svgArrowMarker(false, undefined, dummyColor);

    let pathD: string;
    if (Math.abs(sy - ey) < 5) {
      pathD = `M ${sx} ${sy} L ${ex} ${ey}`;
    } else {
      const mx = (sx + ex) / 2;
      pathD = `M ${sx} ${sy} L ${mx} ${sy} L ${mx} ${ey} L ${ex} ${ey}`;
    }

    group.appendChild(svgEl('path', {
      d: pathD, fill: 'none', stroke: dummyColor,
      'stroke-width': 1.5, 'stroke-dasharray': '6,3',
      'marker-end': markerId
    }));
  }

  parent.appendChild(group);
}
