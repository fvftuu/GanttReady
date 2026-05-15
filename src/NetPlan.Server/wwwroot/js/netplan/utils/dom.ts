// ============================================================
// utils/dom.ts — DOM/SVG 辅助函数
// 负责创建 SVG 元素、处理下载等纯 DOM 操作。
// ============================================================

let _svgArrowIdCounter = 0;

const SVG_NS = 'http://www.w3.org/2000/svg';

/** 创建 SVG 元素 */
export function svgEl(tag: string, attrs: Record<string, string | number> = {}): SVGElement {
  const el = document.createElementNS(SVG_NS, tag);
  for (const [k, v] of Object.entries(attrs)) {
    el.setAttribute(k, String(v));
  }
  return el;
}

/** 创建 SVG defs 中的箭头标记 */
export function svgArrowMarker(
  crit: boolean,
  optCritColor?: string,
  optNormalColor?: string
): string {
  const critColor = optCritColor || '#ff4d4f';
  const normalColor = optNormalColor || '#1890ff';
  const color = crit ? critColor : normalColor;
  const id = `arr-${crit ? 'crit' : 'norm'}-${++_svgArrowIdCounter}`;

  // 查找或创建 defs
  let defs = document.querySelector('svg defs');
  if (!defs) {
    const svg = document.querySelector('svg');
    if (svg) {
      defs = svgEl('defs');
      svg.insertBefore(defs, svg.firstChild);
    }
  }
  if (defs) {
    defs.appendChild(svgEl('marker', {
      id,
      viewBox: '0 0 10 10',
      refX: 10,
      refY: 5,
      markerWidth: 8,
      markerHeight: 8,
      orient: 'auto'
    }));
    const marker = defs.lastElementChild!;
    marker.appendChild(svgEl('path', {
      d: 'M 0 0 L 10 5 L 0 10 Z',
      fill: color
    }));
  }
  return `url(#${id})`;
}

/** 下载 SVG 内容为文件 */
export function downloadSVGFile(svgEl: SVGSVGElement | null, filename: string): void {
  if (!svgEl) return;
  const content = new XMLSerializer().serializeToString(svgEl);
  const blob = new Blob([content], { type: 'image/svg+xml;charset=utf-8' });
  const url = URL.createObjectURL(blob);
  triggerDownload(url, filename);
  URL.revokeObjectURL(url);
}

/** 将 SVG 导出为 PNG */
export function exportSvgAsPng(
  svgEl: SVGSVGElement | null,
  filename: string,
  scale: number = 2
): void {
  if (!svgEl) return;
  const svgData = new XMLSerializer().serializeToString(svgEl);
  const svgW = svgEl.getAttribute('width') || '800';
  const svgH = svgEl.getAttribute('height') || '600';
  const blob = new Blob([svgData], { type: 'image/svg+xml;charset=utf-8' });
  const url = URL.createObjectURL(blob);
  const canvas = document.createElement('canvas');
  canvas.width = parseFloat(String(svgW)) * scale;
  canvas.height = parseFloat(String(svgH)) * scale;
  const ctx = canvas.getContext('2d');
  const img = new Image();
  img.onload = () => {
    ctx?.drawImage(img, 0, 0, canvas.width, canvas.height);
    canvas.toBlob((pngBlob) => {
      if (pngBlob) {
        const pngUrl = URL.createObjectURL(pngBlob);
        triggerDownload(pngUrl, filename);
        URL.revokeObjectURL(pngUrl);
      }
    }, 'image/png');
    URL.revokeObjectURL(url);
  };
  img.src = url;
}

/** 触发文件下载 */
export function triggerDownload(url: string, filename: string): void {
  const a = document.createElement('a');
  a.href = url;
  a.download = filename;
  document.body.appendChild(a);
  a.click();
  document.body.removeChild(a);
}

/** 获取 SVG 元素，按 ID 或 querySelector */
export function getSvg(containerId?: string): SVGSVGElement | null {
  if (containerId) {
    return document.querySelector(`#${containerId} svg`) as SVGSVGElement | null;
  }
  return document.querySelector('svg.network-svg') as SVGSVGElement | null;
}

/** 获取或创建 dummmy SVG group */
export function getOrCreateGroup(parent: SVGElement, id: string): SVGElement {
  let g = parent.querySelector(`#${id}`);
  if (!g) {
    g = svgEl('g', { id });
    parent.appendChild(g);
  }
  return g as SVGElement;
}

/** 清除子元素 */
export function clearChildren(el: Element): void {
  while (el.firstChild) {
    el.removeChild(el.firstChild);
  }
}
