// ============================================================
// index.ts — 入口文件
// 将所有模块的功能挂载到 window 上
// ============================================================

import { calculateTimeParams, applySingleStartEnd } from './core/cpm.js';
import { calculateVerticalLayout } from './core/layout.js';

import { renderNetwork } from './render/network.js';
import { updateProgressColors } from './render/progress.js';

import { initGanttScroll, networkFit } from './interaction/panzoom.js';
import { downloadSvg, exportPng, printSvg } from './interaction/export.js';

import { setGanttDotNet, initPanelResize, loadDraft, saveDraft, clearDraft } from './gantt/binding.js';

import { initResourceChart } from './charts/resource.js';
import { renderAnalysisBarChart } from './charts/analysis.js';

import { getActiveProject, setActiveProject, navToProject,
         getCheckedProject, setCheckedProject, navToChecked } from './storage/project.js';

import type { WindowNetPlan } from './types.js';

const api: WindowNetPlan = {
  // 网络图
  setNetworkDotNet(ref: any) { (window as any)._netDotNet = ref; },
  setNetworkMode(mode: string) { (window as any)._netMode = mode; },
  setNetTimeScaleMode(mode: number) { (window as any)._netTimeScaleMode = mode; },
  clearNetworkOffsets() { (window as any)._netEventOffsets = {}; },
  renderNetwork,
  // 直接使用 renderNetwork（从 render/network.ts 导入）
  initProgressColors() {
    updateProgressColors([], null, new Date());
  },
  networkFit: () => networkFit(),

  // 导出
  downloadSVG: downloadSvg,
  exportNetworkPNG: exportPng,
  printNetwork: printSvg,

  // 甘特图
  setGanttDotNet,
  initGanttScroll,
  initPanelResize,
  loadDraft,
  saveDraft,
  clearDraft,

  // 图表
  initResourceChart,
  renderAnalysisBarChart,

  // 全局项目
  getActiveProject,
  setActiveProject,
  navToProject,
  getCheckedProject,
  setCheckedProject,
  navToChecked,

  // 内部
  _netInsertBlankRow(_layerNum: number) { /* TODO */ },
  _netDeleteBlankRow(_layerNum: number) { /* TODO */ },
};

for (const [key, fn] of Object.entries(api)) {
  (window as any)[key] = fn;
}

// 导出核心算法供测试
export { calculateTimeParams, applySingleStartEnd, calculateVerticalLayout };
