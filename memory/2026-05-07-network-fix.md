# 时标双代号网络图 — 第二轮修正

## 背景
第一轮重构完成了事件合并（按时间点 T{offset}），方向正确。但渲染效果有严重退化，需要逐项修正。

---

## 修复清单（优先级从高到低）

### 1. 节点挤在一起 — 分层算法未生效
**现象**：几乎所有节点都在同一 Y 坐标，重叠在一起。
**原因**：`calculateVerticalLayout` 入度为 0 的节点全部 layer=1，且 `allArcs` 合并 workArcs + dummyArcs 时，dummyArcs 的 source/target 使用了在第一步之后新增的 dummy 事件节点（如 `T{mid}`），这些节点没有被重排编号，且它们的入度/邻接关系没有正确建立。

**修正**：
- 移除 `calculateVerticalLayout`，改为在 `renderNetwork` 中直接分配 Y 坐标：
  - 从 `calculateTimeParams` 返回时，按拓扑 BFS 给事件分层（关键路径优先 layer=1，其余递增）
  - BFS 必须在 evts 重编号之后进行
  - 确保 workArcs + dummyArcs 全部参与邻接构建
  - 每层 LAYER_H=60px, MARGIN_TOP=100px

---

### 2. 箭头线和节点同层重叠
**原因**：分层失败导致所有内容堆在同一层。
修正第1项后本问题自然解决。

---

### 3. 前锋线 + 今日线功能缺失
**现象**：新版 `buildNetworkSvg` 完全没有前锋线和今日线的代码。
**修正**：
- 从旧版移植今日线逻辑（`showTodayLine` 参数已在传入，红虚线 + "今日"标签）
- 从旧版移植前锋线（`showProgressLine` 参数已在传入）：
  - `id="net-progress-line"` 可拖动 group
  - 红色竖线 + 三角手柄 + 日期标签
  - 拖动事件使用 `_dragInfo` 全局状态（与旧版一致）
- 移植 `updateProgressColors` 函数（JGJ/T121-2015 进度评估着色）
- 前锋线拖动后自动调用 `updateProgressColors`

---

### 4. 时间标尺与甘特图不一致
**现象**：新版标尺是单层（只有 yyyy/MM + 日号），与甘特图的 3 档模式不同。
**修正**：恢复双层标尺结构（上层 28px + 下层 24px），分三档：
- dayWidth > 20：上层 yyyy/MM，下层 dd（日号），周末六日灰底(#f0f0f0)+灰字(#999)
- dayWidth 10-20：上层 yyyy，下层"第N周"（ISO 8601）
- dayWidth < 10：上层 yyyy，下层 MM（月号）
- 加竖线网格（年度粗线 #ccc 1.5px，月度细线 #f0f0f0 0.5px）
- 加水平行线（层间 #e8e8e8 dashed）

参考甘特图标尺代码（Gantt.razor 中 `RenderTimeHeader` 逻辑），保持视觉一致。

---

### 5. 节点拖拽 + 双击编辑功能缺失
**现象**：节点双击无反应，拖拽无反应。
**原因**：新版 `renderNetwork` 中：
- 双击事件绑定了 `svg.ondblclick`，但事件节点是用 `<g class="net-event" transform="...">` 包裹的，circle 遮挡了 click 事件
- 拖拽的 `mousedown` 绑定在 renderNetwork 中未调用 `_netNodeDrag` 的初始化

**修正**：
- 确认 `.net-event` 的 circle 添加 `pointer-events: none`（或移入顶层 text）
- 双击事件保持不变（`e.target.closest('.net-event')`），从 `startTasks[0].id` 获取 taskId
- mousedown 拖拽：保持旧版的 `_netNodeDrag` 绑定逻辑不变
- 确认 `_netUpdateArrows` 和 `_netUpdateDummys` 使用的是新的 `data-src`/`data-tgt` 标签名（对应新版 SVG 结构）

---

### 6. 虚工作 toggle-switch（UI 开关）
**修正文件**：`src/NetPlan.Server/Pages/Project/Network.razor`
- 在页面 header 添加虚工作开关：
```razor
<label class="toggle-switch" title="显示虚工作">
    <input type="checkbox" @bind="showDummyArrows" @bind:after="RerenderGraph" />
    <span class="toggle-slider"></span>
</label>
<span class="toggle-label">虚工作</span>
```
- 在 `@code` 块中添加 `private bool showDummyArrows = true;`
- 在 `optionsJson` 序列化和 `RerenderGraph` 中传递 `showDummyArrows`
- 注意：`RerenderGraph` 是 fire-and-forget (`_ = RerenderGraph()`)，`@bind:after` 调用前需确保 `showDummyArrows` 已更新——Blazor 的 bind 在 after 回调前已完成赋值

---

## 约束
- **只改两个文件**：`netplan.js` (主要) + `Network.razor` (仅第6项)
- 不改 C# 后端任何其他文件
- `dotnet build` 0 错误
- 保持甘特图功能不受影响
