// ============================================================
// utils/geometry.ts — 几何计算工具
// 纯函数，可单元测试。
// ============================================================
/**
 * 检测并返回两条线段（AB vs CD）的交点
 * 无交点或平行时返回 null
 */
export function findSegIntersection(ax1, ay1, ax2, ay2, bx1, by1, bx2, by2) {
    const cross = (x1, y1, x2, y2) => x1 * y2 - y1 * x2;
    const c1 = cross(ax2 - ax1, ay2 - ay1, bx1 - ax1, by1 - ay1);
    const c2 = cross(ax2 - ax1, ay2 - ay1, bx2 - ax1, by2 - ay1);
    const c3 = cross(bx2 - bx1, by2 - by1, ax1 - bx1, ay1 - by1);
    const c4 = cross(bx2 - bx1, by2 - by1, ax2 - bx1, ay2 - by1);
    // 快速排斥：不相交
    if (c1 * c2 > 0 || c3 * c4 > 0)
        return null;
    const dxA = ax2 - ax1;
    const dxB = bx2 - bx1;
    const dyA = ay2 - ay1;
    const dyB = by2 - by1;
    const det = dxA * dyB - dyA * dxB;
    if (Math.abs(det) < 1e-10)
        return null; // 平行
    const t = ((bx1 - ax1) * dyB - (by1 - ay1) * dxB) / det;
    const u = ((bx1 - ax1) * dyA - (by1 - ay1) * dxA) / det;
    if (u < 0 || u > 1)
        return null;
    return {
        x: ax1 + t * dxA,
        y: ay1 + t * dyA,
        t
    };
}
/**
 * 两条直线段的交点（给过桥弧用）
 */
export function lineIntersection(a, b) {
    return findSegIntersection(a.x1, a.y1, a.x2, a.y2, b.x1, b.y1, b.x2, b.y2);
}
/**
 * 两点距离
 */
export function dist(x1, y1, x2, y2) {
    return Math.sqrt((x2 - x1) ** 2 + (y2 - y1) ** 2);
}
