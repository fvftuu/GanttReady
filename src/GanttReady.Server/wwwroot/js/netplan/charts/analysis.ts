// ============================================================
// charts/analysis.ts — Chart.js 分析柱状图
// ============================================================

/**
 * 渲染分析页面柱状图
 */
export function renderAnalysisBarChart(canvasId: string, chartData: any): void {
  // 注意: Blazor JS interop 传递的是已解析的对象，不是 JSON 字符串
  const canvas = document.getElementById(canvasId) as HTMLCanvasElement;
  if (!canvas) return;

  const ctx = canvas.getContext('2d');
  if (!ctx) return;

  // 销毁旧图表
  const oldCharts: Record<string, any> = (window as any)._analysisCharts || {};
  if (oldCharts[canvasId]) {
    oldCharts[canvasId].destroy();
  }

  const ChartCtor: any = (window as any).Chart;
  if (!ChartCtor) {
    console.warn('[AnalysisChart] Chart.js not loaded');
    return;
  }

  var datasets = (chartData.datasets || []).map((ds: any, i: number) => ({
    label: ds.label || `系列 ${i + 1}`,
    data: ds.data || [],
    backgroundColor: ds.backgroundColor || '#1890ff',
    borderColor: ds.borderColor || '#1890ff',
    borderWidth: 1,
    ...ds
  }));

  const chart: any = new ChartCtor(ctx, {
    type: 'bar',
    data: {
      labels: chartData.labels || [],
      datasets: datasets
    },
    options: {
      responsive: true,
      maintainAspectRatio: false,
      scales: {
        y: { beginAtZero: true }
      },
      plugins: {
        legend: { position: 'bottom' }
      }
    }
  });

  if (chart) {
    // 保存引用，Key 为 canvasId
    if (!(window as any)._analysisCharts) (window as any)._analysisCharts = {};
    (window as any)._analysisCharts[canvasId] = chart;
  }
}
