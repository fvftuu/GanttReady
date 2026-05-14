/**
 * NetPlan 核心算法单元测试
 * 在 Node.js 下运行: node test/core-algorithms.js
 * 
 * 测试目标:
 * 1. calculateTimeParams — 事件合并 + 工作箭线构建
 * 2. applySingleStartEnd — 虚拟起终点
 * 3. calculateVerticalLayout — BFS 拓扑分层
 */

const path = require('path');
const netplan = require('../src/NetPlan.Server/wwwroot/js/netplan.js');

const { calculateTimeParams, applySingleStartEnd, calculateVerticalLayout } = netplan;

var passed = 0;
var failed = 0;

function assert(cond, msg) {
    if (cond) {
        passed++;
    } else {
        failed++;
        console.error('  FAIL: ' + msg);
    }
}

function assertEqual(actual, expected, msg) {
    if (actual === expected) {
        passed++;
    } else {
        failed++;
        console.error('  FAIL: ' + msg + ' — expected ' + JSON.stringify(expected) + ', got ' + JSON.stringify(actual));
    }
}

function assertDeepEqual(actual, expected, msg) {
    try {
        var a = JSON.stringify(actual);
        var b = JSON.stringify(expected);
        if (a === b) {
            passed++;
        } else {
            failed++;
            console.error('  FAIL: ' + msg);
            console.error('    expected:', b);
            console.error('    actual:  ', a);
        }
    } catch(e) {
        failed++;
        console.error('  FAIL: ' + msg + ' — exception: ' + e.message);
    }
}

// ============================================================
// Test Suite 1: calculateTimeParams — 基本事件合并
// ============================================================
console.log('\n=== Suite 1: calculateTimeParams (basic event merging) ===');

// Case 1: 两个独立任务
(function testTwoTasks() {
    var tasks = [
        { id: 1, es: 0, ef: 5, duration: 5, isCritical: true, tf: 0 },
        { id: 2, es: 5, ef: 10, duration: 5, isCritical: true, tf: 0 },
    ];
    var relations = [{ predecessorId: 1, successorId: 2, type: 0 }];
    var result = calculateTimeParams(tasks, relations);

    assert(result.events !== undefined, 'events exists');
    assert(result.activities !== undefined, 'activities exists');
    // 应该有 3 个事件: T0(Task1开始), T5(Task1结束/Task2开始), T10(Task2结束)
    var eids = Object.keys(result.events);
    assert(eids.length === 3, '3 events for 2 sequential tasks, got ' + eids.length);
    assert(result.events['T0'] !== undefined, 'T0 exists');
    assert(result.events['T5'] !== undefined, 'T5 exists');
    assert(result.events['T10'] !== undefined, 'T10 exists');
    // T5 应该是 both 类型（既是结束也是开始）
    assert(result.events['T5'].type === 'both', 'T5 is both (end of task1, start of task2)');
    assert(result.activities.length === 2, '2 activities');
})();

// Case 2: 三个任务 A→B→C 串联
(function testThreeSequential() {
    var tasks = [
        { id: 1, es: 0, ef: 3, duration: 3, isCritical: true, tf: 0 },
        { id: 2, es: 3, ef: 7, duration: 4, isCritical: true, tf: 0 },
        { id: 3, es: 7, ef: 10, duration: 3, isCritical: true, tf: 0 },
    ];
    var relations = [
        { predecessorId: 1, successorId: 2, type: 0 },
        { predecessorId: 2, successorId: 3, type: 0 },
    ];
    var result = calculateTimeParams(tasks, relations);
    assert(Object.keys(result.events).length === 4, '4 events for 3 sequential tasks');
    assert(result.activities.length === 3, '3 activities');
    assert(result.events['T3'].type === 'both', 'T3 is both');
    assert(result.events['T7'].type === 'both', 'T7 is both');
})();

// Case 3: 两个任务同 ES/EF（并行）
(function testParallelTasks() {
    var tasks = [
        { id: 1, es: 0, ef: 5, duration: 5, isCritical: true, tf: 0 },
        { id: 2, es: 0, ef: 5, duration: 5, isCritical: false, tf: 2 },
    ];
    var result = calculateTimeParams(tasks, []);
    // 应该只有 2 个事件: T0 和 T5（事件合并）
    assert(Object.keys(result.events).length === 2, '2 events for parallel tasks (merged)');
    var t0 = result.events['T0'];
    var t5 = result.events['T5'];
    assert(t0 !== undefined && t5 !== undefined, 'T0 and T5 exist');
    // T0 type: 如果同时有任务从T0开始和结束，type='both'
    // 这里两个任务从T0开始，所以 type 应该是 'start' 或 'both'
    // 由于没有任务在 T0 结束，应为 'start'
    assert(t0.type.indexOf('start') >= 0, 'T0 is at least start type');
    // 如果 TF 信息被合并到事件，取最大值
    assert(t0.isCritical === true, 'T0 is critical (task1 is critical)');
})();

// ============================================================
// Test Suite 2: applySingleStartEnd
// ============================================================
console.log('\n=== Suite 2: applySingleStartEnd ===');

// Case 4: 多起点(无入度的事件) — 应自动插入虚拟开始节点
(function testMultipleStart() {
    var data = {
        events: {
            'T0': { id: 'T0', es: 0, ef: 0, type: 'start', isCritical: false, isVirtual: false },
            'T3': { id: 'T3', es: 3, ef: 3, type: 'both', isCritical: false, isVirtual: false },
            'T5': { id: 'T5', es: 5, ef: 5, type: 'end', isCritical: false, isVirtual: false },
        },
        activities: [
            { id: 1, source: 'T0', target: 'T3', es: 0, ef: 3, isCritical: false },
            { id: 2, source: 'T3', target: 'T5', es: 3, ef: 5, isCritical: false },
        ],
        maxEs: 5, maxEf: 5,
        eventPred: { 'T0': [], 'T3': ['T0'], 'T5': ['T3'] },
        eventSucc: { 'T0': ['T3'], 'T3': ['T5'], 'T5': [] },
        sortedEvents: ['T0','T3','T5']
    };
    applySingleStartEnd(data);
    // 串联: 只有 1 个起点(T0)和 1 个终点(T5),should NOT insert
    assert(data.events['TS'] === undefined, 'TS not inserted for single start/end chain');
})();

// Case 4b: 真正多起点 — 两个无入度事件, es 不同
(function testMultipleStartBranch() {
    var data = {
        events: {
            'T0': { id: 'T0', es: 0, ef: 0, type: 'start', isCritical: false, isVirtual: false },
            'T0b': { id: 'T0b', es: 0, ef: 0, type: 'start', isCritical: false, isVirtual: false },
            'T5': { id: 'T5', es: 5, ef: 5, type: 'end', isCritical: false, isVirtual: false },
        },
        activities: [
            { id: 1, source: 'T0', target: 'T5', es: 0, ef: 5, isCritical: false },
            { id: 2, source: 'T0b', target: 'T5', es: 0, ef: 5, isCritical: false },
        ],
        maxEs: 5, maxEf: 5,
        eventPred: { 'T0': [], 'T0b': [], 'T5': ['T0','T0b'] },
        eventSucc: { 'T0': ['T5'], 'T0b': ['T5'], 'T5': [] },
        sortedEvents: ['T0','T0b','T5']
    };
    applySingleStartEnd(data);
    assert(data.events['TS'] !== undefined, 'TS inserted for multiple start candidates');
    if (data.events['TS']) {
        assert(data.events['TS'].isVirtual === true, 'TS is virtual');
    }
})();

// ============================================================
// Test Suite 3: calculateVerticalLayout
// ============================================================
console.log('\n=== Suite 3: calculateVerticalLayout ===');

// Case 5: 简单串联布局 — 所有节点应在同一层（关键路径）
(function testSimpleChainLayout() {
    var data = {
        events: {
            'T0': { id: 'T0', es: 0, ef: 0, isCritical: true },
            'T5': { id: 'T5', es: 5, ef: 5, isCritical: true },
            'T10': { id: 'T10', es: 10, ef: 10, isCritical: true },
        },
        activities: [
            { id: 1, source: 'T0', target: 'T5', es: 0, ef: 5, isCritical: true },
            { id: 2, source: 'T5', target: 'T10', es: 5, ef: 10, isCritical: true },
        ],
        maxEs: 10, maxEf: 10
    };
    calculateVerticalLayout(data);
    // 所有节点应有 y 坐标
    assert(data.events['T0'].y !== undefined, 'T0 has Y coordinate');
    assert(data.events['T5'].y !== undefined, 'T5 has Y coordinate');
    assert(data.events['T10'].y !== undefined, 'T10 has Y coordinate');
    // 关键路径同层
    assert(data.events['T0'].y === data.events['T5'].y, 'T0 and T5 same row (critical chain)');
    assert(data.events['T0'].y === data.events['T10'].y, 'T0 and T10 same row (critical chain)');
})();

// ============================================================
// Summary
// ============================================================
console.log('\n=== Results ===');
console.log('  Passed: ' + passed);
console.log('  Failed: ' + failed);
if (failed > 0) {
    console.log('  Status: SOME TESTS FAILED');
    process.exit(1);
} else {
    console.log('  Status: ALL TESTS PASSED');
}
