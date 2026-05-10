# 时标双代号网络图 — 第三轮修正

## 问题清单（6 项，严格按序执行）

---

### 1. 水平滚动条消失
**现象**：页面底部 `#network-hscroll` 固定滚动条不显示。
**原因**：新版 `renderNetwork` 生成的 SVG 没有设置 `id="network-svg"`，且 `networkFit()` 依赖该 id。`#network-hscroll-inner` 宽度未同步更新。
**修正**：
- `buildNetworkSvg` 输出的 `<svg>` 标签添加 `id="network-svg"`
- 在 `renderNetwork` 末尾（SVG 注入后）更新 `#network-hscroll-inner` 宽度：
  ```javascript
  var hInner = document.getElementById('network-hscroll-inner');
  if (hInner) hInner.style.width = canvasW + 'px';
  ```
- 确保 `#network-hscroll` 的 scroll 事件同步 `#network-body` 的 scrollLeft（恢复到旧版逻辑）

---

### 2. 节点分层重叠 — 分层算法根本修复
**现象**：大部分节点仍在同一层。
**原因**：当前分层逻辑 `layer[eid] = Math.max(layer[source]+1)` 对没有前驱的节点 layer 未设初始值，默认 `undefined` 导致计算异常。且 MARGIN_TOP=100 与标尺实际高度 52px 不匹配——标尺顶部到标尺底部是 52px，节点应从 52px 之后开始布局。

**修正**：重写 `calculateTimeParams` 中的分层部分：
```
1. 事件节点 Y = 标尺底部(52) + MARGIN_BELOW_RULER(16) + (layer-1) * LAYER_H(60)
   → 即 events[k].y = 52 + 16 + (layer-1) * 60
2. 对所有入度为0的源节点，初始 layer=1
3. BFS 推进：successor.layer = max(successor.layer, current.layer + 1)
4. 关键路径节点优先占据较低层号（即视觉上层），非关键节点向下推
5. 同一层的节点 y 坐标必须完全相同
```

---

### 3. 前锋线/今日线交互 bug
**现象**：开关"今日线"后前锋线反复移动。
**原因**：前锋线默认使用 `todayOff`（今日偏移）作为初始位置，但当 `showTodayLine=false` 切换为 `showTodayLine=true` 触发重渲染时，前锋线被重置到今日位置而非用户拖动后的位置。

**修正**：
- 前锋线的检查日期独立存储：新增全局变量 `_progressCheckDate`（或复用已有的 `_progressDate`）
- 首次渲染时 `_progressCheckDate = new Date()`（今日）
- 用户拖动前锋线后更新 `_progressCheckDate`
- 重渲染时前锋线位置 = `_progressCheckDate` 而非 `new Date()`（今日）
- 今日线和前锋线完全解耦：今日线位置始终基于 `new Date()`，前锋线位置基于 `_progressCheckDate`

---

### 4. 时间标尺 — 与甘特图完全一致
**要求**：时间标尺必须与甘特图标尺视觉完全一致（同一套逻辑）。

**甘特图目前标尺规格**（`Gantt.razor` RenderTimeHeader）：
```
双层结构：
├─ 上层 28px：yyyy/MM（每月一个区块），字体 600 bold #333, 12px
├─ 下层 24px：根据 dayWidth 三档
│   ├─ dayWidth > 20：日号 dd，周末六日灰底(#f0f0f0) + 灰字(#999)
│   ├─ dayWidth 10-20：ISO 8601 周次 "第N周"
│   └─ dayWidth < 10：月份 "N月"
├─ 总高 = 52px
└─ 竖线网格：年度粗线(#ccc 1.5px)，月度细线(#f0f0f0 0.5px)
```

**修正**：完全按照甘特图逻辑重写 `buildNetworkSvg` 中标尺部分。特别注意：
- 当前代码的下层标尺（日/周/月）`(d+1)` 逻辑不对——应该是从 `startDate` 推日期，取 `getDate()`
- 周末检测：`dow === 0 || dow === 6`，与甘特图一致
- 年度线：年份切换时绘制
- 月度线：月份切换时绘制

---

### 5. 节点拖拽 + 双击编辑功能修复
**两项独立 bug**：

**5a. 双击编辑**：当前双击绑定在 `svg.ondblclick`，但 `.net-event` 中的 `<circle>` 和 `<text>` 在前，`closest` 可能穿透。验证 `e.target.closest('.net-event')` 是否正常工作。

**5b. 拖拽**：`_netUpdateArrows` 中 `querySelector('[data-activity-id="..."]')` 与实际 SVG 属性 `data-arc-id` 不匹配（L549 vs L680）。**修正**：将 L680 改为：
```javascript
var g = svg.querySelector('[data-arc-id="' + actId + '"]');
```
同时检查 `_netFindConnected` 中 `_netActivities` 的数据来源是否与新版 `workArcs` 兼容。新版中 activities 数据在 `tp.workArcs` 而非全局 `_netActivities`，需在 `renderNetwork` 中同步：
```javascript
_netActivities = tp.workArcs;
```

**5c. 拖拽 mousedown 绑定**：在 `renderNetwork` 末尾（恢复 _netEventOffsets 之后）添加：
```javascript
if (svg) {
    svg.addEventListener('mousedown', function(e) {
        var g = e.target.closest('.net-event');
        if (!g || !_netLayout || !_netLayout.events) return;
        e.preventDefault();
        var eid = g.getAttribute('data-event-id');
        var off = _netEventOffsets[eid] || { x: 0, y: 0 };
        _netNodeDrag = { eventId: eid, group: g, startX: e.clientX, startY: e.clientY, offX: off.x, offY: off.y };
        g.style.cursor = 'grabbing';
    });
}
```

---

### 6. _netUpdateArrows 属性名不匹配
**已在第 5b 项中合并修复。**

---

## 约束
- **只改 `netplan.js`**（不碰 Network.razor 等 C# 文件）
- `dotnet build` 0 错误
- 甘特图功能不受影响
- 每修复一项，浏览器刷新验证后再做下一项
