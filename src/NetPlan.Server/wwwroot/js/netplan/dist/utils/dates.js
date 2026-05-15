// ============================================================
// utils/dates.ts — 日期工具函数
// 纯函数，无副作用，可单元测试。
// ============================================================
/**
 * 计算日期所在的 ISO 周数
 * 用于时标尺上显示周次
 */
export function getISOWeek(d) {
    const dayNum = (d.getDay() + 6) % 7; // 周一=0
    const yearStart = new Date(d.getFullYear(), 0, 1);
    const weekNum = Math.floor(((d.getTime() - yearStart.getTime()) / 86400000 - dayNum + 10) / 7);
    return weekNum;
}
/**
 * 判断日期是否为周末
 */
export function isWeekend(d) {
    const day = d.getDay();
    return day === 0 || day === 6;
}
/**
 * 日期偏移（天）
 */
export function addDays(d, days) {
    const result = new Date(d);
    result.setDate(result.getDate() + days);
    return result;
}
/**
 * 两个日期之间的天数差
 */
export function dayDiff(a, b) {
    return Math.round((a.getTime() - b.getTime()) / 86400000);
}
/**
 * 格式化日期为 yyyy-MM-dd
 */
export function formatDate(d) {
    const y = d.getFullYear();
    const m = String(d.getMonth() + 1).padStart(2, '0');
    const day = String(d.getDate()).padStart(2, '0');
    return `${y}-${m}-${day}`;
}
/**
 * 从 projectStartDate 偏移天数得到 Date
 */
export function offsetToDate(offset, projectStart) {
    const d = new Date(projectStart);
    d.setDate(d.getDate() + offset);
    return d;
}
