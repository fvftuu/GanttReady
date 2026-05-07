# NetPlan 项目全面总结

## 1. 项目概述
- **项目名称**：NetPlan（网络计划）- 进度计划管理系统
- **技术栈**：Blazor Server (.NET 10) + SQLite + Chart.js
- **运行时**：.NET 10.0.203，node 24.14.0
- **启动端口**：http://localhost:5000
- **核心功能**：甘特图、时标双代号网络图（SVG）、CPM调度引擎、资源管理、计划分析

## 2. 页面路由
| 页面 | 路由1 | 路由2 |
|------|-------|-------|
| 首页 | `/` | |
| 甘特图 | `/project/{ProjectId:int}/gantt` | |
| 网络图 | `/project/{ProjectId:int}/network` | |
| 资源 | `/project/{ProjectId:int}/resources` | |
| 分析 | `/analysis` | `/project/{ProjectId:int}/analysis` |

## 3. 项目结构
```
i:\NetPlan\
├── src/NetPlan.Server/
│   ├── Program.cs                    # 入口，DI注册，中间件
│   ├── GlobalUsings.cs
│   ├── Pages/
│   │   ├── Index.razor              # 首页（项目列表+新建/导入/导出+单选联动）
│   │   └── Project/
│   │       ├── Gantt.razor          # 甘特图页面
│   │       ├── Network.razor        # 时标网络图页面
│   │       ├── Resources.razor      # 资源管理页面
│   │       └── Analysis.razor       # 计划分析仪表板
│   ├── Shared/
│   │   ├── MainLayout.razor         # 全局顶部导航栏（navToChecked联动）
│   │   └── NavMenu.razor            # (未使用，保留的原始模板)
│   ├── Models/
│   │   ├── Project.cs              # 项目实体
│   │   ├── TaskItem.cs             # 任务实体（含CPM字段）
│   │   ├── TaskRelation.cs          # 任务关系（FS/SS/SF/FF）
│   │   ├── Resource.cs             # 资源实体（人工/材料/设备）
│   │   ├── ResourceAssignment.cs   # 资源分配
│   │   └── AnalysisResult.cs       # 分析结果DTO
│   ├── Services/
│   │   ├── ProjectService.cs / IProjectService.cs
│   │   ├── ScheduleEngine.cs / IScheduleEngine.cs  # CPM算法
│   │   ├── ResourceService.cs / IResourceService.cs
│   │   ├── AnalysisService.cs / IAnalysisService.cs
│   │   └── ExcelTemplateService.cs  # Excel导入导出
│   ├── Data/
│   │   └── NetPlanDbContext.cs     # EF Core + SQLite
│   └── wwwroot/
│       ├── css/site.css             # 全局样式
│       └── js/netplan.js            # 核心前端JS（968行）
```

## 4. 甘特图（Gantt.razor）

### 页头结构（header-left + header-right）
```
header-left: [项目下拉] [页面标题] [添加/删除/上移/下移/列设置按钮] [前锋线/今日线/关系线/资源投入 toggle]
header-right: [预览] [导出] [模板] [导入] [- 100% +] [🔍适应页面] [重算计划]
```

### 关键参数
- `zoomLevel`：默认100，范围1-3000，每次±5
- `dayWidth = 40 * (zoomLevel / 100)`：100%时每天40px
- `ZoomToFit()`：`targetDayW = (rightWidth - 20) / totalDays; newZoom = targetDayW / 40 * 100`

### 时间标尺3档（甘特图 & 网络图一致）
| dayWidth | 上层 | 下层 | 周末 |
|----------|------|------|------|
| >20px | yyyy/MM | dd（日） | 六日灰底(#f0f0f0)+灰字(#999) |
| 10-20px | yyyy（年） | 第N周（ISO 8601） | — |
| <10px | yyyy（年） | MM（月） | — |

## 5. 时标网络图（Network.razor + netplan.js）

### 架构
```
Network.razor (C#)
  → BuildData(): 从DB取task+relation → 序列化JSON → hidden input
  → 轮询检测 JS 端 token 变化
  → JS renderNetwork(): 解析JSON → calculateTimeParams() → calculateVerticalLayout() → buildNetworkSvg()
  → SVG innerHTML 注入 #cy 容器
  → 浏览器原生滚动（纵向+横向）
```
**CSS 关键**：`.page-main` 必须为 `display:flex; flex-direction:column`，否则 `.network-wrapper` 的 `flex:1` 在 block 上下文中无效，导致整条链撑破不滚动。

### SVG buildNetworkSvg 渲染层级
1. `<defs>` 箭头标记 + 渐变
2. 时间标尺：上层28px(yyyy/MM或年) + 下层24px(日/周/月) = 52px总高
3. 虚箭线（虚线L形路径）
4. 工作箭线（L形实线 + 箭头 + 名称/工期标注 + 自由时差波形）
5. 事件节点（`<circle>` + `<text>` 编号，class="net-event" data-task-id）
6. 今日线（红色虚线，y=0 到图底）
7. 前锋线（可拖动红色竖线 + 三角手柄 + 日期标签，group id="net-progress-line"）
8. 竖线网格（年度粗线+月度细线，贯穿全图）
9. 图例（关键线路/非关键/虚工作）
10. 标题（左上项目名 + 右上总工期 + 底部规程标准）

### 节点计算法布局（calculateTimeParams + calculateVerticalLayout）
- 正向传播计算 ES/EF，反向计算 LS/LF
- TF = LS-ES, FF = 后续ES-EF
- 垂直分层：拓扑BFS，关键路径在上层
- LAYER_HEIGHT = 60px（紧凑布局）
- X坐标 = MARGIN_LEFT(80) + ES * dayWidth

### 前锋线（JGJ/T121-2015 进度评估）
- ID：`net-progress-line` 可拖动 group
- 拖动事件：mousedown→记录初始位置(startX, startLineX, startScrollLeft)，mousemove→移动+补偿滚动增量+重算，mouseup→停止
- 公式：`Δ = actualCompletion% - clamp((checkDate-ES)/duration*100, 0, 100)`
- 绿色(#27ae60)：Δ>=0（正常/超前）
- 黄色(#f39c12)：-20<=Δ<0（轻微落后）
- 红色(#e74c3c)：Δ<-20（严重落后）或关键路径

### 页头样式（与甘特图对齐）
```
header-left: [项目下拉] [标题] [前锋线/今日线 toggle]
header-right: [🖨预览] [重算计划] [- 100% +] [🔍适应页面] [关键路径/时差 toggle]
```

### C# 关键字段
- `zoomLevel = 100`（double）
- `showCriticalHighlight / showFloatLabels / showProgressLine / showTodayLine`：均为bool
- `BuildData()` 生成 JSON 元素列表（tasks含 id/label/name/eventNum/duration/es/ef/ls/lf/tf/ff/completion/isCritical）+ 关系列表
- `optionsJson` 序列化传递：showCritical/showFloat/showTodayLine/showProgressLine/projectStartDate/totalDays/dayWidth/projectName
- 双击事件：`[JSInvokable] OpenTaskEditor(int taskId)` → 打开编辑对话框

## 6. 资源管理（Resources.razor）
- 分类标签栏（全部/人工/材料/设备）+ 彩色badge
- 多选+批量操作（全选/导出选中/批量删除）
- 批量导入导出Excel（9列模板，3个API端点）
- 共享/项目专属归属设置

## 7. 分析仪表板（Analysis.razor）
- 共享资源数统计卡片
- 各项目资源冲突概览表
- 跨项目资源使用详情

## 8. 构建和启动

### 从 bash（有 NuGet 环境限制）
```bash
# 不能 restore（NuGet path1 bug），需要用 --no-restore
dotnet build src/NetPlan.Server/NetPlan.Server.csproj --no-restore
dotnet run --project src/NetPlan.Server/NetPlan.Server.csproj --no-build
```

### 从 PowerShell（环境正常）
```powershell
dotnet run --project i:\NetPlan\src\NetPlan.Server\NetPlan.Server.csproj
```

### 停服务
```bash
powershell -Command "Get-Process 'NetPlan.Server' -EA 0 | Stop-Process -Force"
```

## 9. 重要教训和坑

1. **NuGet path1 bug**：bash 环境的 `Environment.GetFolderPath` 返回 null，导致 `dotnet restore` 失败。必须用 `--no-restore`。PowerShell 环境正常。

2. **Blazor script 标签限制**：`<script>` 标签内绝不能包含 `@变量` 绑定（DOM diff 会报 SyntaxError）。解决方案：用 hidden input + JS 轮询 token 检测。

3. **OnAfterRenderAsync 预渲染**：预渲染时服务端也执行，JS Interop 会抛异常。需要 try/catch 包裹。

4. **文件锁定**：`dotnet build` 前必须先 `Stop-Process NetPlan.Server`，否则 exe 被锁。

5. **CSS overflow 链**：`.page-main` 必须 `display:flex; flex-direction:column; min-height:0`，否则子孙 flex 容器不约束高度。`#cy` 不能设 `height:100%`（会限制 SVG 撑开），只设 `min-height:400px`。

6. **首页单选联动**：首页用 radio 单选项目（`checkedProjectId` 存 localStorage），MainLayout 导航用 `navToChecked(page)` 跳转到勾选项目页面。所有页面下拉框只显示勾选项目（无勾选时展示全部）。JS 提供 `getCheckedProject()` / `setCheckedProject(id)` / `navToChecked(page)`。

## 10. netplan.js 关键函数索引

| 函数 | 行数(approx) | 用途 |
|------|-------------|------|
| `syncRightToLeft/syncLeftToRight` | 6-27 | 甘特图纵向滚动同步 |
| `initPanelResize` | 49 | 甘特图左侧面板拖拽 |
| `calculateTimeParams` | 232 | CPM节点计算法（正向+反向） |
| `calculateVerticalLayout` | 354 | 拓扑BFS垂直分层 |
| `getISOWeek` | 240 | ISO 8601 周次计算 |
| `svgArrowMarker` | 460 | SVG箭头标记生成 |
| `buildNetworkSvg` | 473 | 完整SVG构建器（标尺+箭线+节点+前锋线） |
| `updateProgressColors` | 710 | JGJ/T121-2015 进度评估+节点着色 |
| `renderNetwork` | 772 | 主渲染入口，解析JSON→计算→构建SVG→绑定事件 |
| `networkFit` | 878 | 适应视图（CSS scale缩放） |
| `getActiveProject/setActiveProject` | 894 | localStorage项目选中状态 |
| `getCheckedProject/setCheckedProject` | 936 | localStorage首页单选（跨标签联动） |
| `navToChecked` | 944 | 导航到勾选项目的指定页面 |
| `startNetworkPoller` | 908 | 轮询检测token变化触发渲染 |
