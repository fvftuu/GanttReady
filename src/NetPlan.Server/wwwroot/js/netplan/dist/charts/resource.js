// ============================================================
// charts/resource.ts — Chart.js 资源投入图
// ============================================================
/**
 * 初始化资源投入量柱状图
 */
export function initResourceChart(chartDataJson) {
    const data = JSON.parse(chartDataJson);
    const canvas = document.getElementById('resourceChart');
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
                label: ds.label || `资源 ${i + 1}`,
                data: ds.data || [],
                backgroundColor: ds.color || `hsl(${i * 60}, 60%, 70%)`,
                borderColor: ds.color || `hsl(${i * 60}, 60%, 50%)`,
                borderWidth: 1
            }))
        },
        options: {
            responsive: true,
            maintainAspectRatio: false,
            scales: {
                x: { stacked: true, title: { display: true, text: '时间' } },
                y: { stacked: true, beginAtZero: true, title: { display: true, text: '投入量' } }
            },
            plugins: {
                legend: { position: 'bottom' }
            }
        }
    }) : null;
    // 保存引用以便后续更新
    if (chart) {
        window._resourceChart = chart;
    }
}
