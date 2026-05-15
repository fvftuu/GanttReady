// ============================================================
// render/legend.ts — 图例
// ============================================================

import { svgEl } from '../utils/dom.js';
// types imported by callers, not directly needed here

export function renderLegend(
  parent: SVGElement,
  dims: { svgWidth: number; svgHeight: number }
): void {
  const group = svgEl('g', { class: 'legend' });
  const legendX = dims.svgWidth - 130;
  const legendY = dims.svgHeight - 60;
  const items = [
    { color: '#ff4d4f', label: '关键线路' },
    { color: '#1890ff', label: '非关键线路' },
    { color: '#52c41a', label: '虚工作' },
  ];

  group.appendChild(svgEl('rect', {
    x: legendX - 8, y: legendY - 8,
    width: 120, height: items.length * 20 + 16,
    fill: '#fff', stroke: '#e8e8e8', rx: 4
  }));

  for (let i = 0; i < items.length; i++) {
    const iy = legendY + i * 20;

    group.appendChild(svgEl('line', {
      x1: legendX, y1: iy + 6, x2: legendX + 16, y2: iy + 6,
      stroke: items[i].color, 'stroke-width': 2
    }));

    const t = svgEl('text', {
      x: legendX + 20, y: iy + 10, 'font-size': 10, fill: '#666'
    });
    t.textContent = items[i].label;
    group.appendChild(t);
  }

  parent.appendChild(group);
}

export function renderSCurve(
  parent: SVGElement
): void {
  // TODO: real S-curve implementation
  const group = svgEl('g', { class: 's-curve' });
  parent.appendChild(group);
}
