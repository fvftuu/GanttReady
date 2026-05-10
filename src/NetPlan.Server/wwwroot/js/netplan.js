// NetPlan.js - 甘特图交互脚本

// 双向滚动同步锁(防止左右互相触发导致无限循环)
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

// ========== 拖拽偏移输入弹出框(甘特 + 网络公用)==========
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

// ========== 甘特图拖拽:防鬼影 + 松手弹出偏移输入 ==========
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

// 草稿保存/加载(localStorage 暂存编辑中的任务)
window.loadDraft = function(key) {
    try { return localStorage.getItem('netplan_draft_' + key); } catch(e) { return null; }
};
window.saveDraft = function(key, json) {
    try { localStorage.setItem('netplan_draft_' + key, json); } catch(e) {}
};
window.clearDraft = function(key) {
    try { localStorage.removeItem('netplan_draft_' + key); } catch(e) {}
};

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
// 时标双代号网络图 - SVG实现(符合JGJ/T 121-2015规范)
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
// 每层额外插入的空行数(数组索引=层索引,值=空行数)
var _netExtraSpacing = {};
// 前锋线全局状态
var _networkCanvas = null;
var _networkData = null;
var _networkOpts = null;
var _netRendered = false;
// 网络图模式: 'time' = 时标网络图, 'logic' = 逻辑双代号图
var _netMode = 'time';
var _progressDate = null;   // 前锋线检查日期(Date)
var _progressCheckDate = null; // 前锋线独立日期(与今日线解耦)
var _progressX = 0;         // 前锋线像素位置
var _dragInfo = null;       // 拖动状态 { startX, startScroll }
var _projStartDate = null;  // 项目开始日期(Date)

// .NET引用设置
window.setNetworkDotNet = function(ref) {
    _netDotNet = ref;
};

// 设置网络图模式
window.setNetworkMode = function(mode) {
    _netMode = mode || 'time';
};

// 清除节点偏移(不重置前锋线日期)
window.clearNetworkOffsets = function() {
    _netEventOffsets = {};
};

// ISO 8601 周次(甘特图/网络图标尺共用)
function getISOWeek(d) {
    var dt = new Date(Date.UTC(d.getFullYear(), d.getMonth(), d.getDate()));
    var dayNum = dt.getUTCDay() || 7;
    dt.setUTCDate(dt.getUTCDate() + 4 - dayNum);
    var yearStart = new Date(Date.UTC(dt.getUTCFullYear(), 0, 1));
    return Math.ceil((((dt - yearStart) / 86400000) + 1) / 7);
}

// ============================================================================
// 第一步:节点计算法时间参数计算
// ============================================================================

function calculateTimeParams(tasks, relations) {
    // 第一步:收集每个任务的 ES/EF,以及每个时间点关联的任务
    var starts = {};  // timeOffset -> unique taskIds
    var ends = {};    // timeOffset -> unique taskIds
    var taskEs = {};  // taskId -> es
    var taskEf = {};  // taskId -> ef

    tasks.forEach(function(task) {
        var es = task.es || 0;
        var ef = task.ef || (es + (task.duration || 0));
        taskEs[task.id] = es;
        taskEf[task.id] = ef;

        if (!starts[es]) starts[es] = [];
        if (starts[es].indexOf(task.id) === -1) starts[es].push(task.id);

        if (!ends[ef]) ends[ef] = [];
        if (ends[ef].indexOf(task.id) === -1) ends[ef].push(task.id);
    });

    // 第二步:合并事件节点,同 ES 共享开始事件,同 EF 共享结束事件
    var events = {};
    var allTimeOffsets = [];
    Object.keys(starts).forEach(function(t) { if (allTimeOffsets.indexOf(t) === -1) allTimeOffsets.push(t); });
    Object.keys(ends).forEach(function(t) { if (allTimeOffsets.indexOf(t) === -1) allTimeOffsets.push(t); });

    // 按数值排序
    allTimeOffsets.sort(function(a, b) { return parseInt(a) - parseInt(b); });

    allTimeOffsets.forEach(function(timeOffset) {
        var eid = 'T' + timeOffset;
        var isStart = starts[timeOffset] !== undefined;
        var isEnd = ends[timeOffset] !== undefined;
        var type = isStart && isEnd ? 'both' : (isStart ? 'start' : 'end');

        // 收集关联的所有任务 ID
        var associated = [];
        [].concat(starts[timeOffset] || []).concat(ends[timeOffset] || []).forEach(function(tid) {
            if (associated.indexOf(tid) === -1) associated.push(tid);
        });

        var taskId = parseInt(associated[0]) || 0;          // 第一个关联任务(用于双击编辑)
        var isCritical = false;
        var tf = 0;
        tasks.forEach(function(t) {
            if (associated.indexOf(t.id) !== -1) {
                if (t.isCritical) isCritical = true;
                if ((t.tf || 0) > tf) tf = t.tf || 0;
            }
        });

        events[eid] = {
            id: eid,
            taskId: taskId,
            type: type,
            num: 0,
            es: parseInt(timeOffset),
            ef: parseInt(timeOffset),
            ls: 0,
            lf: 0,
            tf: tf,
            ff: 0,
            isCritical: isCritical
        };
    });

    // 第三步:构建工作箭线(每个 task 一条)
    var activities = [];
    tasks.forEach(function(task) {
        var es = taskEs[task.id];
        var ef = taskEf[task.id];
        activities.push({
            id: task.id,
            source: 'T' + es,
            target: 'T' + ef,
            es: es,
            ef: ef,
            ls: task.ls || task.lateStart || es,
            lf: task.lf || task.lateFinish || ef,
            duration: task.duration || 0,
            code: task.label || '',
            name: task.name || '',
            isCritical: task.isCritical || false,
            tf: task.tf || 0,
            ff: task.ff || 0,
            completion: task.completion || 0
        });
    });

    // 第四步:建立事件邻接关系
    var eventPred = {};
    var eventSucc = {};
    Object.keys(events).forEach(function(eid) {
        eventPred[eid] = [];
        eventSucc[eid] = [];
    });

    // 辅助:去重添加邻接边
    function addEdge(f, t) {
        if (f === t) return;
        if (!events[f] || !events[t]) return;
        if (eventSucc[f].indexOf(t) === -1) eventSucc[f].push(t);
        if (eventPred[t].indexOf(f) === -1) eventPred[t].push(f);
    }

    // 从工作箭线建立实线连接:T{es} -> T{ef}
    activities.forEach(function(act) {
        addEdge(act.source, act.target);
    });

    // 从关系建立虚箭线连接(V6.1 虚工作逻辑)
    relations.forEach(function(rel) {
        var predEf = taskEf[rel.source];
        var succEs = taskEs[rel.target];
        if (predEf === undefined || succEs === undefined) return;
        // 虚工作:前驱结束节点 → 后继开始节点
        addEdge('T' + predEf, 'T' + succEs);
    });

    // 第五步:拓扑排序并分配事件编号
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

    // 分配事件编号(拓扑序)
    sortedEvents.forEach(function(eid, idx) {
        if (events[eid]) events[eid].num = idx + 1;
    });

    // 补分配:拓扑未覆盖的事件(孤立 / 循环中)按时间偏移排序后分配
    var allEids = Object.keys(events).sort(function(a, b) {
        return (events[a].es || 0) - (events[b].es || 0);
    });
    var nextNum = sortedEvents.length + 1;
    allEids.forEach(function(eid) {
        if (!events[eid].num || events[eid].num === 0) {
            events[eid].num = nextNum++;
            sortedEvents.push(eid);
        }
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
// 第二步:垂直分层布局(关键线路最上层)
// ============================================================================

function calculateVerticalLayout(data) {
    var events = data.events;
    var activities = data.activities;
    var eventPred = data.eventPred;
    var eventSucc = data.eventSucc;

    var LAYER_HEIGHT = 60;
    var MARGIN_TOP = 100;

    // 标记关键线路事件
    var criticalEvents = {};
    activities.forEach(function(act) {
        if (act.isCritical) {
            criticalEvents[act.source] = true;
            criticalEvents[act.target] = true;
        }
    });

    // ===== 改进的分层算法:拓扑BFS(单向推进,不会振荡)=====
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

    // BFS拓扑推进:每往下走一层,层次+1
    while (queue.length > 0) {
        var current = queue.shift();
        var successorIds = eventSucc[current] || [];
        successorIds.forEach(function(next) {
            // 计算后继事件的层次:当前层次 + 1(非关键再加1)
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

    // 边界处理:未分配层次的事件(孤立节点)
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

    // === 事件合并后不再强制同行约束(真双代号图中工作箭线可跨层)===
    // 之前单代号需要同行是因为 S/E 节点属于同一任务
    // 现在节点被多个任务共享,拉平会破坏分层

    // 将事件按层级分组
    var layerEvents = {};
    Object.keys(eventLayer).forEach(function(eid) {
        var layer = eventLayer[eid];
        if (!layerEvents[layer]) layerEvents[layer] = [];
        layerEvents[layer].push(eid);
    });

    // 分配Y坐标:将实际层号映射为连续索引(压缩中间空洞),同一层级y相同
    var sortedLayers = Object.keys(layerEvents).map(Number).sort(function(a,b){return a-b;});
    var layerIndex = {};
    sortedLayers.forEach(function(l, idx) { layerIndex[l] = idx; });
    Object.keys(layerEvents).forEach(function(layer) {
        var y = MARGIN_TOP + layerIndex[layer] * LAYER_HEIGHT;
        layerEvents[layer].forEach(function(eid) {
            events[eid].y = y;
        });
    });

    // 分配X坐标需要在renderNetwork中做(依赖dayWidth)
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
// SVG 绘制函数(替代 Canvas)
// ============================================================================
var _svgArrowId = 0;
function svgArrowMarker(crit, optCritColor, optNormalColor) {
    _svgArrowId++;
    var color = crit ? (optCritColor || '#e63946') : (optNormalColor || '#333');
    var id = 'arr-' + _svgArrowId;
    var m = '<marker id="' + id + '" markerWidth="8" markerHeight="8" refX="7" refY="4" orient="auto">'
          + '<path d="M0,0 L8,4 L0,8 Z" fill="' + color + '"/></marker>';
    return { id: 'url(#' + id + ')', html: m, color: color };
}

function buildNetworkSvg(params) {
    var p = params, parts = [], markers = [];
    var MARGIN_LEFT = 80, RULER_H = 52;
    // A3: 节点半径可配置
    var NODE_R = (p.nodeRadius && p.nodeRadius >= 8 && p.nodeRadius <= 16) ? p.nodeRadius : 11;
    // A3: 节点形状
    var nodeShape = p.nodeShape || 'circle';
    var cw = p.canvasW, ch = p.canvasH;
    // A5: 线颜色及线宽设置
    var criticalColor = p.criticalColor || '#e63946';
    var normalColor = p.normalColor || '#333';
    var dummyColor = p.dummyColor || '#aaa';
    var criticalWidth = p.criticalWidth || 3;
    var normalWidth = p.normalWidth || 1.5;
    var mode = p.mode || 'time';
    var restDayPattern = p.restDayPattern !== false; // A3: show rest day highlight
    var singleStartEnd = p.singleStartEnd === true; // A3: force single start/end (placeholder)
    // Bug 1: 1080p adaptation — smaller fonts when viewport < 1400px
    var isNarrowViewport = (window.innerWidth || document.documentElement.clientWidth || 1920) < 1400;
    console.log('[SVG] opts:', 'todayLine=', p.showTodayLine, 'progressLine=', p.showProgressLine,
        'dayWidth=', p.dayWidth, 'critical=', p.showCritical, 'float=', p.showFloat, 'mode=', mode,
        'restDayPattern=', restDayPattern, 'singleStartEnd=', singleStartEnd);

    var sd = new Date(p.projectStartDate);
    var dw = p.dayWidth, td = p.totalDays;
    var upperH = 28, lowerY = 28, lowerH = 24;

    // 逻辑模式:不画时间标尺
    var isTimeMode = (mode !== 'logic');

    if (isTimeMode) {
    // === 时间标尺(精确对齐甘特图:上层28 + 下层24 = 52px)===
    // 背景
    parts.push('<rect x="0" y="0" width="' + cw + '" height="' + upperH + '" fill="url(#rulerGrad)"/>');
    parts.push('<defs><linearGradient id="rulerGrad" x1="0" y1="0" x2="0" y2="1">'
        + '<stop offset="0%" stop-color="#f5f5f5"/><stop offset="100%" stop-color="#fafafa"/></linearGradient></defs>');
    parts.push('<rect x="0" y="' + lowerY + '" width="' + cw + '" height="' + lowerH + '" fill="#f5f5f5"/>');

    // 上层:年/月标签(font12 bold #333)
    if (dw > 20) {
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

    // 下层:日/周/月
    if (dw > 20) {
        for (var d = 0; d <= td; d++) {
            var dt = new Date(sd); dt.setDate(dt.getDate() + d);
            var x = MARGIN_LEFT + d * dw;
            var dow = dt.getDay();
            var isWeekend = (dow === 0 || dow === 6);
            if (isWeekend && restDayPattern) {
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
    } // isTimeMode

    // === 行背景：间隔色区分不同层级（必须在虚箭线和工作箭线之前）===
    var rowH = p.layerHeight || 60;
    var bgLayerYs = {};
    Object.keys(p.layout.events).forEach(function(eid) {
        var ey = p.layout.events[eid].y;
        if (!bgLayerYs[ey]) bgLayerYs[ey] = true;
    });
    var sortedBgYs = Object.keys(bgLayerYs).map(Number).sort(function(a,b){return a-b;});
    sortedBgYs.forEach(function(ey, idx) {
        if (idx % 2 === 1) {
            var bgY = ey - rowH / 2;
            parts.push('<rect x="0" y="' + bgY + '" width="' + cw + '" height="' + rowH + '" fill="#f0f4f8" opacity="0.4"/>');
        }
    });

    // 竖线网格
    // 计算最底层事件的 Y 坐标(用于网格线延伸和底部标尺定位)
    var maxY = 0;
    Object.keys(p.layout.events).forEach(function(eid) {
        var ey = p.layout.events[eid].y;
        if (ey > maxY) maxY = ey;
    });
    var bottomRulerY = maxY + 60;
    var svgFullH = isTimeMode ? (bottomRulerY + RULER_H + 20) : (maxY + 80);
    var fullH = svgFullH;

    if (isTimeMode) {
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
    }
    // E5: 行标注(左侧MARGIN_LEFT区域内)
    var rowLabels = p.rowLabels || [];
    var drawnY = {};
    Object.keys(p.layout.events).forEach(function(eid) {
        var ey = p.layout.events[eid].y;
        if (!drawnY[ey]) {
            drawnY[ey] = true;
            parts.push('<line x1="0" y1="' + ey + '" x2="' + cw + '" y2="' + ey + '" stroke="#e8e8e8" stroke-width="0.5" stroke-dasharray="4,2"/>');
        }
    });
    // E5: 行标注 - 支持对象格式 [{layerIndex:0, text:"基础工程"}, ...]
    if (rowLabels.length > 0) {
        var rowLabelMap = {};
        rowLabels.forEach(function(rl) {
            if (typeof rl === 'object' && rl.layerIndex !== undefined) {
                rowLabelMap[rl.layerIndex] = rl.text || '';
            } else if (typeof rl === 'string') {
                // fallback for plain string array
                rowLabelMap[Object.keys(rowLabelMap).length] = rl;
            }
        });
        // Build sortedY list to map layerIndex -> Y coordinate
        var sortedYList = [];
        Object.keys(p.layout.events).forEach(function(eid) {
            var ey = p.layout.events[eid].y;
            if (sortedYList.indexOf(ey) === -1) sortedYList.push(ey);
        });
        sortedYList.sort(function(a,b){return a-b;});
        sortedYList.forEach(function(ey, idx) {
            var labelText = rowLabelMap[idx] || '';
            if (labelText) {
                parts.push('<text x="' + (MARGIN_LEFT - 4) + '" y="' + (ey + 4) + '" font-size="10" fill="#666" text-anchor="end">' + labelText + '</text>');
            }
        });
    }

    // === 虚箭线(V6.1:从前驱EF节点 → 后继ES节点) ===
    // Bug 6: 补齐隐式虚箭线（事件邻接但无实际工作的边）
    var showDummy = p.showDummyArrows !== false;
    if (showDummy) {
        // Build set of real activity edges (source→target)
        var realEdges = {};
        p.timeParams.activities.forEach(function(a) {
            realEdges[a.source + '->' + a.target] = true;
        });
        var drawnDummies = {};
        var dummyColorUsed = dummyColor;

        // 预建 taskId→es/ef 映射
        var taskEsMap = {}, taskEfMap = {};
        p.timeParams.activities.forEach(function(a) {
            taskEsMap[a.id] = parseInt(a.source.replace('T',''));
            taskEfMap[a.id] = parseInt(a.target.replace('T',''));
        });
        p.timeParams.relations.forEach(function(rel) {
            var predEf = taskEfMap[rel.source];
            var succEs = taskEsMap[rel.target];
            if (predEf === undefined || succEs === undefined) return;
            var srcId = 'T' + predEf, tgtId = 'T' + succEs;
            var key = srcId + '->' + tgtId;
            if (srcId === tgtId || realEdges[key] || drawnDummies[key]) return;
            drawnDummies[key] = true;
            var src = p.layout.events[srcId];
            var tgt = p.layout.events[tgtId];
            if (!src || !tgt) return;
            var mx = src.x + (tgt.x - src.x) / 2;
            var d = 'M' + src.x + ' ' + src.y + ' L' + mx + ' ' + src.y
                  + ' L' + mx + ' ' + tgt.y + ' L' + tgt.x + ' ' + tgt.y;
            parts.push('<g class="dummy-rel" data-dummy-src="' + srcId + '" data-dummy-tgt="' + tgtId + '">');
            parts.push('<path d="' + d + '" fill="none" stroke="' + dummyColorUsed + '" stroke-width="1" stroke-dasharray="5,3"/>');
            parts.push('</g>');
        });

        // Implicit dummies: event adjacency not covered by activities or explicit relations
        var evtSucc = p.timeParams.eventSucc || {};
        Object.keys(evtSucc).forEach(function(srcId) {
            (evtSucc[srcId] || []).forEach(function(tgtId) {
                var key = srcId + '->' + tgtId;
                if (realEdges[key] || drawnDummies[key]) return;
                drawnDummies[key] = true;
                var src = p.layout.events[srcId], tgt = p.layout.events[tgtId];
                if (!src || !tgt || srcId === tgtId) return;
                // Draw implicit dummy (orthogonal L-shape)
                var mx = src.x + (tgt.x - src.x) / 2;
                var d = 'M' + src.x + ' ' + src.y + ' L' + mx + ' ' + src.y + ' L' + mx + ' ' + tgt.y + ' L' + tgt.x + ' ' + tgt.y;
                parts.push('<g class="dummy-rel" data-dummy-src="' + srcId + '" data-dummy-tgt="' + tgtId + '">');
                parts.push('<path d="' + d + '" fill="none" stroke="' + dummyColorUsed + '" stroke-width="1" stroke-dasharray="5,3"/>');
                parts.push('</g>');
            });
        });
    } // showDummy

    // === 工作箭线 ===
    p.timeParams.activities.forEach(function(act) {
        var src = p.layout.events[act.source], tgt = p.layout.events[act.target];
        if (!src || !tgt) return;
        var isCrit = act.isCritical && p.showCritical;
        var sx = src.x + NODE_R, sy = src.y, ex = tgt.x - NODE_R, ey = tgt.y;
        var dur = act.duration || 0;

        var arrow = svgArrowMarker(isCrit, criticalColor, normalColor);
        // 箭头已取消,不推入 markers

        var pathD;
        if (Math.abs(ey - sy) < 5) {
            pathD = 'M' + sx + ' ' + sy + ' L' + (ex - 2) + ' ' + ey;
        } else {
            pathD = 'M' + sx + ' ' + sy + ' L' + ex + ' ' + sy + ' L' + ex + ' ' + ey;
        }
        parts.push('<g data-activity-id="' + act.id + '" data-src="' + act.source + '" data-tgt="' + act.target + '">');
        parts.push('<path class="act-arrow" d="' + pathD + '" fill="none" stroke="' + arrow.color
            + '" stroke-width="' + (isCrit ? criticalWidth : normalWidth) + '"/>');

        var lx = (sx + ex) / 2, ly = sy - NODE_R - 6;
        var labelFields = p.labelFields || [];
        var fieldSet = {};
        labelFields.forEach(function(f) { fieldSet[f] = true; });
        var aes = act.es || parseInt(act.source.replace('T','') || '0');
        var aef = act.ef || parseInt(act.target.replace('T','') || '0');

        // 中部: code + name + TF
        var labelMain = (act.code || '') + (act.name ? ' ' + act.name : '');
        if (fieldSet['tf']) labelMain += ' TF=' + (act.tf || 0);
        if (labelMain.length > 30) labelMain = labelMain.substring(0, 28) + '...';
        parts.push('<text class="act-label" x="' + lx + '" y="' + ly + '" font-size="10" fill="' + arrow.color
            + '" text-anchor="middle" font-weight="' + (isCrit ? 'bold' : 'normal') + '">' + labelMain + '</text>');

        // 四角字段
        if (fieldSet['es']) parts.push('<text class="act-es" x="' + sx + '" y="' + (sy - NODE_R - 6) + '" font-size="8" fill="#999" text-anchor="start">ES=' + aes + '</text>');
        if (fieldSet['ef']) parts.push('<text class="act-ef" x="' + ex + '" y="' + (ey - NODE_R - 6) + '" font-size="8" fill="#999" text-anchor="end">EF=' + aef + '</text>');
        if (fieldSet['ls']) parts.push('<text class="act-ls" x="' + sx + '" y="' + (sy + NODE_R + 10) + '" font-size="8" fill="#999" text-anchor="start">LS=' + (act.ls || aes) + '</text>');
        if (fieldSet['lf']) parts.push('<text class="act-lf" x="' + ex + '" y="' + (ey + NODE_R + 10) + '" font-size="8" fill="#999" text-anchor="end">LF=' + (act.lf || aef) + '</text>');

        // 工期始终显示在箭线下方
        var durText = dur + 'd';
        if (fieldSet['ff']) durText += ' FF=' + (act.ff || 0);
        parts.push('<text class="act-dur" x="' + lx + '" y="' + (sy + NODE_R + 10) + '" font-size="9" fill="#666" text-anchor="middle">'
            + durText + '</text>');

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
        var fill = isCrit ? criticalColor : '#fff', stroke = isCrit ? '#b30000' : criticalColor;
        var tc = isCrit ? '#fff' : '#000';
        parts.push('<g class="net-event" data-task-id="' + evt.taskId + '" data-event-id="' + eid + '" style="cursor:grab;">');
        if (nodeShape === 'ellipse' || p.nodeEllipse) {
            var rx = nodeShape === 'circle' ? (NODE_R + 4) : (NODE_R * 1.3);
            var ry = nodeShape === 'circle' ? (NODE_R - 2) : NODE_R;
            parts.push('<ellipse cx="' + evt.x + '" cy="' + evt.y + '" rx="' + rx + '" ry="' + ry
                + '" fill="' + fill + '" stroke="' + stroke + '" stroke-width="2"/>');
        } else {
            parts.push('<circle cx="' + evt.x + '" cy="' + evt.y + '" r="' + NODE_R
                + '" fill="' + fill + '" stroke="' + stroke + '" stroke-width="2"/>');
        }
        parts.push('<text x="' + evt.x + '" y="' + (evt.y + 1)
            + '" dominant-baseline="middle" text-anchor="middle" font-size="12"'
            + ' font-weight="bold" fill="' + tc + '" style="pointer-events:none;">' + evt.num + '</text>');
        parts.push('</g>');
    });

    // === 今日线(红色虚线,从标尺顶部到底部标尺上方)===
    if (isTimeMode && p.showTodayLine) {
        var today = new Date();
        var todayOffset = Math.floor((today.getTime() - sd.getTime()) / 86400000);
        if (todayOffset >= 0 && todayOffset < td) {
            var tx = MARGIN_LEFT + todayOffset * dw + dw / 2;
            parts.push('<line x1="' + tx + '" y1="0" x2="' + tx + '" y2="' + bottomRulerY + '" stroke="#e74c3c" stroke-width="2" stroke-dasharray="6,3"/>');
            parts.push('<text x="' + (tx + 4) + '" y="' + (upperH / 2 + 4) + '" font-size="10" fill="#e74c3c" font-weight="bold">今日</text>');
        }
    }

    // === 可拖动前锋线(JGJ/T121-2015)蓝色#1890ff,与今日线解耦 ===
    if (isTimeMode && p.showProgressLine) {
        if (!_progressCheckDate) { _progressCheckDate = new Date(); _progressCheckDate.setHours(0,0,0,0); }
        var progressOffset = Math.floor((_progressCheckDate.getTime() - sd.getTime()) / 86400000);
        if (progressOffset < 0) progressOffset = 0;
        if (progressOffset >= td) progressOffset = td - 1;
        var pxLine = MARGIN_LEFT + progressOffset * dw;
        _progressDate = new Date(sd); _progressDate.setDate(_progressDate.getDate() + Math.round(progressOffset));
        _progressX = pxLine;
        _projStartDate = sd;
        var checkLabel = _progressCheckDate.getFullYear() + '-' + String(_progressCheckDate.getMonth()+1).padStart(2,'0') + '-' + String(_progressCheckDate.getDate()).padStart(2,'0');
        parts.push('<g id="net-progress-line" style="cursor:ew-resize;">'
            + '<line id="net-pl-line" x1="' + pxLine + '" y1="0" x2="' + pxLine + '" y2="' + bottomRulerY + '" stroke="#1890ff" stroke-width="2"/>'
            + '<polygon id="net-pl-handle" points="' + (pxLine-7) + ',0 ' + (pxLine+7) + ',0 ' + pxLine + ',14" fill="#e74c3c"/>'
            + '<text id="net-pl-label" x="' + (pxLine + 4) + '" y="12" font-size="10" fill="#e74c3c" font-weight="bold">' + checkLabel + '</text>'
            + '</g>');
    }

    // === 图例 ===
    var lx = cw - 220, ly = ch - 75;
    parts.push('<rect x="' + lx + '" y="' + ly + '" width="200" height="65" fill="rgba(255,255,255,0.95)" stroke="#ccc"/>');
    parts.push('<text x="' + (lx + 10) + '" y="' + (ly + 15) + '" font-size="11" font-weight="bold" fill="#333">图例</text>');
    [
        { label: '关键线路', color: criticalColor, w: parseInt(criticalWidth), d: false },
        { label: '非关键工作', color: normalColor, w: parseFloat(normalWidth), d: false },
        { label: '虚工作', color: dummyColor, w: 1, d: true }
    ].forEach(function(it, i) {
        var iy = ly + 30 + i * 13;
        parts.push('<line x1="' + (lx + 10) + '" y1="' + iy + '" x2="' + (lx + 50) + '" y2="' + iy
            + '" stroke="' + it.color + '" stroke-width="' + it.w + '"'
            + (it.d ? ' stroke-dasharray="4,3"' : '') + '/>');
        parts.push('<text x="' + (lx + 56) + '" y="' + (iy + 4) + '" font-size="9" fill="#333">' + it.label + '</text>');
    });

    // E1: 进度曲线(基于CompletionPercentage的折线图+Y轴0-40px)
    if (p.showProgressCurve && isTimeMode) {
        var acts = p.timeParams.activities || [];
        if (acts.length > 0) {
            var curvePoints = [];
            // Bug 5: 将进度曲线移至标尺下方（56-88），不覆盖顶部时间标尺（0-52）
            var curveYMin = 88; // bottom of curve area (below ruler, above events)
            var curveYMax = 56; // top of curve area (just below ruler bottom)
            var curveYRange = curveYMin - curveYMax;
            for (var d = 0; d < td; d++) {
                var dayComp = 0;
                var dayCount = 0;
                acts.forEach(function(a) {
                    var aes = a.es || parseInt(a.source.replace('T','') || '0');
                    var aef = a.ef || parseInt(a.target.replace('T','') || '0');
                    if (d >= aes && d < aef) {
                        dayComp += (a.completion || 0);
                        dayCount++;
                    }
                });
                var avgPct = dayCount > 0 ? dayComp / dayCount : 0;
                var px = MARGIN_LEFT + d * dw + dw / 2;
                var py = curveYMax + curveYRange * (1 - avgPct / 100);
                curvePoints.push(px + ',' + py);
            }
            if (curvePoints.length > 1) {
                // 构建填充区域路径: 从底部沿折线到最后一个点,再折回底部
                var fillPath = 'M' + (MARGIN_LEFT + 0 * dw) + ',' + curveYMin + ' L';
                fillPath += curvePoints.join(' L');
                fillPath += ' L' + (MARGIN_LEFT + (td - 1) * dw + dw / 2) + ',' + curveYMin + ' Z';
                parts.push('<path d="' + fillPath + '" fill="rgba(173,216,230,0.3)" stroke="none"/>');
                parts.push('<polyline fill="none" stroke="#5dade2" stroke-width="2" points="' + curvePoints.join(' ') + '"/>');
            }
        }
    }

    // === 标题 ===
    parts.push('<text x="10" y="80" font-size="13" font-weight="bold" fill="#333">' + (p.projectName || '') + '</text>');
    var subtitle = isTimeMode ? '时标网络计划图' : '逻辑双代号网络图';
    parts.push('<text x="10" y="97" font-size="13" font-weight="bold" fill="#333">' + subtitle + '</text>');

    // === 总工期(对齐甘特图:首末任务日期跨度)===
    var totalDur = p.totalDuration || 0;
    parts.push('<text x="' + (cw - 12) + '" y="80" font-size="12" font-weight="bold" fill="#e63946" text-anchor="end">'
        + '总工期=' + totalDur + '天</text>');


    // === 底部镜像时间标尺(最下层任务下方 1 层间隔,bottomRulerY 已在顶部计算)===
    if (isTimeMode) {
    parts.push('<rect x="0" y="' + bottomRulerY + '" width="' + cw + '" height="' + upperH + '" fill="url(#rulerGrad)"/>');
    parts.push('<rect x="0" y="' + (bottomRulerY + 28) + '" width="' + cw + '" height="' + lowerH + '" fill="#f5f5f5"/>');
    // 上层: 年/月
    if (dw > 20) {
        for (var d2 = 0; d2 < td; d2++) {
            var dt2 = new Date(sd); dt2.setDate(dt2.getDate() + d2);
            if (dt2.getDate() === 1) {
                var xb = MARGIN_LEFT + d2 * dw;
                var ms = Math.min(new Date(dt2.getFullYear(), dt2.getMonth()+1, 0).getDate() - dt2.getDate() + 1, td - d2);
                var mwb = ms * dw;
                var rulerInnerFs = isNarrowViewport ? '10' : '12';
                parts.push('<text x="' + (xb + mwb/2) + '" y="' + (bottomRulerY + 18) + '" font-size="' + rulerInnerFs + '" font-weight="600" fill="#333" text-anchor="middle">'
                    + dt2.getFullYear() + '/' + String(dt2.getMonth()+1).padStart(2,'0') + '</text>');
            }
        }
    } else {
        var lastY2 = -1, yStartX2 = 0;
        for (var d2 = 0; d2 <= td; d2++) {
            var dt2 = new Date(sd); dt2.setDate(dt2.getDate() + d2);
            var cy2 = dt2.getFullYear();
            if (cy2 !== lastY2) {
                if (lastY2 >= 0) {
                    var ywb = MARGIN_LEFT + d2 * dw - yStartX2;
                    parts.push('<text x="' + (yStartX2 + ywb/2) + '" y="' + (bottomRulerY + 18) + '" font-size="12" font-weight="600" fill="#333" text-anchor="middle">'
                        + lastY2 + '</text>');
                }
                lastY2 = cy2; yStartX2 = MARGIN_LEFT + d2 * dw;
            }
        }
        if (lastY2 >= 0) {
                var lw = MARGIN_LEFT + td * dw - yStartX2;
                parts.push('<text x="' + (yStartX2 + lw/2) + '" y="' + (bottomRulerY + 18) + '" font-size="12" font-weight="600" fill="#333" text-anchor="middle">'
                        + lastY2 + '</text>');
        }
    }
    // 下层: 日/周/月
    for (var d2 = 0; d2 < td; d2++) {
        var dt2 = new Date(sd); dt2.setDate(dt2.getDate() + d2);
        if (dw > 20) {
            var xb2 = MARGIN_LEFT + d2 * dw + dw / 2;
            parts.push('<text x="' + xb2 + '" y="' + (bottomRulerY + 46) + '" font-size="10" fill="#666" text-anchor="middle">' + String(dt2.getDate()).padStart(2,'0') + '</text>');
            parts.push('<line x1="' + (MARGIN_LEFT + d2 * dw) + '" y1="' + (bottomRulerY + 28) + '" x2="' + (MARGIN_LEFT + d2 * dw) + '" y2="' + (bottomRulerY + 52) + '" stroke="#ddd" stroke-width="0.5"/>');
        } else if (dw > 10) {
            if (d2 % 7 === 0) {
                var wk = getISOWeek(dt2);
                var wkSpan = Math.min(7, td - d2) * dw;
                var xb2 = MARGIN_LEFT + d2 * dw + wkSpan / 2;
                parts.push('<text x="' + xb2 + '" y="' + (bottomRulerY + 46) + '" font-size="10" fill="#666" text-anchor="middle">第' + wk + '周</text>');
                parts.push('<line x1="' + (MARGIN_LEFT + d2 * dw) + '" y1="' + (bottomRulerY + 28) + '" x2="' + (MARGIN_LEFT + d2 * dw) + '" y2="' + (bottomRulerY + 52) + '" stroke="#ddd" stroke-width="0.5"/>');
            }
        } else {
            if (dt2.getDate() === 1) {
                var mSpan = Math.min(new Date(dt2.getFullYear(), dt2.getMonth()+1, 0).getDate(), td - d2) * dw;
                var xb2 = MARGIN_LEFT + d2 * dw + mSpan / 2;
                parts.push('<text x="' + xb2 + '" y="' + (bottomRulerY + 46) + '" font-size="10" fill="#666" text-anchor="middle">' + String(dt2.getMonth()+1).padStart(2,'0') + '</text>');
                parts.push('<line x1="' + (MARGIN_LEFT + d2 * dw) + '" y1="' + (bottomRulerY + 28) + '" x2="' + (MARGIN_LEFT + d2 * dw) + '" y2="' + (bottomRulerY + 52) + '" stroke="#ddd" stroke-width="0.5"/>');
            }
        }
    }
    } // isTimeMode bottom ruler

    // === 规程标注 ===
    var svgStr = '<svg xmlns="http://www.w3.org/2000/svg" id="network-svg" width="' + cw + '" height="' + svgFullH + '" style="display:block;font-family:SimSun,sans-serif;">';
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

    // 遍历所有事件节点,通过 _netLayout 获取该事件的 ES
    var eventGroups = svg.querySelectorAll('.net-event');
    eventGroups.forEach(function(g) {
        var eid = g.getAttribute('data-event-id');
        var evt = _netLayout && _netLayout.events ? _netLayout.events[eid] : null;
        if (!evt) return;

        // 合并节点可能有多个关联任务,取第一个的 duration
        var es = evt.es || 0;
        var dur = 1;
        var act = null;
        if (_netActivities) {
            for (var i = 0; i < _netActivities.length; i++) {
                if (_netActivities[i].source === eid || _netActivities[i].source === ('T' + es)) {
                    dur = _netActivities[i].duration || 1;
                    act = _netActivities[i];
                }
            }
        }

        // JGJ/T121-2015: 计划完成百分比 = (检查日 - ES) / 工期 × 100
        var plannedPct = Math.max(0, Math.min(100, (checkDays - es) / dur * 100));
        var actualPct = act ? (act.completion || 0) : 0;
        var delta = actualPct - plannedPct;

        var statusColor;
        if (delta >= 0) statusColor = '#27ae60';        // 绿色:正常/超前
        else if (delta >= -20) statusColor = '#f39c12'; // 黄色:轻微落后
        else statusColor = '#e74c3c';                    // 红色:严重落后

        // 关键路径节点保持红色(不覆盖)
        var isCrit = evt.isCritical && _networkOpts && _networkOpts.showCritical !== false;
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

// 初始化前锋线颜色(首次渲染后调用)
window.initProgressColors = function() {
    updateProgressColors();
};
window.renderNetwork = function(elementsJson, opts) {
    opts = opts || {};
    // 读取模式: 优先从 opts.mode, 其次全局 _netMode, 默认 'time'
    var mode = opts.mode || _netMode || 'time';
    _netMode = mode;
    var showCritical = opts.showCritical !== false;
    var showFloat = opts.showFloat !== false;
    var showTodayLine = opts.showTodayLine !== false && mode === 'time';
    var showProgressLine = opts.showProgressLine !== false && mode === 'time';
    var psd = opts.projectStartDate || new Date().toISOString().slice(0, 10);
    var totalDays = opts.totalDays || 90;
    // Bug 1: 1080p adaptation — scale dayWidth down on narrower viewports
    var vw = window.innerWidth || document.documentElement.clientWidth || 1920;
    var dayWidth = (mode === 'logic') ? 80 : (opts.dayWidth || 8);
    if (mode !== 'logic' && vw < 1400 && dayWidth > 12) {
        dayWidth = Math.max(8, dayWidth * 0.7);
    }
    var pn = opts.projectName || '网络计划';
    console.log('[NET] SVG render. opts:', JSON.stringify({totalDays, dayWidth, showTodayLine, showProgressLine, mode}));
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
    if (mode === 'logic') {
        // 逻辑双代号图:节点按拓扑顺序从左到右均匀排列,不按时间偏移
        var sortedEids = tp.sortedEvents;
        var logicGap = dayWidth * 1.8; // 节点间距
        sortedEids.forEach(function(eid, idx) {
            layout.events[eid].x = ML + idx * logicGap + logicGap / 2;
        });
        // 活动长度固定
        var cx = ML + sortedEids.length * logicGap + 200;
    } else {
        Object.keys(layout.events).forEach(function(eid) {
            layout.events[eid].x = ML + (layout.events[eid].es || 0) * dayWidth;
        });
        var cx = ML + totalDays * dayWidth + 100;
    }

    // A3: 应用配置参数(必须在计算高度前初始化)
    var netLayerHeight = opts.layerHeight || 60;
    var netNodeRadius = opts.nodeRadius || 11;
    var netNodeShape = opts.nodeShape || 'circle';

    // 自适应高度:基于最大层级 + 底部标尺 + 余量
    var layerKeys = Object.keys(layout.layerEvents || {}).map(Number);
    var maxLayerNum = layerKeys.length > 0 ? Math.max.apply(null, layerKeys) : 1;
    // cySize = 节点区高度(含底部留白),SVG总高 = cySize + 底部标尺
    var cySize = Math.max(100 + maxLayerNum * netLayerHeight + 60, 400);
    console.log('[NET] SVG size:', cx, 'x', cySize);
    // 层级高度缩放
    var layerScale = netLayerHeight / 60;
    if (Math.abs(layerScale - 1) > 0.01) {
        Object.keys(layout.events).forEach(function(eid) {
            layout.events[eid].y = 100 + (layout.events[eid].y - 100) * layerScale;
        });
    }

    // A2: 应用存储的额外空行偏移到布局(使用 netLayerHeight 替换硬编码 60)
    Object.keys(_netExtraSpacing).forEach(function(key) {
        var extraCount = _netExtraSpacing[key];
        if (!extraCount || extraCount <= 0) return;
        var layerNum = parseInt(key);
        Object.keys(layout.events).forEach(function(eid) {
            var evt = layout.events[eid];
            var layerIdx = Math.round((evt.y - 100) / 60);
            if (layerIdx >= layerNum) {
                evt.y += extraCount * netLayerHeight;
            }
        });
    });

    ce.innerHTML = buildNetworkSvg({
        projectStartDate: psd, totalDays: totalDays, totalDuration: opts.totalDuration || 0, dayWidth: dayWidth,
        timeParams: tp, layout: layout, mode: mode,
        showCritical: showCritical, showFloat: showFloat,
        showTodayLine: showTodayLine, showProgressLine: showProgressLine, showDummyArrows: opts.showDummyArrows,
        showProgressCurve: opts.showProgressCurve === true,
        projectName: pn,
        nodeRadius: netNodeRadius, nodeShape: netNodeShape, nodeEllipse: opts.nodeShape === 'ellipse' ? true : (opts.nodeEllipse === true),
        layerHeight: netLayerHeight,
        labelFields: opts.labelFields || [],
        rowLabels: opts.rowLabels || [],
        restDayPattern: opts.restDayPattern !== false,
        singleStartEnd: opts.singleStartEnd === true,
        canvasW: cx, canvasH: cySize
    });
    console.log('[NET] SVG rendered.');

    var svg = ce.querySelector('svg');
    // 双击事件:每次重渲染都重绑定(画布拖拽同理)
    if (svg) {
        svg.ondblclick = function(e) {
            var dotNet = _netDotNet || window._netDotNet;
            if (!dotNet) { console.warn('[NET] dblclick: _netDotNet not set'); return; }
            var el = e.target.closest('.net-event');
            if (el) {
                var tid = parseInt(el.getAttribute('data-task-id'));
                if (tid) { console.log('[NET] dblclick node taskId=', tid); dotNet.invokeMethodAsync('OpenTaskEditor', tid); }
                return;
            }
            var activityEl = e.target.closest('[data-activity-id]');
            if (activityEl) {
                var aid = parseInt(activityEl.getAttribute('data-activity-id'));
                if (!isNaN(aid) && aid > 0) { console.log('[NET] dblclick activity taskId=', aid); dotNet.invokeMethodAsync('OpenTaskEditor', aid); }
                return;
            }
            // C1: 双击空白区,从X坐标推算dayOffset
            var rect = svg.getBoundingClientRect();
            var clickX = e.clientX - rect.left;
            var dayOffset = Math.max(0, Math.round((clickX - 80) / dayWidth));
            console.log('[NET] dblclick blank area, dayOffset=', dayOffset);
            dotNet.invokeMethodAsync('ShowAddTaskModal', 0, dayOffset);
        };
    }

    // ===== 节点拖拽微调 =====
    // 更新全局引用(每次重渲染刷新)
    _netLayout = layout;
    _netActivities = tp.activities;
    _netSvg = svg;
    _netDayWidth = dayWidth;
    // 恢复已保存的节点偏移(重渲染后保持位置)
    Object.keys(_netEventOffsets).forEach(function(eid) {
        var off = _netEventOffsets[eid];
        var g = svg && svg.querySelector('[data-event-id="' + eid + '"]');
        if (g && (off.x !== 0 || off.y !== 0)) {
            g.setAttribute('transform', 'translate(' + off.x + ',' + off.y + ')');
        }
    });
    // 重渲染后恢复箭头路径(基于保存偏移)
    Object.keys(_netEventOffsets).forEach(function(eid) {
        var off = _netEventOffsets[eid];
        if (off.x !== 0 || off.y !== 0) {
            _netUpdateArrows(eid, off.x, off.y);
            _netUpdateDummys(eid, off.x, off.y);
        }
    });

    if (svg) {
        svg.addEventListener('mousedown', function(e) {
            if (e.target.tagName !== 'circle') return;
            var g = e.target.closest('.net-event');
            if (!g || !_netLayout || !_netLayout.events) return;
            var eid = g.getAttribute('data-event-id');
            var off = _netEventOffsets[eid] || { x: 0, y: 0 };
            _netNodeDrag = { eventId: eid, group: g, startX: e.clientX, startY: e.clientY, offX: off.x, offY: off.y, moved: false };
            g.style.cursor = 'grabbing';
            e.stopPropagation(); // stop canvas panning
            // 不 preventDefault,让 dblclick 能触发
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

    // 全局拖动事件(只绑定一次)
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
                _progressCheckDate = new Date(_projStartDate);
                _progressCheckDate.setDate(_progressCheckDate.getDate() + dayOffset);
                _progressDate = new Date(_progressCheckDate);
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

    // ===== 画布拖拽平移(每次重渲染重绑定)=====
    var netBody = document.getElementById('network-body');
    if (netBody) {
        if (!window._panBound) {
            window._panBound = true;
            var panState = null;
            netBody.addEventListener('mousedown', function(e) {
                if (e.target.closest('.net-event') || e.target.closest('[data-activity-id]')) return;
                if (e.target.closest('#net-progress-line')) return;
                panState = { x: e.clientX, y: e.clientY, sx: netBody.scrollLeft, sy: netBody.scrollTop };
                netBody.style.cursor = 'grabbing';
            });
            window.addEventListener('mousemove', function(e) {
                if (!panState) return;
                netBody.scrollLeft = panState.sx - (e.clientX - panState.x);
                netBody.scrollTop = panState.sy - (e.clientY - panState.y);
            });
            window.addEventListener('mouseup', function() {
                if (!panState) return;
                netBody.style.cursor = '';
                panState = null;
            });
        }
    }
};

// ===== 插入/删除空行 (A2) =====
window._netInsertBlankRow = function(layerNum) {
    // layerNum: 从0开始的层索引,在该层上方插入一个空行
    var key = String(layerNum);
    if (_netExtraSpacing[key] === undefined) _netExtraSpacing[key] = 0;
    _netExtraSpacing[key] += 1;
    // A2: 动态获取层高
    var lh = (_networkOpts && _networkOpts.layerHeight) ? _networkOpts.layerHeight : 60;
    if (_netLayout && _netLayout.events) {
        Object.keys(_netLayout.events).forEach(function(eid) {
            var evt = _netLayout.events[eid];
            var layerIdx = Math.round((evt.y - 100) / 60);
            if (layerIdx >= layerNum) {
                evt.y += lh;
            }
        });
    }
    _triggerRerender();
};

window._netDeleteBlankRow = function(layerNum) {
    var key = String(layerNum);
    if (!_netExtraSpacing[key] || _netExtraSpacing[key] <= 0) return;
    _netExtraSpacing[key] -= 1;
    var lh = (_networkOpts && _networkOpts.layerHeight) ? _networkOpts.layerHeight : 60;
    if (_netLayout && _netLayout.events) {
        Object.keys(_netLayout.events).forEach(function(eid) {
            var evt = _netLayout.events[eid];
            var layerIdx = Math.round((evt.y - 100) / 60);
            if (layerIdx >= layerNum) {
                evt.y -= lh;
            }
        });
    }
    _triggerRerender();
};

function _triggerRerender() {
    var tokenEl = document.getElementById('netplan-render-token');
    var dataEl = document.getElementById('netplan-data');
    var optsEl = document.getElementById('netplan-options');
    if (tokenEl && dataEl && optsEl && typeof window.renderNetwork === 'function') {
        _lastRenderedToken = null; // 强制重渲染
        try {
            window.renderNetwork(dataEl.value, JSON.parse(optsEl.value));
        } catch(e) { console.error('[NET] rerender error', e); }
    }
}

// ===== 节点拖拽全局工具(独立于 renderNetwork,避免闭包问题)=====
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
        var nr = 11;
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
// 更新虚箭线(关系线,无活动箭头)
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
// 全局拖拽事件(绑定一次,通过全局 _netNodeDrag 通信)
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
        _netNodeDrag.moved = true;
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
        // 节点拖拽松手
        if (nodeDrag.moved) {
            var eid = nodeDrag.eventId;
            var evt = _netLayout && _netLayout.events ? _netLayout.events[eid] : null;
            var tid = evt ? parseInt(evt.taskId) || 0 : 0;

            // C1d: 从结束节点向右拖出 > 阈值 → "创建后续工作"
            var isCreateNew = false;
            var endTypes = ['end','both'];
            if (evt && endTypes.indexOf(evt.type) !== -1 && deltaDays > 0 && deltaDays >= 1) {
                // 检查目标位置是否无现有节点(空白区域)
                var targetEs = (evt.es || 0) + deltaDays;
                var hasNode = false;
                if (_netLayout && _netLayout.events) {
                    var targetEid = 'T' + targetEs;
                    hasNode = !!_netLayout.events[targetEid];
                }
                // 目标天数是新的(空白)→ 创建新工作
                if (!hasNode) isCreateNew = true;
            }

            if (isCreateNew) {
                // 从节点拖出 → 调用 ShowAddTaskModal(sourceTaskId, dayOffset)
                var dotNet = _netDotNet || window._netDotNet;
                if (dotNet) {
                    _showDayPopup(deltaDays, (nodeDrag.cx || 0), (nodeDrag.cy || 0), function(days) {
                        if (days > 0) {
                            // 弹回原位(新建任务的图会随重算刷新)
                            nodeDrag.group.setAttribute('transform', 'translate(0,0)');
                            delete _netEventOffsets[eid];
                            _netUpdateArrows(eid, 0, 0);
                            _netUpdateDummys(eid, 0, 0);
                            // 取节点的 es 偏移 + 输入的 days → dayOffset
                            var dayOff = (evt.es || 0) + days;
                            dotNet.invokeMethodAsync('ShowAddTaskModal', tid, dayOff);
                        } else {
                            // 取消 → 保留 Y 偏移,重置 X
                            var yOff = _netEventOffsets[eid] ? _netEventOffsets[eid].y : 0;
                            nodeDrag.group.setAttribute('transform', 'translate(0,' + yOff + ')');
                            _netEventOffsets[eid] = { x: 0, y: yOff };
                            _netUpdateArrows(eid, 0, yOff);
                            _netUpdateDummys(eid, 0, yOff);
                        }
                    });
                }
            } else if (tid > 0) {
                // 标准拖拽:修改现有任务日期/工期
                // Y 偏移保留(纵向微调),只对 X 偏移询问
                _showDayPopup(deltaDays, (nodeDrag.cx || 0), (nodeDrag.cy || 0), function(days) {
                    if (days !== 0) {
                        var dotNet = _netDotNet || window._netDotNet;
                        var nodeRole = evt.type === 'start' ? 'start' : (evt.type === 'both' ? 'end' : 'end');
                        if (dotNet) {
                            // 清除 X 偏移(SyncNodeDrag 会重渲染),保留 Y 偏移
                            var yOff = _netEventOffsets[eid] ? _netEventOffsets[eid].y : 0;
                            delete _netEventOffsets[eid];
                            if (yOff !== 0) _netEventOffsets[eid] = { x: 0, y: yOff };
                            dotNet.invokeMethodAsync('SyncNodeDrag', [tid], days, nodeRole);
                        }
                    } else {
                        // 用户取消/输入0 X偏移 → 保留 Y 偏移,只重置 X
                        var yOff = _netEventOffsets[eid] ? _netEventOffsets[eid].y : 0;
                        nodeDrag.group.setAttribute('transform', 'translate(0,' + yOff + ')');
                        _netEventOffsets[eid] = { x: 0, y: yOff };
                        _netUpdateArrows(eid, 0, yOff);
                        _netUpdateDummys(eid, 0, yOff);
                    }
                });
            } else if (dx !== 0 || dy !== 0) {
                // 合并节点(无 taskId 或 tid=0):只做纵向微调,不弹出日期框
                _netEventOffsets[eid] = { x: dx, y: dy };
            }
        } else {
            // 单击(没移动)→ 弹回原位
            g.setAttribute('transform', 'translate(0,0)');
            delete _netEventOffsets[eid];
            _netUpdateArrows(eid, 0, 0);
            _netUpdateDummys(eid, 0, 0);
        }
    });
}

// ===== 导出图形文件 (E4) =====
window.exportNetworkSVG = function() {
    var svg = document.getElementById('network-svg');
    if (!svg) return '';
    return svg.outerHTML;
};

window.downloadSVG = function(filename) {
    var svg = document.getElementById('network-svg');
    if (!svg) return;
    var content = svg.outerHTML;
    var blob = new Blob([content], {type: 'image/svg+xml;charset=utf-8'});
    var url = URL.createObjectURL(blob);
    var a = document.createElement('a');
    a.href = url;
    a.download = filename || 'network.svg';
    document.body.appendChild(a);
    a.click();
    document.body.removeChild(a);
    URL.revokeObjectURL(url);
};

// E4: 导出 PNG - SVG -> Canvas -> toBlob -> download
window.exportNetworkPNG = function(filename) {
    var svg = document.getElementById('network-svg');
    if (!svg) { console.warn('[PNG] no SVG found'); return; }
    var svgData = svg.outerHTML;
    var svgW = parseFloat(svg.getAttribute('width')) || 800;
    var svgH = parseFloat(svg.getAttribute('height')) || 600;
    // 创建内联 SVG Blob
    var blob = new Blob([svgData], {type: 'image/svg+xml;charset=utf-8'});
    var url = URL.createObjectURL(blob);
    var canvas = document.createElement('canvas');
    var scale = 2; // 高清输出
    canvas.width = svgW * scale;
    canvas.height = svgH * scale;
    var ctx = canvas.getContext('2d');
    var img = new Image();
    img.onload = function() {
        ctx.fillStyle = '#fff';
        ctx.fillRect(0, 0, canvas.width, canvas.height);
        ctx.drawImage(img, 0, 0, canvas.width, canvas.height);
        URL.revokeObjectURL(url);
        canvas.toBlob(function(pngBlob) {
            var pngUrl = URL.createObjectURL(pngBlob);
            var a = document.createElement('a');
            a.href = pngUrl;
            a.download = filename || 'network.png';
            document.body.appendChild(a);
            a.click();
            document.body.removeChild(a);
            URL.revokeObjectURL(pngUrl);
        }, 'image/png');
    };
    img.onerror = function() {
        console.warn('[PNG] Image load failed, falling back to data URI download');
        URL.revokeObjectURL(url);
        var encoded = encodeURIComponent(svgData);
        var dataUri = 'data:image/svg+xml,' + encoded;
        var a = document.createElement('a');
        a.href = dataUri;
        a.download = filename || 'network.png';
        document.body.appendChild(a);
        a.click();
        document.body.removeChild(a);
    };
    img.src = url;
};

// E6: 打印 - 隐藏面板 + 缩放适应 A4 横向
window.printNetwork = function(printZoomVal) {
    var zoom = (printZoomVal && printZoomVal > 0) ? printZoomVal / 100 : 1;
    var svg = document.getElementById('network-svg');
    if (svg) {
        svg.style.setProperty('transform', 'scale(' + zoom + ')', 'important');
        svg.style.setProperty('transform-origin', 'top left', 'important');
    }
    // 隐藏 UI 面板
    var panels = document.querySelectorAll('.page-header, .header-extra, .display-toggles, .header-right, .zoom-control');
    panels.forEach(function(p) {
        if (p) p.style.display = 'none';
    });
    setTimeout(function() {
        window.print();
        // 恢复面板显示
        panels.forEach(function(p) {
            if (p) p.style.display = '';
        });
        if (svg) {
            svg.style.removeProperty('transform');
            svg.style.removeProperty('transform-origin');
        }
    }, 100);
};

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

// ===== 首页项目勾选(跨标签联动)=====
window.getCheckedProject = function() {
    return parseInt(localStorage.getItem('netplan_checked') || '0');
};
window.setCheckedProject = function(id) {
    localStorage.setItem('netplan_checked', id);
};
// 导航到勾选项目的指定页面,无勾选则回首页
window.navToChecked = function(page) {
    var checked = window.getCheckedProject();
    if (checked > 0) {
        window.location.href = '/project/' + checked + '/' + page;
    } else {
        window.location.href = '/';
    }
};

// 适应视图:缩放 SVG 使之适应可视区域
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

// 自动轮询检测token变化(兼容Blazor)
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

    // 延迟初始化(等待 Blazor 渲染完成)
    setTimeout(initResize, 1000);
})();
