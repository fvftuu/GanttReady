// ============================================================
// render/generator.ts — SVG 生成器 (buildNetworkSvg)
// DOM API 版：所有内部函数操作 SVG DOM，最后 serialize 为字符串
// ============================================================
import { collectAllSegments, generateCrossArcs } from './crossarc.js';
const SVG_NS = 'http://www.w3.org/2000/svg';
function svgEl(tag, attrs) {
    const el = document.createElementNS(SVG_NS, tag);
    if (attrs) {
        for (const [k, v] of Object.entries(attrs)) {
            el.setAttribute(k, String(v));
        }
    }
    return el;
}
function getISOWeek(dt) {
    let d = new Date(Date.UTC(dt.getFullYear(), dt.getMonth(), dt.getDate()));
    let dayNum = d.getUTCDay() || 7;
    d.setUTCDate(d.getUTCDate() + 4 - dayNum);
    let yearStart = new Date(Date.UTC(d.getUTCFullYear(), 0, 1));
    return Math.ceil((((d.getTime() - yearStart.getTime()) / 86400000) + 1) / 7);
}
/** 生成 SVG 字符串 */
export function buildNetworkSvg(params) {
    let p = params;
    let cvW = p.canvasW || 3780;
    let cvH = p.canvasH || 400;
    let layerH = p.layerHeight || 60;
    let nr = p.nodeRadius || 11;
    let nfs = Math.max(9, nr);
    let prStartDate = p.projectStartDate || new Date().toISOString().slice(0, 10);
    let sd = new Date(prStartDate);
    let dw = p.dayWidth || 8;
    let tsm = window._netTimeScaleMode || 0;
    let mode = p.mode || 'time';
    let isTimeMode = mode === 'time';
    let showCritical = p.showCritical !== false;
    let showFloat = p.showFloat !== false;
    let showGridH = p.showGridH === true;
    let showGridV = p.showGridV === true;
    let activities = (p.timeParams ? p.timeParams.activities : []) || [];
    let layout = p.layout;
    let eventsMap = layout.events;
    // 应用拖拽后的偏移（重算计划时保留节点位置）
    var pendingOffsets = p._pendingOffsets || {};
    Object.keys(pendingOffsets).forEach(function (eid) {
        var off = pendingOffsets[eid];
        if (eventsMap[eid] && off) {
            if (off.x)
                eventsMap[eid].x = off.x;
            if (off.y)
                eventsMap[eid].y = off.y;
        }
    });
    // 创建 SVG 根节点
    const svg = svgEl('svg', {
        id: 'network-svg', class: 'network-svg',
        width: cvW, height: cvH,
        style: 'background:#fafafa',
        xmlns: SVG_NS
    });
    // 背景
    const bg = svgEl('rect', { x: 0, y: 0, width: cvW, height: cvH, fill: '#fafafa', stroke: '#e8e8e8' });
    svg.appendChild(bg);
    // ---- 行背景 ----
    renderRowBg(svg, eventsMap, cvW, layerH, mode);
    // ======== 1. 上层标尺 ========
    const rulerBg = svgEl('rect', { x: 10, y: 0, width: p.totalDays * dw + 42, height: 52, fill: '#fff', stroke: '#e8e8e8' });
    svg.appendChild(rulerBg);
    renderTimeline(svg, sd, dw, p.totalDays, isTimeMode, tsm);
    // ======== 1.5 网格线 ========
    if (isTimeMode && (showGridH || showGridV)) {
        const marginTop = p._marginTop || 100;
        const { lastRowY } = findExtents(eventsMap, p.totalDays, dw, nr);
        const gridY1 = 52; // 上层标尺底部
        const gridY2 = lastRowY + layerH + 10; // 图例顶部
        const maxY = gridY2;
        renderGrid(svg, p.totalDays, dw, showGridH, showGridV, layerH, marginTop, maxY, 12);
    }
    // ======== 2. 活动箭线 ========
    renderActivities(svg, activities, eventsMap, showCritical, showFloat, isTimeMode, dw, sd, nr, p);
    // ======== 3. 母线法 ========
    renderBusbar(svg, activities, eventsMap, nr);
    // ======== 4. 过桥弧 ========
    if (p.timeParams && p.timeParams.activities) {
        let allSegs = collectAllSegments(p.layout, p.timeParams.activities, p.timeParams.relations || [], isTimeMode, nr);
        let crossParts = [];
        generateCrossArcs(allSegs, crossParts, isTimeMode);
        // crossParts 是 SVG 字符串片段，解析后插入 DOM
        if (crossParts.length > 0) {
            let tempDiv = document.createElement('div');
            tempDiv.innerHTML = crossParts.join('');
            for (let ci = 0; ci < tempDiv.children.length; ci++) {
                svg.appendChild(tempDiv.children[ci].cloneNode(true));
            }
        }
    }
    // ======== 5. 事件节点 ========
    renderNodes(svg, eventsMap, showCritical, showFloat, isTimeMode, nr, nfs);
    // ======== 6. 今日线 ========
    renderTodayLine(svg, p, sd, dw, cvH, isTimeMode);
    // ======== 7. 前锋线 ========
    renderProgressLine(svg, p, sd, dw, cvH, eventsMap, isTimeMode);
    // ======== 8. 进度曲线 ========
    renderProgressCurve(svg, p, sd, dw, isTimeMode);
    // ======== 9. 图例 & 总工期 ========
    let legendH = 95;
    let { lastRowY } = findExtents(eventsMap, p.totalDays, dw, nr);
    let legendTop = lastRowY + layerH + 10;
    renderLegend(svg, lastRowY, layerH, p.totalDuration || 0);
    // ======== 10. 底部标尺 ========
    renderBottomRuler(svg, p, sd, dw, cvW, cvH, isTimeMode);
    // ======== 自适应高度 ========
    // 从底部标尺获取实际底部位置（renderBottomRuler 会把 rulerBottom 存入 _netRulerBottom）
    var rulerBottom = window._netRulerBottom || (cvH - 10);
    var actualBottom = Math.max(legendTop + legendH + 10, rulerBottom + 10);
    var finalH = Math.max(cvH, actualBottom);
    svg.setAttribute('height', String(finalH));
    window._netSvgHeight = finalH;
    // 清空拖拽偏移（已应用到新 SVG）
    window._netPendingOffsets = {};
    // 序列化为字符串
    const xmlSer = new XMLSerializer();
    return xmlSer.serializeToString(svg);
}
// ---- 行背景 ----
function renderRowBg(parent, eventsMap, cvW, layerH, mode) {
    let ySet = {};
    Object.keys(eventsMap).forEach(function (eid) {
        let evt = eventsMap[eid];
        if (evt.y !== undefined)
            ySet[evt.y] = true;
    });
    let yList = Object.keys(ySet).map(Number).sort((a, b) => a - b);
    const g = svgEl('g', { class: 'net-row-bg' });
    yList.forEach(function (y, idx) {
        let isEven = idx % 2 === 0;
        g.appendChild(svgEl('rect', {
            class: 'net-row-bg', x: 0, y: y - layerH / 2,
            width: cvW, height: layerH,
            fill: isEven ? '#ffffff' : '#f5f5f5', opacity: 0.3
        }));
        if (mode === 'logic') {
            const txt = svgEl('text', { x: 4, y: y + 4, 'font-size': 10, fill: '#999' });
            txt.textContent = 'L' + idx;
            g.appendChild(txt);
        }
    });
    parent.appendChild(g);
}
// ---- 网格线 ----
// 水平线: 行间分割线 (画在层间间隙，不穿过节点)
// 竖直线: 日期对齐线 (每天一条), 范围同上
function renderGrid(parent, totalDays, dw, showH, showV, layerH, marginTop, maxY, ML) {
    const g = svgEl('g', { class: 'net-grid', opacity: '0.8', 'pointer-events': 'none' });
    const gridY1 = 52; // 上层标尺底部
    const gridY2 = maxY; // 图例顶部
    // 收集每层的 y 坐标（层中心）
    // 行分割线画在两层之间的间隙中间
    // 从 eventsMap 中获取所有层的 y 并排序
    var allYs = [];
    // 也可以通过 grid 函数参数直接传入 sortedYs
    // 因为 eventsMap 不可达，用 marginTop + layerH*i 推算
    // 先找出有多少行
    var maxLine = Math.floor((maxY - marginTop) / layerH) + 1;
    if (showH) {
        // 行间分割线：画在每层的上边界稍微往下一点（层中心下方）
        // 从 marginTop + layerH/2（第一层行间下方）开始，每 layerH 一条
        // 这样线位于两行之间的边界
        var gapY = marginTop + layerH;
        while (gapY <= maxY) {
            if (gapY >= gridY1) {
                g.appendChild(svgEl('line', {
                    x1: ML, y1: gapY, x2: ML + totalDays * dw + 400, y2: gapY,
                    stroke: '#999', 'stroke-width': 0.6, 'stroke-dasharray': '6,4'
                }));
            }
            gapY += layerH;
        }
    }
    if (showV) {
        for (let d = 0; d <= totalDays; d++) {
            let x = ML + d * dw;
            g.appendChild(svgEl('line', {
                x1: x, y1: gridY1, x2: x, y2: gridY2,
                stroke: '#999', 'stroke-width': 0.5, 'stroke-dasharray': '4,6'
            }));
        }
    }
    parent.appendChild(g);
}
// ---- 标尺（精确复刻甘特图 timeline.ts） ----
function renderTimeline(parent, sd, dw, totalDays, isTimeMode, tsm) {
    if (!isTimeMode)
        return;
    const UPPER_H = 28, LOWER_H = 24;
    const MT = 12;
    const g = svgEl('g', { class: 'timeline' });
    if (tsm === 0) {
        let lastYear = -1;
        for (let d = 0; d <= totalDays; d++) {
            let dt = new Date(sd.getTime() + d * 86400000);
            let x = MT + d * dw;
            if (dt.getDate() === 1 || d === 0) {
                let mw = (new Date(dt.getFullYear(), dt.getMonth() + 1, 0).getDate()) * dw;
                const t = svgEl('text', {
                    x: x + mw / 2, y: UPPER_H / 2 + 4,
                    'text-anchor': 'middle', 'font-size': 11, fill: '#333', 'font-weight': 'bold'
                });
                t.textContent = dt.getFullYear() + '-' + String(dt.getMonth() + 1).padStart(2, '0');
                g.appendChild(t);
                if (dt.getFullYear() !== lastYear) {
                    lastYear = dt.getFullYear();
                    const yt = svgEl('text', {
                        x: x, y: UPPER_H / 2 + 4 - 14,
                        'font-size': 10, fill: '#999'
                    });
                    yt.textContent = ' ' + lastYear + '年';
                    g.appendChild(yt);
                }
            }
        }
        let lastW = -1;
        for (let d = 0; d <= totalDays; d++) {
            let dt = new Date(sd.getTime() + d * 86400000);
            let x = MT + d * dw;
            let dow = dt.getDay();
            let isSat = dow === 6, isSun = dow === 0;
            if (isSat || isSun) {
                g.appendChild(svgEl('rect', {
                    x: x, y: UPPER_H, width: dw, height: LOWER_H,
                    fill: isSat ? '#e6f7ff' : '#fff0f0', opacity: 0.1
                }));
            }
            const t = svgEl('text', {
                x: x + dw / 2, y: UPPER_H + LOWER_H / 2 + 4,
                'text-anchor': 'middle', 'font-size': 10,
                fill: isSat || isSun ? '#ccc' : '#666'
            });
            t.textContent = String(dt.getDate());
            g.appendChild(t);
            let wk = getISOWeek(dt);
            if (wk !== lastW) {
                lastW = wk;
                const wt = svgEl('text', {
                    x: x, y: UPPER_H + LOWER_H + 12,
                    'font-size': 9, fill: '#aaa'
                });
                wt.textContent = 'W' + wk;
                g.appendChild(wt);
            }
        }
    }
    else if (tsm === 1) {
        let lastYear = -1;
        for (let d = 0; d <= totalDays; d++) {
            let dt = new Date(sd.getTime() + d * 86400000);
            let x = MT + d * dw;
            if (dt.getDate() === 1 || d === 0) {
                let mw = (new Date(dt.getFullYear(), dt.getMonth() + 1, 0).getDate()) * dw;
                const t = svgEl('text', {
                    x: x + mw / 2, y: UPPER_H / 2 + 4,
                    'text-anchor': 'middle', 'font-size': 11, fill: '#333', 'font-weight': 'bold'
                });
                t.textContent = dt.getFullYear() + '-' + String(dt.getMonth() + 1).padStart(2, '0');
                g.appendChild(t);
                if (dt.getFullYear() !== lastYear) {
                    lastYear = dt.getFullYear();
                }
            }
        }
        let totalPx = totalDays * dw;
        let cellW = Math.max(dw, totalPx / Math.min(totalDays, 20));
        let dayIdx = 0;
        for (let x = MT; x < MT + totalPx; x += cellW) {
            let dt3 = new Date(sd.getTime() + dayIdx * 86400000);
            const t = svgEl('text', {
                x: x + cellW / 2, y: UPPER_H + LOWER_H / 2 + 4,
                'text-anchor': 'middle', 'font-size': 10, fill: '#666'
            });
            t.textContent = (dt3.getMonth() + 1) + '/' + dt3.getDate();
            g.appendChild(t);
            dayIdx += Math.round(cellW / dw);
        }
    }
    else if (tsm === 2) {
        let lastYear = -1;
        for (let d = 0; d <= totalDays; d++) {
            let dt = new Date(sd.getTime() + d * 86400000);
            let x = MT + d * dw;
            if (dt.getDate() === 1 || d === 0) {
                let mw = (new Date(dt.getFullYear(), dt.getMonth() + 1, 0).getDate()) * dw;
                const t = svgEl('text', {
                    x: x + mw / 2, y: UPPER_H / 2 + 4,
                    'text-anchor': 'middle', 'font-size': 11, fill: '#333', 'font-weight': 'bold'
                });
                t.textContent = dt.getFullYear() + '-' + String(dt.getMonth() + 1).padStart(2, '0');
                g.appendChild(t);
                if (dt.getFullYear() !== lastYear) {
                    lastYear = dt.getFullYear();
                }
            }
        }
        let lastW = -1;
        for (let d = 0; d <= totalDays; d++) {
            let dt = new Date(sd.getTime() + d * 86400000);
            let x = MT + d * dw;
            let wk = getISOWeek(dt);
            let dayNum = (dt.getDay() + 6) % 7;
            if (wk !== lastW) {
                let ws = Math.max(0, Math.round((new Date(dt.getFullYear(), dt.getMonth(), dt.getDate() - dayNum).getTime() - sd.getTime()) / 86400000));
                let we = Math.min(totalDays, ws + 7);
                let wkSpan = (we - ws) * dw;
                const t = svgEl('text', {
                    x: x + wkSpan / 2, y: UPPER_H + LOWER_H / 2 + 4,
                    'text-anchor': 'middle', 'font-size': 10, fill: '#666'
                });
                t.textContent = 'W' + wk;
                g.appendChild(t);
                lastW = wk;
            }
        }
    }
    parent.appendChild(g);
}
// ---- 活动箭线 ----
function renderActivities(parent, activities, eventsMap, showCritical, showFloat, isTimeMode, dw, sd, nr, p) {
    let critActs = [];
    let nonCritActs = [];
    (activities || []).forEach(function (act) {
        (act.isCritical && showCritical ? critActs : nonCritActs).push(act);
    });
    const arrowsG = svgEl('g', { class: 'net-arrows' });
    let hasContent = false;
    // 统计入度（多条箭线汇聚到同一目标节点）
    var inDeg = {};
    var inDegActs = {};
    (activities || []).forEach(function (act) {
        var tgt = act.target;
        if (tgt) {
            inDeg[tgt] = (inDeg[tgt] || 0) + 1;
            if (!inDegActs[tgt])
                inDegActs[tgt] = [];
            inDegActs[tgt].push(act);
        }
    });
    // 按 id 排序以保证偏移顺序稳定
    Object.keys(inDegActs).forEach(function (tgt) {
        inDegActs[tgt].sort(function (a, b) { return a.id - b.id; });
    });
    [nonCritActs, critActs].forEach(function (actList) {
        actList.forEach(function (act) {
            let srcEvt = eventsMap[act.source];
            let tgtEvt = eventsMap[act.target];
            if (!srcEvt || !tgtEvt)
                return;
            let x1 = srcEvt.x || 0, y1 = srcEvt.y || 0;
            let x2 = tgtEvt.x || 0, y2 = tgtEvt.y || 0;
            let isCrit = act.isCritical;
            let isDummy = act.isDummy || (act.duration === 0 && act.id < 0);
            let sc = isCrit && showCritical ? '#ff4d4f' : (isDummy ? '#52c41a' : '#1890ff');
            let sw = isCrit && showCritical ? 2.5 : (isDummy ? 1 : 1.5);
            let dash = isDummy ? '6,3' : 'none';
            hasContent = true;
            let isLShape = Math.abs(y2 - y1) >= 2;
            let hasFF = false;
            let waveX1 = x2, waveX2 = x2, waveY = y2;
            if (!isDummy && isTimeMode && act.ff > 0) {
                let ffDist = act.ff * dw;
                if (ffDist > 2) {
                    hasFF = true;
                    waveX1 = x2 - ffDist;
                }
            }
            // 入度偏移（只影响最后水平段到目标节点的 y）
            var tgtActs = inDegActs[act.target] || [];
            var inDegTgt = tgtActs.length;
            var inDegIdx = 0;
            tgtActs.forEach(function (a, i) { if (a.id === act.id)
                inDegIdx = i; });
            var tgtOffY = 0;
            if (inDegTgt > 1) {
                var gap = 6;
                tgtOffY = -((inDegTgt - 1) * gap) / 2 + inDegIdx * gap;
            }
            var inY2 = y2 + tgtOffY;
            // 关键阴影
            if (actList === critActs && showCritical) {
                let offY = 1;
                if (isLShape) {
                    let midX = Math.max(x1 + nr + 4, x2 - nr - 4);
                    arrowsG.appendChild(svgEl('path', {
                        d: 'M' + x1 + ' ' + (y1 + offY) + ' L' + midX + ' ' + (y1 + offY) + ' L' + midX + ' ' + (inY2 + offY) + ' L' + (x2 + nr) + ' ' + (inY2 + offY),
                        stroke: '#ff4d4f', 'stroke-width': 4, opacity: 0.2, fill: 'none'
                    }));
                }
                else if (hasFF) {
                    arrowsG.appendChild(svgEl('line', {
                        x1: x1, y1: y1 + offY, x2: waveX1, y2: inY2 + offY,
                        stroke: '#ff4d4f', 'stroke-width': 4, opacity: 0.2
                    }));
                }
                else {
                    arrowsG.appendChild(svgEl('line', {
                        x1: x1, y1: y1 + offY, x2: x2, y2: inY2 + offY,
                        stroke: '#ff4d4f', 'stroke-width': 4, opacity: 0.2
                    }));
                }
            }
            let aLen = 7;
            if (isLShape) {
                // L 型箭线：从源节点水平引出 → 垂直 → 水平到达目标节点
                // 始终从右侧引出（x1+nr 或更大）
                let midX = Math.max(x1 + nr + 4, x2 - nr - 4);
                if (midX < x1 + nr + 4)
                    midX = x1 + nr + 4;
                let pathStr = 'M' + x1 + ' ' + y1 +
                    ' L' + midX + ' ' + y1 +
                    ' L' + midX + ' ' + inY2 +
                    ' L' + (x2 + nr) + ' ' + inY2;
                arrowsG.appendChild(svgEl('path', {
                    d: pathStr, stroke: sc, 'stroke-width': sw,
                    'stroke-dasharray': dash, fill: 'none', 'data-activity-id': act.id,
                    'data-src': act.source, 'data-tgt': act.target, style: 'cursor:pointer'
                }));
                // 箭头（水平方向指向右侧）
                const arrow = svgEl('polygon', {
                    points: [
                        (x2 - aLen) + ',' + (inY2 - aLen * 0.38),
                        (x2 + nr) + ',' + inY2,
                        (x2 - aLen) + ',' + (inY2 + aLen * 0.38)
                    ].join(' '),
                    fill: sc
                });
                arrowsG.appendChild(arrow);
                // 标签
                if (!isDummy) {
                    let labelX = Math.max(x1 + (midX - x1) / 2, x1 + 20);
                    const nt = svgEl('text', {
                        x: labelX, y: y1 - 4,
                        'text-anchor': 'middle', 'font-size': 9, fill: sc
                    });
                    nt.textContent = act.code || '';
                    arrowsG.appendChild(nt);
                    const dt = svgEl('text', {
                        x: labelX, y: y1 + 8,
                        'text-anchor': 'middle', 'font-size': 8, fill: '#999'
                    });
                    dt.textContent = act.duration;
                    arrowsG.appendChild(dt);
                }
            }
            else if (hasFF) {
                // 直线段
                arrowsG.appendChild(svgEl('line', {
                    x1: x1, y1: y1, x2: waveX1, y2: inY2,
                    stroke: sc, 'stroke-width': sw, 'data-activity-id': act.id,
                    'data-src': act.source, 'data-tgt': act.target, style: 'cursor:pointer'
                }));
                // 波形段
                let wMid = (waveX1 + waveX2) / 2;
                arrowsG.appendChild(svgEl('path', {
                    d: 'M' + waveX1 + ' ' + inY2 +
                        ' Q' + (waveX1 + (waveX2 - waveX1) * 0.25) + ' ' + (inY2 - 4) +
                        ' ' + wMid + ' ' + inY2 +
                        ' Q' + (waveX2 - (waveX2 - waveX1) * 0.25) + ' ' + (inY2 + 4) +
                        ' ' + waveX2 + ' ' + inY2,
                    stroke: sc, 'stroke-width': sw, fill: 'none', 'data-activity-id': act.id,
                    'data-src': act.source, 'data-tgt': act.target, style: 'cursor:pointer'
                }));
                // 箭头
                const arrow = svgEl('polygon', {
                    points: [
                        (x2 - aLen) + ',' + (inY2 - aLen * 0.38),
                        x2 + ',' + inY2,
                        (x2 - aLen) + ',' + (inY2 + aLen * 0.38)
                    ].join(' '),
                    fill: sc
                });
                arrowsG.appendChild(arrow);
                // 标签
                if (!isDummy) {
                    const nt = svgEl('text', {
                        x: (x1 + x2) / 2, y: (y1 + inY2) / 2 - 4,
                        'text-anchor': 'middle', 'font-size': 9, fill: sc
                    });
                    nt.textContent = act.code || '';
                    arrowsG.appendChild(nt);
                    const dt = svgEl('text', {
                        x: (x1 + x2) / 2, y: (y1 + inY2) / 2 + 8,
                        'text-anchor': 'middle', 'font-size': 8, fill: '#999'
                    });
                    dt.textContent = act.duration;
                    arrowsG.appendChild(dt);
                }
            }
            else {
                // 普通直线
                arrowsG.appendChild(svgEl('line', {
                    x1: x1, y1: y1, x2: x2, y2: inY2,
                    stroke: sc, 'stroke-width': sw,
                    'stroke-dasharray': dash, 'data-activity-id': act.id,
                    'data-src': act.source, 'data-tgt': act.target, style: 'cursor:pointer'
                }));
                // 箭头
                let angle = Math.atan2(inY2 - y1, x2 - x1);
                const arrow = svgEl('polygon', {
                    points: [
                        (x2 - aLen * Math.cos(angle - 0.4)) + ',' + (inY2 - aLen * Math.sin(angle - 0.4)),
                        x2 + ',' + inY2,
                        (x2 - aLen * Math.cos(angle + 0.4)) + ',' + (inY2 - aLen * Math.sin(angle + 0.4))
                    ].join(' '),
                    fill: sc
                });
                arrowsG.appendChild(arrow);
                // 标签
                if (!isDummy) {
                    const nt = svgEl('text', {
                        x: (x1 + x2) / 2, y: (y1 + inY2) / 2 - 4,
                        'text-anchor': 'middle', 'font-size': 9, fill: sc
                    });
                    nt.textContent = act.code || '';
                    arrowsG.appendChild(nt);
                    const dt = svgEl('text', {
                        x: (x1 + x2) / 2, y: (y1 + inY2) / 2 + 8,
                        'text-anchor': 'middle', 'font-size': 8, fill: '#999'
                    });
                    dt.textContent = act.duration;
                    arrowsG.appendChild(dt);
                }
            }
        });
    });
    if (hasContent) {
        parent.appendChild(arrowsG);
    }
}
// ---- 母线法 ----
function renderBusbar(parent, activities, eventsMap, nr) {
    let hasBus = false;
    const g = svgEl('g', { class: 'busbar', opacity: 0.5 });
    Object.keys(eventsMap).forEach(function (eid) {
        let evt = eventsMap[eid];
        if (evt.isVirtual)
            return;
        let ex = evt.x || 0, ey = evt.y || 0;
        // 出度母线（右侧），出度 >= 2 使用母线
        let outActs = activities.filter(function (a) { return a.source === eid; });
        if (outActs.length >= 2) {
            hasBus = true;
            let tys = outActs.map(function (a) {
                let t = eventsMap[a.target];
                return t ? (t.y || 0) : ey;
            });
            let minY = Math.min(ey, ...tys), maxY = Math.max(ey, ...tys);
            let bx = ex + nr + 8;
            g.appendChild(svgEl('line', {
                x1: bx, y1: minY, x2: bx, y2: maxY,
                stroke: '#aaa', 'stroke-width': 1.5, opacity: 0.6
            }));
            outActs.forEach(function (a) {
                let t = eventsMap[a.target];
                if (!t)
                    return;
                let ty = t.y || 0, tx = t.x || ex;
                let eX = tx - nr - 2;
                // 母线上水平引出到各目标
                g.appendChild(svgEl('line', {
                    x1: bx, y1: ty, x2: eX, y2: ty,
                    stroke: '#aaa', 'stroke-width': 0.8, opacity: 0.6
                }));
                // 小箭头
                let aLen = 5;
                g.appendChild(svgEl('polygon', {
                    points: [
                        (eX - aLen) + ',' + (ty - aLen * 0.38),
                        eX + ',' + ty,
                        (eX - aLen) + ',' + (ty + aLen * 0.38)
                    ].join(' '),
                    fill: '#aaa', opacity: 0.6
                }));
            });
            // 从源节点到母线的连接线
            g.appendChild(svgEl('line', {
                x1: ex, y1: ey, x2: bx, y2: ey,
                stroke: '#aaa', 'stroke-width': 1, opacity: 0.5
            }));
        }
        // 入度母线（左侧），入度 >= 2 使用母线
        let inActs = activities.filter(function (a) { return a.target === eid; });
        if (inActs.length >= 2) {
            hasBus = true;
            let sys = inActs.map(function (a) {
                let s = eventsMap[a.source];
                return s ? (s.y || 0) : ey;
            });
            let minY = Math.min(ey, ...sys), maxY = Math.max(ey, ...sys);
            let bx = ex - nr - 8;
            g.appendChild(svgEl('line', {
                x1: bx, y1: minY, x2: bx, y2: maxY,
                stroke: '#aaa', 'stroke-width': 1.5, opacity: 0.6
            }));
            inActs.forEach(function (a) {
                let s = eventsMap[a.source];
                if (!s)
                    return;
                let sy = s.y || 0, sx = s.x || 0;
                let eX = sx + nr + 2;
                g.appendChild(svgEl('line', {
                    x1: eX, y1: sy, x2: bx, y2: sy,
                    stroke: '#aaa', 'stroke-width': 0.8, opacity: 0.6
                }));
            });
            // 汇聚到目标节点
            g.appendChild(svgEl('line', {
                x1: bx, y1: ey, x2: ex, y2: ey,
                stroke: '#aaa', 'stroke-width': 1, opacity: 0.5
            }));
        }
    });
    if (hasBus)
        parent.appendChild(g);
}
// ---- 事件节点 ----
function renderNodes(parent, eventsMap, showCritical, showFloat, isTimeMode, nr, nfs) {
    const g = svgEl('g', { class: 'net-events' });
    Object.keys(eventsMap).forEach(function (eid) {
        let evt = eventsMap[eid];
        if (evt.isVirtual)
            return;
        let ex = evt.x || 0, ey = evt.y || 0;
        let isCrit = evt.isCritical && showCritical;
        const eg = svgEl('g', {
            class: 'net-event', 'data-event-id': eid,
            'data-task-id': evt.taskId || 0
        });
        if (isCrit) {
            eg.appendChild(svgEl('circle', {
                cx: ex, cy: ey, r: nr,
                fill: '#fff', stroke: '#ff4d4f', 'stroke-width': 3
            }));
            eg.appendChild(svgEl('circle', {
                cx: ex, cy: ey, r: nr - 4,
                fill: '#ff4d4f', stroke: '#ff4d4f', 'stroke-width': 1.5
            }));
        }
        else {
            eg.appendChild(svgEl('circle', {
                cx: ex, cy: ey, r: nr,
                fill: '#fff', stroke: '#1890ff', 'stroke-width': 1.5
            }));
        }
        // 编号
        const nt = svgEl('text', {
            x: ex, y: ey + 4, 'text-anchor': 'middle',
            'font-size': nfs, fill: isCrit ? '#fff' : '#333',
            'font-weight': 'bold'
        });
        nt.textContent = String(evt.num != null ? evt.num : eid);
        eg.appendChild(nt);
        // ET
        if (isTimeMode) {
            const et = svgEl('text', {
                x: ex, y: ey + nr + 12,
                'text-anchor': 'middle', 'font-size': 8, fill: '#999'
            });
            et.textContent = 'ET=' + evt.es;
            eg.appendChild(et);
        }
        // TF
        if (showFloat && evt.tf > 0) {
            const tft = svgEl('text', {
                x: ex, y: ey + nr + 22,
                'text-anchor': 'middle', 'font-size': 7, fill: '#e67e22'
            });
            tft.textContent = 'TF=' + evt.tf;
            eg.appendChild(tft);
        }
        g.appendChild(eg);
    });
    parent.appendChild(g);
}
// ---- 今日线 ----
function renderTodayLine(parent, p, sd, dw, cvH, isTimeMode) {
    if (!(isTimeMode && p.showTodayLine))
        return;
    let today = new Date();
    let off = Math.floor((today.getTime() - sd.getTime()) / 86400000);
    if (off >= 0 && off < p.totalDays) {
        let tx = 80 + off * dw + dw / 2;
        parent.appendChild(svgEl('line', {
            x1: tx, y1: 0, x2: tx, y2: cvH - 28,
            stroke: '#ff4d4f', 'stroke-width': 1,
            'stroke-dasharray': '4,3', opacity: 0.5
        }));
        const t = svgEl('text', {
            x: tx + 4, y: 14, 'font-size': 10, fill: '#ff4d4f'
        });
        t.textContent = '今日 ' + today.getFullYear() + '-' +
            String(today.getMonth() + 1).padStart(2, '0') + '-' +
            String(today.getDate()).padStart(2, '0');
        parent.appendChild(t);
    }
}
// ---- 前锋线 ----
function renderProgressLine(parent, p, sd, dw, cvH, eventsMap, isTimeMode) {
    if (!(isTimeMode && p.showProgressLine && p.projectStartDate))
        return;
    let saved = window.localStorage && window.localStorage.getItem('netplan_progress_date');
    let gw = window;
    let pdStr = saved || (gw._progressDate
        ? gw._progressDate.getFullYear() + '-' +
            String(gw._progressDate.getMonth() + 1).padStart(2, '0') + '-' +
            String(gw._progressDate.getDate()).padStart(2, '0')
        : null);
    if (!pdStr)
        return;
    let pd = new Date(pdStr);
    let po = Math.round((pd.getTime() - sd.getTime()) / 86400000);
    if (po < 0)
        return;
    let px = 80 + po * dw;
    const g = svgEl('g', { id: 'net-progress-check' });
    g.appendChild(svgEl('line', {
        x1: px, y1: 0, x2: px, y2: cvH - 28,
        stroke: '#52c41a', 'stroke-width': 2, 'stroke-dasharray': '6,3'
    }));
    g.appendChild(svgEl('polygon', {
        points: (px - 7) + ',0 ' + (px + 7) + ',0 ' + px + ',14',
        fill: '#52c41a'
    }));
    const pt = svgEl('text', {
        x: px + 4, y: 14, 'font-size': 11, fill: '#52c41a', 'font-weight': 'bold'
    });
    pt.textContent = pdStr;
    g.appendChild(pt);
    parent.appendChild(g);
    // 进度三角标记
    Object.keys(eventsMap).forEach(function (eid) {
        let evt = eventsMap[eid];
        if (evt.isVirtual)
            return;
        if (po >= evt.es && po <= evt.ef) {
            let ratio = evt.ef > evt.es ? (po - evt.es) / (evt.ef - evt.es) : 1;
            let ex = 80 + (evt.es + ratio * (evt.ef - evt.es)) * dw;
            let ey = (evt.y || 0) - 4;
            parent.appendChild(svgEl('polygon', {
                points: (ex - 4) + ',' + ey + ' ' + (ex + 4) + ',' + ey + ' ' + ex + ',' + (ey - 6),
                fill: '#52c41a'
            }));
        }
    });
}
// ---- 进度曲线 ----
function renderProgressCurve(parent, p, sd, dw, isTimeMode) {
    if (!(p.showProgressCurve && isTimeMode))
        return;
    let acts = (p.timeParams && p.timeParams.activities) || [];
    if (acts.length === 0)
        return;
    let pts = [];
    let yMin = 120, yMax = 90, yR = yMin - yMax;
    let MARGIN_LEFT_PC = 80;
    for (let d = 0; d < p.totalDays; d++) {
        let dc = 0, dn = 0;
        acts.forEach(function (a) {
            let ae = a.es || 0;
            let af = a.ef || 0;
            if (d >= ae && d < af) {
                dc += (a.completion || 0);
                dn++;
            }
        });
        let avg = dn > 0 ? dc / dn : -1;
        if (avg < 0)
            continue;
        let cx = MARGIN_LEFT_PC + d * dw + dw / 2;
        let cy = yMax + yR * (1 - avg / 100);
        pts.push(cx + ',' + cy);
    }
    if (pts.length < 2)
        return;
    let fX = pts[0].split(',')[0], lX = pts[pts.length - 1].split(',')[0];
    let fillD = 'M' + fX + ',' + yMin + ' L' + pts.join(' L') + ' L' + lX + ',' + yMin + ' Z';
    parent.appendChild(svgEl('path', { d: fillD, fill: 'rgba(173,216,230,0.3)', stroke: 'none' }));
    parent.appendChild(svgEl('polyline', {
        fill: 'none', stroke: '#5dade2', 'stroke-width': 2,
        points: pts.join(' ')
    }));
}
// ---- 查找范围 ----
function findExtents(eventsMap, totalDays, dw, nr) {
    let lastRowY = 100, rightmostX = 80 + (totalDays || 90) * dw;
    Object.keys(eventsMap).forEach(function (eid) {
        let evt = eventsMap[eid];
        if (evt.y && evt.y > lastRowY)
            lastRowY = evt.y;
        if (evt.x && evt.x > rightmostX)
            rightmostX = evt.x;
    });
    return { lastRowY, rightmostX };
}
// ---- 图例 ----
function renderLegend(parent, lastRowY, layerH, totalDuration) {
    let legendTop = lastRowY + layerH + 10;
    // 图例框 95px（含总工期，字号统一12）
    parent.appendChild(svgEl('rect', {
        x: 10, y: legendTop, width: 220, height: 95,
        fill: 'rgba(255,255,255,0.95)', stroke: '#ccc', rx: 4
    }));
    // 总工期（框内顶部）
    const durT = svgEl('text', {
        x: 20, y: legendTop + 14,
        'font-size': 12, 'font-weight': 'bold', fill: '#e63946'
    });
    durT.textContent = '总工期=' + totalDuration + '天';
    parent.appendChild(durT);
    // 图例标题
    const legT = svgEl('text', {
        x: 20, y: legendTop + 29,
        'font-size': 12, 'font-weight': 'bold', fill: '#333'
    });
    legT.textContent = '图例';
    parent.appendChild(legT);
    // 图例条目
    [
        { label: '关键线路', color: '#ff4d4f', w: 2.5, d: false },
        { label: '非关键工作', color: '#1890ff', w: 1.5, d: false },
        { label: '虚工作', color: '#52c41a', w: 1, d: true, dw: '6,3' }
    ].forEach(function (it, i) {
        let iy = legendTop + 42 + i * 16;
        parent.appendChild(svgEl('line', {
            x1: 20, y1: iy, x2: 60, y2: iy,
            stroke: it.color, 'stroke-width': it.w,
            'stroke-dasharray': it.d ? (it.dw || '4,3') : 'none'
        }));
        const lt = svgEl('text', {
            x: 66, y: iy + 4, 'font-size': 12, fill: '#333'
        });
        lt.textContent = it.label;
        parent.appendChild(lt);
    });
}
// ---- 底部标尺（翻转：日期在上层，月份在下层） ----
function renderBottomRuler(parent, p, sd, dw, cvW, cvH, isTimeMode) {
    if (!isTimeMode)
        return;
    let { lastRowY } = findExtents(p.layout.events, p.totalDays, dw, p.nodeRadius || 11);
    let layerH = p.layerHeight || 60;
    let legendTop = lastRowY + layerH + 10;
    let legendBottom = legendTop + 95 + 5;
    const UPPER_H = 28, LOWER_H = 24;
    const MT = 12;
    const FS = 11;
    // 底部标尺始终紧跟图例下方，保持固定间距
    let rulerY = legendBottom + 8;
    parent.appendChild(svgEl('rect', {
        x: 0, y: rulerY, width: cvW, height: UPPER_H,
        fill: '#fafafa'
    }));
    parent.appendChild(svgEl('rect', {
        x: 0, y: rulerY + UPPER_H, width: cvW, height: LOWER_H,
        fill: '#f5f5f5'
    }));
    // 上层（翻转后 = 日期层）
    for (let d = 0; d <= p.totalDays; d++) {
        let dt = new Date(sd.getTime() + d * 86400000);
        let x = MT + d * dw;
        let dow = dt.getDay();
        let isSat = dow === 6, isSun = dow === 0;
        const t = svgEl('text', {
            x: x + dw / 2, y: rulerY + UPPER_H / 2 + 4,
            'text-anchor': 'middle', 'font-size': 10,
            fill: isSat || isSun ? '#ccc' : '#666'
        });
        t.textContent = String(dt.getDate());
        parent.appendChild(t);
    }
    // 下层（翻转后 = 月份层）
    let lastMonth = -1;
    for (let d = 0; d <= p.totalDays; d++) {
        let dt = new Date(sd.getTime() + d * 86400000);
        let x = MT + d * dw;
        if (dt.getDate() === 1 || d === 0) {
            let monthSpan = new Date(dt.getFullYear(), dt.getMonth() + 1, 0).getDate();
            let mw = monthSpan * dw;
            const t = svgEl('text', {
                x: x + mw / 2, y: rulerY + UPPER_H + LOWER_H / 2 + 4,
                'text-anchor': 'middle', 'font-size': FS,
                fill: '#333', 'font-weight': 'bold'
            });
            t.textContent = dt.getFullYear() + '-' + String(dt.getMonth() + 1).padStart(2, '0');
            parent.appendChild(t);
            lastMonth = dt.getMonth();
        }
    }
    // 记录底部标尺底部位置，供自适应高度使用
    window._netRulerBottom = rulerY + UPPER_H + LOWER_H;
}
