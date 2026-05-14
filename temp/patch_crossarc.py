import re

with open('netplan.js', 'r', encoding='utf-8') as f:
    content = f.read()

# Fix 1: In buildNetworkSvg, add dummy arrow segments to allSegments for cross detection
# Find the closure of activities loop and add dummies after it
old_act_loop_close = '''    });
    // 两两检测交叉,在非关键线上画跨线符'''
    
new_after_acts = '''    });
    // 添加虚工作线段到交叉检测
    p.timeParams.relations.forEach(function(rel) {
        var predEf = taskEfMap[rel.source];
        var succEs = taskEsMap[rel.target];
        if (predEf === undefined || succEs === undefined) return;
        var srcId = 'T' + predEf, tgtId = 'T' + succEs;
        if (srcId === tgtId) return;
        var src = p.layout.events[srcId], tgt = p.layout.events[tgtId];
        if (!src || !tgt) return;
        var sx = src.x + NODE_R, sy = src.y, ex = tgt.x, ey = tgt.y;
        var dseg;
        if (Math.abs(ey - sy) < 2) {
            dseg = [{x1:sx, y1:sy, x2:ex, y2:ey}];
        } else {
            dseg = [
                {x1:sx, y1:sy, x2:sx, y2:ey},
                {x1:sx, y1:ey, x2:ex, y2:ey}
            ];
        }
        allSegments.push({ actId: 'dummy-' + rel.source + '-' + rel.target, isCritical: false, segs: dseg, isDummy: true });
    });
    // 两两检测交叉,在非关键线上画跨线符'''

if old_act_loop_close in content:
    content = content.replace(old_act_loop_close, new_after_acts, 1)
    print('Fix 1: dummy segments added to buildNetworkSvg cross detection')
else:
    print('WARN: Could not find insertion point for Fix 1')

# Fix 2: In _netUpdateCrossArcs, make arcs visible on dummies if they'd cross
# The key issue: cross-arc has white background + gray border stroke. 
# On dark background it's invisible. Improve visibility by adding a larger white background.

old_arc_path = '''parts.push('<path d="M' + (cross.x - nx * 2) + ' ' + (cross.y - ny * 2)
                                    + ' A' + (arcR + 1) + ' ' + (arcR + 1) + ' 0 0 0 ' + (cross.x + nx * 2) + ' ' + (cross.y + ny * 2)
                                    + '" class="net-cross-arc" fill="none" stroke="#999" stroke-width="1" stroke-linecap="round"/>');'''

new_arc_path = '''parts.push('<path d="M' + (cross.x - nx * 2) + ' ' + (cross.y - ny * 2)
                                    + ' A' + (arcR + 1) + ' ' + (arcR + 1) + ' 0 0 0 ' + (cross.x + nx * 2) + ' ' + (cross.y + ny * 2)
                                    + '" class="net-cross-arc" fill="none" stroke="#999" stroke-width="1.5" stroke-linecap="round"/>');'''

count = content.count(old_arc_path)
if count == 2:
    content = content.replace(old_arc_path, new_arc_path)
    print(f'Fix 2: cross-arc stroke width increased (2 occurrences)')
else:
    print(f'WARN: Found {count} occurrences of arc path (expected 2)')

with open('netplan.js', 'w', encoding='utf-8') as f:
    f.write(content)

print('Done')
