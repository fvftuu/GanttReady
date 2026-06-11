// ============================================================
// core/layout.ts — 垂直布局 v4
// 策略：顶层向下分配 + 同ES分散 + 活动同行约束
// ============================================================
/**
 * 垂直布局：
 * 1. ES 相同的事件按顺序从上到下分配子行（不从中间开始）
 * 2. 同一活动的 source 和 target 必须在同一行
 * 3. 同源多目标的出度节点放到不同子行
 */
export function calculateVerticalLayout(data) {
    var events = data.events;
    var activities = data.activities;
    var LAYER_HEIGHT = 60;
    var MARGIN_TOP = 100;
    // ===== 1. 收集事件 =====
    var eids = Object.keys(events);
    // 活动绑定
    var actPairs = [];
    activities.forEach(function (act) {
        if (act.source && act.target && act.source !== act.target) {
            actPairs.push({ src: act.source, tgt: act.target });
        }
    });
    // ===== 2. 按 ES 收集唯一值 =====
    var esValues = [];
    eids.forEach(function (eid) {
        var es = events[eid].es || 0;
        if (esValues.indexOf(es) === -1)
            esValues.push(es);
    });
    esValues.sort(function (a, b) { return a - b; });
    // ES → baseLayer（小的在上，大的在下）
    var esToBase = {};
    esValues.forEach(function (es, idx) { esToBase[es] = idx; });
    // 每层的事件
    var layerEvents = {};
    eids.forEach(function (eid) {
        var bl = esToBase[events[eid].es || 0];
        if (!layerEvents[bl])
            layerEvents[bl] = [];
        if (layerEvents[bl].indexOf(eid) === -1)
            layerEvents[bl].push(eid);
    });
    // ===== 3. 活动同行约束：仅当 src 和 tgt 在同一 ES 值时强制同行 =====
    // 不同 ES 值的活动不会触发层合并（它们是真实的搭接关系）
    var eventBase = {};
    eids.forEach(function (eid) { eventBase[eid] = esToBase[events[eid].es || 0]; });
    // 只对 src/tgt 在同一 ES 的活动做同行约束
    actPairs.forEach(function (p) {
        var srcEs = events[p.src] ? events[p.src].es || 0 : 0;
        var tgtEs = events[p.tgt] ? events[p.tgt].es || 0 : 0;
        if (srcEs !== tgtEs)
            return; // 不同 ES 不合并层
        var sb = eventBase[p.src], tb = eventBase[p.tgt];
        if (sb !== tb) {
            // 统一到较小的层
            var minB = Math.min(sb, tb);
            eventBase[p.src] = minB;
            eventBase[p.tgt] = minB;
            [sb, tb].forEach(function (b) {
                if (b === minB)
                    return;
                var arr = layerEvents[b] || [];
                var idx = arr.indexOf(p.src);
                if (idx !== -1)
                    arr.splice(idx, 1);
                idx = arr.indexOf(p.tgt);
                if (idx !== -1)
                    arr.splice(idx, 1);
            });
            var tgtArr = layerEvents[minB] || [];
            if (tgtArr.indexOf(p.src) === -1)
                tgtArr.push(p.src);
            if (tgtArr.indexOf(p.tgt) === -1)
                tgtArr.push(p.tgt);
            layerEvents[minB] = tgtArr;
        }
    });
    // ===== 4. 每大层内分配子行（顶层开始，自上而下分配） =====
    var eventSub = {};
    var subCounts = {};
    Object.keys(layerEvents).forEach(function (blKey) {
        var bl = parseInt(blKey);
        var evts = (layerEvents[bl] || []).slice();
        var seen = {};
        var uq = [];
        evts.forEach(function (e) { if (!seen[e]) {
            seen[e] = true;
            uq.push(e);
        } });
        // 并查集：同一活动的 src/tgt 必须在同一子行
        var parent = {};
        uq.forEach(function (e) { parent[e] = e; });
        function find(x) {
            if (parent[x] !== x)
                parent[x] = find(parent[x]);
            return parent[x];
        }
        function union(a, b) {
            var ra = find(a), rb = find(b);
            if (ra !== rb)
                parent[rb] = ra;
        }
        actPairs.forEach(function (p) {
            var srcEs = events[p.src] ? events[p.src].es || 0 : 0;
            var tgtEs = events[p.tgt] ? events[p.tgt].es || 0 : 0;
            if (srcEs === tgtEs && uq.indexOf(p.src) !== -1 && uq.indexOf(p.tgt) !== -1) {
                union(p.src, p.tgt);
            }
        });
        // 分组
        var groups = {};
        uq.forEach(function (e) {
            var r = find(e);
            if (!groups[r])
                groups[r] = [];
            groups[r].push(e);
        });
        var groupList = Object.keys(groups).map(function (r) {
            return { root: r, events: groups[r] };
        });
        // 关键事件组优先排在顶层
        groupList.sort(function (a, b) {
            var aHasCrit = a.events.some(function (e) { return events[e].isCritical; });
            var bHasCrit = b.events.some(function (e) { return events[e].isCritical; });
            if (aHasCrit && !bHasCrit)
                return -1;
            if (!aHasCrit && bHasCrit)
                return 1;
            return 0;
        });
        // 分配子行：0,1,2,... 从顶层开始（关键组优先）
        var subIdx = 0;
        groupList.forEach(function (g) {
            g.events.forEach(function (e) { eventSub[e] = subIdx; });
            subIdx++;
        });
        subCounts[bl] = Math.max(1, subIdx);
    });
    // 确保所有事件都有值
    eids.forEach(function (eid) {
        if (eventSub[eid] === undefined)
            eventSub[eid] = 0;
        if (eventBase[eid] === undefined)
            eventBase[eid] = 0;
    });
    // ===== 5. 出度分散：同源多目标强制分配到不同子行 =====
    var srcOutTargets = {};
    activities.forEach(function (act) {
        var src = act.source;
        if (!srcOutTargets[src])
            srcOutTargets[src] = [];
        if (srcOutTargets[src].indexOf(act.target) === -1)
            srcOutTargets[src].push(act.target);
    });
    Object.keys(srcOutTargets).forEach(function (src) {
        var targets = srcOutTargets[src];
        if (targets.length < 2)
            return;
        var firstSub = eventSub[targets[0]];
        var allSame = targets.every(function (t) { return eventSub[t] === firstSub; });
        if (!allSame)
            return;
        var srcBL = eventBase[src];
        var curSubCount = subCounts[srcBL] || 1;
        targets.forEach(function (tgt, idx) {
            if (idx === 0) {
                eventSub[tgt] = firstSub;
                return;
            }
            var newSub = curSubCount + idx - 1;
            eventSub[tgt] = newSub;
            // 修正：仅对同一 ES 的 tgt 做同行约束
            actPairs.forEach(function (p) {
                if (p.src === tgt || p.tgt === tgt) {
                    var other = p.src === tgt ? p.tgt : p.src;
                    var oEs = events[other] ? events[other].es || 0 : 0;
                    var tEs = events[tgt] ? events[tgt].es || 0 : 0;
                    if (oEs === tEs) {
                        if (p.src === tgt && eventSub[p.tgt] !== newSub)
                            eventSub[p.tgt] = newSub;
                        if (p.tgt === tgt && eventSub[p.src] !== newSub)
                            eventSub[p.src] = newSub;
                    }
                }
            });
        });
        subCounts[srcBL] = Math.max(subCounts[srcBL] || 1, curSubCount + targets.length - 1);
    });
    // 所有 baseLayer 确保 subCounts 至少为 1
    Object.keys(layerEvents).forEach(function (blKey) {
        var bl = parseInt(blKey);
        if (!subCounts[bl])
            subCounts[bl] = 1;
    });
    // ===== 6. 计算绝对层（累积偏移） =====
    var sortedBL = Object.keys(subCounts).map(Number).sort(function (a, b) { return a - b; });
    var cumoff = { 0: 0 };
    var cumulative = 0;
    sortedBL.forEach(function (bl) { cumoff[bl] = cumulative; cumulative += subCounts[bl] || 1; });
    var maxLayer = cumulative;
    var eventAbsLayer = {};
    eids.forEach(function (eid) {
        var bl = eventBase[eid] || 0;
        var sl = eventSub[eid] || 0;
        var abs = (cumoff[bl] || 0) + sl;
        eventAbsLayer[eid] = abs;
        events[eid].layer = abs;
        events[eid].y = MARGIN_TOP + abs * LAYER_HEIGHT;
    });
    // ===== 7. 输出（不居中拉伸） =====
    var layout = {};
    eids.forEach(function (eid) {
        var ly = eventAbsLayer[eid] || 0;
        layout[eid] = { layer: ly, y: MARGIN_TOP + ly * LAYER_HEIGHT, num: events[eid].num };
    });
    return { events: events, activities: activities, layout: layout, maxLayer: maxLayer };
}
