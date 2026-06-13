# GanttReady — Project Schedule Management Tool

[![Star](https://img.shields.io/github/stars/fvftuu/GanttReady?style=social)](https://github.com/fvftuu/GanttReady)
[![License](https://img.shields.io/github/license/fvftuu/GanttReady)](LICENSE)
[![.NET](https://img.shields.io/badge/.NET-8.0-512BD4)](https://dotnet.microsoft.com/download)

> An open-source project management tool with an AI assistant.  
> Gantt Chart · Critical Path Method (CPM) · Earned Value Management (EVM) · Resource Management · AI-Powered Scheduling

---

## Quick Start

```bash
# Clone
git clone https://github.com/fvftuu/GanttReady.git
cd GanttReady/src/GanttReady.Server

# Run (auto-restores NuGet packages and creates SQLite database on first launch)
dotnet run
```

Open `http://localhost:5000` in your browser.

The first launch opens with an empty project. You can:
- Click **Create Project** on the homepage to add tasks manually
- Or open the **AI Assistant** (bottom-right corner) and type something like: *"Create a software project starting July 1st, divided into requirements, design, development, and testing phases"*

## Screenshots

*AI Assistant creating a project via natural language*
![Example](docs/screenshots/%E7%A4%BA%E4%BE%8B.png)

*Project homepage and overview*
![Home](docs/screenshots/%E9%A6%96%E9%A1%B5.png)

*Progress analysis and EVM reports*
![Analysis](docs/screenshots/%E5%88%86%E6%9E%90.png)

> Note: Screenshots are PNG files. For a faster experience, clone the repo and view locally.

## Features

| Feature | Description |
|:--------|:------------|
| Gantt Chart | Interactive Gantt chart with drag & drop, zoom, and three time scale modes |
| Critical Path (CPM) | Auto-calculates critical path, total float, earliest/latest dates |
| Earned Value (EVM) | SPI, CPI, SV, CV, BAC, EAC, trend charts |
| Resource Management | Resource allocation,负荷 chart, resource leveling |
| Work Calendar | Custom working days/holidays, auto-scheduling |
| Analysis Reports | Implementation analysis, monthly reports, annual overview, multi-project health matrix |
| AI Assistant | Describe your project in natural language, AI creates tasks and schedules automatically |
| Export | Excel export |

## Tech Stack

- **Framework**: Blazor Server (.NET 8)
- **Database**: SQLite (EF Core)
- **Frontend**: Vanilla JavaScript + Chart.js
- **AI**: DeepSeek / GLM large language models

## License

MIT License — see [LICENSE](LICENSE) file.

## Status

Current version **v1.7i** — core features are stable. Issues and Pull Requests welcome.

---

[中文版](README.md)
