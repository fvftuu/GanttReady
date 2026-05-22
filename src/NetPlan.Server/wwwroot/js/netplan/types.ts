// ============================================================
// types.ts — 全局数据接口定义
// 所有模块共享的类型。改这里会影响所有模块，谨慎修改。
// ============================================================

export interface EventNode {
  id: number;
  es: number; ef: number; ls: number; lf: number;
  tf: number;
  isCritical: boolean;
  isVirtual?: boolean;
  x?: number; y?: number;
  layer?: number;
  offsetX?: number; offsetY?: number;
}

export interface ActivityEdge {
  id: number | string;
  srcId: number; tgtId: number;
  taskId: number;
  es: number; ef: number; dur: number;
  isCritical: boolean;
  tf: number;
  isDummy?: boolean;
  label?: string;
}

export interface LayoutResult {
  events: Record<number, EventNode>;
  activities: Record<number, ActivityEdge>;
  maxLayer: number;
}

export interface NetworkElement {
  eventId: number;
  es: number; ef: number; ls: number; lf: number;
  tf: number;
  isCritical: boolean;
  isVirtual?: boolean;
}

export interface TaskElement {
  taskId: number;
  name: string;
  es: number; ef: number; dur: number;
  isCritical: boolean;
  tf: number;
  srcEventId: number; tgtEventId: number;
  isDummy?: boolean;
  progressPct?: number;
}

export interface NetworkOpts {
  dayWidth: number;
  totalDays: number;
  mode: 'time' | 'logic';
  timeScaleMode: number;
  showCritical: boolean;
  showFloat: boolean;
  showTodayLine: boolean;
  showProgressLine: boolean;
  progressDate?: string;
  nodeRadius?: number;
  nodeShape?: 'circle' | 'rect';
  layerHeight?: number;
  fontSizes?: { label?: number; node?: number };
  colors?: {
    critical?: string;
    normal?: string;
    dummy?: string;
    weekend?: string;
  };
}

export interface NodeDragState {
  eid: number;
  startX: number; startY: number;
  origOffX: number; origOffY: number;
}

export interface SegLine {
  x1: number; y1: number; x2: number; y2: number;
}

export interface WindowNetPlan {
  renderNetwork(elementsJson: string, optsJson: string): void;
  setNetworkDotNet(ref: any): void;
  setNetworkMode(mode: string): void;
  setNetTimeScaleMode(mode: number): void;
  clearNetworkOffsets(): void;
  initProgressColors(): void;
  networkFit(): void;
  downloadSVG(filename: string): void;
  exportNetworkPNG(filename: string): void;
  printNetwork(printZoomVal: string): void;
  setGanttDotNet(ref: any): void;
  initGanttScroll(): void;
  syncGanttScrollById(): void;
  initPanelResize(): void;
  loadDraft(key: string): string | null;
  saveDraft(key: string, json: string): void;
  clearDraft(key: string): void;
  syncGanttRowHeights(): void;
  initResourceChart(chartData: string): void;
  renderAnalysisBarChart(canvasId: string, chartData: string): void;
  getActiveProject(): string | null;
  setActiveProject(id: string): void;
  navToProject(page: string, id: string): void;
  getCheckedProject(): string | null;
  setCheckedProject(id: string): void;
  navToChecked(page: string): void;
  _netInsertBlankRow(layerNum: number): void;
  _netDeleteBlankRow(layerNum: number): void;
}
