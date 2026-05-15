// ============================================================
// render/crossarc.ts — 过桥弧检测与绘制
// ============================================================

import { svgEl } from '../utils/dom.js';
import { findSegIntersection } from '../utils/geometry.js';
import type { EventNode, ActivityEdge, NetworkOpts } from '../types.js';

const MARGIN_LEFT = 12;

interface Segment {
  x1: number; y1: number; x2: number; y2: number;
}

export function renderCrossArcs(
  parent: SVGElement,
  activities: ActivityEdge[],
  events: EventNode[],
  opts: NetworkOpts
): void {
  const group = svgEl('g', { class: 'cross-arcs' });
  const dw = opts.dayWidth;
  const eMap: Record<number, EventNode> = {};
  for (const e of events) eMap[e.id] = e;

  const allSegments: { srcId: number; tgtId: number; segs: Segment[] }[] = [];

  for (const act of activities) {
    const src = eMap[act.srcId];
    const tgt = eMap[act.tgtId];
    if (!src || !tgt) continue;

    const sx = MARGIN_LEFT + src.es * dw + (src.offsetX || 0);
    const sy = (src.y ?? 0) + (src.offsetY || 0);
    const ex = MARGIN_LEFT + tgt.es * dw + (tgt.offsetX || 0);
    const ey = (tgt.y ?? 0) + (tgt.offsetY || 0);

    const segs: Segment[] = [];
    if (Math.abs(sy - ey) < 5) {
      segs.push({ x1: sx, y1: sy, x2: ex, y2: ey });
    } else if (ex > sx) {
      segs.push({ x1: sx, y1: sy, x2: ex, y2: sy });
      segs.push({ x1: ex, y1: sy, x2: ex, y2: ey });
    } else {
      const mx = sx + 12;
      segs.push({ x1: sx, y1: sy, x2: mx, y2: sy });
      segs.push({ x1: mx, y1: sy, x2: mx, y2: ey });
      segs.push({ x1: mx, y1: ey, x2: ex, y2: ey });
    }

    allSegments.push({ srcId: act.srcId, tgtId: act.tgtId, segs });
  }

  for (let a = 0; a < allSegments.length; a++) {
    for (const s1 of allSegments[a].segs) {
      for (let b = a + 1; b < allSegments.length; b++) {
        for (const s2 of allSegments[b].segs) {
          const cross = findSegIntersection(
            s1.x1, s1.y1, s1.x2, s1.y2,
            s2.x1, s2.y1, s2.x2, s2.y2
          );
          if (!cross) continue;

          const isAKey = activities.find(
            act => act.srcId === allSegments[a].srcId && act.tgtId === allSegments[a].tgtId
          )?.isCritical ?? false;

          const bSeg = allSegments[b].segs.find(
            seg => seg.x1 === s2.x1 && seg.y1 === s2.y1
          );
          if (!bSeg) continue;

          const bdx = bSeg.x2 - bSeg.x1;
          const blen = Math.sqrt(bdx * bdx + (bSeg.y2 - bSeg.y1) ** 2);
          if (blen < 10) continue;

          const nx = -(bSeg.y2 - bSeg.y1) / blen;
          const arcR = 5;
          const cx = cross.x + nx * arcR;
          const cy = cross.y - ((bSeg.x2 - bSeg.x1) / blen) * arcR;

          const lineW = isAKey ? 2.5 : 1.5;

          // 画弧（白色背景弧覆盖原线）
          const arcEl = svgEl('path', {
            d: `M ${s2.x1} ${s2.y1} Q ${cx} ${cy} ${s2.x2} ${s2.y2}`,
            fill: 'none', stroke: '#fafafa',
            'stroke-width': lineW + 2
          });
          group.appendChild(arcEl);
        }
      }
    }
  }

  parent.appendChild(group);
}
