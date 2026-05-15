// ============================================================
// charts/analysis.ts — Chart.js 分析柱状图
// ============================================================

/**
 * 渲染分析页面柱状图
 */
export function renderAnalysisBarChart(canvasId: string, chartDataJson: string): void {
  const data = JSON.parse(chartDataJson);
  const canvas = document.getElementById(canvasId) as HTMLCanvasElement;
  if (!canvas) return;

  const ctx = canvas.getContext('2d');
  if (!ctx) return;

  const chart: any = (window as any).Chart ? new ((window as any).Chart)(ctx, {
    type: 'bar',
    data: {
      labels: data.labels || [],
      datasets: (data.datasets || []).map((ds: any, i: number) => ({
        label: ds.label || `系列 ${i + 1}`,
        data: ds.data || [],
        backgroundColor: ds.backgroundColor || '#1890ff',
        borderColor: ds.borderColor || '#1890ff',
        borderWidth: 1,
        ...ds
      }))
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
  }) : null;

  if (chart) {
    // 保存引用，Key 为 canvasId
    if (!(window as any)._analysisCharts) (window as any)._analysisCharts = {};
    (window as any)._analysisCharts[canvasId] = chart;
  }
}
