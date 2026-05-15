// ============================================================
// render/generator.ts — SVG 生成器 (buildNetworkSvg)
// 从 legacy netplan.js 原封迁移 (846行, 44KB)
// TODO: 逐步提取子函数到 timeline/arrows/nodes/progress 等模块
// ============================================================

/**
 * 同时标双代号网络图 SVG 构建
 * 参数结构保持与 legacy 兼容
 */
import { collectAllSegments, generateCrossArcs } from './crossarc.js';

export function buildNetworkSvg(params: any): string {
    var p = params, parts: string[] = [], markers: string[] = [];
    var MARGIN_LEFT = 80, RULER_H = 52;

    // findSegIntersection and cross: 已迁移到 crossarc.ts

    // 时标尺
    var _prStartDate = p.projectStartDate || new Date().toISOString().slice(0, 10);
    var sd = new Date(_prStartDate);
    var dw = p.dayWidth || 8;
    var tsm = (window as any)._netTimeScaleMode || 0;
    var mode = p.mode || 'time';
    var isTimeMode = mode === 'time';

    // ---- 上层标尺(月/年) ----
    if (isTimeMode) {
        parts.push('<g class="timeline">');
        // 上层标尺(月)
        if (tsm === 0) {
            var lastYear = -1;
            for (var d = 0; d <= p.totalDays; d++) {
                var dt = new Date(sd.getTime() + d * 86400000);
                var x = MARGIN_LEFT + d * dw;
                if (dt.getDate() === 1 || d === 0) {
                    var monthSpan = new Date(dt.getFullYear(), dt.getMonth() + 1, 0).getDate();
                    parts.push('<text x="' + (x + monthSpan * dw / 2) + '" y="20" text-anchor="middle" font-size="11" fill="#333" font-weight="bold">' + dt.getFullYear() + '-' + String(dt.getMonth() + 1).padStart(2, '0') + '</text>');
                    if (dt.getFullYear() !== lastYear) {
                        parts.push('<text x="' + x + '" y="10" font-size="10" fill="#999"> ' + lastYear + '年</text>');
                        lastYear = dt.getFullYear();
                    }
                }
            }
            // 下层标尺(日 + 周末背景)
            var lastWeek = -1;
            for (var d = 0; d <= p.totalDays; d++) {
                var dt = new Date(sd.getTime() + d * 86400000);
                var x = MARGIN_LEFT + d * dw;
                var dow = dt.getDay();
                var isSat = dow === 6, isSun = dow === 0;
                if (isSat || isSun) {
                    parts.push('<rect x="' + x + '" y="26" width="' + dw + '" height="26" fill="' + (isSat ? '#e6f7ff' : '#fff0f0') + '" opacity="0.1" class="rest-day"/>');
                }
                parts.push('<text x="' + (x + dw / 2) + '" y="42" text-anchor="middle" font-size="10" fill="' + (isSat || isSun ? '#ccc' : '#666') + '">' + dt.getDate() + '</text>');
                var wkn = getISOWeek(dt);
                if (wkn !== lastWeek) { parts.push('<text x="' + x + '" y="52" font-size="9" fill="#aaa">W' + wkn + '</text>'); lastWeek = wkn; }
            }
        } else if (tsm === 1) {
            // mode 1: 更紧凑
            var lastYear = -1;
            for (var d = 0; d <= p.totalDays; d++) {
                var dt = new Date(sd.getTime() + d * 86400000);
                var x = MARGIN_LEFT + d * dw;
                if (dt.getDate() === 1 || d === 0) {
                    var monthSpan = new Date(dt.getFullYear(), dt.getMonth() + 1, 0).getDate();
                    parts.push('<text x="' + (x + monthSpan * dw / 2) + '" y="20" text-anchor="middle" font-size="11" fill="#333" font-weight="bold">' + dt.getFullYear() + '-' + String(dt.getMonth() + 1).padStart(2, '0') + '</text>');
                    if (dt.getFullYear() !== lastYear) { lastYear = dt.getFullYear(); }
                }
            }
            // mode 1 下层
            var totalPx = p.totalDays * dw;
            var cellW = Math.max(dw, totalPx / Math.min(p.totalDays, 20));
            var startX = MARGIN_LEFT, endX = MARGIN_LEFT + totalPx;
            var dayIdx = 0;
            for (var x = startX; x < endX; x += cellW) {
                var dt3 = new Date(sd.getTime() + dayIdx * 86400000);
                parts.push('<text x="' + (x + cellW / 2) + '" y="42" text-anchor="middle" font-size="10" fill="#666">' + (dt3.getMonth() + 1) + '/' + dt3.getDate() + '</text>');
                dayIdx += Math.round(cellW / dw);
            }
        } else if (tsm === 2) {
            // mode 2: 按周
            var lastYear = -1;
            for (var d = 0; d <= p.totalDays; d++) {
                var dt = new Date(sd.getTime() + d * 86400000);
                var x = MARGIN_LEFT + d * dw;
                if (dt.getDate() === 1 || d === 0) {
                    var monthSpan = new Date(dt.getFullYear(), dt.getMonth() + 1, 0).getDate();
                    parts.push('<text x="' + (x + monthSpan * dw / 2) + '" y="20" text-anchor="middle" font-size="11" fill="#333" font-weight="bold">' + dt.getFullYear() + '-' + String(dt.getMonth() + 1).padStart(2, '0') + '</text>');
                    if (dt.getFullYear() !== lastYear) { lastYear = dt.getFullYear(); }
                }
            }
            var lastW = -1;
            for (var d = 0; d <= p.totalDays; d++) {
                var dt = new Date(sd.getTime() + d * 86400000);
                var x = MARGIN_LEFT + d * dw;
                var wkn = getISOWeek(dt);
                var dayNum = (dt.getDay() + 6) % 7;
                if (wkn !== lastW) {
                    var weekStart = new Date(dt); weekStart.setDate(dt.getDate() - dayNum);
                    var weekEnd = new Date(weekStart); weekEnd.setDate(weekStart.getDate() + 6);
                    var ws = Math.max(0, Math.round((weekStart.getTime() - sd.getTime()) / 86400000));
                    var we = Math.min(p.totalDays, Math.round((weekEnd.getTime() - sd.getTime()) / 86400000) + 1);
                    parts.push('<text x="' + (x + (we - ws) * dw / 2) + '" y="42" text-anchor="middle" font-size="10" fill="#666">W' + wkn + '</text>');
                    lastW = wkn;
                }
            }
        }
        parts.push('</g>');
    }

    // ---- 活动箭线(先画,作为各层背景) ----
    var layout = p.layout, events = layout.events, activities = p.timeParams.activities;
    var actGroupOpen = false;
    var crossArcs: any[] = [];
    function emitAct() { if (!actGroupOpen) { parts.push('<g class="net-arrows">'); actGroupOpen = true; } }

    // 关键/非关键分离:先画非关键,再画关键(关键在线前)
    var showCritical = p.showCritical !== false;
    var critActs: any[] = [], nonCritActs: any[] = [];
    (activities || []).forEach(function(act: any) { (act.isCritical && showCritical ? critActs : nonCritActs).push(act); });

    // 工作编号: 非关键+虚工作先
    [nonCritActs, critActs].forEach(function(actList) {
        actList.forEach(function(act: any) {
            var srcEvt = events[act.source], tgtEvt = events[act.target];
            if (!srcEvt || !tgtEvt) return;
            var x1 = srcEvt.x || 0, y1 = srcEvt.y || 0;
            var x2 = tgtEvt.x || 0, y2 = tgtEvt.y || 0;
            var isCrit = act.isCritical;
            var isDummy = act.isDummy || (act.duration === 0 && act.id < 0);
            var strokeColor = isCrit && showCritical ? '#ff4d4f' : (isDummy ? '#52c41a' : '#1890ff');
            var strokeW = isCrit && showCritical ? 2.5 : (isDummy ? 1 : 1.5);
            var dash = isDummy ? '6,3' : (isCrit && showCritical ? 'none' : 'none');
            emitAct();

            if (actList === critActs && showCritical && critActs) {
                // 关键线路: 红色粗线 + 下方阴影
                parts.push('<line x1="' + x1 + '" y1="' + (y1 + 1) + '" x2="' + x2 + '" y2="' + (y2 + 1) + '" stroke="#ff4d4f" stroke-width="4" opacity="0.2"/>');
                parts.push('<line x1="' + x1 + '" y1="' + y1 + '" x2="' + x2 + '" y2="' + y2 + '" stroke="' + strokeColor + '" stroke-width="' + strokeW + '" stroke-dasharray="' + dash + '" data-activity-id="' + act.id + '"/>');
            } else {
                parts.push('<line x1="' + x1 + '" y1="' + y1 + '" x2="' + x2 + '" y2="' + y2 + '" stroke="' + strokeColor + '" stroke-width="' + strokeW + '" stroke-dasharray="' + dash + '" data-activity-id="' + act.id + '"/>');
            }

            // 工作名称/时长标注
            var midX = (x1 + x2) / 2, midY = (y1 + y2) / 2;
            if (!isDummy) {
                parts.push('<text x="' + midX + '" y="' + (midY - 4) + '" text-anchor="middle" font-size="9" fill="' + strokeColor + '">' + (act.code || '') + '</text>');
                parts.push('<text x="' + midX + '" y="' + (midY + 8) + '" text-anchor="middle" font-size="8" fill="#999">' + act.duration + '</text>');
            }

            // 箭头标记
            var angle = Math.atan2(y2 - y1, x2 - x1);
            var alen = 7;
            markers.push('<polygon points="' + (x2 - alen * Math.cos(angle - 0.4)) + ',' + (y2 - alen * Math.sin(angle - 0.4)) + ' ' + x2 + ',' + y2 + ' ' + (x2 - alen * Math.cos(angle + 0.4)) + ',' + (y2 - alen * Math.sin(angle + 0.4)) + '" fill="' + strokeColor + '"/>');

            // 收集交叉弧信息
            if (isDummy) crossArcs.push({ x1: x1, y1: y1, x2: x2, y2: y2, type: 'dummy' });
        });
    });
    if (actGroupOpen) parts.push('</g>');

    // ---- 箭头标记层 ----
    markers.forEach(function(m) { parts.push(m); });

    // ---- 虚工作关系箭线(如果showDummyArrows) ----
    if (p.showDummyArrows !== false) {
        // ...

    }

    // ---- 过桥弧 (交叉检测与跨线符) ----
    if (p.timeParams && p.timeParams.activities) {
      var allSegs = collectAllSegments(
        p.layout,
        p.timeParams.activities,
        p.timeParams.relations || [],
        isTimeMode,
        nr
      );
      generateCrossArcs(allSegs, parts, isTimeMode);
    }

    // ---- 事件节点 ----
    var nr = p.nodeRadius || 11;
    var nfs = Math.max(9, nr);
    var showFloat = p.showFloat !== false;

    parts.push('<g class="net-events">');
    Object.keys(events).forEach(function(eid) {
        var evt = events[eid];
        if (evt.isVirtual) return;
        var ex = evt.x || 0, ey = evt.y || 0;
        var isCrit = evt.isCritical && showCritical;
        void 0; // fill not used yet
        parts.push('<g class="net-event" data-event-id="' + eid + '" data-task-id="' + (evt.taskId || 0) + '">');
        if (isCrit) {
            parts.push('<circle cx="' + ex + '" cy="' + ey + '" r="' + nr + '" fill="#fff" stroke="#ff4d4f" stroke-width="3"/>');
            parts.push('<circle cx="' + ex + '" cy="' + ey + '" r="' + (nr - 4) + '" fill="#ff4d4f" stroke="#ff4d4f" stroke-width="1.5"/>');
        } else {
            parts.push('<circle cx="' + ex + '" cy="' + ey + '" r="' + nr + '" fill="#fff" stroke="#1890ff" stroke-width="1.5"/>');
        }
        parts.push('<text x="' + ex + '" y="' + (ey + 4) + '" text-anchor="middle" font-size="' + nfs + '" fill="' + (isCrit ? '#fff' : '#333') + '" font-weight="bold">' + (evt.num || eid) + '</text>');
        // ET 标注
        if (isTimeMode) {
            parts.push('<text x="' + ex + '" y="' + (ey + nr + 12) + '" text-anchor="middle" font-size="8" fill="#999">ET=' + evt.es + '</text>');
        }
        // TF 标注
        if (showFloat && evt.tf > 0) {
            parts.push('<text x="' + ex + '" y="' + (ey + nr + 22) + '" text-anchor="middle" font-size="7" fill="#e67e22">TF=' + evt.tf + '</text>');
        }
        parts.push('</g>');
    });
    parts.push('</g>');

    // ---- 今日线(红色虚线,从标尺顶部到底部标尺上方) ----
    if (isTimeMode && p.showTodayLine) {
      var today = new Date();
      var todayOffset = Math.floor((today.getTime() - sd.getTime()) / 86400000);
      if (todayOffset >= 0 && todayOffset < p.totalDays) {
        var tx = MARGIN_LEFT + todayOffset * dw + dw / 2;
        parts.push('<line x1="' + tx + '" y1="0" x2="' + tx + '" y2="' + (p.canvasH - 28) + '" stroke="#ff4d4f" stroke-width="1" stroke-dasharray="4,3" opacity="0.5"/>');
        parts.push('<text x="' + (tx + 4) + '" y="14" font-size="10" fill="#ff4d4f">今日 ' + today.getFullYear() + '-' + String(today.getMonth() + 1).padStart(2, '0') + '-' + String(today.getDate()).padStart(2, '0') + '</text>');
      }
    }

    // ---- 前锋线(检查日线) ----
    if (isTimeMode && p.showProgressLine && p.projectStartDate) {
      var progressSaved = window.localStorage && window.localStorage.getItem('netplan_progress_date');
      var gWin = window as any;
      var pdStr = progressSaved || (gWin._progressDate ? (gWin._progressDate.getFullYear() + '-' + String(gWin._progressDate.getMonth() + 1).padStart(2, '0') + '-' + String(gWin._progressDate.getDate()).padStart(2, '0')) : null);
      if (pdStr) {
        var pd = new Date(pdStr);
        var pOffset = Math.round((pd.getTime() - sd.getTime()) / 86400000);
        if (pOffset >= 0) {
          var px = MARGIN_LEFT + pOffset * dw;
          parts.push('<g id="net-progress-check">');
          parts.push('<line x1="' + px + '" y1="0" x2="' + px + '" y2="' + (p.canvasH - 28) + '" stroke="#52c41a" stroke-width="2" stroke-dasharray="6,3"/>');
          parts.push('<polygon points="' + (px - 7) + ',0 ' + (px + 7) + ',0 ' + px + ',14" fill="#52c41a"/>');
          parts.push('<text x="' + (px + 4) + '" y="14" font-size="11" fill="#52c41a" font-weight="bold">' + pdStr + '</text>');
          parts.push('</g>');
          
          // 进度三角标记
          var events = p.layout.events;
          Object.keys(events).forEach(function(eid) {
            var evt = events[eid];
            if (evt.isVirtual) return;
            if (pOffset >= evt.es && pOffset <= evt.ef) {
              var ratio = evt.ef > evt.es ? (pOffset - evt.es) / (evt.ef - evt.es) : 1;
              var progX = MARGIN_LEFT + (evt.es + ratio * (evt.ef - evt.es)) * dw;
              var progY = (evt.y || 0) - 4;
              parts.push('<polygon points="' + (progX - 4) + ',' + progY + ' ' + (progX + 4) + ',' + progY + ' ' + progX + ',' + (progY - 6) + '" fill="#52c41a"/>');
            }
          });
        }
      }
    }

    // ---- 进度曲线 ----
    if (p.showProgressCurve && isTimeMode) {
      var acts = (p.timeParams && p.timeParams.activities) || [];
      if (acts.length > 0) {
        var curvePoints: string[] = [];
        var curveYMin = 120, curveYMax = 90;
        var curveYRange = curveYMin - curveYMax;
        for (var d = 0; d < p.totalDays; d++) {
          var dayComp = 0, dayCount = 0;
          acts.forEach(function(a: any) {
            var aes = a.es || parseInt(String(a.source || 'T0').replace('T', '') || '0');
            var aef = a.ef || parseInt(String(a.target || 'T0').replace('T', '') || '0');
            if (d >= aes && d < aef) { dayComp += (a.completion || 0); dayCount++; }
          });
          var avgPct = dayCount > 0 ? dayComp / dayCount : -1;
          if (avgPct < 0) continue;
          var cpx = MARGIN_LEFT + d * dw + dw / 2;
          var cpy = curveYMax + curveYRange * (1 - avgPct / 100);
          curvePoints.push(cpx + ',' + cpy);
        }
        if (curvePoints.length > 1) {
          var firstX = curvePoints[0].split(',')[0];
          var lastX = curvePoints[curvePoints.length - 1].split(',')[0];
          var fillPath = 'M' + firstX + ',' + curveYMin + ' L' + curvePoints.join(' L') + ' L' + lastX + ',' + curveYMin + ' Z';
          parts.push('<path d="' + fillPath + '" fill="rgba(173,216,230,0.3)" stroke="none"/>');
          parts.push('<polyline fill="none" stroke="#5dade2" stroke-width="2" points="' + curvePoints.join(' ') + '"/>');
        }
      }
    }

    // ---- 计算最后一行的 Y 坐标 ----
    var lastRowY = 100; // 默认
    var lh = p.layerHeight || 60;
    Object.keys(events).forEach(function(eid) {
      var evt = events[eid];
      if (evt.y && evt.y > lastRowY) lastRowY = evt.y;
    });
    // 最右侧任务节点的 X 坐标
    var rightmostX = MARGIN_LEFT + (p.totalDays || 90) * dw;
    Object.keys(events).forEach(function(eid) {
      var evt = events[eid];
      if (evt.x && evt.x > rightmostX) rightmostX = evt.x;
    });

    // ---- 图例 ----
    var legendTop = lastRowY + lh + 10; // 最后一行下方一个行距
    parts.push('<rect x="10" y="' + legendTop + '" width="220" height="68" fill="rgba(255,255,255,0.95)" stroke="#ccc" rx="4"/>');
    parts.push('<text x="20" y="' + (legendTop + 15) + '" font-size="11" font-weight="bold" fill="#333">图例</text>');
    [
      { label: '关键线路', color: '#ff4d4f', w: 2.5, d: false },
      { label: '非关键工作', color: '#1890ff', w: 1.5, d: false },
      { label: '虚工作', color: '#52c41a', w: 1, d: true, dw: '6,3' }
    ].forEach(function(it, i) {
      var iy = legendTop + 28 + i * 13;
      parts.push('<line x1="20" y1="' + iy + '" x2="60" y2="' + iy + '" stroke="' + it.color + '" stroke-width="' + it.w + '"' + (it.d ? ' stroke-dasharray="' + (it.dw || '4,3') + '"' : '') + '/>');
      parts.push('<text x="66" y="' + (iy + 4) + '" font-size="9" fill="#333">' + it.label + '</text>');
    });

    // ---- 总工期 ----
    var totalDur = p.totalDuration || 0;
    var durationTextX = rightmostX + (p.nodeRadius || 11) * 2 + 10;
    parts.push('<text x="' + durationTextX + '" y="' + (lastRowY - 30) + '" font-size="12" font-weight="bold" fill="#e63946">总工期=' + totalDur + '天</text>');

    // ---- 底部镜像标尺 ----
    if (isTimeMode) {
      var upperH = 28, lowerH = 24;
      var bottomRulerY = p.canvasH - upperH - lowerH - 5;
      parts.push('<rect x="0" y="' + bottomRulerY + '" width="' + p.canvasW + '" height="' + upperH + '" fill="#fafafa"/>');
      parts.push('<rect x="0" y="' + (bottomRulerY + upperH) + '" width="' + p.canvasW + '" height="' + lowerH + '" fill="#f5f5f5"/>');
      var lastMonth = -1;
      for (var d = 0; d <= p.totalDays; d += 7) {
        var dt = new Date(sd.getTime() + d * 86400000);
        var x = MARGIN_LEFT + d * dw;
        if (dt.getMonth() !== lastMonth) {
          parts.push('<text x="' + x + '" y="' + (bottomRulerY + 18) + '" font-size="10" fill="#999"> ' + dt.getFullYear() + '-' + String(dt.getMonth() + 1).padStart(2, '0') + '</text>');
          lastMonth = dt.getMonth();
        }
      }
    }



    // ---- 组装 SVG ----
    var svg = '<svg id="network-svg" class="network-svg" xmlns="http://www.w3.org/2000/svg" width="' + p.canvasW + '" height="' + p.canvasH + '" style="background:#fafafa">\n';
    // 背景
    svg += '<rect x="0" y="0" width="' + p.canvasW + '" height="' + p.canvasH + '" fill="#fafafa" stroke="#e8e8e8"/>\n';
    // 行背景
    svg += renderRowBackground(p);
    // 标尺区背景
    if (isTimeMode) svg += '<rect x="0" y="0" width="' + p.canvasW + '" height="' + RULER_H + '" fill="#fff" stroke="#e8e8e8"/>\n';
    // 组件
    svg += parts.join('\n');
    svg += '\n</svg>';
    return svg;
}

function getISOWeek(dt: Date): number {
    var d = new Date(Date.UTC(dt.getFullYear(), dt.getMonth(), dt.getDate()));
    var dayNum = d.getUTCDay() || 7;
    d.setUTCDate(d.getUTCDate() + 4 - dayNum);
    var yearStart = new Date(Date.UTC(d.getUTCFullYear(), 0, 1));
    return Math.ceil((((d.getTime() - yearStart.getTime()) / 86400000) + 1) / 7);
}

function renderRowBackground(p: any): string {
    var events = p.layout.events;
    var layers: number[] = [];
    Object.keys(events).forEach(function(eid: string) {
        var evt = events[eid];
        if (evt.y !== undefined && layers.indexOf(evt.y) === -1) layers.push(evt.y);
    });
    layers.sort(function(a, b) { return a - b; });
    var parts: string[] = [];
    parts.push('<g class="net-row-bg">');
    layers.forEach(function(y, idx) {
        var lh = p.layerHeight || 60;
        var isEven = idx % 2 === 0;
        parts.push('<rect class="net-row-bg" x="0" y="' + (y - lh / 2) + '" width="' + p.canvasW + '" height="' + lh + '" fill="' + (isEven ? '#ffffff' : '#f5f5f5') + '" opacity="0.3"/>');
        if (p.mode === 'logic') {
            parts.push('<text x="4" y="' + (y + 4) + '" font-size="10" fill="#999">L' + idx + '</text>');
        }
    });
    parts.push('</g>');
    return parts.join('\n');
}
