# 时标双代号网络图重构

## 问题根因
当前实现是"单代号网络图的节点拆分 + 双代号视觉皮"：每个任务独立拥有 S{id}→E{id} 事件，没有做事件合并。这违背了双代号网络图的根本定义——**事件节点按时间点合并，工作箭线连接合并后的事件**。

---

## 重构目标
将 `netplan.js` 中的网络图渲染改为**真正的时标双代号网络图**（符合 JGJ/T 121-2015），分四步落实。

---

## 第一步：事件合并（`calculateTimeParams` 重写）

**核心逻辑**：把当前"每个任务拆 S/E"的模型改为"按时间点合并事件"。

### 输入
- tasks: `[{id, duration, es, ef, ls, lf, tf, ff, isCritical, label, name}]`
- relations: `[{source, target, relType, lag}]`

### 算法
```
1. 收集所有时间点：
   - 对每个 task，收集 ES（开始时间点）和 EF（结束时间点）
   - 去重：同一 ES 值的 task 共享同一个开始事件节点
   - 去重：同一 EF 值的 task 共享同一个结束事件节点
   
2. 建立合并后的事件节点：
   - eventId 格式改为 "T{日期偏移}"（例：T0, T15, T30）
   - 记录该时间点有哪些 task 开始/结束
   - 事件编号从1开始，按时间顺序从左到右编号

3. 重新定义 activities：
   - 每个 task 变成一条 workArc：source=T{es}, target=T{ef}
   - 保留 duration, code, name, isCritical, tf, ff, completion

4. 根据 relations 自动生成虚工作：
   - 对每条 relation(source=A, target=B, type=FS, lag=L)：
     如果 A.EF + L ≠ B.ES（有间隙），在 T{A.EF+L} 到 T{B.ES} 之间生成虚箭线
   - 如果多个 task 共享事件，需额外插入 dummy 节点避免逻辑交叉
   - 虚工作 duration=0, isDummy=true
```

### 输出格式
```
{
  events: {
    "T0": { id:"T0", num:1, timeOffset:0, tasks:["A1","A3"], x:..., y:... },
    "T15": { id:"T15", num:2, timeOffset:15, tasks:["A2"], x:..., y:... },
    ...
  },
  workArcs: [
    { id:1, source:"T0", target:"T30", duration:30, code:"A1", name:"...", isCritical, tf, ff, isDummy:false },
    ...
  ],
  dummyArcs: [
    { source:"T30", target:"T60", isDummy:true },
    ...
  ]
}
```

**验证标准**：同一个时间点（同一天偏移）的所有 task 开始/结束事件必须合并为同一个节点。节点编号按时间从左到右顺序。

---

## 第二步：垂直分层（`calculateVerticalLayout` 改写）

基于合并后的事件图重新分层：

```
1. 构造事件邻接表（workArcs + dummyArcs 形成的有向图）
2. BFS 分层：
   - 关键路径上的事件分配到第1层（最上层）
   - 非关键事件按拓扑距离向下分配
   - 同层事件 Y 坐标相同（LAYER_HEIGHT=60px, MARGIN_TOP=100px）
3. X 坐标 = MARGIN_LEFT(80) + timeOffset × dayWidth
   - 同一时间点的事件 X 坐标必须相同（已在第一步保证）
```

---

## 第三步：SVG 绘制（`buildNetworkSvg` 改写）

### 时间标尺（不变）
保持与甘特图对齐的时间标尺：上层 yyyy/MM，下层日/周/月，周末高亮。

### 事件节点
- 圆形，半径 NODE_R=17
- 内部显示事件编号（1, 2, 3...）
- 关键节点红色填充(#e63946)，非关键白色填红边
- 位置：`cx=事件X坐标, cy=事件Y坐标`
- 添加 `data-event-id` 属性用于拖拽/双击

### 工作箭线（workArcs）
```
1. 从源事件右边缘 → 目标事件左边缘，L形路径
2. 水平段长度 = duration × dayWidth（严格等于持续时间对应的像素宽度）
3. 箭线上方标注：代号 + 名称
4. 箭线下方标注：duration + "d"
5. 关键箭线：红色(#e63946) 3磅加粗
6. 非关键箭线：深灰(#333) 1.5磅
```

### 虚箭线（dummyArcs）
```
1. 垂直虚线：源事件下方 → 目标事件上方
2. 灰色(#aaa)，stroke-dasharray="5,3"
3. 不需箭头，不需标注
```

### 自由时差波形线
```
仅对非关键工作：从 workArc 的目标事件右边缘出发，水平波形延伸 FF像素
颜色：#FFD700，stroke-dasharray="4,3"
```

### 图例
右下角，四项：关键线路 / 非关键工作 / 虚工作 / 波形线

---

## 第四步：今日线与前锋线（保持不变或微调）

- 今日线：红色虚线贯穿全图
- 前锋线：可拖动的红色竖线 + 三角手柄 + 日期标签
- 前锋线拖动后 `updateProgressColors` 给节点着色
- **注意**：前锋线评估公式简化为 `delta = actualPct - clamped(progressDay, ES, EF)/duration*100`，不追求复杂公式

---

## 关键约束

1. **不改变 C# 后端**：`Network.razor` 和 `ScheduleEngine` 不改，只改 `netplan.js`
2. **不改变数据传递格式**：后端传的 JSON 结构不变（tasks + relations），事件合并在 JS 端完成
3. **不破坏现有功能**：节点拖拽、双击编辑、缩放、前锋线拖动全部保留
4. **构建验证**：`dotnet build` 0 错误，浏览器端验证网络图正常渲染

---

## 验证清单

- [ ] 同一时间点的多个 task 共享同一个事件节点
- [ ] 节点编号按从左到右时间顺序递增
- [ ] 工作箭线水平段 = duration × dayWidth（严格）
- [ ] 虚箭线只为 relation 有间隙或逻辑必要的情况生成
- [ ] 关键路径红色连续，从起点到终点不间断
- [ ] 双击节点打开编辑对话框正常
- [ ] 节点拖拽后箭线跟随更新
- [ ] 缩放（+/-/适应页面）正常
- [ ] 前锋线拖动正常
- [ ] 今日线显示正常
- [ ] 图例显示正常
- [ ] 甘特图功能不受影响
