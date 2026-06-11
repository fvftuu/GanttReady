// ============================================================
// render/timeline.ts — 时标尺渲染
// ============================================================
import { svgEl } from '../utils/dom.js';
import { getISOWeek } from '../utils/dates.js';
export const TIMELINE = {
    UPPER_HEIGHT: 28,
    LOWER_HEIGHT: 24,
    TOTAL_HEIGHT: 52,
    SATURDAY_FILL: '#e6f7ff',
    SUNDAY_FILL: '#fff0f0',
};
export function renderTimeline(parent, opts, projectStartDate) {
    const dw = opts.dayWidth;
    const sd = projectStartDate;
    const isTimeMode = opts.mode === 'time';
    const tsm = opts.timeScaleMode;
    if (!isTimeMode)
        return 0;
    const upperH = TIMELINE.UPPER_HEIGHT;
    const lowerH = TIMELINE.LOWER_HEIGHT;
    const totalH = upperH + lowerH;
    const labelFontSize = 11;
    const MARGIN_LEFT = 12;
    const group = svgEl('g', { class: 'timeline' });
    // 标准模式
    if (tsm === 0) {
        let lastYear = -1;
        for (let d = 0; d <= opts.totalDays; d++) {
            const dt = new Date(sd.getTime() + d * 86400000);
            const x = MARGIN_LEFT + d * dw;
            const cy = upperH / 2 + 4;
            if (dt.getDate() === 1 || d === 0) {
                const monthSpan = new Date(dt.getFullYear(), dt.getMonth() + 1, 0).getDate();
                const mw = monthSpan * dw;
                const t = svgEl('text', {
                    x: x + mw / 2, y: cy, 'text-anchor': 'middle',
                    'font-size': labelFontSize, fill: '#333', 'font-weight': 'bold'
                });
                t.textContent = `${dt.getFullYear()}-${String(dt.getMonth() + 1).padStart(2, '0')}`;
                group.appendChild(t);
                if (dt.getFullYear() !== lastYear) {
                    lastYear = dt.getFullYear();
                    const yt = svgEl('text', { x, y: cy - 14, 'font-size': 10, fill: '#999' });
                    yt.textContent = ` ${lastYear}年`;
                    group.appendChild(yt);
                }
            }
        }
        let lastW = -1;
        for (let d = 0; d <= opts.totalDays; d++) {
            const dt = new Date(sd.getTime() + d * 86400000);
            const x = MARGIN_LEFT + d * dw;
            const dow = dt.getDay();
            const isSat = dow === 6;
            const isSun = dow === 0;
            if (isSat || isSun) {
                group.appendChild(svgEl('rect', {
                    x, y: upperH, width: dw, height: lowerH,
                    fill: isSat ? TIMELINE.SATURDAY_FILL : TIMELINE.SUNDAY_FILL,
                    opacity: 0.1
                }));
            }
            const t = svgEl('text', {
                x: x + dw / 2, y: upperH + lowerH / 2 + 4,
                'text-anchor': 'middle', 'font-size': 10,
                fill: isSat || isSun ? '#ccc' : '#666'
            });
            t.textContent = String(dt.getDate());
            group.appendChild(t);
            const week = getISOWeek(dt);
            if (week !== lastW) {
                lastW = week;
                const wt = svgEl('text', { x, y: upperH + lowerH + 12, 'font-size': 9, fill: '#aaa' });
                wt.textContent = `W${week}`;
                group.appendChild(wt);
            }
        }
    }
    // 模式1
    else if (tsm === 1) {
        let lastYear = -1;
        for (let d = 0; d <= opts.totalDays; d++) {
            const dt = new Date(sd.getTime() + d * 86400000);
            const x = MARGIN_LEFT + d * dw;
            if (dt.getDate() === 1 || d === 0) {
                const monthSpan = new Date(dt.getFullYear(), dt.getMonth() + 1, 0).getDate();
                const mw = monthSpan * dw;
                const t = svgEl('text', {
                    x: x + mw / 2, y: upperH / 2 + 4, 'text-anchor': 'middle',
                    'font-size': labelFontSize, fill: '#333', 'font-weight': 'bold'
                });
                t.textContent = `${dt.getFullYear()}-${String(dt.getMonth() + 1).padStart(2, '0')}`;
                group.appendChild(t);
                if (dt.getFullYear() !== lastYear) {
                    lastYear = dt.getFullYear();
                }
            }
        }
        const totalPx = opts.totalDays * dw;
        const cellW = Math.max(dw, totalPx / Math.min(opts.totalDays, 20));
        const startX = MARGIN_LEFT;
        const endX = MARGIN_LEFT + totalPx;
        let dayIdx = 0;
        for (let x = startX; x < endX; x += cellW) {
            const dt3 = new Date(sd.getTime() + dayIdx * 86400000);
            const t = svgEl('text', {
                x: x + cellW / 2, y: upperH + lowerH / 2 + 4,
                'text-anchor': 'middle', 'font-size': 10, fill: '#666'
            });
            t.textContent = `${dt3.getMonth() + 1}/${dt3.getDate()}`;
            group.appendChild(t);
            dayIdx += Math.round(cellW / dw);
        }
    }
    // 模式2
    else if (tsm === 2) {
        let lastYear = -1;
        for (let d = 0; d <= opts.totalDays; d++) {
            const dt = new Date(sd.getTime() + d * 86400000);
            const x = MARGIN_LEFT + d * dw;
            if (dt.getDate() === 1 || d === 0) {
                const monthSpan = new Date(dt.getFullYear(), dt.getMonth() + 1, 0).getDate();
                const mw = monthSpan * dw;
                const t = svgEl('text', {
                    x: x + mw / 2, y: upperH / 2 + 4, 'text-anchor': 'middle',
                    'font-size': labelFontSize, fill: '#333', 'font-weight': 'bold'
                });
                t.textContent = `${dt.getFullYear()}-${String(dt.getMonth() + 1).padStart(2, '0')}`;
                group.appendChild(t);
                if (dt.getFullYear() !== lastYear) {
                    lastYear = dt.getFullYear();
                }
            }
        }
        let lastW = -1;
        for (let d = 0; d <= opts.totalDays; d++) {
            const dt = new Date(sd.getTime() + d * 86400000);
            const x = MARGIN_LEFT + d * dw;
            const week = getISOWeek(dt);
            const dayNum = (dt.getDay() + 6) % 7;
            if (week !== lastW) {
                lastW = week;
                const weekStart = new Date(dt);
                weekStart.setDate(dt.getDate() - dayNum);
                const weekEnd = new Date(weekStart);
                weekEnd.setDate(weekStart.getDate() + 6);
                const ws = Math.max(0, Math.round((weekStart.getTime() - sd.getTime()) / 86400000));
                const we = Math.min(opts.totalDays, Math.round((weekEnd.getTime() - sd.getTime()) / 86400000) + 1);
                const wkSpan = (we - ws) * dw;
                const t = svgEl('text', {
                    x: x + wkSpan / 2, y: upperH + lowerH / 2 + 4,
                    'text-anchor': 'middle', 'font-size': 10, fill: '#666'
                });
                t.textContent = `W${week}`;
                group.appendChild(t);
            }
        }
    }
    parent.appendChild(group);
    return totalH;
}
