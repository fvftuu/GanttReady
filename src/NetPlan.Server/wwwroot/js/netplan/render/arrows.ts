// ============================================================
// render/arrows.ts — 箭线路径更新（节点拖拽后重绘）
// ============================================================

/**
 * 查找与事件关联的活动 ID
 */
export function findConnectedEvents(eventId: string): number[] {
  var ids: number[] = [];
  var acts: any[] = (window as any)._netActivities || [];
  acts.forEach(function(a: any) {
    if (a.source === eventId || a.target === eventId) ids.push(a.id);
  });
  return ids;
}

/**
 * 更新箭线路径（节点拖拽后实时刷新）
 */
export function updateArrowPaths(eventId: string, dx: number, dy: number): void {
  var ids = findConnectedEvents(eventId);
  var svg = (window as any)._netSvg as SVGSVGElement | null;
  var layout = (window as any)._netLayout;
  var offsets = (window as any)._netEventOffsets || {};
  if (!svg || !layout) return;

  ids.forEach(function(actId) {
    var g = svg!.querySelector('[data-activity-id="' + actId + '"]') as SVGGElement | null;
    if (!g) return;
    var sid = g.getAttribute('data-src');
    var tid = g.getAttribute('data-tgt');
    if (!sid || !tid) return;
    var s = layout.events[sid];
    var t = layout.events[tid];
    if (!s || !t) return;
    var nr = 11;
    var sx = s.x + (sid === eventId ? dx : (offsets[sid] ? offsets[sid].x : 0)) + nr;
    var sy = s.y + (sid === eventId ? dy : (offsets[sid] ? offsets[sid].y : 0));
    var ex = t.x + (tid === eventId ? dx : (offsets[tid] ? offsets[tid].x : 0));
    var ey = t.y + (tid === eventId ? dy : (offsets[tid] ? offsets[tid].y : 0));

    var pd: string;
    if (Math.abs(ey - sy) < 5) {
      pd = 'M' + sx + ' ' + sy + ' L' + (ex - 2) + ' ' + ey;
    } else {
      pd = 'M' + sx + ' ' + sy + ' L' + (ex - 2) + ' ' + sy + ' L' + (ex - 2) + ' ' + ey + ' L' + ex + ' ' + ey;
    }

    var p = g.querySelector('.act-arrow');
    if (p) p.setAttribute('d', pd);

    var lx = (sx + ex) / 2;
    var lb = g.querySelector('.act-label');
    if (lb) { lb.setAttribute('x', String(lx)); lb.setAttribute('y', String(sy - nr - 6)); }
    var du = g.querySelector('.act-dur');
    if (du) { du.setAttribute('x', String(lx)); du.setAttribute('y', String(sy + nr + 10)); }
  });
}

/**
 * 更新虚箭线路径（节点拖拽后实时刷新）
 */
export function updateDummyPaths(eventId: string, dx: number, dy: number): void {
  var svg = (window as any)._netSvg as SVGSVGElement | null;
  var layout = (window as any)._netLayout;
  var offsets = (window as any)._netEventOffsets || {};
  if (!svg || !layout) return;

  var dummies = svg.querySelectorAll('.dummy-rel');
  for (var i = 0; i < dummies.length; i++) {
    var g = dummies[i] as SVGGElement;
    var sid = g.getAttribute('data-dummy-src') || '';
    var tid = g.getAttribute('data-dummy-tgt') || '';

    var s = layout.events[sid];
    var t = layout.events[tid];
    if (!s || !t) continue;

    var nr = (window as any)._netNodeRadius || 11;
    var sx = (sid === eventId)
      ? (s.x + dx + nr)
      : (s.x + (offsets[sid] ? offsets[sid].x : 0) + nr);
    var sy = (sid === eventId)
      ? (s.y + dy)
      : (s.y + (offsets[sid] ? offsets[sid].y : 0));
    var ex = (tid === eventId)
      ? (t.x + dx)
      : (t.x + (offsets[tid] ? offsets[tid].x : 0));
    var ey = (tid === eventId)
      ? (t.y + dy)
      : (t.y + (offsets[tid] ? offsets[tid].y : 0));

    var pd: string;
    if (Math.abs(ey - sy) < 2) {
      pd = 'M' + sx + ' ' + sy + ' L' + ex + ' ' + ey;
    } else {
      var mx = (sx + ex) / 2;
      pd = 'M' + sx + ' ' + sy + ' L' + mx + ' ' + sy + ' L' + mx + ' ' + ey + ' L' + ex + ' ' + ey;
    }

    var p = g.querySelector('path');
    if (p) p.setAttribute('d', pd);
  }
}
