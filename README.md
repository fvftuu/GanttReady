# GanttReady — 进度计划管理系统

> 一个开源的、带 AI 助手的项目进度管理工具。  
> 甘特图 · 关键路径 · 挣值分析 · 资源管理 · AI 辅助创建

---

## 快速开始

```bash
# 克隆
git clone https://github.com/YOUR_USER/GanttReady.git
cd GanttReady/src/GanttReady.Server

# 运行（首次自动还原 NuGet 包 + 创建 SQLite 数据库）
dotnet run
```

打开 `http://localhost:5000` 即可使用。

## 功能一览

| 功能 | 说明 |
|:-----|:------|
| 甘特图 | 交互式甘特图，支持拖拽、缩放、三种时间标尺 |
| 关键路径 CPM | 自动计算关键路径、总时差、最早/最晚时间 |
| 挣值管理 EVM | SPI、CPI、SV、CV、BAC、EAC、趋势图 |
| 资源管理 | 资源分配、负荷图、资源平衡 |
| 工作日历 | 自定义工作日/节假日，自动排程 |
| 分析报告 | 实施分析、月度报告、年度总览、多项目健康度矩阵 |
| AI 助手 | 自然语言描述项目，AI 自动创建任务/排期 |
| 导出 | Excel 导出 |

## 技术栈

- **框架**：Blazor Server (.NET 8)
- **数据库**：SQLite (EF Core)
- **前端**：原生 JavaScript + Chart.js
- **AI**：DeepSeek / GLM 大语言模型

## 许可证

MIT License — 详见 [LICENSE](LICENSE) 文件。

## 状态

当前版本 **v1.7i** — 核心功能基本稳定，欢迎提 Issue 和 Pull Request。
