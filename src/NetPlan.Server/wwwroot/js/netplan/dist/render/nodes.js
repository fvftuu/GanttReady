// ============================================================
// render/nodes.ts — 事件节点绘制
// ============================================================
import { svgEl } from '../utils/dom.js';
const MARGIN_LEFT = 12;
const NODE_RADIUS = 14;
export function renderEventNodes(parent, events, opts) {
    const group = svgEl('g', { class: 'event-nodes' });
    const dw = opts.dayWidth;
    const nr = opts.nodeRadius || NODE_RADIUS;
    const critFill = opts.colors?.critical || '#ff4d4f';
    const normFill = opts.colors?.normal || '#1890ff';
    for (const evt of events) {
        if (evt.isVirtual)
            continue;
        const evtX = MARGIN_LEFT + evt.es * dw + (evt.offsetX || 0);
        const evtY = (evt.y ?? 0) + (evt.offsetY || 0);
        const isCrit = evt.isCritical;
        const stroke = isCrit ? critFill : normFill;
        if (isCrit) {
            group.appendChild(svgEl('circle', {
                cx: evtX, cy: evtY, r: nr,
                fill: '#fff', stroke: critFill, 'stroke-width': 3
            }));
            group.appendChild(svgEl('circle', {
                cx: evtX, cy: evtY, r: nr - 4,
                fill: critFill, stroke: critFill, 'stroke-width': 1.5
            }));
        }
        else {
            group.appendChild(svgEl('circle', {
                cx: evtX, cy: evtY, r: nr,
                fill: '#fff', stroke, 'stroke-width': 1.5
            }));
        }
        // 编号
        const t = svgEl('text', {
            x: evtX, y: evtY + 4, 'text-anchor': 'middle',
            'font-size': 11, fill: isCrit ? '#fff' : '#333', 'font-weight': 'bold'
        });
        t.textContent = String(evt.id);
        group.appendChild(t);
        // ET
        const et = svgEl('text', {
            x: evtX, y: evtY + nr + 12, 'text-anchor': 'middle',
            'font-size': 9, fill: '#666'
        });
        et.textContent = `ET=${evt.es}`;
        group.appendChild(et);
    }
    parent.appendChild(group);
}
