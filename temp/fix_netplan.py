"""
一次性修复 netplan.js：前锋线蓝色+独立日期、showDummy toggle、双击弹窗
"""
import re

with open(r'I:\NetPlan\src\NetPlan.Server\wwwroot\js\netplan.js', 'r', encoding='utf-8') as f:
    content = f.read()

changes = 0

# 1. _progressCheckDate 全局变量声明
old1 = 'var _progressDate = null;   // 前锋线检查日期（Date）\nvar _progressX = 0;         // 前锋线像素位置'
new1 = 'var _progressDate = null;   // 前锋线检查日期（Date）\nvar _progressCheckDate = null; // 前锋线独立日期（与今日线解耦）\nvar _progressX = 0;         // 前锋线像素位置'
if old1 in content:
    content = content.replace(old1, new1)
    changes += 1
    print('[1] _progressCheckDate added')

# 2. 前锋线段：颜色#1890ff + 使用_progressCheckDate
old2 = '''    // === 可拖动前锋线（JGJ/T121-2015）===
    if (p.showProgressLine) {
        var today = new Date();
        var progressOffset = Math.floor((today.getTime() - sd.getTime()) / 86400000);
        if (progressOffset < 0) progressOffset = 0;
        if (progressOffset >= td) progressOffset = td - 1;
        var pxLine = MARGIN_LEFT + progressOffset * dw + dw / 2;
        _progressDate = new Date(sd); _progressDate.setDate(_progressDate.getDate() + Math.round(progressOffset));
        _progressX = pxLine;
        _projStartDate = sd;
        var checkLabel = _progressDate.getFullYear() + '-' + String(_progressDate.getMonth()+1).padStart(2,'0') + '-' + String(_progressDate.getDate()).padStart(2,'0');
        parts.push('<g id="net-progress-line" style="cursor:ew-resize;">'
            + '<line id="net-pl-line" x1="' + pxLine + '" y1="0" x2="' + pxLine + '" y2="' + (ch + RULER_H) + '" stroke="#e74c3c" stroke-width="2"/>'''  # old color is red
new2 = '''    // === 可拖动前锋线（JGJ/T121-2015）蓝色#1890ff，与今日线解耦 ===
    if (p.showProgressLine) {
        if (!_progressCheckDate) { _progressCheckDate = new Date(); _progressCheckDate.setHours(0,0,0,0); }
        var progressOffset = Math.floor((_progressCheckDate.getTime() - sd.getTime()) / 86400000);
        if (progressOffset < 0) progressOffset = 0;
        if (progressOffset >= td) progressOffset = td - 1;
        var pxLine = MARGIN_LEFT + progressOffset * dw;
        _progressDate = new Date(sd); _progressDate.setDate(_progressDate.getDate() + Math.round(progressOffset));
        _progressX = pxLine;
        _projStartDate = sd;
        var checkLabel = _progressCheckDate.getFullYear() + '-' + String(_progressCheckDate.getMonth()+1).padStart(2,'0') + '-' + String(_progressCheckDate.getDate()).padStart(2,'0');
        parts.push('<g id="net-progress-line" style="cursor:ew-resize;">'
            + '<line id="net-pl-line" x1="' + pxLine + '" y1="0" x2="' + pxLine + '" y2="' + (ch + RULER_H) + '" stroke="#1890ff" stroke-width="2"/>'''  # new color blue
if old2 in content:
    content = content.replace(old2, new2)
    changes += 1
    print('[2] progress line color #1890ff + _progressCheckDate')
else:
    print('[2] WARNING: old progress line block not found!')

# 3. 虚线工作 toggle: 加 showDummy if 块
old3 = '    // === 虚工作 ===\n    p.timeParams.relations.forEach(function(rel) {'
new3 = '    // === 虚工作（toggle 控制）===\n    var showDummy = p.showDummyArrows !== false;\n    if (showDummy) {\n    p.timeParams.relations.forEach(function(rel) {'
if old3 in content:
    content = content.replace(old3, new3)
    # 加闭合 } 在 }); 之后
    content = content.replace(
        '    });\n\n    // === 工作箭线 ===',
        '    });\n    } // if showDummy\n\n    // === 工作箭线 ==='
    )
    changes += 1
    print('[3] showDummy toggle')
else:
    print('[3] WARNING: dummy forEach not found')

# 4. renderNetwork 传 showDummyArrows
old4 = 'showTodayLine: showTodayLine, showProgressLine: showProgressLine,'
new4 = 'showTodayLine: showTodayLine, showProgressLine: showProgressLine, showDummyArrows: opts.showDummyArrows,'
if old4 in content:
    content = content.replace(old4, new4)
    changes += 1
    print('[4] showDummyArrows passed to buildNetworkSvg')

# 5. svg.ondblclick: 加箭线和空白双击
old5 = '''        svg.ondblclick = function(e) {
            var el = e.target.closest('.net-event');
            if (!el) return;
            var tid = parseInt(el.getAttribute('data-task-id'));
            if (tid) _netDotNet.invokeMethodAsync('OpenTaskEditor', tid);
        };'''
new5 = '''        svg.ondblclick = function(e) {
            var el = e.target.closest('.net-event');
            if (el) {
                var tid = parseInt(el.getAttribute('data-task-id'));
                if (tid) _netDotNet.invokeMethodAsync('OpenTaskEditor', tid);
                return;
            }
            var arcEl = e.target.closest('[data-arc-id]');
            var arcId = arcEl ? parseInt(arcEl.getAttribute('data-arc-id')) || 0 : 0;
            _netDotNet.invokeMethodAsync('ShowAddTaskModal', arcId, 0);
        };'''
if old5 in content:
    content = content.replace(old5, new5)
    changes += 1
    print('[5] dblclick: arrow + blank area')
else:
    print('[5] WARNING: svg.ondblclick not found!')

# 6. 前锋线拖动保存 _progressCheckDate
old6 = '''            // 计算新检查日期
            if (_projStartDate) {
                var dayOffset = Math.round((newX - 80) / dayWidth);
                _progressDate = new Date(_projStartDate);
                _progressDate.setDate(_progressDate.getDate() + dayOffset);'''
new6 = '''            // 计算新检查日期
            if (_projStartDate) {
                var dayOffset = Math.round((newX - 80) / dayWidth);
                _progressCheckDate = new Date(_projStartDate);
                _progressCheckDate.setDate(_progressCheckDate.getDate() + dayOffset);
                _progressDate = new Date(_progressCheckDate);'''
if old6 in content:
    content = content.replace(old6, new6)
    changes += 1
    print('[6] _progressCheckDate saved on drag')

# 7. clearNetworkOffsets
old7 = '// .NET引用设置\nwindow.setNetworkDotNet = function(ref) {\n    _netDotNet = ref;\n};'
new7 = '// .NET引用设置\nwindow.setNetworkDotNet = function(ref) {\n    _netDotNet = ref;\n};\n\n// 清除节点偏移和前锋线状态\nwindow.clearNetworkOffsets = function() {\n    _netEventOffsets = {};\n    _progressCheckDate = null;\n};'
if old7 in content:
    content = content.replace(old7, new7)
    changes += 1
    print('[7] clearNetworkOffsets added')

# Write back
with open(r'I:\NetPlan\src\NetPlan.Server\wwwroot\js\netplan.js', 'w', encoding='utf-8') as f:
    f.write(content)

print(f'\nTotal: {changes} changes applied')
