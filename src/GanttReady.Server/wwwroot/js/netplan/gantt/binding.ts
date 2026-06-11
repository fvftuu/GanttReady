// ============================================================
// gantt/binding.ts — 甘特图绑定（滚动同步 + 面板拖拽）
// ============================================================

import { syncGanttRowHeights } from './sync-rows.js';

let _ganttDotNet: any = null;

/**
 * 设置甘特图的 C# DotNet 引用
 */
export function setGanttDotNet(ref: any): void {
  _ganttDotNet = ref;
}

/**
 * 获取甘特图 C# 引用
 */
export function getGanttDotNet(): any {
  return _ganttDotNet;
}

/**
 * 初始化面板分割拖拽
 */
export function initPanelResize(): void {
  const tryInit = (attempt = 1): void => {
    if (attempt > 50) return;
    const handle = document.getElementById('gantt-resize-handle') as HTMLElement;
    const left = document.querySelector('.gantt-left') as HTMLElement;
    if (!handle || !left) {
      setTimeout(() => tryInit(attempt + 1), 100);
      return;
    }
    setupResize(handle, left);
    // 同时初始化列宽拖拽
    initColumnResizeInternal();
  };
  tryInit();
}

function initColumnResizeInternal(): void {
  document.querySelectorAll('.col-resize-handle').forEach(function(h) {
    if (h.getAttribute('data-inited')) return;
    h.setAttribute('data-inited', '1');
    var headerCell = h.parentElement as HTMLElement;
    var startX = 0, startW = 0;
    h.addEventListener('mousedown', function(e) {
      e.preventDefault();
      e.stopPropagation();
      startX = (e as MouseEvent).clientX;
      startW = headerCell.offsetWidth;
      document.body.style.cursor = 'col-resize';
      document.body.style.userSelect = 'none';
      var onMouseMove = function(me: MouseEvent) {
        var delta = me.clientX - startX;
        var newW = Math.max(40, startW + delta);
        headerCell.style.width = newW + 'px';
        // 同步所有行同名字段
        var cls = headerCell.className.split(' ').filter(function(c) { return c.indexOf('col-') === 0; })[0];
        if (cls) {
          document.querySelectorAll('.gantt-left-row .' + cls).forEach(function(cell) {
            (cell as HTMLElement).style.width = newW + 'px';
          });
        }
      };
      var onMouseUp = function() {
        document.removeEventListener('mousemove', onMouseMove as any);
        document.removeEventListener('mouseup', onMouseUp as any);
        document.body.style.cursor = '';
        document.body.style.userSelect = '';
        // 列宽变化可能导致文本换行 → 行高变化 → 重新对齐水平线
        requestAnimationFrame(() => syncGanttRowHeights());
      };
      document.addEventListener('mousemove', onMouseMove as any);
      document.addEventListener('mouseup', onMouseUp as any);
    });
  });
}

function setupResize(handle: HTMLElement, leftPanel: HTMLElement): void {
  let startX = 0, startW = 0;

  handle.addEventListener('mousedown', (e) => {
    startX = e.clientX;
    startW = leftPanel.offsetWidth;
    document.body.style.cursor = 'col-resize';
    document.body.style.userSelect = 'none';
    handle.classList.add('active');

    const onMouseMove = (me: MouseEvent) => {
      const delta = me.clientX - startX;
      let newWidth = startW + delta;
      const minWidth = 280;
      const maxWidth = Math.max(minWidth + 100, window.innerWidth * 0.6);
      newWidth = Math.max(minWidth, Math.min(maxWidth, newWidth));
      leftPanel.style.width = `${newWidth}px`;
    };

    const onMouseUp = () => {
      document.removeEventListener('mousemove', onMouseMove);
      document.removeEventListener('mouseup', onMouseUp);
      document.body.style.cursor = '';
      document.body.style.userSelect = '';
      handle.classList.remove('active');
      // 面板宽度变化可能导致文本换行 → 行高变化 → 重新对齐水平线
      requestAnimationFrame(() => syncGanttRowHeights());
    };

    document.addEventListener('mousemove', onMouseMove);
    document.addEventListener('mouseup', onMouseUp);
  });
}

/**
 * 草稿箱操作（localStorage）
 */
export function loadDraft(key: string): string | null {
  try {
    return localStorage.getItem(`gantt_draft_${key}`);
  } catch {
    return null;
  }
}

export function saveDraft(key: string, json: string): void {
  try {
    localStorage.setItem(`gantt_draft_${key}`, json);
  } catch {
    // 存储满等情况忽略
  }
}

export function clearDraft(key: string): void {
  try {
    localStorage.removeItem(`gantt_draft_${key}`);
  } catch {
    // 忽略
  }
}
