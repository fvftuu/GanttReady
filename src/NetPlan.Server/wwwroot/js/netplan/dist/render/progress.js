// ============================================================
// render/progress.ts — 前锋线及进度颜色
// ============================================================
import { svgEl } from '../utils/dom.js';
const MARGIN_LEFT = 12;
export function renderProgressLine(parent, events, opts, projectStartDate) {
    if (!opts.showProgressLine)
        return;
    const group = svgEl('g', { class: 'progress-line' });
    const dw = opts.dayWidth;
    // 今天竖线
    const today = new Date();
    const todayOffset = Math.round((today.getTime() - projectStartDate.getTime()) / 86400000);
    if (todayOffset >= 0 && todayOffset <= opts.totalDays) {
        const tx = MARGIN_LEFT + todayOffset * dw;
        group.appendChild(svgEl('line', {
            x1: tx, y1: 0, x2: tx, y2: 2000,
            stroke: '#ff4d4f', 'stroke-width': 1,
            'stroke-dasharray': '4,3', opacity: 0.5
        }));
    }
    // 前锋线
    if (opts.progressDate) {
        const pd = opts.progressDate;
        const savedDate = new Date(pd);
        const progressOffset = Math.round((savedDate.getTime() - projectStartDate.getTime()) / 86400000);
        if (progressOffset >= 0) {
            const pxLine = MARGIN_LEFT + progressOffset * dw;
            group.appendChild(svgEl('line', {
                x1: pxLine, y1: 0, x2: pxLine, y2: 2000,
                stroke: '#52c41a', 'stroke-width': 2,
                'stroke-dasharray': '6,3'
            }));
            const cl = svgEl('text', {
                x: pxLine + 4, y: 14, 'font-size': 11,
                fill: '#52c41a', 'font-weight': 'bold'
            });
            cl.textContent = `前锋线 ${pd}`;
            group.appendChild(cl);
        }
    }
    // 进度三角形标记
    if (opts.progressDate) {
        for (const evt of events) {
            if (evt.isVirtual)
                continue;
            const progDate = new Date(opts.progressDate);
            const progOff = Math.round((progDate.getTime() - projectStartDate.getTime()) / 86400000);
            if (progOff >= evt.es && progOff <= evt.ef) {
                const ratio = evt.ef > evt.es
                    ? (progOff - evt.es) / (evt.ef - evt.es) : 1;
                const progX = MARGIN_LEFT + (evt.es + ratio * (evt.ef - evt.es)) * dw;
                const progY = (evt.y ?? 0) - 4;
                group.appendChild(svgEl('polygon', {
                    points: `${progX - 4},${progY} ${progX + 4},${progY} ${progX},${progY - 6}`,
                    fill: '#52c41a'
                }));
            }
        }
    }
    parent.appendChild(group);
}
export function updateProgressColors(events, progressCheckDate, _projectStartDate) {
    if (!progressCheckDate)
        return;
    // TODO: real progress color update
    console.log('updateProgressColors called with', events.length, 'events');
}
