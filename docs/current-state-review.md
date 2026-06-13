# NetPlan 网络图模块 — 现状说明

> 版本: 2026-05-12
> 本文档供第三方架构审核使用，对方不需要阅读项目代码。

---

## 一、项目背景

NetPlan 是一个工程网络计划编制工具，对标 CCProject / 梦龙等专业软件。核心功能包括甘特图（Gantt）和双代号时标网络图（Time-Scaled Network Diagram）。

技术栈：
- 前端：Blazor Server (.NET 8) + 原生 JavaScript (~2040 行单文件)
- 渲染：内联 SVG（JS 拼接 SVG 字符串）
- 数据库：SQLite（EF Core）
- 数据流：C# 序列化 JSON → JS 端的 `renderNetwork()` 函数

---

## 二、网络图核心功能现状

### 2.1 已完成功能

| 功能 | 状态 |
|------|------|
| 时标网络图 | ✅ 正常 |
| 逻辑双代号图 | ✅ 刚修复（之前存在严重 bug） |
| 事件合并（真双代号图） | ✅ 同 ES/EF 的工作共享事件节点 |
| 虚工作自动生成 | ✅ 从前置关系推算虚箭线 |
| 自由时差波形线 | ✅ 金色锯齿线在箭线末端 |
| 唯一起点/终点 | ✅ 自动插入虚拟 S/E 节点 |
| 前锋线 | ✅ 蓝色折线，支持拖拽检查日期 |
| 今日线 | ✅ 红色虚线 |
| 时间标尺 | ✅ 双层（年/月 + 日/周/月），底部镜像标尺 |
| 节点拖拽 | ✅ 弹出偏移天数输入框 |
| 画布平移 | ✅ 空白区拖拽 |
| 缩放 | ✅ 通过调整 dayWidth 重渲染 |
| 双击编辑/创建 | ✅ 与甘特图一致的对话框 |
| 纵向微调节点位置 | ✅ 通过 Y 偏移保留 |
| 行标注 | ✅ 左侧标注列 |
| 图属性设置 | ✅ 层高、节点半径、节点形状等 |
| 母线法 | ✅ 多进/多出节点汇聚短线 |
| 导出 SVG/PNG | ✅ |
| 打印预览 | ✅ |
| 进度曲线 | ✅ 标尺下方淡蓝折线 |

### 2.2 已知问题

| # | 问题 | 严重度 |
|---|------|--------|
| 1 | 虚箭线水平长度非零（应为纯垂直，当前画了 3 段折线） | 高 |
| 2 | 非关键路径箭线颜色为灰色（应为蓝色） | 中 |
| 3 | 非关键节点描边也为红色（应与箭线同色） | 中 |
| 4 | 虚工作颜色为灰色（应为蓝色虚线） | 中 |
| 5 | 过桥法未实现（箭线交叉时没有过桥标记） | 中 |
| 6 | 前锋线日期不持久（页面刷新丢失检查日期） | 中 |
| 7 | 缺少异步调用异常处理（DotNet.invokeMethodAsync 无 .catch） | 低 |

---

## 三、当前渲染流程

### 3.1 整体流水线

```
C# 层:
  读取 DB → TaskItem[] + TaskRelation[]
  → 序列化为 JSON (含 es/ef/ls/lf/tf/ff/isCritical 等)
  → 更新 Blazor 隐藏 input + 调用 JS.InvokeVoidAsync('renderNetwork', json, opts)

JS 层:
  1. calculateTimeParams(tasks, relations)
     - 每个 task 的 {es, ef} 映射到事件节点 T{timeOffset}
     - 同 es 共享开始事件, 同 ef 共享结束事件 → 真双代号
     - 从前置关系推虚工作的邻接边
     - 拓扑排序为事件编号

  2. applySingleStartEnd(data)
     - 检测多起点/多终点, 插入虚拟 TS / TE

  3. calculateVerticalLayout(data)
     - BFS 拓扑分层（非施工段）
     - 关键线路第 1 层, 非关键每传播一个边 +2 层
     - 按 TF 值微调
     - 层号压缩为连续 Y 坐标

  4. renderNetwork()
     - 确定 X 坐标: x = 80 + es * dayWidth
     - 计算 SVG 尺寸 (cx, cySize)
     - 调用 buildNetworkSvg({...}) 拼接 SVG 字符串
     - ce.innerHTML = svgStr
     - 绑定双击事件
     - 恢复节点偏移量 (_netEventOffsets)
     - 绑定节点拖拽 (mousedown → mousemove → mouseup)
     - 绑定前锋线拖拽
     - 绑定横向滚动条同步
     - 绑定画布平移 (mousedown → mousemove → mouseup)

  5. validateNetworkRender()  ← 新增自检
     - 检查 SVG 创建、布局数据、DotNet 引用
     - 检查虚工作水平长度是否为 0
     - 检查箭线长度偏差
```

### 3.2 SVG 结构（buildNetworkSvg）

```
<svg id="network-svg">
  <defs><marker id="arr-N" .../></defs>   <!-- 箭头标记 -->
  <rect/>  <!-- 时间标尺上层背景 -->
  <rect/>  <!-- 时间标尺下层背景 -->
  <text/>  <!-- 年/月标签 -->
  <text/>  <!-- 日/周/月标签 -->
  <line/>  <!-- 分隔线 -->
  <line/>  <!-- 竖线网格 -->
  <rect/>  <!-- 行背景（间隔色） -->
  <line/>  <!-- 行分隔线 -->
  <text/>  <!-- 左侧行标注 -->
  
  <!-- 虚箭线 -->
  <g class="dummy-rel" data-dummy-src="T{offset}" data-dummy-tgt="T{offset}">
    <path d="..." fill="none" stroke="#aaa" stroke-dasharray="5,3"/>
  </g>
  
  <!-- 实箭线 -->
  <g data-activity-id="{id}" data-src="T{es}" data-tgt="T{ef}">
    <path class="act-arrow" d="..." marker-end="url(#arr-N)"/>
    <text class="act-label"/>  <!-- 工作名称在上 -->
    <text class="act-dur"/>    <!-- 持续时间在下 -->
  </g>
  
  <!-- 波形线(自由时差) -->
  <path class="act-wave" .../>
  
  <!-- 事件节点 -->
  <g class="net-event" data-task-id="{tid}" data-event-id="T{offset}">
    <circle r="11"/>  <!-- 或 ellipse -->
    <text/>  <!-- 节点编号居中 -->
  </g>
  
  <!-- 母线法标记 -->
  <line/>  <!-- 当进出度≥3时 -->
  
  <!-- 今日线 / 前锋线 / 进度曲线 / 图例 / 底部标尺 -->
</svg>
```

---

## 四、关键全局状态（JS 端）

```javascript
_netLayout         // { events: { T{offset}: {id, taskId, type, num, es, ef, x, y, ...} }, ... }
_netActivities     // [{ id, source, target, es, ef, duration, isCritical, ... }]
_netSvg            // 当前 SVG DOM 元素引用
_netEventOffsets   // { eventId: { x, y } } — 节点拖拽的 SVG transform 偏移量
_netExtraSpacing   // { layerIndex: blankRowCount } — 用户插入的空行
_netDayWidth       // 当前时标比例（像素/天）
_progressCheckDate // 前锋线检查日期（仅浏览器内存）
_netDotNet         // Blazor JSInvokable 引用
```

---

## 五、当前交互机制

### 5.1 双击

```
SVG.ondblclick:
  e.target 向上查找:
    .net-event → data-task-id → C# OpenTaskEditor(taskId)
    [data-activity-id] → taskId → C# OpenTaskEditor(taskId)
    空白区域 → 从 X 坐标推算 dayOffset → C# ShowAddTaskModal(0, dayOffset)
```

### 5.2 节点拖拽

```
mousedown (SVG 上, e.target.tagName === 'circle'):
  → 设置 _netNodeDrag = { eventId, group, startX, startY, offX, offY }

mousemove (文档级, 通过 _netNodeDrag 判断):
  → dx = offX + (clientX - startX)  // 累计偏移
  → dy = offY + round((clientY - startY) / LAYER_H) * LAYER_H  // 吸附到行
  → group.setAttribute('transform', 'translate(' + dx + ',' + dy + ')')
  → _netUpdateArrows(eventId, dx, dy)

mouseup (文档级):
  → 保存最终偏移到 _netEventOffsets[eid]
  → 如果拖动过: 弹出 _showDayPopup(偏移天数, ...)
     → Enter/确定: C# SyncNodeDrag([tid], days, role)
     → ESC/取消: 恢复到拖拽前偏移
  → 如果没拖动(单击): 弹回原位, 清除偏移
```

### 5.3 画布平移

```
mousedown (#network-body 空白区):
  → panState = { x: clientX, sx: scrollLeft, ... }

mousemove (window 级):
  → netBody.scrollLeft = panState.sx - (clientX - panState.x)

mouseup: 清除 panState
```

### 5.4 数据流

```
C# 端:
  OnInitializedAsync → BuildData() → BuildDataInternal()
    → 读取 DB → 序列化 JSON → 更新 graphJson/optionsJson/renderToken
    → StateHasChanged() + JS.InvokeVoidAsync('renderNetwork', json, opts)

  RerenderGraph() (选项切换/缩放时):
    → 重新计算选项 JSON → renderToken++
    → StateHasChanged() + JS.InvokeVoidAsync('renderNetwork', json, opts)

JS 端 _triggerRerender:
  → 读 hidden input('#netplan-data', '#netplan-options')
  → 调用 renderNetwork(data, opts)
```

---

## 六、与目标规范的差距

近期对标了一份绘图规则文档，主要差距如下：

### 优先级1（铁律，必须满足）

| 规则 | 当前 | 差距 |
|------|------|------|
| 节点坐标对齐时标/区段线 | ✅ | 一致 |
| 实箭线长度 = 工期 × 比例 | ⚠️ | 起点偏移了半径(~11px) |
| **虚箭线水平长度 = 0** | ❌ | 当前为 3 段折线，水平段非零 |
| 波形线在末端、长度 = FF | ✅ | 一致 |
| 箭头编号 > 箭尾编号 | ✅ | 拓扑排序保证 |

### 优先级2（布局规则）

| 规则 | 当前 | 差距 |
|------|------|------|
| **按施工段/楼层排列** | ❌ | 当前用 BFS 拓扑分层，无施工段概念 |
| **同段水平、跨段垂直** | ❌ | 同上 |
| 禁止斜向箭线 | ✅ | 同层水平 + 跨层折线 |
| **过桥法** | ❌ | 完全禁用 |

### 优先级3（标注规则）

| 规则 | 当前 | 差距 |
|------|------|------|
| 名称在上，时间在下 | ✅ | |
| 节点编号居中 | ✅ | |
| 关键路径红色 | ✅ | |
| **非关键蓝色 + 空心节点** | ❌ | 当前灰色箭线 + 白色节点但红描边 |
| **虚工作蓝色虚线 + 无标注** | ❌ | 当前灰色虚线，无标注✅ |

### 优先级4（优化规则）

| 规则 | 当前 | 差距 |
|------|------|------|
| **同时间节点纵向错开** | ❌ | 事件合并后共享节点，无错开 |
| **过短箭线标注避让** | ❌ | 无此逻辑 |
| 自由时差0不画波形线 | ✅ | |

---

## 七、运行时潜在风险

在维护过程中已发现的隐藏问题：

1. **DOM 生命周期 vs 事件监听器** — 节点拖拽和画布平移的 mousedown/mousemove/mouseup 监听器在页面初始化时只绑一次。如果 Blazor 重新渲染了 DOM 子树，旧监听器绑在已移除的 DOM 上就失效了。

2. **全局状态叠加** — 拖拽偏移量 `_netEventOffsets` 在重渲染时恢复。当 `SyncNodeDrag` 触发布局重算后，新坐标上叠加旧偏移可能导致位置错误。

3. **层高调整与空行索引不匹配** — `_netExtraSpacing` 按层号存空行数，层高改变后渲染偏移不对应。

4. **异步调用异常静默** — 所有 `DotNet.invokeMethodAsync` 没有 `.catch()`，C# 端异常在 JS 端不可见。

5. **前锋线日期不持久** — 存在浏览器内存中，刷新丢失。

---

## 八、期望第三方审核的要点

1. **施工段分层方案** — 在「施工段」这个新维度下，现有的拓扑排序 + 事件合并模型能否复用？数据模型需要怎么扩展？

2. **虚箭线水平=0 的实现** — 时标网络图中，逻辑依赖的时间跨度应该怎么表达？如何在约束下保持竖向直线？

3. **过桥法检测策略** — 箭线交叉的检测时机（构建时/渲染时）和实现方式。

4. **着色方案** — 非关键路径蓝色、虚工作蓝色的具体色值和标注规则。

5. **布局优化优先级** — 在施工段、过桥、着色、标注避让这几项中，按实施顺序和依赖关系给出建议。

6. **架构风险** — 对于第 7 节列出的运行时风险，是否有更根本的解决思路。
