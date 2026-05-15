// ============================================================
// core/layout.ts — 拓扑分层与事件定位
// 包含: calculateVerticalLayout 及内部辅助函数
// ============================================================

/**
 * 垂直布局: 按 BFS 拓扑分层，事件同行合并，最终确定每个事件的 y 坐标
 * 规则(双代号):
 * 1. 每个活动的 source 和 target 事件必须在同一行
 * 2. 有共同前驱/后继的事件放在同一行
 * 3. 关键路径事件优先居中
 */
export function calculateVerticalLayout(data: any): any {
  var events = data.events;
  var activities = data.activities;
  var eventPred = data.eventPred;
  var eventSucc = data.eventSucc;

  var LAYER_HEIGHT = 60;
  var MARGIN_TOP = 100;

  // ===== 双代号同行算法 =====
  // 每条活动的 source 和 target 事件必须在同一行
  var sameRow: Record<string, string[]> = {};

  // 连通域标记: 通过活动连接的事件属于同一行
  function union(rowKey: string, eid: string) {
    if (!sameRow[rowKey]) sameRow[rowKey] = [];
    if (sameRow[rowKey].indexOf(eid) === -1) sameRow[rowKey].push(eid);
  }

  // 事件按 ES 分组
  var esGroup: Record<number, string[]> = {};
  Object.keys(events).forEach(function(eid) {
    var es = events[eid].es || 0;
    if (!esGroup[es]) esGroup[es] = [];
    esGroup[es].push(eid);
  });

  // 同 ES 的事件放同一行
  Object.keys(esGroup).forEach(function(es) {
    var rowKey = 'ES_' + es;
    esGroup[Number(es)].forEach(function(eid) { union(rowKey, eid); });
  });

  // 同一活动的前后事件放同一行
  activities.forEach(function(act: any) {
    var rowKey = 'act_' + act.source + '_' + act.target;
    union(rowKey, act.source);
    union(rowKey, act.target);
  });

  // ===== 计算分层 =====
  // 基于拓扑顺序 + 父层
  var layers: Record<string, number> = {};

  // BFS 从起点开始
  var visited: Record<string, boolean> = {};
  var queue: string[] = [];
  var degrees: Record<string, number> = {};
  // 安全访问（测试数据可能不传 eventPred/eventSucc）
  var pred = eventPred || {} as Record<string, string[]>;
  var succ = eventSucc || {} as Record<string, string[]>;

  Object.keys(events).forEach(function(eid) {
    degrees[eid] = pred[eid] ? pred[eid].length : 0;
  });

  Object.keys(degrees).forEach(function(eid) {
    if (degrees[eid] === 0) {
      queue.push(eid);
      layers[eid] = 0;
      visited[eid] = true;
    }
  });

  while (queue.length > 0) {
    var cur = queue.shift()!;
    var curLayer = layers[cur] || 0;

    if (succ[cur]) {
      succ[cur].forEach(function(next: string) {
        if (!visited[next]) {
          degrees[next]--;
          if (degrees[next] === 0) {
            // 找同行里最大的层数
            var maxLayer = curLayer;
            Object.keys(sameRow).forEach(function(rk) {
              if (sameRow[rk].indexOf(next) !== -1) {
                sameRow[rk].forEach(function(sib) {
                  if (layers[sib] !== undefined && layers[sib] > maxLayer) maxLayer = layers[sib];
                });
              }
            });
            layers[next] = maxLayer + 1;
            queue.push(next);
            visited[next] = true;
          }
        }
      });
    }
  }

  // ===== 关键路径水平居中 =====
  var maxLayer = 0;
  Object.keys(layers).forEach(function(eid) {
    if (layers[eid] > maxLayer) maxLayer = layers[eid];
  });

  // 找关键事件
  var critEvents: string[] = [];
  Object.keys(events).forEach(function(eid) {
    if (events[eid].isCritical) critEvents.push(eid);
  });

  // 关键路径事件尽量放在中间区域
  var halfLayer = Math.floor(maxLayer / 2);
  Object.keys(layers).forEach(function(eid) {
    if (critEvents.indexOf(eid) !== -1) {
      var mid = halfLayer;
      if (layers[eid] < mid) layers[eid] = Math.min(mid, layers[eid] + Math.floor(mid / 2));
    }
  });

  // ===== 生成布局 =====
  var layout: Record<string, any> = {};
  Object.keys(events).forEach(function(eid) {
    var layer = layers[eid] || 0;
    events[eid].layer = layer;
    events[eid].y = MARGIN_TOP + layer * LAYER_HEIGHT;
    layout[eid] = { layer: layer, y: MARGIN_TOP + layer * LAYER_HEIGHT, num: events[eid].num };
  });

  return {
    events: events,
    activities: activities,
    layout: layout,
    maxLayer: maxLayer
  };
}
