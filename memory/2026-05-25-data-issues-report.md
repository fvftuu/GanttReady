# AI 分析数据失真的完整调查报告

## 问题现象

以"数字城市智能交通管理系统"（ID=8）为例：

1. **GV-01 项目立项审批**（甘特图 100% 完成）
   - AI 报告说 "已完成50%，2/4子任务" → 甘特图与实际不符
   
2. **GV-06 智能信号控制系统**（计划 2026-04-22 → 2026-08-19, 24% 完成率）
   - AI 报告说 "未开始 滞后约34天" → 状态判定错误

3. **所有项目的进度偏差卡片**显示巨大的负数（-51,666 ~ -1,070,909）
   - 用户质疑数据的准确性

---

## 根因分析

### 根因 1：种子数据缺失关键字段（最根本的问题）

`DemoProjectSeeder.cs` 创建任务时**从未设置以下字段**：

| 缺失字段 | 影响 |
|----------|------|
| `ActualStartDate` | AI 无法区分"已开工"和"未开始" |
| `ActualEndDate` | 已修（昨晚上次 commit），但只对 Completion >= 100 的任务设了值，未对已开始但未完成的设 |
| 大多数任务的 `CompletionPercentage` | 只有里程碑是 100%，其余大多数是 0%。GV-06 显示 24% 完成率，但那是用户后改的，种子默认是 0% |

**后果**：AI 通过 `get_tasks` 看到 GV-06 是 0% 完成、无实际开工日期 → 自动判为"未开始"。
服务器 `GetScheduleVarianceAsync` 虽判为 `in_progress`（因为 Today < PlanEnd），但 AI 自行用 `Today - PlanStart` 算出"滞后34天"并覆盖了工具结果。

### 根因 2：阶段数据与任务数据混用

AI 同时调用了 `get_stage_completion` 和 `get_tasks`：

- `get_stage_completion` 返回阶段A=50%(2/4任务完成)，包含任务 GV-01~GV-04
- AI 把"阶段A 的 2/4"张冠李戴到 GV-01 上，说成"2/4子任务"

### 根因 3：EVM 数值巨大因所有任务完成率=0%

`GetEarnedValueAsync` 算出的 SV = EV - PV：
- EV = Σ(taskBudget × Completion% / 100) ≈ 0（所有任务 0%）
- PV = Σ(taskBudget × plannedPct) > 0（随时间增长）
- SV ≈ -PV → 巨大负数

这不是 bug，是**种子数据不合理**——大多任务有预算但完成率是 0%。

### 根因 4：服务器日期 vs 项目日期跨度

服务器 `DateTime.Today` = 2026-05-25。但数字城市项目跨度到 2027-07-16。在 2026-05-25 这个时间点，项目才进行了 78 天（总共 495 天），大部分任务尚未开始或处于早期。这是正常的项目阶段，但 AI 和卡片显示的数据看起来像"严重滞后"。

---

## 已做的修复

| 修复 | 文件 | 效果 |
|------|------|------|
| 提示词加当前系统日期 | `ChatPromptBuilder.cs` | AI 不再猜日期 |
| BuildAiReportData 加分析基准日期 | `Analysis.razor` | 分析报告含日期 |
| 禁用 localStorage 旧缓存 | `AIChatPanel.razor` | 不再用旧报告混淆 AI |
| get_tasks 加 ActualStartDate 显示 | `AIChatPanel.razor` | AI 能看到"实际开工日期" |
| 阶段数据加【阶段】前缀 | `AIChatPanel.razor` | 减少阶段/任务混淆 |
| get_stage_completion 工具描述强调阶段分组 | `AiToolDefinitions.cs` | |
| 偏差卡片去掉"天"后缀 | `Analysis.razor` | SV 不再误标为天数 |
| GetScheduleVarianceAsync 重写 | `AnalysisService.cs` | 未开始/进行中不纳入偏差 |
| 模板替换 {pid} 修复 | `ChatPromptBuilder.cs` | 双大括号→单大括号 |
| 链式调用 400 修复 | `AIChatPanel.razor` | 纯文本代替 tool_calls 接力 |
| 种子数据补 ActualEndDate | `DemoProjectSeeder.cs` | 重建数据库后生效 |

## 仍未修的问题（需要新session修复）

### 🔴 P0：种子数据缺少 ActualStartDate

`DemoProjectSeeder.cs` 中没有任何任务设置 `ActualStartDate`。
即使 GV-06 的 PlanStart=2026-04-22 且已过 33 天，`ActualStartDate` 仍为 null。
AI 看到 "0% 完成 + 无实际开工日期" → 判"未开始"。

**修复方案**：
```csharp
// 在 DemoProjectSeeder.cs 创建任务时添加：
ActualStartDate = td.Completion > 0 ? td.Start : null,
```
对于完成率 > 0 的任务，设 ActualStartDate = PlanStartDate。
这样 AI 看到"实际开工 2026-04-22，0%完成"就知道是"进行中但进度缓慢"，不是"未开始"。

### 🔴 P0：种子数据大多任务完成率是 0%

GV-03~GV-13 全部是 0% 完成率。只有里程碑 GV-04(大件安装)和 GV-12(试运行)是 100%。
即使项目已进行 78 天，大多数任务仍是 0%。

**修复方案**：对已过 PlanStart 的任务设合理的默认完成率（如按时间进度百分比），或至少在代码中注释说明这是种子数据的问题。

### 🟡 P1：EVM SV 巨大负数

同上，因为完成率=0% + 有预算 → EV≈0 → SV≈-PV。
不是代码 bug 但用户体验极差。

### 🟡 P1：Analysis.razor 无 OnParametersSetAsync

URL 直接导航切换项目时数据不刷新。

### 🟢 P2：分析基准日期可以改为用户可配置

目前所有分析硬编码 `DateTime.Today`，无法按历史日期回看。
