// ============================================================
// render/network.ts — 网络图主渲染
// 当前: 导入 legacy renderNetwork 逻辑作为初始实现
// TODO: 逐步提取子函数到各 render/ 子模块
// ============================================================
import { calculateTimeParams, applySingleStartEnd } from '../core/cpm.js';
import { calculateVerticalLayout } from '../core/layout.js';
import { buildNetworkSvg } from './generator.js';
import { updateArrowPaths, updateDummyPaths } from './arrows.js';
import { updateCrossArcOverlays } from './crossarc.js';
import { showDayPopup } from '../interaction/nodedrag.js';
/**
 * renderNetwork — 网络图渲染入口
 * 保持与 legacy 完全相同的参数和返回结构
 */
export function renderNetwork(elementsJson, optsIn) {
    var opts = optsIn;
    // 兼容 C# 传入 JSON 字符串
    if (typeof opts === 'string') {
        try {
            opts = JSON.parse(opts);
        }
        catch (e) {
            opts = {};
        }
    }
    opts = opts || {};
    var mode = opts.mode || window._netMode || 'time';
    window._netMode = mode;
    var showCritical = opts.showCritical !== false;
    var showFloat = opts.showFloat !== false;
    var showTodayLine = opts.showTodayLine !== false && mode === 'time';
    var showProgressLine = opts.showProgressLine !== false && mode === 'time';
    var psd = opts.projectStartDate || new Date().toISOString().slice(0, 10);
    var totalDays = opts.totalDays || 90;
    var vw = window.innerWidth || document.documentElement.clientWidth || 1920;
    var dayWidth = (mode === 'logic') ? 80 : (opts.dayWidth || 8);
    if (mode !== 'logic' && vw < 1400 && dayWidth > 12) {
        dayWidth = Math.max(8, dayWidth * 0.7);
    }
    var pn = opts.projectName || '网络计划';
    console.log('[NET] SVG render. opts:', JSON.stringify({ totalDays, dayWidth, showTodayLine, showProgressLine, mode }));
    window._netRendered = false;
    var ce = document.getElementById('cy');
    if (!ce) {
        console.warn('[NET] cy not found, retry in 100ms');
        setTimeout(function () { renderNetwork(elementsJson, opts); }, 100);
        return;
    }
    var elements;
    try {
        elements = JSON.parse(elementsJson);
    }
    catch (e) {
        console.error('JSON parse', e);
        return;
    }
    var tasks = [], rels = [];
    elements.forEach(function (el) {
        if (el.data) {
            if (el.data.es !== undefined)
                tasks.push(el.data);
            if (el.data.source)
                rels.push(el.data);
        }
    });
    if (!tasks.length) {
        console.warn('[NET] No tasks');
        return;
    }
    console.log('[NET] tasks:', tasks.length, '| rels:', rels.length);
    var tp = calculateTimeParams(tasks, rels);
    try {
        applySingleStartEnd(tp);
    }
    catch (e) {
        console.error('[NET] applySingleStartEnd failed:', e);
    }
    var layout = calculateVerticalLayout(tp);
    // 入/出度
    layout.eventIn = {};
    layout.eventOut = {};
    Object.keys(layout.events).forEach(function (eid) {
        layout.eventIn[eid] = (tp.eventPred[eid] || []).length;
        layout.eventOut[eid] = (tp.eventSucc[eid] || []).length;
    });
    var ML = 12;
    // 逻辑模式 vs 时标模式
    var cx;
    if (mode === 'logic') {
        var sortedEids = tp.sortedEvents;
        var logicGap = dayWidth * 1.8;
        sortedEids.forEach(function (eid, idx) {
            layout.events[eid].x = ML + idx * logicGap + logicGap / 2;
        });
        cx = ML + sortedEids.length * logicGap + 200;
    }
    else {
        var maxEs = 0;
        Object.keys(layout.events).forEach(function (eid) {
            layout.events[eid].x = ML + (layout.events[eid].es || 0) * dayWidth;
            if ((layout.events[eid].es || 0) > maxEs)
                maxEs = layout.events[eid].es;
        });
        var rightMargin = Math.max(totalDays, maxEs) * dayWidth;
        cx = ML + rightMargin + 100;
    }
    var netLayerHeight = opts.layerHeight || 60;
    var netNodeRadius = opts.nodeRadius || 11;
    // 自适应高度
    var layerKeys = Object.keys(layout.layerEvents || {}).map(Number);
    var maxLayerNum = layerKeys.length > 0 ? Math.max.apply(null, layerKeys) : 1;
    // 需要为底部标尺(57px) + 图例(68px) + 间距(10px)留出空间
    var cySize = Math.max(100 + maxLayerNum * netLayerHeight + 60, 400);
    // 底部标尺(57) + 图例(85) + 间距(30) = 172，加余量到 300
    cySize += 300;
    console.log('[NET] SVG initial size:', cx, 'x', cySize);
    var layerScale = netLayerHeight / 60;
    if (Math.abs(layerScale - 1) > 0.01) {
        Object.keys(layout.events).forEach(function (eid) {
            layout.events[eid].y = 100 + (layout.events[eid].y - 100) * layerScale;
        });
    }
    // 空行偏移
    var extraSpacing = window._netExtraSpacing || {};
    Object.keys(extraSpacing).forEach(function (key) {
        var extraCount = extraSpacing[key];
        if (!extraCount || extraCount <= 0)
            return;
        var layerNum = parseInt(key);
        Object.keys(layout.events).forEach(function (eid) {
            var evt = layout.events[eid];
            var layerIdx = Math.round((evt.y - 100) / netLayerHeight);
            if (layerIdx >= layerNum) {
                evt.y += extraCount * netLayerHeight;
            }
        });
    });
    // ===== 构建 SVG =====
    ce.innerHTML = buildNetworkSvg({
        projectStartDate: psd,
        totalDays: totalDays,
        totalDuration: opts.totalDuration || 0,
        dayWidth: dayWidth,
        timeParams: tp,
        layout: layout,
        mode: mode,
        showCritical: showCritical,
        showFloat: showFloat,
        showTodayLine: showTodayLine,
        showProgressLine: showProgressLine,
        showDummyArrows: opts.showDummyArrows,
        showProgressCurve: opts.showProgressCurve === true,
        showGridH: opts.showGridH === true,
        showGridV: opts.showGridV === true,
        _maxLayer: typeof layout.maxLayer === 'number' ? layout.maxLayer : 10,
        _marginTop: 100,
        _pendingOffsets: window._netPendingOffsets || null,
        projectName: pn,
        nodeRadius: netNodeRadius,
        layerHeight: netLayerHeight,
        labelFontSize: opts.labelFontSize || 10,
        labelFields: opts.labelFields || [],
        rowLabels: opts.rowLabels || [],
        restDayPattern: opts.restDayPattern !== false,
        canvasW: cx,
        canvasH: cySize
    });
    // 自适应高度
    var actualH = window._netSvgHeight || cySize;
    if (actualH > cySize) {
        ce.style.height = actualH + 'px';
    }
    console.log('[NET] SVG rendered. height:', actualH);
    var svg = ce.querySelector('svg');
    // 双击事件
    if (svg) {
        svg.ondblclick = function (e) {
            var dotNet = window._netDotNet;
            if (window._netLastPopupTime && (Date.now() - window._netLastPopupTime) < 500)
                return;
            if (!dotNet) {
                console.warn('[NET] dblclick: _netDotNet not set');
                return;
            }
            var el = e.target.closest('.net-event');
            if (el) {
                var tid = parseInt(el.getAttribute('data-task-id') || '');
                if (tid) {
                    console.log('[NET] dblclick node taskId=', tid);
                    dotNet.invokeMethodAsync('OpenTaskEditor', tid).catch(function (e) { console.warn('[NET] invokeMethodAsync error:', e); });
                }
                return;
            }
            var activityEl = e.target.closest('[data-activity-id]');
            if (activityEl) {
                var aid = parseInt(activityEl.getAttribute('data-activity-id') || '');
                if (!isNaN(aid) && aid > 0) {
                    console.log('[NET] dblclick activity taskId=', aid);
                    dotNet.invokeMethodAsync('OpenTaskEditor', aid).catch(function (e) { console.warn('[NET] invokeMethodAsync error:', e); });
                }
                return;
            }
            // 双击空白区
            var rect = svg.getBoundingClientRect();
            var clickX = e.clientX - rect.left;
            var dayOffset = Math.max(0, Math.round((clickX - 12) / dayWidth));
            console.log('[NET] dblclick blank area, dayOffset=', dayOffset);
            dotNet.invokeMethodAsync('ShowAddTaskModal', 0, dayOffset).catch(function (e) { console.warn('[NET] invokeMethodAsync error:', e); });
        };
    }
    // 行高亮
    var rowRects = svg ? svg.querySelectorAll('.net-row-bg') : [];
    rowRects.forEach(function (rect) {
        rect.addEventListener('click', function (e) {
            e.stopPropagation();
            var hadSel = rect.classList.contains('net-row-sel');
            rowRects.forEach(function (r) { r.classList.remove('net-row-sel'); });
            if (!hadSel)
                rect.classList.add('net-row-sel');
        });
    });
    // 更新全局引用
    window._netLayout = layout;
    window._netActivities = tp.activities;
    window._netSvg = svg;
    window._netDayWidth = dayWidth;
    // 偏移补偿
    var pendingOffsets = window._netPendingOffsets;
    if (pendingOffsets) {
        Object.keys(pendingOffsets).forEach(function (eid) {
            var oldPos = pendingOffsets[eid];
            var evt = layout.events[eid];
            var eventOffsets = window._netEventOffsets || {};
            if (evt && eventOffsets[eid]) {
                var deltaLayoutX = evt.x - oldPos.x;
                var deltaLayoutY = evt.y - oldPos.y;
                eventOffsets[eid].x -= deltaLayoutX;
                eventOffsets[eid].y -= deltaLayoutY;
            }
        });
        delete window._netPendingOffsets;
    }
    // 恢复偏移
    var eventOffsets = window._netEventOffsets || {};
    Object.keys(eventOffsets).forEach(function (eid) {
        var off = eventOffsets[eid];
        var g = svg && svg.querySelector('[data-event-id="' + eid + '"]');
        if (g && (off.x !== 0 || off.y !== 0)) {
            g.setAttribute('transform', 'translate(' + off.x + ',' + off.y + ')');
        }
    });
    // 恢复箭头路径
    var hasOffsets = false;
    Object.keys(eventOffsets).forEach(function (eid) {
        var off = eventOffsets[eid];
        if (off.x !== 0 || off.y !== 0) {
            updateArrowPaths(eid, off.x, off.y);
            updateDummyPaths(eid, off.x, off.y);
            hasOffsets = true;
        }
    });
    if (hasOffsets)
        updateCrossArcOverlays();
    // 节点拖拽 + 箭线拖拽
    if (svg) {
        svg.addEventListener('mousedown', function (e) {
            var target = e.target;
            console.log('[NetDrag] mousedown tag=' + target.tagName + ' class=' + target.className);
            // 点击圆形节点 → 节点拖拽
            if (target.tagName === 'circle') {
                var g = target.closest('.net-event');
                if (!g || !window._netLayout || !window._netLayout.events)
                    return;
                var eid = g.getAttribute('data-event-id') || '';
                var off = eventOffsets[eid] || { x: 0, y: 0 };
                window._netNodeDrag = { eventId: eid, group: g, startX: e.clientX, startY: e.clientY, offX: off.x, offY: off.y, moved: false };
                g.style.cursor = 'grabbing';
                e.stopPropagation();
                return;
            }
            // 点击活动路径 → 箭线拖拽
            var activityPath = target.closest('[data-activity-id]');
            console.log('[NetDrag] activityPath=' + (activityPath ? activityPath.getAttribute('data-activity-id') : 'null'));
            if (activityPath) {
                var actId = activityPath.getAttribute('data-activity-id') || '';
                if (!actId)
                    return;
                // 从路径数据判断是水平 → 行移动，还是竖直 → 水平移动
                var sid = activityPath.getAttribute('data-src') || '';
                var tid = activityPath.getAttribute('data-tgt') || '';
                if (!sid || !tid)
                    return;
                var layout = window._netLayout;
                if (!layout || !layout.events)
                    return;
                var s = layout.events[sid], t = layout.events[tid];
                if (!s || !t)
                    return;
                var sameLayer = Math.abs((t.y || 0) - (s.y || 0)) < 2;
                if (sameLayer) {
                    // 水平箭线 → 行拖拽
                    var eY = s.y || 0;
                    var layerH = window._netLayerHeight || 60;
                    var marginTop = 100;
                    var layerIdx = Math.round((eY - marginTop) / layerH);
                    var events = layout.events;
                    var eids = Object.keys(events).filter(function (eid) {
                        var evt = events[eid];
                        var l = Math.round(((evt.y || 0) - marginTop) / layerH);
                        return l === layerIdx && !evt.isVirtual;
                    });
                    if (eids.length > 0) {
                        window._netRowDrag = {
                            startY: e.clientY, eids: eids, moved: false,
                            startOffsets: JSON.parse(JSON.stringify(eventOffsets))
                        };
                    }
                }
                else {
                    // 竖直箭线 → 水平位置拖拽
                    window._netVertDrag = {
                        startX: e.clientX, startY: e.clientY,
                        srcId: sid, tgtId: tid, actId: actId,
                        sX: s.x || 0, tX: t.x || 0, moved: false
                    };
                }
                e.stopPropagation();
                return;
            }
        });
    }
    // 前锋线拖动
    if (showProgressLine) {
        var plGroup = document.getElementById('net-progress-check');
        if (plGroup && svg) {
            plGroup.onmousedown = function (e) {
                e.preventDefault();
                var bodyEl = document.getElementById('network-body');
                window._dragInfo = { startX: e.clientX, startLineX: window._progressX, startScrollLeft: bodyEl ? bodyEl.scrollLeft : 0 };
            };
            plGroup.style.cursor = 'ew-resize';
        }
    }
    // 全局拖动事件(只绑定一次)
    if (!window._netDragBound) {
        window._netDragBound = true;
        document.addEventListener('mousemove', function (e) {
            // ===== 竖直箭线拖拽预览 =====
            var vd = window._netVertDrag;
            if (vd) {
                if (Math.abs(e.clientX - vd.startX) > 3)
                    vd.moved = true;
                return;
            }
            // ===== 行拖拽预览 =====
            var rd = window._netRowDrag;
            if (rd) {
                var layerH = window._netLayerHeight || 60;
                var rdDy = Math.round((e.clientY - rd.startY) / layerH) * layerH;
                if (Math.abs(e.clientY - rd.startY) > 3)
                    rd.moved = true;
                if (rd.moved) {
                    var svgEl = document.querySelector('#network-svg');
                    rd.eids.forEach(function (eid) {
                        var g = svgEl ? svgEl.querySelector('[data-event-id="' + eid + '"]') : null;
                        if (g) {
                            var origOff = rd.startOffsets[eid] || { x: 0, y: 0 };
                            g.setAttribute('transform', 'translate(' + origOff.x + ',' + (origOff.y + rdDy) + ')');
                        }
                    });
                }
                return;
            }
            // ===== 节点拖拽预览 =====
            var nd = window._netNodeDrag;
            if (nd) {
                var layerH = window._netLayerHeight || 60;
                var dx = (e.clientX - nd.startX) + nd.offX;
                var dy = nd.offY + Math.round((e.clientY - nd.startY) / layerH) * layerH;
                if (Math.abs(e.clientX - nd.startX) > 3 || Math.abs(e.clientY - nd.startY) > 3) {
                    nd.moved = true;
                    nd.group.setAttribute('transform', 'translate(' + dx + ',' + dy + ')');
                }
                return;
            }
            // ===== 前锋线拖拽 =====
            var dragInfo = window._dragInfo;
            if (!dragInfo)
                return;
            var pdx = e.clientX - dragInfo.startX;
            var bodyEl = document.getElementById('network-body');
            var scrollDelta = (bodyEl ? bodyEl.scrollLeft : 0) - dragInfo.startScrollLeft;
            var newX = dragInfo.startLineX + pdx + scrollDelta;
            var minX = 12, maxX = parseFloat(svg.getAttribute('width') || '800') - 100;
            newX = Math.max(minX, Math.min(newX, maxX));
            var checkGroup = document.getElementById('net-progress-check');
            var line = checkGroup ? checkGroup.querySelector('line') : null;
            var handle = checkGroup ? checkGroup.querySelector('polygon') : null;
            var label = checkGroup ? checkGroup.querySelector('text') : null;
            if (line) {
                line.setAttribute('x1', String(newX));
                line.setAttribute('x2', String(newX));
            }
            if (handle)
                handle.setAttribute('points', (newX - 7) + ',0 ' + (newX + 7) + ',0 ' + newX + ',14');
            if (label) {
                label.setAttribute('x', String(newX + 4));
            }
            if (window._projStartDate) {
                var dayOffset = Math.round((newX - 80) / dayWidth);
                var pd = new Date(window._projStartDate);
                pd.setDate(pd.getDate() + dayOffset);
                window._progressCheckDate = pd;
                window._progressDate = pd;
                window._progressX = newX;
                var cl = pd.getFullYear() + '-' + String(pd.getMonth() + 1).padStart(2, '0') + '-' + String(pd.getDate()).padStart(2, '0');
                localStorage.setItem('netplan_progress_date', cl);
                if (label)
                    label.textContent = cl;
            }
            // 更新进度颜色
            if (typeof window.updateProgressColors === 'function')
                window.updateProgressColors();
        });
        document.addEventListener('mouseup', function (e) {
            window._dragInfo = null;
            // ===== 竖直箭线拖拽 mouseup =====
            var vd = window._netVertDrag;
            if (vd) {
                delete window._netVertDrag;
                if (vd.moved) {
                    var dayWidth = window._netDayWidth || 8;
                    var vdx = e.clientX - vd.startX;
                    var deltaDays = Math.round(vdx / dayWidth);
                    if (deltaDays !== 0) {
                        var offsets = window._netEventOffsets || {};
                        var srcOff = offsets[vd.srcId] || { x: 0, y: 0 };
                        offsets[vd.srcId] = { x: srcOff.x + deltaDays * dayWidth, y: srcOff.y || 0 };
                        updateArrowPaths(vd.srcId, offsets[vd.srcId].x, offsets[vd.srcId].y);
                        updateDummyPaths(vd.srcId, offsets[vd.srcId].x, offsets[vd.srcId].y);
                        updateCrossArcOverlays();
                    }
                }
                return;
            }
            // ===== 行拖拽 mouseup =====
            var rd = window._netRowDrag;
            if (rd) {
                delete window._netRowDrag;
                if (rd.moved) {
                    var layerH = window._netLayerHeight || 60;
                    var rdDy = Math.round((e.clientY - rd.startY) / layerH) * layerH;
                    if (rdDy !== 0) {
                        var offsets = window._netEventOffsets || {};
                        var layout = window._netLayout;
                        rd.eids.forEach(function (eid) {
                            var origOff = rd.startOffsets[eid] || { x: 0, y: 0 };
                            offsets[eid] = { x: origOff.x || 0, y: (origOff.y || 0) + rdDy };
                            // 更新箭线
                            updateArrowPaths(eid, offsets[eid].x, offsets[eid].y);
                            updateDummyPaths(eid, offsets[eid].x, offsets[eid].y);
                        });
                        updateCrossArcOverlays();
                    }
                }
                return;
            }
            // ===== 节点拖拽 mouseup =====
            var nd = window._netNodeDrag;
            if (nd) {
                if (nd.group)
                    nd.group.style.cursor = '';
                var dx = (e.clientX - nd.startX) + nd.offX;
                var dy = nd.offY; // Y 偏移在 mousemove 中已吸附
                if ((nd.startY !== undefined)) {
                    var layerH = window._netLayerHeight || 60;
                    dy = nd.offY + Math.round((e.clientY - nd.startY) / layerH) * layerH;
                }
                if (nd.moved && (dx !== 0 || dy !== 0)) {
                    var eid = nd.eventId;
                    var offsets = window._netEventOffsets || {};
                    var layout = window._netLayout;
                    var evt = layout && layout.events ? layout.events[eid] : null;
                    if (evt) {
                        var dayWidth = window._netDayWidth || 8;
                        var deltaDays = Math.round(dx / dayWidth);
                        if (deltaDays !== 0 || dy !== 0) {
                            // 记住最终偏移
                            offsets[eid] = { x: deltaDays * dayWidth, y: dy };
                            // 存储布局旧位置供 _netPendingOffsets 补偿
                            var pending = window._netPendingOffsets || {};
                            pending[eid] = { x: evt.x, y: evt.y };
                            window._netPendingOffsets = pending;
                            // 弹出天数对话框
                            var existingOffXDays = Math.round((offsets[eid] ? offsets[eid].x : 0) / dayWidth);
                            window._netLastPopupTime = Date.now();
                            showDayPopup(deltaDays, e.clientX, e.clientY, function (confirmedDays) {
                                window._netLastPopupTime = Date.now();
                                var finalX = confirmedDays * dayWidth;
                                if (confirmedDays === 0) {
                                    // 取消：直接清除 SVG transform 回原位
                                    if (nd.group)
                                        nd.group.removeAttribute('transform');
                                    delete offsets[eid];
                                    delete pending[eid];
                                    return;
                                }
                                // 保存本轮累计总偏移
                                offsets[eid] = { x: finalX, y: dy };
                                // 立即应用偏移到 SVG，不等 C# 异步响应
                                if (nd.group) {
                                    nd.group.setAttribute('transform', 'translate(' + finalX + ',' + dy + ')');
                                }
                                // 同步更新事件的连线（不走重渲染）
                                updateArrowPaths(eid, finalX, dy);
                                updateDummyPaths(eid, finalX, dy);
                                updateCrossArcOverlays();
                                // 计算净增加的天数（相对 C# 已保存的上次值）
                                var netDelta = confirmedDays - existingOffXDays;
                                if (netDelta !== 0) {
                                    var dotNet = window._netDotNet;
                                    if (dotNet && eid) {
                                        var tid = evt.taskId || parseInt(eid.replace('T', ''));
                                        if (tid && tid > 0) {
                                            dotNet.invokeMethodAsync('SyncNodeDrag', tid, netDelta, 0)
                                                .catch(function (err) { console.warn('[NET] SyncNodeDrag error:', err); });
                                        }
                                    }
                                }
                                // 更新 existingOffXDays 为当前值，供下一次拖拽使用
                                existingOffXDays = confirmedDays;
                            });
                        }
                    }
                }
                else {
                    // 没有移动，清除
                    var eid = nd.eventId;
                    var offsets = window._netEventOffsets || {};
                    if (offsets[eid]) {
                        delete offsets[eid];
                        if (nd.group) {
                            nd.group.removeAttribute('transform');
                        }
                    }
                }
                delete window._netNodeDrag;
            }
        });
    }
    // 首次渲染后初始化前锋线颜色
    setTimeout(function () { if (typeof window.updateProgressColors === 'function')
        window.updateProgressColors(); }, 200);
    // === 横向滚动条同步 ===
    var hscroll = document.getElementById('network-hscroll');
    var hscrollInner = document.getElementById('network-hscroll-inner');
    var bodyEl = document.getElementById('network-body');
    if (hscrollInner)
        hscrollInner.style.width = cx + 'px';
    var _hSyncing = false;
    if (hscroll && bodyEl) {
        hscroll.onscroll = function () {
            if (_hSyncing)
                return;
            _hSyncing = true;
            bodyEl.scrollLeft = hscroll.scrollLeft;
            _hSyncing = false;
        };
        bodyEl.onscroll = function () {
            if (_hSyncing)
                return;
            _hSyncing = true;
            hscroll.scrollLeft = bodyEl.scrollLeft;
            _hSyncing = false;
        };
    }
    window._netRendered = true;
    window._networkData = { events: layout.events, activities: tp.activities, relations: tp.relations };
    window._networkOpts = opts;
    // 渲染后自检
    validateNetworkRender();
    // 画布拖拽平移
    var netBody = document.getElementById('network-body');
    if (netBody && !window._panBound) {
        window._panBound = true;
        var panState = null;
        netBody.addEventListener('mousedown', function (e) {
            if (e.target.closest('.net-event') || e.target.closest('[data-activity-id]'))
                return;
            if (e.target.closest('#net-progress-line') || e.target.closest('#net-progress-check'))
                return;
            panState = { x: e.clientX, y: e.clientY, sx: netBody.scrollLeft, sy: netBody.scrollTop };
            netBody.style.cursor = 'grabbing';
        });
        window.addEventListener('mousemove', function (e) {
            if (!panState)
                return;
            netBody.scrollLeft = panState.sx - (e.clientX - panState.x);
            netBody.scrollTop = panState.sy - (e.clientY - panState.y);
        });
        window.addEventListener('mouseup', function () {
            if (!panState)
                return;
            netBody.style.cursor = '';
            panState = null;
        });
    }
}
// 渲染自检
function validateNetworkRender() {
    var errors = [];
    var svg = document.getElementById('cy')?.querySelector('svg');
    if (!svg) {
        errors.push('SVG元素不存在'); /* don't return, check more */
    }
    if (!window._netLayout || !window._netActivities)
        errors.push('_netLayout/_netActivities 为空');
    if (!window._netDotNet)
        errors.push('_netDotNet 未设置');
    if (errors.length > 0) {
        console.warn('[VALIDATE] ' + errors.length + ' issue(s):');
        errors.forEach(function (e) { console.warn('  [VALIDATE] ' + e); });
    }
    else {
        console.log('[VALIDATE] All checks passed');
    }
}
