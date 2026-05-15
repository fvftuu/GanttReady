# NetPlan 项目记忆

## 项目概述
- **项目名称**: NetPlan（网络计划）- 进度计划管理系统
- **技术栈**: Blazor Server + .NET 8 + SQLite
- **核心功能**: 甘特图、双代号网络图、CPM调度引擎、资源管理

## 已完成工作
- [x] 项目架构搭建
- [x] 甘特图功能重构（冻结窗格 + 双击编辑）
  - 左侧任务列表固定，右侧甘特图可横向滚动
  - 双击任务行打开编辑对话框
  - 任务选中高亮
  - 关键任务红色高亮
  - 时间轴周末高亮
- [x] 甘特图头部布局优化（header-left/header-right布局）
- [x] 缩放控制按钮（+ / - 按钮，每次5%幅度）
- [x] 首页添加删除项目功能（选中项目后显示删除按钮）
- [x] 甘特图左侧添加/删除任务图标按钮
- [x] Open Iconic图标库集成
- [x] 样例工程数据扩展（30任务/3年跨度）
- [x] 甘特图前置关系线绘制（竖线 + SVG箭头线）
- [x] 任务拖拽调整日期
- [x] 条形颜色规则（按状态/里程碑）
  - 5种颜色状态：未开始(蓝)、进行中(橙)、已完成(绿)、关键(红)、里程碑(紫)
  - 条形内部进度填充条显示完成百分比
  - 左侧列表添加状态徽标圆点
  - 编辑对话框新增里程碑复选框
  - 样例数据设置完成率(100%/65%/30%)和里程碑标记

## 技术细节
- 甘特图缩放: 5%-1500%
- 行高: 36px, 条形高度: 22px
- 冻结窗格宽度: 560px -> 720px -> 742px（含状态列）
- JS同步滚动: wwwroot/js/netplan.js
- 时间标尺显示逻辑:
  - dayWidth > 20px: 上层 yyyy/MM，下层 dd
  - dayWidth 10-20px: 上层 yyyy，下层 MM/W
  - dayWidth < 10px: 上层 yyyy，下层 MM

## 最新更新 (2026-05-01)
- [x] 修复 OpenXml 程序集缺失：csproj 添加 `DocumentFormat.OpenXml` 2.16.0 显式引用
- [x] 资源页面：共享/项目专属归属设置（Radio按钮 + 项目下拉）
- [x] 资源页面：批量导入导出（Excel 9列模板，3个API端点）
- [x] 资源页面：分类标签栏（全部/人工/材料/设备）+ 彩色badge
- [x] 资源页面：多选+批量操作（全选/导出选中/批量删除/取消选择）
- [x] 构建验证通过：0错误 13警告，DLL 340KB，OpenXml.dll 在输出中

## 最新更新 (2026-05-02)
- [x] 甘特图周模式：下层改为"第X周"显示（ISO 8601 周次）
- [x] 手动排程保护：TaskItem 新增 IsManualSchedule 字段，拖拽自动标记
- [x] CPM 引擎适配：手动排程任务保留拖拽日期（取 max(CPM, manual)）
- [x] 状态栏总工期：改为从 tasks.Max(PlanEndDate) 实时计算
- [x] 编辑对话框：新增 🔒手动排程 checkbox

## 最新更新 (2026-05-03)
- [x] 网络图关键路径高亮（红节点+红边）
- [x] 网络图时差可视化（三行节点：ES|工期|EF / 代号 / LS|TF|LF）
- [x] 网络图双击节点编辑（JSInvokable + DotNetObjectReference）
- [x] 网络图工具栏（重算CPM、高亮开关、时差开关、适应视图）
- [x] 资源冲突检测（分析页，按月统计峰值与容量比对）
- [x] ResourceService 新增 GetAssignmentsByProjectAsync
- [x] 关键修复：网络图用轮询 token 方案替代 script 内嵌变量（避免 Blazor DOM diff appendChild 错误）

## 最新更新 (2026-05-05)
- [x] 网络图节点拖拽微调：拖动事件节点 → 同步更新连接箭线（transform + updateArrows）
- [x] SVG 自适应高度：cySize 按实际 Y 坐标范围计算而非固定公式
- [x] 甘特图拖拽防上移：hideDragGhost 从 Blazor 序列化调用改为原生 JS dragstart 事件
- [x] 首页勾选改单选（checkbox→radio），下拉框恢复全量（首页仅做导航入口）
- [x] 工作箭线包裹在 `<g data-activity-id>` 中便于拖拽联动

## 最新更新 (2026-05-04)
- [x] 网络图时间标尺对齐甘特图：周末高亮（六日灰底+灰字）+ 日数修复（d<=td）
- [x] 网络图纵向滚动修复：根因 `.page-main` 为 block 容器 → 改为 `display:flex; flex-direction:column`
- [x] 网络图 `#cy` 去 `height:100%`（只留 `min-height:400px`），清理重复 CSS 规则
- [x] 首页项目单选联动：radio 单选 → localStorage → `navToChecked(page)` 导航
- [x] 所有页面（Gantt/Network/Resources/Analysis）下拉框只显示勾选项目
- [x] 前锋线拖动补偿校正：`startScrollLeft` 记录初始滚动位置，用增量补偿
- [x] JS 新增：`getCheckedProject()` / `setCheckedProject(id)` / `navToChecked(page)`

## 时间标尺更新
- 日模式新增周末列：周六日背景 `#f0f0f0`，文字 `#999`（甘特图 & 网络图一致）

## 首页导航更新
- 首页 radio 单选项目，存入 localStorage key `netplan_checked`
- MainLayout 导航链接改为 `javascript:navToChecked('gantt')` 等
- 无勾选项目时导航回首页；有勾选跳转到对应项目页面

## 开发规范
- 编译后自动启动测试
- Blazor @onscroll 事件绑定在 .NET 10 有兼容性问题，使用原生JS事件处理
- 新增 NuGet 依赖：DocumentFormat.OpenXml 2.16.0（显式引用，解决 ClosedXML 传递依赖缺失）
- 沙箱构建：nuget.exe 手动 restore + dotnet build --no-restore（因 Environment.GetFolderPath 限制）
- **重要教训**：Blazor Server `<script>` 标签内绝对不能包含 `@变量` 绑定（会被DOM diff解析出错，SyntaxError: appendChild）。解决方案：用 hidden input 传数据，用 JS 轮询检测变化。
- **重要教训**：OnAfterRenderAsync(firstRender=true) 在预渲染时也会执行（服务器端，无浏览器），JS Interop 会抛异常，需要 try/catch 包裹。

## 已知问题清单 (2026-05-05 审计·修正版)

> 经内部复查 + 外部审计交叉验证，12+4=16 项。**2026-05-06 修复完成 14/16 项**，2 项留待后续

### 🔴 严重 (3 项) — ✅ 全部已修复
1. ~~NuGet.Config 空源~~ → 已添加 nuget.org 源；根因是 Git Bash 缺少 `PROGRAMFILES` 环境变量
2. ~~csproj 12 个 HintPath~~ → 已全部移除
3. ~~SeedDemoData 死代码~~ → 已删除不可达删除逻辑

### 🟡 中等 (7 项) — ✅ 6 项已修复，1 项 TODO
4. ~~CSS 冗余规则~~ → 已删除 L21 scrollbar 隐藏、重复 histogram-legend、旧 gantt-bar 区块
5. ~~遗留文件~~ → 已删除 cytoscape-dagre.js/nuget 备份/WeatherForecast
6. ~~Program.cs 过长~~ → 已移除 MigrateSchema，模板生成待后续拆分
7. ~~ResourceService UnitPrice 覆盖~~ → 加 `!IsEmpty()` 守卫
8. ~~ParseDuration 格式不完整~~ → 已支持 PTxxD/PTxxM，用 `Ceiling` 向上取整
9. ~~RenderChartAsync 静默失败~~ → catch 设 `chartError`，markup 显示错误横幅
10. ~~RefreshFloatWarnings 时序~~ → LoadTasks 改为始终重算 CPM（30任务毫秒级），CPM 引擎已正确处理 IsManualSchedule

### 🟢 低优先级 (6 项) — ✅ 3 项已修复
11. ~~根目录混乱~~ → 已清理 *.txt/*.log/*.png 临时文件
12. ~~手动 ALTER TABLE~~ → 已迁移到 EF Core Migration（`Migrations/InitialCreate`）
13. 0 单元测试 — 后续
14. ~~版本描述不一致~~ → MEMORY.md .NET 10→8，global.json 确认 8.0.201
15. ~~ES/LS int? 文档化~~ → ScheduleEngine.cs 添加 `<remarks>` XML 注释
16. TaskRelation 缺 ProjectId — 后续
