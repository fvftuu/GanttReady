// ============================================================
// charts/analysis.ts — Chart.js 分析柱状图
// ============================================================
/**
 * 渲染分析页面柱状图
 */
export function renderAnalysisBarChart(canvasId, chartDataJson) {
    const data = JSON.parse(chartDataJson);
    const canvas = document.getElementById(canvasId);
    if (!canvas)
        return;
    const ctx = canvas.getContext('2d');
    if (!ctx)
        return;
    const chart = window.Chart ? new (window.Chart)(ctx, {
        type: 'bar',
        data: {
            labels: data.labels || [],
            datasets: (data.datasets || []).map((ds, i) => ({
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
        if (!window._analysisCharts)
            window._analysisCharts = {};
        window._analysisCharts[canvasId] = chart;
    }
}
