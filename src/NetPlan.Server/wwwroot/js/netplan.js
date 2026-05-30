"use strict";
var NetPlan = (() => {
  var __defProp = Object.defineProperty;
  var __getOwnPropDesc = Object.getOwnPropertyDescriptor;
  var __getOwnPropNames = Object.getOwnPropertyNames;
  var __hasOwnProp = Object.prototype.hasOwnProperty;
  var __export = (target, all) => {
    for (var name in all)
      __defProp(target, name, { get: all[name], enumerable: true });
  };
  var __copyProps = (to, from, except, desc) => {
    if (from && typeof from === "object" || typeof from === "function") {
      for (let key of __getOwnPropNames(from))
        if (!__hasOwnProp.call(to, key) && key !== except)
          __defProp(to, key, { get: () => from[key], enumerable: !(desc = __getOwnPropDesc(from, key)) || desc.enumerable });
    }
    return to;
  };
  var __toCommonJS = (mod) => __copyProps(__defProp({}, "__esModule", { value: true }), mod);

  // netplan/index.ts
  var index_exports = {};
  __export(index_exports, {
    applySingleStartEnd: () => applySingleStartEnd,
    calculateTimeParams: () => calculateTimeParams,
    calculateVerticalLayout: () => calculateVerticalLayout
  });

  // netplan/core/cpm.ts
  function calculateTimeParams(tasks, relations) {
    var starts = {};
    var ends = {};
    var taskEs = {};
    var taskEf = {};
    tasks.forEach(function(task) {
      var es = task.es || 0;
      var ef = task.ef || es + (task.duration || 0);
      taskEs[task.id] = es;
      taskEf[task.id] = ef;
      if (!starts[es]) starts[es] = [];
      if (starts[es].indexOf(task.id) === -1) starts[es].push(task.id);
      if (!ends[ef]) ends[ef] = [];
      if (ends[ef].indexOf(task.id) === -1) ends[ef].push(task.id);
    });
    var events = {};
    var allTimeOffsets = [];
    Object.keys(starts).forEach(function(t) {
      if (allTimeOffsets.indexOf(t) === -1) allTimeOffsets.push(t);
    });
    Object.keys(ends).forEach(function(t) {
      if (allTimeOffsets.indexOf(t) === -1) allTimeOffsets.push(t);
    });
    allTimeOffsets.sort(function(a, b) {
      return parseInt(a) - parseInt(b);
    });
    allTimeOffsets.forEach(function(timeOffset) {
      var eid2 = "T" + timeOffset;
      var isStart = starts[timeOffset] !== void 0;
      var isEnd = ends[timeOffset] !== void 0;
      var type = isStart && isEnd ? "both" : isStart ? "start" : "end";
      var associated = [];
      var sArr = starts[timeOffset] || [];
      var eArr = ends[timeOffset] || [];
      var both = sArr.concat(eArr);
      both.forEach(function(tid) {
        if (associated.indexOf(tid) === -1) associated.push(tid);
      });
      var taskId = parseInt(String(associated[0])) || 0;
      var isCritical = false;
      var tf = 0;
      tasks.forEach(function(t) {
        if (associated.indexOf(t.id) !== -1) {
          if (t.isCritical) isCritical = true;
          if ((t.tf || 0) > tf) tf = t.tf || 0;
        }
      });
      events[eid2] = {
        id: eid2,
        taskId,
        type,
        num: 0,
        es: parseInt(timeOffset),
        ef: parseInt(timeOffset),
        ls: 0,
        lf: 0,
        tf,
        ff: 0,
        isCritical
      };
    });
    var activities = [];
    tasks.forEach(function(task) {
      var es = taskEs[task.id];
      var ef = taskEf[task.id];
      activities.push({
        id: task.id,
        source: "T" + es,
        target: "T" + ef,
        es,
        ef,
        ls: task.ls || task.lateStart || es,
        lf: task.lf || task.lateFinish || ef,
        duration: task.duration || 0,
        code: task.label || "",
        name: task.name || "",
        isCritical: task.isCritical || false,
        tf: task.tf || 0,
        ff: task.ff || 0,
        completion: task.completion || 0
      });
    });
    var eventPred = {};
    var eventSucc = {};
    Object.keys(events).forEach(function(eid2) {
      eventPred[eid2] = [];
      eventSucc[eid2] = [];
    });
    function addEdge(f, t) {
      if (f === t) return;
      if (!events[f] || !events[t]) return;
      if (eventSucc[f].indexOf(t) === -1) eventSucc[f].push(t);
      if (eventPred[t].indexOf(f) === -1) eventPred[t].push(f);
    }
    activities.forEach(function(act) {
      addEdge(act.source, act.target);
    });
    relations.forEach(function(rel) {
      var predEf = taskEf[rel.source];
      var succEs = taskEs[rel.target];
      if (predEf === void 0 || succEs === void 0) return;
      addEdge("T" + predEf, "T" + succEs);
    });
    var inDeg = {};
    Object.keys(events).forEach(function(eid2) {
      inDeg[eid2] = eventPred[eid2].length;
    });
    var queue = [];
    Object.keys(inDeg).forEach(function(eid2) {
      if (inDeg[eid2] === 0) queue.push(eid2);
    });
    var sortedEvents = [];
    var nextNum = 1;
    while (queue.length > 0) {
      var eid = queue.shift();
      events[eid].num = nextNum;
      nextNum++;
      sortedEvents.push(eid);
      eventSucc[eid].forEach(function(neighbor) {
        inDeg[neighbor]--;
        if (inDeg[neighbor] === 0) queue.push(neighbor);
      });
    }
    var remaining = Object.keys(events).filter(function(eid2) {
      return events[eid2].num === 0;
    });
    remaining.sort(function(a, b) {
      return (events[a].es || 0) - (events[b].es || 0);
    });
    remaining.forEach(function(eid2) {
      events[eid2].num = nextNum;
      nextNum++;
      sortedEvents.push(eid2);
    });
    return {
      events,
      activities,
      relations,
      eventPred,
      eventSucc,
      sortedEvents
    };
  }
  function applySingleStartEnd(data) {
    var events = data.events;
    var activities = data.activities;
    var eventPred = data.eventPred;
    var eventSucc = data.eventSucc;
    var sortedEvents = data.sortedEvents;
    var maxEf = -Infinity;
    Object.keys(events).forEach(function(eid) {
      if (events[eid].ef > maxEf) maxEf = events[eid].ef;
    });
    var minEs = Infinity;
    Object.keys(events).forEach(function(eid) {
      if (events[eid].es < minEs) minEs = events[eid].es;
    });
    var startCandidates = [];
    var endCandidates = [];
    Object.keys(events).forEach(function(eid) {
      var evt = events[eid];
      if (evt.isVirtual) return;
      if (evt.es === minEs) startCandidates.push(eid);
      if (evt.ef === maxEf) endCandidates.push(eid);
    });
    if (startCandidates.length <= 1 && endCandidates.length <= 1) return data;
    if (startCandidates.length > 1) {
      var sid = "TS";
      events[sid] = {
        id: sid,
        taskId: 0,
        type: "start",
        num: 0,
        es: minEs - 5,
        ef: minEs - 5,
        ls: 0,
        lf: 0,
        tf: 0,
        ff: 0,
        isCritical: false,
        isVirtual: true
      };
      eventPred[sid] = [];
      eventSucc[sid] = startCandidates.slice();
      startCandidates.forEach(function(seid) {
        eventPred[seid].push(sid);
      });
      sortedEvents.unshift(sid);
    }
    if (endCandidates.length > 1) {
      var eid2 = "TE";
      events[eid2] = {
        id: eid2,
        taskId: 0,
        type: "end",
        num: 0,
        es: maxEf + 5,
        ef: maxEf + 5,
        ls: 0,
        lf: 0,
        tf: 0,
        ff: 0,
        isCritical: false,
        isVirtual: true
      };
      eventPred[eid2] = endCandidates.slice();
      eventSucc[eid2] = [];
      endCandidates.forEach(function(eeid) {
        eventSucc[eeid].push(eid2);
      });
      sortedEvents.push(eid2);
    }
    return {
      events,
      activities,
      eventPred,
      eventSucc,
      sortedEvents
    };
  }

  // netplan/core/layout.ts
  function calculateVerticalLayout(data) {
    var events = data.events;
    var activities = data.activities;
    var LAYER_HEIGHT = 60;
    var MARGIN_TOP = 100;
    var eids = Object.keys(events);
    var actPairs = [];
    activities.forEach(function(act) {
      if (act.source && act.target && act.source !== act.target) {
        actPairs.push({ src: act.source, tgt: act.target });
      }
    });
    var esValues = [];
    eids.forEach(function(eid) {
      var es = events[eid].es || 0;
      if (esValues.indexOf(es) === -1) esValues.push(es);
    });
    esValues.sort(function(a, b) {
      return a - b;
    });
    var esToBase = {};
    esValues.forEach(function(es, idx) {
      esToBase[es] = idx;
    });
    var layerEvents = {};
    eids.forEach(function(eid) {
      var bl = esToBase[events[eid].es || 0];
      if (!layerEvents[bl]) layerEvents[bl] = [];
      if (layerEvents[bl].indexOf(eid) === -1) layerEvents[bl].push(eid);
    });
    var eventBase = {};
    eids.forEach(function(eid) {
      eventBase[eid] = esToBase[events[eid].es || 0];
    });
    actPairs.forEach(function(p) {
      var srcEs = events[p.src] ? events[p.src].es || 0 : 0;
      var tgtEs = events[p.tgt] ? events[p.tgt].es || 0 : 0;
      if (srcEs !== tgtEs) return;
      var sb = eventBase[p.src], tb = eventBase[p.tgt];
      if (sb !== tb) {
        var minB = Math.min(sb, tb);
        eventBase[p.src] = minB;
        eventBase[p.tgt] = minB;
        [sb, tb].forEach(function(b) {
          if (b === minB) return;
          var arr = layerEvents[b] || [];
          var idx = arr.indexOf(p.src);
          if (idx !== -1) arr.splice(idx, 1);
          idx = arr.indexOf(p.tgt);
          if (idx !== -1) arr.splice(idx, 1);
        });
        var tgtArr = layerEvents[minB] || [];
        if (tgtArr.indexOf(p.src) === -1) tgtArr.push(p.src);
        if (tgtArr.indexOf(p.tgt) === -1) tgtArr.push(p.tgt);
        layerEvents[minB] = tgtArr;
      }
    });
    var eventSub = {};
    var subCounts = {};
    Object.keys(layerEvents).forEach(function(blKey) {
      var bl = parseInt(blKey);
      var evts = (layerEvents[bl] || []).slice();
      var seen = {};
      var uq = [];
      evts.forEach(function(e) {
        if (!seen[e]) {
          seen[e] = true;
          uq.push(e);
        }
      });
      var parent = {};
      uq.forEach(function(e) {
        parent[e] = e;
      });
      function find(x) {
        if (parent[x] !== x) parent[x] = find(parent[x]);
        return parent[x];
      }
      function union(a, b) {
        var ra = find(a), rb = find(b);
        if (ra !== rb) parent[rb] = ra;
      }
      actPairs.forEach(function(p) {
        var srcEs = events[p.src] ? events[p.src].es || 0 : 0;
        var tgtEs = events[p.tgt] ? events[p.tgt].es || 0 : 0;
        if (srcEs === tgtEs && uq.indexOf(p.src) !== -1 && uq.indexOf(p.tgt) !== -1) {
          union(p.src, p.tgt);
        }
      });
      var groups = {};
      uq.forEach(function(e) {
        var r = find(e);
        if (!groups[r]) groups[r] = [];
        groups[r].push(e);
      });
      var groupList = Object.keys(groups).map(function(r) {
        return { root: r, events: groups[r] };
      });
      groupList.sort(function(a, b) {
        var aHasCrit = a.events.some(function(e) {
          return events[e].isCritical;
        });
        var bHasCrit = b.events.some(function(e) {
          return events[e].isCritical;
        });
        if (aHasCrit && !bHasCrit) return -1;
        if (!aHasCrit && bHasCrit) return 1;
        return 0;
      });
      var subIdx = 0;
      groupList.forEach(function(g) {
        g.events.forEach(function(e) {
          eventSub[e] = subIdx;
        });
        subIdx++;
      });
      subCounts[bl] = Math.max(1, subIdx);
    });
    eids.forEach(function(eid) {
      if (eventSub[eid] === void 0) eventSub[eid] = 0;
      if (eventBase[eid] === void 0) eventBase[eid] = 0;
    });
    var srcOutTargets = {};
    activities.forEach(function(act) {
      var src = act.source;
      if (!srcOutTargets[src]) srcOutTargets[src] = [];
      if (srcOutTargets[src].indexOf(act.target) === -1)
        srcOutTargets[src].push(act.target);
    });
    Object.keys(srcOutTargets).forEach(function(src) {
      var targets = srcOutTargets[src];
      if (targets.length < 2) return;
      var firstSub = eventSub[targets[0]];
      var allSame = targets.every(function(t) {
        return eventSub[t] === firstSub;
      });
      if (!allSame) return;
      var srcBL = eventBase[src];
      var curSubCount = subCounts[srcBL] || 1;
      targets.forEach(function(tgt, idx) {
        if (idx === 0) {
          eventSub[tgt] = firstSub;
          return;
        }
        var newSub = curSubCount + idx - 1;
        eventSub[tgt] = newSub;
        actPairs.forEach(function(p) {
          if (p.src === tgt || p.tgt === tgt) {
            var other = p.src === tgt ? p.tgt : p.src;
            var oEs = events[other] ? events[other].es || 0 : 0;
            var tEs = events[tgt] ? events[tgt].es || 0 : 0;
            if (oEs === tEs) {
              if (p.src === tgt && eventSub[p.tgt] !== newSub) eventSub[p.tgt] = newSub;
              if (p.tgt === tgt && eventSub[p.src] !== newSub) eventSub[p.src] = newSub;
            }
          }
        });
      });
      subCounts[srcBL] = Math.max(subCounts[srcBL] || 1, curSubCount + targets.length - 1);
    });
    Object.keys(layerEvents).forEach(function(blKey) {
      var bl = parseInt(blKey);
      if (!subCounts[bl]) subCounts[bl] = 1;
    });
    var sortedBL = Object.keys(subCounts).map(Number).sort(function(a, b) {
      return a - b;
    });
    var cumoff = { 0: 0 };
    var cumulative = 0;
    sortedBL.forEach(function(bl) {
      cumoff[bl] = cumulative;
      cumulative += subCounts[bl] || 1;
    });
    var maxLayer = cumulative;
    var eventAbsLayer = {};
    eids.forEach(function(eid) {
      var bl = eventBase[eid] || 0;
      var sl = eventSub[eid] || 0;
      var abs = (cumoff[bl] || 0) + sl;
      eventAbsLayer[eid] = abs;
      events[eid].layer = abs;
      events[eid].y = MARGIN_TOP + abs * LAYER_HEIGHT;
    });
    var layout = {};
    eids.forEach(function(eid) {
      var ly = eventAbsLayer[eid] || 0;
      layout[eid] = { layer: ly, y: MARGIN_TOP + ly * LAYER_HEIGHT, num: events[eid].num };
    });
    return { events, activities, layout, maxLayer };
  }

  // netplan/render/crossarc.ts
  function findSegIntersection(ax1, ay1, ax2, ay2, bx1, by1, bx2, by2) {
    var d = (ax2 - ax1) * (by2 - by1) - (ay2 - ay1) * (bx2 - bx1);
    if (Math.abs(d) < 1e-10) return null;
    var t = ((bx1 - ax1) * (by2 - by1) - (by1 - ay1) * (bx2 - bx1)) / d;
    var u = ((bx1 - ax1) * (ay2 - ay1) - (by1 - ay1) * (ax2 - ax1)) / d;
    if (t < 0 || t > 1 || u < 0 || u > 1) return null;
    return { x: ax1 + t * (ax2 - ax1), y: ay1 + t * (ay2 - ay1) };
  }
  function generateCrossArcs(allSegments, parts, _isTimeMode) {
    for (var i = 0; i < allSegments.length; i++) {
      for (var j = i + 1; j < allSegments.length; j++) {
        var a = allSegments[i], b = allSegments[j];
        if (a.actId === b.actId) continue;
        for (var ai = 0; ai < a.segs.length; ai++) {
          for (var bi = 0; bi < b.segs.length; bi++) {
            var s1 = a.segs[ai], s2 = b.segs[bi];
            var cross = findSegIntersection(
              s1.x1,
              s1.y1,
              s1.x2,
              s1.y2,
              s2.x1,
              s2.y1,
              s2.x2,
              s2.y2
            );
            if (cross) {
              var arcOn;
              if (!a.isCritical && b.isCritical) {
                arcOn = a;
              } else if (a.isCritical && !b.isCritical) {
                arcOn = b;
              } else if (!a.isCritical && !b.isCritical) {
                arcOn = a;
              } else {
                arcOn = b;
              }
              if (!arcOn) arcOn = a;
              var bSeg;
              if (arcOn === a) {
                bSeg = s2;
              } else {
                bSeg = s1;
              }
              var bdx = bSeg.x2 - bSeg.x1, bdy = bSeg.y2 - bSeg.y1;
              var blen = Math.sqrt(bdx * bdx + bdy * bdy) || 1;
              var nx = -bdy / blen, ny = bdx / blen;
              var arcR = 5;
              var lineW = arcOn.isCritical ? 3 : 1.5;
              parts.push(
                '<path d="M' + (cross.x - nx * 2) + " " + (cross.y - ny * 2) + " A" + (arcR + 1) + " " + (arcR + 1) + " 0 0 0 " + (cross.x + nx * 2) + " " + (cross.y + ny * 2) + '" class="net-cross-arc" fill="none" stroke="#fff" stroke-width="' + (lineW + 2) + '" stroke-linecap="round"/>'
              );
              parts.push(
                '<path d="M' + (cross.x - nx * 2) + " " + (cross.y - ny * 2) + " A" + (arcR + 1) + " " + (arcR + 1) + " 0 0 0 " + (cross.x + nx * 2) + " " + (cross.y + ny * 2) + '" class="net-cross-arc" fill="none" stroke="#999" stroke-width="1" stroke-linecap="round"/>'
              );
            }
          }
        }
      }
    }
  }
  function collectAllSegments(layout, activities, relations, isTimeMode, NODE_R) {
    var allSegments = [];
    var events = layout.events;
    var taskEsMap = {};
    var taskEfMap = {};
    activities.forEach(function(act) {
      taskEsMap[act.source] = act.es;
      taskEfMap[act.target] = act.ef;
    });
    activities.forEach(function(act) {
      var srcId = act.source, tgtId = act.target;
      var src = events[srcId], tgt = events[tgtId];
      if (!src || !tgt) return;
      var sx = (src.x || 0) + NODE_R, sy = src.y || 0;
      var ex = tgt.x || 0, ey = tgt.y || 0;
      var segs = [];
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
        segs
      });
    });
    relations.forEach(function(rel) {
      var predEf = taskEfMap[rel.source];
      var succEs = taskEsMap[rel.target];
      if (predEf === void 0 || succEs === void 0) return;
      var srcId = "T" + predEf, tgtId = "T" + succEs;
      if (srcId === tgtId) return;
      var src = events[srcId], tgt = events[tgtId];
      if (!src || !tgt) return;
      var sx = (src.x || 0) + NODE_R, sy = src.y || 0;
      var ex = tgt.x || 0, ey = tgt.y || 0;
      var dseg;
      if (Math.abs(ey - sy) < 2) {
        dseg = [{ x1: sx, y1: sy, x2: ex, y2: ey }];
      } else {
        dseg = [
          { x1: sx, y1: sy, x2: sx, y2: ey },
          { x1: sx, y1: ey, x2: ex, y2: ey }
        ];
      }
      allSegments.push({ actId: "dummy-" + rel.source + "-" + rel.target, isCritical: false, segs: dseg, isDummy: true });
    });
    return allSegments;
  }
  function updateCrossArcOverlays() {
    var svg = window._netSvg;
    if (!svg) return;
    var old = svg.querySelectorAll(".net-cross-arc");
    for (var i = 0; i < old.length; i++) {
      old[i].parentNode.removeChild(old[i]);
    }
    var layout = window._netLayout;
    var activities = window._netActivities;
    var offsets = window._netEventOffsets || {};
    var nr = window._netNodeRadius || 11;
    if (!layout || !layout.events || !activities) return;
    var acts = [];
    activities.forEach(function(act) {
      if (!act.source || !act.target) return;
      var s = layout.events[act.source];
      var t = layout.events[act.target];
      if (!s || !t) return;
      var sx = s.x + (offsets[s.id] ? offsets[s.id].x : 0) + nr;
      var sy = s.y + (offsets[s.id] ? offsets[s.id].y : 0);
      var ex = t.x + (offsets[t.id] ? offsets[t.id].x : 0);
      var ey = t.y + (offsets[t.id] ? offsets[t.id].y : 0);
      acts.push({ id: act.id, isCritical: act.isCritical || false, sx, sy, ex, ey });
    });
    if (acts.length < 2) return;
    var allSegments = [];
    acts.forEach(function(a2) {
      var segs;
      if (Math.abs(a2.ey - a2.sy) < 2) {
        segs = [{ x1: a2.sx, y1: a2.sy, x2: a2.ex, y2: a2.ey }];
      } else {
        segs = [
          { x1: a2.sx, y1: a2.sy, x2: a2.ex - 2, y2: a2.sy },
          { x1: a2.ex - 2, y1: a2.sy, x2: a2.ex - 2, y2: a2.ey },
          { x1: a2.ex - 2, y1: a2.ey, x2: a2.ex, y2: a2.ey }
        ];
      }
      allSegments.push({ actId: a2.id, isCritical: a2.isCritical, segs });
    });
    function addArc(crossX, crossY, nx2, ny2, lineW2) {
      var arcR = 5;
      var g = document.createElementNS("http://www.w3.org/2000/svg", "g");
      g.setAttribute("class", "net-cross-arc");
      var arc1 = document.createElementNS("http://www.w3.org/2000/svg", "path");
      arc1.setAttribute("d", "M" + (crossX - nx2 * 2) + " " + (crossY - ny2 * 2) + " A" + (arcR + 1) + " " + (arcR + 1) + " 0 0 0 " + (crossX + nx2 * 2) + " " + (crossY + ny2 * 2));
      arc1.setAttribute("fill", "none");
      arc1.setAttribute("stroke", "#fff");
      arc1.setAttribute("stroke-width", "" + (lineW2 + 2));
      arc1.setAttribute("stroke-linecap", "round");
      g.appendChild(arc1);
      var arc2 = document.createElementNS("http://www.w3.org/2000/svg", "path");
      arc2.setAttribute("d", "M" + (crossX - nx2 * 2) + " " + (crossY - ny2 * 2) + " A" + (arcR + 1) + " " + (arcR + 1) + " 0 0 0 " + (crossX + nx2 * 2) + " " + (crossY + ny2 * 2));
      arc2.setAttribute("fill", "none");
      arc2.setAttribute("stroke", "#999");
      arc2.setAttribute("stroke-width", "1");
      arc2.setAttribute("stroke-linecap", "round");
      g.appendChild(arc2);
      svg.appendChild(g);
    }
    for (var i = 0; i < allSegments.length; i++) {
      for (var j = i + 1; j < allSegments.length; j++) {
        var a = allSegments[i], b = allSegments[j];
        if (a.actId === b.actId) continue;
        for (var ai = 0; ai < a.segs.length; ai++) {
          for (var bi = 0; bi < b.segs.length; bi++) {
            var s1 = a.segs[ai], s2 = b.segs[bi];
            var cross = findSegIntersection(
              s1.x1,
              s1.y1,
              s1.x2,
              s1.y2,
              s2.x1,
              s2.y1,
              s2.x2,
              s2.y2
            );
            if (cross) {
              var skipArc = false;
              var arcOn = null;
              if (!a.isCritical && b.isCritical) {
                arcOn = a;
              } else if (a.isCritical && !b.isCritical) {
                arcOn = b;
              } else if (!a.isCritical && !b.isCritical) {
                arcOn = a;
              } else {
                skipArc = true;
              }
              if (!skipArc && arcOn && cross) {
                var bSeg = arcOn === a ? s2 : s1;
                var bdx = bSeg.x2 - bSeg.x1, bdy = bSeg.y2 - bSeg.y1;
                var blen = Math.sqrt(bdx * bdx + bdy * bdy) || 1;
                var nx = -bdy / blen, ny = bdx / blen;
                var lineW = arcOn.isCritical ? 3 : 1.5;
                addArc(cross.x, cross.y, nx, ny, lineW);
              }
            }
          }
        }
      }
    }
  }

  // netplan/render/generator.ts
  var SVG_NS = "http://www.w3.org/2000/svg";
  function svgEl(tag, attrs) {
    const el = document.createElementNS(SVG_NS, tag);
    if (attrs) {
      for (const [k, v] of Object.entries(attrs)) {
        el.setAttribute(k, String(v));
      }
    }
    return el;
  }
  function getISOWeek(dt) {
    let d = new Date(Date.UTC(dt.getFullYear(), dt.getMonth(), dt.getDate()));
    let dayNum = d.getUTCDay() || 7;
    d.setUTCDate(d.getUTCDate() + 4 - dayNum);
    let yearStart = new Date(Date.UTC(d.getUTCFullYear(), 0, 1));
    return Math.ceil(((d.getTime() - yearStart.getTime()) / 864e5 + 1) / 7);
  }
  function buildNetworkSvg(params) {
    let p = params;
    let cvW = p.canvasW || 3780;
    let cvH = p.canvasH || 400;
    let layerH = p.layerHeight || 60;
    let nr = p.nodeRadius || 11;
    let nfs = Math.max(9, nr);
    let prStartDate = p.projectStartDate || (/* @__PURE__ */ new Date()).toISOString().slice(0, 10);
    let sd = new Date(prStartDate);
    let dw = p.dayWidth || 8;
    let tsm = window._netTimeScaleMode || 0;
    let mode = p.mode || "time";
    let isTimeMode = mode === "time";
    let showCritical = p.showCritical !== false;
    let showFloat = p.showFloat !== false;
    let showGridH = p.showGridH === true;
    let showGridV = p.showGridV === true;
    let activities = (p.timeParams ? p.timeParams.activities : []) || [];
    let layout = p.layout;
    let eventsMap = layout.events;
    var pendingOffsets = p._pendingOffsets || {};
    Object.keys(pendingOffsets).forEach(function(eid) {
      var off = pendingOffsets[eid];
      if (eventsMap[eid] && off) {
        if (off.x) eventsMap[eid].x = off.x;
        if (off.y) eventsMap[eid].y = off.y;
      }
    });
    const svg = svgEl("svg", {
      id: "network-svg",
      class: "network-svg",
      width: cvW,
      height: cvH,
      style: "background:#fafafa",
      xmlns: SVG_NS
    });
    const bg = svgEl("rect", { x: 0, y: 0, width: cvW, height: cvH, fill: "#fafafa", stroke: "#e8e8e8" });
    svg.appendChild(bg);
    renderRowBg(svg, eventsMap, cvW, layerH, mode);
    const rulerBg = svgEl("rect", { x: 10, y: 0, width: p.totalDays * dw + 42, height: 52, fill: "#fff", stroke: "#e8e8e8" });
    svg.appendChild(rulerBg);
    renderTimeline(svg, sd, dw, p.totalDays, isTimeMode, tsm);
    if (isTimeMode && (showGridH || showGridV)) {
      const marginTop = p._marginTop || 100;
      const { lastRowY: lastRowY2 } = findExtents(eventsMap, p.totalDays, dw, nr);
      const gridY1 = 52;
      const gridY2 = lastRowY2 + layerH + 10;
      const maxY = gridY2;
      renderGrid(svg, p.totalDays, dw, showGridH, showGridV, layerH, marginTop, maxY, 12);
    }
    renderActivities(svg, activities, eventsMap, showCritical, showFloat, isTimeMode, dw, sd, nr, p);
    renderBusbar(svg, activities, eventsMap, nr);
    if (p.timeParams && p.timeParams.activities) {
      let allSegs = collectAllSegments(
        p.layout,
        p.timeParams.activities,
        p.timeParams.relations || [],
        isTimeMode,
        nr
      );
      let crossParts = [];
      generateCrossArcs(allSegs, crossParts, isTimeMode);
      if (crossParts.length > 0) {
        let tempDiv = document.createElement("div");
        tempDiv.innerHTML = crossParts.join("");
        for (let ci = 0; ci < tempDiv.children.length; ci++) {
          svg.appendChild(tempDiv.children[ci].cloneNode(true));
        }
      }
    }
    renderNodes(svg, eventsMap, showCritical, showFloat, isTimeMode, nr, nfs);
    renderTodayLine(svg, p, sd, dw, cvH, isTimeMode);
    renderProgressLine(svg, p, sd, dw, cvH, eventsMap, isTimeMode);
    renderProgressCurve(svg, p, sd, dw, isTimeMode);
    let legendH = 95;
    let { lastRowY } = findExtents(eventsMap, p.totalDays, dw, nr);
    let legendTop = lastRowY + layerH + 10;
    renderLegend(svg, lastRowY, layerH, p.totalDuration || 0);
    renderBottomRuler(svg, p, sd, dw, cvW, cvH, isTimeMode);
    var rulerBottom = window._netRulerBottom || cvH - 10;
    var actualBottom = Math.max(legendTop + legendH + 10, rulerBottom + 10);
    var finalH = Math.max(cvH, actualBottom);
    svg.setAttribute("height", String(finalH));
    window._netSvgHeight = finalH;
    window._netPendingOffsets = {};
    const xmlSer = new XMLSerializer();
    return xmlSer.serializeToString(svg);
  }
  function renderRowBg(parent, eventsMap, cvW, layerH, mode) {
    let ySet = {};
    Object.keys(eventsMap).forEach(function(eid) {
      let evt = eventsMap[eid];
      if (evt.y !== void 0) ySet[evt.y] = true;
    });
    let yList = Object.keys(ySet).map(Number).sort((a, b) => a - b);
    const g = svgEl("g", { class: "net-row-bg" });
    yList.forEach(function(y, idx) {
      let isEven = idx % 2 === 0;
      g.appendChild(svgEl("rect", {
        class: "net-row-bg",
        x: 0,
        y: y - layerH / 2,
        width: cvW,
        height: layerH,
        fill: isEven ? "#ffffff" : "#f5f5f5",
        opacity: 0.3
      }));
      if (mode === "logic") {
        const txt = svgEl("text", { x: 4, y: y + 4, "font-size": 10, fill: "#999" });
        txt.textContent = "L" + idx;
        g.appendChild(txt);
      }
    });
    parent.appendChild(g);
  }
  function renderGrid(parent, totalDays, dw, showH, showV, layerH, marginTop, maxY, ML) {
    const g = svgEl("g", { class: "net-grid", opacity: "0.8", "pointer-events": "none" });
    const gridY1 = 52;
    const gridY2 = maxY;
    var allYs = [];
    var maxLine = Math.floor((maxY - marginTop) / layerH) + 1;
    if (showH) {
      var gapY = marginTop + layerH;
      while (gapY <= maxY) {
        if (gapY >= gridY1) {
          g.appendChild(svgEl("line", {
            x1: ML,
            y1: gapY,
            x2: ML + totalDays * dw + 400,
            y2: gapY,
            stroke: "#999",
            "stroke-width": 0.6,
            "stroke-dasharray": "6,4"
          }));
        }
        gapY += layerH;
      }
    }
    if (showV) {
      for (let d = 0; d <= totalDays; d++) {
        let x = ML + d * dw;
        g.appendChild(svgEl("line", {
          x1: x,
          y1: gridY1,
          x2: x,
          y2: gridY2,
          stroke: "#999",
          "stroke-width": 0.5,
          "stroke-dasharray": "4,6"
        }));
      }
    }
    parent.appendChild(g);
  }
  function renderTimeline(parent, sd, dw, totalDays, isTimeMode, tsm) {
    if (!isTimeMode) return;
    const UPPER_H = 28, LOWER_H = 24;
    const MT = 12;
    const g = svgEl("g", { class: "timeline" });
    if (tsm === 0) {
      let lastYear = -1;
      for (let d = 0; d <= totalDays; d++) {
        let dt = new Date(sd.getTime() + d * 864e5);
        let x = MT + d * dw;
        if (dt.getDate() === 1 || d === 0) {
          let mw = new Date(dt.getFullYear(), dt.getMonth() + 1, 0).getDate() * dw;
          const t = svgEl("text", {
            x: x + mw / 2,
            y: UPPER_H / 2 + 4,
            "text-anchor": "middle",
            "font-size": 11,
            fill: "#333",
            "font-weight": "bold"
          });
          t.textContent = dt.getFullYear() + "-" + String(dt.getMonth() + 1).padStart(2, "0");
          g.appendChild(t);
          if (dt.getFullYear() !== lastYear) {
            lastYear = dt.getFullYear();
            const yt = svgEl("text", {
              x,
              y: UPPER_H / 2 + 4 - 14,
              "font-size": 10,
              fill: "#999"
            });
            yt.textContent = " " + lastYear + "\u5E74";
            g.appendChild(yt);
          }
        }
      }
      let lastW = -1;
      for (let d = 0; d <= totalDays; d++) {
        let dt = new Date(sd.getTime() + d * 864e5);
        let x = MT + d * dw;
        let dow = dt.getDay();
        let isSat = dow === 6, isSun = dow === 0;
        if (isSat || isSun) {
          g.appendChild(svgEl("rect", {
            x,
            y: UPPER_H,
            width: dw,
            height: LOWER_H,
            fill: isSat ? "#e6f7ff" : "#fff0f0",
            opacity: 0.1
          }));
        }
        const t = svgEl("text", {
          x: x + dw / 2,
          y: UPPER_H + LOWER_H / 2 + 4,
          "text-anchor": "middle",
          "font-size": 10,
          fill: isSat || isSun ? "#ccc" : "#666"
        });
        t.textContent = String(dt.getDate());
        g.appendChild(t);
        let wk = getISOWeek(dt);
        if (wk !== lastW) {
          lastW = wk;
          const wt = svgEl("text", {
            x,
            y: UPPER_H + LOWER_H + 12,
            "font-size": 9,
            fill: "#aaa"
          });
          wt.textContent = "W" + wk;
          g.appendChild(wt);
        }
      }
    } else if (tsm === 1) {
      let lastYear = -1;
      for (let d = 0; d <= totalDays; d++) {
        let dt = new Date(sd.getTime() + d * 864e5);
        let x = MT + d * dw;
        if (dt.getDate() === 1 || d === 0) {
          let mw = new Date(dt.getFullYear(), dt.getMonth() + 1, 0).getDate() * dw;
          const t = svgEl("text", {
            x: x + mw / 2,
            y: UPPER_H / 2 + 4,
            "text-anchor": "middle",
            "font-size": 11,
            fill: "#333",
            "font-weight": "bold"
          });
          t.textContent = dt.getFullYear() + "-" + String(dt.getMonth() + 1).padStart(2, "0");
          g.appendChild(t);
          if (dt.getFullYear() !== lastYear) {
            lastYear = dt.getFullYear();
          }
        }
      }
      let totalPx = totalDays * dw;
      let cellW = Math.max(dw, totalPx / Math.min(totalDays, 20));
      let dayIdx = 0;
      for (let x = MT; x < MT + totalPx; x += cellW) {
        let dt3 = new Date(sd.getTime() + dayIdx * 864e5);
        const t = svgEl("text", {
          x: x + cellW / 2,
          y: UPPER_H + LOWER_H / 2 + 4,
          "text-anchor": "middle",
          "font-size": 10,
          fill: "#666"
        });
        t.textContent = dt3.getMonth() + 1 + "/" + dt3.getDate();
        g.appendChild(t);
        dayIdx += Math.round(cellW / dw);
      }
    } else if (tsm === 2) {
      let lastYear = -1;
      for (let d = 0; d <= totalDays; d++) {
        let dt = new Date(sd.getTime() + d * 864e5);
        let x = MT + d * dw;
        if (dt.getDate() === 1 || d === 0) {
          let mw = new Date(dt.getFullYear(), dt.getMonth() + 1, 0).getDate() * dw;
          const t = svgEl("text", {
            x: x + mw / 2,
            y: UPPER_H / 2 + 4,
            "text-anchor": "middle",
            "font-size": 11,
            fill: "#333",
            "font-weight": "bold"
          });
          t.textContent = dt.getFullYear() + "-" + String(dt.getMonth() + 1).padStart(2, "0");
          g.appendChild(t);
          if (dt.getFullYear() !== lastYear) {
            lastYear = dt.getFullYear();
          }
        }
      }
      let lastW = -1;
      for (let d = 0; d <= totalDays; d++) {
        let dt = new Date(sd.getTime() + d * 864e5);
        let x = MT + d * dw;
        let wk = getISOWeek(dt);
        let dayNum = (dt.getDay() + 6) % 7;
        if (wk !== lastW) {
          let ws = Math.max(0, Math.round((new Date(dt.getFullYear(), dt.getMonth(), dt.getDate() - dayNum).getTime() - sd.getTime()) / 864e5));
          let we = Math.min(totalDays, ws + 7);
          let wkSpan = (we - ws) * dw;
          const t = svgEl("text", {
            x: x + wkSpan / 2,
            y: UPPER_H + LOWER_H / 2 + 4,
            "text-anchor": "middle",
            "font-size": 10,
            fill: "#666"
          });
          t.textContent = "W" + wk;
          g.appendChild(t);
          lastW = wk;
        }
      }
    }
    parent.appendChild(g);
  }
  function renderActivities(parent, activities, eventsMap, showCritical, showFloat, isTimeMode, dw, sd, nr, p) {
    let critActs = [];
    let nonCritActs = [];
    (activities || []).forEach(function(act) {
      (act.isCritical && showCritical ? critActs : nonCritActs).push(act);
    });
    const arrowsG = svgEl("g", { class: "net-arrows" });
    let hasContent = false;
    var inDeg = {};
    var inDegActs = {};
    (activities || []).forEach(function(act) {
      var tgt = act.target;
      if (tgt) {
        inDeg[tgt] = (inDeg[tgt] || 0) + 1;
        if (!inDegActs[tgt]) inDegActs[tgt] = [];
        inDegActs[tgt].push(act);
      }
    });
    Object.keys(inDegActs).forEach(function(tgt) {
      inDegActs[tgt].sort(function(a, b) {
        return a.id - b.id;
      });
    });
    [nonCritActs, critActs].forEach(function(actList) {
      actList.forEach(function(act) {
        let srcEvt = eventsMap[act.source];
        let tgtEvt = eventsMap[act.target];
        if (!srcEvt || !tgtEvt) return;
        let x1 = srcEvt.x || 0, y1 = srcEvt.y || 0;
        let x2 = tgtEvt.x || 0, y2 = tgtEvt.y || 0;
        let isCrit = act.isCritical;
        let isDummy = act.isDummy || act.duration === 0 && act.id < 0;
        let sc = isCrit && showCritical ? "#ff4d4f" : isDummy ? "#52c41a" : "#1890ff";
        let sw = isCrit && showCritical ? 2.5 : isDummy ? 1 : 1.5;
        let dash = isDummy ? "6,3" : "none";
        hasContent = true;
        let isLShape = Math.abs(y2 - y1) >= 2;
        let hasFF = false;
        let waveX1 = x2, waveX2 = x2, waveY = y2;
        if (!isDummy && isTimeMode && act.ff > 0) {
          let ffDist = act.ff * dw;
          if (ffDist > 2) {
            hasFF = true;
            waveX1 = x2 - ffDist;
          }
        }
        var tgtActs = inDegActs[act.target] || [];
        var inDegTgt = tgtActs.length;
        var inDegIdx = 0;
        tgtActs.forEach(function(a, i) {
          if (a.id === act.id) inDegIdx = i;
        });
        var tgtOffY = 0;
        if (inDegTgt > 1) {
          var gap = 6;
          tgtOffY = -((inDegTgt - 1) * gap) / 2 + inDegIdx * gap;
        }
        var inY2 = y2 + tgtOffY;
        if (actList === critActs && showCritical) {
          let offY = 1;
          if (isLShape) {
            let midX = Math.max(x1 + nr + 4, x2 - nr - 4);
            arrowsG.appendChild(svgEl("path", {
              d: "M" + x1 + " " + (y1 + offY) + " L" + midX + " " + (y1 + offY) + " L" + midX + " " + (inY2 + offY) + " L" + (x2 + nr) + " " + (inY2 + offY),
              stroke: "#ff4d4f",
              "stroke-width": 4,
              opacity: 0.2,
              fill: "none"
            }));
          } else if (hasFF) {
            arrowsG.appendChild(svgEl("line", {
              x1,
              y1: y1 + offY,
              x2: waveX1,
              y2: inY2 + offY,
              stroke: "#ff4d4f",
              "stroke-width": 4,
              opacity: 0.2
            }));
          } else {
            arrowsG.appendChild(svgEl("line", {
              x1,
              y1: y1 + offY,
              x2,
              y2: inY2 + offY,
              stroke: "#ff4d4f",
              "stroke-width": 4,
              opacity: 0.2
            }));
          }
        }
        let aLen = 7;
        if (isLShape) {
          let midX = Math.max(x1 + nr + 4, x2 - nr - 4);
          if (midX < x1 + nr + 4) midX = x1 + nr + 4;
          let pathStr = "M" + x1 + " " + y1 + " L" + midX + " " + y1 + " L" + midX + " " + inY2 + " L" + (x2 + nr) + " " + inY2;
          arrowsG.appendChild(svgEl("path", {
            d: pathStr,
            stroke: sc,
            "stroke-width": sw,
            "stroke-dasharray": dash,
            fill: "none",
            "data-activity-id": act.id,
            "data-src": act.source,
            "data-tgt": act.target,
            style: "cursor:pointer"
          }));
          const arrow = svgEl("polygon", {
            points: [
              x2 - aLen + "," + (inY2 - aLen * 0.38),
              x2 + nr + "," + inY2,
              x2 - aLen + "," + (inY2 + aLen * 0.38)
            ].join(" "),
            fill: sc
          });
          arrowsG.appendChild(arrow);
          if (!isDummy) {
            let labelX = Math.max(x1 + (midX - x1) / 2, x1 + 20);
            const nt = svgEl("text", {
              x: labelX,
              y: y1 - 4,
              "text-anchor": "middle",
              "font-size": 9,
              fill: sc
            });
            nt.textContent = act.code || "";
            arrowsG.appendChild(nt);
            const dt = svgEl("text", {
              x: labelX,
              y: y1 + 8,
              "text-anchor": "middle",
              "font-size": 8,
              fill: "#999"
            });
            dt.textContent = act.duration;
            arrowsG.appendChild(dt);
          }
        } else if (hasFF) {
          arrowsG.appendChild(svgEl("line", {
            x1,
            y1,
            x2: waveX1,
            y2: inY2,
            stroke: sc,
            "stroke-width": sw,
            "data-activity-id": act.id,
            "data-src": act.source,
            "data-tgt": act.target,
            style: "cursor:pointer"
          }));
          let wMid = (waveX1 + waveX2) / 2;
          arrowsG.appendChild(svgEl("path", {
            d: "M" + waveX1 + " " + inY2 + " Q" + (waveX1 + (waveX2 - waveX1) * 0.25) + " " + (inY2 - 4) + " " + wMid + " " + inY2 + " Q" + (waveX2 - (waveX2 - waveX1) * 0.25) + " " + (inY2 + 4) + " " + waveX2 + " " + inY2,
            stroke: sc,
            "stroke-width": sw,
            fill: "none",
            "data-activity-id": act.id,
            "data-src": act.source,
            "data-tgt": act.target,
            style: "cursor:pointer"
          }));
          const arrow = svgEl("polygon", {
            points: [
              x2 - aLen + "," + (inY2 - aLen * 0.38),
              x2 + "," + inY2,
              x2 - aLen + "," + (inY2 + aLen * 0.38)
            ].join(" "),
            fill: sc
          });
          arrowsG.appendChild(arrow);
          if (!isDummy) {
            const nt = svgEl("text", {
              x: (x1 + x2) / 2,
              y: (y1 + inY2) / 2 - 4,
              "text-anchor": "middle",
              "font-size": 9,
              fill: sc
            });
            nt.textContent = act.code || "";
            arrowsG.appendChild(nt);
            const dt = svgEl("text", {
              x: (x1 + x2) / 2,
              y: (y1 + inY2) / 2 + 8,
              "text-anchor": "middle",
              "font-size": 8,
              fill: "#999"
            });
            dt.textContent = act.duration;
            arrowsG.appendChild(dt);
          }
        } else {
          arrowsG.appendChild(svgEl("line", {
            x1,
            y1,
            x2,
            y2: inY2,
            stroke: sc,
            "stroke-width": sw,
            "stroke-dasharray": dash,
            "data-activity-id": act.id,
            "data-src": act.source,
            "data-tgt": act.target,
            style: "cursor:pointer"
          }));
          let angle = Math.atan2(inY2 - y1, x2 - x1);
          const arrow = svgEl("polygon", {
            points: [
              x2 - aLen * Math.cos(angle - 0.4) + "," + (inY2 - aLen * Math.sin(angle - 0.4)),
              x2 + "," + inY2,
              x2 - aLen * Math.cos(angle + 0.4) + "," + (inY2 - aLen * Math.sin(angle + 0.4))
            ].join(" "),
            fill: sc
          });
          arrowsG.appendChild(arrow);
          if (!isDummy) {
            const nt = svgEl("text", {
              x: (x1 + x2) / 2,
              y: (y1 + inY2) / 2 - 4,
              "text-anchor": "middle",
              "font-size": 9,
              fill: sc
            });
            nt.textContent = act.code || "";
            arrowsG.appendChild(nt);
            const dt = svgEl("text", {
              x: (x1 + x2) / 2,
              y: (y1 + inY2) / 2 + 8,
              "text-anchor": "middle",
              "font-size": 8,
              fill: "#999"
            });
            dt.textContent = act.duration;
            arrowsG.appendChild(dt);
          }
        }
      });
    });
    if (hasContent) {
      parent.appendChild(arrowsG);
    }
  }
  function renderBusbar(parent, activities, eventsMap, nr) {
    let hasBus = false;
    const g = svgEl("g", { class: "busbar", opacity: 0.5 });
    Object.keys(eventsMap).forEach(function(eid) {
      let evt = eventsMap[eid];
      if (evt.isVirtual) return;
      let ex = evt.x || 0, ey = evt.y || 0;
      let outActs = activities.filter(function(a) {
        return a.source === eid;
      });
      if (outActs.length >= 2) {
        hasBus = true;
        let tys = outActs.map(function(a) {
          let t = eventsMap[a.target];
          return t ? t.y || 0 : ey;
        });
        let minY = Math.min(ey, ...tys), maxY = Math.max(ey, ...tys);
        let bx = ex + nr + 8;
        g.appendChild(svgEl("line", {
          x1: bx,
          y1: minY,
          x2: bx,
          y2: maxY,
          stroke: "#aaa",
          "stroke-width": 1.5,
          opacity: 0.6
        }));
        outActs.forEach(function(a) {
          let t = eventsMap[a.target];
          if (!t) return;
          let ty = t.y || 0, tx = t.x || ex;
          let eX = tx - nr - 2;
          g.appendChild(svgEl("line", {
            x1: bx,
            y1: ty,
            x2: eX,
            y2: ty,
            stroke: "#aaa",
            "stroke-width": 0.8,
            opacity: 0.6
          }));
          let aLen = 5;
          g.appendChild(svgEl("polygon", {
            points: [
              eX - aLen + "," + (ty - aLen * 0.38),
              eX + "," + ty,
              eX - aLen + "," + (ty + aLen * 0.38)
            ].join(" "),
            fill: "#aaa",
            opacity: 0.6
          }));
        });
        g.appendChild(svgEl("line", {
          x1: ex,
          y1: ey,
          x2: bx,
          y2: ey,
          stroke: "#aaa",
          "stroke-width": 1,
          opacity: 0.5
        }));
      }
      let inActs = activities.filter(function(a) {
        return a.target === eid;
      });
      if (inActs.length >= 2) {
        hasBus = true;
        let sys = inActs.map(function(a) {
          let s = eventsMap[a.source];
          return s ? s.y || 0 : ey;
        });
        let minY = Math.min(ey, ...sys), maxY = Math.max(ey, ...sys);
        let bx = ex - nr - 8;
        g.appendChild(svgEl("line", {
          x1: bx,
          y1: minY,
          x2: bx,
          y2: maxY,
          stroke: "#aaa",
          "stroke-width": 1.5,
          opacity: 0.6
        }));
        inActs.forEach(function(a) {
          let s = eventsMap[a.source];
          if (!s) return;
          let sy = s.y || 0, sx = s.x || 0;
          let eX = sx + nr + 2;
          g.appendChild(svgEl("line", {
            x1: eX,
            y1: sy,
            x2: bx,
            y2: sy,
            stroke: "#aaa",
            "stroke-width": 0.8,
            opacity: 0.6
          }));
        });
        g.appendChild(svgEl("line", {
          x1: bx,
          y1: ey,
          x2: ex,
          y2: ey,
          stroke: "#aaa",
          "stroke-width": 1,
          opacity: 0.5
        }));
      }
    });
    if (hasBus) parent.appendChild(g);
  }
  function renderNodes(parent, eventsMap, showCritical, showFloat, isTimeMode, nr, nfs) {
    const g = svgEl("g", { class: "net-events" });
    Object.keys(eventsMap).forEach(function(eid) {
      let evt = eventsMap[eid];
      if (evt.isVirtual) return;
      let ex = evt.x || 0, ey = evt.y || 0;
      let isCrit = evt.isCritical && showCritical;
      const eg = svgEl("g", {
        class: "net-event",
        "data-event-id": eid,
        "data-task-id": evt.taskId || 0
      });
      if (isCrit) {
        eg.appendChild(svgEl("circle", {
          cx: ex,
          cy: ey,
          r: nr,
          fill: "#fff",
          stroke: "#ff4d4f",
          "stroke-width": 3
        }));
        eg.appendChild(svgEl("circle", {
          cx: ex,
          cy: ey,
          r: nr - 4,
          fill: "#ff4d4f",
          stroke: "#ff4d4f",
          "stroke-width": 1.5
        }));
      } else {
        eg.appendChild(svgEl("circle", {
          cx: ex,
          cy: ey,
          r: nr,
          fill: "#fff",
          stroke: "#1890ff",
          "stroke-width": 1.5
        }));
      }
      const nt = svgEl("text", {
        x: ex,
        y: ey + 4,
        "text-anchor": "middle",
        "font-size": nfs,
        fill: isCrit ? "#fff" : "#333",
        "font-weight": "bold"
      });
      nt.textContent = String(evt.num != null ? evt.num : eid);
      eg.appendChild(nt);
      if (isTimeMode) {
        const et = svgEl("text", {
          x: ex,
          y: ey + nr + 12,
          "text-anchor": "middle",
          "font-size": 8,
          fill: "#999"
        });
        et.textContent = "ET=" + evt.es;
        eg.appendChild(et);
      }
      if (showFloat && evt.tf > 0) {
        const tft = svgEl("text", {
          x: ex,
          y: ey + nr + 22,
          "text-anchor": "middle",
          "font-size": 7,
          fill: "#e67e22"
        });
        tft.textContent = "TF=" + evt.tf;
        eg.appendChild(tft);
      }
      g.appendChild(eg);
    });
    parent.appendChild(g);
  }
  function renderTodayLine(parent, p, sd, dw, cvH, isTimeMode) {
    if (!(isTimeMode && p.showTodayLine)) return;
    let today = /* @__PURE__ */ new Date();
    let off = Math.floor((today.getTime() - sd.getTime()) / 864e5);
    if (off >= 0 && off < p.totalDays) {
      let tx = 80 + off * dw + dw / 2;
      parent.appendChild(svgEl("line", {
        x1: tx,
        y1: 0,
        x2: tx,
        y2: cvH - 28,
        stroke: "#ff4d4f",
        "stroke-width": 1,
        "stroke-dasharray": "4,3",
        opacity: 0.5
      }));
      const t = svgEl("text", {
        x: tx + 4,
        y: 14,
        "font-size": 10,
        fill: "#ff4d4f"
      });
      t.textContent = "\u4ECA\u65E5 " + today.getFullYear() + "-" + String(today.getMonth() + 1).padStart(2, "0") + "-" + String(today.getDate()).padStart(2, "0");
      parent.appendChild(t);
    }
  }
  function renderProgressLine(parent, p, sd, dw, cvH, eventsMap, isTimeMode) {
    if (!(isTimeMode && p.showProgressLine && p.projectStartDate)) return;
    let saved = window.localStorage && window.localStorage.getItem("netplan_progress_date");
    let gw = window;
    let pdStr = saved || (gw._progressDate ? gw._progressDate.getFullYear() + "-" + String(gw._progressDate.getMonth() + 1).padStart(2, "0") + "-" + String(gw._progressDate.getDate()).padStart(2, "0") : null);
    if (!pdStr) return;
    let pd = new Date(pdStr);
    let po = Math.round((pd.getTime() - sd.getTime()) / 864e5);
    if (po < 0) return;
    let px = 80 + po * dw;
    const g = svgEl("g", { id: "net-progress-check" });
    g.appendChild(svgEl("line", {
      x1: px,
      y1: 0,
      x2: px,
      y2: cvH - 28,
      stroke: "#52c41a",
      "stroke-width": 2,
      "stroke-dasharray": "6,3"
    }));
    g.appendChild(svgEl("polygon", {
      points: px - 7 + ",0 " + (px + 7) + ",0 " + px + ",14",
      fill: "#52c41a"
    }));
    const pt = svgEl("text", {
      x: px + 4,
      y: 14,
      "font-size": 11,
      fill: "#52c41a",
      "font-weight": "bold"
    });
    pt.textContent = pdStr;
    g.appendChild(pt);
    parent.appendChild(g);
    Object.keys(eventsMap).forEach(function(eid) {
      let evt = eventsMap[eid];
      if (evt.isVirtual) return;
      if (po >= evt.es && po <= evt.ef) {
        let ratio = evt.ef > evt.es ? (po - evt.es) / (evt.ef - evt.es) : 1;
        let ex = 80 + (evt.es + ratio * (evt.ef - evt.es)) * dw;
        let ey = (evt.y || 0) - 4;
        parent.appendChild(svgEl("polygon", {
          points: ex - 4 + "," + ey + " " + (ex + 4) + "," + ey + " " + ex + "," + (ey - 6),
          fill: "#52c41a"
        }));
      }
    });
  }
  function renderProgressCurve(parent, p, sd, dw, isTimeMode) {
    if (!(p.showProgressCurve && isTimeMode)) return;
    let acts = p.timeParams && p.timeParams.activities || [];
    if (acts.length === 0) return;
    let pts = [];
    let yMin = 120, yMax = 90, yR = yMin - yMax;
    let MARGIN_LEFT_PC = 80;
    for (let d = 0; d < p.totalDays; d++) {
      let dc = 0, dn = 0;
      acts.forEach(function(a) {
        let ae = a.es || 0;
        let af = a.ef || 0;
        if (d >= ae && d < af) {
          dc += a.completion || 0;
          dn++;
        }
      });
      let avg = dn > 0 ? dc / dn : -1;
      if (avg < 0) continue;
      let cx = MARGIN_LEFT_PC + d * dw + dw / 2;
      let cy = yMax + yR * (1 - avg / 100);
      pts.push(cx + "," + cy);
    }
    if (pts.length < 2) return;
    let fX = pts[0].split(",")[0], lX = pts[pts.length - 1].split(",")[0];
    let fillD = "M" + fX + "," + yMin + " L" + pts.join(" L") + " L" + lX + "," + yMin + " Z";
    parent.appendChild(svgEl("path", { d: fillD, fill: "rgba(173,216,230,0.3)", stroke: "none" }));
    parent.appendChild(svgEl("polyline", {
      fill: "none",
      stroke: "#5dade2",
      "stroke-width": 2,
      points: pts.join(" ")
    }));
  }
  function findExtents(eventsMap, totalDays, dw, nr) {
    let lastRowY = 100, rightmostX = 80 + (totalDays || 90) * dw;
    Object.keys(eventsMap).forEach(function(eid) {
      let evt = eventsMap[eid];
      if (evt.y && evt.y > lastRowY) lastRowY = evt.y;
      if (evt.x && evt.x > rightmostX) rightmostX = evt.x;
    });
    return { lastRowY, rightmostX };
  }
  function renderLegend(parent, lastRowY, layerH, totalDuration) {
    let legendTop = lastRowY + layerH + 10;
    parent.appendChild(svgEl("rect", {
      x: 10,
      y: legendTop,
      width: 220,
      height: 95,
      fill: "rgba(255,255,255,0.95)",
      stroke: "#ccc",
      rx: 4
    }));
    const durT = svgEl("text", {
      x: 20,
      y: legendTop + 14,
      "font-size": 12,
      "font-weight": "bold",
      fill: "#e63946"
    });
    durT.textContent = "\u603B\u5DE5\u671F=" + totalDuration + "\u5929";
    parent.appendChild(durT);
    const legT = svgEl("text", {
      x: 20,
      y: legendTop + 29,
      "font-size": 12,
      "font-weight": "bold",
      fill: "#333"
    });
    legT.textContent = "\u56FE\u4F8B";
    parent.appendChild(legT);
    [
      { label: "\u5173\u952E\u7EBF\u8DEF", color: "#ff4d4f", w: 2.5, d: false },
      { label: "\u975E\u5173\u952E\u5DE5\u4F5C", color: "#1890ff", w: 1.5, d: false },
      { label: "\u865A\u5DE5\u4F5C", color: "#52c41a", w: 1, d: true, dw: "6,3" }
    ].forEach(function(it, i) {
      let iy = legendTop + 42 + i * 16;
      parent.appendChild(svgEl("line", {
        x1: 20,
        y1: iy,
        x2: 60,
        y2: iy,
        stroke: it.color,
        "stroke-width": it.w,
        "stroke-dasharray": it.d ? it.dw || "4,3" : "none"
      }));
      const lt = svgEl("text", {
        x: 66,
        y: iy + 4,
        "font-size": 12,
        fill: "#333"
      });
      lt.textContent = it.label;
      parent.appendChild(lt);
    });
  }
  function renderBottomRuler(parent, p, sd, dw, cvW, cvH, isTimeMode) {
    if (!isTimeMode) return;
    let { lastRowY } = findExtents(p.layout.events, p.totalDays, dw, p.nodeRadius || 11);
    let layerH = p.layerHeight || 60;
    let legendTop = lastRowY + layerH + 10;
    let legendBottom = legendTop + 95 + 5;
    const UPPER_H = 28, LOWER_H = 24;
    const MT = 12;
    const FS = 11;
    let rulerY = legendBottom + 8;
    parent.appendChild(svgEl("rect", {
      x: 0,
      y: rulerY,
      width: cvW,
      height: UPPER_H,
      fill: "#fafafa"
    }));
    parent.appendChild(svgEl("rect", {
      x: 0,
      y: rulerY + UPPER_H,
      width: cvW,
      height: LOWER_H,
      fill: "#f5f5f5"
    }));
    for (let d = 0; d <= p.totalDays; d++) {
      let dt = new Date(sd.getTime() + d * 864e5);
      let x = MT + d * dw;
      let dow = dt.getDay();
      let isSat = dow === 6, isSun = dow === 0;
      const t = svgEl("text", {
        x: x + dw / 2,
        y: rulerY + UPPER_H / 2 + 4,
        "text-anchor": "middle",
        "font-size": 10,
        fill: isSat || isSun ? "#ccc" : "#666"
      });
      t.textContent = String(dt.getDate());
      parent.appendChild(t);
    }
    let lastMonth = -1;
    for (let d = 0; d <= p.totalDays; d++) {
      let dt = new Date(sd.getTime() + d * 864e5);
      let x = MT + d * dw;
      if (dt.getDate() === 1 || d === 0) {
        let monthSpan = new Date(dt.getFullYear(), dt.getMonth() + 1, 0).getDate();
        let mw = monthSpan * dw;
        const t = svgEl("text", {
          x: x + mw / 2,
          y: rulerY + UPPER_H + LOWER_H / 2 + 4,
          "text-anchor": "middle",
          "font-size": FS,
          fill: "#333",
          "font-weight": "bold"
        });
        t.textContent = dt.getFullYear() + "-" + String(dt.getMonth() + 1).padStart(2, "0");
        parent.appendChild(t);
        lastMonth = dt.getMonth();
      }
    }
    window._netRulerBottom = rulerY + UPPER_H + LOWER_H;
  }

  // netplan/render/arrows.ts
  function findConnectedEvents(eventId) {
    var ids = [];
    var acts = window._netActivities || [];
    acts.forEach(function(a) {
      if (a.source === eventId || a.target === eventId) ids.push(a.id);
    });
    return ids;
  }
  function calcInOff(actId, tid) {
    var acts = window._netActivities || [];
    var tgts = acts.filter(function(a) {
      return a.target === tid;
    });
    tgts.sort(function(a, b) {
      return a.id - b.id;
    });
    var cnt = tgts.length;
    if (cnt <= 1) return 0;
    var idx = 0;
    tgts.forEach(function(a, i) {
      if (a.id === actId) idx = i;
    });
    var gap = 6;
    return -((cnt - 1) * gap) / 2 + idx * gap;
  }
  function updateArrowPaths(eventId, dx, dy) {
    var ids = findConnectedEvents(eventId);
    var svg = window._netSvg;
    var layout = window._netLayout;
    var offsets = window._netEventOffsets || {};
    if (!svg || !layout) return;
    ids.forEach(function(actId) {
      var g = svg.querySelector('[data-activity-id="' + actId + '"]');
      if (!g) return;
      var sid = g.getAttribute("data-src");
      var tid = g.getAttribute("data-tgt");
      if (!sid || !tid) return;
      var s = layout.events[sid];
      var t = layout.events[tid];
      if (!s || !t) return;
      var nr = 11;
      var sx = s.x + (sid === eventId ? dx : offsets[sid] ? offsets[sid].x : 0);
      var sy = s.y + (sid === eventId ? dy : offsets[sid] ? offsets[sid].y : 0);
      var ex = t.x + (tid === eventId ? dx : offsets[tid] ? offsets[tid].x : 0);
      var ey = t.y + (tid === eventId ? dy : offsets[tid] ? offsets[tid].y : 0);
      ey += calcInOff(actId, tid);
      var pd;
      if (Math.abs(ey - sy) < 5) {
        pd = "M" + sx + " " + sy + " L" + (ex + nr) + " " + ey;
      } else {
        var midX = Math.max(sx + nr + 4, ex - nr - 4);
        pd = "M" + sx + " " + sy + " L" + midX + " " + sy + " L" + midX + " " + ey + " L" + (ex + nr) + " " + ey;
      }
      var p = g.querySelector(".act-arrow");
      if (p) p.setAttribute("d", pd);
      var lx = (sx + ex) / 2;
      var lb = g.querySelector(".act-label");
      if (lb) {
        lb.setAttribute("x", String(lx));
        lb.setAttribute("y", String(sy - nr - 6));
      }
      var du = g.querySelector(".act-dur");
      if (du) {
        du.setAttribute("x", String(lx));
        du.setAttribute("y", String(sy + nr + 10));
      }
    });
  }
  function updateDummyPaths(eventId, dx, dy) {
    var svg = window._netSvg;
    var layout = window._netLayout;
    var offsets = window._netEventOffsets || {};
    if (!svg || !layout) return;
    var dummies = svg.querySelectorAll(".dummy-rel");
    for (var i = 0; i < dummies.length; i++) {
      var g = dummies[i];
      var sid = g.getAttribute("data-dummy-src") || "";
      var tid = g.getAttribute("data-dummy-tgt") || "";
      var s = layout.events[sid];
      var t = layout.events[tid];
      if (!s || !t) continue;
      var nr = window._netNodeRadius || 11;
      var sx = sid === eventId ? s.x + dx : s.x + (offsets[sid] ? offsets[sid].x : 0);
      var sy = sid === eventId ? s.y + dy : s.y + (offsets[sid] ? offsets[sid].y : 0);
      var ex = tid === eventId ? t.x + dx : t.x + (offsets[tid] ? offsets[tid].x : 0);
      var ey = tid === eventId ? t.y + dy : t.y + (offsets[tid] ? offsets[tid].y : 0);
      var pd;
      if (Math.abs(ey - sy) < 2) {
        pd = "M" + sx + " " + sy + " L" + (ex + nr) + " " + ey;
      } else {
        var midX = Math.max(sx + nr + 4, ex - nr - 4);
        pd = "M" + sx + " " + sy + " L" + midX + " " + sy + " L" + midX + " " + ey + " L" + (ex + nr) + " " + ey;
      }
      var p = g.querySelector("path");
      if (p) p.setAttribute("d", pd);
    }
  }

  // netplan/interaction/nodedrag.ts
  var _dayPopup = null;
  function showDayPopup(deltaDays, cx, cy, onConfirm) {
    _hideDayPopup();
    const popup = document.createElement("div");
    popup.className = "day-popup";
    popup.style.cssText = `
    position: fixed; z-index: 10000;
    left: ${cx}px; top: ${Math.max(10, cy)}px;
    background: #fff; border: 1px solid #d9d9d9;
    border-radius: 6px; box-shadow: 0 2px 8px rgba(0,0,0,0.15);
    padding: 12px; min-width: 160px;
  `;
    const label = document.createElement("div");
    label.textContent = `\u504F\u79FB\u5929\u6570 (\u5F53\u524D: ${deltaDays >= 0 ? "+" : ""}${deltaDays})`;
    label.style.cssText = "font-size: 12px; color: #666; margin-bottom: 8px;";
    popup.appendChild(label);
    const input = document.createElement("input");
    input.type = "number";
    input.value = String(deltaDays);
    input.style.cssText = "width: 120px; margin-bottom: 8px; display: block;";
    popup.appendChild(input);
    const btnGroup = document.createElement("div");
    btnGroup.style.cssText = "display: flex; gap: 8px; justify-content: flex-end;";
    const cancelBtn = document.createElement("button");
    cancelBtn.textContent = "\u53D6\u6D88";
    cancelBtn.onclick = () => _hideDayPopup();
    btnGroup.appendChild(cancelBtn);
    const confirmBtn = document.createElement("button");
    confirmBtn.textContent = "\u786E\u5B9A";
    confirmBtn.style.cssText = "background: #1890ff; color: #fff; border: none; padding: 4px 12px; border-radius: 4px;";
    confirmBtn.onclick = () => {
      const val = parseInt(input.value) || 0;
      _hideDayPopup();
      onConfirm(val);
    };
    btnGroup.appendChild(confirmBtn);
    popup.appendChild(btnGroup);
    document.body.appendChild(popup);
    _dayPopup = popup;
    input.focus();
  }
  function _hideDayPopup() {
    if (_dayPopup) {
      document.body.removeChild(_dayPopup);
      _dayPopup = null;
    }
  }

  // netplan/render/network.ts
  function renderNetwork(elementsJson, optsIn) {
    var opts = optsIn;
    if (typeof opts === "string") {
      try {
        opts = JSON.parse(opts);
      } catch (e) {
        opts = {};
      }
    }
    opts = opts || {};
    var mode = opts.mode || window._netMode || "time";
    window._netMode = mode;
    var showCritical = opts.showCritical !== false;
    var showFloat = opts.showFloat !== false;
    var showTodayLine = opts.showTodayLine !== false && mode === "time";
    var showProgressLine = opts.showProgressLine !== false && mode === "time";
    var psd = opts.projectStartDate || (/* @__PURE__ */ new Date()).toISOString().slice(0, 10);
    var totalDays = opts.totalDays || 90;
    var vw = window.innerWidth || document.documentElement.clientWidth || 1920;
    var dayWidth = mode === "logic" ? 80 : opts.dayWidth || 8;
    if (mode !== "logic" && vw < 1400 && dayWidth > 12) {
      dayWidth = Math.max(8, dayWidth * 0.7);
    }
    var pn = opts.projectName || "\u7F51\u7EDC\u8BA1\u5212";
    console.log("[NET] SVG render. opts:", JSON.stringify({ totalDays, dayWidth, showTodayLine, showProgressLine, mode }));
    window._netRendered = false;
    var ce = document.getElementById("cy");
    if (!ce) {
      console.warn("[NET] cy not found, retry in 100ms");
      setTimeout(function() {
        renderNetwork(elementsJson, opts);
      }, 100);
      return;
    }
    var elements;
    try {
      elements = JSON.parse(elementsJson);
    } catch (e) {
      console.error("JSON parse", e);
      return;
    }
    var tasks = [], rels = [];
    elements.forEach(function(el) {
      if (el.data) {
        if (el.data.es !== void 0) tasks.push(el.data);
        if (el.data.source) rels.push(el.data);
      }
    });
    if (!tasks.length) {
      console.warn("[NET] No tasks");
      return;
    }
    console.log("[NET] tasks:", tasks.length, "| rels:", rels.length);
    var tp = calculateTimeParams(tasks, rels);
    try {
      applySingleStartEnd(tp);
    } catch (e) {
      console.error("[NET] applySingleStartEnd failed:", e);
    }
    var layout = calculateVerticalLayout(tp);
    layout.eventIn = {};
    layout.eventOut = {};
    Object.keys(layout.events).forEach(function(eid) {
      layout.eventIn[eid] = (tp.eventPred[eid] || []).length;
      layout.eventOut[eid] = (tp.eventSucc[eid] || []).length;
    });
    var ML = 12;
    var cx;
    if (mode === "logic") {
      var sortedEids = tp.sortedEvents;
      var logicGap = dayWidth * 1.8;
      sortedEids.forEach(function(eid, idx) {
        layout.events[eid].x = ML + idx * logicGap + logicGap / 2;
      });
      cx = ML + sortedEids.length * logicGap + 200;
    } else {
      var maxEs = 0;
      Object.keys(layout.events).forEach(function(eid) {
        layout.events[eid].x = ML + (layout.events[eid].es || 0) * dayWidth;
        if ((layout.events[eid].es || 0) > maxEs) maxEs = layout.events[eid].es;
      });
      var rightMargin = Math.max(totalDays, maxEs) * dayWidth;
      cx = ML + rightMargin + 100;
    }
    var netLayerHeight = opts.layerHeight || 60;
    var netNodeRadius = opts.nodeRadius || 11;
    var layerKeys = Object.keys(layout.layerEvents || {}).map(Number);
    var maxLayerNum = layerKeys.length > 0 ? Math.max.apply(null, layerKeys) : 1;
    var cySize = Math.max(100 + maxLayerNum * netLayerHeight + 60, 400);
    cySize += 300;
    console.log("[NET] SVG initial size:", cx, "x", cySize);
    var layerScale = netLayerHeight / 60;
    if (Math.abs(layerScale - 1) > 0.01) {
      Object.keys(layout.events).forEach(function(eid) {
        layout.events[eid].y = 100 + (layout.events[eid].y - 100) * layerScale;
      });
    }
    var extraSpacing = window._netExtraSpacing || {};
    Object.keys(extraSpacing).forEach(function(key) {
      var extraCount = extraSpacing[key];
      if (!extraCount || extraCount <= 0) return;
      var layerNum = parseInt(key);
      Object.keys(layout.events).forEach(function(eid) {
        var evt = layout.events[eid];
        var layerIdx = Math.round((evt.y - 100) / netLayerHeight);
        if (layerIdx >= layerNum) {
          evt.y += extraCount * netLayerHeight;
        }
      });
    });
    ce.innerHTML = buildNetworkSvg({
      projectStartDate: psd,
      totalDays,
      totalDuration: opts.totalDuration || 0,
      dayWidth,
      timeParams: tp,
      layout,
      mode,
      showCritical,
      showFloat,
      showTodayLine,
      showProgressLine,
      showDummyArrows: opts.showDummyArrows,
      showProgressCurve: opts.showProgressCurve === true,
      showGridH: opts.showGridH === true,
      showGridV: opts.showGridV === true,
      _maxLayer: typeof layout.maxLayer === "number" ? layout.maxLayer : 10,
      _marginTop: 100,
      _pendingOffsets: window._netPendingOffsets || null,
      projectName: pn,
      nodeRadius: netNodeRadius,
      layerHeight: netLayerHeight,
      labelFontSize: opts.labelFontSize || 10,
      labelFields: opts.labelFields || [],
      rowLabels: opts.rowLabels || [],
      restDayPattern: opts.restDayPattern !== false,
      canvasW: cx,
      canvasH: cySize
    });
    var actualH = window._netSvgHeight || cySize;
    if (actualH > cySize) {
      ce.style.height = actualH + "px";
    }
    console.log("[NET] SVG rendered. height:", actualH);
    var svg = ce.querySelector("svg");
    if (svg) {
      svg.ondblclick = function(e) {
        var dotNet = window._netDotNet;
        if (window._netLastPopupTime && Date.now() - window._netLastPopupTime < 500) return;
        if (!dotNet) {
          console.warn("[NET] dblclick: _netDotNet not set");
          return;
        }
        var el = e.target.closest(".net-event");
        if (el) {
          var tid = parseInt(el.getAttribute("data-task-id") || "");
          if (tid) {
            console.log("[NET] dblclick node taskId=", tid);
            dotNet.invokeMethodAsync("OpenTaskEditor", tid).catch(function(e2) {
              console.warn("[NET] invokeMethodAsync error:", e2);
            });
          }
          return;
        }
        var activityEl = e.target.closest("[data-activity-id]");
        if (activityEl) {
          var aid = parseInt(activityEl.getAttribute("data-activity-id") || "");
          if (!isNaN(aid) && aid > 0) {
            console.log("[NET] dblclick activity taskId=", aid);
            dotNet.invokeMethodAsync("OpenTaskEditor", aid).catch(function(e2) {
              console.warn("[NET] invokeMethodAsync error:", e2);
            });
          }
          return;
        }
        var rect = svg.getBoundingClientRect();
        var clickX = e.clientX - rect.left;
        var dayOffset = Math.max(0, Math.round((clickX - 12) / dayWidth));
        console.log("[NET] dblclick blank area, dayOffset=", dayOffset);
        dotNet.invokeMethodAsync("ShowAddTaskModal", 0, dayOffset).catch(function(e2) {
          console.warn("[NET] invokeMethodAsync error:", e2);
        });
      };
    }
    var rowRects = svg ? svg.querySelectorAll(".net-row-bg") : [];
    rowRects.forEach(function(rect) {
      rect.addEventListener("click", function(e) {
        e.stopPropagation();
        var hadSel = rect.classList.contains("net-row-sel");
        rowRects.forEach(function(r) {
          r.classList.remove("net-row-sel");
        });
        if (!hadSel) rect.classList.add("net-row-sel");
      });
    });
    window._netLayout = layout;
    window._netActivities = tp.activities;
    window._netSvg = svg;
    window._netDayWidth = dayWidth;
    var pendingOffsets = window._netPendingOffsets;
    if (pendingOffsets) {
      Object.keys(pendingOffsets).forEach(function(eid) {
        var oldPos = pendingOffsets[eid];
        var evt = layout.events[eid];
        var eventOffsets2 = window._netEventOffsets || {};
        if (evt && eventOffsets2[eid]) {
          var deltaLayoutX = evt.x - oldPos.x;
          var deltaLayoutY = evt.y - oldPos.y;
          eventOffsets2[eid].x -= deltaLayoutX;
          eventOffsets2[eid].y -= deltaLayoutY;
        }
      });
      delete window._netPendingOffsets;
    }
    var eventOffsets = window._netEventOffsets || {};
    Object.keys(eventOffsets).forEach(function(eid) {
      var off = eventOffsets[eid];
      var g = svg && svg.querySelector('[data-event-id="' + eid + '"]');
      if (g && (off.x !== 0 || off.y !== 0)) {
        g.setAttribute("transform", "translate(" + off.x + "," + off.y + ")");
      }
    });
    var hasOffsets = false;
    Object.keys(eventOffsets).forEach(function(eid) {
      var off = eventOffsets[eid];
      if (off.x !== 0 || off.y !== 0) {
        updateArrowPaths(eid, off.x, off.y);
        updateDummyPaths(eid, off.x, off.y);
        hasOffsets = true;
      }
    });
    if (hasOffsets) updateCrossArcOverlays();
    if (svg) {
      svg.addEventListener("mousedown", function(e) {
        var target = e.target;
        console.log("[NetDrag] mousedown tag=" + target.tagName + " class=" + target.className);
        if (target.tagName === "circle") {
          var g = target.closest(".net-event");
          if (!g || !window._netLayout || !window._netLayout.events) return;
          var eid = g.getAttribute("data-event-id") || "";
          var off = eventOffsets[eid] || { x: 0, y: 0 };
          window._netNodeDrag = { eventId: eid, group: g, startX: e.clientX, startY: e.clientY, offX: off.x, offY: off.y, moved: false };
          g.style.cursor = "grabbing";
          e.stopPropagation();
          return;
        }
        var activityPath = target.closest("[data-activity-id]");
        console.log("[NetDrag] activityPath=" + (activityPath ? activityPath.getAttribute("data-activity-id") : "null"));
        if (activityPath) {
          var actId = activityPath.getAttribute("data-activity-id") || "";
          if (!actId) return;
          var sid = activityPath.getAttribute("data-src") || "";
          var tid = activityPath.getAttribute("data-tgt") || "";
          if (!sid || !tid) return;
          var layout2 = window._netLayout;
          if (!layout2 || !layout2.events) return;
          var s = layout2.events[sid], t = layout2.events[tid];
          if (!s || !t) return;
          var sameLayer = Math.abs((t.y || 0) - (s.y || 0)) < 2;
          if (sameLayer) {
            var eY = s.y || 0;
            var layerH = window._netLayerHeight || 60;
            var marginTop = 100;
            var layerIdx = Math.round((eY - marginTop) / layerH);
            var events = layout2.events;
            var eids = Object.keys(events).filter(function(eid2) {
              var evt = events[eid2];
              var l = Math.round(((evt.y || 0) - marginTop) / layerH);
              return l === layerIdx && !evt.isVirtual;
            });
            if (eids.length > 0) {
              window._netRowDrag = {
                startY: e.clientY,
                eids,
                moved: false,
                startOffsets: JSON.parse(JSON.stringify(eventOffsets))
              };
            }
          } else {
            window._netVertDrag = {
              startX: e.clientX,
              startY: e.clientY,
              srcId: sid,
              tgtId: tid,
              actId,
              sX: s.x || 0,
              tX: t.x || 0,
              moved: false
            };
          }
          e.stopPropagation();
          return;
        }
      });
    }
    if (showProgressLine) {
      var plGroup = document.getElementById("net-progress-check");
      if (plGroup && svg) {
        plGroup.onmousedown = function(e) {
          e.preventDefault();
          var bodyEl2 = document.getElementById("network-body");
          window._dragInfo = { startX: e.clientX, startLineX: window._progressX, startScrollLeft: bodyEl2 ? bodyEl2.scrollLeft : 0 };
        };
        plGroup.style.cursor = "ew-resize";
      }
    }
    if (!window._netDragBound) {
      window._netDragBound = true;
      document.addEventListener("mousemove", function(e) {
        var vd = window._netVertDrag;
        if (vd) {
          if (Math.abs(e.clientX - vd.startX) > 3) vd.moved = true;
          return;
        }
        var rd = window._netRowDrag;
        if (rd) {
          var layerH = window._netLayerHeight || 60;
          var rdDy = Math.round((e.clientY - rd.startY) / layerH) * layerH;
          if (Math.abs(e.clientY - rd.startY) > 3) rd.moved = true;
          if (rd.moved) {
            var svgEl3 = document.querySelector("#network-svg");
            rd.eids.forEach(function(eid) {
              var g = svgEl3 ? svgEl3.querySelector('[data-event-id="' + eid + '"]') : null;
              if (g) {
                var origOff = rd.startOffsets[eid] || { x: 0, y: 0 };
                g.setAttribute("transform", "translate(" + origOff.x + "," + (origOff.y + rdDy) + ")");
              }
            });
          }
          return;
        }
        var nd = window._netNodeDrag;
        if (nd) {
          var layerH = window._netLayerHeight || 60;
          var dx = e.clientX - nd.startX + nd.offX;
          var dy = nd.offY + Math.round((e.clientY - nd.startY) / layerH) * layerH;
          if (Math.abs(e.clientX - nd.startX) > 3 || Math.abs(e.clientY - nd.startY) > 3) {
            nd.moved = true;
            nd.group.setAttribute("transform", "translate(" + dx + "," + dy + ")");
          }
          return;
        }
        var dragInfo = window._dragInfo;
        if (!dragInfo) return;
        var pdx = e.clientX - dragInfo.startX;
        var bodyEl2 = document.getElementById("network-body");
        var scrollDelta = (bodyEl2 ? bodyEl2.scrollLeft : 0) - dragInfo.startScrollLeft;
        var newX = dragInfo.startLineX + pdx + scrollDelta;
        var minX = 12, maxX = parseFloat(svg.getAttribute("width") || "800") - 100;
        newX = Math.max(minX, Math.min(newX, maxX));
        var checkGroup = document.getElementById("net-progress-check");
        var line = checkGroup ? checkGroup.querySelector("line") : null;
        var handle = checkGroup ? checkGroup.querySelector("polygon") : null;
        var label = checkGroup ? checkGroup.querySelector("text") : null;
        if (line) {
          line.setAttribute("x1", String(newX));
          line.setAttribute("x2", String(newX));
        }
        if (handle) handle.setAttribute("points", newX - 7 + ",0 " + (newX + 7) + ",0 " + newX + ",14");
        if (label) {
          label.setAttribute("x", String(newX + 4));
        }
        if (window._projStartDate) {
          var dayOffset = Math.round((newX - 80) / dayWidth);
          var pd = new Date(window._projStartDate);
          pd.setDate(pd.getDate() + dayOffset);
          window._progressCheckDate = pd;
          window._progressDate = pd;
          window._progressX = newX;
          var cl = pd.getFullYear() + "-" + String(pd.getMonth() + 1).padStart(2, "0") + "-" + String(pd.getDate()).padStart(2, "0");
          localStorage.setItem("netplan_progress_date", cl);
          if (label) label.textContent = cl;
        }
        if (typeof window.updateProgressColors === "function") window.updateProgressColors();
      });
      document.addEventListener("mouseup", function(e) {
        window._dragInfo = null;
        var vd = window._netVertDrag;
        if (vd) {
          delete window._netVertDrag;
          if (vd.moved) {
            var dayWidth2 = window._netDayWidth || 8;
            var vdx = e.clientX - vd.startX;
            var deltaDays = Math.round(vdx / dayWidth2);
            if (deltaDays !== 0) {
              var offsets = window._netEventOffsets || {};
              var srcOff = offsets[vd.srcId] || { x: 0, y: 0 };
              offsets[vd.srcId] = { x: srcOff.x + deltaDays * dayWidth2, y: srcOff.y || 0 };
              updateArrowPaths(vd.srcId, offsets[vd.srcId].x, offsets[vd.srcId].y);
              updateDummyPaths(vd.srcId, offsets[vd.srcId].x, offsets[vd.srcId].y);
              updateCrossArcOverlays();
            }
          }
          return;
        }
        var rd = window._netRowDrag;
        if (rd) {
          delete window._netRowDrag;
          if (rd.moved) {
            var layerH = window._netLayerHeight || 60;
            var rdDy = Math.round((e.clientY - rd.startY) / layerH) * layerH;
            if (rdDy !== 0) {
              var offsets = window._netEventOffsets || {};
              var layout2 = window._netLayout;
              rd.eids.forEach(function(eid2) {
                var origOff = rd.startOffsets[eid2] || { x: 0, y: 0 };
                offsets[eid2] = { x: origOff.x || 0, y: (origOff.y || 0) + rdDy };
                updateArrowPaths(eid2, offsets[eid2].x, offsets[eid2].y);
                updateDummyPaths(eid2, offsets[eid2].x, offsets[eid2].y);
              });
              updateCrossArcOverlays();
            }
          }
          return;
        }
        var nd = window._netNodeDrag;
        if (nd) {
          if (nd.group) nd.group.style.cursor = "";
          var dx = e.clientX - nd.startX + nd.offX;
          var dy = nd.offY;
          if (nd.startY !== void 0) {
            var layerH = window._netLayerHeight || 60;
            dy = nd.offY + Math.round((e.clientY - nd.startY) / layerH) * layerH;
          }
          if (nd.moved && (dx !== 0 || dy !== 0)) {
            var eid = nd.eventId;
            var offsets = window._netEventOffsets || {};
            var layout2 = window._netLayout;
            var evt = layout2 && layout2.events ? layout2.events[eid] : null;
            if (evt) {
              var dayWidth2 = window._netDayWidth || 8;
              var deltaDays = Math.round(dx / dayWidth2);
              if (deltaDays !== 0 || dy !== 0) {
                offsets[eid] = { x: deltaDays * dayWidth2, y: dy };
                var pending = window._netPendingOffsets || {};
                pending[eid] = { x: evt.x, y: evt.y };
                window._netPendingOffsets = pending;
                var existingOffXDays = Math.round((offsets[eid] ? offsets[eid].x : 0) / dayWidth2);
                window._netLastPopupTime = Date.now();
                showDayPopup(deltaDays, e.clientX, e.clientY, function(confirmedDays) {
                  window._netLastPopupTime = Date.now();
                  var finalX = confirmedDays * dayWidth2;
                  if (confirmedDays === 0) {
                    if (nd.group) nd.group.removeAttribute("transform");
                    delete offsets[eid];
                    delete pending[eid];
                    return;
                  }
                  offsets[eid] = { x: finalX, y: dy };
                  if (nd.group) {
                    nd.group.setAttribute("transform", "translate(" + finalX + "," + dy + ")");
                  }
                  updateArrowPaths(eid, finalX, dy);
                  updateDummyPaths(eid, finalX, dy);
                  updateCrossArcOverlays();
                  var netDelta = confirmedDays - existingOffXDays;
                  if (netDelta !== 0) {
                    var dotNet = window._netDotNet;
                    if (dotNet && eid) {
                      var tid = evt.taskId || parseInt(eid.replace("T", ""));
                      if (tid && tid > 0) {
                        dotNet.invokeMethodAsync("SyncNodeDrag", tid, netDelta, 0).catch(function(err) {
                          console.warn("[NET] SyncNodeDrag error:", err);
                        });
                      }
                    }
                  }
                  existingOffXDays = confirmedDays;
                });
              }
            }
          } else {
            var eid = nd.eventId;
            var offsets = window._netEventOffsets || {};
            if (offsets[eid]) {
              delete offsets[eid];
              if (nd.group) {
                nd.group.removeAttribute("transform");
              }
            }
          }
          delete window._netNodeDrag;
        }
      });
    }
    setTimeout(function() {
      if (typeof window.updateProgressColors === "function") window.updateProgressColors();
    }, 200);
    var hscroll = document.getElementById("network-hscroll");
    var hscrollInner = document.getElementById("network-hscroll-inner");
    var bodyEl = document.getElementById("network-body");
    if (hscrollInner) hscrollInner.style.width = cx + "px";
    var _hSyncing = false;
    if (hscroll && bodyEl) {
      hscroll.onscroll = function() {
        if (_hSyncing) return;
        _hSyncing = true;
        bodyEl.scrollLeft = hscroll.scrollLeft;
        _hSyncing = false;
      };
      bodyEl.onscroll = function() {
        if (_hSyncing) return;
        _hSyncing = true;
        hscroll.scrollLeft = bodyEl.scrollLeft;
        _hSyncing = false;
      };
    }
    window._netRendered = true;
    window._networkData = { events: layout.events, activities: tp.activities, relations: tp.relations };
    window._networkOpts = opts;
    validateNetworkRender();
    var netBody = document.getElementById("network-body");
    if (netBody && !window._panBound) {
      window._panBound = true;
      var panState = null;
      netBody.addEventListener("mousedown", function(e) {
        if (e.target.closest(".net-event") || e.target.closest("[data-activity-id]")) return;
        if (e.target.closest("#net-progress-line") || e.target.closest("#net-progress-check")) return;
        panState = { x: e.clientX, y: e.clientY, sx: netBody.scrollLeft, sy: netBody.scrollTop };
        netBody.style.cursor = "grabbing";
      });
      window.addEventListener("mousemove", function(e) {
        if (!panState) return;
        netBody.scrollLeft = panState.sx - (e.clientX - panState.x);
        netBody.scrollTop = panState.sy - (e.clientY - panState.y);
      });
      window.addEventListener("mouseup", function() {
        if (!panState) return;
        netBody.style.cursor = "";
        panState = null;
      });
    }
  }
  function validateNetworkRender() {
    var errors = [];
    var svg = document.getElementById("cy")?.querySelector("svg");
    if (!svg) {
      errors.push("SVG\u5143\u7D20\u4E0D\u5B58\u5728");
    }
    if (!window._netLayout || !window._netActivities) errors.push("_netLayout/_netActivities \u4E3A\u7A7A");
    if (!window._netDotNet) errors.push("_netDotNet \u672A\u8BBE\u7F6E");
    if (errors.length > 0) {
      console.warn("[VALIDATE] " + errors.length + " issue(s):");
      errors.forEach(function(e) {
        console.warn("  [VALIDATE] " + e);
      });
    } else {
      console.log("[VALIDATE] All checks passed");
    }
  }

  // netplan/utils/dom.ts
  function downloadSVGFile(svgEl3, filename) {
    if (!svgEl3) return;
    const content = new XMLSerializer().serializeToString(svgEl3);
    const blob = new Blob([content], { type: "image/svg+xml;charset=utf-8" });
    const url = URL.createObjectURL(blob);
    triggerDownload(url, filename);
    URL.revokeObjectURL(url);
  }
  function exportSvgAsPng(svgEl3, filename, scale = 2) {
    if (!svgEl3) return;
    const svgData = new XMLSerializer().serializeToString(svgEl3);
    const svgW = svgEl3.getAttribute("width") || "800";
    const svgH = svgEl3.getAttribute("height") || "600";
    const blob = new Blob([svgData], { type: "image/svg+xml;charset=utf-8" });
    const url = URL.createObjectURL(blob);
    const canvas = document.createElement("canvas");
    canvas.width = parseFloat(String(svgW)) * scale;
    canvas.height = parseFloat(String(svgH)) * scale;
    const ctx = canvas.getContext("2d");
    const img = new Image();
    img.onload = () => {
      ctx?.drawImage(img, 0, 0, canvas.width, canvas.height);
      canvas.toBlob((pngBlob) => {
        if (pngBlob) {
          const pngUrl = URL.createObjectURL(pngBlob);
          triggerDownload(pngUrl, filename);
          URL.revokeObjectURL(pngUrl);
        }
      }, "image/png");
      URL.revokeObjectURL(url);
    };
    img.src = url;
  }
  function triggerDownload(url, filename) {
    const a = document.createElement("a");
    a.href = url;
    a.download = filename;
    document.body.appendChild(a);
    a.click();
    document.body.removeChild(a);
  }
  function getSvg(containerId) {
    if (containerId) {
      return document.querySelector(`#${containerId} svg`);
    }
    return document.querySelector("svg.network-svg");
  }

  // netplan/render/progress.ts
  function updateProgressColors(events, progressCheckDate, _projectStartDate) {
    if (!progressCheckDate) return;
    console.log("updateProgressColors called with", events.length, "events");
  }

  // netplan/interaction/panzoom.ts
  var _syncLock = false;
  function syncGanttScrollById() {
    const rightDiv = document.getElementById("gantt-right");
    const leftBody = document.getElementById("gantt-left-body");
    if (rightDiv && leftBody) {
      leftBody.scrollTop = rightDiv.scrollTop;
    }
  }
  function initGanttScroll() {
    const tryInit = (attempt = 1) => {
      if (attempt > 50) return;
      const rightDiv = document.getElementById("gantt-right");
      const leftBody = document.getElementById("gantt-left-body");
      if (!rightDiv || !leftBody) {
        setTimeout(() => tryInit(attempt + 1), 100);
        return;
      }
      rightDiv.addEventListener("scroll", () => {
        if (_syncLock) return;
        _syncLock = true;
        leftBody.scrollTop = rightDiv.scrollTop;
        _syncLock = false;
      });
      leftBody.addEventListener("scroll", () => {
        if (_syncLock) return;
        _syncLock = true;
        rightDiv.scrollTop = leftBody.scrollTop;
        _syncLock = false;
      });
    };
    tryInit();
  }
  function networkFit(containerId = "network-body") {
    const body = document.getElementById(containerId);
    const svg = body?.querySelector("svg");
    if (!body || !svg) return;
    const vw = body.clientWidth;
    const sw = parseFloat(svg.getAttribute("width") || "800");
    if (sw > 0) {
      const scale = Math.min(vw / sw, 1);
      body.style.zoom = String(scale * 100) + "%";
    }
  }

  // netplan/interaction/export.ts
  function downloadSvg(filename = "network.svg") {
    const svg = getSvg();
    downloadSVGFile(svg, filename);
  }
  function exportPng(filename = "network.png") {
    const svg = getSvg();
    exportSvgAsPng(svg, filename);
  }
  function printSvg(printZoomVal = "100%") {
    const svg = getSvg();
    if (!svg) return;
    const zoom = parseFloat(printZoomVal) / 100;
    const origW = svg.getAttribute("width") || "";
    const origH = svg.getAttribute("height") || "";
    const w = parseFloat(origW) || 800;
    const h = parseFloat(origH) || 600;
    svg.setAttribute("width", String(w * zoom));
    svg.setAttribute("height", String(h * zoom));
    const content = new XMLSerializer().serializeToString(svg);
    const pw = window.open("", "_blank");
    if (pw) {
      pw.document.write(`<html><head><title>\u6253\u5370</title>
      <style>body{margin:0;display:flex;justify-content:center;align-items:center;min-height:100vh}</style>
      </head><body>${content}</body></html>`);
      pw.document.close();
      pw.focus();
      setTimeout(() => {
        pw.print();
        svg.setAttribute("width", origW);
        svg.setAttribute("height", origH);
      }, 500);
    }
  }

  // netplan/gantt/sync-rows.ts
  function syncGanttRowHeights() {
    const leftRows = document.querySelectorAll("#gantt-left-body .gantt-left-row");
    const bars = document.querySelectorAll("#gantt-right .gantt-bar-frame");
    const gridLines = document.querySelector("#gantt-right .gantt-hgrid");
    const relationSvg = document.querySelector("#gantt-right .gantt-relation-lines");
    const barsContainer = document.querySelector("#gantt-right .gantt-bars");
    const gridLinesContainer = document.querySelector("#gantt-right .gantt-grid-lines");
    if (!leftRows.length || !bars.length || !barsContainer) return;
    const rowTops = [];
    const rowHeights = [];
    let cumY = 0;
    leftRows.forEach((row) => {
      rowTops.push(cumY);
      const h = row.offsetHeight;
      rowHeights.push(h);
      cumY += h;
    });
    const totalHeight = cumY;
    barsContainer.style.height = totalHeight + "px";
    if (gridLinesContainer) {
      gridLinesContainer.style.height = totalHeight + "px";
    }
    if (gridLines) {
      gridLines.style.height = totalHeight + "px";
      let lineIndex = 0;
      gridLines.querySelectorAll("line").forEach((line) => {
        const r = lineIndex + 1;
        if (r < rowTops.length) {
          line.setAttribute("y1", String(rowTops[r]));
          line.setAttribute("y2", String(rowTops[r]));
        }
        lineIndex++;
      });
    }
    // 绘制行背景（隔行变色）和水平分隔线
    var hlines = barsContainer.querySelector('.gantt-hlines');
    if (!hlines) {
      hlines = document.createElement('div');
      hlines.className = 'gantt-hlines';
      barsContainer.appendChild(hlines);
    }
    hlines.innerHTML = '';
    for (var ri = 0; ri < rowTops.length; ri++) {
      var top = rowTops[ri];
      var h = rowHeights[ri];
      if (ri % 2 === 1) {
        var bg = document.createElement('div');
        bg.className = 'gantt-hline-bg';
        bg.style.top = top + 'px';
        bg.style.height = h + 'px';
        hlines.appendChild(bg);
      }
      if (ri > 0) {
        var line = document.createElement('div');
        line.className = 'gantt-hline';
        line.style.top = (top - 0.5) + 'px';
        hlines.appendChild(line);
      }
    }

    if (relationSvg) {
      relationSvg.style.height = totalHeight + "px";
      const todayLine = relationSvg.querySelector(".gantt-today-line");
      if (todayLine) {
        todayLine.setAttribute("y2", String(totalHeight));
      }
      const progressLine = relationSvg.querySelector(".gantt-progress-line");
      if (progressLine) {
        const pointsStr = progressLine.getAttribute("points") || "";
        const pts = pointsStr.trim().split(/\s+/).map((p) => {
          const [x, y] = p.split(",").map(Number);
          return { x, y };
        });
        const newPoints = pts.map((pt, i) => {
          if (i < rowHeights.length) {
            return `${pt.x.toFixed(1)},${(rowTops[i] + rowHeights[i] / 2).toFixed(1)}`;
          }
          return `${pt.x.toFixed(1)},${pt.y.toFixed(1)}`;
        });
        progressLine.setAttribute("points", newPoints.join(" "));
      }
      const progressDots = relationSvg.querySelectorAll(".gantt-progress-point");
      progressDots.forEach((dot, i) => {
        if (i < rowHeights.length) {
          dot.setAttribute("cy", (rowTops[i] + rowHeights[i] / 2).toFixed(1));
        }
      });
      // 先更新条形位置（确保关系线坐标正确）
      bars.forEach((bar) => {
        const taskId = bar.getAttribute("data-task-id");
        if (taskId === null) return;
        const leftRow = document.querySelector(`#gantt-left-body .gantt-left-row[data-task-id="${taskId}"]`);
        if (!leftRow) return;
        const rowEls = Array.from(leftRows);
        const idx = rowEls.indexOf(leftRow);
        if (idx < 0 || idx >= rowTops.length) return;
        bar.style.top = rowTops[idx] + "px";
        bar.style.height = rowHeights[idx] + "px";
      });

      // 再更新关系线 X/Y（条形已在正确位置）
      const relationGroups = relationSvg.querySelectorAll("[data-relation-pred][data-relation-succ]");
      relationGroups.forEach((group) => {
        const predId = group.getAttribute("data-relation-pred");
        const succId = group.getAttribute("data-relation-succ");
        if (!predId || !succId) return;

        const predBar = document.querySelector(`#gantt-right .gantt-bar-frame[data-task-id="${predId}"]`);
        const succBar = document.querySelector(`#gantt-right .gantt-bar-frame[data-task-id="${succId}"]`);

        const predRow = document.querySelector(`#gantt-left-body .gantt-left-row[data-task-id="${predId}"]`);
        const succRow = document.querySelector(`#gantt-left-body .gantt-left-row[data-task-id="${succId}"]`);
        if (!predRow || !succRow) return;
        const rowEls = Array.from(leftRows);
        const predIdx = rowEls.indexOf(predRow);
        const succIdx = rowEls.indexOf(succRow);
        if (predIdx < 0 || succIdx < 0) return;
        const predMid = rowTops[predIdx] + rowHeights[predIdx] / 2;
        const succMid = rowTops[succIdx] + rowHeights[succIdx] / 2;

        // 从条形 DOM 计算 X 端点
        let barEdgeX = 0, endX = 0;
        if (predBar && succBar) {
          const predLeft = parseFloat(predBar.style.left) || 0;
          const predW = parseFloat(predBar.style.width) || 0;
          const succLeft = parseFloat(succBar.style.left) || 0;
          barEdgeX = predLeft + predW;
          endX = succLeft;
        }

        const lineEl = group.querySelector("line");
        if (lineEl) {
          lineEl.setAttribute("x1", barEdgeX.toFixed(1));
          lineEl.setAttribute("y1", predMid.toFixed(1));
          lineEl.setAttribute("x2", endX.toFixed(1));
          lineEl.setAttribute("y2", succMid.toFixed(1));
          lineEl.setAttribute("stroke-width", "0.5");
          lineEl.setAttribute("stroke-dasharray", "3,3");
        }
        const pathEl = group.querySelector("path");
        if (pathEl) {
          const newD = `M ${barEdgeX.toFixed(1)} ${predMid.toFixed(1)} L ${barEdgeX.toFixed(1)} ${succMid.toFixed(1)} L ${endX.toFixed(1)} ${succMid.toFixed(1)}`;
          pathEl.setAttribute("d", newD);
          pathEl.setAttribute("stroke-width", "0.5");
          pathEl.setAttribute("stroke-dasharray", "3,3");
        }
      });
    }
  }

  // 延迟版同步：等待浏览器 layout 完成后再执行（解决 Blazor 渲染后 DOM 未稳定的问题）
  // 双 rAF + 防抖：确保 Blazor Server SignalR 批量 DOM 更新 + layout 全部完成后执行
  var _syncPending = false;
  function syncGanttRowHeightsDeferred() {
    if (_syncPending) return;
    _syncPending = true;
    requestAnimationFrame(function() {
      requestAnimationFrame(function() {
        _syncPending = false;
        syncGanttRowHeights();
      });
    });
  }

  // netplan/gantt/binding.ts
  var _ganttDotNet = null;
  function setGanttDotNet(ref) {
    _ganttDotNet = ref;
    window._ganttDotNet = ref;
  }
  function initPanelResize() {
    const tryInit = (attempt = 1) => {
      if (attempt > 50) return;
      const handle = document.getElementById("gantt-resize-handle");
      const left = document.querySelector(".gantt-left");
      if (!handle || !left) {
        setTimeout(() => tryInit(attempt + 1), 100);
        return;
      }
      setupResize(handle, left);
      initColumnResizeInternal();
    };
    tryInit();
  }
  function initColumnResizeInternal() {
    document.querySelectorAll(".col-resize-handle").forEach(function(h) {
      if (h.getAttribute("data-inited")) return;
      h.setAttribute("data-inited", "1");
      var headerCell = h.parentElement;
      var startX = 0, startW = 0;
      h.addEventListener("mousedown", function(e) {
        e.preventDefault();
        e.stopPropagation();
        startX = e.clientX;
        startW = headerCell.offsetWidth;
        document.body.style.cursor = "col-resize";
        document.body.style.userSelect = "none";
        var onMouseMove = function(me) {
          var delta = me.clientX - startX;
          var newW = Math.max(40, startW + delta);
          headerCell.style.width = newW + "px";
          var cls = headerCell.className.split(" ").filter(function(c) {
            return c.indexOf("col-") === 0;
          })[0];
          if (cls) {
            document.querySelectorAll(".gantt-left-row ." + cls).forEach(function(cell) {
              cell.style.width = newW + "px";
            });
          }
        };
        var onMouseUp = function() {
          document.removeEventListener("mousemove", onMouseMove);
          document.removeEventListener("mouseup", onMouseUp);
          document.body.style.cursor = "";
          document.body.style.userSelect = "";
          requestAnimationFrame(() => syncGanttRowHeights());
        };
        document.addEventListener("mousemove", onMouseMove);
        document.addEventListener("mouseup", onMouseUp);
      });
    });
  }
  function setupResize(handle, leftPanel) {
    let startX = 0, startW = 0;
    handle.addEventListener("mousedown", (e) => {
      startX = e.clientX;
      startW = leftPanel.offsetWidth;
      document.body.style.cursor = "col-resize";
      document.body.style.userSelect = "none";
      handle.classList.add("active");
      const onMouseMove = (me) => {
        const delta = me.clientX - startX;
        let newWidth = startW + delta;
        const minWidth = 280;
        const maxWidth = Math.max(minWidth + 100, window.innerWidth * 0.6);
        newWidth = Math.max(minWidth, Math.min(maxWidth, newWidth));
        leftPanel.style.width = `${newWidth}px`;
      };
      const onMouseUp = () => {
        document.removeEventListener("mousemove", onMouseMove);
        document.removeEventListener("mouseup", onMouseUp);
        document.body.style.cursor = "";
        document.body.style.userSelect = "";
        handle.classList.remove("active");
        requestAnimationFrame(() => syncGanttRowHeights());
      };
      document.addEventListener("mousemove", onMouseMove);
      document.addEventListener("mouseup", onMouseUp);
    });
  }
  function loadDraft(key) {
    try {
      return localStorage.getItem(`gantt_draft_${key}`);
    } catch {
      return null;
    }
  }
  function saveDraft(key, json) {
    try {
      localStorage.setItem(`gantt_draft_${key}`, json);
    } catch {
    }
  }
  function clearDraft(key) {
    try {
      localStorage.removeItem(`gantt_draft_${key}`);
    } catch {
    }
  }

  // netplan/charts/resource.ts
  function initResourceChart(chartData) {
    const canvas = document.getElementById("resource-chart");
    if (!canvas) return;
    const ctx = canvas.getContext("2d");
    if (!ctx) return;
    const oldChart = window._resourceChart;
    if (oldChart) {
      oldChart.destroy();
    }
    const ChartCtor = window.Chart;
    if (!ChartCtor) {
      console.warn("[ResourceChart] Chart.js not loaded");
      return;
    }
    var labels = chartData.labels || [];
    var lines = chartData.lines || [];
    var datasets = chartData.datasets || [];
    if (datasets.length === 0 && lines.length > 0) {
      datasets = lines.map(function(line, i) {
        return {
          label: line.name || `\u8D44\u6E90 ${i + 1}`,
          data: line.data || line.points || [],
          backgroundColor: line.color || `hsla(${i * 60}, 60%, 70%, 0.2)`,
          borderColor: line.color || `hsl(${i * 60}, 60%, 50%)`,
          borderWidth: 2,
          fill: false,
          tension: 0.3
        };
      });
    }
    const chart = new ChartCtor(ctx, {
      type: "line",
      data: {
        labels,
        datasets
      },
      options: {
        responsive: true,
        maintainAspectRatio: false,
        scales: {
          x: { stacked: false, title: { display: true, text: "\u65F6\u95F4" } },
          y: { stacked: false, beginAtZero: true, title: { display: true, text: "\u6295\u5165\u91CF" } }
        },
        plugins: {
          legend: { position: "top", align: "start", labels: { boxWidth: 12, padding: 8 } }
        }
      }
    });
    window._resourceChart = chart;
  }

  // netplan/charts/analysis.ts
  function renderAnalysisBarChart(canvasId, chartData) {
    const canvas = document.getElementById(canvasId);
    if (!canvas) return;
    const ctx = canvas.getContext("2d");
    if (!ctx) return;
    const oldCharts = window._analysisCharts || {};
    if (oldCharts[canvasId]) {
      oldCharts[canvasId].destroy();
    }
    const ChartCtor = window.Chart;
    if (!ChartCtor) {
      console.warn("[AnalysisChart] Chart.js not loaded");
      return;
    }
    var datasets = (chartData.datasets || []).map((ds, i) => ({
      label: ds.label || `\u7CFB\u5217 ${i + 1}`,
      data: ds.data || [],
      backgroundColor: ds.backgroundColor || "#1890ff",
      borderColor: ds.borderColor || "#1890ff",
      borderWidth: 1,
      ...ds
    }));
    const chart = new ChartCtor(ctx, {
      type: "bar",
      data: {
        labels: chartData.labels || [],
        datasets
      },
      options: {
        responsive: true,
        maintainAspectRatio: false,
        scales: {
          y: { beginAtZero: true }
        },
        plugins: {
          legend: { position: "bottom" }
        }
      }
    });
    if (chart) {
      if (!window._analysisCharts) window._analysisCharts = {};
      window._analysisCharts[canvasId] = chart;
    }
  }

  // netplan/charts/analysis.ts (continued)
  function renderEVMChart(canvasId, chartData) {
    if (!canvasId || !chartData) return;
    const canvas = document.getElementById(canvasId);
    if (!canvas) return;
    const ctx = canvas.getContext("2d");
    if (!ctx) return;
    try {
      if (!window._analysisCharts) window._analysisCharts = {};
      var oldChart = window._analysisCharts[canvasId];
      if (oldChart) {
        oldChart.destroy();
      }
      const ChartCtor = window.Chart;
      if (!ChartCtor) {
        console.warn("[EVMChart] Chart.js not loaded");
        return;
      }
      var chart = new ChartCtor(ctx, {
        type: "line",
        data: {
          labels: chartData.labels || [],
          datasets: [
            {
              label: "\u8BA1\u5212\u4EF7\u503CPV",
              data: chartData.pvData || [],
              borderColor: "#1890ff",
              borderDash: [5, 3],
              fill: false,
              tension: 0.2
            },
            {
              label: "\u631F\u503CEV",
              data: chartData.evData || [],
              borderColor: "#52c41a",
              backgroundColor: "rgba(82,196,26,0.1)",
              fill: true,
              tension: 0.2,
              borderWidth: 2
            }
          ]
        },
        options: {
          responsive: true,
          maintainAspectRatio: false,
          scales: {
            y: { beginAtZero: true }
          },
          plugins: {
            legend: { position: "bottom" }
          }
        }
      });
      window._analysisCharts[canvasId] = chart;
    } catch (e) {
      console.error("[EVMChart] render error:", e);
    }
  }
  function renderSPITrendChart(canvasId, chartData) {
    if (!canvasId || !chartData) return;
    const canvas = document.getElementById(canvasId);
    if (!canvas) return;
    const ctx = canvas.getContext("2d");
    if (!ctx) return;
    try {
      if (!window._analysisCharts) window._analysisCharts = {};
      var oldChart = window._analysisCharts[canvasId];
      if (oldChart) {
        oldChart.destroy();
      }
      const ChartCtor = window.Chart;
      if (!ChartCtor) {
        console.warn("[SPITrendChart] Chart.js not loaded");
        return;
      }
      var spiData = chartData.spiData || [];
      var yMin = spiData.length > 0 ? Math.min(0.5, Math.min.apply(null, spiData)) : 0.5;
      var chart = new ChartCtor(ctx, {
        type: "line",
        data: {
          labels: chartData.labels || [],
          datasets: [
            {
              label: "SPI",
              data: spiData,
              borderColor: "#faad14",
              backgroundColor: "rgba(250,173,20,0.1)",
              fill: true,
              tension: 0.2,
              pointRadius: 4,
              pointBackgroundColor: spiData.map(function(v) {
                return v >= 1 ? "#52c41a" : "#ff4d4f";
              })
            }
          ]
        },
        options: {
          responsive: true,
          maintainAspectRatio: false,
          scales: {
            y: {
              beginAtZero: false,
              min: yMin,
              title: { display: true, text: "SPI" }
            }
          },
          plugins: {
            legend: { position: "bottom" }
          }
        },
        plugins: [
          {
            id: "spiReferenceLine",
            afterDraw: function(chart) {
              var ctx2 = chart.ctx;
              var yScale = chart.scales["y"];
              if (!yScale) return;
              var y = yScale.getPixelForValue(1);
              if (y === void 0 || y === null) return;
              ctx2.save();
              ctx2.beginPath();
              ctx2.moveTo(chart.chartArea.left, y);
              ctx2.lineTo(chart.chartArea.right, y);
              ctx2.strokeStyle = "#ff4d4f";
              ctx2.lineWidth = 1;
              ctx2.setLineDash([5, 5]);
              ctx2.stroke();
              ctx2.fillStyle = "#ff4d4f";
              ctx2.font = "10px sans-serif";
              ctx2.fillText("SPI=1.0", chart.chartArea.right - 40, y - 5);
              ctx2.restore();
            }
          }
        ]
      });
      window._analysisCharts[canvasId] = chart;
    } catch (e) {
      console.error("[SPITrendChart] render error:", e);
    }
  }
  function renderProgressCurveChart(canvasId, chartData) {
    if (!canvasId || !chartData) return;
    const canvas = document.getElementById(canvasId);
    if (!canvas) return;
    const ctx = canvas.getContext("2d");
    if (!ctx) return;
    try {
      if (!window._analysisCharts) window._analysisCharts = {};
      var oldChart = window._analysisCharts[canvasId];
      if (oldChart) {
        oldChart.destroy();
      }
      const ChartCtor = window.Chart;
      if (!ChartCtor) {
        console.warn("[ProgressCurveChart] Chart.js not loaded");
        return;
      }
      var labels = chartData.labels || [];
      var progressDate = chartData.progressDate || "";
      var chart = new ChartCtor(ctx, {
        type: "line",
        data: {
          labels: labels,
          datasets: [
            {
              label: "\u8BA1\u5212\u7D2F\u8BA1%",
              data: chartData.plannedData || [],
              borderColor: "#1890ff",
              borderDash: [4, 2],
              fill: false,
              tension: 0.3,
              borderWidth: 2
            },
            {
              label: "\u5B9E\u9645\u7D2F\u8BA1%",
              data: chartData.actualData || [],
              borderColor: "#ff4d4f",
              fill: false,
              tension: 0.3,
              borderWidth: 2.5,
              pointRadius: 2
            }
          ]
        },
        options: {
          responsive: true,
          maintainAspectRatio: false,
          scales: {
            x: {
              ticks: {
                maxTicksLimit: 12,
                callback: function(value, index, values) {
                  var label = labels[index];
                  if (!label) return "";
                  var parts = label.split("-");
                  return parts.length >= 2 ? parts[1] + "\u6708" : label;
                }
              }
            },
            y: {
              beginAtZero: true,
              max: 100,
              title: { display: true, text: "\u7D2F\u8BA1\u5B8C\u6210%" }
            }
          },
          plugins: {
            legend: { position: "bottom" }
          }
        },
        plugins: [
          {
            id: "progressDateLine",
            afterDraw: function(chart) {
              if (!progressDate) return;
              var xScale = chart.scales["x"];
              if (!xScale) return;
              var x = xScale.getPixelForValue(progressDate);
              if (x === void 0 || x === null) return;
              var ctx2 = chart.ctx;
              ctx2.save();
              ctx2.beginPath();
              ctx2.moveTo(x, chart.chartArea.top);
              ctx2.lineTo(x, chart.chartArea.bottom);
              ctx2.strokeStyle = "#ff4d4f";
              ctx2.lineWidth = 1.5;
              ctx2.setLineDash([4, 3]);
              ctx2.stroke();
              ctx2.fillStyle = "#ff4d4f";
              ctx2.font = "10px sans-serif";
              ctx2.fillText(progressDate, x + 4, chart.chartArea.top + 14);
              ctx2.restore();
            }
          }
        ]
      });
      window._analysisCharts[canvasId] = chart;
    } catch (e) {
      console.error("[ProgressCurveChart] render error:", e);
    }
  }
  function renderVarianceChart(canvasId, chartData) {
    if (!canvasId || !chartData) return;
    const canvas = document.getElementById(canvasId);
    if (!canvas) return;
    const ctx = canvas.getContext("2d");
    if (!ctx) return;
    try {
      if (!window._analysisCharts) window._analysisCharts = {};
      var oldChart = window._analysisCharts[canvasId];
      if (oldChart) {
        oldChart.destroy();
      }
      const ChartCtor = window.Chart;
      if (!ChartCtor) {
        console.warn("[VarianceChart] Chart.js not loaded");
        return;
      }
      var data = chartData.data || [];
      var bgColors = data.map(function(d) {
        return d > 0 ? "#52c41a" : d < 0 ? "#ff4d4f" : "#d9d9d9";
      });
      var chart = new ChartCtor(ctx, {
        type: "bar",
        data: {
          labels: chartData.labels || [],
          datasets: [
            {
              label: "\u504F\u5DEE\u5929\u6570",
              data: data,
              backgroundColor: bgColors
            }
          ]
        },
        options: {
          responsive: true,
          maintainAspectRatio: false,
          indexAxis: "y",
          scales: {
            x: {
              title: { display: true, text: "\u504F\u5DEE\u5929\u6570\uFF08\u6B63=\u63D0\u524D\uFF0C\u8D1F=\u5EF6\u540E\uFF09" }
            }
          },
          plugins: {
            legend: { display: false }
          }
        }
      });
      window._analysisCharts[canvasId] = chart;
    } catch (e) {
      console.error("[VarianceChart] render error:", e);
    }
  }
  function renderStageChart(canvasId, chartData) {
    if (!canvasId || !chartData) return;
    const canvas = document.getElementById(canvasId);
    if (!canvas) return;
    const ctx = canvas.getContext("2d");
    if (!ctx) return;
    try {
      if (!window._analysisCharts) window._analysisCharts = {};
      var oldChart = window._analysisCharts[canvasId];
      if (oldChart) {
        oldChart.destroy();
      }
      const ChartCtor = window.Chart;
      if (!ChartCtor) {
        console.warn("[StageChart] Chart.js not loaded");
        return;
      }
      var labels = chartData.labels || [];
      var actualPcts = chartData.actualPcts || [];
      var plannedPcts = chartData.plannedPcts || [];
      var delayDays = chartData.delayDays || [];
      var statuses = chartData.statuses || [];
      var completed = chartData.completed || 0;
      var total = chartData.total || 0;

      // Color per stage: completed=green, delayed=red, in_progress=blue, pending=gray
      var actualColors = statuses.map(function(s) {
        switch (s) {
          case "completed": return "#52c41a";
          case "delayed": return "#ff4d4f";
          case "in_progress": return "#1890ff";
          default: return "#d9d9d9";
        }
      });
      var plannedColors = labels.map(function() { return "rgba(0,0,0,0.12)"; });

      var datasets = [
        {
          label: "\u5B9E\u9645\u5B8C\u6210%",
          data: actualPcts,
          backgroundColor: actualColors,
          borderColor: actualColors,
          borderWidth: 0,
          barPercentage: 0.6,
          categoryPercentage: 0.8
        },
        {
          label: "\u8BA1\u5212\u5B8C\u6210%",
          data: plannedPcts,
          backgroundColor: "transparent",
          borderColor: "#fa8c16",
          borderWidth: 2,
          borderDash: [4, 2],
          barPercentage: 0.6,
          categoryPercentage: 0.8,
          type: "line",
          pointStyle: "dash",
          pointRadius: 4,
          pointBorderColor: "#fa8c16",
          pointBackgroundColor: "#fff",
          fill: false
        }
      ];

      var chart = new ChartCtor(ctx, {
        type: "bar",
        data: {
          labels: labels,
          datasets: datasets
        },
        options: {
          responsive: true,
          maintainAspectRatio: false,
          indexAxis: "y",
          scales: {
            x: {
              min: 0,
              max: 100,
              title: { display: true, text: "%" },
              ticks: {
                callback: function(v) { return v + "%"; }
              }
            },
            y: {
              ticks: {
                font: { size: 11 }
              }
            }
          },
          plugins: {
            legend: {
              display: true,
              position: "top",
              labels: {
                usePointStyle: true,
                generateLabels: function(chart) {
                  return [
                    { text: "\u5B9E\u9645\u5B8C\u6210%", fillStyle: "#52c41a", strokeStyle: "#52c41a", lineWidth: 0, pointStyle: "rect" },
                    { text: "\u8BA1\u5212\u5B8C\u6210%", fillStyle: "transparent", strokeStyle: "#fa8c16", lineWidth: 2, pointStyle: "line", borderDash: [4, 2] },
                    { text: completed + "/" + total + " \u9636\u6BB5\u5B8C\u6210", fillStyle: "transparent", strokeStyle: "transparent", lineWidth: 0, pointStyle: false }
                  ];
                }
              }
            }
          }
        }
      });
      window._analysisCharts[canvasId] = chart;
    } catch (e) {
      console.error("[StageChart] render error:", e);
    }
  }
  function renderResourceLoadChart(canvasId, chartData) {
    if (!canvasId || !chartData) return;
    const canvas = document.getElementById(canvasId);
    if (!canvas) return;
    const ctx = canvas.getContext("2d");
    if (!ctx) return;
    try {
      if (!window._analysisCharts) window._analysisCharts = {};
      var oldChart = window._analysisCharts[canvasId];
      if (oldChart) {
        oldChart.destroy();
      }
      const ChartCtor = window.Chart;
      if (!ChartCtor) {
        console.warn("[ResourceLoadChart] Chart.js not loaded");
        return;
      }
      var datasets = (chartData.datasets || []).map(function(ds, i) {
        return {
          label: ds.label || "\u8D44\u6E90 " + (i + 1),
          data: ds.data || [],
          backgroundColor: ds.backgroundColor || "hsl(" + (i * 60) + ", 60%, 60%)"
        };
      });
      var chart = new ChartCtor(ctx, {
        type: "bar",
        data: {
          labels: chartData.labels || [],
          datasets: datasets
        },
        options: {
          responsive: true,
          maintainAspectRatio: false,
          scales: {
            x: { stacked: true },
            y: {
              stacked: true,
              beginAtZero: true,
              title: { display: true, text: "\u8D1F\u8377\u6BD4" }
            }
          },
          plugins: {
            legend: { position: "bottom" }
          }
        },
        plugins: [
          {
            id: "capacityLine",
            afterDraw: function(chart) {
              var ctx2 = chart.ctx;
              var yScale = chart.scales["y"];
              if (!yScale) return;
              var y = yScale.getPixelForValue(1);
              if (y === void 0 || y === null) return;
              ctx2.save();
              ctx2.beginPath();
              ctx2.moveTo(chart.chartArea.left, y);
              ctx2.lineTo(chart.chartArea.right, y);
              ctx2.strokeStyle = "#ff4d4f";
              ctx2.lineWidth = 1;
              ctx2.setLineDash([5, 5]);
              ctx2.stroke();
              ctx2.fillStyle = "#ff4d4f";
              ctx2.font = "10px sans-serif";
              ctx2.fillText("\u5BB9\u91CF\u7EBF(1.0)", chart.chartArea.right - 55, y - 5);
              ctx2.restore();
            }
          }
        ]
      });
      window._analysisCharts[canvasId] = chart;
    } catch (e) {
      console.error("[ResourceLoadChart] render error:", e);
    }
  }

  // netplan/storage/project.ts
  var ACTIVE_PROJECT_KEY = "netplan_active_project";
  var CHECKED_PROJECT_KEY = "netplan_checked_project";
  function getActiveProject() {
    try {
      return localStorage.getItem(ACTIVE_PROJECT_KEY);
    } catch {
      return null;
    }
  }
  function setActiveProject(id) {
    try {
      localStorage.setItem(ACTIVE_PROJECT_KEY, id);
    } catch {
    }
  }
  function navToProject(page, id) {
    setActiveProject(id);
    window.location.href = `/project/${page}?id=${id}`;
  }
  function getCheckedProject() {
    try {
      return localStorage.getItem(CHECKED_PROJECT_KEY);
    } catch {
      return null;
    }
  }
  function setCheckedProject(id) {
    try {
      localStorage.setItem(CHECKED_PROJECT_KEY, id);
    } catch {
    }
  }
  function navToChecked(page) {
    const id = getCheckedProject();
    if (id) {
      window.location.href = `/project/${id}/${page}`;
    } else {
      console.warn("[navToChecked] no checked project");
    }
  }

  // netplan/gantt/bar-act-resize.ts
  function onBarActMouseDown(e) {
    // 只在点击右边缘10px范围内时触发resize（其余区域冒泡给父级拖拽）
    var rect = this.getBoundingClientRect();
    var clickX = e.clientX - rect.left;
    if (clickX < rect.width - 10) return; // 不是右边缘，不拦截

    e.stopPropagation();
    e.preventDefault();

    var barAct = this;
    var barFrame = barAct.closest('.gantt-bar-frame');
    var barPlan = barFrame.querySelector('.bar-plan');
    var taskId = parseInt(barFrame.getAttribute('data-task-id'));
    if (!taskId) return;

    var startX = e.clientX;
    var startWidth = barAct.offsetWidth;
    var planWidth = barPlan.offsetWidth;
    var maxWidth = planWidth;
    var dotNet = window._ganttDotNet;

    function onMouseMove(ev) {
      var delta = ev.clientX - startX;
      var newWidth = Math.max(0, Math.min(maxWidth, startWidth + delta));
      barAct.style.width = newWidth + 'px';
    }

    function onMouseUp(ev) {
      document.removeEventListener('mousemove', onMouseMove);
      document.removeEventListener('mouseup', onMouseUp);

      var delta = ev.clientX - startX;
      var newWidth = Math.max(0, Math.min(maxWidth, startWidth + delta));
      var newPercent = Math.round((newWidth / maxWidth) * 100);

      if (dotNet) {
        dotNet.invokeMethodAsync('UpdateTaskCompletion', taskId, newPercent)
          .catch(function(err) {
            console.warn('[NET] UpdateTaskCompletion error:', err);
          });
      }
    }

    document.addEventListener('mousemove', onMouseMove);
    document.addEventListener('mouseup', onMouseUp);
  }

  function initBarActResize() {
    // 为所有 .bar-act 元素添加拖拽调整完成率功能
    var barActs = document.querySelectorAll('#gantt-right .bar-act');
    barActs.forEach(function(barAct) {
      // 移除旧事件（防重复绑定）
      barAct.removeEventListener('mousedown', onBarActMouseDown);
      barAct.addEventListener('mousedown', onBarActMouseDown);
    });
  }

  // netplan/index.ts
  var api = {
    // 网络�?
    setNetworkDotNet(ref) {
      window._netDotNet = ref;
    },
    setNetworkMode(mode) {
      window._netMode = mode;
    },
    setNetTimeScaleMode(mode) {
      window._netTimeScaleMode = mode;
    },
    clearNetworkOffsets() {
      window._netEventOffsets = {};
    },
    renderNetwork,
    // 直接使用 renderNetwork（从 render/network.ts 导入�?
    initProgressColors() {
      updateProgressColors([], null, /* @__PURE__ */ new Date());
    },
    networkFit: () => networkFit(),
    // 导出
    downloadSVG: downloadSvg,
    exportNetworkPNG: exportPng,
    printNetwork: printSvg,
    // 甘特�?
    setGanttDotNet,
    initGanttScroll,
    syncGanttScrollById,
    initPanelResize,
    loadDraft,
    saveDraft,
    clearDraft,
    syncGanttRowHeights,
    syncGanttRowHeightsDeferred,
    initBarActResize,
    toggleGanttGrid(type, show) {
      if (type === 'horizontal') {
        const el = document.querySelector('#gantt-right .gantt-hlines');
        if (el) el.style.display = show ? '' : 'none';
      }
      if (type === 'vertical') {
        const el = document.querySelector('#gantt-right .gantt-grid-lines');
        if (el) el.style.display = show ? '' : 'none';
      }
    },
    setGanttRowHeight(height) {
      document.documentElement.style.setProperty('--gantt-row-min-height', height + 'px');
      const leftRows = document.querySelectorAll('#gantt-left-body .gantt-left-row');
      leftRows.forEach((row) => {
        (row).style.minHeight = height + 'px';
      });
      setTimeout(() => syncGanttRowHeights(), 50);
    },
    // 图表
    initResourceChart,
    renderAnalysisBarChart,
    renderEVMChart,
    renderSPITrendChart,
    renderProgressCurveChart,
    renderVarianceChart,
    renderStageChart,
    renderResourceLoadChart,
    // 全局项目
    getActiveProject,
    setActiveProject,
    navToProject,
    getCheckedProject,
    setCheckedProject,
    navToChecked,
    // 内部
    _netInsertBlankRow(_layerNum) {
    },
    _netDeleteBlankRow(_layerNum) {
    },
    exportGanttToPng() {
      var container = document.querySelector('.gantt-container');
      var statusbar = document.querySelector('.gantt-statusbar');
      if (!container) return;
      // 用 onclone 来克隆完整展开的 DOM，不修改可见页面
      html2canvas(container, {
        useCORS: true, scale: 2, backgroundColor: '#fff',
        onclone: function(doc) {
          // 展开所有限制高度的容器
          var all = [doc.querySelector('.gantt-container'), doc.querySelector('.gantt-main'),
                     doc.querySelector('.gantt-right'), doc.querySelector('.gantt-chart')];
          all.forEach(function(el) {
            if (!el) return;
            el.style.height = 'auto';
            el.style.maxHeight = 'none';
            el.style.overflow = 'visible';
            el.style.flex = 'none';
          });
          // 展开条形图区
          var bars = doc.querySelector('.gantt-bars');
          if (bars) { bars.style.height = 'auto'; bars.style.overflow = 'visible'; }
        }
      }).then(function(canvas) {
        if (statusbar) {
          html2canvas(statusbar, { useCORS: true, scale: 2, backgroundColor: '#fff' }).then(function(sbCanvas) {
            var combined = document.createElement('canvas');
            combined.width = canvas.width;
            combined.height = canvas.height + sbCanvas.height;
            var ctx = combined.getContext('2d');
            ctx.drawImage(canvas, 0, 0);
            ctx.drawImage(sbCanvas, 0, canvas.height);
            var link = document.createElement('a');
            link.download = '甘特图_' + new Date().toISOString().slice(0,10) + '.png';
            link.href = combined.toDataURL('image/png');
            link.click();
          });
        } else {
          var link = document.createElement('a');
          link.download = '甘特图_' + new Date().toISOString().slice(0,10) + '.png';
          link.href = canvas.toDataURL('image/png');
          link.click();
        }
      });
    },
    exportGanttToPdf() {
      try {
        var container = document.querySelector('.gantt-container');
        var statusbar = document.querySelector('.gantt-statusbar');
        if (!container) return;
        html2canvas(container, {
          useCORS: true, scale: 2, backgroundColor: '#fff',
          onclone: function(doc) {
            var all = [doc.querySelector('.gantt-container'), doc.querySelector('.gantt-main'),
                       doc.querySelector('.gantt-right'), doc.querySelector('.gantt-chart')];
            all.forEach(function(el) { if (el) { el.style.height = 'auto'; el.style.maxHeight = 'none'; el.style.overflow = 'visible'; el.style.flex = 'none'; } });
            var bars = doc.querySelector('.gantt-bars');
            if (bars) { bars.style.height = 'auto'; bars.style.overflow = 'visible'; }
          }
        }).then(function(canvas) {
          try {
            var doPdf = function(c, sb) {
              var combined = c;
              if (sb) {
                combined = document.createElement('canvas');
                combined.width = c.width;
                combined.height = c.height + sb.height;
                var ctx = combined.getContext('2d');
                ctx.drawImage(c, 0, 0);
                ctx.drawImage(sb, 0, c.height);
              }
              var imgData = combined.toDataURL('image/jpeg', 0.95);
              var { jsPDF } = window.jspdf;
              var pdf = new jsPDF({ orientation: 'l', unit: 'mm', format: 'a4' });
              var pdfW = pdf.internal.pageSize.getWidth();
              var pdfH = pdf.internal.pageSize.getHeight();
              var imgW = combined.width;
              var imgH = combined.height;
              var ratio = Math.min(pdfW / imgW, pdfH / imgH);
              pdf.addImage(imgData, 'JPEG', (pdfW - imgW * ratio) / 2, (pdfH - imgH * ratio) / 2, imgW * ratio, imgH * ratio);
              pdf.save('甘特图_' + new Date().toISOString().slice(0,10) + '.pdf');
            };
            if (statusbar) {
              html2canvas(statusbar, { useCORS: true, scale: 2, backgroundColor: '#fff' }).then(function(sb) { doPdf(canvas, sb); }).catch(function(){});
            } else {
              doPdf(canvas, null);
            }
          } catch(e) { console.error('PDF gen error:', e); }
        }).catch(function(){});
      } catch(e) { console.error('PDF export error:', e); }
    },
    positionColumnPanel(btnId) {
      var btn = document.getElementById(btnId);
      var panel = document.querySelector('.column-panel');
      if (!btn || !panel) return;
      var rect = btn.getBoundingClientRect();
      panel.style.position = 'fixed';
      panel.style.top = (rect.bottom + 4) + 'px';
      panel.style.left = rect.left + 'px';
      panel.style.zIndex = 9999;
    },
    downloadWordReport(url, aiHtml) {
      // 用表单 POST 提交，避免 URL 长度限制
      var form = document.createElement('form');
      form.method = 'POST';
      form.action = url;
      form.style.display = 'none';
      var input = document.createElement('input');
      input.name = 'aiReport';
      input.value = aiHtml;
      form.appendChild(input);
      document.body.appendChild(form);
      form.submit();
      setTimeout(function() { document.body.removeChild(form); }, 1000);
    },
    downloadTextFile(fileName, content) {
      var blob = new Blob([content], { type: 'text/plain;charset=utf-8' });
      var link = document.createElement('a');
      link.href = URL.createObjectURL(blob);
      link.download = fileName;
      document.body.appendChild(link);
      link.click();
      document.body.removeChild(link);
      URL.revokeObjectURL(link.href);
    },

  };
  for (const [key, fn] of Object.entries(api)) {
    window[key] = fn;
  }
  return __toCommonJS(index_exports);
})();
