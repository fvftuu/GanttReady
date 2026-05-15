// ============================================================
// render/crossarc.ts — 过桥弧（跨线符）检测与绘制
// 从 legacy buildNetworkSvg 迁移 (L1324-L1477)
// ============================================================

/**
 * 两线段交点检测（精确版，排除端点接触）
 */
export function findSegIntersection(
  ax1: number, ay1: number, ax2: number, ay2: number,
  bx1: number, by1: number, bx2: number, by2: number
): { x: number; y: number } | null {
  var d = (ax2 - ax1) * (by2 - by1) - (ay2 - ay1) * (bx2 - bx1);
  if (Math.abs(d) < 1e-10) return null;
  var t = ((bx1 - ax1) * (by2 - by1) - (by1 - ay1) * (bx2 - bx1)) / d;
  var u = ((bx1 - ax1) * (ay2 - ay1) - (by1 - ay1) * (ax2 - ax1)) / d;
  if (t < 0 || t > 1 || u < 0 || u > 1) return null;
  return { x: ax1 + t * (ax2 - ax1), y: ay1 + t * (ay2 - ay1) };
}

interface Segment {
  x1: number; y1: number; x2: number; y2: number;
}

interface ActivitySegments {
  actId: string | number;
  isCritical: boolean;
  segs: Segment[];
  isDummy?: boolean;
}

/**
 * 交叉弧检测与生成
 * @param allSegments 所有活动的线段（含虚工作）
 * @param parts SVG 片段数组（push 跨线符）
 * @param isTimeMode 是否为时标模式（决定弧半径颜色）
 */
export function generateCrossArcs(
  allSegments: ActivitySegments[],
  parts: string[],
  _isTimeMode: boolean
): void {
  // 两两检测交叉,在非关键线上画跨线符
  for (var i = 0; i < allSegments.length; i++) {
    for (var j = i + 1; j < allSegments.length; j++) {
      var a = allSegments[i], b = allSegments[j];
      if (a.actId === b.actId) continue;
      for (var ai = 0; ai < a.segs.length; ai++) {
        for (var bi = 0; bi < b.segs.length; bi++) {
          var s1 = a.segs[ai], s2 = b.segs[bi];
          var cross = findSegIntersection(
            s1.x1, s1.y1, s1.x2, s1.y2,
            s2.x1, s2.y1, s2.x2, s2.y2
          );
          if (cross) {
            // 跨线符: 画在非关键线上(若两条均关键则画在A上)
            var arcOn: ActivitySegments;
            if (!a.isCritical && b.isCritical) { arcOn = a; }
            else if (a.isCritical && !b.isCritical) { arcOn = b; }
            else if (!a.isCritical && !b.isCritical) { arcOn = a; }
            else { arcOn = b; }
            if (!arcOn) arcOn = a;

            var bSeg: Segment;
            if (arcOn === a) { bSeg = s2; }
            else { bSeg = s1; }

            // 被跨箭线方向向量
            var bdx = bSeg.x2 - bSeg.x1, bdy = bSeg.y2 - bSeg.y1;
            var blen = Math.sqrt(bdx * bdx + bdy * bdy) || 1;
            // 垂直方向单位向量
            var nx = -bdy / blen, ny = bdx / blen;
            var arcR = 5;
            var lineW = arcOn.isCritical ? 3 : 1.5;

            // 白色底色弧（覆盖被跨线）
            parts.push(
              '<path d="M' + (cross.x - nx * 2) + ' ' + (cross.y - ny * 2) +
              ' A' + (arcR + 1) + ' ' + (arcR + 1) + ' 0 0 0 ' + (cross.x + nx * 2) + ' ' + (cross.y + ny * 2) +
              '" class="net-cross-arc" fill="none" stroke="#fff" stroke-width="' + (lineW + 2) + '" stroke-linecap="round"/>'
            );
            // 灰色边缘弧
            parts.push(
              '<path d="M' + (cross.x - nx * 2) + ' ' + (cross.y - ny * 2) +
              ' A' + (arcR + 1) + ' ' + (arcR + 1) + ' 0 0 0 ' + (cross.x + nx * 2) + ' ' + (cross.y + ny * 2) +
              '" class="net-cross-arc" fill="none" stroke="#999" stroke-width="1" stroke-linecap="round"/>'
            );
          }
        }
      }
    }
  }
}

/**
 * 构建线段数据（用于过桥弧检测）
 * @param layout 布局数据（包含 events）
 * @param activities 活动列表
 * @param relations 关系列表（虚工作）
 * @param isTimeMode 时标/逻辑模式
 * @param MARGIN_LEFT 左边界
 * @param NODE_R 节点半径
 */
export function collectAllSegments(
  layout: any,
  activities: any[],
  relations: any[],
  isTimeMode: boolean,
  NODE_R: number
): ActivitySegments[] {
  var allSegments: ActivitySegments[] = [];
  var events = layout.events;

  // 映射 source→es, target→ef
  var taskEsMap: Record<number, number> = {};
  var taskEfMap: Record<number, number> = {};
  activities.forEach(function(act: any) {
    taskEsMap[act.source] = act.es;
    taskEfMap[act.target] = act.ef;
  });

  // 收集实工作线段 (from buildNetworkSvg)
  activities.forEach(function(act: any) {
    var srcId = act.source, tgtId = act.target;
    var src = events[srcId], tgt = events[tgtId];
    if (!src || !tgt) return;
    var sx = (src.x || 0) + NODE_R, sy = src.y || 0;
    var ex = tgt.x || 0, ey = tgt.y || 0;
    var segs: Segment[] = [];
    if (isTimeMode) {
      if (Math.abs(ey - sy) < 2) {
        segs = [{ x1: sx, y1: sy, x2: ex, y2: ey }];
      } else {
        segs = [
          { x1: sx, y1: sy, x2: ex - 2, y2: sy },
          { x1: ex - 2, y1: sy, x2: ex - 2, y2: ey },
          { x1: ex - 2, y1: ey, x2: ex, y2: ey }
        ];
      }
    } else {
      segs = [{ x1: sx, y1: sy, x2: ex, y2: ey }];
    }
    allSegments.push({
      actId: act.id,
      isCritical: act.isCritical || false,
      segs: segs
    });
  });

  // 添加虚工作线段
  relations.forEach(function(rel: any) {
    var predEf = taskEfMap[rel.source];
    var succEs = taskEsMap[rel.target];
    if (predEf === undefined || succEs === undefined) return;
    var srcId = 'T' + predEf, tgtId = 'T' + succEs;
    if (srcId === tgtId) return;
    var src = events[srcId], tgt = events[tgtId];
    if (!src || !tgt) return;
    var sx = (src.x || 0) + NODE_R, sy = src.y || 0;
    var ex = tgt.x || 0, ey = tgt.y || 0;
    var dseg: Segment[];
    if (Math.abs(ey - sy) < 2) {
      dseg = [{ x1: sx, y1: sy, x2: ex, y2: ey }];
    } else {
      dseg = [
        { x1: sx, y1: sy, x2: sx, y2: ey },
        { x1: sx, y1: ey, x2: ex, y2: ey }
      ];
    }
    allSegments.push({ actId: 'dummy-' + rel.source + '-' + rel.target, isCritical: false, segs: dseg, isDummy: true });
  });

  return allSegments;
}
