// ============================================================
// charts/resource.ts — Chart.js 资源投入图
// ============================================================

/**
 * 初始化资源投入量面积图（曲线+填充）
 */
export function initResourceChart(chartData: any): void {
  // 注意: Blazor JS interop 传递的是已解析的对象，不是 JSON 字符串
  const canvas = document.getElementById('resource-chart') as HTMLCanvasElement;
  if (!canvas) return;

  const ctx = canvas.getContext('2d');
  if (!ctx) return;

  // 销毁旧图表
  const oldChart: any = (window as any)._resourceChart;
  if (oldChart) {
    oldChart.destroy();
  }

  const ChartCtor: any = (window as any).Chart;
  if (!ChartCtor) {
    console.warn('[ResourceChart] Chart.js not loaded');
    return;
  }

  // 兼容两种数据格式：legacy {labels, lines} 和 Blazor 直传 {labels, datasets}
  var labels = chartData.labels || [];
  var lines: any[] = chartData.lines || [];
  var datasets: any[] = chartData.datasets || [];

  if (datasets.length === 0 && lines.length > 0) {
    // 兼容 legacy 格式（{labels, lines} → 转换为 datasets）
    datasets = lines.map(function(line: any, i: number) {
      return {
        label: line.name || `资源 ${i + 1}`,
        data: line.data || line.points || [],
        backgroundColor: line.color || `hsla(${i * 60}, 60%, 70%, 0.2)`,
        borderColor: line.color || `hsl(${i * 60}, 60%, 50%)`,
        borderWidth: 2,
        fill: true,
        tension: 0.3
      };
    });
  }

  const chart: any = new ChartCtor(ctx, {
    type: 'line',
    data: {
      labels: labels,
      datasets: datasets
    },
    options: {
      responsive: true,
      maintainAspectRatio: false,
      scales: {
        x: { stacked: false, title: { display: true, text: '时间' } },
        y: { stacked: false, beginAtZero: true, title: { display: true, text: '投入量' } }
      },
      plugins: {
        legend: { position: 'bottom' }
      }
    }
  });

  // 保存引用以便后续更新
  (window as any)._resourceChart = chart;
}
