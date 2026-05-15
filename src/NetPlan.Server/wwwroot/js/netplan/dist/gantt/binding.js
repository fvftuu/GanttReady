// ============================================================
// gantt/binding.ts — 甘特图绑定（滚动同步 + 面板拖拽）
// ============================================================
let _ganttDotNet = null;
/**
 * 设置甘特图的 C# DotNet 引用
 */
export function setGanttDotNet(ref) {
    _ganttDotNet = ref;
}
/**
 * 获取甘特图 C# 引用
 */
export function getGanttDotNet() {
    return _ganttDotNet;
}
/**
 * 初始化面板分割拖拽
 */
export function initPanelResize() {
    const tryInit = (attempt = 1) => {
        if (attempt > 50)
            return;
        const handle = document.querySelector('.panel-resize-handle');
        const left = document.querySelector('.gantt-table-body-container');
        if (!handle || !left) {
            setTimeout(() => tryInit(attempt + 1), 100);
            return;
        }
        setupResize(handle, left);
    };
    tryInit();
}
function setupResize(handle, leftPanel) {
    let startX = 0;
    let minWidth = 200;
    let maxWidth = 800;
    handle.addEventListener('mousedown', (e) => {
        startX = e.clientX;
        minWidth = 200;
        maxWidth = window.innerWidth - 200;
        document.body.style.cursor = 'col-resize';
        document.body.style.userSelect = 'none';
        const onMouseMove = (me) => {
            const delta = me.clientX - startX;
            let newWidth = leftPanel.offsetWidth + delta;
            newWidth = Math.max(minWidth, Math.min(maxWidth, newWidth));
            leftPanel.style.width = `${newWidth}px`;
            startX = me.clientX;
        };
        const onMouseUp = () => {
            document.removeEventListener('mousemove', onMouseMove);
            document.removeEventListener('mouseup', onMouseUp);
            document.body.style.cursor = '';
            document.body.style.userSelect = '';
        };
        document.addEventListener('mousemove', onMouseMove);
        document.addEventListener('mouseup', onMouseUp);
    });
}
/**
 * 草稿箱操作（localStorage）
 */
export function loadDraft(key) {
    try {
        return localStorage.getItem(`gantt_draft_${key}`);
    }
    catch {
        return null;
    }
}
export function saveDraft(key, json) {
    try {
        localStorage.setItem(`gantt_draft_${key}`, json);
    }
    catch {
        // 存储满等情况忽略
    }
}
export function clearDraft(key) {
    try {
        localStorage.removeItem(`gantt_draft_${key}`);
    }
    catch {
        // 忽略
    }
}
