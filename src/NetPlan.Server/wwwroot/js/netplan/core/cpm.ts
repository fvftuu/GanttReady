// ============================================================
// core/cpm.ts — 关键路径法算法
// 包含: calculateTimeParams, applySingleStartEnd
// 保持与 legacy netplan.js 完全相同的逻辑和返回结构
// ============================================================

export interface TimeParamsResult {
  events: Record<string, any>;
  activities: any[];
  eventPred: Record<string, string[]>;
  eventSucc: Record<string, string[]>;
  sortedEvents: string[];
  relations?: any[];
}

export interface SingleStartEndResult extends TimeParamsResult {
  virtualActivities?: any[];
}

/**
 * 计算时间参数，事件合并，构建工作箭线
 * @param tasks 任务数组 (含 es, ef, duration, isCritical 等)
 * @param relations 关系数组 (含 source, target, type)
 * @returns { events, activities, eventPred, eventSucc, sortedEvents }
 */
export function calculateTimeParams(tasks: any[], relations: any[]): TimeParamsResult {
  // 第一步:收集每个任务的 ES/EF,以及每个时间点关联的任务
  var starts: Record<string, number[]> = {};
  var ends: Record<string, number[]> = {};
  var taskEs: Record<number, number> = {};
  var taskEf: Record<number, number> = {};

  tasks.forEach(function(task: any) {
    var es = task.es || 0;
    var ef = task.ef || (es + (task.duration || 0));
    taskEs[task.id] = es;
    taskEf[task.id] = ef;

    if (!starts[es]) starts[es] = [];
    if (starts[es].indexOf(task.id) === -1) starts[es].push(task.id);

    if (!ends[ef]) ends[ef] = [];
    if (ends[ef].indexOf(task.id) === -1) ends[ef].push(task.id);
  });

  // 第二步:合并事件节点,同 ES 共享开始事件,同 EF 共享结束事件
  var events: Record<string, any> = {};
  var allTimeOffsets: string[] = [];
  Object.keys(starts).forEach(function(t) { if (allTimeOffsets.indexOf(t) === -1) allTimeOffsets.push(t); });
  Object.keys(ends).forEach(function(t) { if (allTimeOffsets.indexOf(t) === -1) allTimeOffsets.push(t); });

  // 按数值排序
  allTimeOffsets.sort(function(a, b) { return parseInt(a) - parseInt(b); });

  allTimeOffsets.forEach(function(timeOffset) {
    var eid = 'T' + timeOffset;
    var isStart = starts[timeOffset] !== undefined;
    var isEnd = ends[timeOffset] !== undefined;
    var type = isStart && isEnd ? 'both' : (isStart ? 'start' : 'end');

    // 收集关联的所有任务 ID
    var associated: number[] = [];
    var sArr: number[] = starts[timeOffset] || [];
    var eArr: number[] = ends[timeOffset] || [];
    var both = sArr.concat(eArr);
    both.forEach(function(tid: number) {
      if (associated.indexOf(tid) === -1) associated.push(tid);
    });

    var taskId = parseInt(String(associated[0])) || 0;
    var isCritical = false;
    var tf = 0;
    tasks.forEach(function(t: any) {
      if (associated.indexOf(t.id) !== -1) {
        if (t.isCritical) isCritical = true;
        if ((t.tf || 0) > tf) tf = t.tf || 0;
      }
    });

    events[eid] = {
      id: eid,
      taskId: taskId,
      type: type,
      num: 0,
      es: parseInt(timeOffset),
      ef: parseInt(timeOffset),
      ls: 0,
      lf: 0,
      tf: tf,
      ff: 0,
      isCritical: isCritical
    };
  });

  // 第三步:构建工作箭线(每个 task 一条)
  var activities: any[] = [];
  tasks.forEach(function(task: any) {
    var es = taskEs[task.id];
    var ef = taskEf[task.id];
    activities.push({
      id: task.id,
      source: 'T' + es,
      target: 'T' + ef,
      es: es,
      ef: ef,
      ls: task.ls || task.lateStart || es,
      lf: task.lf || task.lateFinish || ef,
      duration: task.duration || 0,
      code: task.label || '',
      name: task.name || '',
      isCritical: task.isCritical || false,
      tf: task.tf || 0,
      ff: task.ff || 0,
      completion: task.completion || 0
    });
  });

  // 第四步:建立事件邻接关系
  var eventPred: Record<string, string[]> = {};
  var eventSucc: Record<string, string[]> = {};
  Object.keys(events).forEach(function(eid) {
    eventPred[eid] = [];
    eventSucc[eid] = [];
  });

  // 辅助:去重添加邻接边
  function addEdge(f: string, t: string) {
    if (f === t) return;
    if (!events[f] || !events[t]) return;
    if (eventSucc[f].indexOf(t) === -1) eventSucc[f].push(t);
    if (eventPred[t].indexOf(f) === -1) eventPred[t].push(f);
  }

  // 从工作箭线建立实线连接:T{es} -> T{ef}
  activities.forEach(function(act: any) {
    addEdge(act.source, act.target);
  });

  // 从关系建立虚箭线连接(V6.1 虚工作逻辑)
  relations.forEach(function(rel: any) {
    var predEf = taskEf[rel.source];
    var succEs = taskEs[rel.target];
    if (predEf === undefined || succEs === undefined) return;
    // 虚工作:前驱结束节点 → 后继开始节点
    addEdge('T' + predEf, 'T' + succEs);
  });

  // 第五步:拓扑排序并分配事件编号
  var inDeg: Record<string, number> = {};
  Object.keys(events).forEach(function(eid) {
    inDeg[eid] = eventPred[eid].length;
  });

  var queue: string[] = [];
  Object.keys(inDeg).forEach(function(eid) {
    if (inDeg[eid] === 0) queue.push(eid);
  });

  var sortedEvents: string[] = [];
  var nextNum = 1;
  while (queue.length > 0) {
    var eid = queue.shift()!;
    events[eid].num = nextNum;
    nextNum++;
    sortedEvents.push(eid);

    eventSucc[eid].forEach(function(neighbor: string) {
      inDeg[neighbor]--;
      if (inDeg[neighbor] === 0) queue.push(neighbor);
    });
  }

  return {
    events: events,
    activities: activities,
    relations: relations,
    eventPred: eventPred,
    eventSucc: eventSucc,
    sortedEvents: sortedEvents
  };
}

/**
 * 唯一起点/终点: 插入虚拟开始节点(S)和虚拟结束节点(E)
 * 国标GB/T 13400: 双代号网络图仅设一个起始节点和一个终点节点
 * (legacy 兼容: 虚拟节点名为 TS/TE)
 */
export function applySingleStartEnd(data: any): any {
  var events = data.events;
  var activities = data.activities;
  var eventPred = data.eventPred;
  var eventSucc = data.eventSucc;
  var sortedEvents = data.sortedEvents;

  // 虚拟起终点: 起点收集所有最早开始(es===minEs)的事件，终点收集所有最晚结束(ef===maxEf)的事件
  var maxEf = -Infinity;
  Object.keys(events).forEach(function(eid) {
    if (events[eid].ef > maxEf) maxEf = events[eid].ef;
  });
  var minEs = Infinity;
  Object.keys(events).forEach(function(eid) {
    if (events[eid].es < minEs) minEs = events[eid].es;
  });

  var startCandidates: string[] = [];
  var endCandidates: string[] = [];
  Object.keys(events).forEach(function(eid) {
    var evt = events[eid];
    if (evt.isVirtual) return;
    if (evt.es === minEs) startCandidates.push(eid);
    if (evt.ef === maxEf) endCandidates.push(eid);
  });

  if (startCandidates.length <= 1 && endCandidates.length <= 1) return data;

  // 虚拟开始节点 (S)
  if (startCandidates.length > 1) {
    var sid = 'TS';
    events[sid] = {
      id: sid, taskId: 0, type: 'start',
      num: 0, es: minEs - 5, ef: minEs - 5,
      ls: 0, lf: 0, tf: 0, ff: 0,
      isCritical: false, isVirtual: true
    };
    eventPred[sid] = [];
    eventSucc[sid] = startCandidates.slice();
    startCandidates.forEach(function(seid: string) {
      eventPred[seid].push(sid);
    });
    sortedEvents.unshift(sid);
  }

  // 虚拟结束节点 (E)
  if (endCandidates.length > 1) {
    var eid2 = 'TE';
    events[eid2] = {
      id: eid2, taskId: 0, type: 'end',
      num: 0, es: maxEf + 5, ef: maxEf + 5,
      ls: 0, lf: 0, tf: 0, ff: 0,
      isCritical: false, isVirtual: true
    };
    eventPred[eid2] = endCandidates.slice();
    eventSucc[eid2] = [];
    endCandidates.forEach(function(eeid: string) {
      eventSucc[eeid].push(eid2);
    });
    sortedEvents.push(eid2);
  }

  return {
    events: events,
    activities: activities,
    eventPred: eventPred,
    eventSucc: eventSucc,
    sortedEvents: sortedEvents
  };
}
