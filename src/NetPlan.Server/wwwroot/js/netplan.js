// NetPlan.js - 甘特图交互脚本

// 双向滚动同步锁（防止左右互相触发导致无限循环）
var _syncLock = false;

function syncRightToLeft() {
    if (_syncLock) return;
    _syncLock = true;
    var rightDiv = document.getElementById('gantt-right');
    var leftDiv = document.getElementById('gantt-left-body');
    if (rightDiv && leftDiv) {
        leftDiv.scrollTop = rightDiv.scrollTop;
    }
    _syncLock = false;
}

function syncLeftToRight() {
    if (_syncLock) return;
    _syncLock = true;
    var rightDiv = document.getElementById('gantt-right');
    var leftDiv = document.getElementById('gantt-left-body');
    if (rightDiv && leftDiv) {
        rightDiv.scrollTop = leftDiv.scrollTop;
    }
    _syncLock = false;
}

// 供 C# 调用的入口
window.syncGanttScrollVertical = syncRightToLeft;
window.syncGanttScrollById = syncRightToLeft;

// 初始化双向滚动绑定
window.initGanttScroll = function() {
    var attempts = 0;
    var interval = setInterval(function() {
        var rightDiv = document.getElementById('gantt-right');
        var leftBody = document.getElementById('gantt-left-body');
        if (rightDiv && leftBody) {
            rightDiv.addEventListener('scroll', syncRightToLeft);
            leftBody.addEventListener('scroll', syncLeftToRight);
            clearInterval(interval);
        }
        attempts++;
        if (attempts > 50) clearInterval(interval);
    }, 100);
};

// ========== 左侧面板拖拽调整宽度 ==========
window.initPanelResize = function() {
    var attempts = 0;
    var interval = setInterval(function() {
        var handle = document.getElementById('gantt-resize-handle');
        var left = document.getElementById('gantt-left');
        if (handle && left) {
            clearInterval(interval);
            setupResize(handle, left);
        }
        attempts++;
        if (attempts > 50) clearInterval(interval);
    }, 100);
};

function setupResize(handle, leftPanel) {
    var startX, startWidth;
    var minWidth = 400;
    var maxWidth = 1400;

    handle.addEventListener('mousedown', function(e) {
        e.preventDefault();
        startX = e.clientX;
        startWidth = leftPanel.getBoundingClientRect().width;
        document.body.style.cursor = 'col-resize';
        document.body.style.userSelect = 'none';
        handle.classList.add('active');
        document.addEventListener('mousemove', onMouseMove);
        document.addEventListener('mouseup', onMouseUp);
    });

    function onMouseMove(e) {
        var delta = e.clientX - startX;
        var newWidth = Math.max(minWidth, Math.min(maxWidth, startWidth + delta));
        leftPanel.style.width = newWidth + 'px';
        leftPanel.style.minWidth = newWidth + 'px';
    }

    function onMouseUp() {
        document.body.style.cursor = '';
        document.body.style.userSelect = '';
        handle.classList.remove('active');
        document.removeEventListener('mousemove', onMouseMove);
        document.removeEventListener('mouseup', onMouseUp);
    }
}

document.addEventListener('DOMContentLoaded', function() {
    window.initGanttScroll();
    window.initPanelResize();
});

// ========== 拖拽偏移输入弹出框（甘特 + 网络公用）==========
var _dayPopup = null;
function _showDayPopup(deltaDays, cx, cy, onConfirm) {
    _hideDayPopup();
    _dayPopup = document.createElement('div');
    _dayPopup.style.cssText = 'position:fixed;z-index:9999;background:#fff;border:1px solid #d9d9d9;border-radius:6px;padding:8px 12px;box-shadow:0 4px 16px rgba(0,0,0,0.15);font-size:13px;';
    _dayPopup.style.left = cx + 'px';
    _dayPopup.style.top = (cy - 60) + 'px';
    var inp = document.createElement('input');
    inp.type = 'number';
    inp.value = deltaDays;
    inp.style.cssText = 'width:60px;border:1px solid #d9d9d9;border-radius:3px;padding:2px 6px;font-size:13px;';
    var lbl = document.createElement('span');
    lbl.textContent = ' 偏移 天 ';
    lbl.style.cssText = 'color:#666;font-size:12px;';
    var btn = document.createElement('button');
    btn.textContent = '确定';
    btn.style.cssText = 'margin-left:4px;background:#1890ff;color:#fff;border:none;border-radius:3px;padding:2px 10px;cursor:pointer;font-size:12px;';
    btn.onclick = function() { var v = parseInt(inp.value||'0'); _hideDayPopup(); onConfirm(v); };
    inp.onkeydown = function(e) { if (e.key==='Enter') { btn.click(); } else if (e.key==='Escape') { _hideDayPopup(); onConfirm(deltaDays); } };
    _dayPopup.appendChild(lbl);
    _dayPopup.appendChild(inp);
    _dayPopup.appendChild(btn);
    document.body.appendChild(_dayPopup);
    setTimeout(function() { inp.focus(); inp.select(); }, 50);
}
function _hideDayPopup() {
    if (_dayPopup) { _dayPopup.remove(); _dayPopup = null; }
}

// ========== 甘特图拖拽：防鬼影 + 松手弹出偏移输入 ==========
document.addEventListener('dragstart', function(e) {
    var bar = e.target.closest('.gantt-bar-frame');
    if (!bar) return;
    var img = new Image();
    img.src = 'data:image/gif;base64,R0lGODlhAQABAIAAAAAAAP///yH5BAEAAAAALAAAAAABAAEAAAIBRAA7';
    var rect = bar.getBoundingClientRect();
    e.dataTransfer.setDragImage(img, e.clientX - rect.left, e.clientY - rect.top);
    e.dataTransfer.effectAllowed = 'move';
    window._ganttDragCtx = {
        bar: bar,
        taskId: bar.getAttribute('data-task-id'),
        startClientX: e.clientX
    };
});

document.addEventListener('dragover', function(e) {
    if (window._ganttDragCtx) {
        e.preventDefault();
        e.dataTransfer.dropEffect = 'move';
    }
});

document.addEventListener('dragend', function() {
    var ctx = window._ganttDragCtx;
    if (!ctx) return;
    var dx = window._lastDragClientX ? window._lastDragClientX - ctx.startClientX : 0;
    var chart = document.querySelector('.gantt-chart');
    var dw = chart ? parseFloat(chart.getAttribute('data-day-width')) : 40;
    if (dw <= 0) dw = 40;
    var deltaDays = Math.round(dx / dw);
    var cx = window._lastDragClientX || 0, cy = window._lastDragClientY || 0;
    var taskId = ctx.taskId;
    window._ganttDragCtx = null;
    // 弹出输入框 → 确认后调用 C# MoveTaskDays
    _showDayPopup(deltaDays, cx, cy, function(days) {
        if (window._ganttDotNet && days !== 0)
            window._ganttDotNet.invokeMethodAsync('MoveTaskDays', parseInt(taskId), days);
    });
});

// 注册甘特图 DotNet 引用
window.setGanttDotNet = function(ref) { window._ganttDotNet = ref; };

// 记录最后的鼠标位置供 dragend 使用
document.addEventListener('drag', function(e) {
    if (e.clientX > 0) { window._lastDragClientX = e.clientX; window._lastDragClientY = e.clientY; }
});

// ========== Chart.js 资源投入折线图 ==========
let resourceChart = null;
window.initResourceChart = function(chartData) {
    if (typeof window.Chart === 'undefined') { console.warn('Chart.js not loaded'); return; }
    var canvas = document.getElementById('resource-chart');
    if (!canvas) { console.warn('resource-chart canvas not found'); return; }
    var ctx = canvas.getContext('2d');
    if (resourceChart) resourceChart.destroy();

    var datasets = chartData.lines.map(function(line) {
        return {
            label: line.name,
            data: line.points,
            borderColor: line.color,
            backgroundColor: line.color + '33',
            borderWidth: 2,
            pointRadius: 4,
            pointHoverRadius: 6,
            tension: 0.3,
            fill: false
        };
    });

    resourceChart = new Chart(ctx, {
        type: 'line',
        data: {
            labels: chartData.labels,
            datasets: datasets
        },
        options: {
            responsive: true,
            maintainAspectRatio: false,
            interaction: { mode: 'index', intersect: false },
            plugins: {
                legend: { position: 'top', labels: { boxWidth: 12, boxHeight: 12, font: { size: 12 } } },
                tooltip: { enabled: true }
            },
            scales: {
                x: {
                    ticks: { font: { size: 11 }, maxRotation: 45 },
                    grid: { display: false }
                },
                y: {
                    beginAtZero: true,
                    ticks: { font: { size: 11 } },
                    title: { display: true, text: '资源数量' }
                }
            }
        }
    });
};

// ========== 分析页面柱状图 ==========
window.renderAnalysisBarChart = function(canvasId, chartData) {
    if (typeof window.Chart === 'undefined') { console.warn('Chart.js not loaded'); return; }
    var canvas = document.getElementById(canvasId);
    if (!canvas) { console.warn(canvasId + ' not found'); return; }
    var ctx = canvas.getContext('2d');

    // 销毁旧图表
    if (canvas._chartInstance) {
        canvas._chartInstance.destroy();
    }

    var datasets = chartData.datasets.map(function(ds) {
        return {
            label: ds.label,
            data: ds.data,
            backgroundColor: ds.backgroundColor,
            borderColor: ds.borderColor || ds.backgroundColor,
            borderWidth: 1,
            barPercentage: 0.7,
            categoryPercentage: 0.8
        };
    });

    canvas._chartInstance = new Chart(ctx, {
        type: 'bar',
        data: {
            labels: chartData.labels,
            datasets: datasets
        },
        options: {
            responsive: true,
            maintainAspectRatio: false,
            plugins: {
                legend: {
                    position: 'top',
                    labels: { boxWidth: 12, font: { size: 12 } }
                },
                tooltip: { mode: 'index', intersect: false }
            },
            scales: {
                x: {
                    ticks: { font: { size: 11 }, maxRotation: 45 }
                },
                y: {
                    beginAtZero: true,
                    ticks: { font: { size: 11 } }
                }
            }
        }
    });
};

// ============================================================================
// 时标双代号网络图 - SVG实现（符合JGJ/T 121-2015规范）
// ============================================================================

var _netDotNet = null;
var _lastRenderedToken = null;
// 节点拖拽全局状态
var _netNodeDrag = null;
var _netLayout = null;
var _netActivities = null;
var _netSvg = null;
var _netEventOffsets = {};
var _netDayWidth = 40;
// 前锋线全局状态
var _networkCanvas = null;
var _networkData = null;
var _networkOpts = null;
var _netRendered = false;
var _progressDate = null;   // 前锋线检查日期（Date）
var _progressX = 0;         // 前锋线像素位置
var _dragInfo = null;       // 拖动状态 { startX, startScroll }
var _projStartDate = null;  // 项目开始日期（Date）

// .NET引用设置
window.setNetworkDotNet = function(ref) {
    _netDotNet = ref;
};

// ISO 8601 周次（甘特图/网络图标尺共用）
function getISOWeek(d) {
    var dt = new Date(Date.UTC(d.getFullYear(), d.getMonth(), d.getDate()));
    var dayNum = dt.getUTCDay() || 7;
    dt.setUTCDate(dt.getUTCDate() + 4 - dayNum);
    var yearStart = new Date(Date.UTC(dt.getUTCFullYear(), 0, 1));
    return Math.ceil((((dt - yearStart) / 86400000) + 1) / 7);
}

// ============================================================================
// 第一步：节点计算法时间参数计算
// ============================================================================

function calculateTimeParams(tasks, relations) {
    // 为每个任务创建开始事件和结束事件
    var events = {};
    var activities = [];
    var eventList = []; // 有序事件列表

    tasks.forEach(function(task) {
        var startId = 'S' + task.id;
        var endId = 'E' + task.id;
        var es = task.es || 0;
        var ef = task.ef || (es + (task.duration || 0));

        if (!events[startId]) {
            events[startId] = {
                id: startId,
                taskId: task.id,
                type: 'start',
                num: 0,
                es: es,
                ef: es,
                ls: 0,
                lf: 0,
                tf: task.tf || 0,
                ff: 0,
                isCritical: task.isCritical || false
            };
            eventList.push(startId);
        }
        if (!events[endId]) {
            events[endId] = {
                id: endId,
                taskId: task.id,
                type: 'end',
                num: 0,
                es: ef,
                ef: ef,
                ls: 0,
                lf: 0,
                tf: task.tf || 0,
                ff: task.ff || 0,
                isCritical: task.isCritical || false
            };
            eventList.push(endId);
        }

        activities.push({
            id: task.id,
            source: startId,
            target: endId,
            duration: task.duration || 0,
            code: task.label || '',
            name: task.name || '',
            isCritical: task.isCritical || false,
            tf: task.tf || 0,
            ff: task.ff || 0,
            completion: task.completion || 0
        });
    });

    // 建立事件的前驱后继关系
    var eventPred = {};
    var eventSucc = {};
    Object.keys(events).forEach(function(eid) {
        eventPred[eid] = [];
        eventSucc[eid] = [];
    });

    // 从任务建立连接：startEvent -> endEvent
    activities.forEach(function(act) {
        if (events[act.source] && events[act.target]) {
            eventSucc[act.source].push(act.target);
            eventPred[act.target].push(act.source);
        }
    });

    // 从关系建立虚箭线连接
    relations.forEach(function(rel) {
        var predEndId = 'E' + rel.source;
        var succStartId = 'S' + rel.target;
        if (events[predEndId] && events[succStartId]) {
            eventSucc[predEndId].push(succStartId);
            eventPred[succStartId].push(predEndId);
        }
    });

    // 拓扑排序并分配事件编号
    var inDeg = {};
    Object.keys(events).forEach(function(eid) {
        inDeg[eid] = eventPred[eid].length;
    });
    var queue = [];
    Object.keys(inDeg).forEach(function(eid) {
        if (inDeg[eid] === 0) queue.push(eid);
    });
    var sortedEvents = [];
    while (queue.length > 0) {
        var curr = queue.shift();
        sortedEvents.push(curr);
        eventSucc[curr].forEach(function(next) {
            inDeg[next]--;
            if (inDeg[next] === 0) queue.push(next);
        });
    }

    // 分配事件编号（拓扑序）
    sortedEvents.forEach(function(eid, idx) {
        if (events[eid]) events[eid].num = idx + 1;
    });

    return {
        events: events,
        activities: activities,
        relations: relations,
        eventPred: eventPred,
        eventSucc: eventSucc,
        sortedEvents: sortedEvents
    };
}

// ============================================================================
// 第二步：垂直分层布局（关键线路最上层）
// ============================================================================

function calculateVerticalLayout(data) {
    var events = data.events;
    var activities = data.activities;
    var eventPred = data.eventPred;
    var eventSucc = data.eventSucc;

    var LAYER_HEIGHT = 60;  // 紧凑布局
    var MARGIN_TOP = 100;

    // 标记关键线路事件
    var criticalEvents = {};
    activities.forEach(function(act) {
        if (act.isCritical) {
            criticalEvents[act.source] = true;
            criticalEvents[act.target] = true;
        }
    });

    // ===== 改进的分层算法：拓扑BFS（单向推进，不会振荡）=====
    var eventLayer = {};
    var inDeg = {};
    Object.keys(events).forEach(function(eid) {
        inDeg[eid] = eventPred[eid] ? eventPred[eid].length : 0;
    });

    // 关键事件优先分配到第1层
    var queue = [];
    Object.keys(events).forEach(function(eid) {
        if (inDeg[eid] === 0) {
            eventLayer[eid] = criticalEvents[eid] ? 1 : 2;
            queue.push(eid);
        }
    });

    // BFS拓扑推进：每往下走一层，层次+1
    while (queue.length > 0) {
        var current = queue.shift();
        var successorIds = eventSucc[current] || [];
        successorIds.forEach(function(next) {
            // 计算后继事件的层次：当前层次 + 1（非关键再加1）
            var proposedLayer = eventLayer[current] + (criticalEvents[next] ? 1 : 2);
            if (eventLayer[next] === undefined || eventLayer[next] < proposedLayer) {
                eventLayer[next] = proposedLayer;
            }
            inDeg[next]--;
            if (inDeg[next] === 0) {
                queue.push(next);
            }
        });
    }

    // 边界处理：未分配层次的事件（孤立节点）
    Object.keys(events).forEach(function(eid) {
        if (eventLayer[eid] === undefined) {
            eventLayer[eid] = criticalEvents[eid] ? 1 : 2;
        }
    });

    // 按TF微调
    Object.keys(events).forEach(function(eid) {
        if (!criticalEvents[eid]) {
            var tf = events[eid].tf || 0;
            var extraLayer = Math.min(Math.floor(tf / 14), 3); // 紧凑系数
            eventLayer[eid] = eventLayer[eid] + extraLayer;
        }
    });

    // === 同行约束：仅工作箭线两端同一行（虚箭线允许多行）===
    // 单次遍历即可：每条活动的目标事件层拉到与源事件同层
    activities.forEach(function(act) {
        var sl = eventLayer[act.source], tl = eventLayer[act.target];
        if (sl !== undefined && tl !== undefined && sl !== tl) {
            var ml = Math.max(sl, tl);
            eventLayer[act.source] = ml;
            eventLayer[act.target] = ml;
        }
    });

    // 将事件按层级分组
    var layerEvents = {};
    Object.keys(eventLayer).forEach(function(eid) {
        var layer = eventLayer[eid];
        if (!layerEvents[layer]) layerEvents[layer] = [];
        layerEvents[layer].push(eid);
    });

    // 分配Y坐标（同一层级y相同，不同层级相差LAYER_HEIGHT）
    Object.keys(layerEvents).forEach(function(layer) {
        var y = MARGIN_TOP + (parseInt(layer) - 1) * LAYER_HEIGHT;
        layerEvents[layer].forEach(function(eid) {
            events[eid].y = y;
        });
    });

    // 分配X坐标需要在renderNetwork中做（依赖dayWidth）
    // 此处只分配Y坐标和层级

    return {
        events: events,
        eventLayer: eventLayer,
        layerEvents: layerEvents,
        LAYER_HEIGHT: LAYER_HEIGHT,
        MARGIN_TOP: MARGIN_TOP
    };
}


// ============================================================================
// SVG 绘制函数（替代 Canvas）
// ============================================================================
var _svgArrowId = 0;
function svgArrowMarker(crit) {
    _svgArrowId++;
    var color = crit ? '#e63946' : '#333';
    var id = 'arr-' + _svgArrowId;
    var m = '<marker id="' + id + '" markerWidth="8" markerHeight="8" refX="7" refY="4" orient="auto">'
          + '<path d="M0,0 L8,4 L0,8 Z" fill="' + color + '"/></marker>';
    return { id: 'url(#' + id + ')', html: m, color: color };
}

function buildNetworkSvg(params) {
    var p = params, parts = [], markers = [];
    var MARGIN_LEFT = 80, RULER_H = 52, NODE_R = 17; // RULER_H 对齐甘特图: 上层28+下层24
    var cw = p.canvasW, ch = p.canvasH;
    console.log('[SVG] opts:', 'todayLine=', p.showTodayLine, 'progressLine=', p.showProgressLine,
        'dayWidth=', p.dayWidth, 'critical=', p.showCritical, 'float=', p.showFloat);

    // === 时间标尺（精确对齐甘特图：上层28 + 下层24 = 52px）===
    var sd = new Date(p.projectStartDate);
    var dw = p.dayWidth, td = p.totalDays;
    var upperH = 28, lowerY = 28, lowerH = 24;

    // 背景
    parts.push('<rect x="0" y="0" width="' + cw + '" height="' + upperH + '" fill="url(#rulerGrad)"/>');
    parts.push('<defs><linearGradient id="rulerGrad" x1="0" y1="0" x2="0" y2="1">'
        + '<stop offset="0%" stop-color="#f5f5f5"/><stop offset="100%" stop-color="#fafafa"/></linearGradient></defs>');
    parts.push('<rect x="0" y="' + lowerY + '" width="' + cw + '" height="' + lowerH + '" fill="#f5f5f5"/>');

    // 上层：年/月标签（font12 bold #333）
    if (dw > 20) {
        // full模式：每月 yyyy/MM
        for (var d = 0; d < td; d++) {
            var dt = new Date(sd); dt.setDate(dt.getDate() + d);
            if (dt.getDate() === 1) {
                var x = MARGIN_LEFT + d * dw;
                var monthSpan = Math.min(new Date(dt.getFullYear(), dt.getMonth()+1, 0).getDate() - dt.getDate() + 1, td - d);
                var mw = monthSpan * dw;
                parts.push('<text x="' + (x + mw/2) + '" y="18" font-size="12" font-weight="600" fill="#333" text-anchor="middle">'
                    + dt.getFullYear() + '/' + String(dt.getMonth()+1).padStart(2,'0') + '</text>');
            }
        }
    } else {
        // year模式：按年分组
        var lastYear = -1, yearStartX = 0;
        for (var d = 0; d <= td; d++) {
            var dt = new Date(sd); dt.setDate(dt.getDate() + d);
            var cy = dt.getFullYear();
            if (cy !== lastYear) {
                if (lastYear >= 0) {
                    var yw = MARGIN_LEFT + d * dw - yearStartX;
                    parts.push('<text x="' + (yearStartX + yw/2) + '" y="18" font-size="12" font-weight="600" fill="#333" text-anchor="middle">'
                        + lastYear + '</text>');
                }
                lastYear = cy; yearStartX = MARGIN_LEFT + d * dw;
            }
        }
        if (lastYear >= 0) {
            var lastW = MARGIN_LEFT + td * dw - yearStartX;
            parts.push('<text x="' + (yearStartX + lastW/2) + '" y="18" font-size="12" font-weight="600" fill="#333" text-anchor="middle">'
                + lastYear + '</text>');
        }
    }

    // 分隔线
    parts.push('<line x1="0" y1="' + lowerY + '" x2="' + cw + '" y2="' + lowerY + '" stroke="#d9d9d9"/>');
    parts.push('<line x1="0" y1="' + RULER_H + '" x2="' + cw + '" y2="' + RULER_H + '" stroke="#b0b0b0" stroke-width="2"/>');

    // 下层：日/周/月（font-size 10, #666，周末高亮对齐甘特图）
    if (dw > 20) {
        for (var d = 0; d <= td; d++) {
            var dt = new Date(sd); dt.setDate(dt.getDate() + d);
            var x = MARGIN_LEFT + d * dw;
            var dow = dt.getDay();
            var isWeekend = (dow === 0 || dow === 6);
            if (isWeekend) {
                parts.push('<rect x="' + x + '" y="' + lowerY + '" width="' + dw
                    + '" height="' + lowerH + '" fill="#f0f0f0"/>');
            }
            parts.push('<text x="' + (x + dw/2) + '" y="42" font-size="10" fill="'
                + (isWeekend ? '#999' : '#666') + '" text-anchor="middle">'
                + String(dt.getDate()).padStart(2,'0') + '</text>');
        }
    } else if (dw >= 10) {
        var lastWeek = -1;
        for (var d = 0; d < td; d++) {
            var dt = new Date(sd); dt.setDate(dt.getDate() + d);
            var x = MARGIN_LEFT + d * dw;
            var week = getISOWeek(dt);
            if (week !== lastWeek) {
                lastWeek = week;
                parts.push('<text x="' + (x + 3) + '" y="42" font-size="10" fill="#666">第' + week + '周</text>');
            }
        }
    } else {
        for (var d = 0; d < td; d++) {
            var dt = new Date(sd); dt.setDate(dt.getDate() + d);
            if (dt.getDate() === 1) {
                var x = MARGIN_LEFT + d * dw;
                parts.push('<text x="' + (x + 3) + '" y="42" font-size="10" fill="#666">' + String(dt.getMonth()+1) + '月</text>');
            }
        }
    }

    // 竖线网格
    var fullH = ch + RULER_H;
    // 年度粗线
    var lastY = -1;
    for (var d = 0; d <= td; d++) {
        var dt = new Date(sd); dt.setDate(dt.getDate() + d);
        if (dt.getMonth() === 0 && dt.getDate() === 1 && dt.getFullYear() !== lastY) {
            var yx = MARGIN_LEFT + d * dw;
            parts.push('<line x1="' + yx + '" y1="0" x2="' + yx + '" y2="' + fullH + '" stroke="#ccc" stroke-width="1.5"/>');
            lastY = dt.getFullYear();
        }
    }
    // 月度细线
    for (var d = 0; d <= td; d++) {
        var dt = new Date(sd); dt.setDate(dt.getDate() + d);
        if (dt.getDate() === 1 && !(dt.getMonth() === 0 && dt.getDate() === 1)) {
            var mx = MARGIN_LEFT + d * dw;
            parts.push('<line x1="' + mx + '" y1="' + RULER_H + '" x2="' + mx + '" y2="' + fullH + '" stroke="#f0f0f0" stroke-width="0.5"/>');
        }
    }
    // 水平行线（层间参考线）
    var drawnY = {};
    Object.keys(p.layout.events).forEach(function(eid) {
        var ey = p.layout.events[eid].y;
        if (!drawnY[ey]) {
            drawnY[ey] = true;
            parts.push('<line x1="0" y1="' + ey + '" x2="' + cw + '" y2="' + ey + '" stroke="#e8e8e8" stroke-width="0.5" stroke-dasharray="4,2"/>');
        }
    });

    // === 虚箭线 ===
    p.timeParams.relations.forEach(function(rel) {
        var src = p.layout.events['E' + rel.source];
        var tgt = p.layout.events['S' + rel.target];
        if (!src || !tgt) return;
        var mx = src.x + (tgt.x - src.x) / 2;
        var d = 'M' + src.x + ' ' + src.y + ' L' + mx + ' ' + src.y
              + ' L' + mx + ' ' + tgt.y + ' L' + tgt.x + ' ' + tgt.y;
        parts.push('<g class="dummy-rel" data-dummy-src="E' + rel.source + '" data-dummy-tgt="S' + rel.target + '">');
        parts.push('<path d="' + d + '" fill="none" stroke="#aaa" stroke-width="1" stroke-dasharray="5,3"/>');
        parts.push('</g>');
    });

    // === 工作箭线 ===
    p.timeParams.activities.forEach(function(act) {
        var src = p.layout.events[act.source], tgt = p.layout.events[act.target];
        if (!src || !tgt) return;
        var isCrit = act.isCritical && p.showCritical;
        var sx = src.x + NODE_R, sy = src.y, ex = tgt.x - NODE_R, ey = tgt.y;
        var dur = act.duration || 0;

        var arrow = svgArrowMarker(isCrit);
        // 箭头已取消，不推入 markers

        var pathD;
        if (Math.abs(ey - sy) < 5) {
            pathD = 'M' + sx + ' ' + sy + ' L' + (ex - 2) + ' ' + ey;
        } else {
            pathD = 'M' + sx + ' ' + sy + ' L' + ex + ' ' + sy + ' L' + ex + ' ' + ey;
        }
        parts.push('<g data-activity-id="' + act.id + '" data-src="' + act.source + '" data-tgt="' + act.target + '">');
        parts.push('<path class="act-arrow" d="' + pathD + '" fill="none" stroke="' + arrow.color
            + '" stroke-width="' + (isCrit ? 3 : 1.5) + '"/>');

        var lx = (sx + ex) / 2, ly = sy - NODE_R - 6;
        var label = (act.code || '') + (act.name ? ' ' + act.name : '');
        if (label.length > 25) label = label.substring(0, 23) + '...';
        parts.push('<text class="act-label" x="' + lx + '" y="' + ly + '" font-size="10" fill="' + arrow.color
            + '" text-anchor="middle" font-weight="' + (isCrit ? 'bold' : 'normal') + '">' + label + '</text>');
        parts.push('<text class="act-dur" x="' + lx + '" y="' + (sy + NODE_R + 10) + '" font-size="9" fill="#666" text-anchor="middle">'
            + dur + 'd</text>');

        if (p.showFloat && act.ff > 0 && !isCrit) {
            var fex = Math.min(ex + act.ff * p.dayWidth, cw - 10);
            var wp = 'M' + ex + ' ' + ey;
            for (var s = 0; s < 10; s++) {
                var t = s / 10;
                wp += ' L' + (ex + (fex - ex) * t) + ' ' + (ey + (s % 2 ? -3 : 3));
            }
            parts.push('<path class="act-wave" d="' + wp + '" fill="none" stroke="#FFD700" stroke-width="1" stroke-dasharray="4,3"/>');
        }
        parts.push('</g>');
    });

    // === 事件节点 ===
    Object.keys(p.layout.events).forEach(function(eid) {
        var evt = p.layout.events[eid];
        var isCrit = evt.isCritical && p.showCritical;
        var fill = isCrit ? '#e63946' : '#fff', stroke = isCrit ? '#b30000' : '#e63946';
        var tc = isCrit ? '#fff' : '#000';
        parts.push('<g class="net-event" data-task-id="' + evt.taskId + '" data-event-id="' + eid + '" style="cursor:grab;">');
        parts.push('<circle cx="' + evt.x + '" cy="' + evt.y + '" r="' + NODE_R
            + '" fill="' + fill + '" stroke="' + stroke + '" stroke-width="2"/>');
        parts.push('<text x="' + evt.x + '" y="' + (evt.y + 1)
            + '" dominant-baseline="middle" text-anchor="middle" font-size="' + Math.floor(NODE_R * 0.7)
            + '" font-weight="bold" fill="' + tc + '" style="pointer-events:none;">' + evt.num + '</text>');
        parts.push('</g>');
    });

    // === 今日线（红色虚线，贯穿全图）===
    if (p.showTodayLine) {
        var today = new Date();
        var todayOffset = Math.floor((today.getTime() - sd.getTime()) / 86400000);
        if (todayOffset >= 0 && todayOffset < td) {
            var tx = MARGIN_LEFT + todayOffset * dw + dw / 2;
            parts.push('<line x1="' + tx + '" y1="0" x2="' + tx + '" y2="' + (ch + RULER_H) + '" stroke="#e74c3c" stroke-width="2" stroke-dasharray="6,3"/>');
            parts.push('<text x="' + (tx + 4) + '" y="' + (upperH / 2 + 4) + '" font-size="10" fill="#e74c3c" font-weight="bold">今日</text>');
        }
    }

    // === 可拖动前锋线（JGJ/T121-2015）===
    if (p.showProgressLine) {
        var today = new Date();
        var progressOffset = Math.floor((today.getTime() - sd.getTime()) / 86400000);
        if (progressOffset < 0) progressOffset = 0;
        if (progressOffset >= td) progressOffset = td - 1;
        var pxLine = MARGIN_LEFT + progressOffset * dw + dw / 2;
        _progressDate = new Date(sd); _progressDate.setDate(_progressDate.getDate() + Math.round(progressOffset));
        _progressX = pxLine;
        _projStartDate = sd;
        var checkLabel = _progressDate.getFullYear() + '-' + String(_progressDate.getMonth()+1).padStart(2,'0') + '-' + String(_progressDate.getDate()).padStart(2,'0');
        parts.push('<g id="net-progress-line" style="cursor:ew-resize;">'
            + '<line id="net-pl-line" x1="' + pxLine + '" y1="0" x2="' + pxLine + '" y2="' + (ch + RULER_H) + '" stroke="#e74c3c" stroke-width="2"/>'
            + '<polygon id="net-pl-handle" points="' + (pxLine-7) + ',0 ' + (pxLine+7) + ',0 ' + pxLine + ',14" fill="#e74c3c"/>'
            + '<text id="net-pl-label" x="' + (pxLine + 4) + '" y="12" font-size="10" fill="#e74c3c" font-weight="bold">' + checkLabel + '</text>'
            + '</g>');
    }

    // === 图例 ===
    var lx = cw - 220, ly = ch - 75;
    parts.push('<rect x="' + lx + '" y="' + ly + '" width="200" height="65" fill="rgba(255,255,255,0.95)" stroke="#ccc"/>');
    parts.push('<text x="' + (lx + 10) + '" y="' + (ly + 15) + '" font-size="11" font-weight="bold" fill="#333">图例</text>');
    [
        { label: '关键线路', color: '#e63946', w: 3, d: false },
        { label: '非关键工作', color: '#333', w: 1.5, d: false },
        { label: '虚工作', color: '#aaa', w: 1, d: true }
    ].forEach(function(it, i) {
        var iy = ly + 30 + i * 13;
        parts.push('<line x1="' + (lx + 10) + '" y1="' + iy + '" x2="' + (lx + 50) + '" y2="' + iy
            + '" stroke="' + it.color + '" stroke-width="' + it.w + '"'
            + (it.d ? ' stroke-dasharray="4,3"' : '') + '/>');
        parts.push('<text x="' + (lx + 56) + '" y="' + (iy + 4) + '" font-size="9" fill="#333">' + it.label + '</text>');
    });

    // === 标题 ===
    parts.push('<text x="10" y="80" font-size="13" font-weight="bold" fill="#333">' + (p.projectName || '') + '</text>');
    parts.push('<text x="10" y="97" font-size="13" font-weight="bold" fill="#333">时标网络计划图</text>');

    // === 总工期（对齐甘特图：首末任务日期跨度）===
    var totalDur = p.totalDuration || 0;
    parts.push('<text x="' + (cw - 12) + '" y="80" font-size="12" font-weight="bold" fill="#e63946" text-anchor="end">'
        + '总工期=' + totalDur + '天</text>');

    // === 规程标注 ===
    parts.push('<text x="10" y="' + (ch - 5) + '" font-size="9" fill="#999">符合《工程网络计划技术规程JGJ/T 121-2015》</text>');

    var svgStr = '<svg xmlns="http://www.w3.org/2000/svg" id="network-svg" width="' + cw + '" height="' + (ch + RULER_H) + '" style="display:block;font-family:SimSun,sans-serif;">';
    svgStr += '<defs>' + markers.join('') + '</defs>';
    svgStr += parts.join('');
    svgStr += '</svg>';
    return svgStr;
}

// ============================================================================
// JGJ/T121-2015 前锋线进度评估
// ============================================================================
function updateProgressColors() {
    if (!_progressDate || !_projStartDate) return;
    var svg = document.querySelector('#cy svg');
    if (!svg) return;

    var checkDays = Math.round((_progressDate.getTime() - _projStartDate.getTime()) / 86400000);

    // 遍历所有事件节点的 <g class="net-event">，查找其对应的 activity 数据
    var eventGroups = svg.querySelectorAll('.net-event');
    eventGroups.forEach(function(g) {
        var tid = parseInt(g.getAttribute('data-task-id'));
        if (!tid) return;

        // 从 _networkData 中查找 activity
        var act = null;
        if (_networkData && _networkData.activities) {
            for (var i = 0; i < _networkData.activities.length; i++) {
                if (_networkData.activities[i].id === tid) { act = _networkData.activities[i]; break; }
            }
        }
        if (!act) return;

        // 找到开始事件获取 ES
        var srcEvt = _networkData.events['S' + tid];
        var es = srcEvt ? srcEvt.es : 0;
        var dur = act.duration || 1;

        // JGJ/T121-2015: 计划完成百分比 = (检查日 - ES) / 工期 × 100
        var plannedPct = Math.max(0, Math.min(100, (checkDays - es) / dur * 100));
        var actualPct = act.completion || 0;
        var delta = actualPct - plannedPct;

        var statusColor;
        if (delta >= 0) statusColor = '#27ae60';        // 绿色：正常/超前
        else if (delta >= -20) statusColor = '#f39c12'; // 黄色：轻微落后
        else statusColor = '#e74c3c';                    // 红色：严重落后

        // 关键路径节点保持红色（不覆盖）
        var isCrit = act.isCritical && _networkOpts && _networkOpts.showCritical !== false;
        if (!isCrit) {
            var circle = g.querySelector('circle');
            if (circle) {
                circle.setAttribute('fill', statusColor);
                circle.setAttribute('stroke', statusColor);
            }
            var text = g.querySelector('text');
            if (text) text.setAttribute('fill', '#fff');
        }
    });
}

// 初始化前锋线颜色（首次渲染后调用）
window.initProgressColors = function() {
    updateProgressColors();
};
window.renderNetwork = function(elementsJson, opts) {
    opts = opts || {};
    var showCritical = opts.showCritical !== false;
    var showFloat = opts.showFloat !== false;
    var showTodayLine = opts.showTodayLine !== false;
    var showProgressLine = opts.showProgressLine !== false;
    var psd = opts.projectStartDate || new Date().toISOString().slice(0, 10);
    var totalDays = opts.totalDays || 90, dayWidth = opts.dayWidth || 8;
    var pn = opts.projectName || '网络计划';
    console.log('[NET] SVG render. opts:', JSON.stringify({totalDays, dayWidth, showTodayLine, showProgressLine}));
    _netRendered = false;

    var ce = document.getElementById('cy');
    if (!ce) { console.error('[NET] cy not found'); return; }

    var elements;
    try { elements = JSON.parse(elementsJson); } catch(e) { console.error('JSON parse', e); return; }

    var tasks = [], rels = [];
    elements.forEach(function(el) {
        if (el.data) {
            if (el.data.es !== undefined) tasks.push(el.data);
            if (el.data.source) rels.push(el.data);
        }
    });
    if (!tasks.length) { console.warn('[NET] No tasks'); return; }
    console.log('[NET] tasks:', tasks.length, '| rels:', rels.length);

    var tp = calculateTimeParams(tasks, rels);
    var layout = calculateVerticalLayout(tp);

    var ML = 80, RH = 70;
    Object.keys(layout.events).forEach(function(eid) {
        layout.events[eid].x = ML + (layout.events[eid].es || 0) * dayWidth;
    });

    var cx = ML + totalDays * dayWidth + 100;
    // 自适应高度：取最大实际 Y 坐标 + 120px 底部留白
    var maxY = 0;
    Object.keys(layout.events).forEach(function(eid) {
        if (layout.events[eid].y > maxY) maxY = layout.events[eid].y;
    });
    var cySize = Math.max(maxY + 120, 400);
    console.log('[NET] SVG size:', cx, 'x', cySize);

    ce.innerHTML = buildNetworkSvg({
        projectStartDate: psd, totalDays: totalDays, totalDuration: opts.totalDuration || 0, dayWidth: dayWidth,
        timeParams: tp, layout: layout,
        showCritical: showCritical, showFloat: showFloat,
        showTodayLine: showTodayLine, showProgressLine: showProgressLine,
        projectName: pn,
        canvasW: cx, canvasH: cySize
    });
    console.log('[NET] SVG rendered.');

    var svg = ce.querySelector('svg');
    if (svg && _netDotNet) {
        svg.ondblclick = function(e) {
            var el = e.target.closest('.net-event');
            if (!el) return;
            var tid = parseInt(el.getAttribute('data-task-id'));
            if (tid) _netDotNet.invokeMethodAsync('OpenTaskEditor', tid);
        };
    }

    // ===== 节点拖拽微调 =====
    // 更新全局引用（每次重渲染刷新）
    _netLayout = layout;
    _netActivities = tp.activities;
    _netSvg = svg;
    _netDayWidth = dayWidth;
    // 恢复已保存的节点偏移（重渲染后保持位置）
    Object.keys(_netEventOffsets).forEach(function(eid) {
        var off = _netEventOffsets[eid];
        var g = svg && svg.querySelector('[data-event-id="' + eid + '"]');
        if (g && (off.x !== 0 || off.y !== 0)) {
            g.setAttribute('transform', 'translate(' + off.x + ',' + off.y + ')');
        }
    });
    // 重渲染后恢复箭头路径（基于保存偏移）
    Object.keys(_netEventOffsets).forEach(function(eid) {
        var off = _netEventOffsets[eid];
        if (off.x !== 0 || off.y !== 0) {
            _netUpdateArrows(eid, off.x, off.y);
            _netUpdateDummys(eid, off.x, off.y);
        }
    });

    if (svg) {
        svg.addEventListener('mousedown', function(e) {
            var g = e.target.closest('.net-event');
            if (!g) return;
            e.preventDefault();
            e.stopPropagation();
            var eid = g.getAttribute('data-event-id');
            var off = _netEventOffsets[eid] || { x: 0, y: 0 };
            _netNodeDrag = { eventId: eid, group: g, startX: e.clientX, startY: e.clientY, offX: off.x, offY: off.y };
            g.style.cursor = 'grabbing';
        });
    }

    // ===== 前锋线拖动 =====
    if (showProgressLine) {
        var plGroup = document.getElementById('net-progress-line');
        if (plGroup && svg) {
            plGroup.onmousedown = function(e) {
                e.preventDefault();
                var bodyEl = document.getElementById('network-body');
                _dragInfo = { startX: e.clientX, startLineX: _progressX, startScrollLeft: bodyEl ? bodyEl.scrollLeft : 0 };
            };
        }
    }

    // 全局拖动事件（只绑定一次）
    if (!window._netDragBound) {
        window._netDragBound = true;
        document.addEventListener('mousemove', function(e) {
            if (!_dragInfo) return;
            var dx = e.clientX - _dragInfo.startX;
            var bodyEl = document.getElementById('network-body');
            var scrollDelta = (bodyEl ? bodyEl.scrollLeft : 0) - _dragInfo.startScrollLeft;
            var newX = _dragInfo.startLineX + dx + scrollDelta;

            // 限制在标尺范围内
            var minX = 80, maxX = parseFloat(svg.getAttribute('width')) - 100;
            newX = Math.max(minX, Math.min(newX, maxX));

            // 移动前锋线元素
            var line = document.getElementById('net-pl-line');
            var handle = document.getElementById('net-pl-handle');
            var label = document.getElementById('net-pl-label');
            if (line) { line.setAttribute('x1', newX); line.setAttribute('x2', newX); }
            if (handle) handle.setAttribute('points', (newX-7)+',0 '+(newX+7)+',0 '+newX+',14');
            if (label) label.setAttribute('x', newX + 4);

            // 计算新检查日期
            if (_projStartDate) {
                var dayOffset = Math.round((newX - 80) / dayWidth);
                _progressDate = new Date(_projStartDate);
                _progressDate.setDate(_progressDate.getDate() + dayOffset);
                _progressX = newX;
                var cl = _progressDate.getFullYear()+'-'+String(_progressDate.getMonth()+1).padStart(2,'0')+'-'+String(_progressDate.getDate()).padStart(2,'0');
                if (label) label.textContent = cl;
            }

            // 更新进度颜色
            updateProgressColors();
        });

        document.addEventListener('mouseup', function() {
            _dragInfo = null;
        });
    }

    // 首次渲染后初始化前锋线颜色
    setTimeout(function() { updateProgressColors(); }, 200);

    // === 固定横向滚动条同步 ===
    var hscroll = document.getElementById('network-hscroll');
    var hscrollInner = document.getElementById('network-hscroll-inner');
    var bodyEl = document.getElementById('network-body');
    if (hscrollInner) hscrollInner.style.width = cx + 'px';
    var _hSyncing = false;
    if (hscroll && bodyEl) {
        hscroll.onscroll = function() {
            if (_hSyncing) return; _hSyncing = true;
            bodyEl.scrollLeft = hscroll.scrollLeft;
            _hSyncing = false;
        };
        bodyEl.onscroll = function() {
            if (_hSyncing) return; _hSyncing = true;
            hscroll.scrollLeft = bodyEl.scrollLeft;
            _hSyncing = false;
        };
    }

    _netRendered = true;
    _networkData = { events: layout.events, activities: tp.activities, relations: tp.relations };
    _networkOpts = opts;
};

// ===== 节点拖拽全局工具（独立于 renderNetwork，避免闭包问题）=====
function _netFindConnected(eventId) {
    var ids = [];
    if (_netActivities) _netActivities.forEach(function(a) { if (a.source === eventId || a.target === eventId) ids.push(a.id); });
    return ids;
}
function _netUpdateArrows(eventId, dx, dy) {
    var ids = _netFindConnected(eventId);
    var svg = _netSvg;
    if (!svg) return;
    ids.forEach(function(actId) {
        var g = svg.querySelector('[data-activity-id="' + actId + '"]');
        if (!g) return;
        var sid = g.getAttribute('data-src'), tid = g.getAttribute('data-tgt');
        var s = _netLayout ? _netLayout.events[sid] : null, t = _netLayout ? _netLayout.events[tid] : null;
        if (!s || !t) return;
        var nr = 17;
        var sx = s.x + (sid === eventId ? dx : (_netEventOffsets[sid] ? _netEventOffsets[sid].x : 0)) + nr;
        var sy = s.y + (sid === eventId ? dy : (_netEventOffsets[sid] ? _netEventOffsets[sid].y : 0));
        var ex = t.x + (tid === eventId ? dx : (_netEventOffsets[tid] ? _netEventOffsets[tid].x : 0)) - nr;
        var ey = t.y + (tid === eventId ? dy : (_netEventOffsets[tid] ? _netEventOffsets[tid].y : 0));
        var pd = Math.abs(ey - sy) < 5
            ? 'M' + sx + ' ' + sy + ' L' + (ex - 2) + ' ' + ey
            : 'M' + sx + ' ' + sy + ' L' + ex + ' ' + sy + ' L' + ex + ' ' + ey;
        var p = g.querySelector('.act-arrow'); if (p) p.setAttribute('d', pd);
        var lx = (sx + ex) / 2;
        var lb = g.querySelector('.act-label'); if (lb) { lb.setAttribute('x', lx); lb.setAttribute('y', sy - nr - 6); }
        var du = g.querySelector('.act-dur'); if (du) { du.setAttribute('x', lx); du.setAttribute('y', sy + nr + 10); }
    });
}
// 更新虚箭线（关系线，无活动箭头）
function _netUpdateDummys(eventId, dx, dy) {
    var svg = _netSvg;
    if (!svg || !_netLayout) return;
    // 查找所有以该事件为源或目标的虚箭线
    var dummies = svg.querySelectorAll('.dummy-rel');
    for (var i = 0; i < dummies.length; i++) {
        var g = dummies[i];
        var sid = g.getAttribute('data-dummy-src'), tid = g.getAttribute('data-dummy-tgt');
        var srcId = (sid === eventId) ? eventId : null;
        var tgtId = (tid === eventId) ? eventId : null;
        if (!srcId && !tgtId) continue;
        var s = _netLayout.events[sid], t = _netLayout.events[tid];
        if (!s || !t) continue;
        var sx = s.x + (srcId ? dx : (_netEventOffsets[sid] ? _netEventOffsets[sid].x : 0));
        var sy = s.y + (srcId ? dy : (_netEventOffsets[sid] ? _netEventOffsets[sid].y : 0));
        var ex = t.x + (tgtId ? dx : (_netEventOffsets[tid] ? _netEventOffsets[tid].x : 0));
        var ey = t.y + (tgtId ? dy : (_netEventOffsets[tid] ? _netEventOffsets[tid].y : 0));
        var mx = sx + (ex - sx) / 2;
        var pd = 'M' + sx + ' ' + sy + ' L' + mx + ' ' + sy + ' L' + mx + ' ' + ey + ' L' + ex + ' ' + ey;
        var p = g.querySelector('path'); if (p) p.setAttribute('d', pd);
    }
}
// 全局拖拽事件（绑定一次，通过全局 _netNodeDrag 通信）
if (!window._netDragSetup) {
    window._netDragSetup = true;
    document.addEventListener('mousemove', function(e) {
        if (!_netNodeDrag) return;
        var rawDx = e.clientX - _netNodeDrag.startX, rawDy = e.clientY - _netNodeDrag.startY;
        var dx = _netNodeDrag.offX + rawDx;
        var LAYER_H = 60;
        var dy = _netNodeDrag.offY + Math.round(rawDy / LAYER_H) * LAYER_H;
        _netNodeDrag.group.setAttribute('transform', 'translate(' + dx + ',' + dy + ')');
        _netUpdateArrows(_netNodeDrag.eventId, dx, dy);
        _netUpdateDummys(_netNodeDrag.eventId, dx, dy);
        // 记录实时天数
        _netNodeDrag.rawDx = rawDx; _netNodeDrag.cx = e.clientX; _netNodeDrag.cy = e.clientY;
    });
    document.addEventListener('mouseup', function() {
        if (!_netNodeDrag) return;
        var g = _netNodeDrag.group, eid = _netNodeDrag.eventId;
        var tf = g.getAttribute('transform') || '';
        var mx = tf.match(/translate\(([-\d.]+),([-\d.]+)\)/);
        var dx = mx ? parseFloat(mx[1]) : 0, dy = mx ? parseFloat(mx[2]) : 0;
        _netEventOffsets[eid] = { x: dx, y: dy };
        // 弹出输入框确认偏移天数
        var deltaDays = Math.round((_netNodeDrag.rawDx || 0) / (_netDayWidth || 40));
        var cx = _netNodeDrag.cx || 0, cy = _netNodeDrag.cy || 0;
        var nodeDrag = _netNodeDrag;
        _netNodeDrag = null;
        g.style.cursor = 'grab';
        _showDayPopup(deltaDays, cx, cy, function(days) {
            if (days !== 0 && _netDotNet) {
                delete _netEventOffsets[nodeDrag.eventId];
                _netDotNet.invokeMethodAsync('SyncNodeDrag', nodeDrag.eventId, days);
            }
        });
    });
}

// ===== 全局项目选中状态（localStorage）=====
window.getActiveProject = function() {
    return parseInt(localStorage.getItem('netplan_activeProject') || '0');
};
window.setActiveProject = function(id) {
    localStorage.setItem('netplan_activeProject', id);
};
window.navToProject = function(page, id) {
    if (id <= 0) { window.location.href = '/'; return; }
    window.location.href = '/project/' + id + '/' + page;
};

// ===== 首页项目勾选（跨标签联动）=====
window.getCheckedProject = function() {
    return parseInt(localStorage.getItem('netplan_checked') || '0');
};
window.setCheckedProject = function(id) {
    localStorage.setItem('netplan_checked', id);
};
// 导航到勾选项目的指定页面，无勾选则回首页
window.navToChecked = function(page) {
    var checked = window.getCheckedProject();
    if (checked > 0) {
        window.location.href = '/project/' + checked + '/' + page;
    } else {
        window.location.href = '/';
    }
};

// 适应视图：缩放 SVG 使之适应可视区域
window.networkFit = function() {
    var body = document.getElementById('network-body');
    var svg = document.getElementById('network-svg');
    if (!body || !svg) return;

    // 重置缩放
    svg.style.transform = '';

    // 计算缩放比例
    var vw = body.clientWidth;
    var sw = parseFloat(svg.getAttribute('width')) || vw;
    var scale = vw > 10 ? Math.min(1, vw / sw) : 1;

    svg.style.transform = 'scale(' + scale + ')';
    body.scrollTop = 0;
    body.scrollLeft = 0;

    // 同步滚动条
    var hscroll = document.getElementById('network-hscroll');
    if (hscroll) hscroll.scrollLeft = 0;
};

// 自动轮询检测token变化（兼容Blazor）
(function startNetworkPoller() {
    setInterval(function() {
        var tokenEl = document.getElementById('netplan-render-token');
        if (!tokenEl) return;
        var token = tokenEl.value;
        if (token === _lastRenderedToken) return;
        if (typeof window.renderNetwork !== 'function') return;
        var dataEl = document.getElementById('netplan-data');
        var optsEl = document.getElementById('netplan-options');
        if (!dataEl || !optsEl) return;
        _lastRenderedToken = token;
        try {
            console.log('[NET] poller: calling renderNetwork...');
            window.renderNetwork(dataEl.value, JSON.parse(optsEl.value));
        } catch(e) { console.error('[NET] renderNetwork error', e); }
    }, 300);
})();

// ========== 甘特图列宽拖拽调整 ==========
(function() {
    var handle = null, startX = 0, startW = 0, targetCol = null;

    function initResize() {
        var header = document.querySelector('.gantt-left-header');
        if (!header) return;
        header.querySelectorAll('div').forEach(function(col) {
            // 仅 col-name 和 col-resource 可拖拽宽度
            var cn = col.className;
            if (cn.indexOf('col-name')===-1 && cn.indexOf('col-resource')===-1) return;
            if (col.querySelector('.col-resize-handle')) return;
            var handle = document.createElement('div');
            handle.className = 'col-resize-handle';
            col.appendChild(handle);
            handle.addEventListener('mousedown', function(e) {
                e.preventDefault(); e.stopPropagation();
                targetCol = col;
                startX = e.clientX;
                startW = col.offsetWidth;
                document.body.style.cursor = 'col-resize';
                document.body.style.userSelect = 'none';
            });
        });
    }

    document.addEventListener('mousemove', function(e) {
        if (!targetCol) return;
        var newW = Math.max(30, startW + (e.clientX - startX));
        if (targetCol.classList.contains('col-name')) newW = Math.max(70, Math.min(250, newW));
        targetCol.style.width = newW + 'px';
        targetCol.style.minWidth = newW + 'px';
        targetCol.style.maxWidth = newW + 'px';
        targetCol.style.flex = 'none';
        // 同步所有 body 列
        var colClass = Array.from(targetCol.classList).find(function(c) { return c.startsWith('col-'); });
        if (colClass) {
            document.querySelectorAll('.gantt-left-body .' + colClass).forEach(function(cell) {
                cell.style.width = newW + 'px';
                cell.style.minWidth = newW + 'px';
                cell.style.maxWidth = newW + 'px';
                cell.style.flex = 'none';
            });
        }
    });

    document.addEventListener('mouseup', function() {
        if (!targetCol) return;
        document.body.style.cursor = '';
        document.body.style.userSelect = '';
        targetCol = null;
    });

    // 延迟初始化（等待 Blazor 渲染完成）
    setTimeout(initResize, 1000);
})();
