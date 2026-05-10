"""
Complete fix for netplan.js - all 4 issues:
1. Progress line reset → don't clear _progressCheckDate in clearNetworkOffsets
2. Double-click → no conflict with node drag mousedown  
3. Canvas drag → add panning on #network-body
4. Bottom ruler → add mirrored time ruler below the SVG
"""
import re

with open(r'I:\NetPlan\src\NetPlan.Server\wwwroot\js\netplan.js', 'r', encoding='utf-8') as f:
    content = f.read()

changes = 0

# ============================================================
# FIX 1: Don't clear _progressCheckDate in clearNetworkOffsets
# ============================================================
old1 = """// 清除节点偏移和前锋线状态
window.clearNetworkOffsets = function() {
    _netEventOffsets = {};
    _progressCheckDate = null;
};"""
new1 = """// 清除节点偏移（不重置前锋线日期）
window.clearNetworkOffsets = function() {
    _netEventOffsets = {};
};"""
if old1 in content:
    content = content.replace(old1, new1)
    changes += 1
    print('[1] clearNetworkOffsets no longer resets _progressCheckDate')

# ============================================================
# FIX 2: svg.ondblclick - fine as is, but need to prevent mousedown from eating dblclick.
# The real issue: node drag's preventDefault + stopPropagation may eat dblclick.
# Fix: In the global mousedown handler, don't call preventDefault for circles 
# (only in mousemove/up). Let dblclick bubble.
# ============================================================
# Find the drag mousedown that does e.preventDefault
old2 = """        svg.addEventListener('mousedown', function(e) {
            // 只处�?circle 上的 mousedown（不干扰文本选择等）
            if (e.target.tagName !== 'circle') return;
            var g = e.target.closest('.net-event');
            if (!g || !_netLayout || !_netLayout.events) return;
            e.preventDefault();
            e.stopPropagation();"""
new2 = """        svg.addEventListener('mousedown', function(e) {
            // 只处理 circle 上的 mousedown
            if (e.target.tagName !== 'circle') return;
            var g = e.target.closest('.net-event');
            if (!g || !_netLayout || !_netLayout.events) return;
            // 不阻止默认行为，让 dblclick 能触发；仅在 mousemove 后才开始拖动"""
if old2 in content:
    content = content.replace(old2, new2)
    changes += 1
    print('[2] Node drag no longer blocks dblclick')
else:
    print('[2] WARNING: mousedown drag block not found (may need line search)')
    # Fallback: search by pattern
    idx = content.find("svg.addEventListener('mousedown'")
    if idx >= 0:
        print(f'  mousedown found at char {idx}: {content[idx:idx+100]}')

# ============================================================
# FIX 3: Canvas panning (drag on blank area of #network-body)
# Add after the existing node drag code block
# ============================================================
# Find the end of renderNetwork function to add panning setup
old3 = """    // 重算后恢复已保存的节点偏移"""
# Actually let's find a better anchor - the end of the mousedown handler for node drag
# Then add canvas panning via #network-body mousedown
# We need to find: // ===== 前锋线拖动 =====
old3_marker = '// ===== 前锋线拖动 ====='
if old3_marker in content:
    # Find the mousemove/mouseup that handles drag end
    # After node drag mouseup, before progress line drag, add canvas panning
    # The canvas panning code block
    
    # Actually let's add panning BEFORE the node drag section inside #network-body
    # Find: var ce = document.getElementById('network-body');
    old3b = "var ce = document.getElementById('network-body');"
    if old3b in content:
        canvas_pan_code = """
    // ===== 画布拖拽平移（空白区域按住拖动）=====
    var ce3 = document.getElementById('network-body');
    if (ce3 && !ce3._panSetup) {
        ce3._panSetup = true;
        var panState = null;
        ce3.addEventListener('mousedown', function(e) {
            // 忽略节点/箭线上的 mousedown（留给拖拽和双击）
            if (e.target.closest('.net-event') || e.target.closest('[data-arc-id]')) return;
            if (e.target.closest('#net-progress-line')) return;
            // 忽略 SVG 内部元素的 mousedown（留给 node drag）
            if (e.target.closest('circle') || e.target.closest('polygon')) return;
            panState = { x: e.clientX, y: e.clientY, scrollLeft: ce3.scrollLeft, scrollTop: ce3.scrollTop };
            ce3.style.cursor = 'grabbing';
            e.preventDefault();
        });
        window.addEventListener('mousemove', function(e) {
            if (!panState) return;
            ce3.scrollLeft = panState.scrollLeft - (e.clientX - panState.x);
            ce3.scrollTop = panState.scrollTop - (e.clientY - panState.y);
        });
        window.addEventListener('mouseup', function() {
            if (!panState) return;
            ce3.style.cursor = '';
            panState = null;
        });
    }
"""
        # Insert after ce assignment
        content = content.replace(old3b, old3b + canvas_pan_code)
        changes += 1
        print('[3] Canvas panning added')
    else:
        print('[3] ERROR: #network-body element not found in JS')
else:
    print('[3] ERROR: progress line drag marker not found')

# ============================================================
# FIX 4: Bottom mirror ruler (底标尺)
# After the main SVG is built, add a bottom ruler section
# ============================================================
# In buildNetworkSvg, after the main content but before the closing </svg>, add a bottom ruler
# Find: parts.push('<text x="10" y="' + (ch - 5) + '" 
# Then after that, add bottom ruler

# Actually, bottom ruler should be drawn as SVG elements within the existing buildNetworkSvg
# Let's find the line: '// === 规程标注 ==='
old4_marker = "// === 规程标注 ==="
if old4_marker in content:
    # Add bottom ruler before the regulation label
    bottom_ruler = """
    // === 底部镜像时间标尺 ===
    var bottomRulerY = ch - RULER_H;
    parts.push('<rect x="0" y="' + bottomRulerY + '" width="' + cw + '" height="' + upperH + '" fill="url(#rulerGrad)"/>');
    parts.push('<rect x="0" y="' + (bottomRulerY + 28) + '" width="' + cw + '" height="' + lowerH + '" fill="#f5f5f5"/>');
    // 上层: 年/月
    if (dw > 20) {
        for (var d2 = 0; d2 < td; d2++) {
            var dt2 = new Date(sd); dt2.setDate(dt2.getDate() + d2);
            if (dt2.getDate() === 1) {
                var xb = MARGIN_LEFT + d2 * dw;
                var ms = Math.min(new Date(dt2.getFullYear(), dt2.getMonth()+1, 0).getDate() - dt2.getDate() + 1, td - d2);
                var mwb = ms * dw;
                parts.push('<text x="' + (xb + mwb/2) + '" y="' + (bottomRulerY + 18) + '" font-size="12" font-weight="600" fill="#333" text-anchor="middle">'
                    + dt2.getFullYear() + '/' + String(dt2.getMonth()+1).padStart(2,'0') + '</text>');
            }
        }
    } else {
        var lastY2 = -1, yStartX2 = 0;
        for (var d2 = 0; d2 <= td; d2++) {
            var dt2 = new Date(sd); dt2.setDate(dt2.getDate() + d2);
            var cy2 = dt2.getFullYear();
            if (cy2 !== lastY2) {
                if (lastY2 >= 0) {
                    var ywb = MARGIN_LEFT + d2 * dw - yStartX2;
                    parts.push('<text x="' + (yStartX2 + ywb/2) + '" y="' + (bottomRulerY + 18) + '" font-size="12" font-weight="600" fill="#333" text-anchor="middle">'
                        + lastY2 + '</text>');
                }
                lastY2 = cy2; yStartX2 = MARGIN_LEFT + d2 * dw;
            }
        }
        if (lastY2 >= 0) {
                var lw = MARGIN_LEFT + td * dw - yStartX2;
                parts.push('<text x="' + (yStartX2 + lw/2) + '" y="' + (bottomRulerY + 18) + '" font-size="12" font-weight="600" fill="#333" text-anchor="middle">'
                        + lastY2 + '</text>');
        }
    }
    // 下层: 日/周/月
    for (var d2 = 0; d2 < td; d2++) {
        var dt2 = new Date(sd); dt2.setDate(dt2.getDate() + d2);
        if (dw > 20) {
            var xb2 = MARGIN_LEFT + d2 * dw + dw / 2;
            parts.push('<text x="' + xb2 + '" y="' + (bottomRulerY + 46) + '" font-size="10" fill="#666" text-anchor="middle">' + String(dt2.getDate()).padStart(2,'0') + '</text>');
            parts.push('<line x1="' + (MARGIN_LEFT + d2 * dw) + '" y1="' + (bottomRulerY + 28) + '" x2="' + (MARGIN_LEFT + d2 * dw) + '" y2="' + (bottomRulerY + 52) + '" stroke="#ddd" stroke-width="0.5"/>');
        } else if (dw > 10) {
            if (d2 % 7 === 0) {
                var wk = getISOWeek(dt2);
                var wkSpan = Math.min(7, td - d2) * dw;
                var xb2 = MARGIN_LEFT + d2 * dw + wkSpan / 2;
                parts.push('<text x="' + xb2 + '" y="' + (bottomRulerY + 46) + '" font-size="10" fill="#666" text-anchor="middle">第' + wk + '周</text>');
                parts.push('<line x1="' + (MARGIN_LEFT + d2 * dw) + '" y1="' + (bottomRulerY + 28) + '" x2="' + (MARGIN_LEFT + d2 * dw) + '" y2="' + (bottomRulerY + 52) + '" stroke="#ddd" stroke-width="0.5"/>');
            }
        } else {
            if (dt2.getDate() === 1) {
                var mSpan = Math.min(new Date(dt2.getFullYear(), dt2.getMonth()+1, 0).getDate(), td - d2) * dw;
                var xb2 = MARGIN_LEFT + d2 * dw + mSpan / 2;
                parts.push('<text x="' + xb2 + '" y="' + (bottomRulerY + 46) + '" font-size="10" fill="#666" text-anchor="middle">' + String(dt2.getMonth()+1).padStart(2,'0') + '</text>');
                parts.push('<line x1="' + (MARGIN_LEFT + d2 * dw) + '" y1="' + (bottomRulerY + 28) + '" x2="' + (MARGIN_LEFT + d2 * dw) + '" y2="' + (bottomRulerY + 52) + '" stroke="#ddd" stroke-width="0.5"/>');
            }
        }
    }
"""
    content = content.replace(old4_marker, bottom_ruler + "\n    " + old4_marker)
    changes += 1
    print('[4] Bottom mirror ruler added')
else:
    # Try alternative markers
    print('[4] WARNING: regulation label marker not found, searching...')
    idx = content.find("规程标注")
    if idx >= 0:
        print(f'  Found at char {idx}')
    else:
        print('  Not found at all - encoding issue?')

# Verify and write
print(f'\nTotal changes: {changes}')
with open(r'I:\NetPlan\src\NetPlan.Server\wwwroot\js\netplan.js', 'w', encoding='utf-8') as f:
    f.write(content)
