// ============================================================
// interaction/nodedrag.ts — 节点拖拽、偏移弹窗
// ============================================================
import { updateCrossArcOverlays } from '../render/crossarc.js';
let _dayPopup = null;
export function showDayPopup(deltaDays, cx, cy, onConfirm) {
    _hideDayPopup();
    const popup = document.createElement('div');
    popup.className = 'day-popup';
    popup.style.cssText = `
    position: fixed; z-index: 10000;
    left: ${cx}px; top: ${Math.max(10, cy)}px;
    background: #fff; border: 1px solid #d9d9d9;
    border-radius: 6px; box-shadow: 0 2px 8px rgba(0,0,0,0.15);
    padding: 12px; min-width: 160px;
  `;
    const label = document.createElement('div');
    label.textContent = `偏移天数 (当前: ${deltaDays >= 0 ? '+' : ''}${deltaDays})`;
    label.style.cssText = 'font-size: 12px; color: #666; margin-bottom: 8px;';
    popup.appendChild(label);
    const input = document.createElement('input');
    input.type = 'number';
    input.value = String(deltaDays);
    input.style.cssText = 'width: 120px; margin-bottom: 8px; display: block;';
    popup.appendChild(input);
    const btnGroup = document.createElement('div');
    btnGroup.style.cssText = 'display: flex; gap: 8px; justify-content: flex-end;';
    const cancelBtn = document.createElement('button');
    cancelBtn.textContent = '取消';
    cancelBtn.onclick = () => _hideDayPopup();
    btnGroup.appendChild(cancelBtn);
    const confirmBtn = document.createElement('button');
    confirmBtn.textContent = '确定';
    confirmBtn.style.cssText = 'background: #1890ff; color: #fff; border: none; padding: 4px 12px; border-radius: 4px;';
    confirmBtn.onclick = () => {
        const val = parseInt(input.value) || 0;
        _hideDayPopup();
        onConfirm(val);
    };
    btnGroup.appendChild(confirmBtn);
    popup.appendChild(btnGroup);
    document.body.appendChild(popup);
    _dayPopup = popup;
    input.focus();
}
export function hideDayPopup() {
    _hideDayPopup();
}
function _hideDayPopup() {
    if (_dayPopup) {
        document.body.removeChild(_dayPopup);
        _dayPopup = null;
    }
}
export function getDragEventData(eid, events) {
    const evt = events.find(e => e.id === eid);
    if (!evt || evt.isVirtual)
        return null;
    return { evt, tid: 0 };
}
/**
 * 更新过桥弧（节点拖拽后）
 */
export function updateCrossArcs() {
    updateCrossArcOverlays();
}
