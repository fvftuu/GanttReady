# NetPlan - 进度计划系统

## 技术架构文档

### 项目概述
- **项目名称**：NetPlan（网络计划）
- **技术栈**：Blazor Server + .NET 8 + SQLite + Ant Design Blazor
- **部署目标**：单机 + 局域网
- **开源协议**：MIT

---

## 技术架构图

```
┌─────────────────────────────────────────────────────────────┐
│                      用户界面层 (UI)                         │
├─────────────────────────────────────────────────────────────┤
│  ┌──────────────┐  ┌──────────────┐  ┌──────────────┐       │
│  │   甘特图      │  │   网络图      │  │   分析面板    │       │
│  │  (AntD Gantt)│  │ (Cytoscape.js)│  │  (Chart.js)  │       │
│  └──────────────┘  └──────────────┘  └──────────────┘       │
├─────────────────────────────────────────────────────────────┤
│                      业务逻辑层 (BLL)                        │
├─────────────────────────────────────────────────────────────┤
│  ┌──────────────┐  ┌──────────────┐  ┌──────────────┐       │
│  │   任务调度    │  │   资源管理    │  │   计划分析    │       │
│  │   引擎        │  │   服务       │  │   服务       │       │
│  │  (CPM算法)    │  │             │  │             │       │
│  └──────────────┘  └──────────────┘  └──────────────┘       │
├─────────────────────────────────────────────────────────────┤
│                      数据访问层 (DAL)                        │
├─────────────────────────────────────────────────────────────┤
│  ┌──────────────────────────────────────────────────┐       │
│  │            Entity Framework Core + SQLite         │       │
│  └──────────────────────────────────────────────────┘       │
└─────────────────────────────────────────────────────────────┘
                              │
                              ▼
                    ┌──────────────────┐
                    │   SQLite 数据库   │
                    │  (Project.db)    │
                    └──────────────────┘
```

---

## 项目结构

```
NetPlan/
├── NetPlan.sln                          # 解决方案文件
├── src/
│   └── NetPlan.Server/                  # 主项目（Blazor Server）
│       ├── Program.cs                   # 入口点
│       ├── appsettings.json             # 配置文件
│       │
│       ├── Components/                  # 通用Blazor组件
│       │   ├── GanttChart.razor        # 甘特图组件
│       │   ├── NetworkDiagram.razor     # 网络图组件
│       │   ├── TaskGrid.razor           # 任务表格组件
│       │   ├── ResourceGrid.razor       # 资源表格组件
│       │   └── DynamicColumn.razor      # 动态列组件
│       │
│       ├── Pages/                       # 页面
│       │   ├── Index.razor              # 首页/项目列表
│       │   ├── Project/
│       │   │   ├── Gantt.razor          # 甘特图页面
│       │   │   ├── Network.razor         # 网络图页面
│       │   │   ├── Resources.razor       # 资源管理页面
│       │   │   └── Analysis.razor        # 计划分析页面
│       │   └── Settings/
│       │       └── ColumnConfig.razor   # 列配置页面
│       │
│       ├── Models/                      # 实体模型
│       │   ├── Project.cs               # 项目
│       │   ├── TaskItem.cs              # 任务
│       │   ├── TaskRelation.cs          # 任务关系（FS/SS/SF/FF）
│       │   ├── Resource.cs              # 资源
│       │   ├── ResourceAssignment.cs    # 资源分配
│       │   └── ColumnDefinition.cs      # 列定义
│       │
│       ├── Data/                        # 数据库上下文
│       │   └── NetPlanDbContext.cs     # EF Core DbContext
│       │
│       ├── Services/                    # 业务服务
│       │   ├── IScheduleEngine.cs        # 调度引擎接口
│       │   ├── ScheduleEngine.cs         # 调度引擎实现（CPM）
│       │   ├── IProjectService.cs        # 项目服务接口
│       │   ├── ProjectService.cs         # 项目服务实现
│       │   ├── IResourceService.cs        # 资源服务接口
│       │   ├── ResourceService.cs         # 资源服务实现
│       │   ├── IAnalysisService.cs        # 分析服务接口
│       │   └── AnalysisService.cs         # 分析服务实现
│       │
│       └── _Imports.razor               # 全局引用
│
├── tests/
│   └── NetPlan.Tests/                   # 单元测试
│       ├── ScheduleEngineTests.cs       # 调度引擎测试
│       └── ProjectServiceTests.cs        # 项目服务测试
│
└── docs/
    ├── API.md                           # API文档
    └── 数据库设计.md                      # 数据库设计文档
```

---

## 核心数据模型

### 1. Project（项目）
```csharp
public class Project
{
    public int Id { get; set; }
    public string Code { get; set; }           // 项目代号
    public string Name { get; set; }            // 项目名称
    public DateTime PlanStartDate { get; set; } // 计划开始日期
    public DateTime PlanEndDate { get; set; }   // 计划结束日期
    public DateTime? ActualStartDate { get; set; } // 实际开始日期
    public DateTime? ActualEndDate { get; set; }  // 实际结束日期
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public ICollection<TaskItem> Tasks { get; set; }
    public ICollection<Resource> Resources { get; set; }
}
```

### 2. TaskItem（任务）
```csharp
public class TaskItem
{
    public int Id { get; set; }
    public int ProjectId { get; set; }
    public string Code { get; set; }            // 任务代号
    public string Name { get; set; }            // 任务名称
    public int SortOrder { get; set; }          // 排序顺序
    public int? ParentTaskId { get; set; }      // 父任务（支持WBS层级）

    // 计划信息
    public DateTime PlanStartDate { get; set; }
    public DateTime PlanEndDate { get; set; }
    public int PlanDuration { get; set; }       // 计划工期（天）

    // 实际信息
    public DateTime? ActualStartDate { get; set; }
    public DateTime? ActualEndDate { get; set; }
    public int? ActualDuration { get; set; }    // 实际工期

    // 计算字段（调度后填充）
    public int? EarlyStart { get; set; }         // 最早开始（相对于项目开始）
    public int? EarlyFinish { get; set; }        // 最早完成
    public int? LateStart { get; set; }          // 最迟开始
    public int? LateFinish { get; set; }         // 最迟完成
    public int? TotalFloat { get; set; }         // 总时差
    public int? FreeFloat { get; set; }          // 自由时差
    public bool IsCritical { get; set; }         // 是否关键工序

    // 扩展字段（JSON存储）
    public string ExtraData { get; set; }         // 扩展数据（JSON）

    public Project Project { get; set; }
    public ICollection<TaskRelation> Predecessors { get; set; }
    public ICollection<TaskRelation> Successors { get; set; }
    public ICollection<ResourceAssignment> ResourceAssignments { get; set; }
}
```

### 3. TaskRelation（任务关系）
```csharp
public class TaskRelation
{
    public int Id { get; set; }
    public int PredecessorTaskId { get; set; }   // 紧前任务
    public int SuccessorTaskId { get; set; }     // 紧后任务
    public RelationType Type { get; set; }       // 关系类型
    public int Lag { get; set; }                 // 时差（天），可为负数

    public TaskItem PredecessorTask { get; set; }
    public TaskItem SuccessorTask { get; set; }
}

public enum RelationType
{
    FS,  // Finish to Start（完成-开始）- 默认
    SS,  // Start to Start（开始-开始）
    SF,  // Start to Finish（开始-完成）
    FF   // Finish to Finish（完成-完成）
}
```

### 4. Resource（资源）
```csharp
public class Resource
{
    public int Id { get; set; }
    public int ProjectId { get; set; }
    public string Code { get; set; }            // 资源代号
    public string Name { get; set; }            // 资源名称
    public ResourceType Type { get; set; }       // 资源类型
    public string Unit { get; set; }             // 单位
    public decimal Quantity { get; set; }        // 数量
    public decimal UnitPrice { get; set; }      // 单价
    public string ExtraData { get; set; }        // 扩展数据

    public Project Project { get; set; }
    public ICollection<ResourceAssignment> Assignments { get; set; }
}

public enum ResourceType
{
    Labor,     // 人（工日/人天）
    Material,  // 材（材料）
    Equipment  // 机（机械台班）
}
```

### 5. ResourceAssignment（资源分配）
```csharp
public class ResourceAssignment
{
    public int Id { get; set; }
    public int TaskId { get; set; }
    public int ResourceId { get; set; }
    public decimal Quantity { get; set; }       // 分配数量

    public TaskItem Task { get; set; }
    public Resource Resource { get; set; }
}
```

### 6. ColumnDefinition（列定义）
```csharp
public class ColumnDefinition
{
    public int Id { get; set; }
    public string ViewName { get; set; }         // 视图名称（Gantt/Grid）
    public string FieldName { get; set; }        // 字段名
    public string DisplayName { get; set; }     // 显示名称
    public int Width { get; set; }               // 列宽
    public int SortOrder { get; set; }           // 排序顺序
    public bool IsVisible { get; set; }          // 是否可见
    public bool IsEditable { get; set; }         // 是否可编辑
}
```

---

## 调度引擎算法（CPM - 关键路径法）

### 1. 正向计算（计算最早时间）
```
对于每个任务：
  ES = max(所有紧前任务的EF + Lag)
  EF = ES + Duration
```

### 2. 反向计算（计算最迟时间）
```
项目工期 = max(所有任务的EF)
对于每个任务（逆序）：
  LF = min(所有紧后任务的LS - Lag)
  LS = LF - Duration
```

### 3. 时差计算
```
TotalFloat = LS - ES  （或 LF - EF）
FreeFloat = 后续任务ES - EF - Lag
IsCritical = (TotalFloat == 0)
```

### 4. 关系类型处理
```csharp
// FS: successor.ES = predecessor.EF + Lag
// SS: successor.ES = predecessor.ES + Lag
// SF: successor.LF = predecessor.EF + Lag
// FF: successor.LF = predecessor.EF + Lag
```

---

## 甘特图实现方案

### 方案：Ant Design Blazor + 自定义渲染

```razor
<!-- GanttChart.razor -->
<div class="gantt-container">
    <!-- 左侧：任务列表（可冻结窗格） -->
    <div class="gantt-left">
        <DynamicColumnGrid Items="Tasks"
                          Columns="VisibleColumns"
                          OnColumnConfig="OpenColumnConfig" />
    </div>

    <!-- 右侧：甘特条形图 -->
    <div class="gantt-right" style="width: @(TotalDays * DayWidth)px">
        <GanttBars Tasks="Tasks"
                   DayWidth="DayWidth"
                   ScrollPosition="ScrollLeft"
                   OnTaskClick="SelectTask" />
    </div>
</div>

@code {
    [Parameter] public List<TaskItem> Tasks { get; set; }
    [Parameter] public double DayWidth { get; set; } = 40;
    // 支持 5%-1500% 缩放：DayWidth = 40 * (ZoomLevel / 100)
}
```

### 甘特图缩放实现
```csharp
// 缩放级别映射
var zoomLevels = new Dictionary<string, double>
{
    { "5%", 2 },
    { "10%", 4 },
    { "25%", 10 },
    { "50%", 20 },
    { "75%", 30 },
    { "100%", 40 },   // 默认
    { "150%", 60 },
    { "200%", 80 },
    { "300%", 120 },
    { "500%", 200 },
    { "1000%", 400 },
    { "1500%", 600 }
};
```

---

## 网络图实现方案

### 方案：Cytoscape.js（通过JS互操作）

```razor
<!-- NetworkDiagram.razor -->
<div class="network-container">
    <div id="cy" style="width: 100%; height: 600px;"></div>
</div>

@code {
    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            await JS.InvokeVoidAsync("initCytoscape", GetCyElements(), GetCyStyle());
        }
    }

    private async Task OnNodeClick(string nodeId)
    {
        // 处理节点点击事件
        SelectedTask = Tasks.FirstOrDefault(t => t.Id.ToString() == nodeId);
    }
}
```

### Cytoscape 配置
```javascript
function initCytoscape(elements, style) {
    var cy = cytoscape({
        container: document.getElementById('cy'),
        elements: elements,
        style: [
            {
                selector: 'node',
                style: {
                    'label': 'data(label)',
                    'background-color': 'data(isCritical) ? "#ff4d4f" : "#1890ff"'
                }
            },
            {
                selector: 'edge',
                style: {
                    'width': 2,
                    'line-color': '#ccc',
                    'target-arrow-color': '#ccc',
                    'target-arrow-shape': 'triangle'
                }
            }
        ],
        layout: { name: 'dagre', rankDir: 'TB' }
    });
}
```

---

## 动态列实现

```csharp
// DynamicColumnService.cs
public List<ColumnDefinition> GetVisibleColumns(string viewName)
{
    return DbContext.ColumnDefinitions
        .Where(c => c.ViewName == viewName && c.IsVisible)
        .OrderBy(c => c.SortOrder)
        .ToList();
}

public async Task UpdateColumnVisibility(string viewName, int columnId, bool isVisible)
{
    var column = await DbContext.ColumnDefinitions
        .FirstOrDefaultAsync(c => c.Id == columnId && c.ViewName == viewName);

    if (column != null)
    {
        column.IsVisible = isVisible;
        await DbContext.SaveChangesAsync();
    }
}
```

```razor
<!-- DynamicColumnGrid.razor -->
<Table TItem="TaskItem" DataSource="Items">
    @foreach (var col in Columns)
    {
        <PropertyColumn Property="t => t.@col.FieldName"
                        Title="@col.DisplayName"
                        Width="@col.Width"
                        Editable="@col.IsEditable" />
    }
    <ActionColumn Title="操作">
        <Button @onclick="() => Edit(context)">编辑</Button>
    </ActionColumn>
</Table>
```

---

## 部署方案

### 方案1：局域网 IIS 部署（推荐）

**服务器要求**：
- Windows Server 2019+
- IIS 10+
- .NET 8 Runtime

**部署步骤**：
1. 发布应用：`dotnet publish -c Release`
2. 配置 IIS：
   - 创建应用程序池（.NET 8）
   - 创建网站或应用程序
   - 绑定端口（如 5000）
3. 用户访问：`http://server-ip:5000`

### 方案2：单机自宿主

```powershell
# 运行命令
dotnet NetPlan.Server.dll --urls "http://0.0.0.0:5000"

# 用户访问
http://localhost:5000          # 本机
http://192.168.1.100:5000       # 局域网其他电脑
```

### 方案3：Docker 容器

```dockerfile
FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app
COPY publish /app
EXPOSE 5000
ENTRYPOINT ["dotnet", "NetPlan.Server.dll"]
```

```bash
# 构建和运行
docker build -t netplan:latest .
docker run -p 5000:80 netplan:latest
```

---

## 性能优化

| 优化点 | 方案 |
|-------|------|
| 大数据量 | 虚拟滚动（Virtual Scrolling） |
| 甘特图渲染 | Canvas 替代 SVG |
| 网络图 | 懒加载节点 |
| 数据库 | SQLite 索引 + 分页 |
| 缓存 | 内存缓存 + CircuitHandler |

---

## 后续开发计划

| 迭代 | 功能 | 预计工时 |
|------|------|---------|
| **v1.0** | 项目管理 + 任务CRUD + 基础甘特图 | 2周 |
| **v1.1** | 甘特图缩放 + 冻结窗格 + 动态列 | 1周 |
| **v1.2** | 任务关系（FS/SS/SF/FF）+ 调度引擎 | 2周 |
| **v1.3** | 网络图（Cytoscape.js） | 1周 |
| **v1.4** | 资源管理 + 资源分配 | 1周 |
| **v1.5** | 计划分析（工期/资源/进度） | 2周 |
| **v2.0** | Excel导入导出 + 打印 | 1周 |

---

## 参考资源

- [Ant Design Blazor](https://antblazor.com/)
- [Cytoscape.js](https://js.cytoscape.org/)
- [Blazor 官方文档](https://docs.microsoft.com/zh-cn/aspnet/core/blazor/)
- [.NET 8 发布指南](https://docs.microsoft.com/zh-cn/aspnet/core/publishing/)
