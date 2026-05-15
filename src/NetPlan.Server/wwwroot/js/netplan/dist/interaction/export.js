// ============================================================
// interaction/export.ts — SVG/PNG 导出及打印
// ============================================================
import { downloadSVGFile, exportSvgAsPng, getSvg } from '../utils/dom.js';
export function downloadSvg(filename = 'network.svg') {
    const svg = getSvg();
    downloadSVGFile(svg, filename);
}
export function exportPng(filename = 'network.png') {
    const svg = getSvg();
    exportSvgAsPng(svg, filename);
}
export function printSvg(printZoomVal = '100%') {
    const svg = getSvg();
    if (!svg)
        return;
    const zoom = parseFloat(printZoomVal) / 100;
    const origW = svg.getAttribute('width') || '';
    const origH = svg.getAttribute('height') || '';
    const w = parseFloat(origW) || 800;
    const h = parseFloat(origH) || 600;
    svg.setAttribute('width', String(w * zoom));
    svg.setAttribute('height', String(h * zoom));
    const content = new XMLSerializer().serializeToString(svg);
    const pw = window.open('', '_blank');
    if (pw) {
        pw.document.write(`<html><head><title>打印</title>
      <style>body{margin:0;display:flex;justify-content:center;align-items:center;min-height:100vh}</style>
      </head><body>${content}</body></html>`);
        pw.document.close();
        pw.focus();
        setTimeout(() => {
            pw.print();
            svg.setAttribute('width', origW);
            svg.setAttribute('height', origH);
        }, 500);
    }
}
