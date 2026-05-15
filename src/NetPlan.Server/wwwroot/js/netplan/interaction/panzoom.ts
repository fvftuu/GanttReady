// ============================================================
// interaction/panzoom.ts — 缩放、平移、滚动同步
// ============================================================

let _syncLock = false;

export function initGanttScroll(): void {
  const tryInit = (attempt = 1): void => {
    if (attempt > 50) return;
    const rightDiv = document.querySelector('.gantt-timeline') as HTMLElement;
    const leftBody = document.querySelector('.gantt-table-body') as HTMLElement;
    if (!rightDiv || !leftBody) {
      setTimeout(() => tryInit(attempt + 1), 100);
      return;
    }

    rightDiv.addEventListener('scroll', () => {
      if (_syncLock) return;
      _syncLock = true;
      leftBody.scrollTop = rightDiv.scrollTop;
      _syncLock = false;
    });

    leftBody.addEventListener('scroll', () => {
      if (_syncLock) return;
      _syncLock = true;
      rightDiv.scrollTop = leftBody.scrollTop;
      _syncLock = false;
    });
  };
  tryInit();
}

let _panState: { startX: number; startY: number; startScrollLeft: number; startScrollTop: number } | null = null;

export function initNetworkPan(container: HTMLElement): void {
  container.addEventListener('mousedown', (e) => {
    const target = e.target as HTMLElement;
    if (target.tagName === 'svg' || target.classList.contains('network-body')) {
      _panState = {
        startX: e.clientX, startY: e.clientY,
        startScrollLeft: container.scrollLeft,
        startScrollTop: container.scrollTop
      };
    }
  });

  document.addEventListener('mousemove', (e) => {
    if (!_panState) return;
    const dx = e.clientX - _panState.startX;
    const dy = e.clientY - _panState.startY;
    container.scrollLeft = _panState.startScrollLeft - dx;
    container.scrollTop = _panState.startScrollTop - dy;
  });

  document.addEventListener('mouseup', () => { _panState = null; });
}

export function networkFit(containerId: string = 'network-body'): void {
  const body = document.getElementById(containerId) as HTMLElement;
  const svg = body?.querySelector('svg') as SVGSVGElement;
  if (!body || !svg) return;

  const vw = body.clientWidth;
  const sw = parseFloat(svg.getAttribute('width') || '800');
  if (sw > 0) {
    const scale = Math.min(vw / sw, 1);
    body.style.zoom = String(scale * 100) + '%';
  }
}
