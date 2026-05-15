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

// 全局状态（与 legacy 保持兼容）
declare var _netMode: string;
declare var _netRendered: boolean;
declare var _netLayout: any;
declare var _netActivities: any[];
declare var _netSvg: SVGSVGElement | null;
declare var _netDayWidth: number;
declare var _netEventOffsets: Record<string, { x: number; y: number }>;
declare var _netPendingOffsets: any;
declare var _netDotNet: any;
declare var _netExtraSpacing: Record<string, number>;
declare var _netNodeDrag: any;
declare var _netLastPopupTime: number;
declare var _networkOpts: any;
declare var _networkData: any;
declare var _dragInfo: any;
declare var _progressDate: Date | null;
declare var _progressCheckDate: Date | null;
declare var _progressX: number;
declare var _projStartDate: Date | null;

/**
 * renderNetwork — 网络图渲染入口
 * 保持与 legacy 完全相同的参数和返回结构
 */
export function renderNetwork(elementsJson: string, optsIn: any): void {
  var opts = optsIn;
  // 兼容 C# 传入 JSON 字符串
  if (typeof opts === 'string') { try { opts = JSON.parse(opts); } catch(e) { opts = {}; } }
  opts = opts || {};

  var mode = opts.mode || (window as any)._netMode || 'time';
  (window as any)._netMode = mode;
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
  (window as any)._netRendered = false;

  var ce = document.getElementById('cy');
  if (!ce) {
    console.warn('[NET] cy not found, retry in 100ms');
    setTimeout(function() { renderNetwork(elementsJson, opts); }, 100);
    return;
  }

  var elements;
  try { elements = JSON.parse(elementsJson); } catch(e) { console.error('JSON parse', e); return; }

  var tasks: any[] = [], rels: any[] = [];
  elements.forEach(function(el: any) {
    if (el.data) {
      if (el.data.es !== undefined) tasks.push(el.data);
      if (el.data.source) rels.push(el.data);
    }
  });
  if (!tasks.length) { console.warn('[NET] No tasks'); return; }
  console.log('[NET] tasks:', tasks.length, '| rels:', rels.length);

  var tp = calculateTimeParams(tasks, rels);
  try { applySingleStartEnd(tp); } catch(e) { console.error('[NET] applySingleStartEnd failed:', e); }
  var layout = calculateVerticalLayout(tp);

  // 入/出度
  layout.eventIn = {};
  layout.eventOut = {};
  Object.keys(layout.events).forEach(function(eid: string) {
    layout.eventIn[eid] = (tp.eventPred[eid] || []).length;
    layout.eventOut[eid] = (tp.eventSucc[eid] || []).length;
  });

  var ML = 80;
  // 逻辑模式 vs 时标模式
  var cx: number;
  if (mode === 'logic') {
    var sortedEids = tp.sortedEvents;
    var logicGap = dayWidth * 1.8;
    sortedEids.forEach(function(eid: string, idx: number) {
      layout.events[eid].x = ML + idx * logicGap + logicGap / 2;
    });
    cx = ML + sortedEids.length * logicGap + 200;
  } else {
    var maxEs = 0;
    Object.keys(layout.events).forEach(function(eid: string) {
      layout.events[eid].x = ML + (layout.events[eid].es || 0) * dayWidth;
      if ((layout.events[eid].es || 0) > maxEs) maxEs = layout.events[eid].es;
    });
    var rightMargin = Math.max(totalDays, maxEs) * dayWidth;
    cx = ML + rightMargin + 100;
  }

  var netLayerHeight = opts.layerHeight || 60;
  var netNodeRadius = opts.nodeRadius || 11;

  // 自适应高度
  var layerKeys = Object.keys(layout.layerEvents || {}).map(Number);
  var maxLayerNum = layerKeys.length > 0 ? Math.max.apply(null, layerKeys) : 1;
  var cySize = Math.max(100 + maxLayerNum * netLayerHeight + 60, 400);
  console.log('[NET] SVG size:', cx, 'x', cySize);
  var layerScale = netLayerHeight / 60;
  if (Math.abs(layerScale - 1) > 0.01) {
    Object.keys(layout.events).forEach(function(eid: string) {
      layout.events[eid].y = 100 + (layout.events[eid].y - 100) * layerScale;
    });
  }

  // 空行偏移
  var extraSpacing = (window as any)._netExtraSpacing || {};
  Object.keys(extraSpacing).forEach(function(key: string) {
    var extraCount = extraSpacing[key];
    if (!extraCount || extraCount <= 0) return;
    var layerNum = parseInt(key);
    Object.keys(layout.events).forEach(function(eid: string) {
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

  console.log('[NET] SVG rendered.');
  var svg = ce.querySelector('svg') as SVGSVGElement;

  // 双击事件
  if (svg) {
    svg.ondblclick = function(e) {
      var dotNet = (window as any)._netDotNet;
      if ((window as any)._netLastPopupTime && (Date.now() - (window as any)._netLastPopupTime) < 500) return;
      if (!dotNet) { console.warn('[NET] dblclick: _netDotNet not set'); return; }

      var el = (e.target as HTMLElement).closest('.net-event') as HTMLElement;
      if (el) {
        var tid = parseInt(el.getAttribute('data-task-id') || '');
        if (tid) { console.log('[NET] dblclick node taskId=', tid); dotNet.invokeMethodAsync('OpenTaskEditor', tid).catch(function(e: any) { console.warn('[NET] invokeMethodAsync error:', e); }); }
        return;
      }

      var activityEl = (e.target as HTMLElement).closest('[data-activity-id]') as HTMLElement;
      if (activityEl) {
        var aid = parseInt(activityEl.getAttribute('data-activity-id') || '');
        if (!isNaN(aid) && aid > 0) { console.log('[NET] dblclick activity taskId=', aid); dotNet.invokeMethodAsync('OpenTaskEditor', aid).catch(function(e: any) { console.warn('[NET] invokeMethodAsync error:', e); }); }
        return;
      }

      // 双击空白区
      var rect = svg.getBoundingClientRect();
      var clickX = e.clientX - rect.left;
      var dayOffset = Math.max(0, Math.round((clickX - 80) / dayWidth));
      console.log('[NET] dblclick blank area, dayOffset=', dayOffset);
      dotNet.invokeMethodAsync('ShowAddTaskModal', 0, dayOffset).catch(function(e: any) { console.warn('[NET] invokeMethodAsync error:', e); });
    };
  }

  // 行高亮
  var rowRects = svg ? svg.querySelectorAll('.net-row-bg') : [];
  rowRects.forEach(function(rect: Element) {
    rect.addEventListener('click', function(e) {
      e.stopPropagation();
      var hadSel = rect.classList.contains('net-row-sel');
      rowRects.forEach(function(r: Element) { r.classList.remove('net-row-sel'); });
      if (!hadSel) rect.classList.add('net-row-sel');
    });
  });

  // 更新全局引用
  (window as any)._netLayout = layout;
  (window as any)._netActivities = tp.activities;
  (window as any)._netSvg = svg;
  (window as any)._netDayWidth = dayWidth;

  // 偏移补偿
  var pendingOffsets = (window as any)._netPendingOffsets;
  if (pendingOffsets) {
    Object.keys(pendingOffsets).forEach(function(eid: string) {
      var oldPos = pendingOffsets[eid];
      var evt = layout.events[eid];
      var eventOffsets = (window as any)._netEventOffsets || {};
      if (evt && eventOffsets[eid]) {
        var deltaLayoutX = evt.x - oldPos.x;
        var deltaLayoutY = evt.y - oldPos.y;
        eventOffsets[eid].x -= deltaLayoutX;
        eventOffsets[eid].y -= deltaLayoutY;
      }
    });
    delete (window as any)._netPendingOffsets;
  }

  // 恢复偏移
  var eventOffsets = (window as any)._netEventOffsets || {};
  Object.keys(eventOffsets).forEach(function(eid: string) {
    var off = eventOffsets[eid];
    var g = svg && svg.querySelector('[data-event-id="' + eid + '"]');
    if (g && (off.x !== 0 || off.y !== 0)) {
      g.setAttribute('transform', 'translate(' + off.x + ',' + off.y + ')');
    }
  });

  // 恢复箭头路径
  var hasOffsets = false;
  Object.keys(eventOffsets).forEach(function(eid: string) {
    var off = eventOffsets[eid];
    if (off.x !== 0 || off.y !== 0) {
      updateArrowPaths(eid, off.x, off.y);
      updateDummyPaths(eid, off.x, off.y);
      hasOffsets = true;
    }
  });
  if (hasOffsets) updateCrossArcOverlays();

  // 节点拖拽
  if (svg) {
    svg.addEventListener('mousedown', function(e) {
      if ((e.target as HTMLElement).tagName !== 'circle') return;
      var g = (e.target as HTMLElement).closest('.net-event') as HTMLElement;
      if (!g || !(window as any)._netLayout || !(window as any)._netLayout.events) return;
      var eid = g.getAttribute('data-event-id') || '';
      var off = eventOffsets[eid] || { x: 0, y: 0 };
      (window as any)._netNodeDrag = { eventId: eid, group: g, startX: e.clientX, startY: e.clientY, offX: off.x, offY: off.y, moved: false };
      g.style.cursor = 'grabbing';
      e.stopPropagation();
    });
  }

  // 前锋线拖动
  if (showProgressLine) {
    var plGroup = document.getElementById('net-progress-check');
    if (plGroup && svg) {
      plGroup.onmousedown = function(e) {
        e.preventDefault();
        var bodyEl = document.getElementById('network-body');
        (window as any)._dragInfo = { startX: e.clientX, startLineX: (window as any)._progressX, startScrollLeft: bodyEl ? bodyEl.scrollLeft : 0 };
      };
      (plGroup as HTMLElement).style.cursor = 'ew-resize';
    }
  }

  // 全局拖动事件(只绑定一次)
  if (!(window as any)._netDragBound) {
    (window as any)._netDragBound = true;

    document.addEventListener('mousemove', function(e) {
      var dragInfo = (window as any)._dragInfo;
      if (!dragInfo) return;
      var dx = e.clientX - dragInfo.startX;
      var bodyEl = document.getElementById('network-body');
      var scrollDelta = (bodyEl ? bodyEl.scrollLeft : 0) - dragInfo.startScrollLeft;
      var newX = dragInfo.startLineX + dx + scrollDelta;
      var minX = 80, maxX = parseFloat((svg as SVGSVGElement).getAttribute('width') || '800') - 100;
      newX = Math.max(minX, Math.min(newX, maxX));

      var checkGroup = document.getElementById('net-progress-check');
      var line = checkGroup ? checkGroup.querySelector('line') : null;
      var handle = checkGroup ? checkGroup.querySelector('polygon') : null;
      var label = checkGroup ? checkGroup.querySelector('text') : null;
      if (line) { line.setAttribute('x1', String(newX)); line.setAttribute('x2', String(newX)); }
      if (handle) handle.setAttribute('points', (newX - 7) + ',0 ' + (newX + 7) + ',0 ' + newX + ',14');
      if (label) { label.setAttribute('x', String(newX + 4)); }

      if ((window as any)._projStartDate) {
        var dayOffset = Math.round((newX - 80) / dayWidth);
        var pd = new Date((window as any)._projStartDate);
        pd.setDate(pd.getDate() + dayOffset);
        (window as any)._progressCheckDate = pd;
        (window as any)._progressDate = pd;
        (window as any)._progressX = newX;
        var cl = pd.getFullYear() + '-' + String(pd.getMonth() + 1).padStart(2, '0') + '-' + String(pd.getDate()).padStart(2, '0');
        localStorage.setItem('netplan_progress_date', cl);
        if (label) label.textContent = cl;
      }

      // 更新进度颜色
      if (typeof (window as any).updateProgressColors === 'function') (window as any).updateProgressColors();
    });

    document.addEventListener('mouseup', function() {
      (window as any)._dragInfo = null;
    });
  }

  // 首次渲染后初始化前锋线颜色
  setTimeout(function() { if (typeof (window as any).updateProgressColors === 'function') (window as any).updateProgressColors(); }, 200);

  // === 横向滚动条同步 ===
  var hscroll = document.getElementById('network-hscroll');
  var hscrollInner = document.getElementById('network-hscroll-inner');
  var bodyEl = document.getElementById('network-body');
  if (hscrollInner) (hscrollInner as HTMLElement).style.width = cx + 'px';
  var _hSyncing = false;
  if (hscroll && bodyEl) {
    (hscroll as HTMLElement).onscroll = function() {
      if (_hSyncing) return; _hSyncing = true;
      (bodyEl as HTMLElement).scrollLeft = (hscroll as HTMLElement).scrollLeft;
      _hSyncing = false;
    };
    bodyEl.onscroll = function() {
      if (_hSyncing) return; _hSyncing = true;
      (hscroll as HTMLElement).scrollLeft = (bodyEl as HTMLElement).scrollLeft;
      _hSyncing = false;
    };
  }

  (window as any)._netRendered = true;
  (window as any)._networkData = { events: layout.events, activities: tp.activities, relations: tp.relations };
  (window as any)._networkOpts = opts;

  // 渲染后自检
  validateNetworkRender();

  // 画布拖拽平移
  var netBody = document.getElementById('network-body');
  if (netBody && !(window as any)._panBound) {
    (window as any)._panBound = true;
    var panState: any = null;
    netBody.addEventListener('mousedown', function(e) {
      if ((e.target as HTMLElement).closest('.net-event') || (e.target as HTMLElement).closest('[data-activity-id]')) return;
      if ((e.target as HTMLElement).closest('#net-progress-line') || (e.target as HTMLElement).closest('#net-progress-check')) return;
      panState = { x: e.clientX, y: e.clientY, sx: (netBody as HTMLElement).scrollLeft, sy: (netBody as HTMLElement).scrollTop };
      (netBody as HTMLElement).style.cursor = 'grabbing';
    });
    window.addEventListener('mousemove', function(e) {
      if (!panState) return;
      (netBody as HTMLElement).scrollLeft = panState.sx - (e.clientX - panState.x);
      (netBody as HTMLElement).scrollTop = panState.sy - (e.clientY - panState.y);
    });
    window.addEventListener('mouseup', function() {
      if (!panState) return;
      (netBody as HTMLElement).style.cursor = '';
      panState = null;
    });
  }
}

// 渲染自检
function validateNetworkRender(): void {
  var errors: string[] = [];
  var svg = document.getElementById('cy')?.querySelector('svg');
  if (!svg) { errors.push('SVG元素不存在'); /* don't return, check more */ }
  if (!(window as any)._netLayout || !(window as any)._netActivities) errors.push('_netLayout/_netActivities 为空');
  if (!(window as any)._netDotNet) errors.push('_netDotNet 未设置');
  if (errors.length > 0) {
    console.warn('[VALIDATE] ' + errors.length + ' issue(s):');
    errors.forEach(function(e) { console.warn('  [VALIDATE] ' + e); });
  } else {
    console.log('[VALIDATE] All checks passed');
  }
}
