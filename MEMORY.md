# MEMORY.md — 永久的记忆

## 用户的个人记忆库
- 路径：`I:\My memory\`
- 新会话必读：`INDEX.md`
- 包含：`INDEX.md`、`projects.md`（17个项目知识库）、`skills.md`（Agent Skills设计范式）、`workflows.md`（历史方案）、`preferences.md`（个人偏好和约定）

## NetPlan 项目

### 架构决策
- **技术栈**：Blazor Server (.NET 8) + 原生 JS（SVG 渲染）
- **数据库**：SQLite (EF Core)
- **2026-05-15 决策**：JS 端用 TypeScript + 模块化重写，一次性完成 V6.1 所有功能
- 不改变后端架构，不换 Canvas

### V6.1 清单
V6.1 功能对标清单在 `memory/2026-05-10-v61-checklist.md`。用户排除项：C2(横道图增加工作)、C4(流水布图)、C5(子网)、C6(多视图同步)、E2(分组)、E3(过滤)。

### 当前状态（2026-05-15）
- Step1(核心bug修复)+Step2(质量基建)+Step3(体验打磨) 已合并到 master
- `netplan.js` ~114KB 单文件，含网络图+甘特图全部渲染交互
- 已修问题：前锋线折线、颜色统一(#1890ff)、localStorage持久化、catch处理、过桥法(部分)
- 仍有问题：虚工作开关、过桥弧绘制、节点拖拽弹回(有待TS重构解决)
- 测试：`test/core-algorithms.js` 24个测试用例

### 项目规范
- 修改前：`node --check netplan.js` + `node test/core-algorithms.js` + `dotnet build`
- 修改后：同样流程 + 浏览器验证
- 所有修改走分支 → merge master
- CHANGELOG 必须更新
- 开发规范详见 `.workbuddy\memory\MEMORY.md`（Blazor 规范：`<script>` 不用 `@变量`，用 hidden input 轮询；`OnAfterRenderAsync` 需 try/catch）

## 用户技术偏好（详见 I:\My memory\preferences.md）
- .NET 8 Blazor Server，SQLite EF Core
- 中文注释和变量名混用
- 直接干练沟通风格，跳过废话前缀
- 复杂任务先展示分析再执行
- 批量操作分批次

## OpenClaw 配置
- 入口：`C:\Users\REX\AppData\Roaming\LobsterAI\config\openclaw.json`
- 技能目录：`~/AppData/Roaming/LobsterAI/SKILLs/`
- 工作空间：`I:\NetPlan` (main)
- OpenSpace MCP 桥接：`http://localhost:4876`，secret: `openspace-bridge-secret`
